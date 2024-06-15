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
    /// Defines methods which query the state of user-based structures in an AccessManager implementation.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerUserQueryProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Returns a collection of all users in the access manager.
        /// </summary>
        IEnumerable<TUser> Users
        {
            get;
        }

        /// <summary>
        /// Returns true if the specified user exists.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <returns>True if the user exists.  False otherwise.</returns>
        Boolean ContainsUser(TUser user);

        /// <summary>
        /// Gets the groups that the specified user is mapped to (i.e. is a member of).
        /// </summary>
        /// <param name="user">The user to retrieve the groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of groups the specified user is a member of.</returns>
        HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings);

        /// <summary>
        /// Gets the users that are mapped to the specified group.
        /// </summary>
        /// <param name="group">The group to retrieve the users for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to the group via other groups).</param>
        /// <returns>A collection of users that are mapped to the specified group.</returns>
        HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings);

        /// <summary>
        /// Gets the application component and access level pairs that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the application component and access level pairs that the specified user is mapped to.</returns>
        IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user);

        /// <summary>
        /// Gets users that are mapped to the specific application component and access level pair.
        /// </summary>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to an application component and access level via groups).</param>
        /// <returns>A collection of users that are mapped to the specified application component and access level.</returns>
        IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings);

        /// <summary>
        /// Gets the entities that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified user is mapped to.</returns>
        IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user);

        /// <summary>
        /// Gets the entities of a given type that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A collection of entities that the specified user is mapped to.</returns>
        IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType);

        /// <summary>
        /// Gets the users that are mapped to the specified entity.
        /// </summary>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to the entity via groups).</param>
        /// <returns>A collection of users that are mapped to the specified entity.</returns>
        IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings);

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to an application component at the specified level of access.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>True if the user has access the component.  False otherwise.</returns>
        Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to the specified entity.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the user has access the entity.  False otherwise.</returns>
        Boolean HasAccessToEntity(TUser user, String entityType, String entity);

        /// <summary>
        /// Gets all application components and levels of access that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the user has access to.</returns>
        HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user);

        /// <summary>
        /// Gets all entities that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the entities for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the user has access to.</returns>
        HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user);

        /// <summary>
        /// Gets all entities of a given type that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <returns>The entities the user has access to.</returns>
        HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType);
    }
}
