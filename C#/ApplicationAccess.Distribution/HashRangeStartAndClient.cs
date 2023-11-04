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
    /// Model/container class holding the first (inclusive) value in a range of hash codes, and an <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> used to connect to a shard which manages data elements in that range.  Implements <see cref="IComparable{T}"/> on the range start value, so the class can be used as the type parameter in a <see cref="WeightBalancedTree{T}"/>.
    /// </summary>
    public class HashRangeStartAndClient : IComparable<HashRangeStartAndClient>, IEquatable<HashRangeStartAndClient>
    {
        /// <summary>
        /// The hash range start value.
        /// </summary>
        public Int32 HashRangeStart { get; protected set; }

        /// <summary>
        /// The AccessManager client.
        /// </summary>
        public IDistributedAccessManagerAsyncClient<String, String, String, String> Client { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.HashRangeStartAndClient class.
        /// </summary>
        /// <param name="hashRangeStart">The hash range start value.</param>
        /// <param name="client">The AccessManager client.</param>
        public HashRangeStartAndClient(Int32 hashRangeStart, IDistributedAccessManagerAsyncClient<String, String, String, String> client)
        {
            this.HashRangeStart = hashRangeStart;
            this.Client = client;
        }

        /// <inheritdoc/>
        public Int32 CompareTo(HashRangeStartAndClient other)
        {
            return HashRangeStart.CompareTo(other.HashRangeStart);
        }

        /// <inheritdoc/>
        public Boolean Equals(HashRangeStartAndClient other)
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
