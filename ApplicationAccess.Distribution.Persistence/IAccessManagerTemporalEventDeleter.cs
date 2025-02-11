﻿/*
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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Distribution.Persistence
{
    /// <summary>
    /// Defines methods which permanently delete events from an AccessManager instance persistent storage.
    /// </summary>
    public interface IAccessManagerTemporalEventDeleter
    {
        /// <summary>
        /// Permanently deletes all events within a specified hash code range from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="includeGroupEvents">Whether to delete <see cref="GroupEventBufferItem{TGroup}">group events</see>.</param>
        void DeleteEvents(Int32 hashRangeStart, Int32 hashRangeEnd, Boolean includeGroupEvents);
    }
}
