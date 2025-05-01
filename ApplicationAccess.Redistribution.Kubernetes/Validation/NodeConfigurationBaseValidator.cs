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
using k8s.Models;

namespace ApplicationAccess.Redistribution.Kubernetes.Validation
{
    /// <summary>
    /// Validator for <see cref="NodeConfigurationBase"/> instances.
    /// </summary>
    public class NodeConfigurationBaseValidator<T> : ValidatorBase where T : NodeConfigurationBase
    {
        /// <summary>The name representing <see cref="NodeConfigurationBase"/> instance to use in exception messages.</summary>
        protected String nodeConfigurationClassName = "NodeConfiguration";

        /// <summary>
        /// Validates the specified <see cref="NodeConfigurationBase"/>.
        /// </summary>
        /// <param name="nodeConfiguration">The <see cref="NodeConfigurationBase"/> to validate.</param>
        public virtual void Validate(T nodeConfiguration)
        {
            ThrowExceptionIfPropertyNull(nodeConfigurationClassName, nodeConfiguration.ContainerImage, nameof(nodeConfiguration.ContainerImage));
            ThrowExceptionIfPropertyNull(nodeConfigurationClassName, nodeConfiguration.AppSettingsConfigurationTemplate, nameof(nodeConfiguration.AppSettingsConfigurationTemplate));
            ThrowExceptionIfPropertyNull(nodeConfigurationClassName, nodeConfiguration.CpuResourceRequest, nameof(nodeConfiguration.CpuResourceRequest));
            ThrowExceptionIfPropertyNull(nodeConfigurationClassName, nodeConfiguration.MemoryResourceRequest, nameof(nodeConfiguration.MemoryResourceRequest));
            ValidateResourceQuantityProperty(nodeConfiguration.CpuResourceRequest, nameof(nodeConfiguration.CpuResourceRequest));
            ValidateResourceQuantityProperty(nodeConfiguration.MemoryResourceRequest, nameof(nodeConfiguration.MemoryResourceRequest));
        }

        #pragma warning disable 1591

        protected void ValidateResourceQuantityProperty(String propertyValue, String propertyName)
        {
            try
            {
                var resourceQuantity = new ResourceQuantity(propertyValue);
                resourceQuantity.Validate();
            }
            catch (Exception e)
            {
                throw new ArgumentException($"{nodeConfigurationClassName} property '{propertyName}' with value '{propertyValue}' failed to validate.", propertyName, e);
            }
        }

        #pragma warning restore 1591
    }
}
