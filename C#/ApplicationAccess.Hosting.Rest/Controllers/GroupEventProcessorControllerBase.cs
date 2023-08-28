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
    /// Base for controller which exposes methods on the <see cref="IAccessManagerGroupEventProcessor{TGroup, TComponent, TAccess}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "GroupEventProcessor")]
    public abstract class GroupEventProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerGroupEventProcessor<String, String, String> groupEventProcessor;
        protected ILogger<GroupEventProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.GroupEventProcessorControllerBase class.
        /// </summary>
        public GroupEventProcessorControllerBase(GroupEventProcessorHolder groupEventProcessorHolder, ILogger<GroupEventProcessorControllerBase> logger)
        {
            groupEventProcessor = groupEventProcessorHolder.GroupEventProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <response code="200">The group was removed.</response>
        [HttpDelete]
        [Route("groups/{group}")]
        public void RemoveGroup([FromRoute] String group)
        {
            groupEventProcessor.RemoveGroup(group);
        }

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("groupToApplicationComponentAndAccessLevelMappings/group/{group}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddGroupToApplicationComponentAndAccessLevelMapping([FromRoute] String group, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            groupEventProcessor.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("groupToApplicationComponentAndAccessLevelMappings/group/{group}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping([FromRoute] String group, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            groupEventProcessor.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("groupToEntityMappings/group/{group}/entityType/{entityType}/entity/{entity}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddGroupToEntityMapping([FromRoute] String group, [FromRoute] String entityType, [FromRoute] String entity)
        {
            groupEventProcessor.AddGroupToEntityMapping(group, entityType, entity);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("groupToEntityMappings/group/{group}/entityType/{entityType}/entity/{entity}")]
        public void RemoveGroupToEntityMapping([FromRoute] String group, [FromRoute] String entityType, [FromRoute] String entity)
        {
            groupEventProcessor.RemoveGroupToEntityMapping(group, entityType, entity);
        }
    }
}
