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
using System.Text.RegularExpressions;

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
            this.ReadQueryGenerator = new SqlServerReadQueryGenerator();
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override IEnumerable<T> ExecuteQueryAndConvertColumn<T>(String query, String columnToConvert, Func<String, T> conversionFromStringFunction)
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
        /// Performs teardown/deconstruct operations on the the specified <see cref="SqlConnection"/> and <see cref="SqlCommand"/> after utilizing them.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="command">The command.</param>
        protected void TeardownConnectionAndCommand(SqlConnection connection, SqlCommand command)
        {
            connection.RetryLogicProvider.Retrying -= connectionRetryAction;
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
