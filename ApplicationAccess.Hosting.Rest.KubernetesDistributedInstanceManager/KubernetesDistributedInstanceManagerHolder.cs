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

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// Holds an instance of <see cref="IKubernetesDistributedInstanceManager"/>.
    /// </summary>
    /// <remarks>An instance of this class can be set on the ASP.NET services collection before the application is built, and then the 'KubernetesDistributedInstanceManager' property set after building (specifically during the IHostedService.StartAsync() method... when the value set on 'KubernetesDistributedInstanceManager' is actually instantiated).  Works around one of the 'chicken and egg'-type problems which often arise when trying to instantiate and populate the ASP.NET services collection.</remarks>
    public class KubernetesDistributedInstanceManagerHolder
    {
        #pragma warning disable 0649

        public IKubernetesDistributedInstanceManager KubernetesDistributedInstanceManager { get; set; }

        #pragma warning restore 0649
    }
}
