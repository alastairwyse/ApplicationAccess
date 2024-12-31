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
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Utilities;
using ApplicationAccess.Validation;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Buffers events which change the structure of an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> class in memory before writing them to an instance of <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventPersisterBufferBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The persister to use to write flushed events to permanent storage.</summary>
        protected IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister;
        /// <summary>Stores the number of events processed during a call to the Flush() method.</summary>
        protected Int32 flushedEventCount;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator, 
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator)
        {
            this.eventPersister = eventPersister;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, metricLogger)
        {
            this.eventPersister = eventPersister;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger, 
            IGuidProvider guidProvider,
            IDateTimeProvider dateTimeProvider
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, metricLogger, guidProvider, dateTimeProvider)
        {
            this.eventPersister = eventPersister;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            flushedEventCount = 0;
            Guid beginId = metricLogger.Begin(new FlushTime());
            try
            {
                base.Flush();
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

        /// <inheritdoc/>
        protected override void ProcessUserEventBufferItem(UserEventBufferItem<TUser> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddUser
                    (
                        eventBufferItem.User,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime, 
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveUser
                    (
                        eventBufferItem.User,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "user");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage); 
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessGroupEventBufferItem(GroupEventBufferItem<TGroup> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddGroup
                    (
                        eventBufferItem.Group,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveGroup
                    (
                        eventBufferItem.Group,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "group");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessUserToGroupMappingEventBufferItem(UserToGroupMappingEventBufferItem<TUser, TGroup> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddUserToGroupMapping
                    (
                        eventBufferItem.User,
                        eventBufferItem.Group,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveUserToGroupMapping
                    (
                        eventBufferItem.User,
                        eventBufferItem.Group,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "user to group mapping");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessGroupToGroupMappingEventBufferItem(GroupToGroupMappingEventBufferItem<TGroup> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddGroupToGroupMapping
                    (
                        eventBufferItem.FromGroup,
                        eventBufferItem.ToGroup,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveGroupToGroupMapping
                    (
                        eventBufferItem.FromGroup,
                        eventBufferItem.ToGroup,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "group to group mapping");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessUserToApplicationComponentAndAccessLevelMappingEventBufferItem(UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddUserToApplicationComponentAndAccessLevelMapping
                    (
                        eventBufferItem.User,
                        eventBufferItem.ApplicationComponent,
                        eventBufferItem.AccessLevel,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveUserToApplicationComponentAndAccessLevelMapping
                    (
                        eventBufferItem.User,
                        eventBufferItem.ApplicationComponent,
                        eventBufferItem.AccessLevel,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "user to application component and access level mapping");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessGroupToApplicationComponentAndAccessLevelMappingEventBufferItem(GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddGroupToApplicationComponentAndAccessLevelMapping
                    (
                        eventBufferItem.Group,
                        eventBufferItem.ApplicationComponent,
                        eventBufferItem.AccessLevel,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveGroupToApplicationComponentAndAccessLevelMapping
                    (
                        eventBufferItem.Group,
                        eventBufferItem.ApplicationComponent,
                        eventBufferItem.AccessLevel,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "group to application component and access level mapping");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessEntityTypeEventBufferItem(EntityTypeEventBufferItem eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddEntityType
                    (
                        eventBufferItem.EntityType,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveEntityType
                    (
                        eventBufferItem.EntityType,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "entity type");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessEntityEventBufferItem(EntityEventBufferItem eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddEntity
                    (
                        eventBufferItem.EntityType,
                        eventBufferItem.Entity,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveEntity
                    (
                        eventBufferItem.EntityType,
                        eventBufferItem.Entity,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "entity");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessUserToEntityMappingEventBufferItem(UserToEntityMappingEventBufferItem<TUser> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddUserToEntityMapping
                    (
                        eventBufferItem.User,
                        eventBufferItem.EntityType,
                        eventBufferItem.Entity,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveUserToEntityMapping
                    (
                        eventBufferItem.User,
                        eventBufferItem.EntityType,
                        eventBufferItem.Entity,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "user to entity mapping");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
        }

        /// <inheritdoc/>
        protected override void ProcessGroupToEntityMappingEventBufferItem(GroupToEntityMappingEventBufferItem<TGroup> eventBufferItem)
        {
            Action<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> persisterAction;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                persisterAction = (persister) =>
                {
                    persister.AddGroupToEntityMapping
                    (
                        eventBufferItem.Group,
                        eventBufferItem.EntityType,
                        eventBufferItem.Entity,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    );
                };
            }
            else
            {
                persisterAction = (persister) =>
                {
                    persister.RemoveGroupToEntityMapping
                    (
                        eventBufferItem.Group,
                        eventBufferItem.EntityType,
                        eventBufferItem.Entity,
                        eventBufferItem.EventId,
                        eventBufferItem.OccurredTime,
                        eventBufferItem.HashCode
                    ); ;
                };
            }
            String persistenceExceptionMessage = GeneratePersistenceExceptionMessage(eventBufferItem.EventAction, "group to entity mapping");
            InvokeEventPersisterAction(persisterAction, persistenceExceptionMessage);
            flushedEventCount++;
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

        /// <summary>
        /// Generates an exception message describing a failure to persist a buffered event.
        /// </summary>
        /// <param name="eventAction">The type of action of the event.</param>
        /// <param name="eventDataName">The human-readable name of the data contained in the event.</param>
        /// <returns>The exception message.</returns>
        protected String GeneratePersistenceExceptionMessage(EventAction eventAction, String eventDataName)
        {
            string eventActionString;
            if (eventAction == EventAction.Add)
            {
                eventActionString = "add";
            }
            else
            {
                eventActionString = "remove";
            }

            return $"Failed to persist '{eventActionString} {eventDataName}' event.";
        }

        #endregion
    }
}
