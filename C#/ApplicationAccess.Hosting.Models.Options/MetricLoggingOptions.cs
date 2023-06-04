/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
    /// Container class storing options for metric logging, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class MetricLoggingOptions
    {
        #pragma warning disable 0649

        public const String MetricLoggingOptionsName = "MetricLogging";

        [Required(ErrorMessage = $"Configuration for '{nameof(MetricLoggingEnabled)}' is required.")]
        public Nullable<Boolean> MetricLoggingEnabled { get; set; }

        public String MetricCategorySuffix { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(MetricBufferProcessing)}' is required.")]
        public MetricBufferProcessingOptions? MetricBufferProcessing { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(MetricsSqlServerConnection)}' is required.")]
        public MetricsSqlServerConnectionOptions? MetricsSqlServerConnection { get; set; }

        public MetricLoggingOptions()
        {
            MetricLoggingEnabled = false;
            MetricCategorySuffix = "";
            MetricBufferProcessing = null;
            MetricsSqlServerConnection = null;
        }

        #pragma warning restore 0649
    }
}
