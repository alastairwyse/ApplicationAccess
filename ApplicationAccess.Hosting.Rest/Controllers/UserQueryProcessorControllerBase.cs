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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IAccessManagerUserQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "UserQueryProcessor")]
    public abstract class UserQueryProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerUserQueryProcessor<String, String, String, String> userQueryProcessor;
        protected ILogger<UserQueryProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.UserQueryProcessorControllerBase class.
        /// </summary>
        public UserQueryProcessorControllerBase(UserQueryProcessorHolder userQueryProcessorHolder, ILogger<UserQueryProcessorControllerBase> logger)
        {
            userQueryProcessor = userQueryProcessorHolder.UserQueryProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Returns all users.
        /// </summary>
        /// <returns>All users.</returns>
        [HttpGet]
        [Route("users")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<String> Users()
        {
            return userQueryProcessor.Users;
        }

        /// <summary>
        /// Returns the specified user if it exists.
        /// </summary>
        /// <param name="user">The id of the user.</param>
        /// <returns>The id of the user.</returns>
        /// <response code="404">The user doesn't exist.</response>
        [HttpGet]
        [Route("users/{user}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<String> ContainsUser([FromRoute] String user)
        {
            if (userQueryProcessor.ContainsUser(user) == true)
            {
                return user;
            }
            else
            {
                throw new NotFoundException($"User '{user}' does not exist.", user);
            }
        }

        /// <summary>
        /// Gets the groups that the specified user is mapped to (i.e. is a member of).
        /// </summary>
        /// <param name="user">The user to retrieve the groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of mappings between a user and a group.</returns>
        [HttpGet]
        [Route("userToGroupMappings/user/{user}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<UserAndGroup<String, String>> GetUserToGroupMappings([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            foreach (String currentGroup in userQueryProcessor.GetUserToGroupMappings(user, includeIndirectMappings))
            {
                yield return new UserAndGroup<String, String>(user, currentGroup);
            }
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of mappings between a user, and an application component and access level.</returns>
        [HttpGet]
        [Route("userToApplicationComponentAndAccessLevelMappings/user/{user}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<UserAndApplicationComponentAndAccessLevel<String, String, String>> GetUserToApplicationComponentAndAccessLevelMappings([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = userQueryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(user);
            }
            else
            {
                methodReturnValue = userQueryProcessor.GetApplicationComponentsAccessibleByUser(user);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new UserAndApplicationComponentAndAccessLevel<String, String, String>(user, currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Gets the entities that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of mappings between a user, and an entity type and entity.</returns>
        [HttpGet]
        [Route("userToEntityMappings/user/{user}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<UserAndEntity<String>> GetUserToEntityMappings([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = userQueryProcessor.GetUserToEntityMappings(user);
            }
            else
            {
                methodReturnValue = userQueryProcessor.GetEntitiesAccessibleByUser(user);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new UserAndEntity<String>(user, currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Gets the entities of a given type that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of mappings between a user, and an entity type and entity.</returns>
        [HttpGet]
        [Route("userToEntityMappings/user/{user}/entityType/{entityType}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<UserAndEntity<String>> GetUserToEntityMappings([FromRoute] String user, [FromRoute] String entityType, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<String> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = userQueryProcessor.GetUserToEntityMappings(user, entityType);
            }
            else
            {
                methodReturnValue = userQueryProcessor.GetEntitiesAccessibleByUser(user, entityType);
            }
            foreach (String currentEntity in methodReturnValue)
            {
                yield return new UserAndEntity<String>(user, entityType, currentEntity);
            }
        }

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to an application component at the specified level of access.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>True if the user has access the component.  False otherwise.</returns>
        [HttpGet]
        [Route("dataElementAccess/applicationComponent/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<Boolean> HasAccessToApplicationComponent([FromRoute] String user, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            return userQueryProcessor.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
        }

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to the specified entity.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the user has access the entity.  False otherwise.</returns>
        [HttpGet]
        [Route("dataElementAccess/entity/user/{user}/entityType/{entityType}/entity/{entity}")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<Boolean> HasAccessToEntity([FromRoute] String user, [FromRoute] String entityType, [FromRoute] String entity)
        {
            return userQueryProcessor.HasAccessToEntity(user, entityType, entity);
        }
    }
}