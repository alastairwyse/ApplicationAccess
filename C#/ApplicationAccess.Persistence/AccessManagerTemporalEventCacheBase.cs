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
using System.Collections.Generic;
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Base for classes which cache a predefined number of AccessManager <see cref="TemporalEventBufferItemBase"/> events.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class AccessManagerTemporalEventCacheBase<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess>
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
        protected Utilities.IDateTimeProvider dateTimeProvider;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCacheBase class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        public AccessManagerTemporalEventCacheBase(Int32 cachedEventCount)
        {
            if (cachedEventCount < 1)
                throw new ArgumentOutOfRangeException(nameof(cachedEventCount), $"Parameter '{nameof(cachedEventCount)}' must be greater than or equal to 1.");

            this.cachedEventCount = cachedEventCount;
            cachedEvents = new LinkedList<TemporalEventBufferItemBase>();
            cachedEventsGuidIndex = new Dictionary<Guid, LinkedListNode<TemporalEventBufferItemBase>>();
            metricLogger = new NullMetricLogger();
            guidProvider = new Utilities.DefaultGuidProvider();
            dateTimeProvider = new DefaultDateTimeProvider();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCacheBase class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventCacheBase(Int32 cachedEventCount, IMetricLogger metricLogger)
            : this(cachedEventCount)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventCacheBase class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventCacheBase(Int32 cachedEventCount, IMetricLogger metricLogger, Utilities.IGuidProvider guidProvider, Utilities.IDateTimeProvider dateTimeProvider)
            : this(cachedEventCount, metricLogger)
        {
            this.guidProvider = guidProvider;
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <inheritdoc/>
        public IList<TemporalEventBufferItemBase> GetAllEventsSince(Guid eventId)
        {
            var returnList = new List<TemporalEventBufferItemBase>();
            Guid beginId = metricLogger.Begin(new CachedEventsReadTime());
            lock (cachedEvents)
            {
                if (cachedEvents.Count == 0)
                {
                    metricLogger.CancelBegin(beginId, new CachedEventsReadTime());
                    throw new EventCacheEmptyException("The event cache is empty.");
                }
                if (cachedEventsGuidIndex.ContainsKey(eventId) == false)
                {
                    metricLogger.CancelBegin(beginId, new CachedEventsReadTime());
                    throw new EventNotCachedException($"No event with {nameof(eventId)} '{eventId}' was found in the cache.");
                }
                    
                LinkedListNode<TemporalEventBufferItemBase> currentNode = cachedEventsGuidIndex[eventId];
                currentNode = currentNode.Next;
                while (currentNode != null)
                {
                    returnList.Add(currentNode.Value);
                    currentNode = currentNode.Next;
                }
            }
            metricLogger.End(beginId, new CachedEventsReadTime());
            metricLogger.Add(new CachedEventsRead(), returnList.Count);

            return returnList;
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
