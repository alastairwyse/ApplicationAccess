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
using System.ComponentModel;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose primary 'Add*' entity event methods as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "EntityEventProcessor")]
    public abstract class AddPrimaryEntityEventProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerEntityEventProcessor entityEventProcessor;
        protected ILogger<EntityEventProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.AddPrimaryEntityEventProcessorControllerBase class.
        /// </summary>
        public AddPrimaryEntityEventProcessorControllerBase(EntityEventProcessorHolder entityEventProcessorHolder, ILogger<EntityEventProcessorControllerBase> logger)
        {
            entityEventProcessor = entityEventProcessorHolder.EntityEventProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <response code="201">The entity type was added.</response>
        [HttpPost]
        [Route("entityTypes/{entityType}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult AddEntityType([FromRoute] String entityType)
        {
            entityEventProcessor.AddEntityType(Uri.UnescapeDataString(entityType));

            return new StatusCodeResult(StatusCodes.Status201Created);
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
        public StatusCodeResult AddEntity([FromRoute] String entityType, [FromRoute] String entity)
        {
            entityEventProcessor.AddEntity(Uri.UnescapeDataString(entityType), Uri.UnescapeDataString(entity));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }
    }
}
