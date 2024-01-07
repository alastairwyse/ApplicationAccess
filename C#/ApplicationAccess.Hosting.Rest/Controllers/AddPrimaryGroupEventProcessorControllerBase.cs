/*
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

using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose primary 'Add*' group event methods as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "GroupEventProcessor")]
    public abstract class AddPrimaryGroupEventProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerGroupEventProcessor<String, String, String> groupEventProcessor;
        protected ILogger<GroupEventProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.AddPrimaryGroupEventProcessorControllerBase class.
        /// </summary>
        public AddPrimaryGroupEventProcessorControllerBase(GroupEventProcessorHolder groupEventProcessorHolder, ILogger<GroupEventProcessorControllerBase> logger)
        {
            groupEventProcessor = groupEventProcessorHolder.GroupEventProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <response code="201">The group was added.</response>
        [HttpPost]
        [Route("groups/{group}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddGroup([FromRoute] String group)
        {
            groupEventProcessor.AddGroup(group);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }
    }
}
