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
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution.Models;
using ApplicationAccess.Redistribution.Kubernetes.Models;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// Defines methods to manage an instance of a distributed AccessManager implementation hosted in Kubernetes, and using Microsoft SqlServer for persistent storage.
    /// </summary>
    public interface IKubernetesDistributedInstanceManager
    {
        /// <summary>
        /// URL for the distributed operation router component used for shard group splitting.
        /// </summary>
        Uri DistributedOperationRouterUrl { set; }

        /// <summary>
        /// URL for a first writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        Uri Writer1Url { set; }

        /// <summary>
        /// URL for a second writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        Uri Writer2Url { set; }

        /// <summary>
        /// URL for the distributed operation coordinator component.
        /// </summary>
        Uri DistributedOperationCoordinatorUrl { set; }

        /// <summary>
        /// Configuration for the distributed AccessManager instance.
        /// </summary>
        KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> InstanceConfiguration { get; }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access the distributed router component used for shard group splitting, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the load balancer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        Task<IPAddress> CreateDistributedOperationRouterLoadBalancerServiceAsync(UInt16 port);

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access a first writer component which is part of a shard group undergoing a split or merge operation, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the writer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        Task<IPAddress> CreateWriter1LoadBalancerServiceAsync(UInt16 port);

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access a second writer component which is part of a shard group undergoing a split or merge operation, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the writer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        Task<IPAddress> CreateWriter2LoadBalancerServiceAsync(UInt16 port);

        /// <summary>
        /// Creates a new distributed AccessManager instance.
        /// </summary>
        /// <param name="userShardGroupConfiguration">The configuration of the user shard groups to create.</param>
        /// <param name="groupToGroupMappingShardGroupConfiguration">The configuration of the group to group mapping shard groups to create.</param>
        /// <param name="groupShardGroupConfiguration">The configuration of the group shard groups to create.</param>
        /// <remarks>If the <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}.PersistentStorageCredentials"/> properties in each of the <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/> parameters is set to null, persistent storage instances will be created for those shard groups.  If a value is provided, the provided credentials will be used to connect the shard group to persistent storage.</remarks> 
        Task CreateDistributedAccessManagerInstanceAsync
        (
            IList<ShardGroupConfiguration<SqlServerLoginCredentials>> userShardGroupConfiguration,
            IList<ShardGroupConfiguration<SqlServerLoginCredentials>> groupToGroupMappingShardGroupConfiguration,
            IList<ShardGroupConfiguration<SqlServerLoginCredentials>> groupShardGroupConfiguration
        );

        /// <summary>
        /// Deletes the distributed AccessManager instance.
        /// </summary>
        /// <param name="deletePersistentStorageInstances">Whether to delete the persistent storage instances in the access manager instance.</param>
        Task DeleteDistributedAccessManagerInstanceAsync(Boolean deletePersistentStorageInstances);

        /// <summary>
        /// Splits a shard group in the distributed AccessManager instance, by moving elements whose hash codes fall within a specified range to a new shard group.
        /// </summary>
        /// <param name="dataElement">The data element of the shard group to split.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group to split.</param>
        /// <param name="splitHashRangeStart">The first (inclusive) in the range of hash codes to move to the new shard group.</param>
        /// <param name="splitHashRangeEnd">The last (inclusive) in the range of hash codes to move to the new shard group.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard group in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard group, before copying of the final batch of events (event copy will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        Task SplitShardGroupAsync
        (
            DataElement dataElement,
            Int32 hashRangeStart,
            Int32 splitHashRangeStart,
            Int32 splitHashRangeEnd,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        );

        /// <summary>
        /// Merges a two shard groups with consecutive hash code ranges in the distributed AccessManager instance.
        /// </summary>
        /// <param name="dataElement">The data element of the shard groups to merge.</param>
        /// <param name="sourceShardGroup1HashRangeStart">The first (inclusive) in the range of hash codes managed by the first shard group to merge.</param>
        /// <param name="sourceShardGroup2HashRangeStart">The first (inclusive) in the range of hash codes managed by the second shard group to merge.</param>
        /// <param name="sourceShardGroup2HashRangeEnd">The last (inclusive) in the range of hash codes managed by the second shard group to merge.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard groups in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard groups, before merging of the final batch of events (event merge will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        Task MergeShardGroupsAsync
        (
            DataElement dataElement,
            Int32 sourceShardGroup1HashRangeStart,
            Int32 sourceShardGroup2HashRangeStart,
            Int32 sourceShardGroup2HashRangeEnd,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        );
    }
}
