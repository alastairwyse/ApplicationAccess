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
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Utilities;
using ApplicationAccess.Validation;
using MoreComplexDataStructures;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Base for classes which buffer events which change the structure of an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> class in memory before persisting the events."/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class AccessManagerTemporalEventPersisterBufferBase<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventBuffer<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        /// <summary>The validator to use to validate events.</summary>
        protected IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator;
        /// <summary>The strategy to use for flushing the buffers.</summary>
        protected IAccessManagerEventBufferFlushStrategy bufferFlushStrategy;
        /// <summary>The hash code generator for users.</summary>
        protected IHashCodeGenerator<TUser> userHashCodeGenerator;
        /// <summary>The hash code generator for groups.</summary>
        protected IHashCodeGenerator<TGroup> groupHashCodeGenerator;
        /// <summary>The hash code generator for entity types.</summary>
        protected IHashCodeGenerator<String> entityTypeHashCodeGenerator;
        /// <summary>The provider to use for random Guids.</summary>
        protected Utilities.IGuidProvider guidProvider;
        /// <summary>The provider to use for the current date and time.</summary>
        protected Utilities.IDateTimeProvider dateTimeProvider;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>The delegate which handles a <see cref="IAccessManagerEventBufferFlushStrategy.BufferFlushed">BufferFlushed</see> event.</summary>
        protected EventHandler bufferFlushedEventHandler;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;
        /// <summary>The sequence number used for the last event buffered.</summary>
        protected Int64 lastEventSequenceNumber;

        // The Int64 value in the below queue tuple item is the ordinal sequence number of the queued event, unique across all queues/buffers
        /// <summary>The queue used to buffer user events.</summary>
        protected LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>> userEventBuffer;
        /// <summary>The queue used to buffer group events.</summary>
        protected LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>> groupEventBuffer;
        /// <summary>The queue used to buffer user to group mapping events.</summary>
        protected LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>> userToGroupMappingEventBuffer;
        /// <summary>The queue used to buffer group to group mapping events.</summary>
        protected LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>> groupToGroupMappingEventBuffer;
        /// <summary>The queue used to buffer user to application component and access level mapping events.</summary>
        protected LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>> userToApplicationComponentAndAccessLevelMappingEventBuffer;
        /// <summary>The queue used to buffer group to application component and access level mapping events.</summary>
        protected LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>> groupToApplicationComponentAndAccessLevelMappingEventBuffer;
        /// <summary>The queue used to buffer entity type events.</summary>
        protected LinkedList<Tuple<EntityTypeEventBufferItem, Int64>> entityTypeEventBuffer;
        /// <summary>The queue used to buffer entity events.</summary>
        protected LinkedList<Tuple<EntityEventBufferItem, Int64>> entityEventBuffer;
        /// <summary>The queue used to buffer user to entity mapping events.</summary>
        protected LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>> userToEntityMappingEventBuffer;
        /// <summary>The queue used to buffer group to entity mapping events.</summary>
        protected LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>> groupToEntityMappingEventBuffer;

        /// <summary>Manages acquiring locks on queues.</summary>
        protected LockManager lockManager;
        // Separate lock objects are required.  The queues cannot be locked directly as they are reassigned whilst locked as part of the flush process
        /// <summary>Lock object for the user event queue.</summary>
        protected Object userEventBufferLock;
        /// <summary>Lock object for the group event queue.</summary>
        protected Object groupEventBufferLock;
        /// <summary>Lock object for the user to group mapping event queue.</summary>
        protected Object userToGroupMappingEventBufferLock;
        /// <summary>Lock object for the group to group mapping event queue.</summary>
        protected Object groupToGroupMappingEventBufferLock;
        /// <summary>Lock object for the user to application component and access level mapping event queue.</summary>
        protected Object userToApplicationComponentAndAccessLevelMappingEventBufferLock;
        /// <summary>Lock object for the group to application component and access level mapping event queue.</summary>
        protected Object groupToApplicationComponentAndAccessLevelMappingEventBufferLock;
        /// <summary>Lock object for the entity type event queue.</summary>
        protected Object entityTypeEventBufferLock;
        /// <summary>Lock object for the entity event queue.</summary>
        protected Object entityEventBufferLock;
        /// <summary>Lock object for the user to entity mapping event queue.</summary>
        protected Object userToEntityMappingEventBufferLock;
        /// <summary>Lock object for the group to entity mapping event queue.</summary>
        protected Object groupToEntityMappingEventBufferLock;
        /// <summary>Lock object for the 'lastEventSequenceNumber' and 'dateTimeProvider' members, to ensure that their sequence orders are maintained between queuing of different events.</summary>
        protected Object eventSequenceNumberLock;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBufferBase class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        public AccessManagerTemporalEventPersisterBufferBase
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator
        )
        {
            this.eventValidator = eventValidator;
            this.bufferFlushStrategy = bufferFlushStrategy;
            // Subscribe to the bufferFlushStrategy's 'BufferFlushed' event
            bufferFlushedEventHandler = (Object sender, EventArgs e) => { Flush(); };
            bufferFlushStrategy.BufferFlushed += bufferFlushedEventHandler;
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.entityTypeHashCodeGenerator = entityTypeHashCodeGenerator;
            guidProvider = new Utilities.DefaultGuidProvider();
            dateTimeProvider = new Utilities.StopwatchDateTimeProvider();
            metricLogger = new NullMetricLogger();
            disposed = false;
            lastEventSequenceNumber = -1;

            userEventBuffer = new LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>>();
            groupEventBuffer = new LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>>();
            userToGroupMappingEventBuffer = new LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>>();
            groupToGroupMappingEventBuffer = new LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>>();
            userToApplicationComponentAndAccessLevelMappingEventBuffer = new LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>>();
            groupToApplicationComponentAndAccessLevelMappingEventBuffer = new LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>>();
            entityTypeEventBuffer = new LinkedList<Tuple<EntityTypeEventBufferItem, Int64>>();
            entityEventBuffer = new LinkedList<Tuple<EntityEventBufferItem, Int64>>();
            userToEntityMappingEventBuffer = new LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>>();
            groupToEntityMappingEventBuffer = new LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>>();

            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBufferBase class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventPersisterBufferBase
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator, 
            IMetricLogger metricLogger
        ) : this(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBufferBase class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventPersisterBufferBase
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IMetricLogger metricLogger, 
            Utilities.IGuidProvider guidProvider,
            Utilities.IDateTimeProvider dateTimeProvider
        ) : this(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, metricLogger)
        {
            this.guidProvider = guidProvider;
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void AddUser(TUser user)
        {
            lockManager.AcquireLocksAndInvokeAction(userEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUser(user, BufferAddUserEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUser(TUser user)
        {
            lockManager.AcquireLocksAndInvokeAction(userEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUser(user, BufferRemoveUserEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void AddGroup(TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(groupEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroup(group, BufferAddGroupEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroup(TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(groupEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroup(group, BufferRemoveGroupEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToGroupMapping(user, group, BufferAddUserToGroupMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToGroupMapping(user, group, BufferRemoveUserToGroupMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToGroupMapping(fromGroup, toGroup, BufferAddGroupToGroupMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToGroupMapping(fromGroup, toGroup, BufferRemoveGroupToGroupMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(userToApplicationComponentAndAccessLevelMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, BufferAddUserToApplicationComponentAndAccessLevelMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(userToApplicationComponentAndAccessLevelMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, BufferRemoveUserToApplicationComponentAndAccessLevelMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToApplicationComponentAndAccessLevelMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, BufferAddGroupToApplicationComponentAndAccessLevelMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToApplicationComponentAndAccessLevelMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, BufferRemoveGroupToApplicationComponentAndAccessLevelMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void AddEntityType(string entityType)
        {
            lockManager.AcquireLocksAndInvokeAction(entityTypeEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddEntityType(entityType, BufferAddEntityTypeEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void RemoveEntityType(string entityType)
        {
            lockManager.AcquireLocksAndInvokeAction(entityTypeEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveEntityType(entityType, BufferRemoveEntityTypeEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void AddEntity(string entityType, string entity)
        {
            lockManager.AcquireLocksAndInvokeAction(entityEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddEntity(entityType, entity, BufferAddEntityEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void RemoveEntity(string entityType, string entity)
        {
            lockManager.AcquireLocksAndInvokeAction(entityEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveEntity(entityType, entity, BufferRemoveEntityEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddUserToEntityMapping(TUser user, string entityType, string entity)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToEntityMapping(user, entityType, entity, BufferAddUserToEntityMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUserToEntityMapping(TUser user, string entityType, string entity)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToEntityMapping(user, entityType, entity, BufferRemoveUserToEntityMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToEntityMapping(group, entityType, entity, BufferAddGroupToEntityMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMappingEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToEntityMapping(group, entityType, entity, BufferRemoveGroupToEntityMappingEventAction));
            }));
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void Flush()
        {
            // Move all events to temporary queues
            LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>> tempUserEventBuffer = null;
            LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>> tempGroupEventBuffer = null;
            LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>> tempUserToGroupMappingEventBuffer = null;
            LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>> tempGroupToGroupMappingEventBuffer = null;
            LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>> tempUserToApplicationComponentAndAccessLevelMappingEventBuffer = null;
            LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>> tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer = null;
            LinkedList<Tuple<EntityTypeEventBufferItem, Int64>> tempEntityTypeEventBuffer = null;
            LinkedList<Tuple<EntityEventBufferItem, Int64>> tempEntityEventBuffer = null;
            LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>> tempUserToEntityMappingEventBuffer = null;
            LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>> tempGroupToEntityMappingEventBuffer = null;
            MoveEventsToTemporaryQueues
            (
                out tempUserEventBuffer,
                out tempGroupEventBuffer,
                out tempUserToGroupMappingEventBuffer,
                out tempGroupToGroupMappingEventBuffer,
                out tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
                out tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
                out tempEntityTypeEventBuffer,
                out tempEntityEventBuffer,
                out tempUserToEntityMappingEventBuffer,
                out tempGroupToEntityMappingEventBuffer
            );

            // Do a k-way merge to send everything to the event persister in sequence number order

            // Initialize the heap
            var nextSequenceNumbers = new MinHeap<SequenceNumberAndEventBuffer>();
            InitializeHeap
            (
                nextSequenceNumbers,
                tempUserEventBuffer,
                tempGroupEventBuffer,
                tempUserToGroupMappingEventBuffer,
                tempGroupToGroupMappingEventBuffer,
                tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
                tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
                tempEntityTypeEventBuffer,
                tempEntityEventBuffer,
                tempUserToEntityMappingEventBuffer,
                tempGroupToEntityMappingEventBuffer
            );

            // Do the k-way merge
            while (nextSequenceNumbers.Count > 0)
            {
                SequenceNumberAndEventBuffer nextSequenceNumber = nextSequenceNumbers.ExtractMin();
                switch (nextSequenceNumber.EventBuffer)
                {
                    case EventBuffer.User:
                        ProcessKWayMergeStep<LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>>, UserEventBufferItem<TUser>>
                        (
                            tempUserEventBuffer,
                            () => { ProcessUserEventBufferItem(tempUserEventBuffer.First.Value.Item1); },
                            EventBuffer.User,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.Group:
                        ProcessKWayMergeStep<LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>>, GroupEventBufferItem<TGroup>>
                        (
                            tempGroupEventBuffer,
                            () => { ProcessGroupEventBufferItem(tempGroupEventBuffer.First.Value.Item1); },
                            EventBuffer.Group,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.UserToGroupMapping:
                        ProcessKWayMergeStep<LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>>, UserToGroupMappingEventBufferItem<TUser, TGroup>>
                        (
                            tempUserToGroupMappingEventBuffer,
                            () => { ProcessUserToGroupMappingEventBufferItem(tempUserToGroupMappingEventBuffer.First.Value.Item1); },
                            EventBuffer.UserToGroupMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.GroupToGroupMapping:
                        ProcessKWayMergeStep<LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>>, GroupToGroupMappingEventBufferItem<TGroup>>
                        (
                            tempGroupToGroupMappingEventBuffer,
                            () => { ProcessGroupToGroupMappingEventBufferItem(tempGroupToGroupMappingEventBuffer.First.Value.Item1); },
                            EventBuffer.GroupToGroupMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.UserToApplicationComponentAndAccessLevelMapping:
                        ProcessKWayMergeStep<LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>>, UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>
                        (
                            tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
                            () => { ProcessUserToApplicationComponentAndAccessLevelMappingEventBufferItem(tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1); },
                            EventBuffer.UserToApplicationComponentAndAccessLevelMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.GroupToApplicationComponentAndAccessLevelMapping:
                        ProcessKWayMergeStep<LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>>, GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>
                        (
                            tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
                            () => { ProcessGroupToApplicationComponentAndAccessLevelMappingEventBufferItem(tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1); },
                            EventBuffer.GroupToApplicationComponentAndAccessLevelMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.EntityType:
                        ProcessKWayMergeStep<LinkedList<Tuple<EntityTypeEventBufferItem, Int64>>, EntityTypeEventBufferItem>
                        (
                            tempEntityTypeEventBuffer,
                            () => { ProcessEntityTypeEventBufferItem(tempEntityTypeEventBuffer.First.Value.Item1); },
                            EventBuffer.EntityType,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.Entity:
                        ProcessKWayMergeStep<LinkedList<Tuple<EntityEventBufferItem, Int64>>, EntityEventBufferItem>
                        (
                            tempEntityEventBuffer,
                            () => { ProcessEntityEventBufferItem(tempEntityEventBuffer.First.Value.Item1); },
                            EventBuffer.Entity,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.UserToEntityMapping:
                        ProcessKWayMergeStep<LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>>, UserToEntityMappingEventBufferItem<TUser>>
                        (
                            tempUserToEntityMappingEventBuffer,
                            () => { ProcessUserToEntityMappingEventBufferItem(tempUserToEntityMappingEventBuffer.First.Value.Item1); },
                            EventBuffer.UserToEntityMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.GroupToEntityMapping:
                        ProcessKWayMergeStep<LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>>, GroupToEntityMappingEventBufferItem<TGroup>>
                        (
                            tempGroupToEntityMappingEventBuffer,
                            () => { ProcessGroupToEntityMappingEventBufferItem(tempGroupToEntityMappingEventBuffer.First.Value.Item1); },
                            EventBuffer.GroupToEntityMapping,
                            nextSequenceNumbers
                        );
                        break;

                    default:
                        throw new NotImplementedException($"No K-merge step implementation exists for {nameof(EventBuffer)} '{nameof(nextSequenceNumber.EventBuffer)}'.");
                }
            }
        }

        #region Private/Protected Methods

        #region Abstract Methods

        /// <summary>
        /// Processes a <see cref="UserEventBufferItem{TUser}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessUserEventBufferItem(UserEventBufferItem<TUser> eventBufferItem);

        /// <summary>
        /// Processes a <see cref="GroupEventBufferItem{TGroup}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessGroupEventBufferItem(GroupEventBufferItem<TGroup> eventBufferItem);

        /// <summary>
        /// Processes a <see cref="UserToGroupMappingEventBufferItem{TUser, TGroup}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessUserToGroupMappingEventBufferItem(UserToGroupMappingEventBufferItem<TUser, TGroup> eventBufferItem);

        /// <summary>
        /// Processes a <see cref="GroupToGroupMappingEventBufferItem{TGroup}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessGroupToGroupMappingEventBufferItem(GroupToGroupMappingEventBufferItem<TGroup> eventBufferItem);

        /// <summary>
        /// Processes a <see cref="UserToApplicationComponentAndAccessLevelMappingEventBufferItem{TUser, TComponent, TAccess}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessUserToApplicationComponentAndAccessLevelMappingEventBufferItem(UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> eventBufferItem);

        /// <summary>
        /// Processes a <see cref="GroupToApplicationComponentAndAccessLevelMappingEventBufferItem{TGroup, TComponent, TAccess}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessGroupToApplicationComponentAndAccessLevelMappingEventBufferItem(GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> eventBufferItem);

        /// <summary>
        /// Processes an <see cref="EntityTypeEventBufferItem"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessEntityTypeEventBufferItem(EntityTypeEventBufferItem eventBufferItem);

        /// <summary>
        /// Processes an <see cref="EntityEventBufferItem"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessEntityEventBufferItem(EntityEventBufferItem eventBufferItem);

        /// <summary>
        /// Processes a <see cref="UserToEntityMappingEventBufferItem{TUser}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessUserToEntityMappingEventBufferItem(UserToEntityMappingEventBufferItem<TUser> eventBufferItem);

        /// <summary>
        /// Processes a <see cref="GroupToEntityMappingEventBufferItem{TGroup}"/> stored in the buffer.
        /// </summary>
        /// <param name="eventBufferItem">The event to process.</param>
        protected abstract void ProcessGroupToEntityMappingEventBufferItem(GroupToEntityMappingEventBufferItem<TGroup> eventBufferItem);

        #endregion

        /// <summary>
        /// Initializes the classes' lock objects and dependencies.
        /// </summary>
        protected virtual void InitializeLockObjects()
        {
            userEventBufferLock = new Object();
            groupEventBufferLock = new Object();
            userToGroupMappingEventBufferLock = new Object();
            groupToGroupMappingEventBufferLock = new Object();
            userToApplicationComponentAndAccessLevelMappingEventBufferLock = new Object();
            groupToApplicationComponentAndAccessLevelMappingEventBufferLock = new Object();
            entityTypeEventBufferLock = new Object();
            entityEventBufferLock = new Object();
            userToEntityMappingEventBufferLock = new Object();
            groupToEntityMappingEventBufferLock = new Object();
            eventSequenceNumberLock = new Object();
            lockManager.RegisterLockObject(userEventBufferLock);
            lockManager.RegisterLockObject(groupEventBufferLock);
            lockManager.RegisterLockObject(userToGroupMappingEventBufferLock);
            lockManager.RegisterLockObject(groupToGroupMappingEventBufferLock);
            lockManager.RegisterLockObject(userToApplicationComponentAndAccessLevelMappingEventBufferLock);
            lockManager.RegisterLockObject(groupToApplicationComponentAndAccessLevelMappingEventBufferLock);
            lockManager.RegisterLockObject(entityTypeEventBufferLock);
            lockManager.RegisterLockObject(entityEventBufferLock);
            lockManager.RegisterLockObject(userToEntityMappingEventBufferLock);
            lockManager.RegisterLockObject(groupToEntityMappingEventBufferLock);
        }

        /// <summary>
        /// Re-throws the exception which caused validation failure, if the exception exists.
        /// </summary>
        /// <param name="validationResult">The validation result to check.</param>
        protected void ThrowExceptionIfValidationFails(ValidationResult validationResult)
        {
            if (validationResult.Successful == false && validationResult.ValidationExceptionDispatchInfo != null)
            {
                validationResult.ValidationExceptionDispatchInfo.Throw();
            }
        }

        /// <summary>
        /// Returns the next event sequence number and the current timestamp.
        /// </summary>
        /// <returns>A tuple containing: the next event sequence number, and the current timestamp.</returns>
        protected Tuple<Int64, DateTime> GetNextEventSequenceNumberAndTimestamp()
        {
            lock (eventSequenceNumberLock)
            {
                // ++ returns the incremented value
                return new Tuple<Int64, DateTime>(++lastEventSequenceNumber, dateTimeProvider.UtcNow());
            }
        }

        /// <summary>
        /// Initializes the specified heap with the first elements of each of the specified buffers, in order to perform a k-merge using the heap.
        /// </summary>
        protected void InitializeHeap
        (
            MinHeap<SequenceNumberAndEventBuffer> heap,
            LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>> tempUserEventBuffer,
            LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>> tempGroupEventBuffer,
            LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>> tempUserToGroupMappingEventBuffer,
            LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>> tempGroupToGroupMappingEventBuffer,
            LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>> tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
            LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>> tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
            LinkedList<Tuple<EntityTypeEventBufferItem, Int64>> tempEntityTypeEventBuffer,
            LinkedList<Tuple<EntityEventBufferItem, Int64>> tempEntityEventBuffer,
            LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>> tempUserToEntityMappingEventBuffer,
            LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>> tempGroupToEntityMappingEventBuffer
        )
        {
            AddBufferedEventToHeap<LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>>, UserEventBufferItem<TUser>>(tempUserEventBuffer, heap, EventBuffer.User);
            AddBufferedEventToHeap<LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>>, GroupEventBufferItem<TGroup>>(tempGroupEventBuffer, heap, EventBuffer.Group);
            AddBufferedEventToHeap<LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>>, UserToGroupMappingEventBufferItem<TUser, TGroup>>(tempUserToGroupMappingEventBuffer, heap, EventBuffer.UserToGroupMapping);
            AddBufferedEventToHeap<LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>>, GroupToGroupMappingEventBufferItem<TGroup>>(tempGroupToGroupMappingEventBuffer, heap, EventBuffer.GroupToGroupMapping);
            AddBufferedEventToHeap<LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>>, UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>(tempUserToApplicationComponentAndAccessLevelMappingEventBuffer, heap, EventBuffer.UserToApplicationComponentAndAccessLevelMapping);
            AddBufferedEventToHeap<LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>>, GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>(tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer, heap, EventBuffer.GroupToApplicationComponentAndAccessLevelMapping);
            AddBufferedEventToHeap<LinkedList<Tuple<EntityTypeEventBufferItem, Int64>>, EntityTypeEventBufferItem>(tempEntityTypeEventBuffer, heap, EventBuffer.EntityType);
            AddBufferedEventToHeap<LinkedList<Tuple<EntityEventBufferItem, Int64>>, EntityEventBufferItem>(tempEntityEventBuffer, heap, EventBuffer.Entity);
            AddBufferedEventToHeap<LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>>, UserToEntityMappingEventBufferItem<TUser>>(tempUserToEntityMappingEventBuffer, heap, EventBuffer.UserToEntityMapping);
            AddBufferedEventToHeap<LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>>, GroupToEntityMappingEventBufferItem<TGroup>>(tempGroupToEntityMappingEventBuffer, heap, EventBuffer.GroupToEntityMapping);
        }

        /// <summary>
        /// Moves all events from the buffer queues to temporary queues.
        /// </summary>
        protected void MoveEventsToTemporaryQueues
        (
            out LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>> tempUserEventBuffer,
            out LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>> tempGroupEventBuffer,
            out LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>> tempUserToGroupMappingEventBuffer,
            out LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>> tempGroupToGroupMappingEventBuffer,
            out LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>> tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
            out LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>> tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
            out LinkedList<Tuple<EntityTypeEventBufferItem, Int64>> tempEntityTypeEventBuffer,
            out LinkedList<Tuple<EntityEventBufferItem, Int64>> tempEntityEventBuffer,
            out LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>> tempUserToEntityMappingEventBuffer,
            out LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>> tempGroupToEntityMappingEventBuffer
        )
        {
            Int64 maxSequenceNumber;
            lock (eventSequenceNumberLock)
            {
                maxSequenceNumber = lastEventSequenceNumber;
            }
            MoveEventsToTemporaryQueue<LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>>, UserEventBufferItem<TUser>>
            (
                ref userEventBuffer,
                out tempUserEventBuffer,
                userEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.UserEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>>, GroupEventBufferItem<TGroup>>
            (
                ref groupEventBuffer,
                out tempGroupEventBuffer,
                groupEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.GroupEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>>, UserToGroupMappingEventBufferItem<TUser, TGroup>>
            (
                ref userToGroupMappingEventBuffer,
                out tempUserToGroupMappingEventBuffer,
                userToGroupMappingEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.UserToGroupMappingEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>>, GroupToGroupMappingEventBufferItem<TGroup>>
            (
                ref groupToGroupMappingEventBuffer,
                out tempGroupToGroupMappingEventBuffer,
                groupToGroupMappingEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>>, UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>
            (
                ref userToApplicationComponentAndAccessLevelMappingEventBuffer,
                out tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
                userToApplicationComponentAndAccessLevelMappingEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>>, GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>
            (
                ref groupToApplicationComponentAndAccessLevelMappingEventBuffer,
                out tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
                groupToApplicationComponentAndAccessLevelMappingEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<EntityTypeEventBufferItem, Int64>>, EntityTypeEventBufferItem>
            (
                ref entityTypeEventBuffer,
                out tempEntityTypeEventBuffer,
                entityTypeEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.EntityTypeEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<EntityEventBufferItem, Int64>>, EntityEventBufferItem>
            (
                ref entityEventBuffer,
                out tempEntityEventBuffer,
                entityEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.EntityEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>>, UserToEntityMappingEventBufferItem<TUser>>
            (
                ref userToEntityMappingEventBuffer,
                out tempUserToEntityMappingEventBuffer,
                userToEntityMappingEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.UserToEntityMappingEventBufferItemCount = eventBufferItemCount; }
            );
            MoveEventsToTemporaryQueue<LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>>, GroupToEntityMappingEventBufferItem<TGroup>>
            (
                ref groupToEntityMappingEventBuffer,
                out tempGroupToEntityMappingEventBuffer,
                groupToEntityMappingEventBufferLock,
                maxSequenceNumber,
                (Int32 eventBufferItemCount) => { bufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = eventBufferItemCount; }
            );
        }

        /// <summary>
        /// Moves all events with sequence number below or equal to that specified, from an event buffer to a temporary event buffer.
        /// </summary>
        /// <typeparam name="TEventBuffer">The type of the event buffer.</typeparam>
        /// <typeparam name="TEventBufferItemType">The type of items in the event buffer.</typeparam>
        /// <param name="eventBuffer">The event buffer to move events from.</param>
        /// <param name="temporaryEventBuffer">The temporary event buffer to move events to.</param>
        /// <param name="eventBufferLockObject">Lock object used to serialize access to the event buffer parameter.</param>
        /// <param name="maxSequenceNumber">The maximum (inclusive) sequence number of events to move.  Only events with a sequence number below or equal to this maximum will be moved.</param>
        /// <param name="bufferFlushStrategyEventCountSetAction">An action which sets the relevant 'EventBufferItemCount' property on the 'bufferFlushStrategy' member.</param>
        protected virtual void MoveEventsToTemporaryQueue<TEventBuffer, TEventBufferItemType>
        (
            ref TEventBuffer eventBuffer,
            out TEventBuffer temporaryEventBuffer,
            Object eventBufferLockObject,
            Int64 maxSequenceNumber,
            Action<Int32> bufferFlushStrategyEventCountSetAction
        )
            where TEventBuffer : LinkedList<Tuple<TEventBufferItemType, Int64>>, new()
            where TEventBufferItemType : TemporalEventBufferItemBase
        {
            // TODO: For consistency and correctness we should be using the 'lockManager' member to do this locking, but run into issues since we have to pass a lambda to its AcquireLocks*() methods
            //   and lambdas don't work with ref and out parameters.  Since this locking will only ever need to lock the queue/buffer whose events are being moved (we don't care about dependencies
            //   on elements in other queues at this point), we can get away with using the 'lock' statement directly.
            lock (eventBufferLockObject)
            {
                // If the sequence number of the first item in the event buffer is greater than parameter 'maxSequenceNumber', it means that all events were buffered after the current flush process started, and hence none of them need to be processed
                if (eventBuffer.Count > 0 && eventBuffer.First.Value.Item2 > maxSequenceNumber)
                {
                    temporaryEventBuffer = new TEventBuffer();
                }
                else
                {
                    Int32 movedBackEventCount = 0;
                    temporaryEventBuffer = eventBuffer;
                    eventBuffer = new TEventBuffer();
                    // Move any events queued since 'maxSequenceNumber' was captured back to the front of the main queue
                    //   This will prevent any events from being processed before an event they depend on (e.g. if a 'user add' event and an 'user to entity mapping add' event both occur after the flush has started, and after the 'userEventBuffer' has been processed, but before the 'userToEntityMappingEventBuffer' has been processed)
                    while (temporaryEventBuffer.Count > 0 && temporaryEventBuffer.Last.Value.Item2 > maxSequenceNumber)
                    {
                        eventBuffer.AddFirst(temporaryEventBuffer.Last.Value);
                        temporaryEventBuffer.RemoveLast();
                        movedBackEventCount++;
                    }
                    metricLogger.Add(new EventsExcludedFromFlush(), movedBackEventCount);
                }
                bufferFlushStrategyEventCountSetAction.Invoke(eventBuffer.Count);
            }
        }

        /// <summary>
        /// Processes a step of the k-way merge process used to order and process all events to flush, for a single event buffer.
        /// </summary>
        /// <typeparam name="TEventBuffer">The type of the event buffer to process.</typeparam>
        /// <typeparam name="TEventBufferItemType">The type of items in the event buffer.</typeparam>
        /// <param name="temporaryEventBuffer">The event buffer to process.</param>
        /// <param name="processAction">An action which processes the next event identified by the k-way merge.</param>
        /// <param name="eventBufferEnum">The enum representing the temporary event buffer.</param>
        /// <param name="nextSequenceNumbers">A min-heap containing the sequence numbers of events to process and the event buffer they exist in.</param>
        protected void ProcessKWayMergeStep<TEventBuffer, TEventBufferItemType>
        (
            TEventBuffer temporaryEventBuffer,
            Action processAction,
            EventBuffer eventBufferEnum,
            MinHeap<SequenceNumberAndEventBuffer> nextSequenceNumbers
        )
            where TEventBuffer : LinkedList<Tuple<TEventBufferItemType, Int64>>
            where TEventBufferItemType : TemporalEventBufferItemBase
        {
            processAction.Invoke();
            temporaryEventBuffer.RemoveFirst();
            AddBufferedEventToHeap<TEventBuffer, TEventBufferItemType>(temporaryEventBuffer, nextSequenceNumbers, eventBufferEnum);
        }

        /// <summary>
        /// Takes an event from the specified event buffer (if one exists) and adds it to the specified min-heap of sequence number and event buffer references.
        /// </summary>
        /// <typeparam name="TEventBuffer">The type of the event buffer to take the event from.</typeparam>
        /// <typeparam name="TEventBufferItemType">The type of items in the event buffer.</typeparam>
        /// <param name="eventBuffer">The event buffer to take the event from.</param>
        /// <param name="nextSequenceNumbers">The min-heap to add the event to.</param>
        /// <param name="eventBufferEnum">The enum representing the event buffer.</param>
        protected void AddBufferedEventToHeap<TEventBuffer, TEventBufferItemType>(TEventBuffer eventBuffer, MinHeap<SequenceNumberAndEventBuffer> nextSequenceNumbers, EventBuffer eventBufferEnum)
            where TEventBuffer : LinkedList<Tuple<TEventBufferItemType, Int64>>
            where TEventBufferItemType : TemporalEventBufferItemBase
        {
            if (eventBuffer.Count > 0)
            {
                nextSequenceNumbers.Insert(new SequenceNumberAndEventBuffer(eventBuffer.First.Value.Item2, eventBufferEnum));
            }
        }

        #region BufferEventActions

        /// <summary>Action which buffers an event to add the specified user.</summary>
        protected Action<TUser> BufferAddUserEventAction
        {
            get
            {
                return (TUser user) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var userEvent = new UserEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Add, user, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserEventBufferItem<TUser>, Int64>(userEvent, nextSequenceNumberAndTimestamp.Item1);
                    userEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserEventBufferItemCount = userEventBuffer.Count;
                    metricLogger.Increment(new AddUserEventBuffered());
                    metricLogger.Set(new UserEventsBuffered(), userEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified user.</summary>
        protected Action<TUser> BufferRemoveUserEventAction
        {
            get
            {
                return (TUser user) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var userEvent = new UserEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Remove, user, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserEventBufferItem<TUser>, Int64>(userEvent, nextSequenceNumberAndTimestamp.Item1);
                    userEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserEventBufferItemCount = userEventBuffer.Count;
                    metricLogger.Increment(new RemoveUserEventBuffered());
                    metricLogger.Set(new UserEventsBuffered(), userEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified group.</summary>
        protected Action<TGroup> BufferAddGroupEventAction
        {
            get
            {
                return (TGroup group) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var groupEvent = new GroupEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Add, group, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(group));
                    var bufferItem = new Tuple<GroupEventBufferItem<TGroup>, Int64>(groupEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupEventBufferItemCount = groupEventBuffer.Count;
                    metricLogger.Increment(new AddGroupEventBuffered());
                    metricLogger.Set(new GroupEventsBuffered(), groupEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified group.</summary>
        protected Action<TGroup> BufferRemoveGroupEventAction
        {
            get
            {
                return (TGroup group) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var groupEvent = new GroupEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Remove, group, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(group));
                    var bufferItem = new Tuple<GroupEventBufferItem<TGroup>, Int64>(groupEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupEventBufferItemCount = groupEventBuffer.Count;
                    metricLogger.Increment(new RemoveGroupEventBuffered());
                    metricLogger.Set(new GroupEventsBuffered(), groupEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified user to group mapping.</summary>
        protected Action<TUser, TGroup> BufferAddUserToGroupMappingEventAction
        {
            get
            {
                return (TUser user, TGroup group) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(guidProvider.NewGuid(), EventAction.Add, user, group, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    userToGroupMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserToGroupMappingEventBufferItemCount = userToGroupMappingEventBuffer.Count;
                    metricLogger.Increment(new AddUserToGroupMappingEventBuffered());
                    metricLogger.Set(new UserToGroupMappingEventsBuffered(), userToGroupMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified user to group mapping.</summary>
        protected Action<TUser, TGroup> BufferRemoveUserToGroupMappingEventAction
        {
            get
            {
                return (TUser user, TGroup group) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(guidProvider.NewGuid(), EventAction.Remove, user, group, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    userToGroupMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserToGroupMappingEventBufferItemCount = userToGroupMappingEventBuffer.Count;
                    metricLogger.Increment(new RemoveUserToGroupMappingEventBuffered());
                    metricLogger.Set(new UserToGroupMappingEventsBuffered(), userToGroupMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified group to group mapping.</summary>
        protected Action<TGroup, TGroup> BufferAddGroupToGroupMappingEventAction
        {
            get
            {
                return (TGroup fromGroup, TGroup toGroup) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new GroupToGroupMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Add, fromGroup, toGroup, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(fromGroup));
                    var bufferItem = new Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupToGroupMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = groupToGroupMappingEventBuffer.Count;
                    metricLogger.Increment(new AddGroupToGroupMappingEventBuffered());
                    metricLogger.Set(new GroupToGroupMappingEventsBuffered(), groupToGroupMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified group to group mapping.</summary>
        protected Action<TGroup, TGroup> BufferRemoveGroupToGroupMappingEventAction
        {
            get
            {
                return (TGroup fromGroup, TGroup toGroup) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new GroupToGroupMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Remove, fromGroup, toGroup, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(fromGroup));
                    var bufferItem = new Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupToGroupMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = groupToGroupMappingEventBuffer.Count;
                    metricLogger.Increment(new RemoveGroupToGroupMappingEventBuffered());
                    metricLogger.Set(new GroupToGroupMappingEventsBuffered(), groupToGroupMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified user to application component and access level mapping.</summary>
        protected Action<TUser, TComponent, TAccess> BufferAddUserToApplicationComponentAndAccessLevelMappingEventAction
        {
            get
            {
                return (TUser user, TComponent applicationComponent, TAccess accessLevel) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Add, user, applicationComponent, accessLevel, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    userToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = userToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                    metricLogger.Increment(new AddUserToApplicationComponentAndAccessLevelMappingEventBuffered());
                    metricLogger.Set(new UserToApplicationComponentAndAccessLevelMappingEventsBuffered(), userToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified user to application component and access level mapping.</summary>
        protected Action<TUser, TComponent, TAccess> BufferRemoveUserToApplicationComponentAndAccessLevelMappingEventAction
        {
            get
            {
                return (TUser user, TComponent applicationComponent, TAccess accessLevel) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Remove, user, applicationComponent, accessLevel, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    userToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = userToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                    metricLogger.Increment(new RemoveUserToApplicationComponentAndAccessLevelMappingEventBuffered());
                    metricLogger.Set(new UserToApplicationComponentAndAccessLevelMappingEventsBuffered(), userToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified group to application component and access level mapping.</summary>
        protected Action<TGroup, TComponent, TAccess> BufferAddGroupToApplicationComponentAndAccessLevelMappingEventAction
        {
            get
            {
                return (TGroup group, TComponent applicationComponent, TAccess accessLevel) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Add, group, applicationComponent, accessLevel, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(group));
                    var bufferItem = new Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                    metricLogger.Increment(new AddGroupToApplicationComponentAndAccessLevelMappingEventBuffered());
                    metricLogger.Set(new GroupToApplicationComponentAndAccessLevelMappingEventsBuffered(), groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified group to application component and access level mapping.</summary>
        protected Action<TGroup, TComponent, TAccess> BufferRemoveGroupToApplicationComponentAndAccessLevelMappingEventAction
        {
            get
            {
                return (TGroup group, TComponent applicationComponent, TAccess accessLevel) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Remove, group, applicationComponent, accessLevel, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(group));
                    var bufferItem = new Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                    metricLogger.Increment(new RemoveGroupToApplicationComponentAndAccessLevelMappingEventBuffered());
                    metricLogger.Set(new GroupToApplicationComponentAndAccessLevelMappingEventsBuffered(), groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified entity type.</summary>
        protected Action<String> BufferAddEntityTypeEventAction
        {
            get
            {
                return (String entityType) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var entityTypeEvent = new EntityTypeEventBufferItem(guidProvider.NewGuid(), EventAction.Add, entityType, nextSequenceNumberAndTimestamp.Item2, entityTypeHashCodeGenerator.GetHashCode(entityType));
                    var bufferItem = new Tuple<EntityTypeEventBufferItem, Int64>(entityTypeEvent, nextSequenceNumberAndTimestamp.Item1);
                    entityTypeEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.EntityTypeEventBufferItemCount = entityTypeEventBuffer.Count;
                    metricLogger.Increment(new AddEntityTypeEventBuffered());
                    metricLogger.Set(new EntityTypeEventsBuffered(), entityTypeEventBuffer.Count);
                };
            }
        }


        /// <summary>Action which buffers an event to remove the specified entity type.</summary>
        protected Action<String> BufferRemoveEntityTypeEventAction
        {
            get
            {
                return (String entityType) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var entityTypeEvent = new EntityTypeEventBufferItem(guidProvider.NewGuid(), EventAction.Remove, entityType, nextSequenceNumberAndTimestamp.Item2, entityTypeHashCodeGenerator.GetHashCode(entityType));
                    var bufferItem = new Tuple<EntityTypeEventBufferItem, Int64>(entityTypeEvent, nextSequenceNumberAndTimestamp.Item1);
                    entityTypeEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.EntityTypeEventBufferItemCount = entityTypeEventBuffer.Count;
                    metricLogger.Increment(new RemoveEntityTypeEventBuffered());
                    metricLogger.Set(new EntityTypeEventsBuffered(), entityTypeEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified entity.</summary>
        protected Action<String, String> BufferAddEntityEventAction
        {
            get
            {
                return (String entityType, String entity) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var entityEvent = new EntityEventBufferItem(guidProvider.NewGuid(), EventAction.Add, entityType, entity, nextSequenceNumberAndTimestamp.Item2, entityTypeHashCodeGenerator.GetHashCode(entityType));
                    var bufferItem = new Tuple<EntityEventBufferItem, Int64>(entityEvent, nextSequenceNumberAndTimestamp.Item1);
                    entityEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.EntityEventBufferItemCount = entityEventBuffer.Count;
                    metricLogger.Increment(new AddEntityEventBuffered());
                    metricLogger.Set(new EntityEventsBuffered(), entityEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified entity.</summary>
        protected Action<String, String> BufferRemoveEntityEventAction
        {
            get
            {
                return (String entityType, String entity) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var entityEvent = new EntityEventBufferItem(guidProvider.NewGuid(), EventAction.Remove, entityType, entity, nextSequenceNumberAndTimestamp.Item2, entityTypeHashCodeGenerator.GetHashCode(entityType));
                    var bufferItem = new Tuple<EntityEventBufferItem, Int64>(entityEvent, nextSequenceNumberAndTimestamp.Item1);
                    entityEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.EntityEventBufferItemCount = entityEventBuffer.Count;
                    metricLogger.Increment(new RemoveEntityEventBuffered());
                    metricLogger.Set(new EntityEventsBuffered(), entityEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified user to entity mapping.</summary>
        protected Action<TUser, String, String> BufferAddUserToEntityMappingEventAction
        {
            get
            {
                return (TUser user, String entityType, String entity) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new UserToEntityMappingEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Add, user, entityType, entity, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    userToEntityMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserToEntityMappingEventBufferItemCount = userToEntityMappingEventBuffer.Count;
                    metricLogger.Increment(new AddUserToEntityMappingEventBuffered());
                    metricLogger.Set(new UserToEntityMappingEventsBuffered(), userToEntityMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified user to entity mapping.</summary>
        protected Action<TUser, String, String> BufferRemoveUserToEntityMappingEventAction
        {
            get
            {
                return (TUser user, String entityType, String entity) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new UserToEntityMappingEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Remove, user, entityType, entity, nextSequenceNumberAndTimestamp.Item2, userHashCodeGenerator.GetHashCode(user));
                    var bufferItem = new Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    userToEntityMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.UserToEntityMappingEventBufferItemCount = userToEntityMappingEventBuffer.Count;
                    metricLogger.Increment(new RemoveUserToEntityMappingEventBuffered());
                    metricLogger.Set(new UserToEntityMappingEventsBuffered(), userToEntityMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to add the specified group to entity mapping.</summary>
        protected Action<TGroup, String, String> BufferAddGroupToEntityMappingEventAction
        {
            get
            {
                return (TGroup group, String entityType, String entity) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new GroupToEntityMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Add, group, entityType, entity, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(group));
                    var bufferItem = new Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupToEntityMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = groupToEntityMappingEventBuffer.Count;
                    metricLogger.Increment(new AddGroupToEntityMappingEventBuffered());
                    metricLogger.Set(new GroupToEntityMappingEventsBuffered(), groupToEntityMappingEventBuffer.Count);
                };
            }
        }

        /// <summary>Action which buffers an event to remove the specified group to entity mapping.</summary>
        protected Action<TGroup, String, String> BufferRemoveGroupToEntityMappingEventAction
        {
            get
            {
                return (TGroup group, String entityType, String entity) =>
                {
                    Tuple<Int64, DateTime> nextSequenceNumberAndTimestamp = GetNextEventSequenceNumberAndTimestamp();
                    var mappingEvent = new GroupToEntityMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Remove, group, entityType, entity, nextSequenceNumberAndTimestamp.Item2, groupHashCodeGenerator.GetHashCode(group));
                    var bufferItem = new Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>(mappingEvent, nextSequenceNumberAndTimestamp.Item1);
                    groupToEntityMappingEventBuffer.AddLast(bufferItem);
                    bufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = groupToEntityMappingEventBuffer.Count;
                    metricLogger.Increment(new RemoveGroupToEntityMappingEventBuffered());
                    metricLogger.Set(new GroupToEntityMappingEventsBuffered(), groupToEntityMappingEventBuffer.Count);
                };
            }
        }

        #endregion

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the AccessManagerTemporalEventPersisterBuffer.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~AccessManagerTemporalEventPersisterBufferBase()
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
                    bufferFlushStrategy.BufferFlushed -= bufferFlushedEventHandler;
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion

        #region Nested Classes

        #pragma warning disable 1591

        /// <summary>
        /// Represents one of the event buffer queues.
        /// </summary>
        protected enum EventBuffer
        {
            User,
            Group,
            UserToGroupMapping,
            GroupToGroupMapping,
            UserToApplicationComponentAndAccessLevelMapping,
            GroupToApplicationComponentAndAccessLevelMapping,
            EntityType,
            Entity,
            UserToEntityMapping,
            GroupToEntityMapping
        }

        /// <summary>
        /// Container class which holds a sequence number and an EventBuffer, and is comparable on the sequence number.  Used to order/prioritize items across event buffer queues.
        /// </summary>
        protected class SequenceNumberAndEventBuffer : IComparable<SequenceNumberAndEventBuffer>
        {
            protected Int64 eventSequenceNumber;
            protected EventBuffer eventBuffer;

            /// <summary>
            /// The sequence number of the event.
            /// </summary>
            public Int64 EventSequenceNumber
            {
                get { return eventSequenceNumber; }
            }

            /// <summary>
            /// The buffer that the event is stored in.
            /// </summary>
            public EventBuffer EventBuffer
            {
                get { return eventBuffer; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer+SequenceNumberAndEventBuffer class.
            /// </summary>
            /// <param name="eventSequenceNumber">The sequence number of the event.</param>
            /// <param name="eventBuffer"> The buffer that the event is stored in.</param>
            public SequenceNumberAndEventBuffer(Int64 eventSequenceNumber, EventBuffer eventBuffer)
            {
                this.eventSequenceNumber = eventSequenceNumber;
                this.eventBuffer = eventBuffer;
            }

            public Int32 CompareTo(SequenceNumberAndEventBuffer other)
            {
                return eventSequenceNumber.CompareTo(other.eventSequenceNumber);
            }
        }

        #endregion
    }
}
