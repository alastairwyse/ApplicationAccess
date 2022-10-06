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
using System.Text;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Caches events written to an <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/>, and defines methods to return the events in order of occurrence.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>
    {
        // TODO: Move XML comments to InterfaceDocumentationComments.xml

        /// <summary>
        /// Retrieves all events which occurred since the event with the specified id.
        /// </summary>
        /// <param name="eventId">The id of the event to retrieve all events since.</param>
        /// <returns>An ordered list of events which occurred since the specified event, and not including the specified event.  Returned in order from least recent to most recent.</returns>
        /// <exception cref="EventNotCachedException">The event with the specified id was not found in the cache.</exception>
        IList<TemporalEventBufferItemBase> GetAllEventsSince(Guid eventId);
    }
}
