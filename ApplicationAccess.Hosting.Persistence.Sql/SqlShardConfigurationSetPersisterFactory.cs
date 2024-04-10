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
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Persistence.SqlServer;
using ApplicationAccess.Distribution.Serialization;
using ApplicationLogging;
using Microsoft.Data.SqlClient;

namespace ApplicationAccess.Hosting.Persistence.Sql
{
    /// <summary>
    /// Factory for instances of <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> based on SQL database connection parameters.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The implementation of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> embedded within items in the shard configuration set.</typeparam>
    /// <typeparam name="TClientConfigurationJsonSerializer">An implementation of <see cref="IDistributedAccessManagerAsyncClientConfigurationJsonSerializer{T}"/> which serializes <typeparamref name="TClientConfiguration"/> instances.</typeparam>
    public class SqlShardConfigurationSetPersisterFactory<TClientConfiguration, TClientConfigurationJsonSerializer>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
        where TClientConfigurationJsonSerializer : IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<TClientConfiguration>
    {
        /// <summary>JSON serializer for <see cref="AccessManagerRestClientConfiguration"/> objects.</summary>
        protected TClientConfigurationJsonSerializer clientConfigurationJsonSerializer;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Persistence.Sql.SqlShardConfigurationSetPersisterFactory class.
        /// </summary>
        /// <param name="clientConfigurationJsonSerializer">JSON serializer for <see cref="AccessManagerRestClientConfiguration"/> objects.</param>
        /// <param name="logger">The logger for general logging.</param>
        public SqlShardConfigurationSetPersisterFactory(TClientConfigurationJsonSerializer clientConfigurationJsonSerializer, IApplicationLogger logger)
        {
            this.clientConfigurationJsonSerializer = clientConfigurationJsonSerializer;
            this.logger = logger;
        }

        /// <summary>
        /// Returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance which connects to a SQL database.
        /// </summary>
        /// <typeparam name="TConnectionParameters">The type of database connection parameters to use to create the persister.</typeparam>
        /// <param name="databaseConnectionParameters">The database connection parameters to use to create the persister.</param>
        /// <returns>The <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</returns>
        public IShardConfigurationSetPersister<TClientConfiguration, TClientConfigurationJsonSerializer> GetPersister<TConnectionParameters>(TConnectionParameters databaseConnectionParameters)
            where TConnectionParameters : SqlDatabaseConnectionParametersBase
        {
            if (databaseConnectionParameters is SqlServerConnectionParameters)
            {
                var typedDatabaseConnectionParameters = databaseConnectionParameters as SqlServerConnectionParameters;
                String connectionString = null;
                if (databaseConnectionParameters.ConnectionString == null)
                {
                    var connectionStringBuilder = new SqlConnectionStringBuilder();
                    connectionStringBuilder.Encrypt = false;
                    connectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
                    connectionStringBuilder.DataSource = typedDatabaseConnectionParameters.DataSource;
                    connectionStringBuilder.InitialCatalog = typedDatabaseConnectionParameters.InitialCatalog;
                    connectionStringBuilder.UserID = typedDatabaseConnectionParameters.UserId;
                    connectionStringBuilder.Password = typedDatabaseConnectionParameters.Password;
                    try
                    {
                        connectionString = connectionStringBuilder.ConnectionString;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to create SQL Server connection string.", e);
                    }
                }
                else
                {
                    connectionString = databaseConnectionParameters.ConnectionString;
                }

                return new SqlServerShardConfigurationSetPersister<TClientConfiguration, TClientConfigurationJsonSerializer>
                (
                    connectionString,
                    typedDatabaseConnectionParameters.RetryCount,
                    typedDatabaseConnectionParameters.RetryInterval,
                    typedDatabaseConnectionParameters.OperationTimeout,
                    clientConfigurationJsonSerializer,
                    logger
                );
            }
            else
            {
                throw new ArgumentException($"Parameter '{nameof(databaseConnectionParameters)}' is of unhandled type '{databaseConnectionParameters.GetType().FullName}'.", nameof(databaseConnectionParameters));
            }
        }
    }
}
