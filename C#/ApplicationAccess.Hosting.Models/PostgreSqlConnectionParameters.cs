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

namespace ApplicationAccess.Hosting.Models
{    
    /// <summary>
    /// Container class which holds parameters used to connect to a PostgreSQL database.
    /// </summary>
    public class PostgreSqlConnectionParameters : SqlDatabaseConnectionParametersBase
    {
        /// <summary>The hostname or IP address of the PostgreSQL server to connect to.  Null if property <see cref="SqlDatabaseConnectionParametersBase.connectionString"/> is set.</summary>
        protected String host;
        /// <summary>The PostgreSQL database to connect to.  Null if property <see cref="SqlDatabaseConnectionParametersBase.connectionString"/> is set.</summary>
        protected String database;
        /// <summary>The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error.</summary>
        protected Int32 commandTimeout;

        /// <summary>
        /// The username to connect with.   Null if property <see cref="SqlDatabaseConnectionParametersBase.ConnectionString"/> is set.
        /// </summary>
        public String UserName
        {
            get { return userId; }
        }

        /// <summary>
        /// The hostname or IP address of the PostgreSQL server to connect to.   Null if property <see cref="SqlDatabaseConnectionParametersBase.ConnectionString"/> is set.
        /// </summary>
        public String Host
        {
            get { return host; }
        }

        /// <summary>
        /// The PostgreSQL database to connect to.  Null if property <see cref="SqlDatabaseConnectionParametersBase.ConnectionString"/> is set.
        /// </summary>
        public String Database
        {
            get { return database; }
        }

        /// <summary>
        /// The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error.
        /// </summary>
        public Int32 CommandTimeout
        {
            get { return commandTimeout; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.PostgreSqlConnectionParameters class.
        /// </summary>
        /// <param name="connectionString">String containing concatenated parameters used to connect to the PostgreSQL database.</param>
        /// <param name="commandTimeout">The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error.</param>
        public PostgreSqlConnectionParameters(String connectionString, Int32 commandTimeout)
            : base(connectionString)
        {
            this.commandTimeout = commandTimeout;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.PostgreSqlConnectionParameters class.
        /// </summary>
        /// <param name="userName">The username to connect with.</param>
        /// <param name="password">The password to connect with.</param>
        /// <param name="host">The hostname or IP address of the PostgreSQL server to connect to.</param>
        /// <param name="database">The PostgreSQL database to connect to.</param>
        /// <param name="commandTimeout">The time to wait (in seconds) while trying to execute a command before terminating the attempt and generating an error.</param>
        public PostgreSqlConnectionParameters(String userName, String password, String host, String database, Int32 commandTimeout)
            : base(userName, password)
        {
            this.host = host;
            this.database = database;
            this.commandTimeout = commandTimeout;
        }
    }
}
