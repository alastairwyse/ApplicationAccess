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

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Caches a predefined number of AccessManager <see cref="TemporalEventBufferItemBase"/> events, allowing writing of events in a bulk/consolidated operation.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerTemporalEventBulkCache<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventCacheBase<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkCache class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        public AccessManagerTemporalEventBulkCache(Int32 cachedEventCount)
            : base(cachedEventCount)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkCache class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventBulkCache(Int32 cachedEventCount, IMetricLogger metricLogger)
            : base(cachedEventCount, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkCache class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public AccessManagerTemporalEventBulkCache(Int32 cachedEventCount, IMetricLogger metricLogger, Utilities.IGuidProvider guidProvider, IDateTimeProvider dateTimeProvider)
            : base(cachedEventCount, metricLogger, guidProvider, dateTimeProvider)
        {
        }

        /// <summary>
        /// Adds a sequence of events which subclass <see cref="TemporalEventBufferItemBase"/> to the cache.
        /// </summary>
        /// <param name="events">The events to cache.</param>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            Guid beginId = metricLogger.Begin(new EventsCachingTime());

            cachedEventsLock.EnterWriteLock();
            try
            {
                foreach (TemporalEventBufferItemBase currentEvent in events)
                {
                    cachedEvents.AddLast(currentEvent);
                    cachedEventsGuidIndex.Add(currentEvent.EventId, cachedEvents.Last);
                    TrimCachedEvents();
                }
            }
            finally
            {
                cachedEventsLock.ExitWriteLock();
            }

            metricLogger.End(beginId, new EventsCachingTime());
            metricLogger.Add(new EventsCached(), events.Count);
        }
    }
}
