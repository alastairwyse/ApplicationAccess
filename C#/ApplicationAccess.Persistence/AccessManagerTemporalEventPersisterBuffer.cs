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
using ApplicationAccess.Utilities;
using MoreComplexDataStructures;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Buffers events which change the structure of an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> class in memory before writing them to an instance of <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/>"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventBuffer<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        /// <summary>The validator to use to validate events.</summary>
        protected IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator;
        /// <summary>The strategy to use for flushing the buffers.</summary>
        protected IAccessManagerEventBufferFlushStrategy bufferFlushStrategy;
        /// <summary>The persister to use to write flushed events to permanent storage.</summary>
        protected IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister;
        /// <summary>The provider to use for random Guids.</summary>
        protected Utilities.IGuidProvider guidProvider;
        /// <summary>The provider to use for the current date and time.</summary>
        protected IDateTimeProvider dateTimeProvider;
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

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        )
        {
            this.eventValidator = eventValidator;
            this.bufferFlushStrategy = bufferFlushStrategy;
            // Subscribe to the bufferFlushStrategy's 'BufferFlushed' event
            bufferFlushedEventHandler = (Object sender, EventArgs e) => { Flush(); };
            bufferFlushStrategy.BufferFlushed += bufferFlushedEventHandler;
            this.eventPersister = eventPersister;
            guidProvider = new Utilities.DefaultGuidProvider();
            dateTimeProvider = new StopwatchDateTimeProvider();
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
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger
        ) : this(eventValidator, bufferFlushStrategy, eventPersister)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="lastEventSequenceNumber">The sequence number used for the last event buffered.</param>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            Int64 lastEventSequenceNumber
        ) : this(eventValidator, bufferFlushStrategy, eventPersister)
        {
            if (lastEventSequenceNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(lastEventSequenceNumber), $"Parameter '{nameof(lastEventSequenceNumber)}' with value {lastEventSequenceNumber} cannot be less than 0.");

            this.lastEventSequenceNumber = lastEventSequenceNumber;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="lastEventSequenceNumber">The sequence number used for the last event buffered.</param>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger,
            Int64 lastEventSequenceNumber
        ) : this(eventValidator, bufferFlushStrategy, eventPersister, lastEventSequenceNumber)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            Utilities.IGuidProvider guidProvider,
            IDateTimeProvider dateTimeProvider
        ) : this(eventValidator, bufferFlushStrategy, eventPersister)
        {
            this.guidProvider = guidProvider;
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddUser(TUser user)
        {
            Action<TUser> postValidationAction = (actionUser) =>
            {
                var userEvent = new UserEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Add, user, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserEventBufferItem<TUser>, Int64>(userEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserEventBufferItemCount = userEventBuffer.Count;
                metricLogger.Increment(new AddUserEventBuffered());
                metricLogger.Set(new UserEventsBuffered(), userEventBuffer.Count);
            };
            lock (userEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUser(user, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUser(TUser user)
        {
            Action<TUser> postValidationAction = (actionUser) =>
            {
                var userEvent = new UserEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Remove, user, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserEventBufferItem<TUser>, Int64>(userEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserEventBufferItemCount = userEventBuffer.Count;
                metricLogger.Increment(new RemoveUserEventBuffered());
                metricLogger.Set(new UserEventsBuffered(), userEventBuffer.Count);
            };
            lock (userEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUser(user, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddGroup(TGroup group)
        {
            Action<TGroup> postValidationAction = (actionGroup) =>
            {
                var groupEvent = new GroupEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Add, group, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupEventBufferItem<TGroup>, Int64>(groupEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupEventBufferItemCount = groupEventBuffer.Count;
                metricLogger.Increment(new AddGroupEventBuffered());
                metricLogger.Set(new GroupEventsBuffered(), groupEventBuffer.Count);
            };
            lock (groupEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroup(group, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroup(TGroup group)
        {
            Action<TGroup> postValidationAction = (actionGroup) =>
            {
                var groupEvent = new GroupEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Remove, group, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupEventBufferItem<TGroup>, Int64>(groupEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupEventBufferItemCount = groupEventBuffer.Count;
                metricLogger.Increment(new RemoveGroupEventBuffered());
                metricLogger.Set(new GroupEventsBuffered(), groupEventBuffer.Count);
            };
            lock (groupEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroup(group, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup> postValidationAction = (actionUser, actionGroup) =>
            {
                var mappingEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(guidProvider.NewGuid(), EventAction.Add, user, group, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userToGroupMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserToGroupMappingEventBufferItemCount = userToGroupMappingEventBuffer.Count;
                metricLogger.Increment(new AddUserToGroupMappingEventBuffered());
                metricLogger.Set(new UserToGroupMappingEventsBuffered(), userToGroupMappingEventBuffer.Count);
            };
            lock (userToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToGroupMapping(user, group, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup> postValidationAction = (actionUser, actionGroup) =>
            {
                var mappingEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(guidProvider.NewGuid(), EventAction.Remove, user, group, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userToGroupMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserToGroupMappingEventBufferItemCount = userToGroupMappingEventBuffer.Count;
                metricLogger.Increment(new RemoveUserToGroupMappingEventBuffered());
                metricLogger.Set(new UserToGroupMappingEventsBuffered(), userToGroupMappingEventBuffer.Count);
            };
            lock (userToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToGroupMapping(user, group, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                var mappingEvent = new GroupToGroupMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Add, fromGroup, toGroup, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupToGroupMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = groupToGroupMappingEventBuffer.Count;
                metricLogger.Increment(new AddGroupToGroupMappingEventBuffered());
                metricLogger.Set(new GroupToGroupMappingEventsBuffered(), groupToGroupMappingEventBuffer.Count);
            };
            lock (groupToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToGroupMapping(fromGroup, toGroup, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                var mappingEvent = new GroupToGroupMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Remove, fromGroup, toGroup, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupToGroupMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = groupToGroupMappingEventBuffer.Count;
                metricLogger.Increment(new RemoveGroupToGroupMappingEventBuffered());
                metricLogger.Set(new GroupToGroupMappingEventsBuffered(), groupToGroupMappingEventBuffer.Count);
            };
            lock (groupToGroupMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToGroupMapping(fromGroup, toGroup, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Add, user, applicationComponent, accessLevel, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = userToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                metricLogger.Increment(new AddUserToApplicationComponentAndAccessLevelMappingEventBuffered());
                metricLogger.Set(new UserToApplicationComponentAndAccessLevelMappingEventsBuffered(), userToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            };
            lock (userToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Remove, user, applicationComponent, accessLevel, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = userToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                metricLogger.Increment(new RemoveUserToApplicationComponentAndAccessLevelMappingEventBuffered());
                metricLogger.Set(new UserToApplicationComponentAndAccessLevelMappingEventsBuffered(), userToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            };
            lock (userToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Add, group, applicationComponent, accessLevel, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                metricLogger.Increment(new AddGroupToApplicationComponentAndAccessLevelMappingEventBuffered());
                metricLogger.Set(new GroupToApplicationComponentAndAccessLevelMappingEventsBuffered(), groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            };
            lock (groupToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                var mappingEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(guidProvider.NewGuid(), EventAction.Remove, group, applicationComponent, accessLevel, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupToApplicationComponentAndAccessLevelMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count;
                metricLogger.Increment(new RemoveGroupToApplicationComponentAndAccessLevelMappingEventBuffered());
                metricLogger.Set(new GroupToApplicationComponentAndAccessLevelMappingEventsBuffered(), groupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            };
            lock (groupToApplicationComponentAndAccessLevelMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddEntityType(string entityType)
        {
            Action<string> postValidationAction = (actionEntityType) =>
            {
                var entityTypeEvent = new EntityTypeEventBufferItem(guidProvider.NewGuid(), EventAction.Add, entityType, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<EntityTypeEventBufferItem, Int64>(entityTypeEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                entityTypeEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.EntityTypeEventBufferItemCount = entityTypeEventBuffer.Count;
                metricLogger.Increment(new AddEntityTypeEventBuffered());
                metricLogger.Set(new EntityTypeEventsBuffered(), entityTypeEventBuffer.Count);
            };
            lock (entityTypeEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddEntityType(entityType, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveEntityType(string entityType)
        {
            Action<string> postValidationAction = (actionEntityType) =>
            {
                var entityTypeEvent = new EntityTypeEventBufferItem(guidProvider.NewGuid(), EventAction.Remove, entityType, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<EntityTypeEventBufferItem, Int64>(entityTypeEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                entityTypeEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.EntityTypeEventBufferItemCount = entityTypeEventBuffer.Count;
                metricLogger.Increment(new RemoveEntityTypeEventBuffered());
                metricLogger.Set(new EntityTypeEventsBuffered(), entityTypeEventBuffer.Count);
            };
            lock (entityTypeEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveEntityType(entityType, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddEntity(string entityType, string entity)
        {
            Action<string, string> postValidationAction = (actionEntityType, actionEntity) =>
            {
                var entityEvent = new EntityEventBufferItem(guidProvider.NewGuid(), EventAction.Add, entityType, entity, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<EntityEventBufferItem, Int64>(entityEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                entityEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.EntityEventBufferItemCount = entityEventBuffer.Count;
                metricLogger.Increment(new AddEntityEventBuffered());
                metricLogger.Set(new EntityEventsBuffered(), entityEventBuffer.Count);
            };
            lock (entityEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddEntity(entityType, entity, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveEntity(string entityType, string entity)
        {
            Action<string, string> postValidationAction = (actionEntityType, actionEntity) =>
            {
                var entityEvent = new EntityEventBufferItem(guidProvider.NewGuid(), EventAction.Remove, entityType, entity, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<EntityEventBufferItem, Int64>(entityEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                entityEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.EntityEventBufferItemCount = entityEventBuffer.Count;
                metricLogger.Increment(new RemoveEntityEventBuffered());
                metricLogger.Set(new EntityEventsBuffered(), entityEventBuffer.Count);
            };
            lock (entityEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveEntity(entityType, entity, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddUserToEntityMapping(TUser user, string entityType, string entity)
        {
            Action<TUser, string, string> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new UserToEntityMappingEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Add, user, entityType, entity, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userToEntityMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserToEntityMappingEventBufferItemCount = userToEntityMappingEventBuffer.Count;
                metricLogger.Increment(new AddUserToEntityMappingEventBuffered());
                metricLogger.Set(new UserToEntityMappingEventsBuffered(), userToEntityMappingEventBuffer.Count);
            };
            lock (userToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddUserToEntityMapping(user, entityType, entity, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveUserToEntityMapping(TUser user, string entityType, string entity)
        {
            Action<TUser, string, string> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new UserToEntityMappingEventBufferItem<TUser>(guidProvider.NewGuid(), EventAction.Remove, user, entityType, entity, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                userToEntityMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.UserToEntityMappingEventBufferItemCount = userToEntityMappingEventBuffer.Count;
                metricLogger.Increment(new RemoveUserToEntityMappingEventBuffered());
                metricLogger.Set(new UserToEntityMappingEventsBuffered(), userToEntityMappingEventBuffer.Count);
            };
            lock (userToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveUserToEntityMapping(user, entityType, entity, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void AddGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            Action<TGroup, string, string> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new GroupToEntityMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Add, group, entityType, entity, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupToEntityMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = groupToEntityMappingEventBuffer.Count;
                metricLogger.Increment(new AddGroupToEntityMappingEventBuffered());
                metricLogger.Set(new GroupToEntityMappingEventsBuffered(), groupToEntityMappingEventBuffer.Count);
            };
            lock (groupToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateAddGroupToEntityMapping(group, entityType, entity, postValidationAction));
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void RemoveGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            Action<TGroup, string, string> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                var mappingEvent = new GroupToEntityMappingEventBufferItem<TGroup>(guidProvider.NewGuid(), EventAction.Remove, group, entityType, entity, dateTimeProvider.UtcNow());
                var queueNode = new Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>(mappingEvent, Interlocked.Increment(ref lastEventSequenceNumber));
                groupToEntityMappingEventBuffer.AddLast(queueNode);
                bufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = groupToEntityMappingEventBuffer.Count;
                metricLogger.Increment(new RemoveGroupToEntityMappingEventBuffered());
                metricLogger.Set(new GroupToEntityMappingEventsBuffered(), groupToEntityMappingEventBuffer.Count);
            };
            lock (groupToEntityMappingEventBufferLock)
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveGroupToEntityMapping(group, entityType, entity, postValidationAction));
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerEventBuffer`4.Flush"]/*'/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public void Flush()
        {
            Int32 flushedEventCount = 0;
            Guid beginId = metricLogger.Begin(new FlushTime());

            try
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
                                (eventPersister) =>
                                {
                                    UserEventBufferItem<TUser> bufferFirstItem = tempUserEventBuffer.First.Value.Item1;
                                    eventPersister.AddUser
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.EventId, 
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                (eventPersister) =>
                                {
                                    UserEventBufferItem<TUser> bufferFirstItem = tempUserEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveUser
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "user",
                                EventBuffer.User,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.Group:
                            ProcessKWayMergeStep<LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>>, GroupEventBufferItem<TGroup>>
                            (
                                tempGroupEventBuffer,
                                (eventPersister) => 
                                {
                                    GroupEventBufferItem<TGroup> bufferFirstItem = tempGroupEventBuffer.First.Value.Item1;
                                    eventPersister.AddGroup
                                    (
                                        bufferFirstItem.Group,
                                        bufferFirstItem.EventId, 
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                (eventPersister) =>
                                {
                                    GroupEventBufferItem<TGroup> bufferFirstItem = tempGroupEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveGroup
                                    (
                                        bufferFirstItem.Group,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "group",
                                EventBuffer.Group,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.UserToGroupMapping:
                            ProcessKWayMergeStep<LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>>, UserToGroupMappingEventBufferItem<TUser, TGroup>>
                            (
                                tempUserToGroupMappingEventBuffer,
                                (eventPersister) => 
                                {
                                    UserToGroupMappingEventBufferItem<TUser, TGroup> bufferFirstItem = tempUserToGroupMappingEventBuffer.First.Value.Item1;
                                    eventPersister.AddUserToGroupMapping
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.Group,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                (eventPersister) =>
                                {
                                    UserToGroupMappingEventBufferItem<TUser, TGroup> bufferFirstItem = tempUserToGroupMappingEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveUserToGroupMapping
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.Group,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "user to group mapping",
                                EventBuffer.UserToGroupMapping,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.GroupToGroupMapping:
                            ProcessKWayMergeStep<LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>>, GroupToGroupMappingEventBufferItem<TGroup>>
                            (
                                tempGroupToGroupMappingEventBuffer,
                                (eventPersister) => 
                                {
                                    GroupToGroupMappingEventBufferItem<TGroup> bufferFirstItem = tempGroupToGroupMappingEventBuffer.First.Value.Item1;
                                    eventPersister.AddGroupToGroupMapping
                                    (
                                        bufferFirstItem.FromGroup,
                                        bufferFirstItem.ToGroup,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    ); 
                                },
                                (eventPersister) =>
                                {
                                    GroupToGroupMappingEventBufferItem<TGroup> bufferFirstItem = tempGroupToGroupMappingEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveGroupToGroupMapping
                                    (
                                        bufferFirstItem.FromGroup,
                                        bufferFirstItem.ToGroup,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "group to group mapping",
                                EventBuffer.GroupToGroupMapping,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.UserToApplicationComponentAndAccessLevelMapping:
                            ProcessKWayMergeStep<LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>>, UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>>
                            (
                                tempUserToApplicationComponentAndAccessLevelMappingEventBuffer,
                                (eventPersister) =>
                                {
                                    UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> bufferFirstItem = tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
                                    eventPersister.AddUserToApplicationComponentAndAccessLevelMapping
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.ApplicationComponent,
                                        bufferFirstItem.AccessLevel,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                (eventPersister) =>
                                {
                                    UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> bufferFirstItem = tempUserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.ApplicationComponent,
                                        bufferFirstItem.AccessLevel,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "user to application component and access level mapping",
                                EventBuffer.UserToApplicationComponentAndAccessLevelMapping,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.GroupToApplicationComponentAndAccessLevelMapping:
                            ProcessKWayMergeStep<LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>>, GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>>
                            (
                                tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer,
                                (eventPersister) =>
                                {
                                    GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> bufferFirstItem = tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
                                    eventPersister.AddGroupToApplicationComponentAndAccessLevelMapping
                                    (
                                        bufferFirstItem.Group,
                                        bufferFirstItem.ApplicationComponent,
                                        bufferFirstItem.AccessLevel,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                (eventPersister) =>
                                {
                                    GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> bufferFirstItem = tempGroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping
                                    (
                                        bufferFirstItem.Group,
                                        bufferFirstItem.ApplicationComponent,
                                        bufferFirstItem.AccessLevel,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "group to application component and access level mapping",
                                EventBuffer.GroupToApplicationComponentAndAccessLevelMapping,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.EntityType:
                            ProcessKWayMergeStep<LinkedList<Tuple<EntityTypeEventBufferItem, Int64>>, EntityTypeEventBufferItem>
                            (
                                tempEntityTypeEventBuffer,
                                (eventPersister) =>
                                {
                                    EntityTypeEventBufferItem bufferFirstItem = tempEntityTypeEventBuffer.First.Value.Item1;
                                    eventPersister.AddEntityType
                                    (
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    ); 
                                },
                                (eventPersister) =>
                                {
                                    EntityTypeEventBufferItem bufferFirstItem = tempEntityTypeEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveEntityType
                                    (
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "entity type",
                                EventBuffer.EntityType,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.Entity:
                            ProcessKWayMergeStep<LinkedList<Tuple<EntityEventBufferItem, Int64>>, EntityEventBufferItem>
                            (
                                tempEntityEventBuffer,
                                (eventPersister) => 
                                {
                                    EntityEventBufferItem bufferFirstItem = tempEntityEventBuffer.First.Value.Item1;
                                    eventPersister.AddEntity
                                    (
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.Entity,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    ); 
                                },
                                (eventPersister) =>
                                {
                                    EntityEventBufferItem bufferFirstItem = tempEntityEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveEntity
                                    (
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.Entity,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "entity",
                                EventBuffer.Entity,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.UserToEntityMapping:
                            ProcessKWayMergeStep<LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>>, UserToEntityMappingEventBufferItem<TUser>>
                            (
                                tempUserToEntityMappingEventBuffer,
                                (eventPersister) =>
                                {
                                    UserToEntityMappingEventBufferItem<TUser> bufferFirstItem = tempUserToEntityMappingEventBuffer.First.Value.Item1;
                                    eventPersister.AddUserToEntityMapping
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.Entity,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                (eventPersister) =>
                                {
                                    UserToEntityMappingEventBufferItem<TUser> bufferFirstItem = tempUserToEntityMappingEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveUserToEntityMapping
                                    (
                                        bufferFirstItem.User,
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.Entity,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                "user to entity mapping",
                                EventBuffer.UserToEntityMapping,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        case EventBuffer.GroupToEntityMapping:
                            ProcessKWayMergeStep<LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>>, GroupToEntityMappingEventBufferItem<TGroup>>
                            (
                                tempGroupToEntityMappingEventBuffer,
                                (eventPersister) =>
                                {
                                    GroupToEntityMappingEventBufferItem<TGroup> bufferFirstItem = tempGroupToEntityMappingEventBuffer.First.Value.Item1;
                                    eventPersister.AddGroupToEntityMapping
                                    (
                                        bufferFirstItem.Group,
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.Entity,
                                        bufferFirstItem.EventId,
                                        bufferFirstItem.OccurredTime
                                    );
                                },
                                (eventPersister) =>
                                {
                                    GroupToEntityMappingEventBufferItem<TGroup> bufferFirstItem = tempGroupToEntityMappingEventBuffer.First.Value.Item1;
                                    eventPersister.RemoveGroupToEntityMapping
                                    (
                                        bufferFirstItem.Group,
                                        bufferFirstItem.EntityType,
                                        bufferFirstItem.Entity,
                                        bufferFirstItem.EventId, 
                                        bufferFirstItem.OccurredTime
                                    ); ;
                                },
                                "group to entity mapping",
                                EventBuffer.GroupToEntityMapping,
                                nextSequenceNumbers
                            );
                            flushedEventCount++;
                            break;

                        default:
                            throw new NotImplementedException($"No K-merge step implementation exists for {nameof(EventBuffer)} '{nameof(nextSequenceNumber.EventBuffer)}'.");
                    }
                }
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new FlushTime());
                throw;
            }

            metricLogger.End(beginId, new FlushTime());
            metricLogger.Add(new BufferedEventsFlushed(), flushedEventCount);
            metricLogger.Increment(new BufferFlushOperationCompleted());
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
            Int64 maxSequenceNumber = lastEventSequenceNumber;
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
            where TEventBuffer : LinkedList<Tuple<TEventBufferItemType, Int64>>
            where TEventBufferItemType : TemporalEventBufferItemBase
        {
            if (temporaryEventBuffer.First.Value.Item1.EventAction == EventAction.Add)
            {
                InvokeEventPersisterAction(addAction, $"Failed to persist 'add {eventName}' event.");
            }
            else if (temporaryEventBuffer.First.Value.Item1.EventAction == EventAction.Remove)
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
            where TEventBuffer : LinkedList<Tuple<TEventBufferItemType, Int64>>
            where TEventBufferItemType : TemporalEventBufferItemBase
        {
            if (eventBuffer.Count > 0)
            {
                nextSequenceNumbers.Insert(new SequenceNumberAndEventBuffer(eventBuffer.First.Value.Item2, eventBufferEnum));
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
        /// Releases the unmanaged resources used by the AccessManagerTemporalEventPersisterBuffer.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~AccessManagerTemporalEventPersisterBuffer()
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
