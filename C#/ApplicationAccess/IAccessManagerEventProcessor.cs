/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods to process events which change the structure of an AccessManager implementation.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        void AddUser(TUser user);

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        void RemoveUser(TUser user);

        /// <summary>
        /// Adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        void AddGroup(TGroup group);

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        void RemoveGroup(TGroup group);

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        void AddUserToGroupMapping(TUser user, TGroup group);

        /// <summary>
        /// Removes the mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        void RemoveUserToGroupMapping(TUser user, TGroup group);

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup);

        /// <summary>
        /// Removes the mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup);

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        void AddEntityType(String entityType);

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        void RemoveEntityType(String entityType);

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        void AddEntity(String entityType, String entity);

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        void RemoveEntity(String entityType, String entity);

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        void AddUserToEntityMapping(TUser user, String entityType, String entity);

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        void RemoveUserToEntityMapping(TUser user, String entityType, String entity);

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        void AddGroupToEntityMapping(TGroup group, String entityType, String entity);

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity);
    }
}
