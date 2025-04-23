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
    }
}
