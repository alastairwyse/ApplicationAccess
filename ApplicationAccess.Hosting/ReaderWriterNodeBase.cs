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
using ApplicationAccess.Validation;
using ApplicationAccess.Persistence;
using ApplicationAccess.Metrics;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// Base for nodes in a single-reader, single-writer ApplicationAccess hosting environment which handle both reading and writing of permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <typeparam name="TAccessManager">The subclass of <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> which should be used to read and write the permissions and authorizations.</typeparam>
    /// <remarks>Note that as per remarks for <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> interval metrics are not logged for <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> methods that return <see cref="IEnumerable{T}"/>, or perform simple dictionary and set lookups (e.g. <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}.ContainsUser(TUser)">ContainsUser()</see>).  If these metrics are required, they must be logged outside of this class.  In the case of methods that return <see cref="IEnumerable{T}"/> the metric logging must wrap the code that enumerates the result.</remarks>
    public abstract class ReaderWriterNodeBase<TUser, TGroup, TComponent, TAccess, TAccessManager> : IAccessManager<TUser, TGroup, TComponent, TAccess>, IMetricLoggingComponent, IDisposable
       where TAccessManager : ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>, IMetricLoggingComponent
    {
        /// <summary>The AccessManager instance used to store all permissions and back the event validator.</summary>
        protected TAccessManager concurrentAccessManager;
        /// <summary>Validates events created by calls to <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> methods.</summary>
        protected IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator;
        /// <summary>Buffers events events created by calls to <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> methods.</summary>
        protected AccessManagerTemporalEventPersisterBufferBase<TUser, TGroup, TComponent, TAccess> eventBuffer;
        /// <summary>Flush strategy for the event buffer.</summary>
        protected IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to load the complete state of the AccessManager instance.</summary>
        protected IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <inheritdoc/>
        public Boolean MetricLoggingEnabled
        {
            get
            {
                return concurrentAccessManager.MetricLoggingEnabled;
            }

            set
            {
                concurrentAccessManager.MetricLoggingEnabled = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderWriterNodeBase class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        public ReaderWriterNodeBase
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        )
        {
            metricLogger = new NullMetricLogger();
            Initialize(eventBufferFlushStrategy, persistentReader);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderWriterNodeBase class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        public ReaderWriterNodeBase
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        )
        {
            metricLogger = new NullMetricLogger();
            Initialize(eventBufferFlushStrategy, persistentReader);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderWriterNodeBase class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ReaderWriterNodeBase
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger
        )
        {
            this.metricLogger = metricLogger;
            Initialize(eventBufferFlushStrategy, persistentReader);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderWriterNodeBase class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ReaderWriterNodeBase
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger
        )
        {
            this.metricLogger = metricLogger;
            Initialize(eventBufferFlushStrategy, persistentReader);
        }

        /// <summary>
        /// Loads the latest state of the AccessManager from persistent storage.
        /// </summary>
        /// <param name="throwExceptionIfStorageIsEmpty">When set true an exception will be thrown in the case that the persistent storage is empty.</param>
        public void Load(Boolean throwExceptionIfStorageIsEmpty)
        {
            Guid beginId = metricLogger.Begin(new ReaderWriterNodeLoadTime());
            try
            {
                concurrentAccessManager.MetricLoggingEnabled = false;
                persistentReader.Load(concurrentAccessManager);
            }
            catch (PersistentStorageEmptyException pse)
            {
                if (throwExceptionIfStorageIsEmpty == true)
                {
                    metricLogger.CancelBegin(beginId, new ReaderWriterNodeLoadTime());
                    throw new Exception("Failed to load access manager state from persistent storage.", pse);
                }
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new ReaderWriterNodeLoadTime());
                throw new Exception("Failed to load access manager state from persistent storage.", e);
            }
            finally
            {
                concurrentAccessManager.MetricLoggingEnabled = true;
            }
            metricLogger.End(beginId, new ReaderWriterNodeLoadTime());
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> Users
        {
            get
            {
                return concurrentAccessManager.Users;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                return concurrentAccessManager.Groups;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                return concurrentAccessManager.EntityTypes;
            }
        }

        /// <inheritdoc/>
        public Boolean ContainsUser(TUser user)
        {
            return concurrentAccessManager.ContainsUser(user);
        }

        /// <inheritdoc/>
        public Boolean ContainsGroup(TGroup group)
        {
            return concurrentAccessManager.ContainsGroup(group);
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetUserToGroupMappings(user, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetGroupToUserMappings(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetGroupToGroupMappings(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetGroupToGroupReverseMappings(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return concurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(user);
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return concurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(group);
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public Boolean ContainsEntityType(String entityType)
        {
            return concurrentAccessManager.ContainsEntityType(entityType);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            return concurrentAccessManager.GetEntities(entityType);
        }

        /// <inheritdoc/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            return concurrentAccessManager.ContainsEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return concurrentAccessManager.GetUserToEntityMappings(user);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return concurrentAccessManager.GetUserToEntityMappings(user, entityType);
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetEntityToUserMappings(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return concurrentAccessManager.GetGroupToEntityMappings(group);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return concurrentAccessManager.GetGroupToEntityMappings(group, entityType);
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetEntityToGroupMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return concurrentAccessManager.GetEntityToGroupMappings(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return concurrentAccessManager.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            return concurrentAccessManager.HasAccessToEntity(user, entityType, entity);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            return concurrentAccessManager.GetApplicationComponentsAccessibleByUser(user);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            return concurrentAccessManager.GetApplicationComponentsAccessibleByGroup(group);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByUser(user);
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByUser(user, entityType);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByGroup(group);
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            return concurrentAccessManager.GetEntitiesAccessibleByGroup(group, entityType);
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
        public void Initialize
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy, 
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader
        )
        {
            this.eventBufferFlushStrategy = eventBufferFlushStrategy;
            this.persistentReader = persistentReader;
            concurrentAccessManager = InitializeAccessManager();
            eventValidator = new ConcurrentAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>(concurrentAccessManager);
            disposed = false;
        }

        /// <summary>
        /// Initializes a new <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance used to read and write the permissions and authorizations.
        /// </summary>
        /// <returns>The new <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance.</returns>
        protected abstract TAccessManager InitializeAccessManager();

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the ReaderWriterNodeBase.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~ReaderWriterNodeBase()
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
