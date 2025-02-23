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
using System.Threading;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Moves a subset of events (defined by a range of hash codes) from a source shard to a target shard in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedAccessManagerShardSplitter
    {
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.DistributedAccessManagerShardSplitter class.
        /// </summary>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerShardSplitter(IApplicationLogger logger, IMetricLogger metricLogger)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Copies a subset of events from a source to a target shard in batches, blocking events from reaching the source shard during moving of the final batch to ensure consistency and completeness.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <param name="sourceShardEventReader">The event reader for the source shard.</param>
        /// <param name="targetShardEventPersister">The event persister for the target shard.</param>
        /// <param name="operationRouter">An operation router which sits in front of the source and target shards, and is used to pause incoming events during copying of the final batch.</param>
        /// <param name="sourceShardWriterAdministrator">Used to clear/flush all buffered events from the source shard to ensure completeness of the copying process.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will move all group events if set to false.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard, before copying of the final batch of events (event copy will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        /// <remarks>
        ///   <para>Parameter <paramref name="filterGroupEventsByHashRange"/> should be set depending on the type of data element managed by the shard.  For user shards the parameter should be set false, to copy all the groups which may be present in user to group mappings.  For group shards it should be set true, to properly filter the groups and group mappings.</para>
        ///   <para>Incoming events to the source shard will be paused during the copying process using the router in parameter <paramref name="operationRouter"/>.  The caller must resume event processing after performing any post-copying steps/actions.</para>
        /// </remarks>
        public void CopyEventsToTargetShard<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardEventReader,
            IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardEventPersister,
            IDistributedAccessManagerOperationRouter operationRouter,
            IDistributedAccessManagerWriterAdministrator sourceShardWriterAdministrator,
            Int32 hashRangeStart,
            Int32 hashRangeEnd,
            Boolean filterGroupEventsByHashRange,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        )
        {
            if (sourceWriterNodeOperationsCompleteCheckRetryAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceWriterNodeOperationsCompleteCheckRetryAttempts), $"Parameter '{nameof(sourceWriterNodeOperationsCompleteCheckRetryAttempts)}' with value {sourceWriterNodeOperationsCompleteCheckRetryAttempts} must be greater than or equal to 0.");
            if (sourceWriterNodeOperationsCompleteCheckRetryInterval < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceWriterNodeOperationsCompleteCheckRetryInterval), $"Parameter '{nameof(sourceWriterNodeOperationsCompleteCheckRetryInterval)}' with value {sourceWriterNodeOperationsCompleteCheckRetryInterval} must be greater than or equal to 0.");
            if (eventBatchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(eventBatchSize), $"Parameter '{nameof(eventBatchSize)}' with value {eventBatchSize} must be greater than 0.");

            // Get the id of the first event in the source shard
            Nullable<Guid> lastEventId = null;
            Nullable<Guid> nextEventId = GetInitialEvent(sourceShardEventReader);

            // Copy the events to the target shard in batches
            logger.Log(this, LogLevel.Information, "Copying subset of events from source shard to target shard...");
            Int32 currentBatchNumber = 1;
            logger.Log(this, LogLevel.Information, "Starting initial event batch copy...");
            lastEventId = CopyEventBatchesToTargetShard(sourceShardEventReader, targetShardEventPersister, ref currentBatchNumber, nextEventId.Value, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventBatchSize);
            logger.Log(this, LogLevel.Information, "Completed initial event batch copy.");

            // Hold any incoming operations to the source and target shards
            logger.Log(this, LogLevel.Information, "Pausing operations in the source and target shards.");
            PauseOperations(operationRouter);

            // Wait until all events are finished processing in the source writer node
            logger.Log(this, LogLevel.Information, "Waiting for source writer node event processing to complete...");
            WaitForSourceWriterNodeEventProcessingCompletion(sourceShardWriterAdministrator, sourceWriterNodeOperationsCompleteCheckRetryAttempts, sourceWriterNodeOperationsCompleteCheckRetryInterval);
            logger.Log(this, LogLevel.Information, "Source writer node event processing to complete.");

            // Flush the event buffer(s) in the source writer node
            logger.Log(this, LogLevel.Information, "Flushing source writer node event buffers...");
            FlushSourceWriterNodeEventBuffers(sourceShardWriterAdministrator);
            logger.Log(this, LogLevel.Information, "Completed flushing source writer node event buffers.");

            // Copy the final event batch to the target shard
            nextEventId = GetNextEventAfter(sourceShardEventReader, lastEventId.Value);
            if (nextEventId.HasValue == true)
            {
                logger.Log(this, LogLevel.Information, "Starting final event batch copy...");
                CopyEventBatchesToTargetShard(sourceShardEventReader, targetShardEventPersister, ref currentBatchNumber, nextEventId.Value, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventBatchSize);
                logger.Log(this, LogLevel.Information, "Completed final event batch copy.");
            }
            logger.Log(this, LogLevel.Information, "Completed copying subset of events from source shard to target shard.");
        }

        /// <summary>
        /// Deletes a subset of events from a source shard.
        /// </summary>
        /// <param name="sourceShardEventDeleter">The event deleter for the source shard.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="includeGroupEvents">Whether to delete <see cref="GroupEventBufferItem{TGroup}">group events</see>.</param>
        public void DeleteEventsFromSourceShard
        (
            IAccessManagerTemporalEventDeleter sourceShardEventDeleter,
            Int32 hashRangeStart,
            Int32 hashRangeEnd,
            Boolean includeGroupEvents
        )
        {
            logger.Log(this, LogLevel.Information, $"Deleting events from source shard...");
            Guid beginId = metricLogger.Begin(new EventDeleteTime());
            try
            {
                sourceShardEventDeleter.DeleteEvents(hashRangeStart, hashRangeEnd, includeGroupEvents);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventDeleteTime());
                throw new Exception("Failed to delete events from the source shard.", e);
            }
            metricLogger.End(beginId, new EventDeleteTime());
            logger.Log(this, LogLevel.Information, $"Completed deleting events from source shard.");
        }

        #region Private/Protected Methods

        /// <summary>
        /// Gets the id of the first event returned from the specified <see cref="IAccessManagerTemporalEventBatchReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="IAccessManagerTemporalEventBatchReader"/> to get the event from.</param>
        /// <returns>The event id.</returns>
        protected Nullable<Guid> GetInitialEvent(IAccessManagerTemporalEventBatchReader reader)
        {
            try
            {
                return reader.GetInitialEvent();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to retrieve initial event id from the source shard.", e);
            }
        }

        /// <summary>
        /// Pauses/holds any incoming operation requests to the specified <see cref="IDistributedAccessManagerOperationRouter"/>.
        /// </summary>
        /// <param name="operationRouter">The <see cref="IDistributedAccessManagerOperationRouter"/> to pause operations on.</param>
        protected void PauseOperations(IDistributedAccessManagerOperationRouter operationRouter)
        {
            try
            {
                operationRouter.PauseOperations();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to hold/pause incoming operations to the source and target shards.", e);
            }
        }

        /// <summary>
        /// Copies a portion of events from a source to a target shard in batches
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <param name="sourceShardEventReader">The event reader for the source shard.</param>
        /// <param name="targetShardEventPersister">The event persister for the target shard.</param>
        /// <param name="currentBatchNumber">The sequential number of the first batch of events (may be set to greater than 1 if this method is called multiple times).</param>
        /// <param name="firstEventId">The id of the first event in the sequence of events to copy.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will move all group events if set to false.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard in each batch.</param>
        /// <returns>The id of the last event that was copied.</returns>
        protected Guid CopyEventBatchesToTargetShard<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardEventReader,
            IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardEventPersister,
            ref Int32 currentBatchNumber, 
            Nullable<Guid> firstEventId, 
            Int32 hashRangeStart,
            Int32 hashRangeEnd,
            Boolean filterGroupEventsByHashRange,
            Int32 eventBatchSize
        )
        {
            Nullable<Guid> nextEventId = firstEventId;
            Nullable<Guid> lastEventId = null;
            while (nextEventId.HasValue)
            {
                logger.Log(this, LogLevel.Information, $"Copying batch {currentBatchNumber} of events from source shard to target shard.");
                IList<TemporalEventBufferItemBase> currentEvents;
                Guid beginId = metricLogger.Begin(new EventBatchReadTime());
                try
                {
                    currentEvents = sourceShardEventReader.GetEvents(nextEventId.Value, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventBatchSize);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new EventBatchReadTime());
                    throw new Exception($"Failed to retrieve event batch from the source shard beginning with event with id '{nextEventId.Value}'.", e);
                }
                metricLogger.End(beginId, new EventBatchReadTime());
                logger.Log(this, LogLevel.Information, $"Read {currentEvents.Count} events from source shard.");

                beginId = metricLogger.Begin(new EventBatchWriteTime());
                try
                {
                    targetShardEventPersister.PersistEvents(currentEvents);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new EventBatchWriteTime());
                    throw new Exception($"Failed to write events to the target shard.", e);
                }
                metricLogger.End(beginId, new EventBatchWriteTime());
                metricLogger.Add(new EventsCopiedFromSourceToTargetShard(), currentEvents.Count);
                metricLogger.Increment(new EventBatchCopyCompleted());
                logger.Log(this, LogLevel.Information, $"Wrote {currentEvents.Count} events to target shard.");

                // GetNextEventAfter() will return null if no subsequent event exist, which will drop out of while loop on next iteration
                lastEventId = currentEvents[currentEvents.Count - 1].EventId;
                nextEventId = GetNextEventAfter(sourceShardEventReader, lastEventId.Value);
                currentBatchNumber++;
            }

            return lastEventId.Value;
        }

        /// <summary>
        /// Retrieves the id of the next event after the specified event. 
        /// </summary>
        /// <param name="reader">The <see cref="IAccessManagerTemporalEventBatchReader"/> to get the event from.</param>
        /// <param name="inputEventId">The id of the preceding event.</param>
        /// <returns>The next event, or null of the specified event is the latest.</returns>
        protected Nullable<Guid> GetNextEventAfter(IAccessManagerTemporalEventBatchReader reader, Guid inputEventId)
        {
            try
            {
                return reader.GetNextEventAfter(inputEventId);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to retrieve next event after event with id '{inputEventId.ToString()}'.", e);
            }
        }

        /// <summary>
        /// Waits until any active event processing in the source shard writer node is completed.
        /// </summary>
        /// <param name="sourceShardWriterAdministrator">The source shard writer node client.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking active operations.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        protected void WaitForSourceWriterNodeEventProcessingCompletion
        (
            IDistributedAccessManagerWriterAdministrator sourceShardWriterAdministrator,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        )
        {
            Int32 originalRetryAttemptsValue = sourceWriterNodeOperationsCompleteCheckRetryAttempts;
            Int32 currentEventProcessingCount = -1;
            while (sourceWriterNodeOperationsCompleteCheckRetryAttempts >= 0)
            {
                try
                {
                    currentEventProcessingCount = sourceShardWriterAdministrator.GetEventProcessingCount();
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to check for active operations in the source shard event writer node.", e);
                }
                metricLogger.Set(new WriterNodeEventProcessingCount(), currentEventProcessingCount); 
                if (currentEventProcessingCount == 0)
                {
                    break;
                }
                else
                {
                    if (sourceWriterNodeOperationsCompleteCheckRetryInterval > 0)
                    {
                        Thread.Sleep(sourceWriterNodeOperationsCompleteCheckRetryInterval);
                    }
                    sourceWriterNodeOperationsCompleteCheckRetryAttempts--;
                    if (sourceWriterNodeOperationsCompleteCheckRetryAttempts >= 0)
                    {
                        metricLogger.Increment(new EventProcessingCountCheckRetried());
                    }
                }
            }
            if (currentEventProcessingCount != 0)
            {
                throw new Exception($"Active operations in the source shard event writer node remains at {currentEventProcessingCount} after {originalRetryAttemptsValue} retries with {sourceWriterNodeOperationsCompleteCheckRetryInterval}ms interval.");
            }
        }

        /// <summary>
        /// Flushes the event buffer(s) on the source shard's writer node.
        /// </summary>
        /// <param name="sourceShardWriterAdministrator">The source shard writer node client.</param>
        protected void FlushSourceWriterNodeEventBuffers(IDistributedAccessManagerWriterAdministrator sourceShardWriterAdministrator)
        {
            try
            {
                sourceShardWriterAdministrator.FlushEventBuffers();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to flush event buffer(s) in the source shard event writer node.", e);
            }
        }

        #endregion
    }
}
