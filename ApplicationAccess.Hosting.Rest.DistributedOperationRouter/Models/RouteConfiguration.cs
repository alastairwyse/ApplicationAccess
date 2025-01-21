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
    /// Container class holding configuration for source and target shards used in a DistributedOperationRouter node.
    /// </summary>
    public class RouteConfiguration
    {
        /// <summary>The configuration of the source shard the router should route to.</summary>
        public RestShardConfiguration SourceShardConfiguration { get; protected set; }

        /// <summary>The configuration of the target shard the router should route to.</summary>
        public RestShardConfiguration TargetShardConfiguration { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models.RouteConfiguration class.
        /// </summary>
        /// <param name="sourceShardConfiguration">The configuration of the source shard the router should route to.</param>
        /// <param name="targetShardConfiguration">The configuration of the target shard the router should route to.</param>
        public RouteConfiguration(RestShardConfiguration sourceShardConfiguration, RestShardConfiguration targetShardConfiguration)
        {
            if (sourceShardConfiguration.HashRangeEnd + 1 != targetShardConfiguration.HashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(targetShardConfiguration), $"Property '{nameof(targetShardConfiguration.HashRangeStart)}' of parameter '{nameof(targetShardConfiguration)}' with value {targetShardConfiguration.HashRangeStart} must be contiguous with property '{nameof(sourceShardConfiguration.HashRangeEnd)}' of parameter '{nameof(sourceShardConfiguration)}' with value {sourceShardConfiguration.HashRangeEnd}.");

            this.SourceShardConfiguration = sourceShardConfiguration;
            this.TargetShardConfiguration = targetShardConfiguration;
        }
    }
}
