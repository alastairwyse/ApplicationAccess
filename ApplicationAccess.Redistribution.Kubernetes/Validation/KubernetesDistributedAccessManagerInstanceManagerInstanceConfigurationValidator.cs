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
using System.Linq;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Models;

namespace ApplicationAccess.Redistribution.Kubernetes.Validation
{
    /// <summary>
    /// Validator for <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instances.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManagerInstanceConfigurationValidator<TPersistentStorageCredentials> 
        where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
    {
        /// <summary>
        /// Validates the specified <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/>.
        /// </summary>
        /// <param name="instanceConfiguration">The <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> to validate.</param>
        public void Validate(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration)
        {
            // All *ShardGroupConfiguration properties must all null, or all non-null
            if (instanceConfiguration.UserShardGroupConfiguration == null || instanceConfiguration.GroupToGroupMappingShardGroupConfiguration == null || instanceConfiguration.GroupShardGroupConfiguration == null)
            {
                if (instanceConfiguration.UserShardGroupConfiguration != null || instanceConfiguration.GroupToGroupMappingShardGroupConfiguration != null || instanceConfiguration.GroupShardGroupConfiguration != null)
                {
                    throw new ArgumentException($"{nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>)} '*ShardGroupConfiguration' properties must be either all null or all non-null.");
                }
            }
            if (instanceConfiguration.UserShardGroupConfiguration == null)
            {
                if (instanceConfiguration.DistributedOperationCoordinatorUrl != null)
                {
                    throw new ArgumentException($"{nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>)} '{nameof(instanceConfiguration.DistributedOperationCoordinatorUrl)}' property must null when the '*ShardGroupConfiguration' properties are null.", nameof(instanceConfiguration.DistributedOperationCoordinatorUrl));
                }
            }
            else
            {
                if (instanceConfiguration.UserShardGroupConfiguration.Count == 0)
                    throw new ArgumentException($"{nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>)} property '{nameof(instanceConfiguration.UserShardGroupConfiguration)}' cannot be empty.", nameof(instanceConfiguration.UserShardGroupConfiguration));
                if (instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count != 1)
                    throw new ArgumentException($"{nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>)} property '{nameof(instanceConfiguration.GroupToGroupMappingShardGroupConfiguration)}' must contain a single value (actually contained {instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count}).  Only a single group to group mapping shard group is supported.", nameof(instanceConfiguration.GroupToGroupMappingShardGroupConfiguration));
                if (instanceConfiguration.GroupShardGroupConfiguration.Count == 0)
                    throw new ArgumentException($"{nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>)} property '{nameof(instanceConfiguration.GroupShardGroupConfiguration)}' cannot be empty.", nameof(instanceConfiguration.GroupShardGroupConfiguration));
                ValidateShardGroupConfigurationList(nameof(instanceConfiguration.UserShardGroupConfiguration), instanceConfiguration.UserShardGroupConfiguration);
                ValidateShardGroupConfigurationList(nameof(instanceConfiguration.GroupToGroupMappingShardGroupConfiguration), instanceConfiguration.GroupToGroupMappingShardGroupConfiguration);
                ValidateShardGroupConfigurationList(nameof(instanceConfiguration.GroupShardGroupConfiguration), instanceConfiguration.GroupShardGroupConfiguration);
                ThrowExceptionIfPropertyNullWhenShardGroupConfigurationIsPopulated(nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>), instanceConfiguration.DistributedOperationRouterUrl, nameof(instanceConfiguration.DistributedOperationRouterUrl));
                ThrowExceptionIfPropertyNullWhenShardGroupConfigurationIsPopulated(nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>), instanceConfiguration.Writer1Url, nameof(instanceConfiguration.Writer1Url));
                ThrowExceptionIfPropertyNullWhenShardGroupConfigurationIsPopulated(nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>), instanceConfiguration.Writer2Url, nameof(instanceConfiguration.Writer2Url));
                ThrowExceptionIfPropertyNullWhenShardGroupConfigurationIsPopulated(nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>), instanceConfiguration.ShardConfigurationPersistentStorageCredentials, nameof(instanceConfiguration.ShardConfigurationPersistentStorageCredentials));
                ThrowExceptionIfPropertyNullWhenShardGroupConfigurationIsPopulated(nameof(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>), instanceConfiguration.DistributedOperationCoordinatorUrl, nameof(instanceConfiguration.DistributedOperationCoordinatorUrl));
            }
        }

        /// <summary>
        /// Validates a property or method parameter containing a list of <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/> objects.
        /// </summary>
        /// <param name="parameterName">The name of the property or parameter.</param>
        /// <param name="parameterValue">The value of the property or parameter.</param>
        /// <typeparam name="TShardGroupConfiguration">Subclass or instance of <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/>.</typeparam>
        public void ValidateShardGroupConfigurationList<TShardGroupConfiguration>(String parameterName, IList<TShardGroupConfiguration> parameterValue)
            where TShardGroupConfiguration : ShardGroupConfiguration<TPersistentStorageCredentials>
        {
            HashSet<Int32> allHashRangeStartValues = new();
            Int32 minHashRangeStartValue = Int32.MaxValue;
            foreach (ShardGroupConfiguration<TPersistentStorageCredentials> currentParameterValue in parameterValue)
            {
                if (currentParameterValue.HashRangeStart < minHashRangeStartValue)
                {
                    minHashRangeStartValue = currentParameterValue.HashRangeStart;
                }
                if (allHashRangeStartValues.Contains(currentParameterValue.HashRangeStart) == true)
                {
                    throw new ArgumentException($"Property or parameter '{parameterName}' contains duplicate hash range start value {currentParameterValue.HashRangeStart}.", parameterName);
                }
                allHashRangeStartValues.Add(currentParameterValue.HashRangeStart);
            }
            if (minHashRangeStartValue != Int32.MinValue)
            {
                throw new ArgumentException($"Property or parameter '{parameterName}' must contain one element with value {Int32.MinValue}.", parameterName);
            }
        }

        #region Private/Protected Methods

        #pragma warning disable 1591

        protected void ThrowExceptionIfPropertyNullWhenShardGroupConfigurationIsPopulated(String className, Object propertyValue, String propertyName)
        {
            if (propertyValue == null)
                throw new ArgumentNullException(propertyName, $"{className} property '{propertyName}' cannot be null when the '*ShardGroupConfiguration' properties are non-null.");
        }

        #pragma warning restore 1591

        #endregion
    }
}
