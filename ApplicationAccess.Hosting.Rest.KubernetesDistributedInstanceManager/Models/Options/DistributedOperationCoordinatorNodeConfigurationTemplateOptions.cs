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
using Microsoft.Extensions.Logging;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options
{
    /// <summary>
    /// Model/container class storing options corresponding to class <see cref="DistributedOperationCoordinatorNodeConfiguration"/>, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class DistributedOperationCoordinatorNodeConfigurationTemplateOptions
    {
        #pragma warning disable 0649

        public const String DistributedOperationCoordinatorNodeConfigurationTemplateOptionsName = "DistributedOperationCoordinatorNodeConfigurationTemplate";

        protected const String ValidationErrorMessagePrefix = $"Error validating {DistributedOperationCoordinatorNodeConfigurationTemplateOptionsName} options";

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(TerminationGracePeriod)}' is required.")]
        [Range(0, 65535, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public UInt16? TerminationGracePeriod { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(ContainerImage)}' is required.")]
        public String ContainerImage { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(MinimumLogLevel)}' is required.")]
        public LogLevel MinimumLogLevel { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(CpuResourceRequest)}' is required.")]
        public String CpuResourceRequest { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(MemoryResourceRequest)}' is required.")]
        public String MemoryResourceRequest { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(StartupProbePeriod)}' is required.")]
        [Range(1, 65535, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public UInt16? StartupProbePeriod { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(StartupProbeFailureThreshold)}' is required.")]
        [Range(0, 65535, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public UInt16? StartupProbeFailureThreshold { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(ReplicaCount)}' is required.")]
        [Range(1, 65535, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public UInt16? ReplicaCount { get; set; }

        #pragma warning restore 0649
    }
}
