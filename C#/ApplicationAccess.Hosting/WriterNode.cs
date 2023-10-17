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
using ApplicationAccess.Persistence;
using ApplicationAccess.Metrics;
using ApplicationMetrics;
using System.Collections.Generic;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in a multi-reader, single-writer ApplicationAccess hosting environment which allows writing permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class WriterNode<TUser, TGroup, TComponent, TAccess> : WriterNodeBase<TUser, TGroup, TComponent, TAccess, MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which change the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventCache,
            Boolean storeBidirectionalMappings
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, eventCache, storeBidirectionalMappings)
        {
            var eventDistributor = new AccessManagerTemporalEventPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which change the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventCache,
            Boolean storeBidirectionalMappings
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, eventCache, storeBidirectionalMappings)
        {
            var eventDistributor = new AccessManagerTemporalEventBulkPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which change the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventCache,
            Boolean storeBidirectionalMappings,
            IMetricLogger metricLogger
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, eventCache, storeBidirectionalMappings, metricLogger)
        {
            var eventDistributor = new AccessManagerTemporalEventPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which change the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventCache,
            Boolean storeBidirectionalMappings,
            IMetricLogger metricLogger
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, eventCache, storeBidirectionalMappings, metricLogger)
        {
            var eventDistributor = new AccessManagerTemporalEventBulkPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor, metricLogger);
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> InitializeAccessManager(Boolean storeBidirectionalMappings)
        {
            return new MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>(storeBidirectionalMappings, metricLogger);
        }

        #endregion
    }
}
