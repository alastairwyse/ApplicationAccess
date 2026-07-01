/*
 * Copyright 2026 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApplicationAccess.Distribution.Persistence;

namespace ApplicationAccess.Hosting.Rest.DistributedReader.Controllers
{    
    /// <summary>
    /// Exposes admin methods for REST-hosted DistributedReaders.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public class DistributedReaderController : ControllerBase
    {
        #pragma warning disable 1591

        protected IManuallyLoadableComponent manuallyLoadableComponent;

        #pragma warning restore 1591

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedReader.Controllers.DistributedReaderController class.
        /// </summary>
        public DistributedReaderController(ManuallyLoadableComponentHolder manuallyLoadableComponentHolder)
        {
            this.manuallyLoadableComponent = manuallyLoadableComponentHolder.ManuallyLoadableComponent;
        }

        // TODO: Below method is copied verbatim in ApplicationAccess.Hosting.Rest.DistributedWriter.Controllers.DistributedWriterController.  Find a way to put in a common base.

        /// <summary>
        /// Loads the latest state of the DistributedReader node from persistent storage.
        /// </summary>
        [HttpPost]
        [Route("dataElements:load")]
        [ApiExplorerSettings(GroupName = "Administration")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public void Load()
        {
            manuallyLoadableComponent.Load(false);
        }
    }
}
