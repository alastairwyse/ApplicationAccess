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
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ApplicationAccess.Hosting.Models;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controller which exposes methods on the <see cref="IAccessManagerEntityQueryProcessor"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public abstract class EntityQueryProcessorControllerBase : ControllerBase
    {
        private readonly IAccessManagerEntityQueryProcessor _entityQueryProcessor;
        private readonly ILogger<EntityQueryProcessorControllerBase> _logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.EntityQueryProcessorControllerBase class.
        /// </summary>
        public EntityQueryProcessorControllerBase(EntityQueryProcessorHolder entityQueryProcessorHolder, ILogger<EntityQueryProcessorControllerBase> logger)
        {
            _entityQueryProcessor = entityQueryProcessorHolder.EntityQueryProcessor;
            _logger = logger;
        }

        /// <summary>
        /// Returns all entity types.
        /// </summary>
        /// <returns>All entity types.</returns>
        [HttpGet]
        [Route("entityTypes")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<String> EntityTypes()
        {
            return _entityQueryProcessor.EntityTypes;
        }

        /// <summary>
        /// Returns the specified entity type if it exists.
        /// </summary>
        /// <param name="entityType">The id of the entity type.</param>
        /// <returns>The id of the entity type.</returns>
        /// <response code="404">The entity type doesn't exist.</response>
        [HttpGet]
        [Route("entityTypes/{entityType}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<String> ContainsEntityType([FromRoute] String entityType)
        {
            if (_entityQueryProcessor.ContainsEntityType(entityType) == true)
            {
                return entityType;
            }
            else
            {
                throw new NotFoundException($"Entity type '{entityType}' does not exist.", entityType);
            }
        }

        /// <summary>
        /// Returns all entities of the specified type.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>All entities of the specified type.</returns>
        [HttpGet]
        [Route("entityTypes/{entityType}/entities")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<EntityTypeAndEntity> GetEntities([FromRoute] String entityType)
        {
            foreach (String currentEntity in _entityQueryProcessor.GetEntities(entityType))
            {
                yield return new EntityTypeAndEntity(entityType, currentEntity);
            }
        }

        /// <summary>
        /// Returns the specified entity if it exists.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The id of the entity.</param>
        /// <returns>The entity.</returns>
        /// <response code="404">The entity doesn't exist.</response>
        [HttpGet]
        [Route("entityTypes/{entityType}/entities/{entity}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<EntityTypeAndEntity> ContainsEntity([FromRoute] String entityType, [FromRoute] String entity)
        {
            if (_entityQueryProcessor.ContainsEntity(entityType, entity) == true)
            {
                return new EntityTypeAndEntity(entityType, entity);
            }
            else
            {
                throw new NotFoundException($"Entity '{entity}' of type '{entityType}' does not exist.", entity);
            }
        }

    }
}
