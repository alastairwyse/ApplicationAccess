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
    /// Base for controller which exposes methods on the <see cref="IAccessManagerGroupQueryProcessor{TGroup, TComponent, TAccess}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public abstract class GroupQueryProcessorControllerBase : ControllerBase
    {
        private readonly IAccessManagerGroupQueryProcessor<String, String, String> _groupQueryProcessor;
        private readonly ILogger<GroupQueryProcessorControllerBase> _logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.GroupQueryProcessorControllerBase class.
        /// </summary>
        public GroupQueryProcessorControllerBase(GroupQueryProcessorHolder groupQueryProcessorHolder, ILogger<GroupQueryProcessorControllerBase> logger)
        {
            _groupQueryProcessor = groupQueryProcessorHolder.GroupQueryProcessor;
            _logger = logger;
        }

        /// <summary>
        /// Returns all groups.
        /// </summary>
        /// <returns>All groups.</returns>
        [HttpGet]
        [Route("groups")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<String> Groups()
        {
            return _groupQueryProcessor.Groups;
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
        public ActionResult<String> ContainsGroup([FromRoute] String group)
        {
            if (_groupQueryProcessor.ContainsGroup(group) == true)
            {
                return group;
            }
            else
            {
                throw new NotFoundException($"Group '{group}' does not exist.", group);
            }
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
        public IEnumerable<GroupAndApplicationComponentAndAccessLevel<String, String, String>> GetGroupToApplicationComponentAndAccessLevelMappings([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = _groupQueryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(group);
            }
            else
            {
                methodReturnValue = _groupQueryProcessor.GetApplicationComponentsAccessibleByGroup(group);
            }
            foreach (Tuple<String, String> currentTuple in methodReturnValue)
            {
                yield return new GroupAndApplicationComponentAndAccessLevel<String, String, String>(group, currentTuple.Item1, currentTuple.Item2);
            }
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
        public IEnumerable<GroupAndEntity<String>> GetGroupToEntityMappings([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<Tuple<String, String>> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = _groupQueryProcessor.GetGroupToEntityMappings(group);
            }
            else
            {
                methodReturnValue = _groupQueryProcessor.GetEntitiesAccessibleByGroup(group);
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
        public IEnumerable<GroupAndEntity<String>> GetGroupToEntityMappings([FromRoute] String group, [FromRoute] String entityType, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            IEnumerable<String> methodReturnValue = null;
            if (includeIndirectMappings == false)
            {
                methodReturnValue = _groupQueryProcessor.GetGroupToEntityMappings(group, entityType);
            }
            else
            {
                methodReturnValue = _groupQueryProcessor.GetEntitiesAccessibleByGroup(group, entityType);
            }
            foreach (String currentEntity in methodReturnValue)
            {
                yield return new GroupAndEntity<String>(group, entityType, currentEntity);
            }
        }
    }
}
