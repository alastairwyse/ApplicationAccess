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

using ApplicationAccess.Validation;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Subclass of <see cref="AccessManagerTemporalEventBulkPersisterBuffer{TUser, TGroup, TComponent, TAccess}"/> where...
    /// <para>1. Event methods can be called successfully without first satisfying dependecies which are required by <see cref="AccessManagerTemporalEventBulkPersisterBuffer{TUser, TGroup, TComponent, TAccess}"/>, e.g. the AddUserToGroupMapping() method can be used to add a user to group mapping, without first explicitly adding the user and group.</para>
    /// <para>2. Event methods are idempotent, e.g. the AddUserToGroupMapping() method will return success if the specified mapping already exists.</para>
    /// The class is designed to work in conjunction with an <see cref="IAccessManagerEventValidator{TUser, TGroup, TComponent, TAccess}"/> implementation which wraps a <see cref="DependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance, which takes care of generating any required events to be prepended before the requested event.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        public DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        ) : base(eventValidator, bufferFlushStrategy, eventPersister)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger
        ) : base(eventValidator, bufferFlushStrategy, eventPersister, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger,
            IGuidProvider guidProvider,
            IDateTimeProvider dateTimeProvider
        ) : base(eventValidator, bufferFlushStrategy, eventPersister, metricLogger, guidProvider, dateTimeProvider)
        {
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user)
        {
            if (lockManager.LockObjectIsLockedByCurrentThread(userEventBufferLock) == true)
            {
                BufferAddUserEventAction.Invoke(user);
            }
            else
            {
                base.AddUser(user);
            }
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group)
        {
            if (lockManager.LockObjectIsLockedByCurrentThread(groupEventBufferLock) == true)
            {
                BufferAddGroupEventAction.Invoke(group);
            }
            else
            {
                base.AddGroup(group);
            }
        }

        /// <inheritdoc/>
        public override void AddEntityType(string entityType)
        {
            if (lockManager.LockObjectIsLockedByCurrentThread(entityTypeEventBufferLock) == true)
            {
                BufferAddEntityTypeEventAction.Invoke(entityType);
            }
            else
            {
                base.AddEntityType(entityType);
            }
        }

        /// <inheritdoc/>
        public override void AddEntity(string entityType, string entity)
        {
            if (lockManager.LockObjectIsLockedByCurrentThread(entityEventBufferLock) == true)
            {
                BufferAddEntityEventAction.Invoke(entityType, entity);
            }
            else
            {
                base.AddEntity(entityType, entity);
            }
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override void InitializeLockObjects()
        {
            base.InitializeLockObjects();
            lockManager.RegisterLockObjectDependency(userToGroupMappingEventBufferLock, userEventBufferLock);
            lockManager.RegisterLockObjectDependency(userToGroupMappingEventBufferLock, groupEventBufferLock);
            lockManager.RegisterLockObjectDependency(groupToGroupMappingEventBufferLock, groupEventBufferLock);
            lockManager.RegisterLockObjectDependency(userToApplicationComponentAndAccessLevelMappingEventBufferLock, userEventBufferLock);
            lockManager.RegisterLockObjectDependency(entityEventBufferLock, entityTypeEventBufferLock);
            lockManager.RegisterLockObjectDependency(userToEntityMappingEventBufferLock, userEventBufferLock);
            lockManager.RegisterLockObjectDependency(userToEntityMappingEventBufferLock, entityTypeEventBufferLock);
            lockManager.RegisterLockObjectDependency(userToEntityMappingEventBufferLock, entityEventBufferLock);
            lockManager.RegisterLockObjectDependency(groupToApplicationComponentAndAccessLevelMappingEventBufferLock, groupEventBufferLock);
            lockManager.RegisterLockObjectDependency(groupToEntityMappingEventBufferLock, groupEventBufferLock);
            lockManager.RegisterLockObjectDependency(groupToEntityMappingEventBufferLock, entityTypeEventBufferLock);
            lockManager.RegisterLockObjectDependency(groupToEntityMappingEventBufferLock, entityEventBufferLock);
        }

        #endregion
    }
}
