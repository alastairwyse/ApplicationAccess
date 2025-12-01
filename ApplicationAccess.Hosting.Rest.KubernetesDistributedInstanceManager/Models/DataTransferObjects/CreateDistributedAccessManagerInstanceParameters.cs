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
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Controllers;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.DataTransferObjects
{
    /// <summary>
    /// DTO container class holding parameters for the <see cref="KubernetesDistributedInstanceManagerController.CreateDistributedAccessManagerInstanceAsync(CreateDistributedAccessManagerInstanceParameters)">CreateDistributedAccessManagerInstanceAsync()</see> method.
    /// </summary>
    public class CreateDistributedAccessManagerInstanceParameters
    {
        #pragma warning disable 1591

        public List<ShardGroupConfigurationWithSqlServerConnectionString> UserShardGroupConfiguration { get; set; }
        public List<ShardGroupConfigurationWithSqlServerConnectionString> GroupToGroupMappingShardGroupConfiguration { get; set; }
        public List<ShardGroupConfigurationWithSqlServerConnectionString> GroupShardGroupConfiguration { get; set; }

        #pragma warning restore 1591
    }
}
