﻿/*
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

using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ApplicationAccess.Hosting.Models.DataTransferObjects;

namespace ApplicationAccess.Hosting.Rest.Controllers
{
    /// <summary>
    /// Base for controllers which expose methods on the <see cref="IAccessManagerGroupToGroupQueryProcessor{TGroup}"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "GroupToGroupQueryProcessor")]
    public abstract class GroupToGroupQueryProcessorControllerBase : ControllerBase
    {
        protected IAccessManagerGroupToGroupQueryProcessor<String> groupToGroupQueryProcessor;
        protected ILogger<GroupToGroupQueryProcessorControllerBase> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Controllers.GroupToGroupQueryProcessorControllerBase class.
        /// </summary>
        public GroupToGroupQueryProcessorControllerBase(GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder, ILogger<GroupToGroupQueryProcessorControllerBase> logger)
        {
            groupToGroupQueryProcessor = groupToGroupQueryProcessorHolder.GroupToGroupQueryProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Gets the groups that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped to' group is itself mapped to further groups).</param>
        /// <returns>>A collection of mappings between two groups.</returns>
        [HttpGet]
        [Route("groupToGroupMappings/group/{group}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<FromGroupAndToGroup<String>> GetGroupToGroupMappings([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {
            String decodedGroup = Uri.UnescapeDataString(group);
            foreach (String currentGroup in groupToGroupQueryProcessor.GetGroupToGroupMappings(decodedGroup, includeIndirectMappings))
            {
                yield return new FromGroupAndToGroup<String>(decodedGroup, currentGroup);
            }
        }

        /// <summary>
        /// Gets the groups that are mapped to the specified group.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped from' group is itself mapped from further groups).</param>
        /// <returns>A collection of mappings between two groups.</returns>
        [HttpGet]
        [Route("groupToGroupReverseMappings/group/{group}")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<FromGroupAndToGroup<String>> GetGroupToGroupReverseMappings([FromRoute] String group, [FromQuery, BindRequired] Boolean includeIndirectMappings)
        {

            String decodedGroup = Uri.UnescapeDataString(group);
            foreach (String currentGroup in groupToGroupQueryProcessor.GetGroupToGroupReverseMappings(decodedGroup, includeIndirectMappings))
            {
                yield return new FromGroupAndToGroup<String>(currentGroup, decodedGroup);
            }
        }
    }
}
