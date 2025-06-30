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
    /// Model/container class storing options corresponding to class <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/>, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class ShardGroupConfigurationOptions
    {
        #pragma warning disable 0649

        #pragma warning disable 1591

        public const String ShardGroupConfigurationOptionsName = "ShardGroupConfiguration";

        protected const String ValidationErrorMessagePrefix = $"Error validating {ShardGroupConfigurationOptionsName} options";

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(HashRangeStart)}' is required.")]
        [Range(-2147483648, 2147483647, ErrorMessage = ValidationErrorMessagePrefix + ".  Value for '{0}' must be between {1} and {2}.")]
        public Int32? HashRangeStart { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(ReaderNodeClientUrl)}' is required.")]
        public String ReaderNodeClientUrl { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(WriterNodeClientUrl)}' is required.")]
        public String WriterNodeClientUrl { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(SqlServerConnectionString)}' is required.")]
        public String SqlServerConnectionString { get; set; }

        #pragma warning restore 1591

        #pragma warning restore 0649
    }
}
