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
using ApplicationAccess.Hosting.Models.Options;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options
{
    /// <summary>
    /// Model/container class storing options for configuring a distributed AccessManager instance, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class DistributedAccessManagerInstanceOptions
    {
        #pragma warning disable 0649

        #pragma warning disable 1591

        public const String DistributedAccessManagerInstanceOptionsName = "DistributedAccessManagerInstance";

        protected const String ValidationErrorMessagePrefix = $"Error validating {DistributedAccessManagerInstanceOptionsName} options";

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(SqlServerDatabaseConnection)}' is required.")]
        public AccessManagerSqlDatabaseConnectionOptions SqlServerDatabaseConnection { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(ShardConnection)}' is required.")]
        public ShardConnectionOptions ShardConnection { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(StaticConfiguration)}' is required.")]
        public StaticConfigurationOptions StaticConfiguration { get; set; }

        public InstanceConfigurationOptions InstanceConfiguration { get; set; }

        public DistributedAccessManagerInstanceOptions()
        {
            InstanceConfiguration = null;
        }

        #pragma warning restore 1591

        #pragma warning restore 0649
    }
}
