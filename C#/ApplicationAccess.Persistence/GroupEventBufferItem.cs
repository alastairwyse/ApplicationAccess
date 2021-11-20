﻿/*
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
    /// Container class for buffered group events.
    /// </summary>
    public class GroupEventBufferItem<TGroup> : EventBufferItemBase
    {
        /// <summary>The group the event occured for.</summary>
        protected TGroup group;

        /// <summary>
        /// The group the event occured for.
        /// </summary>
        public TGroup Group
        {
            get { return group; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.GroupEventBufferItem class.
        /// </summary>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="group">The group the event occured for.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="sequenceNumber">The ordinal sequence number of the event.</param>
        public GroupEventBufferItem(EventAction eventAction, TGroup group, DateTime occurredTime, Int64 sequenceNumber)
            : base(eventAction, occurredTime, sequenceNumber)
        {
            this.group = group;
        }
    }
}
