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
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IDistributedAccessManagerGroupToGroupQueryProcessor{TGroup}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "GroupToGroupQueryProcessor")]
    public abstract class DistributedGroupToGroupQueryProcessorControllerBase : ControllerBase
    {
        protected IDistributedAccessManagerGroupToGroupQueryProcessor<String> distributedGroupToGroupQueryProcessor;
        protected ILogger<DistributedGroupToGroupQueryProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.DistributedGroupToGroupQueryProcessorControllerBase class.
        /// </summary>
        public DistributedGroupToGroupQueryProcessorControllerBase(DistributedGroupToGroupQueryProcessorHolder distributedGroupToGroupQueryProcessorHolder, ILogger<DistributedGroupToGroupQueryProcessorControllerBase> logger)
        {
            distributedGroupToGroupQueryProcessor = distributedGroupToGroupQueryProcessorHolder.DistributedGroupToGroupQueryProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the groups that all of the specified groups are directly and indirectly mapped to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mapped groups for.</param>
        /// <returns>A collection of groups the specified groups are mapped to, and including the specified groups.</returns>
        [HttpGet]
        [Route("groupToGroupMappings")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<String> GetGroupToGroupMappings([FromQuery, BindRequired] IEnumerable<String> groups)
        {
            return distributedGroupToGroupQueryProcessor.GetGroupToGroupMappings(groups);
        }

        /// <summary>
        /// Gets the groups that are directly and indirectly mapped to any of the specified groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mapped groups for.</param>
        /// <returns>A collection of groups that are mapped to the specified groups, and including the specified groups.</returns>
        [HttpGet]
        [Route("groupToGroupReverseMappings")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<String> GetGroupToGroupReverseMappings([FromQuery, BindRequired] IEnumerable<String> groups)
        {
            return distributedGroupToGroupQueryProcessor.GetGroupToGroupReverseMappings(groups);
        }
    }
}
