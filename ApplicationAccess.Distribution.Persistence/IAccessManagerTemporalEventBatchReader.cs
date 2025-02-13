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
    public interface IAccessManagerTemporalEventBatchReader
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
        Nullable<Guid> GetNextEventAfter(Guid inputEventId);

        /// <summary>
        /// Retrieves the sequence of events which follow (and potentially include) the specified event.
        /// </summary>
        /// <param name="initialEventId">The id of the earliest event in the sequence.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will return all group events if set to false.</param>
        /// <param name="eventCount">The number of events to retrieve (including that specified in <paramref name="initialEventId"/>).</param>
        /// <returns>The sequence of events in order of ascending date/time, and including that specified in <paramref name="initialEventId"/>, or an empty list if the event represented by <paramref name="initialEventId"/> is the latest.</returns>
        /// <remarks>If the event specified in parameter <paramref name="initialEventId"/> is not within the specified hash code range, then the first event in the returned sequence will be the first event after <paramref name="initialEventId"/> which falls within the hash code range.</remarks>
        IList<TemporalEventBufferItemBase> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd, Boolean filterGroupEventsByHashRange, Int32 eventCount);

        /// <summary>
        /// Retrieves the sequence of all events which follow (and potentially include) the specified event.
        /// </summary>
        /// <param name="initialEventId">The id of the earliest event in the sequence.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will return all group events if set to false.</param>
        /// <returns>The sequence of events in order of ascending date/time, and including that specified in <paramref name="initialEventId"/>, or an empty list if the event represented by <paramref name="initialEventId"/> is the latest.</returns>
        /// <remarks>If the event specified in parameter <paramref name="initialEventId"/> is not within the specified hash code range, then the first event in the returned sequence will be the first event after <paramref name="initialEventId"/> which falls within the hash code range.</remarks>
        IList<TemporalEventBufferItemBase> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd, Boolean filterGroupEventsByHashRange);
    }
}
