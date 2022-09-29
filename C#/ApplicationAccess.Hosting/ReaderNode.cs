/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in an ApplicationAccess hosting environment which allows reading permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class ReaderNode<TUser, TGroup, TComponent, TAccess> : IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Users"]/*'/>
        public IEnumerable<TUser> Users
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Groups"]/*'/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.EntityTypes"]/*'/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsUser(`0)"]/*'/>
        public Boolean ContainsUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsGroup(`1)"]/*'/>
        public Boolean ContainsGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToGroupMappings(`0)"]/*'/>
        public IEnumerable<TGroup> GetUserToGroupMappings(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToGroupMappings(`1)"]/*'/>
        public IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToApplicationComponentAndAccessLevelMappings(`0)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToApplicationComponentAndAccessLevelMappings(`1)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntityType(System.String)"]/*'/>
        public Boolean ContainsEntityType(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntities(System.String)"]/*'/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntity(System.String,System.String)"]/*'/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0,System.String)"]/*'/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1,System.String)"]/*'/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccessToApplicationComponent(`0,`2,`3)"]/*'/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccessToEntity(`0,System.String,System.String)"]/*'/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetApplicationComponentsAccessibleByUser(`0)"]/*'/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetApplicationComponentsAccessibleByGroup(`1)"]/*'/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntitiesAccessibleByUser(`0,System.String)"]/*'/>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntitiesAccessibleByGroup(`1,System.String)"]/*'/>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            throw new NotImplementedException();
        }
    }
}
