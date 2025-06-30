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
using System.ComponentModel.DataAnnotations;
using ApplicationAccess.Redistribution.Kubernetes.Models;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options
{
    /// <summary>
    /// Model/container class storing options corresponding to class <see cref="KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration"/>, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class StaticConfigurationOptions
    {
        #pragma warning disable 0649

        #pragma warning disable 1591

        public const String StaticConfigurationOptionsName = "StaticConfiguration";

        protected const String ValidationErrorMessagePrefix = $"Error validating {StaticConfigurationOptionsName} options";

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(PodPort)}' is required.")]
        [Range(1, 65535, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public UInt16? PodPort { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(ExternalPort)}' is required.")]
        [Range(1, 65535, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public UInt16? ExternalPort { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(NameSpace)}' is required.")]
        public String NameSpace { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(PersistentStorageInstanceNamePrefix)}' is required.")]
        public String PersistentStorageInstanceNamePrefix { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(LoadBalancerServicesHttps)}' is required.")]
        public Boolean? LoadBalancerServicesHttps { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(DeploymentWaitPollingInterval)}' is required.")]
        [Range(1, 2147483647, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public Int32? DeploymentWaitPollingInterval { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(ServiceAvailabilityWaitAbortTimeout)}' is required.")]
        [Range(1, 2147483647, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public Int32? ServiceAvailabilityWaitAbortTimeout { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(DistributedOperationCoordinatorRefreshIntervalWaitBuffer)}' is required.")]
        [Range(1, 2147483647, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public Int32? DistributedOperationCoordinatorRefreshIntervalWaitBuffer { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(ReaderNodeConfigurationTemplate)}' is required.")]
        public ReaderNodeConfigurationTemplateOptions ReaderNodeConfigurationTemplate { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(EventCacheNodeConfigurationTemplate)}' is required.")]
        public EventCacheNodeConfigurationTemplateOptions EventCacheNodeConfigurationTemplate { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(WriterNodeConfigurationTemplate)}' is required.")]
        public WriterNodeConfigurationTemplateOptions WriterNodeConfigurationTemplate { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(DistributedOperationCoordinatorNodeConfigurationTemplate)}' is required.")]
        public DistributedOperationCoordinatorNodeConfigurationTemplateOptions DistributedOperationCoordinatorNodeConfigurationTemplate { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(DistributedOperationRouterNodeConfigurationTemplate)}' is required.")]
        public DistributedOperationRouterNodeConfigurationTemplateOptions DistributedOperationRouterNodeConfigurationTemplate { get; set; }

        #pragma warning restore 1591

        #pragma warning restore 0649
    }
}
