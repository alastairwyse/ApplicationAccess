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
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IDistributedAccessManagerUserQueryProcessor{TUser, TGroup}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "UserQueryProcessor")]
    public abstract class DistributedUserQueryProcessorControllerBase : ControllerBase
    {
        protected IDistributedAccessManagerUserQueryProcessor<String, String> distributedUserQueryProcessor;
        protected ILogger<DistributedUserQueryProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.DistributedUserQueryProcessorControllerBase class.
        /// </summary>
        public DistributedUserQueryProcessorControllerBase(DistributedUserQueryProcessorHolder distributedUserQueryProcessorHolder, ILogger<DistributedUserQueryProcessorControllerBase> logger)
        {
            distributedUserQueryProcessor = distributedUserQueryProcessorHolder.DistributedUserQueryProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the users that are directly mapped to any of the specified groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the users for.</param>
        /// <returns>A collection of users that are mapped to the specified groups.</returns>
        [HttpGet]
        [Route("userToGroupMappings")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<String> GetGroupToUserMappings([FromBody, BindRequired] IEnumerable<String> groups)
        {
            return distributedUserQueryProcessor.GetGroupToUserMappings(groups);
        }
    }
}
