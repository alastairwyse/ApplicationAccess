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

using ApplicationAccess.Hosting.Rest.Controllers;
using Microsoft.Extensions.Logging;

namespace ApplicationAccess.Hosting.Rest.DistributedReader.Controllers
{
    /// <summary>
    /// Derives from abstract base controller class <see cref="DistributedGroupQueryProcessorControllerBase"/> to expose it as a controller.
    /// </summary>
    public class DistributedGroupQueryProcessorController : DistributedGroupQueryProcessorControllerBase
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedReader.Controllers.DistributedGroupQueryProcessorController class.
        /// </summary>
        public DistributedGroupQueryProcessorController(DistributedGroupQueryProcessorHolder distributedGroupQueryProcessorHolder, ILogger<DistributedGroupQueryProcessorController> logger)
            : base(distributedGroupQueryProcessorHolder, logger)
        {
        }
    }
}
