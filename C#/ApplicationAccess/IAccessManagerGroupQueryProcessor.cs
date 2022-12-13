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

using System;
using System.Collections.Generic;

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods which query state of group-based structures in an AccessManager implementation.
    /// </summary>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerGroupQueryProcessor<TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Returns a collection of all groups in the access manager.
        /// </summary>
        IEnumerable<TGroup> Groups
        {
            get;
        }

        /// <summary>
        /// Returns true if the specified group exists.
        /// </summary>
        /// <param name="group">The group to check for.</param>
        /// <returns>True if the group exists.  False otherwise.</returns>
        Boolean ContainsGroup(TGroup group);

        /// <summary>
        /// Gets the application component and access level pairs that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the application component and access level pairs that the specified group is mapped to.</returns>
        IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group);

        /// <summary>
        /// Gets the entities that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified group is mapped to.</returns>
        IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group);

        /// <summary>
        /// Gets the entities of a given type that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A collection of entities that the specified group is mapped to.</returns>
        IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType);

        /// <summary>
        /// Gets all application components and levels of access that the specified group (or group that the specified group is mapped to) has access to.
        /// </summary>
        /// <param name="group">The group to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the group has.</returns>
        HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group);

        /// <summary>
        /// Gets all entities that the specified group (or group that the specified group is mapped to) has access to.
        /// </summary>
        /// <param name="group">The group to retrieve the entities for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the group has access to.</returns>
        HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group);

        /// <summary>
        /// Gets all entities of a given type that the specified group (or group that the specified group is mapped to) has access to.
        /// </summary>
        /// <param name="group">The group to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <returns>The entities the group has access to.</returns>
        HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType);
    }
}
