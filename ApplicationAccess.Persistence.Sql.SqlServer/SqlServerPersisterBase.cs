/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using System.Data;

namespace ApplicationAccess.Persistence.Sql.SqlServer
{
    /// <summary>
    /// Base class providing common funcionality for classes which read and write to and from Microsoft SQL Server databases.
    /// </summary>
    public abstract class SqlServerPersisterBase
    {
        /// <summary>The string to use to connect to the SQL Server database.</summary>
        protected String connectionString;
        /// <summary>The number of times an operation against the SQL Server database should be retried in the case of execution failure.</summary>
        protected Int32 retryCount;
        /// <summary>The time in seconds between operation retries.</summary>
        protected Int32 retryInterval;
        /// <summary>The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</summary>
        protected Int32 operationTimeout;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>The retry logic to use when connecting to and executing against the SQL Server database.</summary>
        protected SqlRetryLogicOption sqlRetryLogicOption;
        /// <summary>A set of SQL Server database engine error numbers which denote a transient fault.</summary>
        /// <see href="https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors?view=sql-server-ver16"/>
        /// <see href="https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues?view=azuresql"/>
        protected List<Int32> sqlServerTransientErrorNumbers;
        /// <summary>The action to invoke if an action is retried due to a transient error.</summary>
        protected EventHandler<SqlRetryingEventArgs> connectionRetryAction;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerPersisterBase class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="logger">The logger for general logging.</param>
        public SqlServerPersisterBase
        (
            String connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            Int32 operationTimeout,
            IApplicationLogger logger
        )
        {
            SqlServerPersisterUtilities<String, String, String, String>.ThrowExceptionIfConnectionStringParameterNullOrWhitespace(nameof(connectionString), connectionString);
            SqlServerPersisterUtilities<String, String, String, String>.ThrowExceptionIfOperationTimeoutParameterLessThanZero(nameof(operationTimeout), operationTimeout);
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be less than 0.");
            if (retryCount > 59)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be greater than 59.");
            if (retryInterval < 0)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be less than 0.");
            if (retryInterval > 120)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be greater than 120.");
            if (operationTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(operationTimeout), $"Parameter '{nameof(operationTimeout)}' with value {operationTimeout} cannot be less than 0.");

            this.connectionString = connectionString;
            this.retryCount = retryCount;
            this.retryInterval = retryInterval;
            this.operationTimeout = operationTimeout;
            this.logger = logger;
            this.metricLogger = new NullMetricLogger();
            // Setup retry logic
            sqlServerTransientErrorNumbers = GenerateSqlServerTransientErrorNumbers();
            sqlRetryLogicOption = new SqlRetryLogicOption();
            sqlRetryLogicOption.NumberOfTries = retryCount + 1;  // According to documentation... "1 means to execute one time and if an error is encountered, don't retry"
            sqlRetryLogicOption.MinTimeInterval = TimeSpan.FromSeconds(0);
            sqlRetryLogicOption.MaxTimeInterval = TimeSpan.FromSeconds(120);
            sqlRetryLogicOption.DeltaTime = TimeSpan.FromSeconds(retryInterval);
            sqlRetryLogicOption.TransientErrors = sqlServerTransientErrorNumbers;
            connectionRetryAction = (Object sender, SqlRetryingEventArgs eventArgs) =>
            {
                Exception lastException = eventArgs.Exceptions[eventArgs.Exceptions.Count - 1];
                Int32 retryDelayInSeconds = eventArgs.Delay.Seconds;
                if (typeof(SqlException).IsAssignableFrom(lastException.GetType()) == true)
                {
                    var se = (SqlException)lastException;
                    logger.Log(this, LogLevel.Warning, $"SQL Server error with number {se.Number} occurred when executing command.  Retrying in {retryDelayInSeconds} seconds (retry {eventArgs.RetryCount} of {retryCount}).", se);
                }
                else
                {
                    logger.Log(this, LogLevel.Warning, $"Exception occurred when executing command.  Retrying in {retryDelayInSeconds} seconds (retry {eventArgs.RetryCount} of {retryCount}).", lastException);
                }
                metricLogger.Increment(new SqlCommandExecutionRetried());
            };
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns a list of SQL Server error numbers which indicate errors which are transient (i.e. could be recovered from after retry).
        /// </summary>
        /// <returns>The list of SQL Server error numbers.</returns>
        /// <remarks>See <see href="https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues?view=azuresql">Troubleshooting connectivity issues and other errors with Azure SQL Database and Azure SQL Managed Instance</see></remarks> 
        protected List<Int32> GenerateSqlServerTransientErrorNumbers()
        {
            // Below obtained from https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues?view=azuresql
            var returnList = new List<Int32>() { 26, 40, 615, 926, 4060, 4221, 10053, 10928, 10929, 11001, 40197, 40501, 40613, 40615, 40544, 40549, 49918, 49919, 49920 };
            // These are additional error numbers encountered during testing
            returnList.AddRange(new List<Int32>() { -2, 53, 121 });

            return returnList;
        }

        /// <summary>
        /// Creates a <see cref="SqlParameter" />.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <returns>The created parameter.</returns>
        protected SqlParameter CreateSqlParameterWithValue(String parameterName, SqlDbType parameterType, Object parameterValue)
        {
            var returnParameter = new SqlParameter(parameterName, parameterType);
            returnParameter.Value = parameterValue;

            return returnParameter;
        }

        /// <summary>
        /// Attempts to execute a stored procedure which does not return a result set.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters to pass to the stored procedure.</param>
        protected void ExecuteStoredProcedure(String procedureName, IEnumerable<SqlParameter> parameters)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(procedureName))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (SqlParameter currentParameter in parameters)
                    {
                        command.Parameters.Add(currentParameter);
                    }
                    connection.RetryLogicProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(sqlRetryLogicOption);
                    connection.RetryLogicProvider.Retrying += connectionRetryAction;
                    connection.Open();
                    command.Connection = connection;
                    command.CommandTimeout = operationTimeout;
                    command.ExecuteNonQuery();
                    connection.RetryLogicProvider.Retrying -= connectionRetryAction;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute stored procedure '{procedureName}' in SQL Server.", e);
            }
        }

        /// <summary>
        /// Attempts to execute a stored procedure which does not return a result set, catching any deadlock (<see href="https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error?view=sql-server-ver16">1205</see>) exceptions and retrying according to the specified retry logic.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters to pass to the stored procedure.</param>
        protected void ExecuteStoredProcedureWithDeadlockRetry(String procedureName, IEnumerable<SqlParameter> parameters)
        {
            const Int32 deadlockErrorNumber = 1205;

            Int32 retryCount = sqlRetryLogicOption.NumberOfTries - 1;
            var exceptions = new List<Exception>();
            while (true)
            {
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    using (var command = new SqlCommand(procedureName))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        foreach (SqlParameter currentParameter in parameters)
                        {
                            command.Parameters.Add(currentParameter);
                        }
                        connection.RetryLogicProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(sqlRetryLogicOption);
                        connection.RetryLogicProvider.Retrying += connectionRetryAction;
                        connection.Open();
                        command.Connection = connection;
                        command.CommandTimeout = operationTimeout;
                        String setDeadlockPriorityStatement = $"SET DEADLOCK_PRIORITY LOW;";
                        using (var setDeadlockPriorityCommand = new SqlCommand(setDeadlockPriorityStatement))
                        {
                            setDeadlockPriorityCommand.Connection = connection;
                            setDeadlockPriorityCommand.CommandTimeout = operationTimeout;
                            setDeadlockPriorityCommand.ExecuteNonQuery();
                        }
                        try
                        {
                            command.ExecuteNonQuery();
                            break;
                        }
                        finally
                        {
                            connection.RetryLogicProvider.Retrying -= connectionRetryAction;
                            command.Parameters.Clear();
                        }
                    }
                }
                catch (SqlException sqlException)
                {
                    if (sqlException.Errors.Count > 0 && sqlException.Errors[0].Number == deadlockErrorNumber)
                    {
                        exceptions.Add(sqlException);
                        if (retryCount > 0)
                        {
                            var retryEventArgs = new SqlRetryingEventArgs(sqlRetryLogicOption.NumberOfTries - retryCount, new TimeSpan(0), exceptions);
                            connectionRetryAction.Invoke(this, retryEventArgs);
                            retryCount--;
                        }
                        else
                        {
                            String exceptionMessage = $"The number of deadlock retries has exceeded the maximum of {sqlRetryLogicOption.NumberOfTries} attempt(s).";
                            throw new AggregateException(exceptionMessage, exceptions);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        #endregion
    }
}
