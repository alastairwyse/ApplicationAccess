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
using ApplicationAccess.Persistence;
using ApplicationMetrics;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in an ApplicationAccess hosting environment which caches <see cref="TemporalEventBufferItemBase"/> events for <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}">ReaderNodes</see>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class TemporalEventBulkCachingNode<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The underlying object which performs caching.</summary>
        protected AccessManagerTemporalEventBulkCache<TUser, TGroup, TComponent, TAccess> eventCache;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.TemporalEventBulkCachingNode class.
        /// </summary>
        /// <param name="cachedEventCount">The number of events to retain on the cache.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public TemporalEventBulkCachingNode(Int32 cachedEventCount, IMetricLogger metricLogger)
        {
            eventCache = new AccessManagerTemporalEventBulkCache<TUser, TGroup, TComponent, TAccess>(cachedEventCount, metricLogger);
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            eventCache.PersistEvents(events);
        }

        /// <inheritdoc/>
        public IList<TemporalEventBufferItemBase> GetAllEventsSince(Guid eventId)
        {
            return eventCache.GetAllEventsSince(eventId);
        }
    }
}
