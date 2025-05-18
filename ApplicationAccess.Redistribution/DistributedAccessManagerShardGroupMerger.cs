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
using System.Text;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Merges events from two source source shard groups to a target shard group in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedAccessManagerShardGroupMerger : DistributedAccessManagerShardGroupMergerSplitterBase
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupMerger class.
        /// </summary>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerShardGroupMerger(IApplicationLogger logger, IMetricLogger metricLogger)
            : base(logger, metricLogger)
        {
        }

        public void MergeEventsToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardGroup1EventReader,
            IAccessManagerTemporalEventBatchReader sourceShardGroup2EventReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
            IDistributedAccessManagerOperationRouter operationRouter,
            IDistributedAccessManagerWriterAdministrator sourceShardGroup1WriterAdministrator,
            IDistributedAccessManagerWriterAdministrator sourceShardGroup2WriterAdministrator,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        )
        {
            // Don't need to use a heap since there's just 2 sources
            //   But will need queue or linked list to buffer events from the dbs
            // Algo should be
            //   Take the lowest from one side
            //   If one side buffer is empty, attempt to re-populated buffer from DB
            //   If no events come from the DB, then write all from the other side (incl trying to take from DB once all are written from the buffer)
            //   In this case GetEvents() should always return events since we won't be filtering by hash code

            // After this method will need to 
            //   Shutdown source shard group
            //   Rename the DBs appropriately
            //   Start source shard group
            //   Resume operations
        }

        #region Private/Protected Methods

        protected Tuple<Nullable<Guid>, Nullable<Guid>> MergeEventBatchesToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardGroup1EventReader,
            IAccessManagerTemporalEventBatchReader sourceShardGroup2EventReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
            ref Int32 currentBatchNumber,
            Guid sourceShardGroup1FirstEventId,
            Guid sourceShardGroup2FirstEventId,
            Int32 eventBatchSize
        )
        {
            logger.Log(this, LogLevel.Information, $"Copying batch {currentBatchNumber} of events from source shard groups to target shard group.");

            // Retrieve the initial batches of events
            var sourceShardGroup1EventQueue = new Queue<TemporalEventBufferItemBase>();
            var sourceShardGroup2EventQueue = new Queue<TemporalEventBufferItemBase>();
            IList<TemporalEventBufferItemBase> sourceShardGroup1CurrentEvents;
            IList<TemporalEventBufferItemBase> sourceShardGroup2CurrentEvents;
            Nullable<Guid> sourceShardGroup1LastEventId = null;
            Nullable<Guid> sourceShardGroup2LastEventId = null;
            Guid beginId = metricLogger.Begin(new EventBatchReadTime());
            try
            {
                sourceShardGroup1CurrentEvents = sourceShardGroup1EventReader.GetEvents(sourceShardGroup1FirstEventId, Int32.MinValue, Int32.MaxValue, false, eventBatchSize);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventBatchReadTime());
                throw new Exception($"Failed to retrieve event batch from first source shard group beginning with event with id '{sourceShardGroup1FirstEventId}'.", e);
            }
            metricLogger.End(beginId, new EventBatchReadTime());
            beginId = metricLogger.Begin(new EventBatchReadTime());
            try
            {
                sourceShardGroup2CurrentEvents = sourceShardGroup2EventReader.GetEvents(sourceShardGroup2FirstEventId, Int32.MinValue, Int32.MaxValue, false, eventBatchSize);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventBatchReadTime());
                throw new Exception($"Failed to retrieve event batch from second source shard group beginning with event with id '{sourceShardGroup2FirstEventId}'.", e);
            }
            metricLogger.End(beginId, new EventBatchReadTime());
            foreach (TemporalEventBufferItemBase currentEvent in sourceShardGroup1CurrentEvents)
            {
                sourceShardGroup1EventQueue.Enqueue(currentEvent);
            }
            foreach (TemporalEventBufferItemBase currentEvent in sourceShardGroup2CurrentEvents)
            {
                sourceShardGroup2EventQueue.Enqueue(currentEvent);
            }
            sourceShardGroup1LastEventId = sourceShardGroup1CurrentEvents[sourceShardGroup1CurrentEvents.Count - 1].EventId;
            sourceShardGroup2LastEventId = sourceShardGroup2CurrentEvents[sourceShardGroup2CurrentEvents.Count - 1].EventId;

            // Write events to the target in batches
            while (true)
            {
                if (sourceShardGroup1EventQueue.Count == 0)
                {

                }
            }

        }

        /// <summary>
        /// Reads a batch of events from a source shard group and puts the events into a queue. 
        /// </summary>
        /// <param name="initialEventId">The id of the first/earliest event in the batch.</param>
        /// <param name="sourceShardGroupEventReader">The reader to use to read the events.</param>
        /// <param name="destinationEventQueue">The queue to put the events in.</param>
        /// <param name="eventCount">The number of events to read and put into the queue.</param>
        /// <param name="readerIsFirstEventReader">Whether the reader in parameter <paramref name="sourceShardGroupEventReader"/> is the reader for the first shard group to merge (assumed to be the second shard group reader if set to false).</param>
        /// <returns>The id of the last event put in the queue, or null if no events were read.</returns>
        protected Nullable<Guid> ReadSourceShardGroupEventsIntoQueue
        (
            Guid initialEventId, 
            IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader, 
            Queue<TemporalEventBufferItemBase> destinationEventQueue,
            Int32 eventCount, 
            Boolean readerIsFirstEventReader
        )
        {
            if (destinationEventQueue.Count != 0)
                throw new ArgumentException($"The queue in parameter '{nameof(destinationEventQueue)}' is not empty.", nameof(destinationEventQueue));

            IList<TemporalEventBufferItemBase> eventList;
            String shardGroupReaderName = "first";
            if (readerIsFirstEventReader == false)
            {
                shardGroupReaderName = "second";
            }
            Guid beginId = metricLogger.Begin(new EventBatchReadTime());
            try
            {
                eventList = sourceShardGroupEventReader.GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventBatchReadTime());
                throw new Exception($"Failed to retrieve event batch from {shardGroupReaderName} source shard group beginning with event with id '{initialEventId}'.", e);
            }
            metricLogger.End(beginId, new EventBatchReadTime());
            foreach (TemporalEventBufferItemBase currentEvent in eventList)
            {
                destinationEventQueue.Enqueue(currentEvent);
            }

            if (eventList.Count == 0)
            {
                return null;
            }
            else
            {
                return eventList[eventList.Count - 1].EventId;
            }
        }

        #endregion
    }
}
