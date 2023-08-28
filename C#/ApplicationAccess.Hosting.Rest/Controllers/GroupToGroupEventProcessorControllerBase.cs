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
    /// Base for controller which exposes methods on the <see cref="IAccessManagerGroupToGroupEventProcessor{TGroup}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "GroupToGroupEventProcessor")]
    public abstract class GroupToGroupEventProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerGroupToGroupEventProcessor<String> groupToGroupEventProcessor;
        protected ILogger<GroupToGroupEventProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.GroupToGroupEventProcessorControllerBase class.
        /// </summary>
        public GroupToGroupEventProcessorControllerBase(GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder, ILogger<GroupToGroupEventProcessorControllerBase> logger)
        {
            groupToGroupEventProcessor = groupToGroupEventProcessorHolder.GroupToGroupEventProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("groupToGroupMappings/fromGroup/{fromGroup}/toGroup/{toGroup}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddGroupToGroupMapping([FromRoute] String fromGroup, [FromRoute] String toGroup)
        {
            groupToGroupEventProcessor.AddGroupToGroupMapping(fromGroup, toGroup);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes the mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("groupToGroupMappings/fromGroup/{fromGroup}/toGroup/{toGroup}")]
        public void RemoveGroupToGroupMapping([FromRoute] String fromGroup, [FromRoute] String toGroup)
        {
            groupToGroupEventProcessor.RemoveGroupToGroupMapping(fromGroup, toGroup);
        }

    }
}
