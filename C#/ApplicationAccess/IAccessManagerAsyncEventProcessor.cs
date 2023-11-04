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
using System.Threading.Tasks;

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods to process events which change the structure of an AccessManager implementation as asyncronous operations.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerAsyncEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Adds a user as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddUserAsync(TUser user);

        /// <summary>
        /// Removes a user as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveUserAsync(TUser user);

        /// <summary>
        /// Adds a group as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddGroupAsync(TGroup group);

        /// <summary>
        /// Removes a group as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveGroupAsync(TGroup group);

        /// <summary>
        /// Adds a mapping between the specified user and group as an asyncronous operation as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddUserToGroupMappingAsync(TUser user, TGroup group);

        /// <summary>
        /// Removes the mapping between the specified user and group as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveUserToGroupMappingAsync(TUser user, TGroup group);

        /// <summary>
        /// Adds a mapping between the specified groups as an asyncronous operation.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddGroupToGroupMappingAsync(TGroup fromGroup, TGroup toGroup);

        /// <summary>
        /// Removes the mapping between the specified groups as an asyncronous operation.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveGroupToGroupMappingAsync(TGroup fromGroup, TGroup toGroup);

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddUserToApplicationComponentAndAccessLevelMappingAsync(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(TGroup group, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(TGroup group, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Adds an entity type as an asyncronous operation.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddEntityTypeAsync(String entityType);

        /// <summary>
        /// Removes an entity type as an asyncronous operation.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveEntityTypeAsync(String entityType);

        /// <summary>
        /// Adds an entity as an asyncronous operation.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddEntityAsync(String entityType, String entity);

        /// <summary>
        /// Removes an entity as an asyncronous operation.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveEntityAsync(String entityType, String entity);

        /// <summary>
        /// Adds a mapping between the specified user, and entity as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddUserToEntityMappingAsync(TUser user, String entityType, String entity);

        /// <summary>
        /// Removes a mapping between the specified user, and entity as an asyncronous operation.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveUserToEntityMappingAsync(TUser user, String entityType, String entity);

        /// <summary>
        /// Adds a mapping between the specified group, and entity as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task AddGroupToEntityMappingAsync(TGroup group, String entityType, String entity);

        /// <summary>
        /// Removes a mapping between the specified group, and entity as an asyncronous operation.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        Task RemoveGroupToEntityMappingAsync(TGroup group, String entityType, String entity);
    }
}
