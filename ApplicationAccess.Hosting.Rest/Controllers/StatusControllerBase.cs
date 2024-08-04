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
using System.Diagnostics;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using ApplicationAccess.Hosting.Models.DataTransferObjects;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose the status of the current running ApplicationAccess node as a REST method.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public abstract class StatusControllerBase : ControllerBase
    {
        /// <summary>
        /// Returns the current status of the node.
        /// </summary>
        /// <returns>The current status of the node.</returns>
        [HttpGet]
        [Route("status")]
        [Produces(MediaTypeNames.Application.Json)]
        public NodeStatus Status()
        {
            var nodeStatus = new NodeStatus(Process.GetCurrentProcess().StartTime.ToUniversalTime());

            return nodeStatus;
        }
    }
}
