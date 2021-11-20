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

namespace ApplicationAccess
{
    /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="T:ApplicationAccess.IAccessManagerQueryProcessor`4"]/*'/>
    public interface IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Users"]/*'/>
        IEnumerable<TUser> Users
        {
            get;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Groups"]/*'/>
        IEnumerable<TGroup> Groups
        {
            get;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.EntityTypes"]/*'/>
        IEnumerable<String> EntityTypes
        {
            get;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsUser(`0)"]/*'/>
        Boolean ContainsUser(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsGroup(`1)"]/*'/>
        Boolean ContainsGroup(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToGroupMappings(`0)"]/*'/>
        IEnumerable<TGroup> GetUserToGroupMappings(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToGroupMappings(`1)"]/*'/>
        IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToApplicationComponentAndAccessLevelMappings(`0)"]/*'/>
        IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToApplicationComponentAndAccessLevelMappings(`1)"]/*'/>
        IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntityType(System.String)"]/*'/>
        Boolean ContainsEntityType(String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntities(System.String)"]/*'/>
        IEnumerable<String> GetEntities(String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntity(System.String,System.String)"]/*'/>
        Boolean ContainsEntity(String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0)"]/*'/>
        IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0,System.String)"]/*'/>
        IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1)"]/*'/>
        IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1,System.String)"]/*'/>
        IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccess(`0,`2,`3)"]/*'/>
        Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccess(`0,System.String,System.String)"]/*'/>
        Boolean HasAccessToEntity(TUser user, String entityType, String entity);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetAccessibleEntities(`0,System.String)"]/*'/>
        HashSet<String> GetAccessibleEntities(TUser user, String entityType);
    }
}
