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

namespace ApplicationAccess.Persistence.MongoDb.Models.Dtos
{
    /// <summary>
    /// DTO container class for a buffered/cached group to group mapping event event.
    /// </summary>
    /// <remarks>DTO equivalent of <see cref="ApplicationAccess.Persistence.Models.GroupToGroupMappingEventBufferItem{TGroup}"/>.</remarks>
    public class GroupToGroupMappingEventBufferItem : TemporalEventBufferItemBase
    {
        /// <summary>The 'from' group in the mapping.</summary>
        public String FromGroup { get; protected set; }

        /// <summary>The 'to' group in the mapping.</summary>
        public String ToGroup { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.MongoDb.Models.Dtos.GroupToGroupMappingEventBufferItem class.
        /// </summary>
        /// <param name="eventId">A unique id for the event.</param>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="hashCode">The hash code for the user.</param>
        public GroupToGroupMappingEventBufferItem(Guid eventId, EventAction eventAction, String fromGroup, String toGroup, DateTime occurredTime, Int32 hashCode)
            : base(eventId, eventAction, occurredTime, hashCode)
        {
            this.FromGroup = fromGroup;
            this.ToGroup = toGroup;
        }
    }
}
