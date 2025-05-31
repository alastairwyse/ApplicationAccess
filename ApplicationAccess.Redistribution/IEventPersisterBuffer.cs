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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Defines methods for buffering <see cref="TemporalEventBufferItemBase"/> objects from two sources, persisting them when the buffer reaches a specified size.
    /// </summary>
    public interface IEventPersisterBuffer
    {
        /// <summary>
        /// Adds the specified event to the buffer, persisting the events if the maximum size of the buffer is reached.
        /// </summary>
        /// <param name="inputEvent">The event to buffer.</param>
        /// <param name="sourcedFromFirstShardGroup">Whether the event was sourced from the first shard group being merged (assumed that the event was sourced from the second shard group if set false).</param>
        /// <returns>A tuple containing: the id of the first shard group event most recently processed (null if no events have been processed from the first shard group), and the id of the second shard group event most recently processed (null if no events have been processed from the second shard group).</returns>
        Tuple<Nullable<Guid>, Nullable<Guid>> BufferEvent(TemporalEventBufferItemBase inputEvent, Boolean sourcedFromFirstShardGroup);
    }
}
