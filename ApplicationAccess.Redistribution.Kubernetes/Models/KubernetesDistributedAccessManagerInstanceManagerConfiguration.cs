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

namespace ApplicationAccess.Redistribution.Kubernetes.Models
{
    /// <summary>
    /// Model/container class holding configuration for a <see cref="KubernetesDistributedAccessManagerInstanceManager"/> instance.
    /// </summary>
    public record KubernetesDistributedAccessManagerInstanceManagerConfiguration
    {
        /// <summary>The port to use to expose trafic between the pods/deployments.</summary>
        public required UInt16 PodPort { get; init; }

        /// <summary>Base/template for configuration of reader node pods/deployments within the distributed AccessManager implementation.</summary>
        public required ReaderNodeConfiguration ReaderNodeConfigurationTemplate { get; init; }
    }
}
