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
using ApplicationAccess.Validation;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerEventValidator{TUser, TGroup, TComponent, TAccess}"/> which allows interception of method calls via a call to <see cref="IMethodCallInterceptor.Intercept">IMethodCallInterceptor.Intercept()</see>, and subsequently calls the equivalent method in an instance of <see cref="NullAccessManagerEventValidator{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    internal class MethodInterceptingAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>A mock of IMethodCallInterceptor (for intercepting method calls).</summary>
        protected IMethodCallInterceptor interceptor;
        /// <summary>An instance of NullAccessManagerEventValidator to perform the IAccessManagerEventValidator functionality.</summary>
        protected NullAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> nullAccessManagerEventValidator;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.MethodInterceptingAccessManagerEventValidator class.
        /// </summary>
        /// <param name="interceptor">A mock of IMethodCallInterceptor (for intercepting method calls).</param>
        /// <param name="nullAccessManagerEventValidator">An instance of NullAccessManagerEventValidator to perform the IAccessManagerEventValidator functionality.</param>
        public MethodInterceptingAccessManagerEventValidator(IMethodCallInterceptor interceptor, NullAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> nullAccessManagerEventValidator)
        {
            this.interceptor = interceptor;
            this.nullAccessManagerEventValidator = nullAccessManagerEventValidator;
        }

        public ValidationResult ValidateAddUser(TUser user, Action<TUser> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddUser(user, postValidationAction);
        }

        public ValidationResult ValidateRemoveUser(TUser user, Action<TUser> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveUser(user, postValidationAction);
        }

        public ValidationResult ValidateAddGroup(TGroup group, Action<TGroup> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddGroup(group, postValidationAction);
        }

        public ValidationResult ValidateRemoveGroup(TGroup group, Action<TGroup> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveGroup(group, postValidationAction);
        }

        public ValidationResult ValidateAddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddUserToGroupMapping(user, group, postValidationAction);
        }

        public ValidationResult ValidateRemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveUserToGroupMapping(user, group, postValidationAction);
        }

        public ValidationResult ValidateAddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddGroupToGroupMapping(fromGroup, toGroup, postValidationAction);
        }

        public ValidationResult ValidateRemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveGroupToGroupMapping(fromGroup, toGroup, postValidationAction);
        }

        public ValidationResult ValidateAddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction);
        }

        public ValidationResult ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction);
        }

        public ValidationResult ValidateAddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction);
        }

        public ValidationResult ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction);
        }

        public ValidationResult ValidateAddEntityType(String entityType, Action<String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddEntityType(entityType, postValidationAction);
        }

        public ValidationResult ValidateRemoveEntityType(String entityType, Action<String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveEntityType(entityType, postValidationAction);
        }

        public ValidationResult ValidateAddEntity(String entityType, String entity, Action<String, String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddEntity(entityType, entity, postValidationAction);
        }

        public ValidationResult ValidateRemoveEntity(String entityType, String entity, Action<String, String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveEntity(entityType, entity, postValidationAction);
        }

        public ValidationResult ValidateAddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddUserToEntityMapping(user, entityType, entity, postValidationAction);
        }

        public ValidationResult ValidateRemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveUserToEntityMapping(user, entityType, entity, postValidationAction);
        }

        public ValidationResult ValidateAddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateAddGroupToEntityMapping(group, entityType, entity, postValidationAction);
        }

        public ValidationResult ValidateRemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postValidationAction)
        {
            interceptor.Intercept();
            return nullAccessManagerEventValidator.ValidateRemoveGroupToEntityMapping(group, entityType, entity, postValidationAction);
        }
    }
}
