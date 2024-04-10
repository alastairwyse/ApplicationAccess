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
    /// Container class for a buffered/cached group to group mapping event.
    /// </summary>
    public class GroupToGroupMappingEventBufferItem<TGroup> : TemporalEventBufferItemBase
    {
        /// <summary>The 'from' group in the mapping.</summary>
        protected TGroup fromGroup;
        /// <summary>The 'to' group in the mapping.</summary>
        protected TGroup toGroup;

        /// <summary>
        /// The 'from' group in the mapping.
        /// </summary>
        public TGroup FromGroup
        {
            get { return fromGroup; }
        }

        /// <summary>
        /// The 'to' group in the mapping.
        /// </summary>
        public TGroup ToGroup
        {
            get { return toGroup; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.GroupToGroupMappingEventBufferItem class.
        /// </summary>
        /// <param name="eventId">A unique id for the event.</param>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        public GroupToGroupMappingEventBufferItem(Guid eventId, EventAction eventAction, TGroup fromGroup, TGroup toGroup, DateTime occurredTime)
            : base(eventId, eventAction, occurredTime)
        {
            this.fromGroup = fromGroup;
            this.toGroup = toGroup;
        }
    }
}
