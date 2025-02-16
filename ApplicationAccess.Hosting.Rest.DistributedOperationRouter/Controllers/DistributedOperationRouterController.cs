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

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Controllers
{
    /// <summary>
    /// Derives from abstract base controller class <see cref="DistributedOperationProcessorControllerBase"/> to expose it as a controller.
    /// </summary>
    public class DistributedOperationRouterController : DistributedOperationProcessorControllerBase
    {
        #pragma warning disable 1591

        protected IDistributedAccessManagerOperationRouter distributedOperationRouter;

        #pragma warning restore 1591

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Controllers.DistributedOperationRouterController class.
        /// </summary>
        public DistributedOperationRouterController
        (
            AsyncQueryProcessorHolder asyncQueryProcessorHolder,
            AsyncEventProcessorHolder asyncEventProcessorHolder,
            DistributedOperationRouterHolder distributedOperationRouterHolder, 
            ILogger<DistributedOperationRouterController> logger
        )
            : base(asyncQueryProcessorHolder, asyncEventProcessorHolder, logger)
        {
            this.distributedOperationRouter = distributedOperationRouterHolder.DistributedOperationRouter;
        }

        /// <summary>
        /// Switches on routing functionality on.
        /// </summary>
        [HttpPost]
        [Route("routing:switchOn")]
        [ApiExplorerSettings(GroupName = "Administration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void SetRoutingOn()
        {
            distributedOperationRouter.RoutingOn = true;
        }

        /// <summary>
        /// Switches on routing functionality off.  All operations will be routed to the source shard.
        /// </summary>
        [HttpPost]
        [Route("routing:switchOff")]
        [ApiExplorerSettings(GroupName = "Administration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void SetRoutingOff()
        {
            distributedOperationRouter.RoutingOn = false;
        }

        /// <summary>
        /// Pauses/holds any incoming operation requests.
        /// </summary>
        [HttpPost]
        [Route("operationProcessing:pause")]
        [ApiExplorerSettings(GroupName = "Administration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void PauseOperations()
        {
            distributedOperationRouter.PauseOperations();
        }

        /// <summary>
        /// Resumes any incoming operation requests following a preceding pause.
        /// </summary>
        [HttpPost]
        [Route("operationProcessing:resume")]
        [ApiExplorerSettings(GroupName = "Administration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void ResumeOperations()
        {
            distributedOperationRouter.ResumeOperations();
        }
    }
}
