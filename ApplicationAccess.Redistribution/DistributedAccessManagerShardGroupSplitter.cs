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
    /// Moves a subset of events (defined by a range of hash codes) from a source shard group to a target shard group in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedAccessManagerShardGroupSplitter : IDistributedAccessManagerShardGroupSplitter
    {
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupSplitter class.
        /// </summary>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerShardGroupSplitter(IApplicationLogger logger, IMetricLogger metricLogger)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public void CopyEventsToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
            IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
            IDistributedAccessManagerOperationRouter operationRouter,
            IDistributedAccessManagerWriterAdministrator sourceShardGroupWriterAdministrator,
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

            // Get the id of the first event in the source shard group
            Nullable<Guid> lastEventId = null;
            Nullable<Guid> nextEventId = GetInitialEvent(sourceShardGroupEventReader);

            // Copy the events to the target shard group in batches
            logger.Log(this, LogLevel.Information, "Copying subset of events from source shard group to target shard group...");
            Int32 currentBatchNumber = 1;
            logger.Log(this, LogLevel.Information, "Starting initial event batch copy...");
            lastEventId = CopyEventBatchesToTargetShardGroup(sourceShardGroupEventReader, targetShardGroupEventPersister, ref currentBatchNumber, nextEventId.Value, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventBatchSize);
            logger.Log(this, LogLevel.Information, "Completed initial event batch copy.");

            // Hold any incoming operations to the source and target shard groups
            logger.Log(this, LogLevel.Information, "Pausing operations in the source and target shard groups.");
            PauseOperations(operationRouter);

            // Wait until all events are finished processing in the source writer node
            logger.Log(this, LogLevel.Information, "Waiting for source writer node event processing to complete...");
            WaitForSourceWriterNodeEventProcessingCompletion(sourceShardGroupWriterAdministrator, sourceWriterNodeOperationsCompleteCheckRetryAttempts, sourceWriterNodeOperationsCompleteCheckRetryInterval);
            logger.Log(this, LogLevel.Information, "Source writer node event processing to complete.");

            // Flush the event buffer(s) in the source writer node
            logger.Log(this, LogLevel.Information, "Flushing source writer node event buffers...");
            FlushSourceWriterNodeEventBuffers(sourceShardGroupWriterAdministrator);
            logger.Log(this, LogLevel.Information, "Completed flushing source writer node event buffers.");

            // Copy the final event batch to the target shard group
            nextEventId = GetNextEventAfter(sourceShardGroupEventReader, lastEventId.Value);
            if (nextEventId.HasValue == true)
            {
                logger.Log(this, LogLevel.Information, "Starting final event batch copy...");
                CopyEventBatchesToTargetShardGroup(sourceShardGroupEventReader, targetShardGroupEventPersister, ref currentBatchNumber, nextEventId.Value, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventBatchSize);
                logger.Log(this, LogLevel.Information, "Completed final event batch copy.");
            }
            logger.Log(this, LogLevel.Information, "Completed copying subset of events from source shard group to target shard group.");
        }

        /// <inheritdoc/>
        public void DeleteEventsFromSourceShardGroup
        (
            IAccessManagerTemporalEventDeleter sourceShardGroupEventDeleter,
            Int32 hashRangeStart,
            Int32 hashRangeEnd,
            Boolean includeGroupEvents
        )
        {
            logger.Log(this, LogLevel.Information, $"Deleting events from source shard group...");
            Guid beginId = metricLogger.Begin(new EventDeleteTime());
            try
            {
                sourceShardGroupEventDeleter.DeleteEvents(hashRangeStart, hashRangeEnd, includeGroupEvents);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventDeleteTime());
                throw new Exception("Failed to delete events from the source shard group.", e);
            }
            metricLogger.End(beginId, new EventDeleteTime());
            logger.Log(this, LogLevel.Information, $"Completed deleting events from source shard group.");
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
                throw new Exception($"Failed to retrieve initial event id from the source shard group.", e);
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
                throw new Exception("Failed to hold/pause incoming operations to the source and target shard groups.", e);
            }
        }

        /// <summary>
        /// Copies a portion of events from a source to a target shard group in batches
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <param name="sourceShardGroupEventReader">The event reader for the source shard group.</param>
        /// <param name="targetShardGroupEventPersister">The event persister for the target shard group.</param>
        /// <param name="currentBatchNumber">The sequential number of the first batch of events (may be set to greater than 1 if this method is called multiple times).</param>
        /// <param name="firstEventId">The id of the first event in the sequence of events to copy.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will move all group events if set to false.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard group in each batch.</param>
        /// <returns>The id of the last event that was copied.</returns>
        protected Guid CopyEventBatchesToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
            IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
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
                logger.Log(this, LogLevel.Information, $"Copying batch {currentBatchNumber} of events from source shard group to target shard group.");
                IList<TemporalEventBufferItemBase> currentEvents;
                Guid beginId = metricLogger.Begin(new EventBatchReadTime());
                try
                {
                    currentEvents = sourceShardGroupEventReader.GetEvents(nextEventId.Value, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventBatchSize);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new EventBatchReadTime());
                    throw new Exception($"Failed to retrieve event batch from the source shard group beginning with event with id '{nextEventId.Value}'.", e);
                }
                metricLogger.End(beginId, new EventBatchReadTime());
                logger.Log(this, LogLevel.Information, $"Read {currentEvents.Count} events from source shard group.");

                beginId = metricLogger.Begin(new EventBatchWriteTime());
                try
                {
                    targetShardGroupEventPersister.PersistEvents(currentEvents);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new EventBatchWriteTime());
                    throw new Exception($"Failed to write events to the target shard group.", e);
                }
                metricLogger.End(beginId, new EventBatchWriteTime());
                metricLogger.Add(new EventsCopiedFromSourceToTargetShardGroup(), currentEvents.Count);
                metricLogger.Increment(new EventBatchCopyCompleted());
                logger.Log(this, LogLevel.Information, $"Wrote {currentEvents.Count} events to target shard group.");

                // GetNextEventAfter() will return null if no subsequent event exist, which will drop out of while loop on next iteration
                lastEventId = currentEvents[currentEvents.Count - 1].EventId;
                nextEventId = GetNextEventAfter(sourceShardGroupEventReader, lastEventId.Value);
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
        /// Waits until any active event processing in the source shard group writer node is completed.
        /// </summary>
        /// <param name="sourceShardGroupWriterAdministrator">The source shard group writer node client.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking active operations.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        protected void WaitForSourceWriterNodeEventProcessingCompletion
        (
            IDistributedAccessManagerWriterAdministrator sourceShardGroupWriterAdministrator,
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
                    currentEventProcessingCount = sourceShardGroupWriterAdministrator.GetEventProcessingCount();
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to check for active operations in the source shard group event writer node.", e);
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
                throw new Exception($"Active operations in the source shard group event writer node remains at {currentEventProcessingCount} after {originalRetryAttemptsValue} retries with {sourceWriterNodeOperationsCompleteCheckRetryInterval}ms interval.");
            }
        }

        /// <summary>
        /// Flushes the event buffer(s) on the source shard group's writer node.
        /// </summary>
        /// <param name="sourceShardGroupWriterAdministrator">The source shard group writer node client.</param>
        protected void FlushSourceWriterNodeEventBuffers(IDistributedAccessManagerWriterAdministrator sourceShardGroupWriterAdministrator)
        {
            try
            {
                sourceShardGroupWriterAdministrator.FlushEventBuffers();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to flush event buffer(s) in the source shard group event writer node.", e);
            }
        }

        #endregion
    }
}
