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
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IAccessManagerEntityEventProcessor"/> interface (except the primary 'Add*' methods) as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "EntityEventProcessor")]
    public abstract class EntityEventProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerEntityEventProcessor entityEventProcessor;
        protected ILogger<EntityEventProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.EntityEventProcessorControllerBase class.
        /// </summary>
        public EntityEventProcessorControllerBase(EntityEventProcessorHolder entityEventProcessorHolder, ILogger<EntityEventProcessorControllerBase> logger)
        {
            entityEventProcessor = entityEventProcessorHolder.EntityEventProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <response code="200">The entity type was removed.</response>
        [HttpDelete]
        [Route("entityTypes/{entityType}")]
        public void RemoveEntityType([FromRoute] String entityType)
        {
            entityEventProcessor.RemoveEntityType(entityType);
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <response code="200">The entity was removed.</response>
        [HttpDelete]
        [Route("entityTypes/{entityType}/entities/{entity}")]
        public void RemoveEntity([FromRoute] String entityType, [FromRoute] String entity)
        {
            entityEventProcessor.RemoveEntity(entityType, entity);
        }
    }
}
