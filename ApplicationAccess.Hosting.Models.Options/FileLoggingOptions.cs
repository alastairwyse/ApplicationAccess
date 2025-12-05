/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Serilog.Events;

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Container class storing options for logging to files, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class FileLoggingOptions
    {
        #pragma warning disable 0649

        public const String FileLoggingOptionsName = "FileLogging";

        public String? LogFilePath { get; set; }

        public String? LogFileNamePrefix { get; set; }

        public LogEventLevel? LogLevel { get; set; }

        public FileLoggingOptions()
        {
            LogFilePath = null;
            LogFileNamePrefix = null;
            LogLevel = null;
        }

        #pragma warning restore 0649
    }
}
