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

namespace ApplicationAccess.Redistribution.Kubernetes.Models
{
    /// <summary>
    /// Model/container class holding distributed AccessManager instance configuration for a <see cref="KubernetesDistributedAccessManagerInstanceManager{TPersistentStorageCredentials}"/>.
    /// </summary>
    /// <typeparam name="TPersistentStorageCredentials">An implementation of <see cref="IPersistentStorageLoginCredentials"/> defining the type of login credentials for persistent storage instances.</typeparam>
    public class KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>
        where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
    {
        /// <summary>URL for the distributed router component used for shard group splitting or merging.</summary>
        public Uri DistributedOperationRouterUrl;

        /// <summary>URL for a writer component which is part of a shard group undergoing a split or merge operation (the first of two).</summary>
        public Uri Writer1Url;

        /// <summary>URL for a writer component which is part of a shard group undergoing a split or merge operation (the second of two).</summary>
        public Uri Writer2Url;

        /// <summary>The login credentials for the persistent storage instance used to store shard configuration.</summary>
        public TPersistentStorageCredentials ShardConfigurationPersistentStorageCredentials;

        /// <summary>Configuration of the shard groups managing users in the distributed AccessManager instance.</summary>
        public IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> UserShardGroupConfiguration;

        /// <summary>Configuration of the shard groups managing group to group mappings in the distributed AccessManager instance.</summary>
        public IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> GroupToGroupMappingShardGroupConfiguration;

        /// <summary>Configuration of the shard groups managing groups in the distributed AccessManager instance.</summary>
        public IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> GroupShardGroupConfiguration;

        /// <summary>URL for the distributed coordinator component.</summary>
        public Uri DistributedOperationCoordinatorUrl;
    }
}
