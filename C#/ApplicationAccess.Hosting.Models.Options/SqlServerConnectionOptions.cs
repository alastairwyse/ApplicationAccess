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
    /// Container class storing options for connecting to a Microsoft SQL Server database, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class SqlServerConnectionOptions
    {
        #pragma warning disable 0649

        [Required(ErrorMessage = $"Configuration for '{nameof(DataSource)}' is required.")]
        public String DataSource { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(InitialCatalog)}' is required.")]
        public String InitialCatalog { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(UserId)}' is required.")]
        public String UserId { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(Password)}' is required.")]
        public String Password { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(RetryCount)}' is required.")]
        [Range(0, 59, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public Int32 RetryCount { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(RetryInterval)}' is required.")]
        [Range(0, 120, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public Int32 RetryInterval { get; set; }

        public SqlServerConnectionOptions()
        {
            DataSource = "";
            InitialCatalog = "";
            UserId = "";
            Password = "";
            RetryCount = -1;
            RetryInterval = -1;
        }

        #pragma warning restore 0649
    }
}