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

namespace ApplicationAccess.Redistribution.Persistence.SqlServer
{
    /// <summary>
    /// Defines methods which execute scripts against a Microsoft SQL Server database.
    /// </summary>
    public interface ISqlServerScriptExecutor
    {
        /// <summary>
        /// Executes a collection of scripts in order against SQL Server.
        /// </summary>
        /// <param name="scriptsAndContents">A collection of tuples each containing two values: the script to execute, and a description of the purpose of that script script to use in exception messages in the case execution fails (for example 'create the ApplicationAccess database').</param>
        void ExecuteScripts(IEnumerable<Tuple<String, String>> scriptsAndContents);
    }
}
