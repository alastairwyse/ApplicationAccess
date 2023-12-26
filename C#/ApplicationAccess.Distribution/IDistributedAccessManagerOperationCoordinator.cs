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
using System.Text;
using System.Text.RegularExpressions;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods which coordinate operations in an AccessManager implementation where responsibility for subsets of elements is distributed across multiple computers in shards.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public interface IDistributedAccessManagerOperationCoordinator<TClientConfiguration> :
        IAccessManagerAsyncQueryProcessor<String, String, String, String>,
        IAccessManagerAsyncEventProcessor<String, String, String, String>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        /// <summary>
        /// Refreshes the internally stored shard configuration with the specified shard configuration if the configurations differ (if they are the same, no refresh is performed).
        /// </summary>
        /// <param name="shardConfiguration"></param>
        /// <exception cref="ShardConfigurationRefreshException">An exception occurred whilst attempting to refresh/update the shard configuration.</exception>
        void RefreshShardConfiguration(ShardConfigurationSet<TClientConfiguration> shardConfiguration);
    }
}
