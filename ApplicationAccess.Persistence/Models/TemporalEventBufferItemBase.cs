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

namespace ApplicationAccess.Persistence.Models
{
    /// <summary>
    /// Base class for container classes which represent a historic change in the structure of an AccessManager implementation, and are stored in an AccessManager event buffer or cache.
    /// </summary>
    /// <remarks>Classes deriving from this like <see cref="UserEventBufferItem{TUser}"/> remove 'Temporal' from their name for brevity, since currently only the temporal version of the class is used for buffering and caching.</remarks>
    public abstract class TemporalEventBufferItemBase : EventBufferItemBase
    {
        /// <summary>The time that the event originally occurred.</summary>
        protected DateTime occurredTime;
        /// <summary>The hash code for the key primary element of the event.</summary>
        protected Int32 hashCode;

        /// <summary>
        /// The time that the event originally occurred.
        /// </summary>
        public DateTime OccurredTime
        {
            get { return occurredTime; }
        }

        /// <summary>
        /// The hash code for the key primary element of the event.
        /// </summary>
        public Int32 HashCode
        {
            get { return hashCode; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.TemporalEventBufferItemBase class.
        /// </summary>
        /// <param name="eventId">A unique id for the event.</param>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="hashCode">The hash code for the key primary element of the event.</param>
        public TemporalEventBufferItemBase(Guid eventId, EventAction eventAction, DateTime occurredTime, Int32 hashCode)
            : base(eventId, eventAction)
        {
            if (occurredTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"Parameter '{nameof(occurredTime)}' must be based in UTC (i.e. '{nameof(occurredTime.Kind)}' property must be '{nameof(DateTimeKind.Utc)}').", nameof(occurredTime));

            this.occurredTime = occurredTime;
            this.hashCode = hashCode;
        }
    }
}
