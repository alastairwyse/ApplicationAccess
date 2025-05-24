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

namespace ApplicationAccess.Redistribution.Models
{
    /// <summary>
    /// Defines actions which can be taken during merge of shard group events, when 0 events are read from a shard group.
    /// </summary>
    public enum NoEventsReadDuringMergeAction
    {
        /// <summary>
        /// Don't merge any further events, even if subsequent events are known to exist in the other merge source.
        /// </summary>
        /// <remarks>This option should be used if it can't be guaranteed that subsequent events in the current merge source don't exist (i.e. in the case that events have been generated in a source shard group which have not been persisted yet).</remarks>
        StopMerging,

        /// <summary>
        /// Persist all subsequent events in the other merge source.
        /// </summary>
        /// <remarks>This option should be used if it can be guaranteed that subsequent events in the current merge source don't exist (i.e. in the case that operations have been paused and event buffers flushed in the source shard groups).</remarks>
        PersistAllEventsFromOtherSource
    }
}
