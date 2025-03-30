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
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace ApplicationAccess.Redistribution.Persistence.SqlServer
{
    /// <summary>
    /// Executes scripts against a Microsoft SQL Server database.
    /// </summary>
    public class SqlServerScriptExecutor : ISqlServerScriptExecutor
    {
        /// <summary>The connection string to use to connect to SQL Server.</summary>
        protected String connectionString;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Persistence.SqlServer.SqlServerScriptExecutor class.
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to SQL Server.</param>
        public SqlServerScriptExecutor(String connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{nameof(connectionString)}' must contain a value.", nameof(connectionString));

            this.connectionString = connectionString;
        }

        /// <inheritdoc/>
        public void ExecuteScripts(IEnumerable<Tuple<String, String>> scriptsAndContents)
        {
            foreach (Tuple<String, String> currentScriptAndContents in scriptsAndContents)
            {
                if (String.IsNullOrWhiteSpace(currentScriptAndContents.Item1) == true)
                    throw new ArgumentException($"Parameter '{nameof(scriptsAndContents)}' contained an empty script.", nameof(scriptsAndContents));
                if (String.IsNullOrWhiteSpace(currentScriptAndContents.Item2) == true)
                    throw new ArgumentException($"Parameter '{nameof(scriptsAndContents)}' contained an empty script description.", nameof(scriptsAndContents));
            }

            using (var connection = new SqlConnection(connectionString))
            {
                var server = new Server(new ServerConnection(connection));
                foreach (Tuple<String, String> currentScriptAndContents in scriptsAndContents)
                {
                    try
                    {
                        server.ConnectionContext.ExecuteNonQuery(currentScriptAndContents.Item1);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to execute script to {currentScriptAndContents.Item2} in SQL Server", e);
                    }
                }
            }
        }
    }
}
