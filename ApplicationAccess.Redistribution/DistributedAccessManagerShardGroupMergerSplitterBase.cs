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
using System.Threading;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Base for classes which merge or split events within shard groups in a distributed AccessManager implementation.
    /// </summary>
    public abstract class DistributedAccessManagerShardGroupMergerSplitterBase
    {
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupMergerSplitterBase class.
        /// </summary>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerShardGroupMergerSplitterBase(IApplicationLogger logger, IMetricLogger metricLogger)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Gets the id of the first event returned from the specified <see cref="IAccessManagerTemporalEventBatchReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="IAccessManagerTemporalEventBatchReader"/> to get the event from.</param>
        /// <returns>The event id.</returns>
        protected Guid GetInitialEvent(IAccessManagerTemporalEventBatchReader reader)
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
                    throw new Exception("Failed to check for active operations in source shard group event writer node.", e);
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
                throw new Exception($"Active operations in source shard group event writer node remains at {currentEventProcessingCount} after {originalRetryAttemptsValue} retries with {sourceWriterNodeOperationsCompleteCheckRetryInterval}ms interval.");
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
