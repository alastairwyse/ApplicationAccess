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

namespace ApplicationAccess.Persistence.Models
{
    /// <summary>
    /// Container class holding properties which uniquely identify the persisted state of an AccessManager.
    /// </summary>
    /// <remarks>A unique persisted state can be identified by either the <see cref="EventId"/> property, or the <see cref="StateTime"/> and <see cref="StateSequence"/> properties in combination.</remarks>
    public class AccessManagerState
    {
        /// <summary>
        /// The unique id of the most recent event persisted into the access manager at the returned state.
        /// </summary>
        public Guid EventId { get; protected set; }

        /// <summary>
        /// The UTC timestamp the most recent event persisted into the access manager at the returned state occurred at.
        /// </summary>
        public DateTime StateTime { get; protected set; }

        /// <summary>
        /// Sequence number used to distinguish events which occured at the same <see cref="StateTime"/> (higher numbers indicate later events.).
        /// </summary>
        public Int32 StateSequence { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerState class.
        /// </summary>
        /// <param name="eventId">The unique id of the most recent event persisted into the access manager at the returned state.</param>
        /// <param name="stateTime">The UTC timestamp the most recent event persisted into the access manager at the returned state occurred at.</param>
        /// <param name="stateSequence">Sequence number used to distinguish events which occured at the same <see cref="StateTime"/> (higher numbers indicate later events.).</param>
        /// <exception cref="ArgumentException">Parameter '<paramref name="stateTime"/>' must be expressed as UTC.</exception>
        public AccessManagerState(Guid eventId, DateTime stateTime, Int32 stateSequence)
        {
            if (stateTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' must be expressed as UTC.", nameof(stateTime));

            EventId = eventId;
            StateTime = stateTime;
            StateSequence = stateSequence;
        }
    }
}
