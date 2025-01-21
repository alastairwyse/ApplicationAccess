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

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models
{
    /// <summary>
    /// Container class holding configuration for a single shard in a distributed AccessManager implementation hosted as a REST-based web API.
    /// </summary>
    public class RestShardConfiguration
    {
        /// <summary>The scheme component of the URL for the shard.</summary>
        public String UriScheme { get; protected set; }

        /// <summary>The host component of the URL for the shard.</summary>
        public String UriHost { get; protected set; }

        /// <summary>The port component of the URL for the shard.</summary>
        public UInt16 UriPort { get; protected set; }

        /// <summary>The first (inclusive) in the range of hash codes of data elements the shard manages.</summary>
        public Int32 HashRangeStart { get; protected set; }

        /// <summary>The last (inclusive) in the range of hash codes of data elements the shard manages.</summary>
        public Int32 HashRangeEnd { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models.RestShardConfiguration class.
        /// </summary>
        /// <param name="uriScheme">The scheme component of the URL for the shard.</param>
        /// <param name="uriHost">The host component of the URL for the shard.</param>
        /// <param name="uriPort">The port component of the URL for the shard.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of data elements the shard manages.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of data elements the shard manages.</param>
        public RestShardConfiguration
        (
            String uriScheme,
            String uriHost,
            UInt16 uriPort,
            Int32 hashRangeStart,
            Int32 hashRangeEnd
        )
        {
            if (hashRangeEnd < hashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(hashRangeEnd), $"Parameter '{nameof(hashRangeEnd)}' with value {hashRangeEnd} must be greater than or equal to the value {hashRangeStart} of parameter '{nameof(hashRangeStart)}'.");

            this.UriScheme = uriScheme;
            this.UriHost = uriHost;
            this.UriPort = uriPort;
            this.HashRangeStart = hashRangeStart;
            this.HashRangeEnd = hashRangeEnd;
        }
    }
}
