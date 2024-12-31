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
using ApplicationMetrics;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Buffers events which change the structure of an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> class in memory before writing them to an instance of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/>.  
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventPersisterBufferBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The bulk persister to use to write flushed events to permanent storage.</summary>
        protected IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister;
        /// <summary>Temporarily holds all events processed during a Flush() operation, before passing them to the event persister.</summary>
        protected List<TemporalEventBufferItemBase> flushedEvents;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        public AccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator)
        {
            this.eventPersister = eventPersister;
            flushedEvents = new List<TemporalEventBufferItemBase>();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, metricLogger)
        {
            this.eventPersister = eventPersister;
            flushedEvents = new List<TemporalEventBufferItemBase>();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger, 
            IGuidProvider guidProvider,
            IDateTimeProvider dateTimeProvider
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, metricLogger, guidProvider, dateTimeProvider)
        {
            this.eventPersister = eventPersister;
            flushedEvents = new List<TemporalEventBufferItemBase>();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            flushedEvents.Clear();
            Guid beginId = metricLogger.Begin(new FlushTime());
            try
            {
                base.Flush();
                eventPersister.PersistEvents(flushedEvents);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new FlushTime());
                throw new Exception($"Failed to process buffers and persist flushed events.", e);
            }
            metricLogger.End(beginId, new FlushTime());
            metricLogger.Add(new BufferedEventsFlushed(), flushedEvents.Count);
            metricLogger.Increment(new BufferFlushOperationCompleted());
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override void ProcessUserEventBufferItem(UserEventBufferItem<TUser> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessGroupEventBufferItem(GroupEventBufferItem<TGroup> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessUserToGroupMappingEventBufferItem(UserToGroupMappingEventBufferItem<TUser, TGroup> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessGroupToGroupMappingEventBufferItem(GroupToGroupMappingEventBufferItem<TGroup> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessUserToApplicationComponentAndAccessLevelMappingEventBufferItem(UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessGroupToApplicationComponentAndAccessLevelMappingEventBufferItem(GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessEntityTypeEventBufferItem(EntityTypeEventBufferItem eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessEntityEventBufferItem(EntityEventBufferItem eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessUserToEntityMappingEventBufferItem(UserToEntityMappingEventBufferItem<TUser> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        /// <inheritdoc/>
        protected override void ProcessGroupToEntityMappingEventBufferItem(GroupToEntityMappingEventBufferItem<TGroup> eventBufferItem)
        {
            flushedEvents.Add(eventBufferItem);
        }

        #endregion
    }
}
