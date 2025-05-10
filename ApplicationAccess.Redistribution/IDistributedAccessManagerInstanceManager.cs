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
using System.Net;
using System.Threading.Tasks;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Models;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Defines methods to manage a running instance of a distributed AccessManager implementation.
    /// </summary>
    /// <typeparam name="TPersistentStorageCredentials">An implementation of <see cref="IPersistentStorageLoginCredentials"/> defining the type of login credentials for persistent storage instances.</typeparam>
    public interface IDistributedAccessManagerInstanceManager<TPersistentStorageCredentials>
        where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
    {
        /// <summary>
        /// Creates a new distributed AccessManager instance.
        /// </summary>
        /// <param name="userShardGroupConfiguration">The configuration of the user shard groups to create.</param>
        /// <param name="groupToGroupMappingShardGroupConfiguration">The configuration of the group to group mapping shard groups to create.</param>
        /// <param name="groupShardGroupConfiguration">The configuration of the group shard groups to create.</param>
        /// <remarks>If the <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}.PersistentStorageCredentials"/> properties in each of the <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/> parameters is set to null, persistent storage instances will be created for those shard groups.  If a value is provided, the provided credentials will be used to connect the shard group to persistent storage.</remarks> 
        Task CreateDistributedAccessManagerInstanceAsync
        (
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> userShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupToGroupMappingShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupShardGroupConfiguration
        );

        /// <summary>
        /// Splits a shard group in the distributed AccessManager instance, by moving elements whose hash codes fall within a specified range to a new shard group.
        /// </summary>
        /// <param name="dataElement">The data element of the shard group to split..</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group to split.</param>
        /// <param name="splitHashRangeStart">The first (inclusive) in the range of hash codes to move to the new shard group.</param>
        /// <param name="splitHashRangeEnd">The last (inclusive) in the range of hash codes to move to the new shard group.</param>
        /// <param name="sourceShardGroupEventReaderCreationFunction">A function used to create a reader used to read events from the source shard group persistent storage instance.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerTemporalEventBatchReader"/> instance.</param>
        /// <param name="targetShardGroupEventPersisterCreationFunction">A function used to create a persister used to write events to the target shard group persistent storage instance.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerIdempotentTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance.</param>
        /// <param name="sourceShardGroupEventDeleterCreationFunction">A function used to create a deleter used to delete events from the source shard group persistent storage instance.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerTemporalEventDeleter"/> instance.</param>
        /// <param name="operationRouterCreationFunction">A function used to create a client used control the router which directs operations between the source and target shard groups.  Accepts a <see cref="Uri"/> and returns an <see cref="IDistributedAccessManagerOperationRouter"/> instance.</param>
        /// <param name="sourceShardGroupWriterAdministratorCreationFunction">A function used to create a client used control the writer node in the target shard group.  Accepts a <see cref="Uri"/> and returns an <see cref="IDistributedAccessManagerWriterAdministrator"/> instance.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard group in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard group, before copying of the final batch of events (event copy will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        Task SplitShardGroupAsync
        (
            DataElement dataElement,
            Int32 hashRangeStart,
            Int32 splitHashRangeStart,
            Int32 splitHashRangeEnd,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBatchReader> sourceShardGroupEventReaderCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> targetShardGroupEventPersisterCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventDeleter> sourceShardGroupEventDeleterCreationFunction,
            Func<Uri, IDistributedAccessManagerOperationRouter> operationRouterCreationFunction,
            Func<Uri, IDistributedAccessManagerWriterAdministrator> sourceShardGroupWriterAdministratorCreationFunction,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        );
    }
}
