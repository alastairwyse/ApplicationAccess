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

using System.Collections.Generic;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// Distributes methods calls syncronously to multiple <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instances.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerTemporalEventBulkPersister instances.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerTemporalEventBulkPersister instances.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerTemporalEventBulkPersister instances.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to a component.</typeparam>
    public class AccessManagerTemporalEventBulkPersisterDistributor<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Holds the <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</summary>
        protected List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.AccessManagerTemporalEventBulkPersisterDistributor class.
        /// </summary>
        /// <param name="eventPersisters">The <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</param>
        public AccessManagerTemporalEventBulkPersisterDistributor(IEnumerable<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>> eventPersisters)
        {
            this.eventPersisters = new List<IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>>(eventPersisters);
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            foreach (var currentEventPersister in eventPersisters)
            {
                currentEventPersister.PersistEvents(events);
            }
        }
    }
}
