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
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Persistence;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Defines methods to merges events from two source source shard groups to a target shard group in a distributed AccessManager implementation.
    /// </summary>
    public interface IDistributedAccessManagerShardGroupMerger
    {
        /// <summary>
        /// Merges all events from two source shard groups into a target shard group in batches, blocking events from reaching the source shard groups during merging of the final batch to ensure consistency and completeness.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TComponent">The type of components in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <param name="sourceShardGroup1EventReader">The event reader for the first source shard group.</param>
        /// <param name="sourceShardGroup2EventReader">The event reader for the second source shard group.</param>
        /// <param name="targetShardGroupEventPersister">The event persister for the target shard group.</param>
        /// <param name="operationRouter">An operation router which sits in front of the shard groups, and is used to pause incoming events during copying of the final batch.</param>
        /// <param name="sourceShardGroup1WriterAdministrator">Used to clear/flush all buffered events from the first source shard group to ensure completeness of the copying process.</param>
        /// <param name="sourceShardGroup2WriterAdministrator">Used to clear/flush all buffered events from the second source shard group to ensure completeness of the copying process.</param>
        /// <param name="eventBatchSize">The number of events which should be read from and persisted to the shard group in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard groups, before merging of the final batch of events (event merge will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        /// <remarks>Incoming events to the source shard groups will be paused during the merging process using the router in parameter <paramref name="operationRouter"/>.  The caller must resume event processing after performing any post-merging steps/actions.</remarks>
        void MergeEventsToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
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
        );
    }
}
