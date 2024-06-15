/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Net.NetworkInformation;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods which query the state of user-based structures in a distributed implementation of an AccessManager.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    public interface IDistributedAccessManagerUserQueryProcessor<TUser, TGroup>
    {
        /// <summary>
        /// Gets the users that are directly mapped to any of the specified groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the users for.</param>
        /// <returns>A collection of users that are mapped to the specified groups.</returns>
        /// <remarks>Note that <see cref="NonLeafVertexNotFoundException{TGroup}">Exceptions</see> are not thrown if any of the groups in <paramref name="groups" /> don't exist, since it's possible that groups could exist when this method call is initiated, but could be removed by a concurrent call whilst the initial call is being processed.</remarks>
        HashSet<TUser> GetGroupToUserMappings(IEnumerable<TGroup> groups);
    }
}
