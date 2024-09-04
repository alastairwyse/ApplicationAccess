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
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.File;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Persistence.Sql.PostgreSql;
using ApplicationLogging;
using ApplicationMetrics;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace ApplicationAccess.Hosting.Persistence.Sql
{
    /// <summary>
    /// Factory for instances of <see cref="IAccessManagerTemporalBulkPersister{TUser, TGroup, TComponent, TAccess}"/> based on SQL database connection parameters.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class SqlAccessManagerTemporalBulkPersisterFactory<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;        
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Persistence.Sql.SqlAccessManagerTemporalBulkPersisterFactory class.
        /// </summary>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        public SqlAccessManagerTemporalBulkPersisterFactory
        (
            IUniqueStringifier<TUser> userStringifier, 
            IUniqueStringifier<TGroup> groupStringifier, 
            IUniqueStringifier<TComponent> applicationComponentStringifier, 
            IUniqueStringifier<TAccess> accessLevelStringifier, 
            IApplicationLogger logger
        )
        {
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            this.logger = logger;
            this.metricLogger = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Persistence.Sql.SqlAccessManagerTemporalBulkPersisterFactory class.
        /// </summary>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public SqlAccessManagerTemporalBulkPersisterFactory
        (
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Returns an <see cref="IAccessManagerTemporalBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance which connects to a SQL database.
        /// </summary>
        /// <typeparam name="T">The type of database connection parameters to use to create the persister.</typeparam>
        /// <param name="databaseConnectionParameters">The database connection parameters to use to create the persister.</param>
        /// <param name="persisterBackupFilePath">The full path to a file used to back up events in the case persistence to the SQL database fails, or null if no backup file should be used.</param>
        /// <returns>The <see cref="IAccessManagerTemporalBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance.</returns>
        public IAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> GetPersister<T>(T databaseConnectionParameters, String persisterBackupFilePath) 
            where T: SqlDatabaseConnectionParametersBase
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

                SqlServerAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> sqlPersister;
                if (metricLogger == null)
                {
                    sqlPersister = new SqlServerAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                    (
                        connectionString,
                        typedDatabaseConnectionParameters.RetryCount,
                        typedDatabaseConnectionParameters.RetryInterval,
                        typedDatabaseConnectionParameters.OperationTimeout,
                        userStringifier, 
                        groupStringifier, 
                        applicationComponentStringifier, 
                        accessLevelStringifier, 
                        logger
                    );
                }
                else
                {
                    sqlPersister = new SqlServerAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                    (
                        connectionString,
                        typedDatabaseConnectionParameters.RetryCount,
                        typedDatabaseConnectionParameters.RetryInterval,
                        typedDatabaseConnectionParameters.OperationTimeout,
                        userStringifier,
                        groupStringifier,
                        applicationComponentStringifier,
                        accessLevelStringifier,
                        logger, 
                        metricLogger
                    );
                }

                if (persisterBackupFilePath == null)
                {
                    return sqlPersister;
                }
                else
                {
                    if (metricLogger == null)
                    {
                        var backupPersister = new FileAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess>
                        (
                            persisterBackupFilePath,
                            userStringifier,
                            groupStringifier,
                            applicationComponentStringifier,
                            accessLevelStringifier, 
                            logger
                        );
                        var redundantPersister = new AccessManagerRedundantTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                        (
                            sqlPersister,
                            sqlPersister, 
                            backupPersister, 
                            logger
                        );

                        return redundantPersister;
                    }
                    else
                    {
                        var backupPersister = new FileAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess>
                        (
                            persisterBackupFilePath,
                            userStringifier,
                            groupStringifier,
                            applicationComponentStringifier,
                            accessLevelStringifier,
                            logger, 
                            metricLogger
                        );
                        var redundantPersister = new AccessManagerRedundantTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                        (
                            sqlPersister,
                            sqlPersister,
                            backupPersister,
                            logger,
                            metricLogger
                        );

                        return redundantPersister;
                    }
                }
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

                PostgreSqlAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> sqlPersister;
                if (metricLogger == null)
                {
                    sqlPersister = new PostgreSqlAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                    (
                        connectionString,
                        typedDatabaseConnectionParameters.CommandTimeout,
                        userStringifier,
                        groupStringifier,
                        applicationComponentStringifier,
                        accessLevelStringifier,
                        logger
                    );
                }
                else
                {
                    sqlPersister = new PostgreSqlAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                    (
                        connectionString,
                        typedDatabaseConnectionParameters.CommandTimeout,
                        userStringifier,
                        groupStringifier,
                        applicationComponentStringifier,
                        accessLevelStringifier,
                        logger, 
                        metricLogger
                    );
                }

                if (persisterBackupFilePath == null)
                {
                    return sqlPersister;
                }
                else
                {
                    if (metricLogger == null)
                    {
                        var backupPersister = new FileAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess>
                        (
                            persisterBackupFilePath,
                            userStringifier,
                            groupStringifier,
                            applicationComponentStringifier,
                            accessLevelStringifier,
                            logger
                        );
                        var redundantPersister = new AccessManagerRedundantTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                        (
                            sqlPersister,
                            sqlPersister,
                            backupPersister,
                            logger
                        );

                        return redundantPersister;
                    }
                    else
                    {
                        var backupPersister = new FileAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess>
                        (
                            persisterBackupFilePath,
                            userStringifier,
                            groupStringifier,
                            applicationComponentStringifier,
                            accessLevelStringifier,
                            logger,
                            metricLogger
                        );
                        var redundantPersister = new AccessManagerRedundantTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
                        (
                            sqlPersister,
                            sqlPersister,
                            backupPersister,
                            logger,
                            metricLogger
                        );

                        return redundantPersister;
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Parameter '{nameof(databaseConnectionParameters)}' is of unhandled type '{databaseConnectionParameters.GetType().FullName}'.", nameof(databaseConnectionParameters));
            }
        }
    }
}
