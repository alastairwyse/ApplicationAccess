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
using System.Text.RegularExpressions;
using Npgsql;

namespace ApplicationAccess.Persistence.Sql.PostgreSql
{
    /// <summary>
    /// Utility methods for classes which write and read data associated with AccessManager classes to and from a PostgreSQL database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class PostgreSqlPersisterUtilities<TUser, TGroup, TComponent, TAccess> : SqlPersisterUtilitiesBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The datasource to use to create connections to PostgreSQL.</summary>
        protected NpgsqlDataSource dataSource;
        /// <summary>The time in seconds to wait while trying to execute a command, before terminating the attempt and generating an error. Set to zero for infinity.</summary>
        protected Int32 commandTimeout;

        /// <inheritdoc/>
        protected override string DatabaseName
        {
            get
            {
                return "PostgreSQL";
            }
        }

        /// <inheritdoc/>
        protected override String TimestampColumnFormatString
        {
            get
            {
                return "yyyy-MM-dd HH:mm:ss.ffffff";
            }
        }

        /// <inheritdoc/>
        protected override ReadQueryGeneratorBase ReadQueryGenerator
        {
            get;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.PostgreSql.PostgreSqlPersisterUtilities class.
        /// </summary>
        /// <param name="dataSource">The datasource to use to create connections to PostgreSQL.</param>
        /// <param name="commandTimeout">The time in seconds to wait while trying to execute a command, before terminating the attempt and generating an error. Set to zero for infinity.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        public PostgreSqlPersisterUtilities
        (
            NpgsqlDataSource dataSource,
            Int32 commandTimeout,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier
        ) : base(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
        {
            ThrowExceptionIfCommandTimeoutParameterLessThanZero(nameof(commandTimeout), commandTimeout);

            this.dataSource = dataSource;
            this.commandTimeout = commandTimeout;
            this.ReadQueryGenerator = new PostgreSqlReadQueryGenerator();
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override IEnumerable<T> ExecuteQueryAndConvertColumn<T>(String query, String columnToConvert, Func<String, T> conversionFromStringFunction)
        {
            using (NpgsqlConnection connection = dataSource.OpenConnection())
            using (var command = new NpgsqlCommand(query))
            {
                PrepareConnectionAndCommand(connection, command);
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read() == true)
                    {
                        String currentDataItemAsString = reader.GetString(reader.GetOrdinal(columnToConvert));
                        yield return conversionFromStringFunction(currentDataItemAsString);
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
            using (NpgsqlConnection connection = dataSource.OpenConnection())
            using (var command = new NpgsqlCommand(query))
            {
                PrepareConnectionAndCommand(connection, command);
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read() == true)
                    {
                        String firstDataItemAsString = reader.GetString(reader.GetOrdinal(columnToConvert1));
                        String secondDataItemAsString = reader.GetString(reader.GetOrdinal(columnToConvert2));
                        TReturn1 firstDataItemConverted = returnType1ConversionFromStringFunction(firstDataItemAsString);
                        TReturn2 secondDataItemConverted = returnType2ConversionFromStringFunction(secondDataItemAsString);

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
            using (NpgsqlConnection connection = dataSource.OpenConnection())
            using (var command = new NpgsqlCommand(query))
            {
                PrepareConnectionAndCommand(connection, command);
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read() == true)
                    {
                        String firstDataItemAsString = reader.GetString(reader.GetOrdinal(columnToConvert1));
                        String secondDataItemAsString = reader.GetString(reader.GetOrdinal(columnToConvert2));
                        String thirdDataItemAsString = reader.GetString(reader.GetOrdinal(columnToConvert3));
                        TReturn1 firstDataItemConverted = returnType1ConversionFromStringFunction(firstDataItemAsString);
                        TReturn2 secondDataItemConverted = returnType2ConversionFromStringFunction(secondDataItemAsString);
                        TReturn3 thirdDataItemConverted = returnType3ConversionFromStringFunction(thirdDataItemAsString);

                        yield return new Tuple<TReturn1, TReturn2, TReturn3>(firstDataItemConverted, secondDataItemConverted, thirdDataItemConverted);
                    }
                }
                TeardownConnectionAndCommand(connection, command);
            }
        }

        /// <summary>
        /// Prepare the specified <see cref="NpgsqlConnection"/> and <see cref="NpgsqlCommand"/> to execute a query against them.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="command">The command which runs the query.</param>
        protected void PrepareConnectionAndCommand(NpgsqlConnection connection, NpgsqlCommand command)
        {
            command.Connection = connection;
            command.CommandTimeout = commandTimeout;
        }

        /// <summary>
        /// Performs teardown/deconstruct operations on the the specified <see cref="NpgsqlConnection"/> and <see cref="NpgsqlCommand"/> after utilizing them.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="command">The command.</param>
        protected void TeardownConnectionAndCommand(NpgsqlConnection connection, NpgsqlCommand command)
        {
            connection.Close();
        }

        #endregion

        #region Static Parameter Exception Handlers

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified 'commandTimeout' parameter is less than 0.
        /// </summary>
        /// <param name="commandTimeoutParameterName">The name of the parameter.</param>
        /// <param name="commandTimeout">The value of the parameter.</param>
        public static void ThrowExceptionIfCommandTimeoutParameterLessThanZero(String commandTimeoutParameterName, Int32 commandTimeout)
        {
            if (commandTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(commandTimeout), $"Parameter '{commandTimeoutParameterName}' with value {commandTimeout} cannot be less than 0.");
        }

        #endregion
    }
}
