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

namespace ApplicationAccess.Validation
{
    /// <summary>
    /// Defines methods to validate events which change the structure of an AccessManager implementation.  Includes the ability to invoke an arbitary 'postValidationAction, which in the case of implementations which use mutual-exclusion locks, should be invoked while those locks are still acquired.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Validates an event which adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddUser(TUser user, Action<TUser> postValidationAction);

        /// <summary>
        /// Validates an event which removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <param name="postValidationAction">An action to invoke after removing the user but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveUser(TUser user, Action<TUser> postValidationAction);

        /// <summary>
        /// Validates an event which adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddGroup(TGroup group, Action<TGroup> postValidationAction);

        /// <summary>
        /// Validates an event which removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveGroup(TGroup group, Action<TGroup> postValidationAction);

        /// <summary>
        /// Validates an event which adds a mapping between a user and a group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction);

        /// <summary>
        /// Validates an event which removes a mapping between a user and a group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction);

        /// <summary>
        /// Validates an event which adds a mapping between groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction);

        /// <summary>
        /// Validates an event which removes a mapping between groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction);

        /// <summary>
        /// Validates an event which adds a mapping between a user, an application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction);

        /// <summary>
        /// Validates an event which removes a mapping between a user, an application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction);

        /// <summary>
        /// Validates an event which adds a mapping between a group, an application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction);

        /// <summary>
        /// Validates an event which removes a mapping between a group, an application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction);

        /// <summary>
        /// Validates an event which adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddEntityType(String entityType, Action<String> postValidationAction);

        /// <summary>
        ///   Validates an event which removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveEntityType(String entityType, Action<String> postValidationAction);

        /// <summary>
        /// Validates an event which adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddEntity(String entityType, String entity, Action<String, String> postValidationAction);

        /// <summary>
        /// Validates an event which removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveEntity(String entityType, String entity, Action<String, String> postValidationAction);

        /// <summary>
        /// Validates an event which adds a mapping between a user, and an entity..
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postValidationAction);

        /// <summary>
        /// Validates an event which removes a mapping between a user, and an entity..
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postValidationAction);

        /// <summary>
        /// Validates an event which adds a mapping between a group, and an entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateAddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postValidationAction);

        /// <summary>
        /// Validates an event which removes a mapping between a group, and an entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postValidationAction">An action to invoke after validating the event but whilst any mutual-exclusion locks are still acquired.</param>
        /// <returns>The result of the validation.</returns>
        ValidationResult ValidateRemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postValidationAction);
    }
}
