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

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUser(`0,System.Action{`0})"]/*'/>
        public ValidationResult ValidateAddUser(TUser user, Action<TUser> postValidationAction)
        {
            postValidationAction.Invoke(user);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUser(`0,System.Action{`0})"]/*'/>
        public ValidationResult ValidateRemoveUser(TUser user, Action<TUser> postValidationAction)
        {
            postValidationAction.Invoke(user);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroup(`1,System.Action{`1})"]/*'/>
        public ValidationResult ValidateAddGroup(TGroup group, Action<TGroup> postValidationAction)
        {
            postValidationAction.Invoke(group);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroup(`1,System.Action{`1})"]/*'/>
        public ValidationResult ValidateRemoveGroup(TGroup group, Action<TGroup> postValidationAction)
        {
            postValidationAction.Invoke(group);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUserToGroupMapping(`0,`1,System.Action{`0,`1})"]/*'/>
        public ValidationResult ValidateAddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(user, group);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUserToGroupMapping(`0,`1,System.Action{`0,`1})"]/*'/>
        public ValidationResult ValidateRemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(user, group);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroupToGroupMapping(`1,`1,System.Action{`1,`1})"]/*'/>
        public ValidationResult ValidateAddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(fromGroup, toGroup);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroupToGroupMapping(`1,`1,System.Action{`1,`1})"]/*'/>
        public ValidationResult ValidateRemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
        {
            postValidationAction.Invoke(fromGroup, toGroup);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Action{`0,`2,`3})"]/*'/>
        public ValidationResult ValidateAddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(user, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Action{`0,`2,`3})"]/*'/>
        public ValidationResult ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(user, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Action{`1,`2,`3})"]/*'/>
        public ValidationResult ValidateAddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(group, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Action{`1,`2,`3})"]/*'/>
        public ValidationResult ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
        {
            postValidationAction.Invoke(group, applicationComponent, accessLevel);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddEntityType(System.String,System.Action{System.String})"]/*'/>
        public ValidationResult ValidateAddEntityType(string entityType, Action<string> postValidationAction)
        {
            postValidationAction.Invoke(entityType);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveEntityType(System.String,System.Action{System.String})"]/*'/>
        public ValidationResult ValidateRemoveEntityType(string entityType, Action<string> postValidationAction)
        {
            postValidationAction.Invoke(entityType);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddEntity(System.String,System.String,System.Action{System.String,System.String})"]/*'/>
        public ValidationResult ValidateAddEntity(string entityType, string entity, Action<string, string> postValidationAction)
        {
            postValidationAction.Invoke(entityType, entity);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveEntity(System.String,System.String,System.Action{System.String,System.String})"]/*'/>
        public ValidationResult ValidateRemoveEntity(string entityType, string entity, Action<string, string> postValidationAction)
        {
            postValidationAction.Invoke(entityType, entity);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUserToEntityMapping(`0,System.String,System.String,System.Action{`0,System.String,System.String})"]/*'/>
        public ValidationResult ValidateAddUserToEntityMapping(TUser user, string entityType, string entity, Action<TUser, string, string> postValidationAction)
        {
            postValidationAction.Invoke(user, entityType, entity);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUserToEntityMapping(`0,System.String,System.String,System.Action{`0,System.String,System.String})"]/*'/>
        public ValidationResult ValidateRemoveUserToEntityMapping(TUser user, string entityType, string entity, Action<TUser, string, string> postValidationAction)
        {
            postValidationAction.Invoke(user, entityType, entity);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroupToEntityMapping(`1,System.String,System.String,System.Action{`1,System.String,System.String})"]/*'/>
        public ValidationResult ValidateAddGroupToEntityMapping(TGroup group, string entityType, string entity, Action<TGroup, string, string> postValidationAction)
        {
            postValidationAction.Invoke(group, entityType, entity);
            return new ValidationResult(true);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroupToEntityMapping(`1,System.String,System.String,System.Action{`1,System.String,System.String})"]/*'/>
        public ValidationResult ValidateRemoveGroupToEntityMapping(TGroup group, string entityType, string entity, Action<TGroup, string, string> postValidationAction)
        {
            postValidationAction.Invoke(group, entityType, entity);
            return new ValidationResult(true);
        }
    }
}
