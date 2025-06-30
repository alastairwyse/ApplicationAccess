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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ApplicationAccess.Redistribution.Kubernetes.Models;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options
{
    /// <summary>
    /// Model/container class storing options corresponding to class <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/>, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class InstanceConfigurationOptions
    {
        #pragma warning disable 0649

        #pragma warning disable 1591

        public const String InstanceConfigurationOptionsName = "InstanceConfiguration";

        protected const String ValidationErrorMessagePrefix = $"Error validating {InstanceConfigurationOptionsName} options";

        public String DistributedOperationRouterUrl { get; set; }

        public String Writer1Url { get; set; }

        public String Writer2Url { get; set; }

        public String ShardConfigurationSqlServerConnectionString { get; set; }

        public List<ShardGroupConfigurationOptions> UserShardGroupConfiguration { get; set; }

        public List<ShardGroupConfigurationOptions> GroupToGroupMappingShardGroupConfiguration { get; set; }

        public List<ShardGroupConfigurationOptions> GroupShardGroupConfiguration { get; set; }

        public String DistributedOperationCoordinatorUrl { get; set; }

        public InstanceConfigurationOptions()
        {
            DistributedOperationRouterUrl = null;
            Writer1Url = null;
            Writer2Url = null;
            ShardConfigurationSqlServerConnectionString = null;
            UserShardGroupConfiguration = null;
            GroupToGroupMappingShardGroupConfiguration = null;
            GroupShardGroupConfiguration = null;
            DistributedOperationCoordinatorUrl = null;
        }

        #pragma warning restore 1591

        #pragma warning restore 0649
    }
}
