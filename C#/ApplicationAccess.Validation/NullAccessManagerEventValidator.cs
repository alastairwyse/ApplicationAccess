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
    /// An implementation of <see cref="IAccessManagerEventValidator{TUser, TGroup, TComponent, TAccess}"/> which calls the post-validation action without validating.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class NullAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Validation.NullAccessManagerEventValidator class.
        /// </summary>
        public NullAccessManagerEventValidator()
        {
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddUser(TUser user, Action<TUser> postValidationAction)
        {
            postValidationAction.Invoke(user);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveUser(TUser user, Action<TUser> postValidationAction)
        {
            postValidationAction.Invoke(user);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddGroup(TGroup group, Action<TGroup> postValidationAction)
        {
            postValidationAction.Invoke(group);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveGroup(TGroup group, Action<TGroup> postValidationAction)
        {
            postValidationAction.Invoke(group);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(user, group);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(user, group);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(fromGroup, toGroup);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(fromGroup, toGroup);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(user, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(user, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(group, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(group, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddEntityType(string entityType, Action<string> postValidationAction)
        {
            postValidationAction.Invoke(entityType);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveEntityType(string entityType, Action<string> postValidationAction)
        {
            postValidationAction.Invoke(entityType);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddEntity(string entityType, string entity, Action<string, string> postValidationAction)
        {
            postValidationAction.Invoke(entityType, entity);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveEntity(string entityType, string entity, Action<string, string> postValidationAction)
        {
            postValidationAction.Invoke(entityType, entity);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddUserToEntityMapping(TUser user, string entityType, string entity, Action<TUser, string, string> postValidationAction)
        {
            postValidationAction.Invoke(user, entityType, entity);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveUserToEntityMapping(TUser user, string entityType, string entity, Action<TUser, string, string> postValidationAction)
        {
            postValidationAction.Invoke(user, entityType, entity);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateAddGroupToEntityMapping(TGroup group, string entityType, string entity, Action<TGroup, string, string> postValidationAction)
        {
            postValidationAction.Invoke(group, entityType, entity);
            return new ValidationResult(true);
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRemoveGroupToEntityMapping(TGroup group, string entityType, string entity, Action<TGroup, string, string> postValidationAction)
        {
            postValidationAction.Invoke(group, entityType, entity);
            return new ValidationResult(true);
        }
    }
}
