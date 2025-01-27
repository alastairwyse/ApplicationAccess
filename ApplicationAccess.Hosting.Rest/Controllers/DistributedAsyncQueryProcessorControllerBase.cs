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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Models.DataTransferObjects;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IDistributedAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public abstract class DistributedAsyncQueryProcessorControllerBase : ControllerBase
    {
        // TODO: Could consider splitting this into separate *ControllerBases for users and groups
        //   List for DistributedUserQueryProcessorControllerBase

        protected IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String> distributedAccessManagerAsyncQueryProcessor;
        protected ILogger<DistributedAsyncQueryProcessorControllerBase> logger;

        public DistributedAsyncQueryProcessorControllerBase
        (
            DistributedAsyncQueryProcessorHolder distributedAsyncQueryProcessorHolder, 
            ILogger<DistributedAsyncQueryProcessorControllerBase> logger
        )
        {
            distributedAccessManagerAsyncQueryProcessor = distributedAsyncQueryProcessorHolder.DistributedAsyncQueryProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the users that are directly mapped to any of the specified groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the users for.</param>
        /// <returns>A collection of users that are mapped to the specified groups.</returns>
        [HttpGet]
        [Route("userToGroupMappings")]
        [ApiExplorerSettings(GroupName = "UserQueryProcessor")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<List<String>> GetGroupToUserMappingsAsync([FromBody, BindRequired] IEnumerable<String> groups)
        {
            return await distributedAccessManagerAsyncQueryProcessor.GetGroupToUserMappingsAsync(groups);
        }

        // TODO: Not including methods GetGroupToGroupMappingsAsync() nor GetGroupToGroupReverseMappingsAsync() for now as they're not supported
        //   by DistributedAccessManagerOperationRouter, but may need to add later.

        /// <summary>
        /// Checks whether any of the specified groups have access to an application component at the specified level of access.
        /// </summary>
        /// <param name="groups">The groups to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>True if any of the groups have access the component.  False otherwise.</returns>
        [HttpGet]
        [Route("dataElementAccess/applicationComponent/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        [ApiExplorerSettings(GroupName = "GroupQueryProcessor")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Boolean>> HasAccessToApplicationComponentAsync([FromBody, BindRequired] IEnumerable<String> groups, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            String decodedApplicationComponent = Uri.UnescapeDataString(applicationComponent);
            String decodedAccessLevel = Uri.UnescapeDataString(accessLevel);

            return await distributedAccessManagerAsyncQueryProcessor.HasAccessToApplicationComponentAsync(groups, decodedApplicationComponent, decodedAccessLevel);
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
        [ApiExplorerSettings(GroupName = "GroupQueryProcessor")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Boolean>> HasAccessToEntityAsync([FromBody, BindRequired] IEnumerable<String> groups, [FromRoute] String entityType, [FromRoute] String entity)
        {
            String decodedEntityType = Uri.UnescapeDataString(entityType);
            String decodedEntity = Uri.UnescapeDataString(entity);

            return await distributedAccessManagerAsyncQueryProcessor.HasAccessToEntityAsync(groups, decodedEntityType, decodedEntity);
        }

        /// <summary>
        /// Gets all application components and levels of access that the specified groups have access to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the groups have.</returns>
        [HttpGet]
        [Route("groupToApplicationComponentAndAccessLevelMappings")]
        [ApiExplorerSettings(GroupName = "GroupQueryProcessor")]
        [Produces(MediaTypeNames.Application.Json)]
        public async IAsyncEnumerable<ApplicationComponentAndAccessLevel<String, String>> GetApplicationComponentsAccessibleByGroupsAsync([FromBody, BindRequired] IEnumerable<String> groups)
        {
            foreach (Tuple<String, String> currentTuple in await distributedAccessManagerAsyncQueryProcessor.GetApplicationComponentsAccessibleByGroupsAsync(groups))
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
        [ApiExplorerSettings(GroupName = "GroupQueryProcessor")]
        [Route("groupToEntityMappings")]
        [Produces(MediaTypeNames.Application.Json)]
        public async IAsyncEnumerable<EntityTypeAndEntity> GetEntitiesAccessibleByGroupsAsync([FromBody, BindRequired] IEnumerable<String> groups)
        {
            foreach (Tuple<String, String> currentTuple in await distributedAccessManagerAsyncQueryProcessor.GetEntitiesAccessibleByGroupsAsync(groups))
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
        [ApiExplorerSettings(GroupName = "GroupQueryProcessor")]
        [Route("groupToEntityMappings/entityType/{entityType}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IEnumerable<String>> GetEntitiesAccessibleByGroupsAsync([FromBody, BindRequired] IEnumerable<String> groups, [FromRoute] String entityType)
        {
            String decodedEntityType = Uri.UnescapeDataString(entityType);

            return await distributedAccessManagerAsyncQueryProcessor.GetEntitiesAccessibleByGroupsAsync(groups, decodedEntityType);
        }
    }
}
