/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Caches a predefined number of AccessManager <see cref="TemporalEventBufferItemBase"/> events.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventCacheBase<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The hash code generator for users.</summary>
        protected IHashCodeGenerator<TUser> userHashCodeGenerator;
        /// <summary>The hash code generator for groups.</summary>
        protected IHashCodeGenerator<TGroup> groupHashCodeGenerator;
        /// <summary>The hash code generator for entity types.</summary>
        protected IHashCodeGenerator<String> entityTypeHashCodeGenerator;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCache class.
        /// </summary>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        public AccessManagerTemporalEventCache
        (
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator, 
            Int32 cachedEventCount
        )
            : base(cachedEventCount)
        {
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.entityTypeHashCodeGenerator = entityTypeHashCodeGenerator;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCache class.
        /// </summary>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventCache
        (
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            Int32 cachedEventCount, 
            IMetricLogger metricLogger
        )
            : base(cachedEventCount, metricLogger)
        {
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.entityTypeHashCodeGenerator = entityTypeHashCodeGenerator;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCache class.
        /// </summary>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventCache
        (
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            Int32 cachedEventCount, 
            IMetricLogger metricLogger, 
            Utilities.IGuidProvider 
            guidProvider, 
            IDateTimeProvider dateTimeProvider
        )
            : base(cachedEventCount, metricLogger, guidProvider, dateTimeProvider)
        {
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.entityTypeHashCodeGenerator = entityTypeHashCodeGenerator;
        }

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            AddUser(user, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            RemoveUser(user, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            AddGroup(group, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(group));
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            RemoveGroup(group, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(group));
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            AddUserToGroupMapping(user, group, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            RemoveUserToGroupMapping(user, group, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            AddGroupToGroupMapping(fromGroup, toGroup, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(fromGroup));
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            RemoveGroupToGroupMapping(fromGroup, toGroup, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(fromGroup));
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(group));
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(group));
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            AddEntityType(entityType, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), entityTypeHashCodeGenerator.GetHashCode(entityType));
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            RemoveEntityType(entityType, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), entityTypeHashCodeGenerator.GetHashCode(entityType));
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            AddEntity(entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), entityTypeHashCodeGenerator.GetHashCode(entityType));
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            RemoveEntity(entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), entityTypeHashCodeGenerator.GetHashCode(entityType));
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            AddUserToEntityMapping(user, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            RemoveUserToEntityMapping(user, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), userHashCodeGenerator.GetHashCode(user));
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            AddGroupToEntityMapping(group, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(group));
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            RemoveGroupToEntityMapping(group, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow(), groupHashCodeGenerator.GetHashCode(group));
        }

        /// <inheritdoc/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserEventBufferItem<TUser>(eventId, EventAction.Add, user, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserEventBufferItem<TUser>(eventId, EventAction.Remove, user, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupEventBufferItem<TGroup>(eventId, EventAction.Add, group, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupEventBufferItem<TGroup>(eventId, EventAction.Remove, group, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(eventId, EventAction.Add, user, group, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(eventId, EventAction.Remove, user, group, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupToGroupMappingEventBufferItem<TGroup>(eventId, EventAction.Add, fromGroup, toGroup, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupToGroupMappingEventBufferItem<TGroup>(eventId, EventAction.Remove, fromGroup, toGroup, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(eventId, EventAction.Add, user, applicationComponent, accessLevel, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(eventId, EventAction.Remove, user, applicationComponent, accessLevel, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(eventId, EventAction.Add, group, applicationComponent, accessLevel, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(eventId, EventAction.Remove, group, applicationComponent, accessLevel, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new EntityTypeEventBufferItem(eventId, EventAction.Add, entityType, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new EntityTypeEventBufferItem(eventId, EventAction.Remove, entityType, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new EntityEventBufferItem(eventId, EventAction.Add, entityType, entity, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new EntityEventBufferItem(eventId, EventAction.Remove, entityType, entity, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserToEntityMappingEventBufferItem<TUser>(eventId, EventAction.Add, user, entityType, entity, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new UserToEntityMappingEventBufferItem<TUser>(eventId, EventAction.Remove, user, entityType, entity, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupToEntityMappingEventBufferItem<TGroup>(eventId, EventAction.Add, group, entityType, entity, occurredTime, hashCode);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            var newEvent = new GroupToEntityMappingEventBufferItem<TGroup>(eventId, EventAction.Remove, group, entityType, entity, occurredTime, hashCode);
            CacheEvent(newEvent);
        }
    }
}
