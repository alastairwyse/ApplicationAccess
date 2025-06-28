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
using System.ComponentModel.DataAnnotations;
using System.Text;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution.Kubernetes.Models;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// Parses <see cref="DistributedAccessManagerInstanceOptions"/>, converting them to a <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance.
    /// </summary>
    public class KubernetesDistributedInstanceManagerInstanceConfigurationOptionsParser
    {
        /// <summary>
        /// Parses <see cref="DistributedAccessManagerInstanceOptions"/>, converting them to a <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance.
        /// </summary>
        /// <param name="instanceConfigurationOptions">The <see cref="DistributedAccessManagerInstanceOptions"/> to parse.</param>
        /// <returns>The <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance.</returns>
        public KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> Parse(InstanceConfigurationOptions instanceConfigurationOptions)
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> returnInstanceConfiguration = null;
            if (instanceConfigurationOptions != null)
            {
                List<KubernetesShardGroupConfiguration<SqlServerLoginCredentials>> ConvertShardGroupConfigurationOptions(List<ShardGroupConfigurationOptions> shardGroupConfigurationOptions)
                {
                    List<KubernetesShardGroupConfiguration<SqlServerLoginCredentials>> returnConfiguration = null;
                    if (shardGroupConfigurationOptions != null)
                    {
                        returnConfiguration = new List<KubernetesShardGroupConfiguration<SqlServerLoginCredentials>>();
                        foreach (ShardGroupConfigurationOptions currentShardGroupConfigurationOptions in shardGroupConfigurationOptions)
                        {
                            Uri readerNodeUrl = ValidateAndConvertOptionsUrl
                            (
                                currentShardGroupConfigurationOptions.ReaderNodeClientUrl,
                                new List<String> { DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName, InstanceConfigurationOptions.InstanceConfigurationOptionsName, ShardGroupConfigurationOptions.ShardGroupConfigurationOptionsName },
                                $"'{nameof(currentShardGroupConfigurationOptions.ReaderNodeClientUrl)}' with value '{currentShardGroupConfigurationOptions.ReaderNodeClientUrl}' contains an invalid URL."
                            );
                            Uri writerNodeUrl = ValidateAndConvertOptionsUrl
                            (
                                currentShardGroupConfigurationOptions.WriterNodeClientUrl,
                                new List<String> { DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName, InstanceConfigurationOptions.InstanceConfigurationOptionsName, ShardGroupConfigurationOptions.ShardGroupConfigurationOptionsName },
                                $"'{nameof(currentShardGroupConfigurationOptions.WriterNodeClientUrl)}' with value '{currentShardGroupConfigurationOptions.WriterNodeClientUrl}' contains an invalid URL."
                            );
                            KubernetesShardGroupConfiguration<SqlServerLoginCredentials> newConfiguration = new
                            (
                                currentShardGroupConfigurationOptions.HashRangeStart.Value,
                                new SqlServerLoginCredentials(currentShardGroupConfigurationOptions.SqlServerConnectionString),
                                new AccessManagerRestClientConfiguration(readerNodeUrl),
                                new AccessManagerRestClientConfiguration(writerNodeUrl)
                            );
                            returnConfiguration.Add(newConfiguration);
                        }
                    }

                    return returnConfiguration;
                }

                Uri ValidateAndConvertInstanceOptionsUrl(String urlValue, String instanceOptionsPropertyName)
                {
                    return ValidateAndConvertOptionsUrl
                    (
                        urlValue,
                        new List<String> { DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName, InstanceConfigurationOptions.InstanceConfigurationOptionsName },
                        $"'{instanceOptionsPropertyName}' with value '{urlValue}' contains an invalid URL."
                    );
                }
                Uri distributedOperationRouterUrl = null;
                if (instanceConfigurationOptions.DistributedOperationRouterUrl != null)
                {
                    distributedOperationRouterUrl = ValidateAndConvertInstanceOptionsUrl(instanceConfigurationOptions.DistributedOperationRouterUrl, nameof(instanceConfigurationOptions.DistributedOperationRouterUrl));
                }
                Uri writer1Url = null;
                if (instanceConfigurationOptions.Writer1Url != null)
                {
                    writer1Url = ValidateAndConvertInstanceOptionsUrl(instanceConfigurationOptions.Writer1Url, nameof(instanceConfigurationOptions.Writer1Url));
                }
                Uri writer2Url = null;
                if (instanceConfigurationOptions.Writer2Url != null)
                {
                    writer2Url = ValidateAndConvertInstanceOptionsUrl(instanceConfigurationOptions.Writer2Url, nameof(instanceConfigurationOptions.Writer2Url));
                }
                SqlServerLoginCredentials shardConfigurationPersistentStorageCredentials = null;
                if (instanceConfigurationOptions.ShardConfigurationSqlServerConnectionString != null)
                {
                    shardConfigurationPersistentStorageCredentials = new SqlServerLoginCredentials(instanceConfigurationOptions.ShardConfigurationSqlServerConnectionString);
                }
                Uri distributedOperationCoordinatorUrl = null;
                if (instanceConfigurationOptions.DistributedOperationCoordinatorUrl != null)
                {
                    distributedOperationCoordinatorUrl = ValidateAndConvertInstanceOptionsUrl(instanceConfigurationOptions.DistributedOperationCoordinatorUrl, nameof(instanceConfigurationOptions.DistributedOperationCoordinatorUrl));
                }

                List<KubernetesShardGroupConfiguration<SqlServerLoginCredentials>> userShardGroupConfiguration = ConvertShardGroupConfigurationOptions(instanceConfigurationOptions.UserShardGroupConfiguration);
                List<KubernetesShardGroupConfiguration<SqlServerLoginCredentials>> groupToGroupMappingShardGroupConfiguration = ConvertShardGroupConfigurationOptions(instanceConfigurationOptions.GroupToGroupMappingShardGroupConfiguration);
                List<KubernetesShardGroupConfiguration<SqlServerLoginCredentials>> groupShardGroupConfiguration = ConvertShardGroupConfigurationOptions(instanceConfigurationOptions.GroupShardGroupConfiguration);
                returnInstanceConfiguration = new()
                {
                    DistributedOperationRouterUrl = distributedOperationRouterUrl,
                    Writer1Url = writer1Url,
                    Writer2Url = writer2Url,
                    ShardConfigurationPersistentStorageCredentials = shardConfigurationPersistentStorageCredentials,
                    UserShardGroupConfiguration = userShardGroupConfiguration,
                    GroupToGroupMappingShardGroupConfiguration = groupToGroupMappingShardGroupConfiguration,
                    GroupShardGroupConfiguration = groupShardGroupConfiguration,
                    DistributedOperationCoordinatorUrl = distributedOperationCoordinatorUrl
                };
            }

            return returnInstanceConfiguration;
        }
        
        #region Private/Protected Methods

        /// <summary>
        /// Validates a stringified URL value contained in an <see cref="IOptions{TOptions}"/> object and returns it as a <see cref="Uri"/>.
        /// </summary>
        /// <param name="urlValue">The stringified URL value.</param>
        /// <param name="optionsParentObjectNamePath">The hierarchy of names of <see cref="IOptions{TOptions}"/> objects wrapping the URL property, starting at the outermost object (For use in exception messages).</param>
        /// <param name="exceptionMessage">The message for the exception to throw is validation fails.</param>
        /// <returns>The <see cref="Uri"/>.</returns>
        protected Uri ValidateAndConvertOptionsUrl(String urlValue, IEnumerable<String> optionsParentObjectNamePath, String exceptionMessage)
        {
            Uri returnUrl = null;
            try
            {
                returnUrl = new Uri(urlValue);
            }
            catch (Exception e)
            {
                StringBuilder exceptionMessageBuilder = new();
                foreach (String currentParentObjectName in optionsParentObjectNamePath)
                {
                    exceptionMessageBuilder.Append($"Error validating {currentParentObjectName} options.  ");
                }
                exceptionMessageBuilder.Append(exceptionMessage);
                throw new ValidationException(exceptionMessageBuilder.ToString(), e);
            }

            return returnUrl;
        }

        #endregion
    }
}
