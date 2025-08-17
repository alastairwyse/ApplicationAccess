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

namespace ApplicationAccess.Hosting.Models
{
    /// <summary>
    /// Model/container class holding parameters used to connect to an ApplicationAccess database or persistent storage instance.
    /// </summary>
    public class DatabaseConnectionParameters
    {
        /// <summary>Parameters used to connect to a SQL database.</summary>
        protected SqlDatabaseConnectionParametersBase sqlDatabaseConnectionParameters;

        /// <summary>
        /// Parameters used to connect to a SQL database.
        /// </summary>
        public SqlDatabaseConnectionParametersBase SqlDatabaseConnectionParameters
        {
            get { return sqlDatabaseConnectionParameters; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.DatabaseConnectionParameters class.
        /// </summary>
        public DatabaseConnectionParameters()
        {
            sqlDatabaseConnectionParameters = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.DatabaseConnectionParameters class.
        /// </summary>
        /// <param name="sqlDatabaseConnectionParameters">Parameters used to connect to a SQL database.</param>
        public DatabaseConnectionParameters(SqlDatabaseConnectionParametersBase sqlDatabaseConnectionParameters)
            : this()
        {
            this.sqlDatabaseConnectionParameters = sqlDatabaseConnectionParameters;
        }
    }
}
