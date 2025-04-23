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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Redistribution.Models
{
    /// <summary>
    /// Model/container class holding the configuration of a shard group in a distributed AccessManager instance.
    /// </summary>
    /// <typeparam name="TPersistentStorageCredentials">An implementation of <see cref="IPersistentStorageLoginCredentials"/> defining the type of login credentials for the persistent storage instance used by the shard group.</typeparam>
    public class ShardGroupConfiguration<TPersistentStorageCredentials>
        where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
    {
        /// <summary>The first (inclusive) in the range of hash codes of data elements managed by the shard group.</summary>
        public Int32 HashRangeStart { get; protected set; }

        /// <summary>The login credentials for the persistent storage instance used by the shard group.</summary>
        public TPersistentStorageCredentials PersistentStorageCredentials { get; set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Models.ShardGroupConfiguration class.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of data elements managed by the shard group.</param>
        public ShardGroupConfiguration(Int32 hashRangeStart)
        {
            this.HashRangeStart = hashRangeStart;
            this.PersistentStorageCredentials = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Models.ShardGroupConfiguration class.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of data elements managed by the shard group.</param>
        /// <param name="persistentStorageCredentials">The login credentials for the persistent storage instance used by the shard group.</param>
        public ShardGroupConfiguration(Int32 hashRangeStart, TPersistentStorageCredentials persistentStorageCredentials)
            : this(hashRangeStart)
        {
            this.PersistentStorageCredentials = persistentStorageCredentials;
        }
    }
}
