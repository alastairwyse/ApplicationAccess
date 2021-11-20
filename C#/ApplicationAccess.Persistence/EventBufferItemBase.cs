/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Base class for container classes which get stored in an AccessManager event buffer.
    /// </summary>
    public abstract class EventBufferItemBase
    {
        /// <summary>The action of the event.</summary>
        protected EventAction eventAction;
        /// <summary>The time that the event originally occurred.</summary>
        protected DateTime occurredTime;
        /// <summary>The ordinal sequence number of the event.</summary>
        protected Int64 sequenceNumber;

        /// <summary>
        /// The action of the event.
        /// </summary>
        public EventAction EventAction
        {
            get { return eventAction; }
        }

        /// <summary>
        /// The time that the event originally occurred.
        /// </summary>
        public DateTime OccurredTime
        {
            get { return occurredTime; }
        }

        /// <summary>
        /// The ordinal sequence number of the event.
        /// </summary>
        public Int64 SequenceNumber
        {
            get { return sequenceNumber; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.EventBufferItemBase class.
        /// </summary>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="sequenceNumber">The ordinal sequence number of the event.</param>
        public EventBufferItemBase(EventAction eventAction, DateTime occurredTime, Int64 sequenceNumber)
        {
            if (occurredTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"Parameter '{nameof(occurredTime)}' must be based in UTC (i.e. '{nameof(occurredTime.Kind)}' property must be '{nameof(DateTimeKind.Utc)}').", nameof(occurredTime));
            if (sequenceNumber < 0)
                throw new ArgumentException($"Parameter '{nameof(sequenceNumber)}' must be greater than or equal to 0.", nameof(sequenceNumber));

            this.eventAction = eventAction;
            this.occurredTime = occurredTime;
            this.sequenceNumber = sequenceNumber;
        }
    }
}
