/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.Controllers;


namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public class TestController : ControllerBase
    {
        protected RoutingSwitch routingSwitch;
        protected IAccessManagerAsyncQueryProcessor<String, String, String, String> queryProcessor;
        protected IAccessManagerAsyncEventProcessor<String, String, String, String> eventProcessor;
        protected IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String> distributedQueryProcessor;
        protected ILogger<TestController> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.TestController class.
        /// </summary>
        public TestController
        (
            RoutingSwitch routingSwitch, 
            IAccessManagerAsyncQueryProcessor<String, String, String, String> queryProcessor,
            IAccessManagerAsyncEventProcessor<String, String, String, String> eventProcessor,
            IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String> distributedQueryProcessor,
            ILogger<TestController> logger
        )
        {
            this.routingSwitch = routingSwitch;
            this.queryProcessor = queryProcessor;
            this.eventProcessor = eventProcessor;
            this.distributedQueryProcessor = distributedQueryProcessor;
            this.logger = logger;
        }

        [HttpGet]
        [Route("users")]
        [ApiExplorerSettings(GroupName = "UserQueryProcessor")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IEnumerable<String>> GetUsersAsync()
        {
            return await queryProcessor.GetUsersAsync();
        }

        [HttpPost]
        [Route("userToGroupMappings/user/{user}/group/{group}")]
        [ApiExplorerSettings(GroupName = "UserEventProcessor")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddUserToGroupMappingAsync([FromRoute] String user, [FromRoute] String group)
        {
            // check if routing is on (hence need route swicth from DI)
            // either send to source node, OR decide which to send to
            // need config of both nodes
            //   can I reuse any existing model classes for this?
            // hosted service wrapper needs to read config and set on some classes



            throw new NotImplementedException();
        }

        // TODO: Implement these as first step...
        // Task<Boolean> ContainsUserAsync(String user)
        // Task<List<String>> GetGroupToGroupReverseMappingsAsync(IEnumerable<String> groups)


    }
}
