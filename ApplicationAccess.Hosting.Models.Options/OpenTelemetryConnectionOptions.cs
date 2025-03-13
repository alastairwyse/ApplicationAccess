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

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Container class storing options for connecting via the OpenTelemetry protocol, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class OpenTelemetryConnectionOptions
    {
        #pragma warning disable 0649

        public const String OpenTelemetryConnectionOptionsName = "OpenTelemetryConnection";

        protected const String ValidationErrorMessagePrefix = $"Error validating {OpenTelemetryConnectionOptionsName} options";

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(Protocol)}' is required.")]
        public OpenTelemetryConnectionProtocol? Protocol { get; set; }

        [Required(ErrorMessage = $"{ValidationErrorMessagePrefix}.  Configuration for '{nameof(Endpoint)}' is required.")]
        public String? Endpoint { get; set; }

        public Int32 Timeout { get; set; }

        public String Headers { get; set; }

        public Int32 ExporterTimeout { get; set; }

        public Int32 MaxExportBatchSize { get; set; }

        public Int32 MaxQueueSize { get; set; }

        public Int32 ScheduledDelay { get; set; }

        public OpenTelemetryConnectionOptions()
        {
            Timeout = 10_000;
            Headers = "";
            ExporterTimeout = 30_000;
            MaxExportBatchSize = 512;
            MaxQueueSize = 2_048;
            ScheduledDelay = 5_000;
        }

        #pragma warning restore 0649
    }
}
