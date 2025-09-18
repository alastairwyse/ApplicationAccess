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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence.MongoDb.Models.Dtos
{
    /// <summary>
    /// Base for DTO container classes which represent a change in the structure of an AccessManager implementation, and are stored in MongoDB.
    /// </summary>
    /// <remarks>DTO equivalent of <see cref="ApplicationAccess.Persistence.Models.TemporalEventBufferItemBase"/>.</remarks>
    public abstract class TemporalEventBufferItemBase
    {
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid _id
        {
            get { return EventId; }
        }

        /// <summary>The time that the event originally occurred.</summary>
        public DateTime OccurredTime { get; protected set; }

        /// <summary>The hash code for the key primary element of the event.</summary>
        public Int32 HashCode { get; protected set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        /// <summary>A unique id for the event.</summary>
        public Guid EventId { get; protected set; }

        /// <summary>The action of the event.</summary>
        public EventAction EventAction { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.MongoDb.Models.Dtos.TemporalEventBufferItemBase class.
        /// </summary>
        /// <param name="eventId">A unique id for the event.</param>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="hashCode">The hash code for the key primary element of the event.</param>
        public TemporalEventBufferItemBase(Guid eventId, EventAction eventAction, DateTime occurredTime, Int32 hashCode)
        {
            this.OccurredTime = occurredTime;
            this.HashCode = hashCode;
            this.EventId = eventId;
            this.EventAction = eventAction;
        }
    }
}
