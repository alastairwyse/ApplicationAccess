﻿/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

using ApplicationAccess.Hosting.Rest.Controllers;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter.Controllers
{
    /// <summary>
    /// Derives from abstract base controller class <see cref="GroupToGroupEventProcessorControllerBase"/> to expose it as a controller.
    /// </summary>
    public class GroupToGroupEventProcessorController : GroupToGroupEventProcessorControllerBase
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ReaderWriter.Controllers.GroupToGroupEventProcessorController class.
        /// </summary>
        public GroupToGroupEventProcessorController(GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder, ILogger<GroupToGroupEventProcessorController> logger)
            : base(groupToGroupEventProcessorHolder, logger)
        {
        }
    }
}
