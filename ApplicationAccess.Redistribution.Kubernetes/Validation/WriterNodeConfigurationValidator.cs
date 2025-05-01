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
    /// Validator for <see cref="WriterNodeConfiguration"/> instances.
    /// </summary>
    public class WriterNodeConfigurationValidator : NodeConfigurationBaseValidator<WriterNodeConfiguration>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.Validation.WriterNodeConfigurationValidator class.
        /// </summary>
        public WriterNodeConfigurationValidator()
        {
            nodeConfigurationClassName = typeof(WriterNodeConfiguration).Name;
        }

        /// <summary>
        /// Validates the specified <see cref="WriterNodeConfiguration"/>.
        /// </summary>
        /// <param name="nodeConfiguration">The <see cref="WriterNodeConfiguration"/> to validate.</param>
        public override void Validate(WriterNodeConfiguration nodeConfiguration)
        {
            base.Validate(nodeConfiguration);
            ThrowExceptionIfPropertyNull(nodeConfigurationClassName, nodeConfiguration.PersistentVolumeClaimName, nameof(nodeConfiguration.PersistentVolumeClaimName));
        }
    }
}
