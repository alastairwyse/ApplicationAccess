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
    /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="T:ApplicationAccess.Validation.IAccessManagerEventValidator`4"]/*'/>
    public interface IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUser(`0,System.Action{`0})"]/*'/>
        ValidationResult ValidateAddUser(TUser user, Action<TUser> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUser(`0,System.Action{`0})"]/*'/>
        ValidationResult ValidateRemoveUser(TUser user, Action<TUser> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroup(`1,System.Action{`1})"]/*'/>
        ValidationResult ValidateAddGroup(TGroup group, Action<TGroup> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroup(`1,System.Action{`1})"]/*'/>
        ValidationResult ValidateRemoveGroup(TGroup group, Action<TGroup> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUserToGroupMapping(`0,`1,System.Action{`0,`1})"]/*'/>
        ValidationResult ValidateAddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUserToGroupMapping(`0,`1,System.Action{`0,`1})"]/*'/>
        ValidationResult ValidateRemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroupToGroupMapping(`1,`1,System.Action{`1,`1})"]/*'/>
        ValidationResult ValidateAddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroupToGroupMapping(`1,`1,System.Action{`1,`1})"]/*'/>
        ValidationResult ValidateRemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Action{`0,`2,`3})"]/*'/>
        ValidationResult ValidateAddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Action{`0,`2,`3})"]/*'/>
        ValidationResult ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Action{`1,`2,`3})"]/*'/>
        ValidationResult ValidateAddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Action{`1,`2,`3})"]/*'/>
        ValidationResult ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddEntityType(System.String,System.Action{System.String})"]/*'/>
        ValidationResult ValidateAddEntityType(String entityType, Action<String> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveEntityType(System.String,System.Action{System.String})"]/*'/>
        ValidationResult ValidateRemoveEntityType(String entityType, Action<String> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddEntity(System.String,System.String,System.Action{System.String,System.String})"]/*'/>
        ValidationResult ValidateAddEntity(String entityType, String entity, Action<String, String> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveEntity(System.String,System.String,System.Action{System.String,System.String})"]/*'/>
        ValidationResult ValidateRemoveEntity(String entityType, String entity, Action<String, String> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddUserToEntityMapping(`0,System.String,System.String,System.Action{`0,System.String,System.String})"]/*'/>
        ValidationResult ValidateAddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveUserToEntityMapping(`0,System.String,System.String,System.Action{`0,System.String,System.String})"]/*'/>
        ValidationResult ValidateRemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateAddGroupToEntityMapping(`1,System.String,System.String,System.Action{`1,System.String,System.String})"]/*'/>
        ValidationResult ValidateAddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postValidationAction);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Validation.IAccessManagerEventValidator`4.ValidateRemoveGroupToEntityMapping(`1,System.String,System.String,System.Action{`1,System.String,System.String})"]/*'/>
        ValidationResult ValidateRemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postValidationAction);
    }
}
