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
using MoreComplexDataStructures;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Model/container class holding the first (inclusive) value in a range of hash codes, an <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> used to connect to a shard which manages data elements in that range, and a description of the configuration of that shard.  Implements <see cref="IComparable{T}"/> on the range start value, so the class can be used as the type parameter in a <see cref="WeightBalancedTree{T}"/>.
    /// </summary>
    public class HashRangeStartClientAndShardDescription : IComparable<HashRangeStartClientAndShardDescription>, IEquatable<HashRangeStartClientAndShardDescription>
    {
        /// <summary>
        /// The hash range start value.
        /// </summary>
        public Int32 HashRangeStart { get; protected set; }

        /// <summary>
        /// The AccessManager client and description of the configuration of the shard it connects to.
        /// </summary>
        public DistributedClientAndShardDescription ClientAndDescription { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.HashRangeStartClientAndShardDescription class.
        /// </summary>
        /// <param name="hashRangeStart">The hash range start value.</param>
        /// <param name="clientAndDescription">The AccessManager client and description of the configuration of the shard it connects to.</param>
        public HashRangeStartClientAndShardDescription(Int32 hashRangeStart, DistributedClientAndShardDescription clientAndDescription)
        {
            this.HashRangeStart = hashRangeStart;
            this.ClientAndDescription = clientAndDescription;
        }

        /// <inheritdoc/>
        public Int32 CompareTo(HashRangeStartClientAndShardDescription other)
        {
            return HashRangeStart.CompareTo(other.HashRangeStart);
        }

        /// <inheritdoc/>
        public Boolean Equals(HashRangeStartClientAndShardDescription other)
        {
            return HashRangeStart == other.HashRangeStart;
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            return HashRangeStart.GetHashCode();
        }
    }
}
