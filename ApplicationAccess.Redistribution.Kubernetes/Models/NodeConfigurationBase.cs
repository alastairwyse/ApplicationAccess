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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Redistribution.Kubernetes.Models
{
    /// <summary>
    /// Base for model/container class holding common configuration for a node pod/deployment within a distributed AccessManager implementation hosted in Kubernetes.
    /// </summary>
    public abstract record NodeConfigurationBase
    {
        /// <summary>The time in seconds to wait on pod shutdown before forcefully terminating the pod.</summary>
        public required UInt16 TerminationGracePeriod { get; init; }

        /// <summary>The container image to use for the pod.</summary>
        public required String ContainerImage { get; init; }

        /// <summary>The minimum detail level of logs to output from the reader node.</summary>
        public required LogLevel MinimumLogLevel { get; init; }

        /// <summary>Base/template for the 'appsettings.json' file contents for the node.</summary>
        public required JObject AppSettingsConfigurationTemplate { get; init; }

        /// <summary>The CPU resource request value for the pod/deployment.</summary>
        public required String CpuResourceRequest { get; init; }

        /// <summary>The memory resource request value for the pod/deployment.</summary>
        public required String MemoryResourceRequest { get; init; }

        /// <summary>The time in seconds between startup probes.</summary>
        public required UInt16 StartupProbePeriod { get; init; }

        /// <summary>The number of startup probes which are allowed to fail before considering that the startup has failed.</summary>
        public required UInt16 StartupProbeFailureThreshold { get; init; }
    }
}
