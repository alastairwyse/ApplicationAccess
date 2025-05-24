/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Persistence;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationAccess.Redistribution.Metrics;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Buffers <see cref="TemporalEventBufferItemBase"/> objects from two sources, persisting them when the buffer reaches a specified size.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager instance the events are persisted to.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager instance the events are persisted to.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager instance the events are persisted to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class EventPersisterBuffer<TUser, TGroup, TComponent, TAccess> : IEventPersisterBuffer
    {
        /// <summary>The buffer for <see cref="TemporalEventBufferItemBase"/> objects.</summary>
        protected Queue<TemporalEventBufferItemBase> eventBuffer;
        /// <summary>The ordinal sequence number of the next batch of events to persist.</summary>
        protected Int32 nextBatchNumber;
        /// <summary>The id of the most recent event from the first shard group that was buffered (or null if no events from the first shard group have been buffered).</summary>
        protected Nullable<Guid> sourceShardGroup1LastBufferedEventId;
        /// <summary>The id of the most recent event from the second shard group that was buffered (or null if no events from the second shard group have been buffered).</summary>
        protected Nullable<Guid> sourceShardGroup2LastBufferedEventId;
        /// <summary>The id of the most recent event from the first shard group that was persisted (or null if no events from the first shard group have been persisted).</summary>
        protected Nullable<Guid> sourceShardGroup1LastPersistedEventId;
        /// <summary>The id of the most recent event from the second shard group that was persisted (or null if no events from the second shard group have been persisted).</summary>
        protected Nullable<Guid> sourceShardGroup2LastPersistedEventId;
        /// <summary>The persister to write events to.</summary>
        protected IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> shardGroupEventPersister;
        /// <summary>The number of events to store in the buffer before persisting.</summary>
        protected readonly Int32 bufferSize;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>The ordinal sequence number of the next batch of events to persist.</summary>
        public Int32 NextBatchNumber
        {
            get { return nextBatchNumber; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.EventPersisterBuffer class.
        /// </summary>
        /// <param name="shardGroupEventPersister">The persister to write events to.</param>
        /// <param name="bufferSize">The number of events to store in the buffer before persisting.</param>
        /// <param name="nextBatchNumber">The ordinal sequence number of the next batch of events to persist.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public EventPersisterBuffer
        (
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> shardGroupEventPersister,
            Int32 bufferSize,
            Int32 nextBatchNumber,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), $"Parameter '{nameof(bufferSize)}' with value {bufferSize} must be greater than 0.");

            eventBuffer = new Queue<TemporalEventBufferItemBase>();
            this.nextBatchNumber = nextBatchNumber;
            this.shardGroupEventPersister = shardGroupEventPersister;
            this.bufferSize = bufferSize;
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public Tuple<Nullable<Guid>, Nullable<Guid>> BufferEvent(TemporalEventBufferItemBase inputEvent, Boolean sourcedFromFirstShardGroup)
        {
            if (sourcedFromFirstShardGroup == true)
            {
                sourceShardGroup1LastBufferedEventId = inputEvent.EventId;
            }
            else
            {
                sourceShardGroup2LastBufferedEventId = inputEvent.EventId;
            }
            eventBuffer.Enqueue(inputEvent);
            if (eventBuffer.Count == bufferSize)
            {
                return Flush();
            }

            return Tuple.Create(sourceShardGroup1LastPersistedEventId, sourceShardGroup2LastPersistedEventId);
        }

        /// <summary>
        /// Persists all events currently buffered.
        /// </summary>
        /// <returns>A tuple containing 2 values: the id of the first shard group event most recently persisted (null if no events have been persisted from the first shard group), and the id of the second shard group event most recently persisted (null if no events have been persisted from the second shard group).</returns>
        public Tuple<Nullable<Guid>, Nullable<Guid>> Flush()
        {
            var eventList = new List<TemporalEventBufferItemBase>();
            while (eventBuffer.Count > 0)
            {
                eventList.Add(eventBuffer.Dequeue());
            }
            logger.Log(this, LogLevel.Information, $"Writing batch {nextBatchNumber} of events from source shard groups to target shard group.");
            Guid beginId = metricLogger.Begin(new EventBatchWriteTime());
            try
            {
                shardGroupEventPersister.PersistEvents(eventList);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventBatchWriteTime());
                throw new Exception($"Failed to write events to the target shard group.", e);
            }
            metricLogger.End(beginId, new EventBatchWriteTime());
            metricLogger.Add(new EventsCopiedFromSourceToTargetShardGroup(), eventList.Count);
            metricLogger.Increment(new EventBatchCopyCompleted());
            logger.Log(this, LogLevel.Information, $"Wrote {eventList.Count} event(s) to target shard group.");
            nextBatchNumber++;
            sourceShardGroup1LastPersistedEventId = sourceShardGroup1LastBufferedEventId;
            sourceShardGroup2LastPersistedEventId = sourceShardGroup2LastBufferedEventId;

            return Tuple.Create(sourceShardGroup1LastPersistedEventId, sourceShardGroup2LastPersistedEventId);
        }
    }
}
