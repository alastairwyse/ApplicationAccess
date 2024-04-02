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
    /// Container class which holds parameters used to connect to a Microsoft SQL Server database.
    /// </summary>
    public class SqlServerConnectionParameters : SqlDatabaseConnectionParametersBase
    {
        /// <summary>The name or network address of the instance of SQL Server to connect to.  Null if property <see cref="SqlDatabaseConnectionParametersBase.connectionString"/> is set.</summary>
        protected String dataSource;
        /// <summary>The name of the database associated with the connection.  Null if property <see cref="SqlDatabaseConnectionParametersBase.connectionString"/> is set.</summary>
        protected String initialCatalog;
        /// <summary>The number of times an operation against the SQL Server database should be retried in the case of execution failure.</summary>
        protected Int32 retryCount;
        /// <summary>The time in seconds between operation retries.</summary>
        protected Int32 retryInterval;
        /// <summary>The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</summary>
        protected Int32 operationTimeout;

        /// <summary>
        /// The user ID to be used when connecting to SQL Server.  Null if property <see cref="SqlDatabaseConnectionParametersBase.ConnectionString"/> is set.
        /// </summary>
        public String UserId
        {
            get { return userId; }
        }

        /// <summary>
        /// The name or network address of the instance of SQL Server to connect to.  Null if property <see cref="SqlDatabaseConnectionParametersBase.ConnectionString"/> is set.
        /// </summary>
        public String DataSource
        {
            get { return dataSource; }
        }

        /// <summary>
        /// The name of the database associated with the connection.  Null if property <see cref="SqlDatabaseConnectionParametersBase.ConnectionString"/> is set.
        /// </summary>
        public String InitialCatalog
        {
            get { return initialCatalog; }
        }

        /// <summary>
        /// The number of times an operation against the SQL Server database should be retried in the case of execution failure.
        /// </summary>
        public Int32 RetryCount
        {
            get { return retryCount; }
        }

        /// <summary>
        /// The time in seconds between operation retries.
        /// </summary>
        public Int32 RetryInterval
        {
            get { return retryInterval; }
        }

        /// <summary>
        /// The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.
        /// </summary>
        public Int32 OperationTimeout
        {
            get { return operationTimeout; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.SqlServerConnectionParameters class.
        /// </summary>
        /// <param name="connectionString">String containing concatenated parameters used to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        public SqlServerConnectionParameters(String connectionString, Int32 retryCount, Int32 retryInterval, Int32 operationTimeout)
            : base(connectionString)
        {
            this.dataSource = null;
            this.initialCatalog = null;
            this.retryCount = retryCount;
            this.retryInterval = retryInterval;
            this.operationTimeout = operationTimeout;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.SqlServerConnectionParameters class.
        /// </summary>
        /// <param name="userId"> The user ID to be used when connecting to SQL Server.</param>
        /// <param name="password">The password for the SQL Server account.</param>
        /// <param name="dataSource">The name or network address of the instance of SQL Server to connect to. </param>
        /// <param name="initialCatalogue">The name of the database associated with the connection.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        public SqlServerConnectionParameters(String userId, String password, String dataSource, String initialCatalogue, Int32 retryCount, Int32 retryInterval, Int32 operationTimeout)
            : base (userId, password)
        {
            this.dataSource = dataSource;
            this.initialCatalog = initialCatalogue;
            this.retryCount = retryCount;
            this.retryInterval = retryInterval;
            this.operationTimeout = operationTimeout;
        }
    }
}
