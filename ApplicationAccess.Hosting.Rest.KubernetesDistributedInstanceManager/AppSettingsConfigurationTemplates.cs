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

using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    // Classes in this file are used to read AccessManager node 'appsettings.json' contents as JSON documents rather than IOptions instances.
    //   IOptions instances cannot automatically bind to JSON document properties, hence these classes are created by manual parsing of 'appsettings.json'
    //   (via the IConfiguration interface), and set as services in dependency injection so that they can be later assembled as part of 
    //   ApplicationAccess.Redistribution.Kubernetes.Models.NodeConfigurationBase classes, and passed as configuration to a
    //   KubernetesDistributedAccessManagerInstanceManager class.

    /// <summary>
    /// Template for 'appsettings.json' configuration files used by reader nodes in a distributed AccessManager instance.
    /// </summary>
    public class ReaderNodeAppSettingsConfigurationTemplate : JObject
    {
    }

    /// <summary>
    /// Template for 'appsettings.json' configuration files used by event cache nodes in a distributed AccessManager instance.
    /// </summary>
    public class EventCacheNodeAppSettingsConfigurationTemplate : JObject
    {
    }

    /// <summary>
    /// Template for 'appsettings.json' configuration files used by writer nodes in a distributed AccessManager instance.
    /// </summary>
    public class WriterNodeAppSettingsConfigurationTemplate : JObject
    {
    }

    /// <summary>
    /// Template for 'appsettings.json' configuration files used by distributed operation coordinator nodes in a distributed AccessManager instance.
    /// </summary>
    public class DistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate : JObject
    {
    }

    /// <summary>
    /// Template for 'appsettings.json' configuration files used by distributed operation router nodes in a distributed AccessManager instance.
    /// </summary>
    public class DistributedOperationRouterNodeAppSettingsConfigurationTemplate : JObject
    {
    }
}
