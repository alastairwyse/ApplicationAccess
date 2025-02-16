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
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApplicationAccess.Distribution.Persistence;

namespace ApplicationAccess.Hosting.Rest.DistributedWriter.Controllers
{
    /// <summary>
    /// Exposes admin methods for REST-hosted DistributedWriters.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public class DistributedWriterController : ControllerBase
    {
        // TODO: If there are ever multiple versions of DistributedWriters, should make this abstract and move to ApplicationAccess.Hosting.Rest.Controllers, following the smae pattern as used by other controllers.

        #pragma warning disable 1591

        protected IManuallyFlushableBufferFlushStrategy manuallyFlushableBufferFlushStrategy;
        protected RequestCounter requestCounter;

        #pragma warning restore 1591

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedWriter.Controllers.DistributedWriterController class.
        /// </summary>
        public DistributedWriterController(ManuallyFlushableBufferFlushStrategyHolder manuallyFlushableBufferFlushStrategyHolder, RequestCounter requestCounter)
        {
            this.manuallyFlushableBufferFlushStrategy = manuallyFlushableBufferFlushStrategyHolder.ManuallyFlushableBufferFlushStrategy;
            this.requestCounter = requestCounter;
        }

        /// <summary>
        /// Flushes the event buffer(s).
        /// </summary>
        [HttpPost]
        [Route("eventBuffer:flush")]
        [ApiExplorerSettings(GroupName = "Administration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void FlushEventBuffers()
        {
            manuallyFlushableBufferFlushStrategy.FlushBuffers();
        }

        /// <summary>
        /// Returns the number of active requests being processed.
        /// </summary>
        /// <returns>The number of active requests being processed.</returns>
        [HttpGet]
        [Route("activeRequests:count")]
        [ApiExplorerSettings(GroupName = "Administration")]
        [Produces(MediaTypeNames.Application.Json)]
        public Int32 GetActiveRequestCount()
        {
            // The value from the request counter will include this request, so need to decrement before returning
            return requestCounter.CounterValue - 1;
        }
    }
}
