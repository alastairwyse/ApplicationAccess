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
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to a component.</typeparam>
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
            dateTimeProvider = new StopwatchDateTimeProvider();
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

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddUser(user, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveUser(user, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddGroup(group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroup(group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddUserToGroupMapping(user, group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveUserToGroupMapping(user, group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddEntityType(entityType, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveEntityType(entityType, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddEntity(entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveEntity(entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            AddGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            RemoveGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUser(user, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUser(user, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroup(group, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroup(group, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToGroupMapping(user, group, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToGroupMapping(user, group, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddEntityType(entityType, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveEntityType(entityType, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddEntity(entityType, entity, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveEntity(entityType, entity, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToEntityMapping(user, entityType, entity, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToEntityMapping(group, entityType, entity, eventId, occurredTime);
            }
        }
    }
}
