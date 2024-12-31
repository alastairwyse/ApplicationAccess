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
    /// Container class for a buffered/cached group to application component and access level mapping event.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> : TemporalEventBufferItemBase
    {        
        /// <summary>The group in the mapping.</summary>
        protected TGroup group;
        /// <summary>The application component in the mapping.</summary>
        protected TComponent applicationComponent;
        /// <summary>The access level in the mapping.</summary>
        protected TAccess accessLevel;

        /// <summary>
        /// The group in the mapping.
        /// </summary>
        public TGroup Group
        {
            get { return group; }
        }

        /// <summary>
        /// The application component in the mapping.
        /// </summary>
        public TComponent ApplicationComponent
        {
            get { return applicationComponent; }
        }

        /// <summary>
        /// The access level in the mapping.
        /// </summary>
        public TAccess AccessLevel
        {
            get { return accessLevel; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.GroupToApplicationComponentAndAccessLevelMappingEventBufferItem class.
        /// </summary>
        /// <param name="eventId">A unique id for the event.</param>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The access level in the mapping.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="hashCode">The hash code for the group.</param>
        public GroupToApplicationComponentAndAccessLevelMappingEventBufferItem(Guid eventId, EventAction eventAction, TGroup group, TComponent applicationComponent, TAccess accessLevel, DateTime occurredTime, Int32 hashCode)
            : base(eventId, eventAction, occurredTime, hashCode)
        {
            this.group = group;
            this.applicationComponent = applicationComponent;
            this.accessLevel = accessLevel;
        }
    }
}
