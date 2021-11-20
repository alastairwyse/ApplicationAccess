/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Threading;
using ApplicationAccess.Validation;
using MoreComplexDataStructures;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Buffers events which change the structure of an AccessManager class in memory.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class InMemoryEventBuffer<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventBuffer<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        /// <summary>The validator to use to validate events.</summary>
        protected IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator;
        /// <summary>The strategy to use for flushing the buffers.</summary>
        protected IAccessManagerEventBufferFlushStrategy<TUser, TGroup, TComponent, TAccess> bufferFlushStrategy;
        /// <summary>The persister to use to write flushed events to permanent storage.</summary>
        protected IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister;
        /// <summary>The provider to use for the current date and time.</summary>
        protected IDateTimeProvider dateTimeProvider;
        /// <summary>The delegate which handles when a BufferFlushed event is raised.</summary>
        protected EventHandler bufferFlushedEventHandler;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected bool disposed;
        /// <summary>The sequence number used for the last event buffered.</summary>
        protected Int64 lastEventSequenceNumber;

        /// <summary>The queue used to buffer user events.</summary>
        protected LinkedList<UserEventBufferItem<TUser>> userEventBuffer;
        /// <summary>The queue used to buffer group events.</summary>
        protected LinkedList<GroupEventBufferItem<TGroup>> groupEventBuffer;
        /// <summary>The queue used to buffer user to group mapping events.</summary>
        protected LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>> userToGroupMappingEventBuffer;
        /// <summary>The queue used to buffer group to group mapping events.</summary>
        protected LinkedList<GroupToGroupMappingEventBufferItem<TGroup>> groupToGroupMappingEventBuffer;
        /// <summary>The queue used to buffer user to application component and access level mapping events.</summary>
        protected LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>> userToApplicationComponentAndAccessLevelMappingEventBuffer;
        /// <summary>The queue used to buffer group to application component and access level mapping events.</summary>
        protected LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>> groupToApplicationComponentAndAccessLevelMappingEventBuffer;
        /// <summary>The queue used to buffer entity type events.</summary>
        protected LinkedList<EntityTypeEventBufferItem> entityTypeEventBuffer;
        /// <summary>The queue used to buffer entity events.</summary>
        protected LinkedList<EntityEventBufferItem> entityEventBuffer;
        /// <summary>The queue used to buffer user to entity mapping events.</summary>
        protected LinkedList<UserToEntityMappingEventBufferItem<TUser>> userToEntityMappingEventBuffer;
        /// <summary>The queue used to buffer group to entity mapping events.</summary>
        protected LinkedList<GroupToEntityMappingEventBufferItem<TGroup>> groupToEntityMappingEventBuffer;

        // Separate lock objects are required.  The queues cannot be locked directly as they are reassigned whilst locked as part of the flush process
        /// <summary>Lock objects for the user event queue.</summary>
        protected Object userEventBufferLock;
        /// <summary>Lock objects for the group event queue.</summary>
        protected Object groupEventBufferLock;
        /// <summary>Lock objects for the user to group mapping event queue.</summary>
        protected Object userToGroupMappingEventBufferLock;
        /// <summary>Lock objects for the group to group mapping event queue.</summary>
        protected Object groupToGroupMappingEventBufferLock;
        /// <summary>Lock objects for the user to application component and access level mapping event queue.</summary>
        protected Object userToApplicationComponentAndAccessLevelMappingEventBufferLock;
        /// <summary>Lock objects for the group to application component and access level mapping event queue.</summary>
        protected Object groupToApplicationComponentAndAccessLevelMappingEventBufferLock;
        /// <summary>Lock objects for the entity type event queue.</summary>
        protected Object entityTypeEventBufferLock;
        /// <summary>Lock objects for the entity event queue.</summary>
        protected Object entityEventBufferLock;
        /// <summary>Lock objects for the user to entity mapping event queue.</summary>
        protected Object userToEntityMappingEventBufferLock;
        /// <summary>Lock objects for the group to entity mapping event queue.</summary>
        protected Object groupToEntityMappingEventBufferLock;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.InMemoryEventBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        public InMemoryEventBuffer(
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator, 
            IAccessManagerEventBufferFlushStrategy<TUser, TGroup, TComponent, TAccess> bufferFlushStrategy, 
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        )
        {
            this.eventValidator = eventValidator;
            this.bufferFlushStrategy = bufferFlushStrategy;
            // Subscribe to the bufferFlushStrategy's 'BufferFlushed' event
            bufferFlushedEventHandler = (Object sender, EventArgs e) => { Flush(); };
            bufferFlushStrategy.BufferFlushed += bufferFlushedEventHandler;
            this.eventPersister = eventPersister;
            dateTimeProvider = new DefaultDateTimeProvider();
            lastEventSequenceNumber = -1;

            userEventBuffer = new LinkedList<UserEventBufferItem<TUser>>();
            groupEventBuffer = new LinkedList<GroupEventBufferItem<TGroup>>();
            userToGroupMappingEventBuffer = new LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>>();
            groupToGroupMappingEventBuffer = new LinkedList<GroupToGroupMappingEventBufferItem<TGroup>>();
            userToApplicationComponentAndAccessLevelMappingEventBuffer = new LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>();
            groupToApplicationComponentAndAccessLevelMappingEventBuffer = new LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>();
            entityTypeEventBuffer = new LinkedList<EntityTypeEventBufferItem>();
            entityEventBuffer = new LinkedList<EntityEventBufferItem>();
            userToEntityMappingEventBuffer = new LinkedList<UserToEntityMappingEventBufferItem<TUser>>();
            groupToEntityMappingEventBuffer = new LinkedList<GroupToEntityMappingEventBufferItem<TGroup>>();

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
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.InMemoryEventBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="lastEventSequenceNumber">The sequence number used for the last event buffered.</param>
        public InMemoryEventBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy<TUser, TGroup, TComponent, TAccess> bufferFlushStrategy, 
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister, 
            Int64 lastEventSequenceNumber
        ) : this(eventValidator, bufferFlushStrategy, eventPersister)
        {
            if (lastEventSequenceNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(lastEventSequenceNumber), $"Parameter '{nameof(lastEventSequenceNumber)}' must be greater than or equal to 0.");

            this.lastEventSequenceNumber = lastEventSequenceNumber;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.InMemoryEventBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        public InMemoryEventBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy<TUser, TGroup, TComponent, TAccess> bufferFlushStrategy, 
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister, 
            IDateTimeProvider dateTimeProvider
        ) : this(eventValidator, bufferFlushStrategy, eventPersister)
        {
            this.dateTimeProvider = dateTimeProvider;
        }

        #pragma warning disable 1591

        public void AddUser(TUser user)
        {
            Action<TUser> postValidationAction = (actionUser) =>
            {
                var userEvent = new UserEventBufferItem<TUser>(EventAction.Add, user, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userEventBuffer.AddLast(userEvent);
                bufferFlushStrategy.AddUser(user);
            };
            lock (userEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUser(user, postValidationAction));
            }
        }

        public void RemoveUser(TUser user)
        {
            Action<TUser> postValidationAction = (actionUser) =>
            {
                var userEvent = new UserEventBufferItem<TUser>(EventAction.Remove, user, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userEventBuffer.AddLast(userEvent);
                bufferFlushStrategy.RemoveUser(user);
            };
            lock (userEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUser(user, postValidationAction));
            }
        }

        public void AddGroup(TGroup group)
        {
            Action<TGroup> postValidationAction = (actionGroup) =>
            {
                var groupEvent = new GroupEventBufferItem<TGroup>(EventAction.Add, group, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupEventBuffer.AddLast(groupEvent);
                bufferFlushStrategy.AddGroup(group);
            };
            lock (groupEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroup(group, postValidationAction));
            }
        }

        public void RemoveGroup(TGroup group)
        {
            Action<TGroup> postValidationAction = (actionGroup) =>
            {
                var groupEvent = new GroupEventBufferItem<TGroup>(EventAction.Remove, group, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupEventBuffer.AddLast(groupEvent);
                bufferFlushStrategy.RemoveGroup(group);
            };
            lock (groupEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroup(group, postValidationAction));
            }
        }

        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup> postValidationAction = (actionUser, actionGroup) =>
            {
                var mappingEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(EventAction.Add, user, group, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userToGroupMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.AddUserToGroupMapping(user, group);
            };
            lock (userToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToGroupMapping(user, group, postValidationAction));
            }
        }

        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup> postValidationAction = (actionUser, actionGroup) =>
            {
                var mappingEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(EventAction.Remove, user, group, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userToGroupMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.RemoveUserToGroupMapping(user, group);
            };
            lock (userToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToGroupMapping(user, group, postValidationAction));
            }
        }

        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                var mappingEvent = new GroupToGroupMappingEventBufferItem<TGroup>(EventAction.Add, fromGroup, toGroup, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupToGroupMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.AddGroupToGroupMapping(fromGroup, toGroup);
            };
            lock (groupToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToGroupMapping(fromGroup, toGroup, postValidationAction));
            }
        }

        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                var mappingEvent = new GroupToGroupMappingEventBufferItem<TGroup>(EventAction.Remove, fromGroup, toGroup, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupToGroupMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.RemoveGroupToGroupMapping(fromGroup, toGroup);
            };
            lock (groupToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToGroupMapping(fromGroup, toGroup, postValidationAction));
            }
        }

        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(EventAction.Add, user, applicationComponent, accessLevel, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            };
            lock (userToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction));
            }
        }

        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(EventAction.Remove, user, applicationComponent, accessLevel, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            };
            lock (userToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction));
            }
        }

        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(EventAction.Add, group, applicationComponent, accessLevel, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            };
            lock (groupToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction));
            }
        }

        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(EventAction.Remove, group, applicationComponent, accessLevel, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            };
            lock (groupToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction));
            }
        }

        public void AddEntityType(string entityType)
        {
            Action<string> postValidationAction = (actionEntityType) =>
            {
                var entityTypeEvent = new EntityTypeEventBufferItem(EventAction.Add, entityType, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                entityTypeEventBuffer.AddLast(entityTypeEvent);
                bufferFlushStrategy.AddEntityType(entityType);
            };
            lock (entityTypeEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddEntityType(entityType, postValidationAction));
            }
        }

        public void RemoveEntityType(string entityType)
        {
            Action<string> postValidationAction = (actionEntityType) =>
            {
                var entityTypeEvent = new EntityTypeEventBufferItem(EventAction.Remove, entityType, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                entityTypeEventBuffer.AddLast(entityTypeEvent);
                bufferFlushStrategy.RemoveEntityType(entityType);
            };
            lock (entityTypeEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveEntityType(entityType, postValidationAction));
            }
        }
    
        public void AddEntity(string entityType, string entity)
        {
            Action<string, string> postValidationAction = (actionEntityType, actionEntity) =>
            {
                var entityEvent = new EntityEventBufferItem(EventAction.Add, entityType, entity, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                entityEventBuffer.AddLast(entityEvent);
                bufferFlushStrategy.AddEntity(entityType, entity);
            };
            lock (entityEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddEntity(entityType, entity, postValidationAction));
            }
        }

        public void RemoveEntity(string entityType, string entity)
        {
            Action<string, string> postValidationAction = (actionEntityType, actionEntity) =>
            {
                var entityEvent = new EntityEventBufferItem(EventAction.Remove, entityType, entity, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                entityEventBuffer.AddLast(entityEvent);
                bufferFlushStrategy.RemoveEntity(entityType, entity);
            };
            lock (entityEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveEntity(entityType, entity, postValidationAction));
            }
        }

        public void AddUserToEntityMapping(TUser user, string entityType, string entity)
        {
            Action<TUser, string, string> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new UserToEntityMappingEventBufferItem<TUser>(EventAction.Add, user, entityType, entity, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userToEntityMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.AddUserToEntityMapping(user, entityType, entity);
            };
            lock (userToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToEntityMapping(user, entityType, entity, postValidationAction));
            }
        }

        public void RemoveUserToEntityMapping(TUser user, string entityType, string entity)
        {
            Action<TUser, string, string> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new UserToEntityMappingEventBufferItem<TUser>(EventAction.Remove, user, entityType, entity, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                userToEntityMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.RemoveUserToEntityMapping(user, entityType, entity);
            };
            lock (userToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToEntityMapping(user, entityType, entity, postValidationAction));
            }
        }

        public void AddGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            Action<TGroup, string, string> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new GroupToEntityMappingEventBufferItem<TGroup>(EventAction.Add, group, entityType, entity, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupToEntityMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.AddGroupToEntityMapping(group, entityType, entity);
            };
            lock (groupToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToEntityMapping(group, entityType, entity, postValidationAction));
            }
        }

        public void RemoveGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            Action<TGroup, string, string> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new GroupToEntityMappingEventBufferItem<TGroup>(EventAction.Remove, group, entityType, entity, dateTimeProvider.UtcNow(), Interlocked.Increment(ref lastEventSequenceNumber));
                groupToEntityMappingEventBuffer.AddLast(mappingEvent);
                bufferFlushStrategy.RemoveGroupToEntityMapping(group, entityType, entity);
            };
            lock (groupToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToEntityMapping(group, entityType, entity, postValidationAction));
            }
        }

        public void Flush()
        {
            // Move all events to temporary queues
            LinkedList<UserEventBufferItem<TUser>> tempUserEventBuffer = null;
            LinkedList<GroupEventBufferItem<TGroup>> tempGroupEventBuffer = null;
            LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>> tempUserToGroupMappingEventBuffer = null;
            LinkedList<GroupToGroupMappingEventBufferItem<TGroup>> tempGroupToGroupMappingEventBuffer = null;
            LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>> tempUserToApplicationComponentAndAccessLevelMappingEventBuffer = null;
            LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>> tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer = null;
            LinkedList<EntityTypeEventBufferItem> tempEntityTypeEventBuffer = null;
            LinkedList<EntityEventBufferItem> tempEntityEventBuffer = null;
            LinkedList<UserToEntityMappingEventBufferItem<TUser>> tempUserToEntityMappingEventBuffer = null;
            LinkedList<GroupToEntityMappingEventBufferItem<TGroup>> tempGroupToEntityMappingEventBuffer = null;
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
                        ProcessKWayMergeStep<LinkedList<UserEventBufferItem<TUser>>, UserEventBufferItem<TUser>>
                        (
                            tempUserEventBuffer,
                            (eventPersister) => { eventPersister.AddUser(tempUserEventBuffer.First.Value.User, tempUserEventBuffer.First.Value.OccurredTime); },
                            (eventPersister) => { eventPersister.RemoveUser(tempUserEventBuffer.First.Value.User, tempUserEventBuffer.First.Value.OccurredTime); },
                            "user",
                            EventBuffer.User, 
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.Group:
                        ProcessKWayMergeStep<LinkedList<GroupEventBufferItem<TGroup>>, GroupEventBufferItem<TGroup>>
                        (
                            tempGroupEventBuffer,
                            (eventPersister) => { eventPersister.AddGroup(tempGroupEventBuffer.First.Value.Group, tempGroupEventBuffer.First.Value.OccurredTime); },
                            (eventPersister) => { eventPersister.RemoveGroup(tempGroupEventBuffer.First.Value.Group, tempGroupEventBuffer.First.Value.OccurredTime); },
                            "group",
                            EventBuffer.Group,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.UserToGroupMapping:
                        ProcessKWayMergeStep<LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>>, UserToGroupMappingEventBufferItem<TUser, TGroup>>
                        (
                            tempUserToGroupMappingEventBuffer,
                            (eventPersister) => { eventPersister.AddUserToGroupMapping(tempUserToGroupMappingEventBuffer.First.Value.User, tempUserToGroupMappingEventBuffer.First.Value.Group, tempUserToGroupMappingEventBuffer.First.Value.OccurredTime); },
                            (eventPersister) => { eventPersister.RemoveUserToGroupMapping(tempUserToGroupMappingEventBuffer.First.Value.User, tempUserToGroupMappingEventBuffer.First.Value.Group, tempUserToGroupMappingEventBuffer.First.Value.OccurredTime); },
                            "user to group mapping",
                            EventBuffer.UserToGroupMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.GroupToGroupMapping:
                        ProcessKWayMergeStep<LinkedList<GroupToGroupMappingEventBufferItem<TGroup>>, GroupToGroupMappingEventBufferItem<TGroup>>
                        (
                            tempGroupToGroupMappingEventBuffer,
                            (eventPersister) => { eventPersister.AddGroupToGroupMapping(tempGroupToGroupMappingEventBuffer.First.Value.FromGroup, tempGroupToGroupMappingEventBuffer.First.Value.ToGroup, tempGroupToGroupMappingEventBuffer.First.Value.OccurredTime); },
                            (eventPersister) => { eventPersister.RemoveGroupToGroupMapping(tempGroupToGroupMappingEventBuffer.First.Value.FromGroup, tempGroupToGroupMappingEventBuffer.First.Value.ToGroup, tempGroupToGroupMappingEventBuffer.First.Value.OccurredTime); },
                            "group to group mapping",
                            EventBuffer.GroupToGroupMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.UserToApplicationComponentAndAccessLevelMapping:
                        ProcessKWayMergeStep<LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>, UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>
                        (
                            tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
                            (eventPersister) => 
                            { 
                                eventPersister.AddUserToApplicationComponentAndAccessLevelMapping
                                (
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.User,
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.ApplicationComponent,
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.AccessLevel,
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.OccurredTime
                                ); 
                            },
                            (eventPersister) =>
                            {
                                eventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping
                                (
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.User,
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.ApplicationComponent,
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.AccessLevel,
                                    tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.OccurredTime
                                );
                            },
                            "user to application component and access level mapping",
                            EventBuffer.UserToApplicationComponentAndAccessLevelMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.GroupToApplicationComponentAndAccessLevelMapping:
                        ProcessKWayMergeStep<LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>, GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>
                        (
                            tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
                            (eventPersister) =>
                            {
                                eventPersister.AddGroupToApplicationComponentAndAccessLevelMapping
                                (
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Group,
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.ApplicationComponent,
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.AccessLevel,
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.OccurredTime
                                );
                            },
                            (eventPersister) =>
                            {
                                eventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping
                                (
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Group,
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.ApplicationComponent,
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.AccessLevel,
                                    tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.OccurredTime
                                );
                            },
                            "group to application component and access level mapping",
                            EventBuffer.GroupToApplicationComponentAndAccessLevelMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.EntityType:
                        ProcessKWayMergeStep<LinkedList<EntityTypeEventBufferItem>, EntityTypeEventBufferItem>
                        (
                            tempEntityTypeEventBuffer,
                            (eventPersister) => { eventPersister.AddEntityType(tempEntityTypeEventBuffer.First.Value.EntityType, tempEntityTypeEventBuffer.First.Value.OccurredTime); },
                            (eventPersister) => { eventPersister.RemoveEntityType(tempEntityTypeEventBuffer.First.Value.EntityType, tempEntityTypeEventBuffer.First.Value.OccurredTime); },
                            "entity type",
                            EventBuffer.EntityType,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.Entity:
                        ProcessKWayMergeStep<LinkedList<EntityEventBufferItem>, EntityEventBufferItem>
                        (
                            tempEntityEventBuffer,
                            (eventPersister) => { eventPersister.AddEntity(tempEntityEventBuffer.First.Value.EntityType, tempEntityEventBuffer.First.Value.Entity, tempEntityEventBuffer.First.Value.OccurredTime); },
                            (eventPersister) => { eventPersister.RemoveEntity(tempEntityEventBuffer.First.Value.EntityType, tempEntityEventBuffer.First.Value.Entity, tempEntityEventBuffer.First.Value.OccurredTime); },
                            "entity",
                            EventBuffer.Entity,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.UserToEntityMapping:
                        ProcessKWayMergeStep<LinkedList<UserToEntityMappingEventBufferItem<TUser>>, UserToEntityMappingEventBufferItem<TUser>>
                        (
                            tempUserToEntityMappingEventBuffer,
                            (eventPersister) => 
                            {
                                eventPersister.AddUserToEntityMapping
                                (
                                    tempUserToEntityMappingEventBuffer.First.Value.User,
                                    tempUserToEntityMappingEventBuffer.First.Value.EntityType, 
                                    tempUserToEntityMappingEventBuffer.First.Value.Entity, 
                                    tempUserToEntityMappingEventBuffer.First.Value.OccurredTime
                                ); 
                            },
                            (eventPersister) =>
                            {
                                eventPersister.RemoveUserToEntityMapping
                                (
                                    tempUserToEntityMappingEventBuffer.First.Value.User,
                                    tempUserToEntityMappingEventBuffer.First.Value.EntityType,
                                    tempUserToEntityMappingEventBuffer.First.Value.Entity,
                                    tempUserToEntityMappingEventBuffer.First.Value.OccurredTime
                                );
                            },
                            "user to entity mapping",
                            EventBuffer.UserToEntityMapping,
                            nextSequenceNumbers
                        );
                        break;

                    case EventBuffer.GroupToEntityMapping:
                        ProcessKWayMergeStep<LinkedList<GroupToEntityMappingEventBufferItem<TGroup>>, GroupToEntityMappingEventBufferItem<TGroup>>
                        (
                            tempGroupToEntityMappingEventBuffer,
                            (eventPersister) =>
                            {
                                eventPersister.AddGroupToEntityMapping
                                (
                                    tempGroupToEntityMappingEventBuffer.First.Value.Group,
                                    tempGroupToEntityMappingEventBuffer.First.Value.EntityType,
                                    tempGroupToEntityMappingEventBuffer.First.Value.Entity,
                                    tempGroupToEntityMappingEventBuffer.First.Value.OccurredTime
                                );
                            },
                            (eventPersister) =>
                            {
                                eventPersister.RemoveGroupToEntityMapping
                                (
                                    tempGroupToEntityMappingEventBuffer.First.Value.Group,
                                    tempGroupToEntityMappingEventBuffer.First.Value.EntityType,
                                    tempGroupToEntityMappingEventBuffer.First.Value.Entity,
                                    tempGroupToEntityMappingEventBuffer.First.Value.OccurredTime
                                );
                            },
                            "group to entity mapping",
                            EventBuffer.GroupToEntityMapping,
                            nextSequenceNumbers
                        );
                        break;

                    default:
                        throw new NotImplementedException($"No K-merge step implementation exists for {nameof(EventBuffer)} 'nameof{nextSequenceNumber.EventBuffer}'.");
                }
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Re-throws the exception which caused validation failure, if the exception exists.
        /// </summary>
        /// <param name="validationResult">The validation result to check.</param>
        protected void ThrowExceptionIfValidationFails(ValidationResult validationResult)
        {
            if (validationResult.Successful == false && validationResult.ValidationException != null)
            {
                throw validationResult.ValidationException;
            }
        }

        /// <summary>
        /// Moves all events from the buffer queues to temporary queues.
        /// </summary>
        protected void MoveEventsToTemporaryQueues
        (
            out LinkedList<UserEventBufferItem<TUser>> tempUserEventBuffer,
            out LinkedList<GroupEventBufferItem<TGroup>> tempGroupEventBuffer,
            out LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>> tempUserToGroupMappingEventBuffer,
            out LinkedList<GroupToGroupMappingEventBufferItem<TGroup>> tempGroupToGroupMappingEventBuffer ,
            out LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>> tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
            out LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>> tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
            out LinkedList<EntityTypeEventBufferItem> tempEntityTypeEventBuffer,
            out LinkedList<EntityEventBufferItem> tempEntityEventBuffer,
            out LinkedList<UserToEntityMappingEventBufferItem<TUser>> tempUserToEntityMappingEventBuffer,
            out LinkedList<GroupToEntityMappingEventBufferItem<TGroup>> tempGroupToEntityMappingEventBuffer
        )
        {
            Int64 maxSequenceNumber = lastEventSequenceNumber;
            MoveEventsToTemporaryQueue<LinkedList<UserEventBufferItem<TUser>>, UserEventBufferItem<TUser>>(ref userEventBuffer, out tempUserEventBuffer, userEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<GroupEventBufferItem<TGroup>>, GroupEventBufferItem<TGroup>>(ref groupEventBuffer, out tempGroupEventBuffer, groupEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>>, UserToGroupMappingEventBufferItem<TUser, TGroup>>(ref userToGroupMappingEventBuffer, out tempUserToGroupMappingEventBuffer, userToGroupMappingEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<GroupToGroupMappingEventBufferItem<TGroup>>, GroupToGroupMappingEventBufferItem<TGroup>>(ref groupToGroupMappingEventBuffer, out tempGroupToGroupMappingEventBuffer, groupToGroupMappingEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>, UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>(ref userToApplicationComponentAndAccessLevelMappingEventBuffer, out tempUserToApplicationComponentAndAccessLevelMappingEventBuffer, userToApplicationComponentAndAccessLevelMappingEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>, GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>(ref groupToApplicationComponentAndAccessLevelMappingEventBuffer, out tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer, groupToApplicationComponentAndAccessLevelMappingEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<EntityTypeEventBufferItem>, EntityTypeEventBufferItem>(ref entityTypeEventBuffer, out tempEntityTypeEventBuffer, entityTypeEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<EntityEventBufferItem>, EntityEventBufferItem>(ref entityEventBuffer, out tempEntityEventBuffer, entityEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<UserToEntityMappingEventBufferItem<TUser>>, UserToEntityMappingEventBufferItem<TUser>>(ref userToEntityMappingEventBuffer, out tempUserToEntityMappingEventBuffer, userToEntityMappingEventBufferLock, maxSequenceNumber);
            MoveEventsToTemporaryQueue<LinkedList<GroupToEntityMappingEventBufferItem<TGroup>>, GroupToEntityMappingEventBufferItem<TGroup>>(ref groupToEntityMappingEventBuffer, out tempGroupToEntityMappingEventBuffer, groupToEntityMappingEventBufferLock, maxSequenceNumber);
        }

        /// <summary>
        /// Initializes the specified heap with the first elements of each of the specified buffers, in order to perform a k-merge using the heap.
        /// </summary>
        protected void InitializeHeap
        (
            MinHeap<SequenceNumberAndEventBuffer> heap,
            LinkedList<UserEventBufferItem<TUser>> tempUserEventBuffer,
            LinkedList<GroupEventBufferItem<TGroup>> tempGroupEventBuffer,
            LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>> tempUserToGroupMappingEventBuffer,
            LinkedList<GroupToGroupMappingEventBufferItem<TGroup>> tempGroupToGroupMappingEventBuffer,
            LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>> tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
            LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>> tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
            LinkedList<EntityTypeEventBufferItem> tempEntityTypeEventBuffer,
            LinkedList<EntityEventBufferItem> tempEntityEventBuffer,
            LinkedList<UserToEntityMappingEventBufferItem<TUser>> tempUserToEntityMappingEventBuffer,
            LinkedList<GroupToEntityMappingEventBufferItem<TGroup>> tempGroupToEntityMappingEventBuffer
        )
        {
            AddBufferedEventToHeap<LinkedList<UserEventBufferItem<TUser>>, UserEventBufferItem<TUser>>(tempUserEventBuffer, heap, EventBuffer.User);
            AddBufferedEventToHeap<LinkedList<GroupEventBufferItem<TGroup>>, GroupEventBufferItem<TGroup>>(tempGroupEventBuffer, heap, EventBuffer.Group);
            AddBufferedEventToHeap<LinkedList<UserToGroupMappingEventBufferItem<TUser, TGroup>>, UserToGroupMappingEventBufferItem<TUser, TGroup>>(tempUserToGroupMappingEventBuffer, heap, EventBuffer.UserToGroupMapping);
            AddBufferedEventToHeap<LinkedList<GroupToGroupMappingEventBufferItem<TGroup>>, GroupToGroupMappingEventBufferItem<TGroup>>(tempGroupToGroupMappingEventBuffer, heap, EventBuffer.GroupToGroupMapping);
            AddBufferedEventToHeap<LinkedList<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>, UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>(tempUserToApplicationComponentAndAccessLevelMappingEventBuffer, heap, EventBuffer.UserToApplicationComponentAndAccessLevelMapping);
            AddBufferedEventToHeap<LinkedList<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>, GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>(tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer, heap, EventBuffer.GroupToApplicationComponentAndAccessLevelMapping);
            AddBufferedEventToHeap<LinkedList<EntityTypeEventBufferItem>, EntityTypeEventBufferItem>(tempEntityTypeEventBuffer, heap, EventBuffer.EntityType);
            AddBufferedEventToHeap<LinkedList<EntityEventBufferItem>, EntityEventBufferItem>(tempEntityEventBuffer, heap, EventBuffer.Entity);
            AddBufferedEventToHeap<LinkedList<UserToEntityMappingEventBufferItem<TUser>>, UserToEntityMappingEventBufferItem<TUser>>(tempUserToEntityMappingEventBuffer, heap, EventBuffer.UserToEntityMapping);
            AddBufferedEventToHeap<LinkedList<GroupToEntityMappingEventBufferItem<TGroup>>, GroupToEntityMappingEventBufferItem<TGroup>>(tempGroupToEntityMappingEventBuffer, heap, EventBuffer.GroupToEntityMapping);
        }

        /// <summary>
        /// Processes a step of the k-way merge process used to order and process all events to flush, for a single event buffer.
        /// </summary>
        /// <typeparam name="TEventBuffer">The type of the event buffer to process.</typeparam>
        /// <typeparam name="TEventBufferItemType">The type of items in the event buffer.</typeparam>
        /// <param name="temporaryEventBuffer">The event buffer to process.</param>
        /// <param name="addAction">The action to perform if the event adds an item.</param>
        /// <param name="removeAction">The action to perform if the event removes an item.</param>
        /// <param name="eventName">The name of the type of the event.  Used in exception messages.</param>
        /// <param name="eventBufferEnum">The enum representing the temporary event buffer.</param>
        /// <param name="nextSequenceNumbers">A min-heap containing the sequence numbers of events to process and the event buffer they exist in.</param>
        protected void ProcessKWayMergeStep<TEventBuffer, TEventBufferItemType>
        (
            TEventBuffer temporaryEventBuffer,
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> addAction,
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> removeAction,
            String eventName,
            EventBuffer eventBufferEnum, 
            MinHeap<SequenceNumberAndEventBuffer> nextSequenceNumbers
        ) 
            where TEventBuffer: LinkedList<TEventBufferItemType>
            where TEventBufferItemType: EventBufferItemBase
        {
            if (temporaryEventBuffer.First.Value.EventAction == EventAction.Add)
            {
                InvokeEventPersisterAction(addAction, $"Failed to persist 'add {eventName}' event.");
            }
            else if (temporaryEventBuffer.First.Value.EventAction == EventAction.Remove)
            {
                InvokeEventPersisterAction(removeAction, $"Failed to persist 'remove {eventName}' event.");
            }
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
            where TEventBuffer : LinkedList<TEventBufferItemType>
            where TEventBufferItemType : EventBufferItemBase
        {
            if (eventBuffer.Count > 0)
            {
                nextSequenceNumbers.Insert(new SequenceNumberAndEventBuffer(eventBuffer.First.Value.SequenceNumber, eventBufferEnum));
            }
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
        protected virtual void MoveEventsToTemporaryQueue<TEventBuffer, TEventBufferItemType>(ref TEventBuffer eventBuffer, out TEventBuffer temporaryEventBuffer, Object eventBufferLockObject, Int64 maxSequenceNumber) 
            where TEventBuffer: LinkedList<TEventBufferItemType>, new() 
            where TEventBufferItemType: EventBufferItemBase
        {
            lock(eventBufferLockObject)
            {
                // If the sequence number of the first item in the event buffer is greater than parameter 'maxSequenceNumber', it means that all events were buffered after the current flush process started, and hence none of them need to be processed
                if (eventBuffer.Count > 0 && eventBuffer.First.Value.SequenceNumber > maxSequenceNumber)
                {
                    temporaryEventBuffer = new TEventBuffer();
                }
                else
                {
                    temporaryEventBuffer = eventBuffer;
                    eventBuffer = new TEventBuffer();
                    // Move any events queued since 'maxSequenceNumber' was captured back to the front of the main queue
                    //   This will prevent any events from being processed before an event they depend on (e.g. if a 'user add' event and an 'user to entity mapping add' event both occur after the flush has started, and after the 'userEventBuffer' has been processed, but before the 'userToEntityMappingEventBuffer' has been processed)
                    while (temporaryEventBuffer.Count > 0 && temporaryEventBuffer.Last.Value.SequenceNumber > maxSequenceNumber)
                    {
                        eventBuffer.AddFirst(temporaryEventBuffer.Last.Value);
                        temporaryEventBuffer.RemoveLast();
                    }
                }
            }
        }

        /// <summary>
        /// Invokes the specified action on the classes' 'eventPersister' member, throwing an exception with the specified message if an exception occurs.
        /// </summary>
        /// <param name="action">The action to on the 'eventPersister' member.  Accepts a single parameter which is the 'eventPersister' member.</param>
        /// <param name="exceptionMessage">The message to use in the exception thrown if an error occurs invoking the action.</param>
        protected void InvokeEventPersisterAction(Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> action, String exceptionMessage)
        {
            try
            {
                action.Invoke(eventPersister);
            }
            catch (Exception e)
            {
                throw new Exception(exceptionMessage, e);
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the InMemoryEventBuffer.
        /// </summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        ~InMemoryEventBuffer()
        {
            Dispose(false);
        }

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

        #pragma warning disable 0693
        
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
            /// Initialises a new instance of the ApplicationAccess.Persistence.InMemoryEventBuffer+SequenceNumberAndEventBuffer class.
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

        #pragma warning restore 0693

        #pragma warning restore 1591

        #endregion
    }
}
