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
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;
using ApplicationMetrics.MetricLoggers.PostgreSql;
using ApplicationLogging;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace ApplicationAccess.Hosting.Metrics
{
    /// <summary>
    /// Factory for instances of <see cref="MetricLoggerBuffer"/> based on SQL database connection parameters.
    /// </summary>
    public class SqlMetricLoggerFactory
    {
        /// <summary>The category to log all metrics under.</summary>
        protected String category;
        /// <summary>Object which implements a processing strategy for the buffers (queues).</summary>
        protected IBufferProcessingStrategy bufferProcessingStrategy;
        /// <summary>The base time unit to use to log interval metrics.</summary>
        protected IntervalMetricBaseTimeUnit intervalMetricBaseTimeUnit;
        /// <summary>Specifies whether an exception should be thrown if the correct order of interval metric logging is not followed (e.g. End() method called before Begin()).  Note that this parameter only has an effect when running in 'non-interleaved' mode.</summary>
        protected Boolean intervalMetricChecking;
        /// <summary>The logger to use for performance statistics.</summary>
        protected IApplicationLogger logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Metrics.SqlMetricLoggerFactory class.
        /// </summary>
        /// <param name="category">The category to log all metrics under.</param>
        /// <param name="bufferProcessingStrategy">Object which implements a processing strategy for the buffers (queues).</param>
        /// <param name="intervalMetricBaseTimeUnit">The base time unit to use to log interval metrics.</param>
        /// <param name="intervalMetricChecking">Specifies whether an exception should be thrown if the correct order of interval metric logging is not followed (e.g. End() method called before Begin()).  Note that this parameter only has an effect when running in 'non-interleaved' mode.</param>
        /// <param name="logger">The logger to use for performance statistics.</param>
        public SqlMetricLoggerFactory
        (
            String category,
            IBufferProcessingStrategy bufferProcessingStrategy,
            IntervalMetricBaseTimeUnit intervalMetricBaseTimeUnit,
            Boolean intervalMetricChecking,
            IApplicationLogger logger
        )
        {
            this.category = category;
            this.bufferProcessingStrategy = bufferProcessingStrategy;
            this.intervalMetricBaseTimeUnit = intervalMetricBaseTimeUnit;
            this.intervalMetricChecking = intervalMetricChecking;
            this.logger = logger;
        }

        /// <summary>
        /// Returns a <see cref="MetricLoggerBuffer"/> instance which logs metrics to a SQL database.
        /// </summary>
        /// <typeparam name="T">The type of database connection parameters to use to create the metric logger.</typeparam>
        /// <param name="databaseConnectionParameters">The database connection parameters to use to create the metric logger.</param>
        /// <returns>The <see cref="MetricLoggerBuffer"/> instance.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public MetricLoggerBuffer GetMetricLogger<T>(T databaseConnectionParameters)
            where T : SqlDatabaseConnectionParametersBase
        {
            if (databaseConnectionParameters is SqlServerConnectionParameters)
            {
                var typedDatabaseConnectionParameters = databaseConnectionParameters as SqlServerConnectionParameters;
                String connectionString = null;
                if (databaseConnectionParameters.ConnectionString == null)
                {
                    // TODO: This block is the same as the one in ApplicationAccess.Hosting.Persistence.Sql.SqlAccessManagerTemporalBulkPersisterFactory
                    //   Could put into common base/utility class etc...
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

                return new SqlServerMetricLogger
                (
                    category, 
                    connectionString, 
                    typedDatabaseConnectionParameters.RetryCount, 
                    typedDatabaseConnectionParameters.RetryInterval, 
                    typedDatabaseConnectionParameters.OperationTimeout, 
                    bufferProcessingStrategy, 
                    intervalMetricBaseTimeUnit, 
                    intervalMetricChecking, 
                    logger
                );
            }
            else if (databaseConnectionParameters is PostgreSqlConnectionParameters)
            {
                var typedDatabaseConnectionParameters = databaseConnectionParameters as PostgreSqlConnectionParameters;
                String connectionString = null;
                if (databaseConnectionParameters.ConnectionString == null)
                {
                    var connectionStringBuilder = new NpgsqlConnectionStringBuilder();
                    connectionStringBuilder.Host = typedDatabaseConnectionParameters.Host;
                    connectionStringBuilder.Database = typedDatabaseConnectionParameters.Database;
                    connectionStringBuilder.Username = typedDatabaseConnectionParameters.UserName;
                    connectionStringBuilder.Password = typedDatabaseConnectionParameters.Password;
                    try
                    {
                        connectionString = connectionStringBuilder.ConnectionString;
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Failed to create PostgreSQL connection string.", e);
                    }
                }
                else
                {
                    connectionString = databaseConnectionParameters.ConnectionString;
                }

                return new PostgreSqlMetricLogger
                (
                    category, 
                    connectionString, 
                    typedDatabaseConnectionParameters.CommandTimeout, 
                    bufferProcessingStrategy, 
                    intervalMetricBaseTimeUnit, 
                    intervalMetricChecking, 
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
