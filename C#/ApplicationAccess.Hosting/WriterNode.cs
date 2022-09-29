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
    /// A node in an ApplicationAccess hosting environment which allows writing permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class WriterNode<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public void AddUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public void RemoveUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public void AddGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public void RemoveGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public void AddEntityType(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public void RemoveEntityType(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public void AddEntity(String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public void RemoveEntity(String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            throw new NotImplementedException();
        }
    }
}
