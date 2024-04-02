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
using Microsoft.Extensions.Configuration;
using ApplicationAccess.Hosting.Models;

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Parses the <see cref="SqlDatabaseConnectionOptions.ConnectionParameters"/> property of a <see cref="SqlDatabaseConnectionOptions"/> instance, converting it to an instance of a subclass of <see cref="SqlDatabaseConnectionParametersBase/>.
    /// </summary>
    public class SqlDatabaseConnectionParametersParser
    {
        #pragma warning disable 1591

        protected const String connectionStringConfigurationKey = "ConnectionString";
        protected const String userIdConfigurationKey = "UserId";
        protected const String usernameConfigurationKey = "Username";
        protected const String passwordConfigurationKey = "Password";
        protected const String dataSourceConfigurationKey = "DataSource";
        protected const String hostConfigurationKey = "Host";
        protected const String initialCatalogConfigurationKey = "InitialCatalog";
        protected const String databaseConfigurationKey = "Database";
        protected const String retryCountConfigurationKey = "RetryCount";
        protected const String retryIntervalConfigurationKey = "RetryInterval";
        protected const String operationTimeoutConfigurationKey = "OperationTimeout";
        protected const String commandTimeoutConfigurationKey = "CommandTimeout";

        #pragma warning restore 1591

        #pragma warning disable 8600

        /// <summary>
        /// Parses the <see cref="SqlDatabaseConnectionOptions.ConnectionParameters"/> property of a <see cref="SqlDatabaseConnectionOptions"/> instance, converting it to an instance of a subclass of <see cref="SqlDatabaseConnectionParametersBase"/>.
        /// </summary>
        /// <param name="databaseType">The type of SQL database.</param>
        /// <param name="connectionParameters">The conection parameters to pass.</param>
        /// <param name="parentConfigurationName">The name of parent of the configuration in the <paramref name="connectionParameters"/> parameter (for use in exception messages).</param>
        /// <returns>The database connection parameters.</returns>
        public SqlDatabaseConnectionParametersBase Parse(DatabaseType databaseType, IConfiguration connectionParameters, String parentConfigurationName)
        {
            String connectionString = connectionParameters[connectionStringConfigurationKey];
            if (databaseType == DatabaseType.SqlServer)
            {
                Int32 retryCount = GetConfigurationValueAsInt32(connectionParameters, retryCountConfigurationKey, parentConfigurationName);
                Int32 retryInterval = GetConfigurationValueAsInt32(connectionParameters, retryIntervalConfigurationKey, parentConfigurationName);
                Int32 operationTimeout = GetConfigurationValueAsInt32(connectionParameters, operationTimeoutConfigurationKey, parentConfigurationName);
                if (connectionString != null)
                {
                    return new SqlServerConnectionParameters(connectionString, retryCount, retryInterval, operationTimeout);
                }
                else
                {
                    String userId = GetConfigurationValue(connectionParameters, userIdConfigurationKey, parentConfigurationName);
                    String password = GetConfigurationValue(connectionParameters, passwordConfigurationKey, parentConfigurationName);
                    String dataSource = GetConfigurationValue(connectionParameters, dataSourceConfigurationKey, parentConfigurationName);
                    String initialCatalogue = GetConfigurationValue(connectionParameters, initialCatalogConfigurationKey, parentConfigurationName);

                    return new SqlServerConnectionParameters(userId, password, dataSource, initialCatalogue, retryCount, retryInterval, operationTimeout);
                }
            }
            else if (databaseType == DatabaseType.PostgreSQL)
            {
                Int32 commandTimeout = GetConfigurationValueAsInt32(connectionParameters, commandTimeoutConfigurationKey, parentConfigurationName);
                if (connectionString != null)
                {
                    return new PostgreSqlConnectionParameters(connectionString, commandTimeout);
                }
                else
                {
                    String userName = GetConfigurationValue(connectionParameters, usernameConfigurationKey, parentConfigurationName);
                    String password = GetConfigurationValue(connectionParameters, passwordConfigurationKey, parentConfigurationName);
                    String host = GetConfigurationValue(connectionParameters, hostConfigurationKey, parentConfigurationName);
                    String database = GetConfigurationValue(connectionParameters, databaseConfigurationKey, parentConfigurationName);

                    return new PostgreSqlConnectionParameters(userName, password, host, database, commandTimeout);
                }
            }
            else
            {
                throw new ArgumentException($"Parameter '{nameof(databaseType)}' contained unhandled value '{databaseType}'.", nameof(databaseType));
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Attempts to read the configuration item with the specified key as a string from an <see cref="IConfiguration"/> instance.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to read from.</param>
        /// <param name="key">The key of the configuration item to read.</param>
        /// <param name="parentConfigurationName">The name of parent of the configuration in the <paramref name="configuration"/> parameter (for use in exception messages).</param>
        /// <returns>The configuration value.</returns>
        /// <exception cref="Exception">If configuration with the specified key doesn't exist.</exception>
        protected String GetConfigurationValue(IConfiguration configuration, String key, String parentConfigurationName)
        {
            String value = configuration[key];
            if (value == null)
            {
                throw new Exception($"Failed to read configuration item with key '{key}' from the '{parentConfigurationName}' configuration.");
            }

            return value;
        }

        /// <summary>
        /// Attempts to read the configuration item with the specified key as an Int32 from an <see cref="IConfiguration"/> instance.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to read from.</param>
        /// <param name="key">The key of the configuration item to read.</param>
        /// <param name="parentConfigurationName">The name of parent of the configuration in the <paramref name="configuration"/> parameter (for use in exception messages).</param>
        /// <returns>The configuration value.</returns>
        /// <exception cref="Exception">If configuration with the specified key doesn't exist.</exception>
        /// <exception cref="Exception">The configuration item could not be converted to an <see cref="Int32"/>.</exception>
        protected Int32 GetConfigurationValueAsInt32(IConfiguration configuration, String key, String parentConfigurationName)
        {
            String valueAsString = GetConfigurationValue(configuration, key, parentConfigurationName);
            Boolean parseResult = Int32.TryParse(valueAsString, out Int32 value);
            if (parseResult == false)
            {
                throw new Exception($"Failed to read configuration item with key '{key}' and value '{valueAsString}' from the '{parentConfigurationName}' configuration as an integer.");
            }

            return value;
        }

        #endregion

        #pragma warning restore 8600
    }
}
