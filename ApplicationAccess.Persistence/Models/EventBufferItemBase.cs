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

namespace ApplicationAccess.Persistence.Models
{
    /// <summary>
    /// Base class for container classes which represent a change in the structure of an AccessManager implementation, and are stored in an AccessManager event buffer or cache.
    /// </summary>
    public abstract class EventBufferItemBase
    {
        /// <summary>A unique id for the event.</summary>
        protected Guid eventId;
        /// <summary>The action of the event.</summary>
        protected EventAction eventAction;

        /// <summary>
        /// A unique id for the event.
        /// </summary>
        public Guid EventId
        {
            get { return eventId; }
        }

        /// <summary>
        /// The action of the event.
        /// </summary>
        public EventAction EventAction
        {
            get { return eventAction; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.EventBufferItemBase class.
        /// </summary>
        /// <param name="eventId">A unique id for the event.</param>
        /// <param name="eventAction">The action of the event.</param>
        public EventBufferItemBase(Guid eventId, EventAction eventAction)
        {
            this.eventId = eventId;
            this.eventAction = eventAction;
        }
    }
}
