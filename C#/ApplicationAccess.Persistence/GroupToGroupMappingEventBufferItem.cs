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
    /// Container class for buffered group to group mapping events.
    /// </summary>
    public class GroupToGroupMappingEventBufferItem<TGroup> : EventBufferItemBase
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
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="sequenceNumber">The ordinal sequence number of the event.</param>
        public GroupToGroupMappingEventBufferItem(EventAction eventAction, TGroup fromGroup, TGroup toGroup, DateTime occurredTime, Int64 sequenceNumber)
            : base(eventAction, occurredTime, sequenceNumber)
        {
            this.fromGroup = fromGroup;
            this.toGroup = toGroup;
        }
    }
}
