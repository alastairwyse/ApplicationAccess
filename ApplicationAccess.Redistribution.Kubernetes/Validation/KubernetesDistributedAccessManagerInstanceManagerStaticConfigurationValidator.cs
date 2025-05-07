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
using ApplicationAccess.Redistribution.Kubernetes.Models;

namespace ApplicationAccess.Redistribution.Kubernetes.Validation
{
    /// <summary>
    /// Validator for <see cref="KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration"/> instances.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManagerStaticConfigurationValidator : ValidatorBase
    {
        /// <summary>
        /// Validates the specified <see cref="KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration"/>.
        /// </summary>
        /// <param name="staticConfiguration">The <see cref="KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration"/> to validate.</param>
        public void Validate(KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration)
        {
            ThrowExceptionIfPropertyNull(typeof(KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration).Name, staticConfiguration.NameSpace, nameof(staticConfiguration.NameSpace));
            ThrowExceptionIfPropertyNull(typeof(KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration).Name, staticConfiguration.PersistentStorageInstanceNamePrefix, nameof(staticConfiguration.PersistentStorageInstanceNamePrefix));
            if (staticConfiguration.DeploymentWaitPollingInterval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(staticConfiguration.DeploymentWaitPollingInterval), $"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property '{nameof(staticConfiguration.DeploymentWaitPollingInterval)}' with value {staticConfiguration.DeploymentWaitPollingInterval} must be greater than 0.");
            }
            if (staticConfiguration.ServiceAvailabilityWaitAbortTimeout < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(staticConfiguration.ServiceAvailabilityWaitAbortTimeout), $"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property '{nameof(staticConfiguration.ServiceAvailabilityWaitAbortTimeout)}' with value {staticConfiguration.ServiceAvailabilityWaitAbortTimeout} must be greater than 0.");
            }
            if (staticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer), $"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property '{nameof(staticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer)}' with value {staticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer} must be greater than or equal to 0.");
            }
            var readerNodeConfigurationValidator = new ReaderNodeConfigurationValidator();
            readerNodeConfigurationValidator.Validate(staticConfiguration.ReaderNodeConfigurationTemplate);
            var eventCacheNodeConfigurationValidator = new NodeConfigurationBaseValidator<EventCacheNodeConfiguration>();
            eventCacheNodeConfigurationValidator.Validate(staticConfiguration.EventCacheNodeConfigurationTemplate);
            var writerNodeConfigurationValidator = new WriterNodeConfigurationValidator();
            writerNodeConfigurationValidator.Validate(staticConfiguration.WriterNodeConfigurationTemplate);
            var distributedOperationCoordinatorNodeConfigurationValidator = new DistributedOperationCoordinatorNodeConfigurationValidator();
            distributedOperationCoordinatorNodeConfigurationValidator.Validate(staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate);
            var distributedOperationRouterNodeConfigurationValidator = new NodeConfigurationBaseValidator<DistributedOperationRouterNodeConfiguration>();
            distributedOperationRouterNodeConfigurationValidator.Validate(staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate);
        }
    }
}
