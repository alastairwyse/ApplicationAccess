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
using System.Threading.Tasks;

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods which query the state/structure of an AccessManager implementation as asyncronous operations.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerAsyncQueryProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Returns a list of all users in the access manager as an asyncronous operation.
        /// </summary>
        /// <returns></returns>
        Task<List<TUser>> GetUsersAsync();

        /// <summary>
        /// Returns a list of all groups in the access manager as an asyncronous operation.
        /// </summary>
        Task<List<TGroup>> GetGroupsAsync();

        /// <summary>
        /// Returns a list of all entity types in the access manager as an asyncronous operation.
        /// </summary>
        Task<List<String>> GetEntityTypesAsync();

        /// <summary>
        /// Returns true if the specified user exists as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <returns>True if the user exists.  False otherwise.</returns>
        Task<Boolean> ContainsUserAsync(TUser user);

        /// <summary>
        /// Returns true if the specified group exists as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to check for.</param>
        /// <returns>True if the group exists.  False otherwise.</returns>
        Task<Boolean> ContainsGroupAsync(TGroup group);

        /// <summary>
        /// Gets the groups that the specified user is mapped to (i.e. is a member of) as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to retrieve the groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A list of groups the specified user is a member of.</returns>
        Task<List<TGroup>> GetUserToGroupMappingsAsync(TUser user, Boolean includeIndirectMappings);

        /// <summary>
        /// Gets the groups that the specified group is mapped to as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped to' group is itself mapped to further groups).</param>
        /// <returns>A list of groups the specified group is mapped to.</returns>
        Task<List<TGroup>> GetGroupToGroupMappingsAsync(TGroup group, Boolean includeIndirectMappings);

        /// <summary>
        /// Gets the application component and access level pairs that the specified user is mapped to as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <returns>A list of Tuples containing the application component and access level pairs that the specified user is mapped to.</returns>
        Task<List<Tuple<TComponent, TAccess>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(TUser user);

        /// <summary>
        /// Gets the application component and access level pairs that the specified group is mapped to as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <returns>A list of Tuples containing the application component and access level pairs that the specified group is mapped to.</returns>
        Task<List<Tuple<TComponent, TAccess>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(TGroup group);

        /// <summary>
        /// Returns true if the specified entity type exists as an asyncronous operation.
        /// </summary>
        /// <param name="entityType">The entity type to check for.</param>
        /// <returns>True if the entity type exists.  False otherwise.</returns>
        Task<Boolean> ContainsEntityTypeAsync(String entityType);

        /// <summary>
        /// Returns all entities of the specified type as an asyncronous operation.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>A list of all entities of the specified type.</returns>
        Task<List<String>> GetEntitiesAsync(String entityType);

        /// <summary>
        /// Returns true if the specified entity exists as an asyncronous operation.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>True if the entity exists.  False otherwise.</returns>
        Task<Boolean> ContainsEntityAsync(String entityType, String entity);

        /// <summary>
        /// Gets the entities that the specified user is mapped to as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <returns>A list of Tuples containing the entity type and entity that the specified user is mapped to.</returns>
        Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(TUser user);

        /// <summary>
        /// Gets the entities of a given type that the specified user is mapped to as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A list of entities that the specified user is mapped to.</returns>
        Task<List<String>> GetUserToEntityMappingsAsync(TUser user, String entityType);

        /// <summary>
        /// Gets the entities that the specified group is mapped to as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <returns>A list of Tuples containing the entity type and entity that the specified group is mapped to.</returns>
        Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(TGroup group);

        /// <summary>
        /// Gets the entities of a given type that the specified group is mapped to as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A list of entities that the specified group is mapped to.</returns>
        Task<List<String>> GetGroupToEntityMappingsAsync(TGroup group, String entityType);

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to an application component at the specified level of access as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>True if the user has access the component.  False otherwise.</returns>
        Task<Boolean> HasAccessToApplicationComponentAsync(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to the specified entity as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the user has access the entity.  False otherwise.</returns>
        Task<Boolean> HasAccessToEntityAsync(TUser user, String entityType, String entity);

        /// <summary>
        /// Gets all application components and levels of access that the specified user (or a group that the user is a member of) has access to as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the user has.</returns>
        Task<List<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByUserAsync(TUser user);

        /// <summary>
        /// Gets all application components and levels of access that the specified group (or group that the specified group is mapped to) has access to as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the group has.</returns>
        Task<List<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByGroupAsync(TGroup group);

        /// <summary>
        /// Gets all entities that the specified user (or a group that the user is a member of) has access to as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to retrieve the entities for.</param>
        /// <returns>A list of Tuples containing the entity type and entity that the user has access to.</returns>
        Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(TUser user);

        /// <summary>
        /// Gets all entities of a given type that the specified user (or a group that the user is a member of) has access to as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <returns>The entities the user has access to.</returns>
        Task<List<String>> GetEntitiesAccessibleByUserAsync(TUser user, String entityType);

        /// <summary>
        /// Gets all entities that the specified group (or group that the specified group is mapped to) has access to as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to retrieve the entities for.</param>
        /// <returns>A list of Tuples containing the entity type and entity that the group has access to.</returns>
        Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(TGroup group);

        /// <summary>
        /// Gets all entities of a given type that the specified group (or group that the specified group is mapped to) has access to as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <returns>The entities the group has access to.</returns>
        Task<List<String>> GetEntitiesAccessibleByGroupAsync(TGroup group, String entityType);
    }
}
