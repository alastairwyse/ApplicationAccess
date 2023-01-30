/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Mime;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controller which exposes methods on the <see cref="IAccessManagerGroupToGroupQueryProcessor{TGroup}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public abstract class GroupToGroupQueryProcessorControllerBase : ControllerBase
    {
        private readonly IAccessManagerGroupToGroupQueryProcessor<String> _groupToGroupQueryProcessor;
        private readonly ILogger<GroupToGroupQueryProcessorControllerBase> _logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.GroupToGroupQueryProcessorControllerBase class.
        /// </summary>
        public GroupToGroupQueryProcessorControllerBase(GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder, ILogger<GroupToGroupQueryProcessorControllerBase> logger)
        {
            _groupToGroupQueryProcessor = groupToGroupQueryProcessorHolder.GroupToGroupQueryProcessor;
            _logger = logger;
        }

        /// <summary>
        ///Gets the groups that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped to' group is itself mapped to further groups).</param>
        /// <returns>>A collection of between two groups.</returns>
        [HttpGet]
        [Route("groupToGroupMappings/group/{group}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<FromGroupAndToGroup<String>> GetGroupToGroupMappings([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            foreach (String currentGroup in _groupToGroupQueryProcessor.GetGroupToGroupMappings(group, includeIndirectMappings))
            {
                yield return new FromGroupAndToGroup<String>(group, currentGroup);
            }
        }

    }
}
