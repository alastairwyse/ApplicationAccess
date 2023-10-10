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
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in a single-reader, single-writer ApplicationAccess hosting environment which handles both reading and writing of permissions and authorizations for an application, and supports the features described in the <see cref="DependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> documentation (idempotency etc...).
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Note that as per remarks for <see cref="MetricLoggingDependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> interval metrics are not logged for <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> methods that return <see cref="IEnumerable{T}"/>, or perform simple dictionary and set lookups (e.g. <see cref="MetricLoggingDependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}.ContainsUser(TUser)">ContainsUser()</see>).  If these metrics are required, they must be logged outside of this class.  In the case of methods that return <see cref="IEnumerable{T}"/> the metric logging must wrap the code that enumerates the result.</remarks>
    public class DependencyFreeReaderWriterNode<TUser, TGroup, TComponent, TAccess> : ReaderWriterNodeBase<TUser, TGroup, TComponent, TAccess, MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DependencyFreeReaderWriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeReaderWriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            Boolean storeBidirectionalMappings
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, storeBidirectionalMappings)
        {
            // Set the event buffer back on the access manager to handle any prepended primary 'add' events
            concurrentAccessManager.EventProcessor = eventBuffer;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DependencyFreeReaderWriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeReaderWriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            Boolean storeBidirectionalMappings
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, storeBidirectionalMappings)
        {
            // Set the event buffer back on the access manager to handle any prepended primary 'add' events
            concurrentAccessManager.EventProcessor = eventBuffer;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DependencyFreeReaderWriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeReaderWriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            Boolean storeBidirectionalMappings,
            IMetricLogger metricLogger
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, storeBidirectionalMappings, metricLogger)
        {
            // Set the event buffer back on the access manager to handle any prepended primary 'add' events
            concurrentAccessManager.EventProcessor = eventBuffer;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DependencyFreeReaderWriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeReaderWriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            Boolean storeBidirectionalMappings,
            IMetricLogger metricLogger
        ) : base(eventBufferFlushStrategy, persistentReader, eventPersister, storeBidirectionalMappings, metricLogger)
        {
            // Set the event buffer back on the access manager to handle any prepended primary 'add' events
            concurrentAccessManager.EventProcessor = eventBuffer;
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess> InitializeAccessManager(Boolean storeBidirectionalMappings)
        {
            return new MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>(storeBidirectionalMappings, metricLogger);
        }

        #endregion
    }
}
