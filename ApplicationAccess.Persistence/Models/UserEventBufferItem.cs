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

namespace ApplicationAccess.Persistence.Models
{
    /// <summary>
    /// Container class for a buffered/cached user event.
    /// </summary>
    public class UserEventBufferItem<TUser> : TemporalEventBufferItemBase
    {
        /// <summary>The user the event occured for.</summary>
        protected TUser user;

        /// <summary>
        /// The user the event occured for.
        /// </summary>
        public TUser User
        {
            get { return user; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.UserEventBufferItem class.
        /// </summary>
        /// <param name="eventId">A unique id for the event.</param>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="user">The user the event occured for.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="hashCode">The hash code for the user.</param>
        public UserEventBufferItem(Guid eventId, EventAction eventAction, TUser user, DateTime occurredTime, Int32 hashCode)
            : base(eventId, eventAction, occurredTime, hashCode)
        {
            this.user = user;
        }
    }
}
