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

using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Mime;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Controller which exposes methods on the <see cref="IAccessManagerUserQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class UserQueryProcessorController : ControllerBase
    {
        private readonly IAccessManagerUserQueryProcessor<String, String, String, String> _userQueryProcessor;
        private readonly ILogger<UserQueryProcessorController> _logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.UserQueryProcessorController class.
        /// </summary>
        public UserQueryProcessorController(UserQueryProcessorHolder userQueryProcessorHolder, ILogger<UserQueryProcessorController> logger)
        {
            _userQueryProcessor = userQueryProcessorHolder.UserQueryProcessor;
            _logger = logger;
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
            return _userQueryProcessor.Users;
        }

        /// <summary>
        /// Returns the specified user if they exist.
        /// </summary>
        /// <param name="user">The id of the user.</param>
        /// <returns>The id of the user.</returns>
        /// <response code="404">The user doesn't exist.</response>
        [HttpGet]
        [Route("users/{user}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<String> ContainsUser(String user)
        {
            if (_userQueryProcessor.ContainsUser(user))
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
        public IEnumerable<UserAndGroup<String, String>> GetUserToGroupMappings(String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            foreach (String currentGroup in _userQueryProcessor.GetUserToGroupMappings(user, includeIndirectMappings))
            {
                yield return new UserAndGroup<String, String>(user, currentGroup);
            }
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings"></param>
        /// <returns>A collection mappings between a user, and an application component and access level.</returns>
        [HttpGet]
        [Route("userToApplicationComponentAndAccessLevelMappings/user/{user}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<UserAndApplicationComponentAndAccessLevel<String, String, String>> GetUserToApplicationComponentAndAccessLevelMappings(String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>>? methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = _userQueryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(user);
            }
            else
            {
                methodReturnValue = _userQueryProcessor.GetApplicationComponentsAccessibleByUser(user);
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
        /// <returns>A collection of mappings between a user, and an entity type and entity.</returns>
        [HttpGet]
        [Route("userToEntityMappings/user/{user}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<UserAndEntity<String>> GetUserToEntityMappings(String user)
        {
            foreach (Tuple<String, String> currentTuple in _userQueryProcessor.GetUserToEntityMappings(user))
            {
                yield return new UserAndEntity<String>(user, currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Gets the entities of a given type that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A collection of mappings between a user, and an entity type and entity.</returns>
        [HttpGet]
        [Route("userToEntityMappings/user/{user}/entityType/{entityType}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<UserAndEntity<String>> GetUserToEntityMappings(String user, String entityType)
        {
            foreach (String currentEntity in _userQueryProcessor.GetUserToEntityMappings(user, entityType))
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
        public Boolean HasAccessToApplicationComponent(String user, String applicationComponent, String accessLevel)
        {
            return _userQueryProcessor.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
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
        public Boolean HasAccessToEntity(String user, String entityType, String entity)
        {
            return _userQueryProcessor.HasAccessToEntity(user, entityType, entity);
        }
    }
}