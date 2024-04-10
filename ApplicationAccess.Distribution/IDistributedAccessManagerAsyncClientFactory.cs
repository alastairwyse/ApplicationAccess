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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods which create <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> instances following the <see href="https://en.wikipedia.org/wiki/Factory_method_pattern">GoF Factory</see> pattern.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of client configuration (implementing <see cref="IDistributedAccessManagerAsyncClientConfiguration"/>).</typeparam>
    /// <typeparam name="TUser">The type of users in the AccessManager the client instances connect to.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager the client instances connect to.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager the client instances connect to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, TUser, TGroup, TComponent, TAccess>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration
    {
        /// <summary>
        /// Creates and returns an <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> based on the specified configuration.
        /// </summary>
        /// <param name="configuration">The client configuration.</param>
        /// <returns>The client.</returns>
        IDistributedAccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess> GetClient(TClientConfiguration configuration);
    }
}
