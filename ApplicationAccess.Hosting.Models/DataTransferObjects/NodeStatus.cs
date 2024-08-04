/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Hosting.Models.DataTransferObjects
{
    /// <summary>
    /// DTO container class holding the status of a running hosted ApplicationAccess node.
    /// </summary>
    public class NodeStatus
    {
        /// <summary>The UTC time the node was started.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.DataTransferObject.NodeStatus class.
        /// </summary>
        /// <param name="startTime">The UTC time the node was started.</param>
        public NodeStatus(DateTime startTime)
        {
            this.StartTime = startTime;
        }
    }
}
