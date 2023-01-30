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

using Microsoft.AspNetCore.Mvc;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controller which exposes methods on the <see cref="IAccessManagerEntityEventProcessor"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public abstract class EntityEventProcessorControllerBase : ControllerBase
    {
        private readonly IAccessManagerEntityEventProcessor _entityEventProcessor;
        private readonly ILogger<EntityEventProcessorControllerBase> _logger;

        /// <summary>
        ///  Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.EntityEventProcessorControllerBase class.
        /// </summary>
        public EntityEventProcessorControllerBase(EntityEventProcessorHolder entityEventProcessorHolder, ILogger<EntityEventProcessorControllerBase> logger)
        {
            _entityEventProcessor = entityEventProcessorHolder.EntityEventProcessor;
            _logger = logger;
        }

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <response code="201">The entity type was added.</response>
        [HttpPost]
        [Route("entityTypes/{entityType}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddEntityType([FromRoute] string entityType)
        {
            _entityEventProcessor.AddEntityType(entityType);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <response code="200">The entity type was removed.</response>
        [HttpDelete]
        [Route("entityTypes/{entityType}")]
        public void RemoveEntityType([FromRoute] string entityType)
        {
            _entityEventProcessor.RemoveEntityType(entityType);
        }

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <response code="201">The entity was added.</response>
        [HttpPost]
        [Route("entityTypes/{entityType}/entities/{entity}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddEntity([FromRoute] string entityType, [FromRoute] string entity)
        {
            _entityEventProcessor.AddEntity(entityType, entity);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <response code="200">The entity was removed.</response>
        [HttpDelete]
        [Route("entityTypes/{entityType}/entities/{entity}")]
        public void RemoveEntity([FromRoute] string entityType, [FromRoute] string entity)
        {
            _entityEventProcessor.RemoveEntity(entityType, entity);
        }
    }
}
