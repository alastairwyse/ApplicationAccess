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
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controller which exposes methods on the <see cref="IDistributedAccessManagerGroupQueryProcessor{TGroup, TComponent, TAccess}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "GroupQueryProcessor")]
    public abstract class DistributedGroupQueryProcessorControllerBase : ControllerBase
    {
        protected IDistributedAccessManagerGroupQueryProcessor<String, String, String> distributedGroupQueryProcessor;
        protected ILogger<DistributedGroupQueryProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.DistributedGroupQueryProcessorControllerBase class.
        /// </summary>
        public DistributedGroupQueryProcessorControllerBase(DistributedGroupQueryProcessorHolder distributedGroupQueryProcessorHolder, ILogger<DistributedGroupQueryProcessorControllerBase> logger)
        {
            distributedGroupQueryProcessor = distributedGroupQueryProcessorHolder.DistributedGroupQueryProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Checks whether any of the specified groups have access to an application component at the specified level of access.
        /// </summary>
        /// <param name="groups">The groups to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>True if any of the groups have access the component.  False otherwise.</returns>
        [HttpGet]
        [Route("dataElementAccess/applicationComponent/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<Boolean> HasAccessToApplicationComponent([FromQuery, BindRequired] IEnumerable<String> groups, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            return distributedGroupQueryProcessor.HasAccessToApplicationComponent(groups, applicationComponent, accessLevel);
        }

        /// <summary>
        /// Checks whether any of the specified groups have access to the specified entity.
        /// </summary>
        /// <param name="groups">The groups to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if any of the groups have access the entity.  False otherwise.</returns>
        [HttpGet]
        [Route("dataElementAccess/entity/entityType/{entityType}/entity/{entity}")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<Boolean> HasAccessToEntity([FromQuery, BindRequired] IEnumerable<String> groups, [FromRoute] String entityType, [FromRoute] String entity)
        {
            return distributedGroupQueryProcessor.HasAccessToEntity(groups, entityType, entity);
        }
    }
}
