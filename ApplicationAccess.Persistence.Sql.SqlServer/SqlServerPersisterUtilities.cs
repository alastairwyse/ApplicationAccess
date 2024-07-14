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
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Persistence.Sql;

namespace ApplicationAccess.Persistence.Sql.SqlServer
{
    /// <summary>
    /// Utility methods for classes which write and read data associated with AccessManager classes to and from a SQL Server database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class SqlServerPersisterUtilities<TUser, TGroup, TComponent, TAccess> : SqlPersisterUtilitiesBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The string to use to connect to the SQL Server database.</summary>
        protected String connectionString;
        /// <summary>The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</summary>
        protected Int32 operationTimeout;
        /// <summary>The retry logic to use when connecting to and executing against the SQL Server database.</summary>
        protected SqlRetryLogicOption sqlRetryLogicOption;
        /// <summary>The action to invoke if an action is retried due to a transient error.</summary>
        protected EventHandler<SqlRetryingEventArgs> connectionRetryAction;
        /// <summary>Maps <see cref="SessionDeadlockPriority"/> values to their equivalent SQL Server string value.</summary>
        protected Dictionary<SessionDeadlockPriority, String> deadlockPriorityToStringValueMap;

        /// <inheritdoc/>
        protected override string DatabaseName 
        { 
            get 
            { 
                return "SQL Server"; 
            } 
        }

        /// <inheritdoc/>
        protected override String TimestampColumnFormatString
        {
            get
            {
                return "yyyy-MM-ddTHH:mm:ss.fffffff";
            }
        }

        /// <inheritdoc/>
        protected override ReadQueryGeneratorBase ReadQueryGenerator
        {
            get;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerPersisterUtilities class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="sqlRetryLogicOption">The retry logic to use when connecting to and executing against the SQL Server database.</param>
        /// <param name="connectionRetryAction">The action to invoke if an action is retried due to a transient error.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        public SqlServerPersisterUtilities
        (
            String connectionString,
            Int32 operationTimeout,
            SqlRetryLogicOption sqlRetryLogicOption,
            EventHandler<SqlRetryingEventArgs> connectionRetryAction,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier
        ) : base(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
        {
            ThrowExceptionIfConnectionStringParameterNullOrWhitespace(nameof(connectionString), connectionString);
            ThrowExceptionIfOperationTimeoutParameterLessThanZero(nameof(operationTimeout), operationTimeout);

            this.connectionString = connectionString;
            this.operationTimeout = operationTimeout;
            this.sqlRetryLogicOption = sqlRetryLogicOption;
            this.connectionRetryAction = connectionRetryAction;
            deadlockPriorityToStringValueMap = new Dictionary<SessionDeadlockPriority, String>()
            {  
                { SessionDeadlockPriority.Low, "LOW"},
                { SessionDeadlockPriority.Normal, "NORMAL"},
                { SessionDeadlockPriority.High, "HIGH"},
            };
            this.ReadQueryGenerator = new SqlServerReadQueryGenerator();
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override IEnumerable<T> ExecuteQueryAndConvertColumn<T>(String query, String columnToConvert, Func<String, T> conversionFromStringFunction)
        {
            return ExecuteQueryAndConvertColumnWithDeadlockRetry(query, columnToConvert, conversionFromStringFunction);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteQueryAndConvertColumn<TReturn1, TReturn2>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction
        )
        {
            return ExecuteQueryAndConvertColumnWithDeadlockRetry(query, columnToConvert1, columnToConvert2, returnType1ConversionFromStringFunction, returnType2ConversionFromStringFunction);
        }

        /// <inheritdoc/>
        protected override IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteQueryAndConvertColumn<TReturn1, TReturn2, TReturn3>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            String columnToConvert3,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction,
            Func<String, TReturn3> returnType3ConversionFromStringFunction
        )
        {
            return ExecuteQueryAndConvertColumnWithDeadlockRetry
            (
                query, 
                columnToConvert1, 
                columnToConvert2,
                columnToConvert3,
                returnType1ConversionFromStringFunction,
                returnType2ConversionFromStringFunction, 
                returnType3ConversionFromStringFunction
            );
        }

        /// <summary>
        /// Attempts to execute the specified query, converting a specified column from each row of the results to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert">The name of the column in the results to convert to the specified type.</param>
        /// <param name="conversionFromStringFunction">A function which converts a single string-valued cell in the results to the specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected IEnumerable<T> ExecuteQueryAndConvertColumnImplementation<T>(String query, String columnToConvert, Func<String, T> conversionFromStringFunction)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query))
            {
                PrepareConnectionAndCommand(connection, command);
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String currentDataItemAsString = (String)dataReader[columnToConvert];
                        yield return conversionFromStringFunction.Invoke(currentDataItemAsString);
                    }
                }
                TeardownConnectionAndCommand(connection, command);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query, converting the specified columns from each row of the results to the specified types.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteQueryAndConvertColumnImplementation<TReturn1, TReturn2>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction
        )
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query))
            {
                PrepareConnectionAndCommand(connection, command);
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String firstDataItemAsString = (String)dataReader[columnToConvert1];
                        String secondDataItemAsString = (String)dataReader[columnToConvert2];
                        TReturn1 firstDataItemConverted = returnType1ConversionFromStringFunction.Invoke(firstDataItemAsString);
                        TReturn2 secondDataItemConverted = returnType2ConversionFromStringFunction.Invoke(secondDataItemAsString);
                        yield return new Tuple<TReturn1, TReturn2>(firstDataItemConverted, secondDataItemConverted);
                    }
                }
                TeardownConnectionAndCommand(connection, command);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query, converting the specified columns from each row of the results to the specified types.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn3">The type of the third data item to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert3">The name of the third column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <param name="returnType3ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the third specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteQueryAndConvertColumnImplementation<TReturn1, TReturn2, TReturn3>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            String columnToConvert3,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction,
            Func<String, TReturn3> returnType3ConversionFromStringFunction
        )
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query))
            {
                PrepareConnectionAndCommand(connection, command);
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String firstDataItemAsString = (String)dataReader[columnToConvert1];
                        String secondDataItemAsString = (String)dataReader[columnToConvert2];
                        String thirdDataItemAsString = (String)dataReader[columnToConvert3];
                        TReturn1 firstDataItemConverted = returnType1ConversionFromStringFunction.Invoke(firstDataItemAsString);
                        TReturn2 secondDataItemConverted = returnType2ConversionFromStringFunction.Invoke(secondDataItemAsString);
                        TReturn3 thirdDataItemConverted = returnType3ConversionFromStringFunction.Invoke(thirdDataItemAsString);
                        yield return new Tuple<TReturn1, TReturn2, TReturn3>(firstDataItemConverted, secondDataItemConverted, thirdDataItemConverted);
                    }
                }
                TeardownConnectionAndCommand(connection, command);
            }
        }


        /// <summary>
        /// Attempts to execute the specified query, converting a specified column from each row of the results to the specified type, and catching any deadlock (<see href="https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error?view=sql-server-ver16">1205</see>) exception and retrying.
        /// </summary>
        /// <typeparam name="T">The type to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert">The name of the column in the results to convert to the specified type.</param>
        /// <param name="conversionFromStringFunction">A function which converts a single string-valued cell in the results to the specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected List<T> ExecuteQueryAndConvertColumnWithDeadlockRetry<T>(String query, String columnToConvert, Func<String, T> conversionFromStringFunction)
        {
            Func<SqlCommand, List<T>> readAndConvertResultsFunction = (SqlCommand command) =>
            {
                var results = new List<T>();
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String currentDataItemAsString = (String)dataReader[columnToConvert];
                        results.Add(conversionFromStringFunction.Invoke(currentDataItemAsString));
                    }
                }

                return results;
            };
            return ExecuteQueryAndConvertColumnWithDeadlockRetry(query, readAndConvertResultsFunction);
        }

        /// <summary>
        /// Attempts to execute the specified query, converting the specified columns from each row of the results to the specified types, and catching any deadlock (<see href="https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error?view=sql-server-ver16">1205</see>) exception and retrying.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteQueryAndConvertColumnWithDeadlockRetry<TReturn1, TReturn2>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction
        )
        {
            Func<SqlCommand, List<Tuple<TReturn1, TReturn2>>> readAndConvertResultsFunction = (SqlCommand command) =>
            {
                var results = new List<Tuple<TReturn1, TReturn2>>();
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String firstDataItemAsString = (String)dataReader[columnToConvert1];
                        String secondDataItemAsString = (String)dataReader[columnToConvert2];
                        TReturn1 firstDataItemConverted = returnType1ConversionFromStringFunction.Invoke(firstDataItemAsString);
                        TReturn2 secondDataItemConverted = returnType2ConversionFromStringFunction.Invoke(secondDataItemAsString);
                        results.Add(new Tuple<TReturn1, TReturn2>(firstDataItemConverted, secondDataItemConverted));
                    }
                }

                return results;
            };
            return ExecuteQueryAndConvertColumnWithDeadlockRetry(query, readAndConvertResultsFunction);
        }


        /// <summary>
        /// Attempts to execute the specified query, converting the specified columns from each row of the results to the specified types, and catching any deadlock (<see href="https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error?view=sql-server-ver16">1205</see>) exception and retrying.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn3">The type of the third data item to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert3">The name of the third column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <param name="returnType3ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the third specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteQueryAndConvertColumnWithDeadlockRetry<TReturn1, TReturn2, TReturn3>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            String columnToConvert3,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction,
            Func<String, TReturn3> returnType3ConversionFromStringFunction
        )
        {
            Func<SqlCommand, List<Tuple<TReturn1, TReturn2, TReturn3>>> readAndConvertResultsFunction = (SqlCommand command) =>
            {
                var results = new List<Tuple<TReturn1, TReturn2, TReturn3>>();
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String firstDataItemAsString = (String)dataReader[columnToConvert1];
                        String secondDataItemAsString = (String)dataReader[columnToConvert2];
                        String thirdDataItemAsString = (String)dataReader[columnToConvert3];
                        TReturn1 firstDataItemConverted = returnType1ConversionFromStringFunction.Invoke(firstDataItemAsString);
                        TReturn2 secondDataItemConverted = returnType2ConversionFromStringFunction.Invoke(secondDataItemAsString);
                        TReturn3 thirdDataItemConverted = returnType3ConversionFromStringFunction.Invoke(thirdDataItemAsString);
                        results.Add(new Tuple<TReturn1, TReturn2, TReturn3>(firstDataItemConverted, secondDataItemConverted, thirdDataItemConverted));
                    }
                }

                return results;
            };
            return ExecuteQueryAndConvertColumnWithDeadlockRetry(query, readAndConvertResultsFunction);
        }

        /// <summary>
        /// Prepare the specified <see cref="SqlConnection"/> and <see cref="SqlCommand"/> to execute a query against them.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="command">The command which runs the query.</param>
        protected void PrepareConnectionAndCommand(SqlConnection connection, SqlCommand command)
        {
            connection.RetryLogicProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(sqlRetryLogicOption);
            connection.RetryLogicProvider.Retrying += connectionRetryAction;
            connection.Open();
            command.Connection = connection;
            command.CommandTimeout = operationTimeout;
        }

        /// <summary>
        /// Prepare the specified <see cref="SqlConnection"/> and <see cref="SqlCommand"/> to execute a query against them, and sets the session deadlock priority.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="command">The command which runs the query.</param>
        /// <param name="deadlockPriority">The <see cref="SessionDeadlockPriority"/> to assign to the session.</param>
        protected virtual void PrepareConnectionAndCommand(SqlConnection connection, SqlCommand command, SessionDeadlockPriority deadlockPriority)
        {
            PrepareConnectionAndCommand(connection, command);
            String setDeadlockPriorityStatement = $"SET DEADLOCK_PRIORITY {deadlockPriorityToStringValueMap[deadlockPriority]};";
            using (var setDeadlockPriorityCommand = new SqlCommand(setDeadlockPriorityStatement))
            {
                setDeadlockPriorityCommand.Connection = connection;
                setDeadlockPriorityCommand.CommandTimeout = operationTimeout;
                setDeadlockPriorityCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Performs teardown/deconstruct operations on the the specified <see cref="SqlConnection"/> and <see cref="SqlCommand"/> after utilizing them.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="command">The command.</param>
        protected virtual void TeardownConnectionAndCommand(SqlConnection connection, SqlCommand command)
        {
            connection.RetryLogicProvider.Retrying -= connectionRetryAction;
        }

        /// <summary>
        /// Executes a function which queries from a SQL Server database, catching any deadlock (<see href="https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error?view=sql-server-ver16">1205</see>) exceptions and retrying according to the retry count specified in the 'sqlRetryLogicOption' member.
        /// </summary>
        /// <typeparam name="T">The type of row data returned from the query function.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="readAndConvertResultsFunction">The function which reads the results of the query and returns a list of results.  Accepts a single parameter which is the <see cref="SqlCommand"/> to use to execute the query and read the results, and returns a list of <typeparamref name="T"/>.</param>   
        /// <returns>An list of <typeparamref name="T"/> containing the results of the query.</returns>
        protected List<T> ExecuteQueryAndConvertColumnWithDeadlockRetry<T>(String query, Func<SqlCommand, List<T>> readAndConvertResultsFunction)
        {
            const Int32 deadlockErrorNumber = 1205;

            Int32 retryCount = sqlRetryLogicOption.NumberOfTries - 1;
            var queryResults = new List<T>();
            var exceptions = new List<Exception>();
            while (true)
            {
                queryResults.Clear();
                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    using (var command = new SqlCommand(query))
                    {
                        PrepareConnectionAndCommand(connection, command, SessionDeadlockPriority.Low);
                        try
                        {
                            queryResults = readAndConvertResultsFunction.Invoke(command);
                            break;
                        }
                        finally
                        {
                            TeardownConnectionAndCommand(connection, command);
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

            return queryResults;
        }

        #endregion

        #region Static Parameter Exception Handlers

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> is the specified 'connectionString' parameter is null or whitespace.
        /// </summary>
        /// <param name="connectionStringParameterName">The name of the parameter.</param>
        /// <param name="connectionString">The value of the parameter.</param>
        public static void ThrowExceptionIfConnectionStringParameterNullOrWhitespace(String connectionStringParameterName, String connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{connectionStringParameterName}' must contain a value.", nameof(connectionString));
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> is the specified 'operationTimeout' parameter is less than 0.
        /// </summary>
        /// <param name="operationTimeoutParameterName">The name of the parameter.</param>
        /// <param name="operationTimeout">The value of the parameter.</param>
        public static void ThrowExceptionIfOperationTimeoutParameterLessThanZero(String operationTimeoutParameterName, Int32 operationTimeout)
        {
            if (operationTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(operationTimeout), $"Parameter '{operationTimeoutParameterName}' with value {operationTimeout} cannot be less than 0.");
        }

        #endregion
    }
}
