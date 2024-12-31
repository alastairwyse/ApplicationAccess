/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Specialization of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> which includes an overload of the <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}.PersistEvents(IList{TemporalEventBufferItemBase})">PersistEvents()</see> method which allows specifying whether events which have already been persisted should be ignored.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Writes a series of events to persistent storage.
        /// </summary>
        /// <param name="events">The events to write.</param>
        /// <param name="ignorePreExistingEvents">Whether events in <paramref name="events"/> which have already been persisted will be ignored/excluded (i.e. not persisted again).  If set to false, an exception will be thrown on encountering events which have already been persisted.</param>
        void PersistEvents(IList<TemporalEventBufferItemBase> events, Boolean ignorePreExistingEvents);
    }
}
