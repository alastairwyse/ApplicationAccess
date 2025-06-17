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
using ApplicationAccess.Distribution;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in a distributed ApplicationAccess hosting environment which allows writing permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class DistributedWriterNode<TUser, TGroup, TComponent, TAccess> : WriterNodeBase<TUser, TGroup, TComponent, TAccess, DistributedAccessManager<TUser, TGroup, TComponent, TAccess>>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DistributedWriterNode class.
        /// </summary>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which change the AccessManager.</param>
        public DistributedWriterNode
        (
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventCache
        ) : base(eventBufferFlushStrategy, persistentReader)
        {
            var eventDistributor = new AccessManagerTemporalEventBulkPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>
            (
                eventValidator,
                eventBufferFlushStrategy,
                userHashCodeGenerator,
                groupHashCodeGenerator,
                entityTypeHashCodeGenerator,
                eventDistributor
            );
            // Set the event buffer back on the access manager to handle any prepended primary 'add' events
            concurrentAccessManager.EventProcessor = eventBuffer;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DistributedWriterNode class.
        /// </summary>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which change the AccessManager.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedWriterNode
        (
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventCache,
            IMetricLogger metricLogger
        ) : base(eventBufferFlushStrategy, persistentReader, metricLogger)
        {
            var eventDistributor = new AccessManagerTemporalEventBulkPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>>() { eventPersister, eventCache }
            );
            eventBuffer = new DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>
            (
                eventValidator,
                eventBufferFlushStrategy,
                userHashCodeGenerator,
                groupHashCodeGenerator,
                entityTypeHashCodeGenerator,
                eventDistributor, 
                metricLogger
            );
            // Set the event buffer back on the access manager to handle any prepended primary 'add' events
            concurrentAccessManager.EventProcessor = eventBuffer;
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override DistributedAccessManager<TUser, TGroup, TComponent, TAccess> InitializeAccessManager()
        {
            return new DistributedAccessManager<TUser, TGroup, TComponent, TAccess>(metricLogger);
        }

        #endregion
    }
}
