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
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationAccess.Redistribution.Models;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Merges events from two source source shard groups to a target shard group in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedAccessManagerShardGroupMerger : DistributedAccessManagerShardGroupMergerSplitterBase, IDistributedAccessManagerShardGroupMerger
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

        /// <inheritdoc/>
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
            ThrowExceptionIfSourceWriterNodeOperationsCompleteCheckRetryAttemptsParameterLessThan0(sourceWriterNodeOperationsCompleteCheckRetryAttempts);
            ThrowExceptionIfSourceWriterNodeOperationsCompleteCheckRetryIntervalParameterLessThan0(sourceWriterNodeOperationsCompleteCheckRetryInterval);
            ThrowExceptionIfEventBatchSizeParameterLessThan1(eventBatchSize);

            // Get the ids of the first events in the source shard groups
            Guid sourceShardGroup1FirstEventId = GetInitialEvent(sourceShardGroup1EventReader);
            Guid sourceShardGroup2FirstEventId = GetInitialEvent(sourceShardGroup2EventReader);

            // Merge the events in batches
            logger.Log(this, LogLevel.Information, "Merging events from source shard groups to target shard group...");
            Int32 currentBatchNumber = 1;
            logger.Log(this, LogLevel.Information, "Starting initial event batch merge...");
            var eventFilter = new PrimaryElementEventDuplicateFilter<TUser, TGroup>(false, logger, metricLogger);
            Tuple<Nullable<Guid>, Nullable<Guid>> lastPersistedEventIds = MergeEventBatchesToTargetShardGroup
            (
                sourceShardGroup1EventReader,
                sourceShardGroup2EventReader, 
                targetShardGroupEventPersister,
                eventFilter,
                ref currentBatchNumber,
                sourceShardGroup1FirstEventId,
                sourceShardGroup2FirstEventId,
                eventBatchSize, 
                NoEventsReadDuringMergeAction.StopMerging
            );
            logger.Log(this, LogLevel.Information, "Completed initial event batch merge.");

            // Hold any incoming operations to the source and target shard groups
            logger.Log(this, LogLevel.Information, "Pausing operations in the source and target shard groups.");
            PauseOperations(operationRouter);

            // Wait until all events are finished processing in the source writer node
            logger.Log(this, LogLevel.Information, "Waiting for source writer nodes event processing to complete...");
            WaitForSourceWriterNodeEventProcessingCompletion(sourceShardGroup1WriterAdministrator, sourceWriterNodeOperationsCompleteCheckRetryAttempts, sourceWriterNodeOperationsCompleteCheckRetryInterval);
            WaitForSourceWriterNodeEventProcessingCompletion(sourceShardGroup2WriterAdministrator, sourceWriterNodeOperationsCompleteCheckRetryAttempts, sourceWriterNodeOperationsCompleteCheckRetryInterval);
            logger.Log(this, LogLevel.Information, "Source writer nodes event processing to complete.");

            // Flush the event buffer(s) in the source writer nodes
            //   TODO: Make these methods async and run in parallel for faster merging
            logger.Log(this, LogLevel.Information, "Flushing source writer nodes event buffers...");
            FlushSourceWriterNodeEventBuffers(sourceShardGroup1WriterAdministrator);
            FlushSourceWriterNodeEventBuffers(sourceShardGroup2WriterAdministrator);
            logger.Log(this, LogLevel.Information, "Completed flushing source writer nodes event buffers.");

            // Merge the final event batches to the target shard group
            Nullable<Guid> sourceShardGroup1NextEventId, sourceShardGroup2NextEventId;
            if (lastPersistedEventIds.Item1.HasValue)
            {
                sourceShardGroup1NextEventId = GetNextEventAfter(sourceShardGroup1EventReader, lastPersistedEventIds.Item1.Value);
            }
            else
            {
                sourceShardGroup1NextEventId = sourceShardGroup1FirstEventId;
            }
            if (lastPersistedEventIds.Item2.HasValue)
            {
                sourceShardGroup2NextEventId = GetNextEventAfter(sourceShardGroup2EventReader, lastPersistedEventIds.Item2.Value);
            }
            else
            {
                sourceShardGroup2NextEventId = sourceShardGroup2FirstEventId;
            }
            if (sourceShardGroup1NextEventId.HasValue || sourceShardGroup2NextEventId.HasValue)
            {
                logger.Log(this, LogLevel.Information, "Starting final event batch merge...");
                lastPersistedEventIds = MergeEventBatchesToTargetShardGroup
                (
                    sourceShardGroup1EventReader,
                    sourceShardGroup2EventReader,
                    targetShardGroupEventPersister,
                    eventFilter,
                    ref currentBatchNumber,
                    sourceShardGroup1NextEventId,
                    sourceShardGroup2NextEventId,
                    eventBatchSize,
                    NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
                );
                logger.Log(this, LogLevel.Information, "Completed final event batch merge.");
            }
            logger.Log(this, LogLevel.Information, "Completed merging events from source shard groups to target shard group.");
        }

        #region Private/Protected Methods

        /// <summary>
        /// Merges events from two source source shard groups to a target shard group in batches.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TComponent">The type of components in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <param name="sourceShardGroup1EventReader">The event reader for the first source shard group.</param>
        /// <param name="sourceShardGroup2EventReader">The event reader for the second source shard group.</param>
        /// <param name="targetShardGroupEventPersister">The event persister for the target shard group.</param>
        /// <param name="eventFilter">Duplicate filter for primary element events.</param>
        /// <param name="nextBatchNumber">The sequential number of the next batch of events to merge (may be set to greater than 1 if this method is called multiple times).</param>
        /// <param name="sourceShardGroup1FirstEventId">The id of the first event from the first source shard group in the sequence of events to merge.</param>
        /// <param name="sourceShardGroup2FirstEventId">The id of the first event from the second source shard group in the sequence of events to merge.</param>
        /// <param name="eventBatchSize">The number of events to read or persist in each batch.</param>
        /// <param name="noEventsReadDuringMergeAction">The action to take when 0 events are read from one of the source shard group readers/</param>
        /// <returns>A tuple containing: the id of the first shard group event most recently processed (i.e. either filtered or persisted) or null if no events have been processed from the first shard group, and the id of the second shard group event most recently processed or null if no events have been processed from the second shard group.</returns>
        /// <remarks>Parameter <paramref name="eventFilter"/> is passed to allow the state of the filter to be preserved between calls to the method (i.e. passed instead of creating an instance within the method).</remarks>
        protected Tuple<Nullable<Guid>, Nullable<Guid>> MergeEventBatchesToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardGroup1EventReader,
            IAccessManagerTemporalEventBatchReader sourceShardGroup2EventReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
            PrimaryElementEventDuplicateFilter<TUser, TGroup> eventFilter,
            ref Int32 nextBatchNumber,
            Nullable<Guid> sourceShardGroup1FirstEventId,
            Nullable<Guid> sourceShardGroup2FirstEventId,
            Int32 eventBatchSize, 
            NoEventsReadDuringMergeAction noEventsReadDuringMergeAction
        )
        {
            if (sourceShardGroup1FirstEventId.HasValue == false && sourceShardGroup2FirstEventId.HasValue == false)
                throw new ArgumentException($"One of parameters '{nameof(sourceShardGroup1FirstEventId)}' or '{nameof(sourceShardGroup2FirstEventId)}' must contain a value.");

            // Setup retrieve the initial batches of events
            var eventBuffer = new EventPersisterBuffer<TUser, TGroup, TComponent, TAccess>(targetShardGroupEventPersister, eventBatchSize, nextBatchNumber, logger, metricLogger);
            eventFilter.EventPersisterBuffer = eventBuffer;
            var sourceShardGroup1EventQueue = new Queue<TemporalEventBufferItemBase>();
            var sourceShardGroup2EventQueue = new Queue<TemporalEventBufferItemBase>();
            Nullable<Guid> sourceShardGroup1LastBufferedEventId = null, sourceShardGroup2LastBufferedEventId = null;

            if (sourceShardGroup1FirstEventId.HasValue)
            {
                ReadSourceShardGroupEventsIntoQueue(sourceShardGroup1FirstEventId.Value, sourceShardGroup1EventReader, sourceShardGroup1EventQueue, eventBatchSize, true);
            }
            if (sourceShardGroup2FirstEventId.HasValue)
            {
                ReadSourceShardGroupEventsIntoQueue(sourceShardGroup2FirstEventId.Value, sourceShardGroup2EventReader, sourceShardGroup2EventQueue, eventBatchSize, false);
            }

            // Write events to the target in batches
            while (true)
            {
                if (sourceShardGroup1EventQueue.Count == 0)
                {
                    if (noEventsReadDuringMergeAction == NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource)
                    {
                        sourceShardGroup2LastBufferedEventId = BufferAllRemainingEvents<TUser, TGroup, TComponent, TAccess>
                        (
                            sourceShardGroup2EventReader,
                            sourceShardGroup2EventQueue,
                            eventFilter,
                            eventBatchSize,
                            false
                        );
                    }
                    eventBuffer.Flush();
                    break;
                }
                else if (sourceShardGroup2EventQueue.Count == 0)
                {
                    if (noEventsReadDuringMergeAction == NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource)
                    {
                        sourceShardGroup1LastBufferedEventId = BufferAllRemainingEvents<TUser, TGroup, TComponent, TAccess>
                        (
                            sourceShardGroup1EventReader,
                            sourceShardGroup1EventQueue,
                            eventFilter,
                            eventBatchSize,
                            true
                        );
                    }
                    eventBuffer.Flush();
                    break;
                }
                else
                {
                    while (sourceShardGroup1EventQueue.Count > 0 && sourceShardGroup2EventQueue.Count > 0)
                    {
                        if (sourceShardGroup1EventQueue.Peek().OccurredTime <= sourceShardGroup2EventQueue.Peek().OccurredTime)
                        {
                            sourceShardGroup1LastBufferedEventId = sourceShardGroup1EventQueue.Peek().EventId;
                            eventFilter.BufferEvent(sourceShardGroup1EventQueue.Dequeue(), true);
                        }
                        else
                        {
                            sourceShardGroup2LastBufferedEventId = sourceShardGroup2EventQueue.Peek().EventId;
                            eventFilter.BufferEvent(sourceShardGroup2EventQueue.Dequeue(), false);
                        }
                    }
                    if (sourceShardGroup1EventQueue.Count == 0)
                    {
                        Nullable<Guid> nextEventId = GetNextEventIdAfter(sourceShardGroup1LastBufferedEventId.Value, sourceShardGroup1EventReader);
                        if (nextEventId.HasValue)
                        {
                            ReadSourceShardGroupEventsIntoQueue(nextEventId.Value, sourceShardGroup1EventReader, sourceShardGroup1EventQueue, eventBatchSize, true);
                        }
                    }
                    else
                    {
                        Nullable<Guid> nextEventId = GetNextEventIdAfter(sourceShardGroup2LastBufferedEventId.Value, sourceShardGroup2EventReader);
                        if (nextEventId.HasValue)
                        {
                            ReadSourceShardGroupEventsIntoQueue(nextEventId.Value, sourceShardGroup2EventReader, sourceShardGroup2EventQueue, eventBatchSize, false);
                        }
                    }
                }
            }
            nextBatchNumber = eventBuffer.NextBatchNumber;

            return Tuple.Create(sourceShardGroup1LastBufferedEventId, sourceShardGroup2LastBufferedEventId);
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
            logger.Log(this, LogLevel.Information, $"Read {eventList.Count} event(s) from {shardGroupReaderName} source shard group.");
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

        /// <summary>
        /// Buffers all remaining events in the specified event queue and reader.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <param name="sourceShardGroupEventReader">The event reader for the source shard group.</param>
        /// <param name="sourceShardGroupEventQueue">Queue containing the next events to persist.</param>
        /// <param name="targetShardGroupEventPersisterBuffer">The event persister buffer for the target shard group.</param>
        /// <param name="eventBatchSize">The number of events to read or persist in each batch.</param>
        /// <param name="sourceShardGroupIsFirst">Whether the events in parameters <paramref name="sourceShardGroupEventReader"/> and <paramref name="sourceShardGroupEventQueue"/> are from the first shard group being merged.</param>
        /// <returns>The id of the last event buffered, or null if no events were buffered.</returns>
        protected Nullable<Guid> BufferAllRemainingEvents<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader, 
            Queue<TemporalEventBufferItemBase> sourceShardGroupEventQueue, 
            IEventPersisterBuffer targetShardGroupEventPersisterBuffer,
            Int32 eventBatchSize,
            Boolean sourceShardGroupIsFirst
        )
        {
            if (sourceShardGroupEventQueue.Count == 0)
                throw new ArgumentException($"The queue in parameter '{nameof(sourceShardGroupEventQueue)}' cannot be empty.", nameof(sourceShardGroupEventQueue));

            Nullable<Guid> lastBufferedEventId = null;
            while (sourceShardGroupEventQueue.Count > 0)
            {
                TemporalEventBufferItemBase nextEvent = sourceShardGroupEventQueue.Dequeue();
                lastBufferedEventId = nextEvent.EventId;
                targetShardGroupEventPersisterBuffer.BufferEvent(nextEvent, sourceShardGroupIsFirst);
            }
            Nullable<Guid> nextEventId = GetNextEventIdAfter(lastBufferedEventId.Value, sourceShardGroupEventReader);
            while (nextEventId.HasValue)
            {
                lastBufferedEventId = ReadSourceShardGroupEventsIntoQueue(nextEventId.Value, sourceShardGroupEventReader, sourceShardGroupEventQueue, eventBatchSize, sourceShardGroupIsFirst);
                while (sourceShardGroupEventQueue.Count > 0)
                {
                    targetShardGroupEventPersisterBuffer.BufferEvent(sourceShardGroupEventQueue.Dequeue(), sourceShardGroupIsFirst);
                }
                nextEventId = GetNextEventIdAfter(lastBufferedEventId.Value, sourceShardGroupEventReader);
            }

            return lastBufferedEventId;
        }

        /// <summary>
        /// Retrieves the id of the next event after the specified event. 
        /// </summary>
        /// <param name="inputEventId">The id of the preceding event.</param>
        /// <param name="sourceShardGroupEventReader">The event reader to retrieve the id from.</param>
        /// <returns>The next event, or null of the specified event is the latest.</returns>
        protected Nullable<Guid> GetNextEventIdAfter(Guid inputEventId, IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader)
        {
            try
            {
                return sourceShardGroupEventReader.GetNextEventAfter(inputEventId);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to read event following event with id '{inputEventId}' from event reader.", e);
            }
        }

        #endregion
    }
}
