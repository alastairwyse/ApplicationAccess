﻿/*
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
    /// Container class storing options for connecting to the Microsoft SQL Server database which is used to persist access manager events, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class AccessManagerSqlServerConnectionOptions : SqlServerConnectionOptions
    {
        public const String AccessManagerSqlServerConnectionOptionsName = "AccessManagerSqlServerConnection";

        // TODO: When application metrics also supports a timeout, push this to the base SqlServerConnectionOptions class
        [Range(0, 2147483647, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public Int32 OperationTimeout { get; set; }
    }
}
