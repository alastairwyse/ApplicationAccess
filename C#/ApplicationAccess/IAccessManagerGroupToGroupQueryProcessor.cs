/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

using System.Collections.Generic;

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods which query state of group to group mapping structures in an AccessManager implementation.
    /// </summary>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    public interface IAccessManagerGroupToGroupQueryProcessor<TGroup>
    {
        /// <summary>
        /// Gets the groups that the specified group is directly mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <returns>A collection of groups the specified group is mapped to.</returns>
        /// <remarks>This method does not traverse the graph which holds group to group mappings, hence only groups mapped directly to the specified group are returned.</remarks>
        IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group);
    }
}
