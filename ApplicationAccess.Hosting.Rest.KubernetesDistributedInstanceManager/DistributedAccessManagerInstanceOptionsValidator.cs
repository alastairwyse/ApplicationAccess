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
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;
using ApplicationAccess.Redistribution.Models;
using k8s.Models;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// Validates a <see cref="DistributedAccessManagerInstanceOptions"/> instance.
    /// </summary>
    public class DistributedAccessManagerInstanceOptionsValidator
    {
        /// <summary>Contains utility method for Options classes.</summary>
        protected OptionsUtilities optionsUtilities;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.DistributedAccessManagerInstanceOptionsValidator class.
        /// </summary>
        public DistributedAccessManagerInstanceOptionsValidator()
        {
            optionsUtilities = new OptionsUtilities();
        }

        /// <summary>
        /// Validates a <see cref="DistributedAccessManagerInstanceOptions"/> instance.
        /// </summary>
        /// <param name="distributedAccessManagerInstanceOptions">The <see cref="DistributedAccessManagerInstanceOptions"/> instance to validate.</param>
        public void Validate(DistributedAccessManagerInstanceOptions distributedAccessManagerInstanceOptions)
        {
            // Validate data annotations in the top level object
            var validationContext = new ValidationContext(distributedAccessManagerInstanceOptions);
            Validator.ValidateObject(distributedAccessManagerInstanceOptions, validationContext, true);

            // Validate the nested options
            void ValidateOptions(Object optionsObject)
            {
                validationContext = new ValidationContext(optionsObject);
                try
                {
                    Validator.ValidateObject(optionsObject, validationContext, true);
                }
                catch (Exception e)
                {
                    throw new ValidationException(GenerateExceptionMessagePrefix(), e);
                }
            }
            ValidateOptions(distributedAccessManagerInstanceOptions.SqlServerDatabaseConnection);
            ValidateOptions(distributedAccessManagerInstanceOptions.ShardConnection);
            ValidateOptions(distributedAccessManagerInstanceOptions.StaticConfiguration);
            ValidateOptions(distributedAccessManagerInstanceOptions.StaticConfiguration.ReaderNodeConfigurationTemplate);
            ValidateOptions(distributedAccessManagerInstanceOptions.StaticConfiguration.EventCacheNodeConfigurationTemplate);
            ValidateOptions(distributedAccessManagerInstanceOptions.StaticConfiguration.WriterNodeConfigurationTemplate);
            ValidateOptions(distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate);
            ValidateOptions(distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate);

            // Validate instance configuration
            if (distributedAccessManagerInstanceOptions.InstanceConfiguration != null)
            {
                ValidateOptions(distributedAccessManagerInstanceOptions.InstanceConfiguration);
                void ValidateShardGroupConfiguration(List<ShardGroupConfigurationOptions> shardGroupConfigurationOptionsList)
                {
                    if (shardGroupConfigurationOptionsList != null)
                    {
                        foreach (ShardGroupConfigurationOptions currentConfigurationOptions in shardGroupConfigurationOptionsList)
                        {
                            ValidateOptions(currentConfigurationOptions);
                        }
                    }
                }
                ValidateShardGroupConfiguration(distributedAccessManagerInstanceOptions.InstanceConfiguration.UserShardGroupConfiguration);
                ValidateShardGroupConfiguration(distributedAccessManagerInstanceOptions.InstanceConfiguration.GroupToGroupMappingShardGroupConfiguration);
                ValidateShardGroupConfiguration(distributedAccessManagerInstanceOptions.InstanceConfiguration.GroupShardGroupConfiguration);
            }

            // Validate Kubernetes resource values within the static configuration
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.ReaderNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.ReaderNodeConfigurationTemplate.CpuResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.ReaderNodeConfigurationTemplate.CpuResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.ReaderNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.ReaderNodeConfigurationTemplate.MemoryResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.ReaderNodeConfigurationTemplate.MemoryResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.EventCacheNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.EventCacheNodeConfigurationTemplate.CpuResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.EventCacheNodeConfigurationTemplate.CpuResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.EventCacheNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.EventCacheNodeConfigurationTemplate.MemoryResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.EventCacheNodeConfigurationTemplate.MemoryResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.WriterNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.WriterNodeConfigurationTemplate.CpuResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.WriterNodeConfigurationTemplate.CpuResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.WriterNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.WriterNodeConfigurationTemplate.MemoryResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.WriterNodeConfigurationTemplate.MemoryResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.CpuResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.CpuResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.MemoryResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.MemoryResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.CpuResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.CpuResourceRequest);
            ValidateKubernetesResourceValue(nameof(StaticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate), nameof(distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.MemoryResourceRequest), distributedAccessManagerInstanceOptions.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.MemoryResourceRequest);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Validates a Kubernetes resource value from a node template within the distributed instance options static configuration.
        /// </summary>
        /// <param name="nodeConfigurationTemplatePropertyName">The name of the node template configuration containing the resource value.</param>
        /// <param name="resourceValuePropertyName">The name of the resource value.</param>
        /// <param name="resourceValue">The resource value.</param>
        protected void ValidateKubernetesResourceValue(String nodeConfigurationTemplatePropertyName, String resourceValuePropertyName, String resourceValue)
        {
            try
            {
                ResourceQuantity resourceQuantity = new(resourceValue);
            }
            catch (Exception e)
            {
                try
                {
                    throw new ValidationException($"Error validating {StaticConfigurationOptions.StaticConfigurationOptionsName} options.  Error validating {nodeConfigurationTemplatePropertyName} options.  Value '{resourceValue}' for '{resourceValuePropertyName}' is invalid.", e);
                }
                catch (Exception e2)
                {
                    throw new ValidationException(GenerateExceptionMessagePrefix(), e2);
                }
            }
        }

        #pragma warning disable 1591

        protected String GenerateExceptionMessagePrefix()
        {
            return optionsUtilities.GenerateValidationExceptionMessagePrefix(DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName);
        }

        #pragma warning restore 1591

        #endregion
    }
}
