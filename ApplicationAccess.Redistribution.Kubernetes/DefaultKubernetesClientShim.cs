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
using System.Threading.Tasks;
using k8s;
using k8s.Models;

namespace ApplicationAccess.Redistribution.Kubernetes
{
    /// <summary>
    /// Default implementation of <see cref="IKubernetesClientShim"/>.
    /// </summary>
    public class DefaultKubernetesClientShim : IKubernetesClientShim
    {
        /// <inheritdoc/>
        public async Task<V1Deployment> CreateNamespacedDeploymentAsync(k8s.Kubernetes kubernetesClient, V1Deployment body, String namespaceParameter)
        {
            return await kubernetesClient.CreateNamespacedDeploymentAsync(body, namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1DeploymentList> ListNamespacedDeploymentAsync(k8s.Kubernetes kubernetesClient, String namespaceParameter)
        {
            return await kubernetesClient.ListNamespacedDeploymentAsync(namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1Status> DeleteNamespacedDeploymentAsync(k8s.Kubernetes kubernetesClient, String deploymentName, String namespaceParameter)
        {
            return await kubernetesClient.DeleteNamespacedDeploymentAsync(deploymentName, namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1Service> CreateNamespacedServiceAsync(k8s.Kubernetes kubernetesClient, V1Service body, String namespaceParameter)
        {
            return await kubernetesClient.CreateNamespacedServiceAsync(body, namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1Service> PatchNamespacedServiceAsync(k8s.Kubernetes kubernetesClient, V1Patch body, String serviceName, String namespaceParameter)
        {
            return await kubernetesClient.PatchNamespacedServiceAsync(body, serviceName, namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1ServiceList> ListNamespacedServiceAsync(k8s.Kubernetes kubernetesClient, String namespaceParameter)
        {
            return await kubernetesClient.ListNamespacedServiceAsync(namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1Service> DeleteNamespacedServiceAsync(k8s.Kubernetes kubernetesClient, String serviceName, String namespaceParameter)
        {
            return await kubernetesClient.DeleteNamespacedServiceAsync(serviceName, namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1Scale> PatchNamespacedDeploymentScaleAsync(k8s.Kubernetes kubernetesClient, V1Patch body, String name, String namespaceParameter)
        {
            return await kubernetesClient.PatchNamespacedDeploymentScaleAsync(body, name, namespaceParameter);
        }

        /// <inheritdoc/>
        public async Task<V1PodList> ListNamespacedPodAsync(k8s.Kubernetes kubernetesClient, String namespaceParameter)
        {
            return await kubernetesClient.ListNamespacedPodAsync(namespaceParameter);
        }
    }
}
