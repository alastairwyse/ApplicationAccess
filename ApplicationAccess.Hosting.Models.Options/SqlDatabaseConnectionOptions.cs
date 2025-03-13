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
using Microsoft.Extensions.Configuration;

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Container class storing generic options for connecting SQL Server databases, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    /// <remarks>Property <see cref="SqlDatabaseConnectionOptions.ConnectionParameters"/> is stored as type <see cref="IConfigurationSection"/> as the specific child parameters will vary depending on the database type.</remarks>
    public abstract class SqlDatabaseConnectionOptions
    {
        #pragma warning disable 0649
        #pragma warning disable 8618

        [Required(ErrorMessage = $"Configuration for '{nameof(DatabaseType)}' is required.")]
        public DatabaseType? DatabaseType { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(ConnectionParameters)}' is required.")]
        public IConfigurationSection ConnectionParameters { get; set; }

        public SqlDatabaseConnectionOptions()
        {
        }

        #pragma warning restore 8618
        #pragma warning restore 0649
    }
}
