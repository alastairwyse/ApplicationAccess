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
    /// Model/container class holding static configuration for a <see cref="KubernetesDistributedAccessManagerInstanceManager{TPersistentStorageCredentials}"/> instance.
    /// </summary>
    public record KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration
    {
        /// <summary>The port to use to expose trafic between the pods/deployments.</summary>
        public required UInt16 PodPort { get; init; }

        /// <summary>The port to use to expose the AccessManager instance outside the Kubernetes cluster.</summary>
        public required UInt16 ExternalPort { get; init; }

        /// <summary>The Kubernetes namespace to create and manage the distributed AccessManager implementation in.</summary>
        public required String NameSpace { get; init; }

        /// <summary>Prefix to use in names for persistent instances created by the manager.</summary>
        /// <remarks>Should only contain printable ASCII characters without whitespace.</remarks>
        public required String PersistentStorageInstanceNamePrefix { get; init; }

        /// <summary>Whether HTTPS should be used to connect to load balancer services exposed outside the Kubernetes cluster.</summary>
        public required Boolean LoadBalancerServicesHttps { get; init; }

        /// <summary>The protocol to use to connect between nodes.</summary>
        public required Protocol NodeInterconnectionProtocol { get; init; }

        /// <summary>
        /// The time in milliseconds between polls to check whether a deployment has either become available or scaled down.
        /// </summary>
        public required Int32 DeploymentWaitPollingInterval { get; init; }

        /// <summary>
        /// The time in milliseconds to wait for a service to become available after creation, before throwing an exception.
        /// </summary>
        public required Int32 ServiceAvailabilityWaitAbortTimeout { get; init; }

        /// <summary>
        /// The time in milliseconds to wait for distributed operation coordinator configuration changes to be applied, over and above the refresh interval configured in the operation coordinator's appsettings.
        /// </summary>
        public required Int32 DistributedOperationCoordinatorRefreshIntervalWaitBuffer { get; init; }

        /// <summary>Whether the reader and writer node appsettings configuration should be updated with credentials from the <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}.ShardConfigurationPersistentStorageCredentials">ShardConfigurationPersistentStorageCredentials</see> configuration.</summary>
        /// <remarks>Generally this should be set true, however it should be set false if no appsettings 'DatabaseConnection' details for the reader and writer nodes are specified (i.e. when data persistence is disabled).</remarks>
        public required Boolean SetReaderWriterNodePersistentStorageCredentials { get; init; }

        /// <summary>Base/template for configuration of reader node pods/deployments within the distributed AccessManager implementation.</summary>
        public required ReaderNodeConfiguration ReaderNodeConfigurationTemplate { get; init; }

        /// <summary>Base/template for configuration of event cache node pods/deployments within the distributed AccessManager implementation.</summary>
        public required EventCacheNodeConfiguration EventCacheNodeConfigurationTemplate { get; init; }

        /// <summary>Base/template for configuration of writer node pods/deployments within the distributed AccessManager implementation.</summary>
        public required WriterNodeConfiguration WriterNodeConfigurationTemplate { get; init; }

        /// <summary>Base/template for configuration of distributed operation coordinator node pods/deployments within the distributed AccessManager implementation.</summary>
        public required DistributedOperationCoordinatorNodeConfiguration DistributedOperationCoordinatorNodeConfigurationTemplate { get; init; }

        /// <summary>Base/template for configuration of distributed operation router node pods/deployments within the distributed AccessManager implementation.</summary>
        public required DistributedOperationRouterNodeConfiguration DistributedOperationRouterNodeConfigurationTemplate { get; init; }
    }
}
