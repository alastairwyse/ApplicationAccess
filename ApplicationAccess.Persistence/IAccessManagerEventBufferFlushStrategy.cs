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
    /// Defines a strategy/methodology for flushing events buffered in an IAccessManagerEventBuffer implementation, by tracking/monitoring the buffer contents, and raising an event to flush the buffer.
    /// </summary>
    public interface IAccessManagerEventBufferFlushStrategy
    {
        /// <summary>Occurs when the buffer is flushed... i.e. when events stored in the buffer are processed.</summary>
        event EventHandler BufferFlushed;

        /// <summary>
        /// The number of user events stored in the buffer.
        /// </summary>
        Int32 UserEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of group events stored in the buffer.
        /// </summary>
        Int32 GroupEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of user to group mapping events stored in the buffer.
        /// </summary>
        Int32 UserToGroupMappingEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of group to group mapping events stored in the buffer.
        /// </summary>
        Int32 GroupToGroupMappingEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of user to application component and access level mapping events stored in the buffer.
        /// </summary>
        Int32 UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of group to application component and access level mapping events stored in the buffer.
        /// </summary>
        Int32 GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of entity type events stored in the buffer.
        /// </summary>
        Int32 EntityTypeEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of entity events stored in the buffer.
        /// </summary>
        Int32 EntityEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of user to entity mapping events stored in the buffer.
        /// </summary>
        Int32 UserToEntityMappingEventBufferItemCount
        {
            set;
        }

        /// <summary>
        /// The number of group to entity mapping events stored in the buffer.
        /// </summary>
        Int32 GroupToEntityMappingEventBufferItemCount
        {
            set;
        }
    }
}
