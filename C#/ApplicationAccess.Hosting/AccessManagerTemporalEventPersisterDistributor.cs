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
using ApplicationAccess.Persistence;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// Distributes methods calls syncronously to multiple <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instances.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerTemporalEventPersister instances.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerTemporalEventPersister instances.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerTemporalEventPersister instances.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an <see cref="TComponent"/>.</typeparam>
    public class AccessManagerTemporalEventPersisterDistributor<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The provider to use for random Guids.</summary>
        protected IGuidProvider guidProvider;
        /// <summary>The provider to use for the current date and time.</summary>
        protected IDateTimeProvider dateTimeProvider;
        /// <summary>Holds the <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</summary>
        protected List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.AccessManagerTemporalEventPersisterDistributor class.
        /// </summary>
        /// <param name="eventPersisters">The <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</param>
        public AccessManagerTemporalEventPersisterDistributor(IEnumerable<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters)
        {
            guidProvider = new DefaultGuidProvider();
            dateTimeProvider = new DefaultDateTimeProvider();
            this.eventPersisters = new List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>>(eventPersisters);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.AccessManagerTemporalEventPersisterDistributor class.
        /// </summary>
        /// <param name="eventPersisters">The <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventPersisterDistributor(IEnumerable<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters, IGuidProvider guidProvider, IDateTimeProvider dateTimeProvider)
            : base()
        {
            this.guidProvider = guidProvider;
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public void AddUser(TUser user)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddUser(user, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public void RemoveUser(TUser user)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveUser(user, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public void AddGroup(TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddGroup(group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public void RemoveGroup(TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroup(group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddUserToGroupMapping(user, group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveUserToGroupMapping(user, group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public void AddEntityType(String entityType)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddEntityType(entityType, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public void RemoveEntityType(String entityType)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveEntityType(entityType, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public void AddEntity(String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            foreach (var currentEventPersister in eventPersisters)
            AddEntity(entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public void RemoveEntity(String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveEntity(entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            foreach (var currentEventPersister in eventPersisters)
            AddUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            foreach (var currentEventPersister in eventPersisters)
            RemoveUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            foreach (var currentEventPersister in eventPersisters)
            AddGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUser(user, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUser(user, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroup(group, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroup(group, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToGroupMapping(user, group, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToGroupMapping(user, group, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddEntityType(entityType, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveEntityType(entityType, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddEntity(entityType, entity, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveEntity(entityType, entity, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
            }
        }
    }
}
