/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Distribution.Models;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods to manage a set of <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> implementations corresponding to a <see cref="ShardConfigurationSet{TClientConfiguration}"/> which connect to shards in a distributed AccessManager implementation.  Provides query methods to return clients based on varying parameters, and the ability to refresh the <see cref="ShardConfigurationSet{TClientConfiguration}"/> to update/rebuild the clients.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration stored in the shard configuration.</typeparam>
    public interface IShardClientManager<TClientConfiguration> : IDisposable
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        /// <summary>
        /// Returns a collection of clients which connect to each unique shard managing the specified element and operation type.
        /// </summary>
        /// <param name="dataElement">The type of element to retrieve the clients for.</param>
        /// <param name="operation">The type of operation to retrieve the clients for.</param>
        /// <returns>A collection of clients which connect to each unique shard, and a description of the configuration of the shard each client connects to (e.g. to identify the client in exception messages).</returns>
        IEnumerable<DistributedClientAndShardDescription> GetAllClients(DataElement dataElement, Operation operation);

        /// <summary>
        /// Returns a client which connects to the shard managing the specified element and operation type.
        /// </summary>
        /// <param name="dataElement">The type of the element.</param>
        /// <param name="operation">The type of operation to retrieve the clients for.</param>
        /// <param name="dataElementValue">The value of the element.</param>
        /// <returns>The client and a description of the configuration of the shard the client connects to (e.g. to identify the client in exception messages).</returns>
        DistributedClientAndShardDescription GetClient(DataElement dataElement, Operation operation, String dataElementValue);

        /// <summary>
        /// Returns a collection of clients which connect to shards managing the specified elements.
        /// </summary>
        /// <param name="dataElement">The type of the element.</param>
        /// <param name="operation">The type of operation to retrieve the clients for.</param>
        /// <param name="dataElementValues">The values of the elements.</param>
        /// <returns>A collection of tuples containing: a client and description of the configuration of the shard the client connects to (e.g. to identify the client in exception messages), and the values from parameter <paramref name="dataElementValues"/> that are managed by that shard client.</returns>
        IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> GetClients(DataElement dataElement, Operation operation, IEnumerable<String> dataElementValues);

        /// <summary>
        /// Refreshes the internally stored shard configuration with the specified shard configuration if the configurations differ (if they are the same, no refresh is performed).
        /// </summary>
        /// <param name="shardConfiguration">The shard configuration to update the manager with.</param>
        /// <remarks>This method should not be called concurrently by multiple threads.</remarks>
        void RefreshConfiguration(ShardConfigurationSet<TClientConfiguration> shardConfiguration);
    }
}
