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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence.Sql.SqlServer
{
    /// <summary>
    /// Model/container class which holds a connection string for SQL Server.
    /// </summary>
    public class SqlServerLoginCredentials : ConnectionStringPersistentStorageLoginCredentials
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerLoginCredentials class.
        /// </summary>
        /// <param name="connectionString">A connection string for SQL Server.</param>
        public SqlServerLoginCredentials(String connectionString)
            : base(connectionString) 
        {
        }
    }
}
