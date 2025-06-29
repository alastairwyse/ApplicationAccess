/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting.Rest.Controllers;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.DataTransferObjects;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Models;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Controllers
{
    /// <summary>
    /// Controller which exposes methods on the <see cref="IKubernetesDistributedInstanceManager"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public class KubernetesDistributedInstanceManagerController
    {
        #pragma warning disable 1591

        protected IKubernetesDistributedInstanceManager kubernetesDistributedInstanceManager;
        protected ILogger<KubernetesDistributedInstanceManagerController> logger;

        public KubernetesDistributedInstanceManagerController(KubernetesDistributedInstanceManagerHolder kubernetesDistributedInstanceManagerHolder, ILogger<KubernetesDistributedInstanceManagerController> logger)
        {
            kubernetesDistributedInstanceManager = kubernetesDistributedInstanceManagerHolder.KubernetesDistributedInstanceManager;
            this.logger = logger;
        }

        #pragma warning restore 1591

        /// <summary>
        /// Sets the URL for the distributed operation router component used for shard group splitting.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPut]
        [Route("distributedOperationRouterUrl/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetDistributedOperationRouterUrl(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.DistributedOperationRouterUrl = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Sets the URL for a first writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPut]
        [Route("writer1Url/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetWriter1Url(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.Writer1Url = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Sets the URL for a second writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPut]
        [Route("writer2Url/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetWriter2Url(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.Writer2Url = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Sets the URL for the distributed operation coordinator component.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPut]
        [Route("distributedOperationCoordinatorUrl/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetDistributedOperationCoordinatorUrl(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.DistributedOperationCoordinatorUrl = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Gets configuration for the distributed AccessManager instance.
        /// </summary>
        [HttpGet]
        [Route("distributedInstance/instanceConfiguration")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [Produces(MediaTypeNames.Application.Json)]
        public InstanceConfiguration GetInstanceConfiguration()
        {
            return ConvertInstanceConfigurationToDto(kubernetesDistributedInstanceManager.InstanceConfiguration);
        }

        /// <summary>
        /// Creates a new distributed AccessManager instance.
        /// </summary>
        /// <response code="200">The instance was created.</response>
        [HttpPost]
        [Route("distributedInstance")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<StatusCodeResult> CreateDistributedAccessManagerInstanceAsync(CreateDistributedAccessManagerInstanceParameters parameters)
        {
            await kubernetesDistributedInstanceManager.CreateDistributedAccessManagerInstanceAsync
            (
                ParseShardGroupConfiguration(parameters.UserShardGroupConfiguration),
                ParseShardGroupConfiguration(parameters.GroupToGroupMappingShardGroupConfiguration),
                ParseShardGroupConfiguration(parameters.GroupShardGroupConfiguration)
            );

            return new StatusCodeResult(StatusCodes.Status200OK);
        }

        /// <summary>
        /// Deletes the distributed AccessManager instance.
        /// </summary>
        /// <response code="200">The instance was deleted.</response>
        [HttpDelete]
        [Route("distributedInstance")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<StatusCodeResult> DeleteDistributedAccessManagerInstanceAsync([FromBody] DeleteDistributedAccessManagerInstanceParameters parameters)
        {
            await kubernetesDistributedInstanceManager.DeleteDistributedAccessManagerInstanceAsync(parameters.DeletePersistentStorageInstances);

            return new StatusCodeResult(StatusCodes.Status200OK);
        }

        /// <summary>
        /// Splits a shard group in the distributed AccessManager instance, by moving elements whose hash codes fall within a specified range to a new shard group.
        /// </summary>
        /// <response code="200">The split was performed successfully.</response>
        [HttpPost]
        [Route("distributedInstance/shardGroups:split")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<StatusCodeResult> SplitShardGroupAsync([FromBody] SplitShardGroupParameters parameters)
        {
            await kubernetesDistributedInstanceManager.SplitShardGroupAsync
            (
                ParseEnumFromString<DataElement>(parameters.DataElement, nameof(parameters.DataElement)),
                parameters.HashRangeStart, 
                parameters.SplitHashRangeStart, 
                parameters.SplitHashRangeEnd, 
                parameters.EventBatchSize, 
                parameters.SourceWriterNodeOperationsCompleteCheckRetryAttempts, 
                parameters.SourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            return new StatusCodeResult(StatusCodes.Status200OK);
        }

        /// <summary>
        /// Merges a two shard groups with consecutive hash code ranges in the distributed AccessManager instance.
        /// </summary>
        /// <response code="200">The merge was performed successfully.</response>
        [HttpPost]
        [Route("distributedInstance/shardGroups:merge")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<StatusCodeResult> MergeShardGroupsAsync([FromBody] MergeShardGroupsParameters parameters)
        {
            await kubernetesDistributedInstanceManager.MergeShardGroupsAsync
            (
                ParseEnumFromString<DataElement>(parameters.DataElement, nameof(parameters.DataElement)),
                parameters.SourceShardGroup1HashRangeStart,
                parameters.SourceShardGroup2HashRangeEnd,
                parameters.SourceShardGroup2HashRangeEnd,
                parameters.EventBatchSize,
                parameters.SourceWriterNodeOperationsCompleteCheckRetryAttempts,
                parameters.SourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            return new StatusCodeResult(StatusCodes.Status200OK);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Converts <see cref="ApplicationAccess.Redistribution.Kubernetes.Models.KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> to its DTO equivalent object.
        /// </summary>
        /// <returns></returns>
        /// <param name="instanceConfiguration">The instance configuration to convert.</param>
        /// <returns>The instance configuration converted to a DTO.</returns>
        protected InstanceConfiguration ConvertInstanceConfigurationToDto(ApplicationAccess.Redistribution.Kubernetes.Models.KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> instanceConfiguration)
        {
            String distributedOperationRouterUrl;
            String writer1Url;
            String writer2Url;
            String shardConfigurationSqlServerConnectionString;
            String distributedOperationCoordinatorUrl;
            void SetStringVariableIfSourceNotNull(Object source, out String target)
            {
                target = null;
                if (source != null)
                {
                    target = source.ToString();
                }
            }
            SetStringVariableIfSourceNotNull(instanceConfiguration.DistributedOperationRouterUrl, out distributedOperationRouterUrl);
            SetStringVariableIfSourceNotNull(instanceConfiguration.Writer1Url, out writer1Url);
            SetStringVariableIfSourceNotNull(instanceConfiguration.Writer2Url, out writer2Url);
            SetStringVariableIfSourceNotNull(instanceConfiguration.ShardConfigurationPersistentStorageCredentials, out shardConfigurationSqlServerConnectionString);
            SetStringVariableIfSourceNotNull(instanceConfiguration.DistributedOperationCoordinatorUrl, out distributedOperationCoordinatorUrl);
            List<KubernetesShardGroupConfiguration> ConvertShardGroupConfiguration(IList<KubernetesShardGroupConfiguration<SqlServerLoginCredentials>> inputConfiguration)
            {
                List<KubernetesShardGroupConfiguration> returnConfiguration = null;
                if (inputConfiguration != null)
                {
                    returnConfiguration = new List<KubernetesShardGroupConfiguration>();
                    foreach(KubernetesShardGroupConfiguration<SqlServerLoginCredentials> currentInputConfiguration in inputConfiguration)
                    {
                        returnConfiguration.Add
                        (
                            new KubernetesShardGroupConfiguration
                            {
                                HashRangeStart = currentInputConfiguration.HashRangeStart, 
                                SqlServerConnectionString = currentInputConfiguration.PersistentStorageCredentials.ConnectionString, 
                                ReaderNodeClientUrl = currentInputConfiguration.ReaderNodeClientConfiguration.BaseUrl.ToString(),
                                WriterNodeClientUrl = currentInputConfiguration.WriterNodeClientConfiguration.BaseUrl.ToString()
                            }
                        );
                    }
                }

                return returnConfiguration;
            }

            return new InstanceConfiguration
            {
                DistributedOperationRouterUrl = distributedOperationRouterUrl, 
                Writer1Url = writer1Url,
                Writer2Url = writer2Url,
                ShardConfigurationSqlServerConnectionString = shardConfigurationSqlServerConnectionString, 
                UserShardGroupConfiguration = ConvertShardGroupConfiguration(instanceConfiguration.UserShardGroupConfiguration),
                GroupToGroupMappingShardGroupConfiguration = ConvertShardGroupConfiguration(instanceConfiguration.GroupToGroupMappingShardGroupConfiguration),
                GroupShardGroupConfiguration = ConvertShardGroupConfiguration(instanceConfiguration.GroupShardGroupConfiguration),
                DistributedOperationCoordinatorUrl = distributedOperationCoordinatorUrl
            };
        }

        /// <summary>
        /// Parses/converts a list of <see cref="ShardGroupConfiguration"/> DTO objects to a list of <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/>.
        /// </summary>
        /// <param name="ShardGroupConfigurationDtos">The <see cref="ShardGroupConfiguration"/> DTO parse.</param>
        /// <returns>The converted <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/>.</returns>
        protected List<ShardGroupConfiguration<SqlServerLoginCredentials>> ParseShardGroupConfiguration(List<ShardGroupConfiguration> ShardGroupConfigurationDtos)
        {
            List<ShardGroupConfiguration<SqlServerLoginCredentials>> returnConfiguration = new();
            foreach (ShardGroupConfiguration currentShardGroupConfigurationDto in ShardGroupConfigurationDtos)
            {
                var newShardGroupConfiguration = new ShardGroupConfiguration<SqlServerLoginCredentials>(currentShardGroupConfigurationDto.HashRangeStart);
                returnConfiguration.Add(newShardGroupConfiguration);
            }

            return returnConfiguration;
        }

        /// <summary>
        /// Attempts to parse a stringified URL.
        /// </summary>
        /// <param name="stringifiedUrl">The stringified URL to parse.</param>
        /// <param name="stringifiedUrlParameterName">The name of the parameter holding the stringified URL.</param>
        /// <returns>The parsed and converted URL.</returns>
        protected Uri ParseUriFromString(String stringifiedUrl, String stringifiedUrlParameterName)
        {
            try
            {
                return new Uri(stringifiedUrl);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Parameter value '{stringifiedUrl}' could not be parsed as a URL.", stringifiedUrlParameterName, e);
            }
        }

        /// <summary>
        /// Attempts to parse a stringified enum value.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="stringifiedEnum">The stringified enum value to parse.</param>
        /// <param name="stringifiedEnumParameterName">The name of the parameter holding the stringified enum value.</param>
        /// <returns>The parsed and converted enum value.</returns>
        /// <exception cref="ArgumentException"></exception>
        protected T ParseEnumFromString<T>(String stringifiedEnum, String stringifiedEnumParameterName) where T : struct, Enum
        {
            try
            {
                return Enum.Parse<T>(stringifiedEnum);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Parameter value '{stringifiedEnum}' could not be parsed as a {typeof(T).Name}.", stringifiedEnumParameterName, e);
            }
        }

        #endregion
    }
}
