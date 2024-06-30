/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationAccess.Utilities;
using ApplicationAccess.Validation;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// Base for nodes in a multi-reader, single-writer ApplicationAccess hosting environment which allow reading permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <typeparam name="TAccessManager">The subclass of <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> which should be used to store the permissions and authorizations.</typeparam>
    /// <remarks>Note that as per remarks for <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> interval metrics are not logged for <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> methods that return <see cref="IEnumerable{T}"/>, or perform simple dictionary and set lookups (e.g. <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}.ContainsUser(TUser)">ContainsUser()</see>).  If these metrics are required, they must be logged outside of this class.  In the case of methods that return <see cref="IEnumerable{T}"/> the metric logging must wrap the code that enumerates the result.</remarks>
    public abstract class ReaderNodeBase<TUser, TGroup, TComponent, TAccess, TAccessManager> : IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>, IDisposable
        where TAccessManager : ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>, IMetricLoggingComponent
    {
        /// <summary>The strategy/methodology to use to refresh the contents of the reader node.</summary>
        protected IReaderNodeRefreshStrategy refreshStrategy;
        /// <summary>Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</summary>
        protected IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess> eventCache;
        /// <summary>Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</summary>
        protected IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader;
        /// <summary>The AccessManager which stores the permissions and authorizations for the application.</summary>
        protected TAccessManager concurrentAccessManager;
        /// <summary>The provider to use for the current date and time.</summary>
        protected Utilities.IDateTimeProvider dateTimeProvider;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>The id of the most recent event which changed the AccessManager.</summary>
        protected Guid latestEventId;
        /// <summary>The delegate which handles an <see cref="IReaderNodeRefreshStrategy.ReaderNodeRefreshed">ReaderNodeRefreshed</see> event.</summary>
        protected EventHandler refreshedEventHandler;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderNodeBase class.
        /// </summary>
        /// <param name="refreshStrategy">The strategy/methodology to use to refresh the contents of the reader node.</param>
        /// <param name="eventCache">Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</param>
        /// <param name="persistentReader">Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</param>
        public ReaderNodeBase(IReaderNodeRefreshStrategy refreshStrategy, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess> eventCache, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader)
        {
            this.refreshStrategy = refreshStrategy;
            this.eventCache = eventCache;
            this.persistentReader = persistentReader;
            dateTimeProvider = new DefaultDateTimeProvider();
            metricLogger = new NullMetricLogger();
            concurrentAccessManager = InitializeAccessManager();
            // Subscribe to the refreshStrategy's 'ReaderNodeRefreshed' event
            refreshedEventHandler = (Object sender, EventArgs e) => { Refresh(); };
            refreshStrategy.ReaderNodeRefreshed += refreshedEventHandler;
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderNodeBase class.
        /// </summary>
        /// <param name="refreshStrategy">The strategy/methodology to use to refresh the contents of the reader node.</param>
        /// <param name="eventCache">Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</param>
        /// <param name="persistentReader">Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ReaderNodeBase(IReaderNodeRefreshStrategy refreshStrategy, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess> eventCache, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader, IMetricLogger metricLogger)
            : this(refreshStrategy, eventCache, persistentReader)
        {
            // Below inclusion filter outputs only ReaderNode specific metrics, and standard query count and interval metrics (i.e. not event metrics)
            //   Reason for this is that event metrics are only generated when the node refreshes... i.e. they don't capture the actual timing of when
            //   those events occurred (that's captured in the WriterNode), nor do they accurately represent the duration of them (again captured in
            //   the WriterNode).  Hence capturing just query count and interval metrics gives a sufficient picture of the activity of the ReaderNode.
            this.metricLogger = new MetricLoggerBaseTypeInclusionFilter
            (
                metricLogger,
                new List<Type>() { typeof(ReaderNodeCountMetric), typeof(QueryCountMetric) },
                new List<Type>() { typeof(ReaderNodeAmountMetric) },
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(ReaderNodeIntervalMetric), typeof(QueryIntervalMetric) }
            );
            concurrentAccessManager = InitializeAccessManager();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderNodeBase class.
        /// </summary>
        /// <param name="refreshStrategy">The strategy/methodology to use to refresh the contents of the reader node.</param>
        /// <param name="eventCache">Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</param>
        /// <param name="persistentReader">Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public ReaderNodeBase(IReaderNodeRefreshStrategy refreshStrategy, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess> eventCache, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader, IMetricLogger metricLogger, Utilities.IDateTimeProvider dateTimeProvider)
            : this(refreshStrategy, eventCache, persistentReader, metricLogger)
        {
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TUser> Users
        {
            get
            {
                return concurrentAccessManager.Users;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                return concurrentAccessManager.Groups;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                return concurrentAccessManager.EntityTypes;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsUser(TUser user)
        {
            return concurrentAccessManager.ContainsUser(user);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsGroup(TGroup group)
        {
            return concurrentAccessManager.ContainsGroup(group);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetUserToGroupMappings(user, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetGroupToUserMappings(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetGroupToGroupMappings(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetGroupToGroupReverseMappings(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return concurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(user);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return concurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(group);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsEntityType(String entityType)
        {
            return concurrentAccessManager.ContainsEntityType(entityType);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> GetEntities(String entityType)
        {
            return concurrentAccessManager.GetEntities(entityType);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            return concurrentAccessManager.ContainsEntity(entityType, entity);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return concurrentAccessManager.GetUserToEntityMappings(user);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return concurrentAccessManager.GetUserToEntityMappings(user, entityType);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetEntityToUserMappings(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return concurrentAccessManager.GetGroupToEntityMappings(group);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return concurrentAccessManager.GetGroupToEntityMappings(group, entityType);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TGroup> GetEntityToGroupMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetEntityToGroupMappings(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return concurrentAccessManager.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            return concurrentAccessManager.HasAccessToEntity(user, entityType, entity);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            return concurrentAccessManager.GetApplicationComponentsAccessibleByUser(user);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            return concurrentAccessManager.GetApplicationComponentsAccessibleByGroup(group);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByUser(user);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByUser(user, entityType);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByGroup(group);
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByGroup(group, entityType);
        }

        /// <summary>
        /// Loads all permissions and authorizations from persistent storage.
        /// </summary>
        /// <param name="throwExceptionIfStorageIsEmpty">When set true an exception will be thrown in the case that the persistent storage is empty.</param>
        public void Load(Boolean throwExceptionIfStorageIsEmpty)
        {
            TAccessManager newAccessManager = InitializeAccessManager();
            newAccessManager.MetricLoggingEnabled = false;
            Guid beginId = metricLogger.Begin(new ReaderNodeLoadTime());
            try
            {
                AccessManagerState state = persistentReader.Load(newAccessManager);
                latestEventId = state.EventId;
            }
            catch (PersistentStorageEmptyException pse)
            {
                if (throwExceptionIfStorageIsEmpty == true)
                {
                    metricLogger.CancelBegin(beginId, new ReaderNodeLoadTime());
                    throw new Exception("Failed to load access manager state from persistent storage.", pse);
                }
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new ReaderNodeLoadTime());
                throw new Exception("Failed to load access manager state from persistent storage.", e);
            }
            newAccessManager.MetricLoggingEnabled = true;
            Interlocked.Exchange(ref concurrentAccessManager, newAccessManager);
            metricLogger.End(beginId, new ReaderNodeLoadTime());
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes a new <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance used for underlying storage of permissions and authorizations.
        /// </summary>
        /// <returns>A new <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance.</returns>
        protected abstract TAccessManager InitializeAccessManager();

        /// <summary>
        /// Refreshes/updates the contents of the AccessManager by first attempting to retrieve any events occurring after the one stored in 'latestEventId' and applying them to the AccessManager instance, or by reading the entire latest state from persistent storage if the event retrieval fails.
        /// </summary>
        protected void Refresh()
        {
            Guid beginId = metricLogger.Begin(new RefreshTime());
            IList<TemporalEventBufferItemBase> updateEvents = null;
            try
            {
                updateEvents = eventCache.GetAllEventsSince(latestEventId);
            }
            catch (EventCacheEmptyException)
            {
                // This will usually occur soon after starting the ReaderNode and EventCache, when no event have yet flowed through the system
                //   In this case capture the specific exception, but don't do anything... i.e. don't Load() again, as it will just reload the exact same data that was already loaded on start
                //   (assuming this node was created as part of a ReaderNodeHostedServiceWrapper)
                metricLogger.Increment(new EventCacheEmpty());
            }
            catch (EventNotCachedException)
            {
                metricLogger.Increment(new CacheMiss());
                try
                {
                    // TODO: Need to think about what this below Boolean parameter should be.
                    //   It's realistic with a new deployment that a reader node refresh could occur before any events have been written...
                    //   in those cases setting below true would result in exception even thought nothing had actually gone wrong.
                    //   On the flipside, making false always could miss picking strange cases where no events are loaded in error
                    //   Need to think about this...
                    Load(false);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new RefreshTime());
                    throw new ReaderNodeRefreshException($"Failed to refresh the entire contents of the reader node.", e);
                }
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new RefreshTime());
                throw new ReaderNodeRefreshException($"Failed to retrieve latest access manager events following event '{latestEventId}' from cache.", e);
            }

            if (updateEvents != null)
            {
                metricLogger.Add(new CachedEventsReceived(), updateEvents.Count);
                if (updateEvents.Count > 0)
                {
                    var eventProcessor = new AccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>(concurrentAccessManager);
                    foreach (TemporalEventBufferItemBase currentEvent in updateEvents)
                    {
                        eventProcessor.Process(currentEvent);
                        TimeSpan processingDelay = dateTimeProvider.UtcNow() - currentEvent.OccurredTime;
                        metricLogger.Add(new EventProcessingDelay(), Convert.ToInt64(Math.Round(processingDelay.TotalMilliseconds)));
                    }
                    latestEventId = updateEvents[updateEvents.Count - 1].EventId;
                }
            }
            metricLogger.End(beginId, new RefreshTime());
            metricLogger.Increment(new RefreshOperationCompleted());
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the ReaderNode.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~ReaderNodeBase()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    refreshStrategy.ReaderNodeRefreshed -= refreshedEventHandler;
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
