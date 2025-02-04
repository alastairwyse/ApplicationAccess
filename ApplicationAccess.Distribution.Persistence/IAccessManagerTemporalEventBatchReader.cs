/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Distribution.Persistence
{
    /// <summary>
    /// Defines methods to read events from an AccessManager instance persistent storage filtered by a shard range and read in batches.  
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerTemporalEventBatchReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Retrieves the id of the first event in the AccessManager.
        /// </summary>
        /// <returns>The <see cref="AccessManagerState"/>.</returns>
        Guid GetInitialEvent();

        /// <summary>
        /// Retrieves the id of the next event after the specified event. 
        /// </summary>
        /// <param name="inputEventId">The id of the preceding event.</param>
        /// <returns>The next event, or null of the specified event is the latest.</returns>
        Nullable<Guid> GetNextStateAfter(Guid inputEventId);

        /// <summary>
        /// Retrieves the sequence of events which follow (and include) the specified event.
        /// </summary>
        /// <param name="initialEventId">The id of the first event in the sequence.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="eventCount">The number of events to retrieve (including that specified in <paramref name="initialEventId"/>).</param>
        /// <returns>A tuple containing: the sequence of events in order of ascending date/time and including that specified in <paramref name="initialEventId"/>, and the id of the next event after the last one in the sequence (or null if the last one in the sequence is the latest).</returns>
        Tuple<IList<TemporalEventBufferItemBase>, Nullable<Guid>> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd, Int32 eventCount);

        /// <summary>
        /// Retrieves the sequence of all events which follow (and include) the specified event.
        /// </summary>
        /// <param name="initialEventId">The id of the first event in the sequence.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <returns>The sequence of events in order of ascending date/time, and including that specified in <paramref name="initialEventId"/>, or an empty list if the event represented by <paramref name="initialEventId"/> is the latest.</returns>
        IList<TemporalEventBufferItemBase> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd);
    }
}
