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

namespace ApplicationAccess.Distribution.Models
{
    /// <summary>
    /// Model/container class holding configuration information for a single shard in a distributed AccessManager implementation.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration stored in the shard configuration.</typeparam>
    public class ShardConfiguration<TClientConfiguration> : IEquatable<ShardConfiguration<TClientConfiguration>>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration
    {
        /// <summary>A unique identifier for the shard configuration.</summary>
        public Int32 Id { get; protected set; }

        /// <summary>The type of data element managed by the shard.</summary>
        public DataElement DataElementType { get; protected set; }

        /// <summary>The type of operation supported by the shard.</summary>
        public Operation OperationType { get; protected set; }

        /// <summary>The first (inclusive) in the range of hash codes of data elements the shard manages.</summary>
        public Int32 HashRangeStart { get; protected set; }

        /// <summary>Configuration which can be used to instantiate a client to connect to the shard.</summary>
        public TClientConfiguration ClientConfiguration { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.ShardConfiguration class.
        /// </summary>
        /// <param name="id">A unique identifier for the shard configuration.</param>
        /// <param name="dataElementType">The type of data element managed by the shard.</param>
        /// <param name="operationType">The type of operation supported by the shard.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of data elements the shard manages.</param>
        /// <param name="clientConfiguration">Configuration which can be used to instantiate a client to connect to the shard.</param>
        public ShardConfiguration(Int32 id, DataElement dataElementType, Operation operationType, Int32 hashRangeStart, TClientConfiguration clientConfiguration)
        {
            Id = id;
            DataElementType = dataElementType;
            OperationType = operationType;
            HashRangeStart = hashRangeStart;
            ClientConfiguration = clientConfiguration;
        }

        /// <summary>
        /// Returns a user-readable description of the shard configuration, primarily used to identify a client which connects to the shard with that configuration in exception messages.
        /// </summary>
        /// <param name="includeHashRangeStart">Whether to include details of the <see cref="HashRangeStart">HashRangeStart</see> property in the description.</param>
        /// <returns>The description.</returns>
        public string Describe(bool includeHashRangeStart)
        {
            if (includeHashRangeStart == true)
            {
                return $"{nameof(Id)} = {Id}, {nameof(DataElementType)} = {DataElementType}, {nameof(OperationType)} = {OperationType}, {nameof(HashRangeStart)} = {HashRangeStart}, {nameof(ClientConfiguration)} = {ClientConfiguration.Description}";
            }
            else
            {
                return $"{nameof(Id)} = {Id}, {nameof(DataElementType)} = {DataElementType}, {nameof(OperationType)} = {OperationType}, {nameof(ClientConfiguration)} = {ClientConfiguration.Description}";
            }
        }

        /// <inheritdoc/>
        public Boolean Equals(ShardConfiguration<TClientConfiguration> other)
        {
            return Id == other.Id;
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
