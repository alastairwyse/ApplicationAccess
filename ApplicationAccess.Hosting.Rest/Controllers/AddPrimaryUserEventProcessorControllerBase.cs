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
    /// Base for controllers which expose primary 'Add*' user event methods as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "UserEventProcessor")]
    public abstract class AddPrimaryUserEventProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerUserEventProcessor<String, String, String, String> userEventProcessor;
        protected ILogger<UserEventProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.AddPrimaryUserEventProcessorControllerBase class.
        /// </summary>
        public AddPrimaryUserEventProcessorControllerBase(UserEventProcessorHolder userEventProcessorHolder, ILogger<UserEventProcessorControllerBase> logger)
        {
            userEventProcessor = userEventProcessorHolder.UserEventProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <response code="201">The user was added.</response>
        [HttpPost]
        [Route("users/{user}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddUser([FromRoute] String user)
        {
            userEventProcessor.AddUser(Uri.UnescapeDataString(user));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }
    }
}
