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

namespace ApplicationAccess.Persistence
{
    /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="T:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4"]/*'/>
    public interface IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUser(`0,System.Int64,System.DateTime)"]/*'/>
        void AddUser(TUser user, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUser(`0,System.Int64,System.DateTime)"]/*'/>
        void RemoveUser(TUser user, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroup(`1,System.Int64,System.DateTime)"]/*'/>
        void AddGroup(TGroup group, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroup(`1,System.Int64,System.DateTime)"]/*'/>
        void RemoveGroup(TGroup group, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToGroupMapping(`0,`1,System.Int64,System.DateTime)"]/*'/>
        void AddUserToGroupMapping(TUser user, TGroup group, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToGroupMapping(`0,`1,System.Int64,System.DateTime)"]/*'/>
        void RemoveUserToGroupMapping(TUser user, TGroup group, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToGroupMapping(`1,`1,System.Int64,System.DateTime)"]/*'/>
        void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToGroupMapping(`1,`1,System.Int64,System.DateTime)"]/*'/>
        void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Int64,System.DateTime)"]/*'/>
        void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Int64,System.DateTime)"]/*'/>
        void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Int64,System.DateTime)"]/*'/>
        void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Int64,System.DateTime)"]/*'/>
        void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntityType(System.String,System.Int64,System.DateTime)"]/*'/>
        void AddEntityType(String entityType, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntityType(System.String,System.Int64,System.DateTime)"]/*'/>
        void RemoveEntityType(String entityType, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntity(System.String,System.String,System.Int64,System.DateTime)"]/*'/>
        void AddEntity(String entityType, String entity, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntity(System.String,System.String,System.Int64,System.DateTime)"]/*'/>
        void RemoveEntity(String entityType, String entity, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToEntityMapping(`0,System.String,System.String,System.Int64,System.DateTime)"]/*'/>
        void AddUserToEntityMapping(TUser user, String entityType, String entity, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToEntityMapping(`0,System.String,System.String,System.Int64,System.DateTime)"]/*'/>
        void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToEntityMapping(`1,System.String,System.String,System.Int64,System.DateTime)"]/*'/>
        void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToEntityMapping(`1,System.String,System.String,System.Int64,System.DateTime)"]/*'/>
        void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Int64 sequenceNumber, DateTime occurredTime);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.Load(System.Int64)"]/*'/>
        AccessManager<TUser, TGroup, TComponent, TAccess> Load(Int64 sequenceNumber);
    }
}
