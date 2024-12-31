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
        /// <summary>The hash code generator for users.</summary>
        protected IHashCodeGenerator<TUser> userHashCodeGenerator;
        /// <summary>The hash code generator for groups.</summary>
        protected IHashCodeGenerator<TGroup> groupHashCodeGenerator;
        /// <summary>The hash code generator for entity types.</summary>
        protected IHashCodeGenerator<String> entityTypeHashCodeGenerator;
        /// <summary>Holds the <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</summary>
        protected List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.AccessManagerTemporalEventPersisterDistributor class.
        /// </summary>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersisters">The <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</param>
        public AccessManagerTemporalEventPersisterDistributor
        (
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IEnumerable<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters
        )
        {
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.entityTypeHashCodeGenerator = entityTypeHashCodeGenerator;
            guidProvider = new DefaultGuidProvider();
            dateTimeProvider = new StopwatchDateTimeProvider();
            this.eventPersisters = new List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>>(eventPersisters);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.AccessManagerTemporalEventPersisterDistributor class.
        /// </summary>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersisters">The <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventPersisterDistributor
        (
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IEnumerable<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters, 
            IGuidProvider guidProvider, 
            IDateTimeProvider dateTimeProvider
        )
            : base()
        {
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.entityTypeHashCodeGenerator = entityTypeHashCodeGenerator;
            this.guidProvider = guidProvider;
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            AddUser(user, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            RemoveUser(user, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(group);
            AddGroup(group, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(group);
            RemoveGroup(group, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            AddUserToGroupMapping(user, group, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            RemoveUserToGroupMapping(user, group, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(fromGroup);
            AddGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(fromGroup);
            RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(group);
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(group);
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = entityTypeHashCodeGenerator.GetHashCode(entityType);
            AddEntityType(entityType, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = entityTypeHashCodeGenerator.GetHashCode(entityType);
            RemoveEntityType(entityType, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = entityTypeHashCodeGenerator.GetHashCode(entityType);
            AddEntity(entityType, entity, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = entityTypeHashCodeGenerator.GetHashCode(entityType);
            RemoveEntity(entityType, entity, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            AddUserToEntityMapping(user, entityType, entity, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            RemoveUserToEntityMapping(user, entityType, entity, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(group);
            AddGroupToEntityMapping(group, entityType, entity, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid eventId = guidProvider.NewGuid();
            DateTime occurredTime = dateTimeProvider.UtcNow();
            Int32 hashCode = groupHashCodeGenerator.GetHashCode(group);
            RemoveGroupToEntityMapping(group, entityType, entity, eventId, occurredTime, hashCode);
        }

        /// <inheritdoc/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUser(user, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUser(user, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroup(group, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroup(group, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToGroupMapping(user, group, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToGroupMapping(user, group, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddEntityType(entityType, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveEntityType(entityType, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddEntity(entityType, entity, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveEntity(entityType, entity, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddUserToEntityMapping(user, entityType, entity, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveUserToEntityMapping(user, entityType, entity, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.AddGroupToEntityMapping(group, entityType, entity, eventId, occurredTime, hashCode);
            }
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.RemoveGroupToEntityMapping(group, entityType, entity, eventId, occurredTime, hashCode);
            }
        }
    }
}
