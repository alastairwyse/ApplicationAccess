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
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IDistributedAccessManagerGroupQueryProcessor{TGroup, TComponent, TAccess}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "GroupQueryProcessor")]
    [Produces(MediaTypeNames.Application.Json)]
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
        public ActionResult<Boolean> HasAccessToApplicationComponent([FromBody, BindRequired] IEnumerable<String> groups, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            String decodedApplicationComponent = Uri.UnescapeDataString(applicationComponent);
            String decodedAccessLevel = Uri.UnescapeDataString(accessLevel);

            return distributedGroupQueryProcessor.HasAccessToApplicationComponent(groups, decodedApplicationComponent, decodedAccessLevel);
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
        public ActionResult<Boolean> HasAccessToEntity([FromBody, BindRequired] IEnumerable<String> groups, [FromRoute] String entityType, [FromRoute] String entity)
        {
            String decodedEntityType = Uri.UnescapeDataString(entityType);
            String decodedEntity = Uri.UnescapeDataString(entity);

            return distributedGroupQueryProcessor.HasAccessToEntity(groups, decodedEntityType, decodedEntity);
        }

        /// <summary>
        /// Gets all application components and levels of access that the specified groups have access to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the groups have.</returns>
        [HttpGet]
        [Route("groupToApplicationComponentAndAccessLevelMappings")]
        public IEnumerable<ApplicationComponentAndAccessLevel<String, String>> GetApplicationComponentsAccessibleByGroups([FromBody, BindRequired] IEnumerable<String> groups)
        {
            foreach (Tuple<String, String> currentTuple in distributedGroupQueryProcessor.GetApplicationComponentsAccessibleByGroups(groups))
            {
                yield return new ApplicationComponentAndAccessLevel<String, String>(currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Gets all entities that the specified groups have access to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the entities for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the groups have access to.</returns>
        [HttpGet]
        [Route("groupToEntityMappings")]
        public IEnumerable<EntityTypeAndEntity> GetEntitiesAccessibleByGroups([FromBody, BindRequired] IEnumerable<String> groups)
        {
            foreach (Tuple<String, String> currentTuple in distributedGroupQueryProcessor.GetEntitiesAccessibleByGroups(groups))
            {
                yield return new EntityTypeAndEntity(currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Gets all entities of a given type that the specified groups have access to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <returns>The entities the groups have access to.</returns>
        [HttpGet]
        [Route("groupToEntityMappings/entityType/{entityType}")]
        public IEnumerable<String> GetEntitiesAccessibleByGroups([FromBody, BindRequired] IEnumerable<String> groups, [FromRoute] String entityType)
        {
            String decodedEntityType = Uri.UnescapeDataString(entityType);

            return distributedGroupQueryProcessor.GetEntitiesAccessibleByGroups(groups, decodedEntityType);
        }
    }
}
