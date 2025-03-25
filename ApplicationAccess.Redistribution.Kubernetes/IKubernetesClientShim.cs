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
    /// Defines methods which operate a Kubernetes cluster.
    /// </summary>
    /// <remarks>Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to the Kubernetes client class for use in unit testing.</remarks>
    public interface IKubernetesClientShim
    {
        /// <summary>
        /// Creates a Deployment.
        /// </summary>
        /// <param name="kubernetesClient">The client to use.</param>
        /// <param name="body">The definition of the deployment.</param>
        /// <param name="namespaceParameter">The namespace to create the deployment in.</param>
        Task<V1Deployment> CreateNamespacedDeploymentAsync(k8s.Kubernetes kubernetesClient, V1Deployment body, String namespaceParameter);

        /// <summary>
        /// Returns all the deployments in the specified namespace.
        /// </summary>
        /// <param name="kubernetesClient">The client to use.</param>
        /// <param name="namespaceParameter">The namespace to get the deployments from.</param>
        /// <returns>A list of deployments.</returns>
        Task<V1DeploymentList> ListNamespacedDeploymentAsync(k8s.Kubernetes kubernetesClient, String namespaceParameter);

        /// <summary>
        /// Creates a Service.
        /// </summary>
        /// <param name="kubernetesClient">The client to use.</param>
        /// <param name="body">The definition of the service.</param>
        /// <param name="namespaceParameter">The namespace to create the service in.</param>
        Task<V1Service> CreateNamespacedServiceAsync(k8s.Kubernetes kubernetesClient, V1Service body, String namespaceParameter);

        /// <summary>
        /// Scales a specified deployment.
        /// </summary>
        /// <param name="kubernetesClient">The client to use.</param>
        /// <param name="body">A <see cref="V1Patch"/> containing the scaling details.</param>
        /// <param name="name">The name of the deplyoment to scale.</param>
        /// <param name="namespaceParameter">The namespace the deployment exists in.</param>
        Task<V1Scale> PatchNamespacedDeploymentScaleAsync(k8s.Kubernetes kubernetesClient, V1Patch body, String name, String namespaceParameter);

        /// <summary>
        /// Returns all the pods in the specified namespace.
        /// </summary>
        /// <param name="kubernetesClient">The client to use.</param>
        /// <param name="namespaceParameter">The namespace to get the pods from.</param>
        /// <returns>A list of pods.</returns>
        Task<V1PodList> ListNamespacedPodAsync(k8s.Kubernetes kubernetesClient, String namespaceParameter);
    }
}
