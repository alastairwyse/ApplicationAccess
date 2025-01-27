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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.Controllers;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Controllers
{
    /// <summary>
    /// Derives from abstract base controller class <see cref="DistributedOperationProcessorControllerBase"/> to expose it as a controller.
    /// </summary>
    public class DistributedOperationRouterController : DistributedOperationProcessorControllerBase
    {
        protected IDistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> distributedOperationRouter;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Controllers.DistributedOperationRouterController class.
        /// </summary>
        public DistributedOperationRouterController
        (
            AsyncQueryProcessorHolder asyncQueryProcessorHolder,
            AsyncEventProcessorHolder asyncEventProcessorHolder,
            IDistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> distributedOperationRouter, 
            ILogger<DistributedOperationProcessorControllerBase> logger
        )
            : base(asyncQueryProcessorHolder, asyncEventProcessorHolder, logger)
        {
            this.distributedOperationRouter = distributedOperationRouter;
        }

        /// <summary>
        /// Whether or not the routing functionality is switched on.  If false (off) all operations are routed to the source shard.
        /// </summary>
        /// <param name="value">Whether or not the routing functionality is switched on.</param>
        [HttpPost]
        [Route("routing/{value}")]
        [ApiExplorerSettings(GroupName = "Routing")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public void SetRoutingOn([FromRoute] Boolean value)
        {
            distributedOperationRouter.RoutingOn = value;
        }

        // TODO: Need endpoints for switching pause/hold on and off
        //   And hence also need to get Pauser AND register it in DI services
        //     Unless the middleware is already registering it
        //     Actually, I need to register that pauser middleware too
        //     AND integration tests needs to include stuff around the pauser
        //   AND... do I need to replcate tests in class DistributedOperationCoordinatorNodeTests ??
        //     Basically just pass-through tests... are they really required??
    }
}
