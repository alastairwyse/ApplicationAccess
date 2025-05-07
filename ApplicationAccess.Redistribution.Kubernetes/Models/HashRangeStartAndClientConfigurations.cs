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
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;

namespace ApplicationAccess.Redistribution.Kubernetes.Models
{
    /// <summary>
    /// Model/container class holding a hash range start value and reader and writer node client configurations.
    /// </summary>
    /// <remarks>Used to update <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> instances.</remarks>
    public record HashRangeStartAndClientConfigurations
    {
        /// <summary>The first (inclusive) in the range of hash codes of data elements managed by a shard group.</summary>
        public Int32 HashRangeStart { get; init; }

        /// <summary>Configuration for a client which connects to the reader node within a shard group.</summary>
        public AccessManagerRestClientConfiguration ReaderNodeClientConfiguration { get; init; }

        /// <summary>Configuration for a client which connects to the writer node within a shard group.</summary>
        public AccessManagerRestClientConfiguration WriterNodeClientConfiguration { get; init; }
    }
}
