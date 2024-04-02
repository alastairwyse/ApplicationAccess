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
    /// Base for container classes which holds parameters used to connect to a SQL database.
    /// </summary>
    public abstract class SqlDatabaseConnectionParametersBase
    {
        /// <summary>String containing concatenated parameters used to connect to a SQL database.  Will be null if other specific parameter properties are set.</summary>
        protected String connectionString;
        /// <summary>The user id used to connect to the database.  Null if property <see cref="SqlDatabaseConnectionParametersBase.connectionString"/> is set.</summary>
        protected String userId;
        /// <summary>The password used to connect to the database.  Null if property <see cref="SqlDatabaseConnectionParametersBase.connectionString"/> is set.</summary>
        protected String password;

        /// <summary>
        /// String containing concatenated parameters used to connect to a SQL database.  Will be null if other specific parameter properties are set.
        /// </summary>
        public String ConnectionString
        {
            get { return connectionString; }
        }

        /// <summary>
        /// The password used to connect to the database.  Null if property <see cref="SqlDatabaseConnectionParametersBase.ConnectionString"/> is set.
        /// </summary>
        public String Password
        {
            get { return password; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.SqlDatabaseConnectionParametersBase class.
        /// </summary>
        /// <param name="connectionString">String containing concatenated parameters used to connect to a SQL database.</param>
        public SqlDatabaseConnectionParametersBase(String connectionString)
        {
            this.connectionString = connectionString;
            this.password = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.SqlDatabaseConnectionParametersBase class.
        /// </summary>
        /// <param name="userId">The user id used to connect to the database.</param>
        /// <param name="password">The password used to connect to the database.</param>
        public SqlDatabaseConnectionParametersBase(String userId, String password)
        {
            this.connectionString = null;
            this.userId = userId;
            this.password = password;
        }
    }
}
