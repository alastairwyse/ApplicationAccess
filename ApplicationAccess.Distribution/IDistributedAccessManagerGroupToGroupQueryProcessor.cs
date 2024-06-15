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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods which query the state of group to group mapping structures in a distributed implementation of an AccessManager.
    /// </summary>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    public interface IDistributedAccessManagerGroupToGroupQueryProcessor<TGroup>
    {
        /// <summary>
        /// Gets the groups that all of the specified groups are directly and indirectly mapped to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mapped groups for.</param>
        /// <returns>A collection of groups the specified groups are mapped to, and including the specified groups.</returns>
        /// <remarks>Note that <see cref="NonLeafVertexNotFoundException{TGroup}">Exceptions</see> are not thrown if any of the groups in <paramref name="groups" /> don't exist, since it's possible that groups could exist when this method call is initiated, but could be removed by a concurrent call whilst the initial call is being processed.</remarks>
        HashSet<TGroup> GetGroupToGroupMappings(IEnumerable<TGroup> groups);

        /// <summary>
        /// Gets the groups that are directly and indirectly mapped to any of the specified groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mapped groups for.</param>
        /// <returns>A collection of groups that are mapped to the specified groups, and including the specified groups.</returns>
        /// <remarks>Note that <see cref="NonLeafVertexNotFoundException{TGroup}">Exceptions</see> are not thrown if any of the groups in <paramref name="groups" /> don't exist, since it's possible that groups could exist when this method call is initiated, but could be removed by a concurrent call whilst the initial call is being processed.</remarks>
        HashSet<TGroup> GetGroupToGroupReverseMappings(IEnumerable<TGroup> groups);
    }
}
