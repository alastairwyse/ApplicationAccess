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
    [Produces(MediaTypeNames.Application.Json)]
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
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<String> ContainsUser([FromRoute] String user)
        {
            String decodedUser = Uri.UnescapeDataString(user);
            if (userQueryProcessor.ContainsUser(decodedUser) == true)
            {
                return user;
            }
            else
            {
                throw new NotFoundException($"User '{decodedUser}' does not exist.", decodedUser);
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
        public IEnumerable<UserAndGroup<String, String>> GetUserToGroupMappings([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            String decodedUser = Uri.UnescapeDataString(user);
            foreach (String currentGroup in userQueryProcessor.GetUserToGroupMappings(decodedUser, includeIndirectMappings))
            {
                yield return new UserAndGroup<String, String>(decodedUser, currentGroup);
            }
        }

        /// <summary>
        /// Gets the users that are mapped to the specified group.
        /// </summary>
        /// <param name="group">The group to retrieve the users for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to the group via other groups).</param>
        /// <returns>A collection of mappings between a user and a group.</returns>
        [HttpGet]
        [Route("userToGroupMappings/group/{group}")]
        public IEnumerable<UserAndGroup<String, String>> GetGroupToUserMappings([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            String decodedGroup = Uri.UnescapeDataString(group);
            foreach (String currentUser in userQueryProcessor.GetGroupToUserMappings(decodedGroup, includeIndirectMappings))
            {
                yield return new UserAndGroup<String, String> (currentUser, decodedGroup);
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
        public IEnumerable<UserAndApplicationComponentAndAccessLevel<String, String, String>> GetUserToApplicationComponentAndAccessLevelMappings([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            String decodedUser = Uri.UnescapeDataString(user);
            if (includeIndirectMappings == false)
            {
                methodReturnValue = userQueryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(decodedUser);
            }
            else
            {
                methodReturnValue = userQueryProcessor.GetApplicationComponentsAccessibleByUser(decodedUser);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new UserAndApplicationComponentAndAccessLevel<String, String, String>(decodedUser, currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Gets users that are mapped to the specific application component and access level pair.
        /// </summary>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to an application component and access level via groups).</param>
        /// <returns>A collection of mappings between a user, and an application component and access level.</returns>
        [HttpGet]
        [Route("userToApplicationComponentAndAccessLevelMappings/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        public IEnumerable<UserAndApplicationComponentAndAccessLevel<String, String, String>> GetApplicationComponentAndAccessLevelToUserMappings([FromRoute] String applicationComponent, [FromRoute] String accessLevel, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            String decodedApplicationComponent = Uri.UnescapeDataString(applicationComponent);
            String decodedAccessLevel = Uri.UnescapeDataString(accessLevel);
            foreach (String currentUser in userQueryProcessor.GetApplicationComponentAndAccessLevelToUserMappings(decodedApplicationComponent, decodedAccessLevel, includeIndirectMappings))
            {
                yield return new UserAndApplicationComponentAndAccessLevel<String, String, String>(currentUser, decodedApplicationComponent, decodedAccessLevel);
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
        public IEnumerable<UserAndEntity<String>> GetUserToEntityMappings([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            String decodedUser = Uri.UnescapeDataString(user);
            if (includeIndirectMappings == false)
            {
                methodReturnValue = userQueryProcessor.GetUserToEntityMappings(decodedUser);
            }
            else
            {
                methodReturnValue = userQueryProcessor.GetEntitiesAccessibleByUser(decodedUser);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new UserAndEntity<String>(decodedUser, currentTuple.Item1, currentTuple.Item2);
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
        public IEnumerable<UserAndEntity<String>> GetUserToEntityMappings([FromRoute] String user, [FromRoute] String entityType, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<String> methodReturnValue = null;
            String decodedUser = Uri.UnescapeDataString(user);
            String decodedEntityType = Uri.UnescapeDataString(entityType);
            if (includeIndirectMappings == false)
            {
                methodReturnValue = userQueryProcessor.GetUserToEntityMappings(decodedUser, decodedEntityType);
            }
            else
            {
                methodReturnValue = userQueryProcessor.GetEntitiesAccessibleByUser(decodedUser, decodedEntityType);
            }
            foreach (String currentEntity in methodReturnValue)
            {
                yield return new UserAndEntity<String>(decodedUser, decodedEntityType, currentEntity);
            }
        }

        /// <summary>
        /// Gets the users that are mapped to the specified entity.
        /// </summary>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to the entity via groups).</param>
        /// <returns>A collection of mappings between a user, and an entity type and entity.</returns>
        [HttpGet]
        [Route("userToEntityMappings/entityType/{entityType}/entity/{entity}")]
        public IEnumerable<UserAndEntity<String>> GetEntityToUserMappings([FromRoute] String entityType, [FromRoute] String entity, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            String decodedEntityType = Uri.UnescapeDataString(entityType);
            String decodedEntity = Uri.UnescapeDataString(entity);
            foreach (String currentUser in userQueryProcessor.GetEntityToUserMappings(decodedEntityType, decodedEntity, includeIndirectMappings))
            {
                yield return new UserAndEntity<String>(currentUser, decodedEntityType, decodedEntity);
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
        public ActionResult<Boolean> HasAccessToApplicationComponent([FromRoute] String user, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            String decodedUser = Uri.UnescapeDataString(user);
            String decodedApplicationComponent = Uri.UnescapeDataString(applicationComponent);
            String decodedAccessLevel = Uri.UnescapeDataString(accessLevel);

            return userQueryProcessor.HasAccessToApplicationComponent(decodedUser, decodedApplicationComponent, decodedAccessLevel);
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
        public ActionResult<Boolean> HasAccessToEntity([FromRoute] String user, [FromRoute] String entityType, [FromRoute] String entity)
        {
            String decodedUser = Uri.UnescapeDataString(user);
            String decodedEntityType = Uri.UnescapeDataString(entityType);
            String decodedEntity = Uri.UnescapeDataString(entity);

            return userQueryProcessor.HasAccessToEntity(decodedUser, decodedEntityType, decodedEntity);
        }
    }
}