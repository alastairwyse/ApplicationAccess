﻿/*
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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods which distribute operations to two shards in a distributed AccessManager implementation.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public interface IDistributedAccessManagerOperationRouter<TClientConfiguration> :
        IAccessManagerAsyncQueryProcessor<String, String, String, String>,
        IAccessManagerAsyncEventProcessor<String, String, String, String>,
        IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        /// <summary>
        /// Whether or not the routing functionality is switched on.
        /// </summary>
        Boolean RoutingOn { get; set; }
    }
}
