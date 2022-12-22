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
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Caches a predefined number of <see cref="EventBufferItemBase"/> events.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The number of events to retain on the cache.</summary>
        protected Int32 cachedEventCount;
        /// <summary>Holds all cached events, with the <see cref="LinkedList{T}.Last">Last</see> property holding the most recently cached.</summary>
        protected LinkedList<TemporalEventBufferItemBase> cachedEvents;
        /// <summary>Holds the <see cref="LinkedListNode{T}"/> wrapping each cached event, indexed by its <see cref="EventBufferItemBase.EventId">EventId</see> property.</summary>
        protected Dictionary<Guid, LinkedListNode<TemporalEventBufferItemBase>> cachedEventsGuidIndex;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>The provider to use for random Guids.</summary>
        protected Utilities.IGuidProvider guidProvider;
        /// <summary>The provider to use for the current date and time.</summary>
        protected IDateTimeProvider dateTimeProvider;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCache class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        public AccessManagerTemporalEventCache(Int32 cachedEventCount)
        {
            if (cachedEventCount < 1)
                throw new ArgumentOutOfRangeException(nameof(cachedEventCount), $"Parameter '{nameof(cachedEventCount)}' must be greater than or equal to 1.");

            this.cachedEventCount = cachedEventCount;
            cachedEvents = new LinkedList<TemporalEventBufferItemBase>();
            cachedEventsGuidIndex = new Dictionary<Guid, LinkedListNode<TemporalEventBufferItemBase>>();
            metricLogger = new NullMetricLogger();
            guidProvider = new Utilities.DefaultGuidProvider();
            dateTimeProvider = new DefaultDateTimeProvider();
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCache class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventCache(Int32 cachedEventCount, IMetricLogger metricLogger)
            : this(cachedEventCount)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCache class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventCache(Int32 cachedEventCount, IMetricLogger metricLogger, Utilities.IGuidProvider guidProvider, IDateTimeProvider dateTimeProvider)
            : this(cachedEventCount, metricLogger)
        {
            this.guidProvider = guidProvider;
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            AddUser(user, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            RemoveUser(user, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            AddGroup(group, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            RemoveGroup(group, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            AddUserToGroupMapping(user, group, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            RemoveUserToGroupMapping(user, group, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            AddGroupToGroupMapping(fromGroup, toGroup, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            RemoveGroupToGroupMapping(fromGroup, toGroup, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            AddEntityType(entityType, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            RemoveEntityType(entityType, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            AddEntity(entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            RemoveEntity(entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            AddUserToEntityMapping(user, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            RemoveUserToEntityMapping(user, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            AddGroupToEntityMapping(group, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            RemoveGroupToEntityMapping(group, entityType, entity, guidProvider.NewGuid(), dateTimeProvider.UtcNow());
        }

        /// <inheritdoc/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserEventBufferItem<TUser>(eventId, EventAction.Add, user, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserEventBufferItem<TUser>(eventId, EventAction.Remove, user, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupEventBufferItem<TGroup>(eventId, EventAction.Add, group, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupEventBufferItem<TGroup>(eventId, EventAction.Remove, group, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(eventId, EventAction.Add, user, group, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserToGroupMappingEventBufferItem<TUser, TGroup>(eventId, EventAction.Remove, user, group, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupToGroupMappingEventBufferItem<TGroup>(eventId, EventAction.Add, fromGroup, toGroup, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupToGroupMappingEventBufferItem<TGroup>(eventId, EventAction.Remove, fromGroup, toGroup, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(eventId, EventAction.Add, user, applicationComponent, accessLevel, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(eventId, EventAction.Remove, user, applicationComponent, accessLevel, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(eventId, EventAction.Add, group, applicationComponent, accessLevel, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(eventId, EventAction.Remove, group, applicationComponent, accessLevel, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new EntityTypeEventBufferItem(eventId, EventAction.Add, entityType, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new EntityTypeEventBufferItem(eventId, EventAction.Remove, entityType, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new EntityEventBufferItem(eventId, EventAction.Add, entityType, entity, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new EntityEventBufferItem(eventId, EventAction.Remove, entityType, entity, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserToEntityMappingEventBufferItem<TUser>(eventId, EventAction.Add, user, entityType, entity, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new UserToEntityMappingEventBufferItem<TUser>(eventId, EventAction.Remove, user, entityType, entity, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupToEntityMappingEventBufferItem<TGroup>(eventId, EventAction.Add, group, entityType, entity, occurredTime);
            CacheEvent(newEvent);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            var newEvent = new GroupToEntityMappingEventBufferItem<TGroup>(eventId, EventAction.Remove, group, entityType, entity, occurredTime);
            CacheEvent(newEvent);
        }

        /// <summary>
        /// Adds a sequence of events which subclass <see cref="TemporalEventBufferItemBase"/> to the cache.
        /// </summary>
        /// <param name="events">The events to cache.</param>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            lock (cachedEvents)
            {
                foreach (TemporalEventBufferItemBase currentEvent in events)
                {
                    cachedEvents.AddLast(currentEvent);
                    cachedEventsGuidIndex.Add(currentEvent.EventId, cachedEvents.Last);
                    TrimCachedEvents();
                    metricLogger.Increment(new EventCached());
                }
            }
        }

        /// <inheritdoc/>
        public IList<TemporalEventBufferItemBase> GetAllEventsSince(Guid eventId)
        {
            lock (cachedEvents)
            {
                if (cachedEventsGuidIndex.ContainsKey(eventId) == false)
                    throw new EventNotCachedException($"No event with {nameof(eventId)} '{eventId}' was found in the cache.");

                var returnList = new List<TemporalEventBufferItemBase>();
                LinkedListNode<TemporalEventBufferItemBase> currentNode = cachedEventsGuidIndex[eventId];
                currentNode = currentNode.Next;
                while (currentNode != null)
                {
                    returnList.Add(currentNode.Value);
                    currentNode = currentNode.Next;
                }
                metricLogger.Add(new CachedEventsRead(), returnList.Count);

                return returnList;
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Adds the specified event to the cache structures.
        /// </summary>
        /// <param name="newEvent">The event to add.</param>
        protected void CacheEvent(TemporalEventBufferItemBase newEvent)
        {
            lock (cachedEvents)
            {
                cachedEvents.AddLast(newEvent);
                cachedEventsGuidIndex.Add(newEvent.EventId, cachedEvents.Last);
                TrimCachedEvents();
                metricLogger.Increment(new EventCached());
            }
        }

        /// <summary>
        /// Trims any events in excess of the 'cachedEventCount' property from the cache structures.
        /// </summary>
        protected void TrimCachedEvents()
        {
            while (cachedEvents.Count > cachedEventCount)
            {
                cachedEventsGuidIndex.Remove(cachedEvents.First.Value.EventId);
                cachedEvents.RemoveFirst();
            }
        }

        #endregion
    }
}
