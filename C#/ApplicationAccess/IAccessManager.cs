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
using System.Collections.Generic;
using System.Text;

namespace ApplicationAccess
{
    /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="XXX"]/*'/>
    public interface IAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManager`4.Users"]/*'/>
        IEnumerable<TUser> Users
        {
            get;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManager`4.Groups"]/*'/>
        IEnumerable<TGroup> Groups
        {
            get;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManager`4.EntityTypes"]/*'/>
        IEnumerable<String> EntityTypes
        {
            get;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddUser(`0)"]/*'/>
        void AddUser(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.ContainsUser(`0)"]/*'/>
        Boolean ContainsUser(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveUser(`0)"]/*'/>
        void RemoveUser(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddGroup(`1)"]/*'/>
        void AddGroup(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.ContainsGroup(`1)"]/*'/>
        Boolean ContainsGroup(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveGroup(`1)"]/*'/>
        void RemoveGroup(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        void AddUserToGroupMapping(TUser user, TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetUserToGroupMappings(`0)"]/*'/>
        IEnumerable<TGroup> GetUserToGroupMappings(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        void RemoveUserToGroupMapping(TUser user, TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetGroupToGroupMappings(`1)"]/*'/>
        IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetUserToApplicationComponentAndAccessLevelMappings(`0)"]/*'/>
        IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetGroupToApplicationComponentAndAccessLevelMappings(`1)"]/*'/>
        IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddEntityType(System.String)"]/*'/>
        void AddEntityType(String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.ContainsEntityType(System.String)"]/*'/>
        Boolean ContainsEntityType(String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveEntityType(System.String)"]/*'/>
        void RemoveEntityType(String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddEntity(System.String,System.String)"]/*'/>
        void AddEntity(String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetEntities(System.String)"]/*'/>
        IEnumerable<String> GetEntities(String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.ContainsEntity(System.String,System.String)"]/*'/>
        Boolean ContainsEntity(String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveEntity(System.String,System.String)"]/*'/>
        void RemoveEntity(String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        void AddUserToEntityMapping(TUser user, String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetUserToEntityMappings(`0)"]/*'/>
        IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetUserToEntityMappings(`0,System.String)"]/*'/>
        IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        void RemoveUserToEntityMapping(TUser user, String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        void AddGroupToEntityMapping(TGroup group, String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetGroupToEntityMappings(`1)"]/*'/>
        IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetGroupToEntityMappings(`1,System.String)"]/*'/>
        IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.HasAccess(`0,`2,`3)"]/*'/>
        Boolean HasAccess(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.HasAccess(`0,System.String,System.String)"]/*'/>
        Boolean HasAccess(TUser user, String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManager`4.GetAccessibleEntities(`0,System.String)"]/*'/>
        HashSet<String> GetAccessibleEntities(TUser user, String entityType);
    }
}
