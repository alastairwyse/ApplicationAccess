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

using System.Collections.Generic;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> which writes AccessManager events to an internal list which can be retrieved later.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>This class is intended to facilitate testing.</remarks>
    public class ListAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The list which holds all the written events.</summary>
        protected List<TemporalEventBufferItemBase> eventsList;

        /// <summary>
        /// The events written by the persister.
        /// </summary>
        public List<TemporalEventBufferItemBase> Events
        {
            get { return eventsList; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.ListAccessManagerTemporalBulkPersister class.
        /// </summary>
        public ListAccessManagerTemporalBulkPersister()
        {
            eventsList = new List<TemporalEventBufferItemBase>();
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            eventsList.AddRange(events);
        }
    }
}
