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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Model/container class holding an <see cref="IDistributedAccessManagerAsyncClient{String, String, String, String}"/> instance, and a description of the configuration of the shard that client connects to.
    /// </summary>
    public class DistributedClientAndShardDescription : IEquatable<DistributedClientAndShardDescription>
    {
        /// <summary>The <see cref="IDistributedAccessManagerAsyncClient{String, String, String, String}"/> instance.</summary>
        public IDistributedAccessManagerAsyncClient<String, String, String, String> Client { get; protected set; }

        /// <summary>A description of the configuration of the shard that client connects to, primarily used to identify the client in exception messages.</summary>
        public String ShardConfigurationDescription { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedClientAndShardDescription class.
        /// </summary>
        /// <param name="client">The <see cref="IDistributedAccessManagerAsyncClient{String, String, String, String}"/> instance.</param>
        /// <param name="shardConfigurationDescription">A description of the configuration of the shard that client connects to, primarily used to identify the client in exception messages.</param>
        public DistributedClientAndShardDescription(IDistributedAccessManagerAsyncClient<String, String, String, String> client, String shardConfigurationDescription)
        {
            Client = client;
            ShardConfigurationDescription = shardConfigurationDescription;
        }

        /// <inheritdoc/>
        public Boolean Equals(DistributedClientAndShardDescription other)
        {
            return Object.ReferenceEquals(this.Client, other.Client);
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            return Client.GetHashCode();
        }
    }
}

