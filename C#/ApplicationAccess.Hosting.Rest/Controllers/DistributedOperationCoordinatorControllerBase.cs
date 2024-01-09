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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IDistributedAccessManagerOperationCoordinator{TClientConfiguration}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public abstract class DistributedOperationCoordinatorControllerBase : ControllerBase
    {
        protected IDistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> distributedAccessManagerOperationCoordinator;
        protected ILogger<DistributedOperationCoordinatorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.DistributedOperationCoordinatorControllerBase class.
        /// </summary>
        /// <param name="distributedOperationCoordinatorHolder"></param>
        /// <param name="logger"></param>
        public DistributedOperationCoordinatorControllerBase(DistributedOperationCoordinatorHolder distributedOperationCoordinatorHolder, ILogger<DistributedOperationCoordinatorControllerBase> logger)
        {
            distributedAccessManagerOperationCoordinator = distributedOperationCoordinatorHolder.DistributedOperationCoordinator;
            this.logger = logger;
        }

        /// <summary>
        /// Returns all users.
        /// </summary>
        /// <returns>All users.</returns>
        [HttpGet]
        [Route("users")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IEnumerable<String>> GetUsersAsync()
        {
            return await distributedAccessManagerOperationCoordinator.GetUsersAsync();
        }

        /// <summary>
        /// Returns all groups.
        /// </summary>
        /// <returns>All groups.</returns>
        [HttpGet]
        [Route("groups")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IEnumerable<String>> GetGroupsAsync()
        {
            return await distributedAccessManagerOperationCoordinator.GetGroupsAsync();
        }

        /// <summary>
        /// Returns all entity types.
        /// </summary>
        /// <returns>All entity types.</returns>
        [HttpGet]
        [Route("entityTypes")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IEnumerable<String>> GetEntityTypesAsync()
        {
            return await distributedAccessManagerOperationCoordinator.GetEntityTypesAsync();
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
        public async Task<ActionResult<String>> ContainsUserAsync([FromRoute] String user)
        {
            if (await distributedAccessManagerOperationCoordinator.ContainsUserAsync(user) == true)
            {
                return user;
            }
            else
            {
                throw new NotFoundException($"User '{user}' does not exist.", user);
            }
        }

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <response code="200">The user was removed.</response>
        [HttpDelete]
        [Route("users/{user}")]
        public async Task RemoveUserAsync([FromRoute] String user)
        {
            await distributedAccessManagerOperationCoordinator.RemoveUserAsync(user);
        }

        /// <summary>
        /// Returns the specified group if it exists.
        /// </summary>
        /// <param name="group">The id of the group.</param>
        /// <returns>The id of the group.</returns>
        /// <response code="404">The group doesn't exist.</response>
        [HttpGet]
        [Route("groups/{group}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<String>> ContainsGroupAsync([FromRoute] String group)
        {
            if (await distributedAccessManagerOperationCoordinator.ContainsGroupAsync(group) == true)
            {
                return group;
            }
            else
            {
                throw new NotFoundException($"Group '{group}' does not exist.", group);
            }
        }

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <response code="200">The group was removed.</response>
        [HttpDelete]
        [Route("groups/{group}")]
        public async Task RemoveGroupAsync([FromRoute] String group)
        {
            await distributedAccessManagerOperationCoordinator.RemoveGroupAsync(group);
        }

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("userToGroupMappings/user/{user}/group/{group}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddUserToGroupMappingAsync([FromRoute] String user, [FromRoute] String group)
        {
            await distributedAccessManagerOperationCoordinator.AddUserToGroupMappingAsync(user, group);

            return new StatusCodeResult(StatusCodes.Status201Created);
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
        public async IAsyncEnumerable<UserAndGroup<String, String>> GetUserToGroupMappingsAsync([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            foreach (String currentGroup in await distributedAccessManagerOperationCoordinator.GetUserToGroupMappingsAsync(user, includeIndirectMappings))
            {
                yield return new UserAndGroup<String, String>(user, currentGroup);
            }
        }

        /// <summary>
        /// Removes the mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("userToGroupMappings/user/{user}/group/{group}")]
        public async Task RemoveUserToGroupMappingAsync([FromRoute] String user, [FromRoute] String group)
        {
            await distributedAccessManagerOperationCoordinator.RemoveUserToGroupMappingAsync(user, group);
        }

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("groupToGroupMappings/fromGroup/{fromGroup}/toGroup/{toGroup}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddGroupToGroupMappingAsync([FromRoute] String fromGroup, [FromRoute] String toGroup)
        {
            await distributedAccessManagerOperationCoordinator.AddGroupToGroupMappingAsync(fromGroup, toGroup);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Gets the groups that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped to' group is itself mapped to further groups).</param>
        /// <returns>>A collection of between two groups.</returns>
        [HttpGet]
        [Route("groupToGroupMappings/group/{group}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async IAsyncEnumerable<FromGroupAndToGroup<String>> GetGroupToGroupMappingsAsync([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            foreach (String currentGroup in await distributedAccessManagerOperationCoordinator.GetGroupToGroupMappingsAsync(group, includeIndirectMappings))
            {
                yield return new FromGroupAndToGroup<String>(group, currentGroup);
            }
        }

        /// <summary>
        /// Removes the mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("groupToGroupMappings/fromGroup/{fromGroup}/toGroup/{toGroup}")]
        public async Task RemoveGroupToGroupMappingAsync([FromRoute] String fromGroup, [FromRoute] String toGroup)
        {
            await distributedAccessManagerOperationCoordinator.RemoveGroupToGroupMappingAsync(fromGroup, toGroup);
        }

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("userToApplicationComponentAndAccessLevelMappings/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddUserToApplicationComponentAndAccessLevelMappingAsync([FromRoute] String user, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            await distributedAccessManagerOperationCoordinator.AddUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);

            return new StatusCodeResult(StatusCodes.Status201Created);
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
        public async IAsyncEnumerable<UserAndApplicationComponentAndAccessLevel<String, String, String>> GetUserToApplicationComponentAndAccessLevelMappingsAsync([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
            }
            else
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(user);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new UserAndApplicationComponentAndAccessLevel<String, String, String>(user, currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("userToApplicationComponentAndAccessLevelMappings/user/{user}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync([FromRoute] String user, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            await distributedAccessManagerOperationCoordinator.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
        }

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("groupToApplicationComponentAndAccessLevelMappings/group/{group}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddGroupToApplicationComponentAndAccessLevelMappingAsync([FromRoute] String group, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            await distributedAccessManagerOperationCoordinator.AddGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of mappings between a group, and an application component and access level.</returns>
        [HttpGet]
        [Route("groupToApplicationComponentAndAccessLevelMappings/group/{group}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async IAsyncEnumerable<GroupAndApplicationComponentAndAccessLevel<String, String, String>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
            }
            else
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetApplicationComponentsAccessibleByGroupAsync(group);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new GroupAndApplicationComponentAndAccessLevel<String, String, String>(group, currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("groupToApplicationComponentAndAccessLevelMappings/group/{group}/applicationComponent/{applicationComponent}/accessLevel/{accessLevel}")]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync([FromRoute] String group, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            await distributedAccessManagerOperationCoordinator.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
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
        public async Task<ActionResult<String>> ContainsEntityTypeAsync([FromRoute] String entityType)
        {
            if (await distributedAccessManagerOperationCoordinator.ContainsEntityTypeAsync(entityType) == true)
            {
                return entityType;
            }
            else
            {
                throw new NotFoundException($"Entity type '{entityType}' does not exist.", entityType);
            }
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <response code="200">The entity type was removed.</response>
        [HttpDelete]
        [Route("entityTypes/{entityType}")]
        public async Task RemoveEntityTypeAsync([FromRoute] String entityType)
        {
            await distributedAccessManagerOperationCoordinator.RemoveEntityTypeAsync(entityType);
        }

        /// <summary>
        /// Returns all entities of the specified type.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>All entities of the specified type.</returns>
        [HttpGet]
        [Route("entityTypes/{entityType}/entities")]
        [Produces(MediaTypeNames.Application.Json)]
        public async IAsyncEnumerable<EntityTypeAndEntity> GetEntitiesAsync([FromRoute] String entityType)
        {
            foreach (String currentEntity in await distributedAccessManagerOperationCoordinator.GetEntitiesAsync(entityType))
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
        public async Task<ActionResult<EntityTypeAndEntity>> ContainsEntityAsync([FromRoute] String entityType, [FromRoute] String entity)
        {
            if (await distributedAccessManagerOperationCoordinator.ContainsEntityAsync(entityType, entity) == true)
            {
                return new EntityTypeAndEntity(entityType, entity);
            }
            else
            {
                throw new NotFoundException($"Entity '{entity}' with type '{entityType}' does not exist.", entity);
            }
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <response code="200">The entity was removed.</response>
        [HttpDelete]
        [Route("entityTypes/{entityType}/entities/{entity}")]
        public async Task RemoveEntityAsync([FromRoute] String entityType, [FromRoute] String entity)
        {
            await distributedAccessManagerOperationCoordinator.RemoveEntityAsync(entityType, entity);
        }

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("userToEntityMappings/user/{user}/entityType/{entityType}/entity/{entity}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddUserToEntityMappingAsync([FromRoute] String user, [FromRoute] String entityType, [FromRoute] String entity)
        {
            await distributedAccessManagerOperationCoordinator.AddUserToEntityMappingAsync(user, entityType, entity);

            return new StatusCodeResult(StatusCodes.Status201Created);
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
        public async IAsyncEnumerable<UserAndEntity<String>> GetUserToEntityMappingsAsync([FromRoute] String user, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetUserToEntityMappingsAsync(user);
            }
            else
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetEntitiesAccessibleByUserAsync(user);
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
        public async IAsyncEnumerable<UserAndEntity<String>> GetUserToEntityMappingsAsync([FromRoute] String user, [FromRoute] String entityType, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<String> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetUserToEntityMappingsAsync(user, entityType);
            }
            else
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetEntitiesAccessibleByUserAsync(user, entityType);
            }
            foreach (String currentEntity in methodReturnValue)
            {
                yield return new UserAndEntity<String>(user, entityType, currentEntity);
            }
        }

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("userToEntityMappings/user/{user}/entityType/{entityType}/entity/{entity}")]
        public async Task RemoveUserToEntityMappingAsync([FromRoute] String user, [FromRoute] String entityType, [FromRoute] String entity)
        {
            await distributedAccessManagerOperationCoordinator.RemoveUserToEntityMappingAsync(user, entityType, entity);
        }

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="201">The mapping was added.</response>
        [HttpPost]
        [Route("groupToEntityMappings/group/{group}/entityType/{entityType}/entity/{entity}")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddGroupToEntityMappingAsync([FromRoute] String group, [FromRoute] String entityType, [FromRoute] String entity)
        {
            await distributedAccessManagerOperationCoordinator.AddGroupToEntityMappingAsync(group, entityType, entity);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Gets the entities that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of mappings between a group, and an entity type and entity.</returns>
        [HttpGet]
        [Route("groupToEntityMappings/group/{group}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async IAsyncEnumerable<GroupAndEntity<String>> GetGroupToEntityMappingsAsync([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetGroupToEntityMappingsAsync(group);
            }
            else
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetEntitiesAccessibleByGroupAsync(group);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new GroupAndEntity<String>(group, currentTuple.Item1, currentTuple.Item2);
            }
        }

        /// <summary>
        /// Gets the entities of a given type that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of mappings between a group, and an entity type and entity.</returns>
        [HttpGet]
        [Route("groupToEntityMappings/group/{group}/entityType/{entityType}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async IAsyncEnumerable<GroupAndEntity<String>> GetGroupToEntityMappingsAsync([FromRoute] String group, [FromRoute] String entityType, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<String> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetGroupToEntityMappingsAsync(group, entityType);
            }
            else
            {
                methodReturnValue = await distributedAccessManagerOperationCoordinator.GetEntitiesAccessibleByGroupAsync(group, entityType);
            }
            foreach (String currentEntity in methodReturnValue)
            {
                yield return new GroupAndEntity<String>(group, entityType, currentEntity);
            }
        }

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <response code="200">The mapping was removed.</response>
        [HttpDelete]
        [Route("groupToEntityMappings/group/{group}/entityType/{entityType}/entity/{entity}")]
        public async Task RemoveGroupToEntityMappingAsync([FromRoute] String group, [FromRoute] String entityType, [FromRoute] String entity)
        {
            await distributedAccessManagerOperationCoordinator.RemoveGroupToEntityMappingAsync(group, entityType, entity);
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
        public async Task<ActionResult<Boolean>> HasAccessToApplicationComponentAsync([FromRoute] String user, [FromRoute] String applicationComponent, [FromRoute] String accessLevel)
        {
            return await distributedAccessManagerOperationCoordinator.HasAccessToApplicationComponentAsync(user, applicationComponent, accessLevel);
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
        public async Task<ActionResult<Boolean>> HasAccessToEntityAsync([FromRoute] String user, [FromRoute] String entityType, [FromRoute] String entity)
        {
            return await distributedAccessManagerOperationCoordinator.HasAccessToEntityAsync(user, entityType, entity);
        }
    }
}
