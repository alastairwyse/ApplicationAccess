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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Defines methods to move a subset of events (defined by a range of hash codes) from a source shard group to a target shard group in a distributed AccessManager implementation.
    /// </summary>
    public interface IDistributedAccessManagerShardGroupSplitter
    {
        /// <summary>
        /// Copies a subset of events from a source shard group to a target shard group in batches, blocking events from reaching the source shard group during moving of the final batch to ensure consistency and completeness.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TComponent">The type of components in the distributed AccessManager implementation that the shard groups are part of.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <param name="sourceShardGroupEventReader">The event reader for the source shard group.</param>
        /// <param name="targetShardGroupEventPersister">The event persister for the target shard group.</param>
        /// <param name="operationRouter">An operation router which sits in front of the source and target shard groups, and is used to pause incoming events during copying of the final batch.</param>
        /// <param name="sourceShardGroupWriterAdministrator">Used to clear/flush all buffered events from the source shard group to ensure completeness of the copying process.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to copy.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will move all group events if set to false.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard group in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard group, before copying of the final batch of events (event copy will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        /// <remarks>
        ///   <para>Parameter <paramref name="filterGroupEventsByHashRange"/> should be set depending on the type of data element managed by the shard group.  For user shard groups the parameter should be set false, to copy all the groups which may be present in user to group mappings.  For group shard groups it should be set true, to properly filter the groups and group mappings.</para>
        ///   <para>Incoming events to the source shard groups will be paused during the copying process using the router in parameter <paramref name="operationRouter"/>.  The caller must resume event processing after performing any post-copying steps/actions.</para>
        /// </remarks>
        void CopyEventsToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
        (
            IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
            IDistributedAccessManagerOperationRouter operationRouter,
            IDistributedAccessManagerWriterAdministrator sourceShardGroupWriterAdministrator,
            Int32 hashRangeStart,
            Int32 hashRangeEnd,
            Boolean filterGroupEventsByHashRange,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        );

        /// <summary>
        /// Deletes a subset of events from a source shard group.
        /// </summary>
        /// <param name="sourceShardGroupEventDeleter">The event deleter for the source shard group.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="includeGroupEvents">Whether to delete <see cref="GroupEventBufferItem{TGroup}">group events</see>.</param>
        void DeleteEventsFromSourceShardGroup
        (
            IAccessManagerTemporalEventDeleter sourceShardGroupEventDeleter,
            Int32 hashRangeStart,
            Int32 hashRangeEnd,
            Boolean includeGroupEvents
        );
    }
}
