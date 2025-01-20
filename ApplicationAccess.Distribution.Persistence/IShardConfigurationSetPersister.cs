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
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Serialization;

namespace ApplicationAccess.Distribution.Persistence
{
    /// <summary>
    /// Defines methods which read and write instances of <see cref="ShardConfigurationSet{TClientConfiguration}"/> to and from persistent storage.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The implementation of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> embedded within items in the shard configuration set.</typeparam>
    /// <typeparam name="TJsonSerializer">An implementation of <see cref="IDistributedAccessManagerAsyncClientConfigurationJsonSerializer{T}"/> which serializes <typeparamref name="TClientConfiguration"/> instances.</typeparam>
    public interface IShardConfigurationSetPersister<TClientConfiguration, TJsonSerializer>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
        where TJsonSerializer : IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<TClientConfiguration>
    {
        /// <summary>
        /// Writes the specified shard configuration set to persistent storage.
        /// </summary>
        /// <param name="shardConfigurationSet">The shard configuration set to write.</param>
        void Write(ShardConfigurationSet<TClientConfiguration> shardConfigurationSet);

        /// <summary>
        /// Reads and returns shard configuration set from persistent storage
        /// </summary>
        /// <returns>The shard configuration set.</returns>
        ShardConfigurationSet<TClientConfiguration> Read();
    }
}
