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
    /// Base for controllers which expose methods on the <see cref="IAccessManagerUserEventProcessor{TUser, TGroup, TComponent, TAccess}"/> interface (except the primary 'Add*' methods) as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "UserEventProcessor")]
    public abstract class UserEventProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerUserEventProcessor<String, String, String, String> userEventProcessor;
        protected ILogger<UserEventProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.UserEventProcessorControllerBase class.
        /// </summary>
        public UserEventProcessorControllerBase(UserEventProcessorHolder userEventProcessorHolder, ILogger<UserEventProcessorControllerBase> logger)
        {
            userEventProcessor = userEventProcessorHolder.UserEventProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <response code="200">The user was removed.</response>
        [HttpDelete]
        [Route("users/{user}")]
        public void RemoveUser([FromRoute] String user)
        {
            userEventProcessor.RemoveUser(user);
        }

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("userToGroupMappings/user/{user}/group/{group}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddUserToGroupMapping([FromRoute] String user, [FromRoute] String group)
        {
            userEventProcessor.AddUserToGroupMapping(user, group);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes the mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("userToGroupMappings/user/{user}/group/{group}")]
        public void RemoveUserToGroupMapping([FromRoute] String user, [FromRoute] String group)
        {
            userEventProcessor.RemoveUserToGroupMapping(user, group);
        }

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("userToApplicationComponentAndAccessLevelMappings/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddUserToApplicationComponentAndAccessLevelMapping([FromRoute] String user, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            userEventProcessor.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("userToApplicationComponentAndAccessLevelMappings/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping([FromRoute] String user, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            userEventProcessor.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("userToEntityMappings/user/{user}/entityType/{entityType}/entity/{entity}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddUserToEntityMapping([FromRoute] String user, [FromRoute] String entityType, [FromRoute] String entity)
        {
            userEventProcessor.AddUserToEntityMapping(user, entityType, entity);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("userToEntityMappings/user/{user}/entityType/{entityType}/entity/{entity}")]
        public void RemoveUserToEntityMapping([FromRoute] String user, [FromRoute] String entityType, [FromRoute] String entity)
        {
            userEventProcessor.RemoveUserToEntityMapping(user, entityType, entity);
        }
    }
}
