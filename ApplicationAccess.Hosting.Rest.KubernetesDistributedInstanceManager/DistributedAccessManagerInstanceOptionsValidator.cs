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
                    foreach (ShardGroupConfigurationOptions currentConfigurationOptions in shardGroupConfigurationOptionsList)
                    {
                        ValidateOptions(currentConfigurationOptions);
                    }
                }
                ValidateShardGroupConfiguration(distributedAccessManagerInstanceOptions.InstanceConfiguration.UserShardGroupConfiguration);
                ValidateShardGroupConfiguration(distributedAccessManagerInstanceOptions.InstanceConfiguration.GroupToGroupMappingShardGroupConfiguration);
                ValidateShardGroupConfiguration(distributedAccessManagerInstanceOptions.InstanceConfiguration.GroupShardGroupConfiguration);
            }
        }

        #region Private/Protected Methods

        #pragma warning disable 1591

        protected String GenerateExceptionMessagePrefix()
        {
            return optionsUtilities.GenerateValidationExceptionMessagePrefix(DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName);
        }

        #pragma warning restore 1591

        #endregion
    }
}
