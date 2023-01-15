/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Validation;
using ApplicationAccess.Persistence;
using ApplicationAccess.Metrics;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in a multi-reader, single-writer ApplicationAccess hosting environment which allows writing permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class WriterNode<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        /// <summary>AccessManager instance which backs the event validator.</summary>
        protected MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> concurrentAccessManager;
        /// <summary>Validates events passed to the event buffer.</summary>
        protected IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator;
        /// <summary>Buffers events which change the AccessManager, writing them to the <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instance and the event cache.</summary>
        protected AccessManagerTemporalEventPersisterBufferBase<TUser, TGroup, TComponent, TAccess> eventBuffer;
        /// <summary>Flush strategy for the event buffer.</summary>
        protected IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to load the complete state of the AccessManager instance.</summary>
        protected IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader;
        /// <summary>Caches recent events which changed the AccessManager, so the can be accessed quickly by <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}">ReaderNodes</see></summary>
        protected IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache;        
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which changed the AccessManager.</param>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache
        )
        {
            metricLogger = new NullMetricLogger();
            Initialize(eventBufferFlushStrategy, persistentReader, eventCache);
            var eventDistributor = new AccessManagerTemporalEventPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>>(){ eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which changed the AccessManager.</param>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache
        )
        {
            metricLogger = new NullMetricLogger();
            Initialize(eventBufferFlushStrategy, persistentReader, eventCache);
            var eventDistributor = new AccessManagerTemporalEventBulkPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which changed the AccessManager.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache,
            IMetricLogger metricLogger
        )
        {
            this.metricLogger = metricLogger;
            Initialize(eventBufferFlushStrategy, persistentReader, eventCache);
            var eventDistributor = new AccessManagerTemporalEventPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which changed the AccessManager.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache,
            IMetricLogger metricLogger
        )
        {
            this.metricLogger = metricLogger;
            Initialize(eventBufferFlushStrategy, persistentReader, eventCache);
            var eventDistributor = new AccessManagerTemporalEventBulkPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor);
        }

        /// <summary>
        /// Loads the latest state of the AccessManager from persistent storage.
        /// </summary>
        /// <param name="throwExceptionIfStorageIsEmpty">When set true an exception will be thrown in the case that the persistent storage is empty.</param>
        public void Load(Boolean throwExceptionIfStorageIsEmpty)
        {
            try
            {
                concurrentAccessManager.MetricLoggingEnabled = false;
                persistentReader.Load(concurrentAccessManager);
                concurrentAccessManager.MetricLoggingEnabled = true;
            }
            catch (PersistentStorageEmptyException pse)
            {
                if (throwExceptionIfStorageIsEmpty == true)
                {
                    throw new Exception("Failed to load access manager state from persistent storage.", pse);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load access manager state from persistent storage.", e);
            }
        }

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            eventBuffer.AddUser(user);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            eventBuffer.RemoveUser(user);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            eventBuffer.AddGroup(group);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            eventBuffer.RemoveGroup(group);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            eventBuffer.AddUserToGroupMapping(user, group);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            eventBuffer.RemoveUserToGroupMapping(user, group);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventBuffer.AddGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            eventBuffer.AddEntityType(entityType);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            eventBuffer.RemoveEntityType(entityType);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            eventBuffer.AddEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            eventBuffer.RemoveEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventBuffer.AddUserToEntityMapping(user, entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventBuffer.RemoveUserToEntityMapping(user, entityType, entity);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventBuffer.AddGroupToEntityMapping(group, entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventBuffer.RemoveGroupToEntityMapping(group, entityType, entity);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes members which are common between all constructors.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventCache">Cache for events which changed the AccessManager.</param>
        public void Initialize
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy, 
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader, 
            IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache
        )
        {
            this.eventBufferFlushStrategy = eventBufferFlushStrategy;
            this.persistentReader = persistentReader;
            this.eventCache = eventCache;
            concurrentAccessManager = new MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>(metricLogger);
            eventValidator = new ConcurrentAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>(concurrentAccessManager);
            disposed = false;
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the WriterNode.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~WriterNode()
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
                    eventBuffer.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
