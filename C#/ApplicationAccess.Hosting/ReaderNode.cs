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
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationMetrics;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in a multi-reader, single-writer ApplicationAccess hosting environment which allows reading permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Note that as per remarks for <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> interval metrics are not logged for <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> methods that return <see cref="IEnumerable{T}"/>, or perform simple dictionary and set lookups (e.g. <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}.ContainsUser(TUser)">ContainsUser()</see>).  If these metrics are required, they must be logged outside of this class.  In the case of methods that return <see cref="IEnumerable{T}"/> the metric logging must wrap the code that enumerates the result.</remarks>
    public class ReaderNode<TUser, TGroup, TComponent, TAccess> : ReaderNodeBase<TUser, TGroup, TComponent, TAccess, MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderNode class.
        /// </summary>
        /// <param name="refreshStrategy">The strategy/methodology to use to refresh the contents of the reader node.</param>
        /// <param name="eventCache">Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</param>
        /// <param name="persistentReader">Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public ReaderNode(IReaderNodeRefreshStrategy refreshStrategy, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess> eventCache, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader, Boolean storeBidirectionalMappings)
            : base(refreshStrategy, eventCache, persistentReader, storeBidirectionalMappings)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderNode class.
        /// </summary>
        /// <param name="refreshStrategy">The strategy/methodology to use to refresh the contents of the reader node.</param>
        /// <param name="eventCache">Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</param>
        /// <param name="persistentReader">Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the access manager underlying the node are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public ReaderNode(IReaderNodeRefreshStrategy refreshStrategy, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess> eventCache, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader, Boolean storeBidirectionalMappings, IMetricLogger metricLogger)
            : base(refreshStrategy, eventCache, persistentReader, storeBidirectionalMappings, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderNode class.
        /// </summary>
        /// <param name="refreshStrategy">The strategy/methodology to use to refresh the contents of the reader node.</param>
        /// <param name="eventCache">Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</param>
        /// <param name="persistentReader">Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public ReaderNode(IReaderNodeRefreshStrategy refreshStrategy, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess> eventCache, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader, Boolean storeBidirectionalMappings, IMetricLogger metricLogger, Utilities.IDateTimeProvider dateTimeProvider)
            : base(refreshStrategy, eventCache, persistentReader, storeBidirectionalMappings, metricLogger, dateTimeProvider)
        {
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> InitializeAccessManager()
        {
            return new MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>(storeBidirectionalMappings, metricLogger);
        }

        #endregion
    }
}
