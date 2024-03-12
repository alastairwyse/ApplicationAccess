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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ApplicationAccess.Persistence.Sql;
using ApplicationLogging;
using ApplicationMetrics;
using Npgsql;
using NpgsqlTypes;

namespace ApplicationAccess.Persistence.Sql.PostgreSql
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> and see <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/> which persists access manager events in bulk to and allows reading of <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> objects from a PostgreSQL database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class PostgreSqlAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        // Connection command timeout is 30 sec by default... should offer as config param or extend
        // COnnetionLifetime should be set to max
        // Can set CA/root cert location on connection
        // Should set encoding for string data
        // IncludeErrorDetail
        // SslCertificate
        // Timeout
        // TrustServerCrt
        // Possible info on transient retry
        //   https://learn.microsoft.com/en-us/azure/postgresql/single-server/concepts-connectivity
        //   https://www.npgsql.org/doc/connection-string-parameters.html

        /// <summary>The maximum size of text columns in the database (restricted by limits on the sizes of index keys... see https://docs.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver16).</summary>
        protected const Int32 columnSizeLimit = 450;
        /// <summary>DateTime format string which can be interpreted by the <see href="https://www.postgresql.org/docs/8.1/functions-formatting.html">PostgreSQL to_timestamp() function</see>.</summary>
        protected const String postgreSQLTimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        /// <summary>The string to use to connect to the PostgreSQL database.</summary>
        protected String connectionString;
        /// <summary>The time in seconds to wait while trying to execute a command, before terminating the attempt and generating an error. Set to zero for infinity.</summary>
        protected Int32 commandTimeout;
        /// <summary>Used to generate queries to read data in the Load() method.</summary>
        protected PostgreSqlReadQueryGenerator queryGenerator;
        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.PostgreSql.PostgreSqlAccessManagerTemporalBulkPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the PostgreSQL database.</param>
        /// <param name="commandTimeout">The time in seconds to wait while trying to execute a command, before terminating the attempt and generating an error. Set to zero for infinity.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        public PostgreSqlAccessManagerTemporalBulkPersister
        (
            String connectionString, 
            Int32 commandTimeout,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger
        )
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{nameof(connectionString)}' must contain a value.", nameof(connectionString));
            if (commandTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(commandTimeout), $"Parameter '{nameof(commandTimeout)}' with value {commandTimeout} cannot be less than 0.");

            this.connectionString = connectionString;
            this.commandTimeout = commandTimeout;
            queryGenerator = new PostgreSqlReadQueryGenerator();
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            this.logger = logger;
        }


        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.PostgreSql.PostgreSqlAccessManagerTemporalBulkPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the PostgreSQL database.</param>
        /// <param name="commandTimeout">The time in seconds to wait while trying to execute a command, before terminating the attempt and generating an error. Set to zero for infinity.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public PostgreSqlAccessManagerTemporalBulkPersister
        (
            String connectionString,
            Int32 commandTimeout,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(connectionString, commandTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public Tuple<Guid, DateTime> Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return Load(DateTime.UtcNow, accessManagerToLoadTo, new PersistentStorageEmptyException("The database does not contain any existing events nor data."));
        }

        /// <inheritdoc/>
        public Tuple<Guid, DateTime> Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            // Get the transaction time corresponding to specified event id
            String query =
            @$" 
            SELECT  TO_CHAR(TransactionTime, 'YYYY-MM-DD HH24:MI:ss.US') AS TransactionTime 
            FROM    EventIdToTransactionTimeMap
            WHERE   EventId = '{eventId.ToString()}';";

            IEnumerable<String> queryResults = ExecuteMultiResultQueryAndHandleException
            (
                query,
                "TransactionTime",
                (String cellValue) => { return cellValue; }
            );
            DateTime stateTime = DateTime.MinValue;
            foreach (String currentResult in queryResults)
            {
                if (stateTime == DateTime.MinValue)
                {
                    stateTime = DateTime.ParseExact(currentResult, postgreSQLTimestampFormat, DateTimeFormatInfo.InvariantInfo);
                    stateTime = DateTime.SpecifyKind(stateTime, DateTimeKind.Utc);
                }
                else
                {
                    throw new Exception($"Multiple EventIdToTransactionTimeMap rows were returned with EventId '{eventId.ToString()}'.");
                }
            }
            if (stateTime == DateTime.MinValue)
            {
                throw new ArgumentException($"No EventIdToTransactionTimeMap rows were returned for EventId '{eventId.ToString()}'.", nameof(eventId));
            }

            LoadToAccessManager(stateTime, accessManagerToLoadTo);

            return new Tuple<Guid, DateTime>(eventId, stateTime);
        }

        /// <inheritdoc/>
        public Tuple<Guid, DateTime> Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return Load(stateTime, accessManagerToLoadTo, new ArgumentException($"No EventIdToTransactionTimeMap rows were returned with TransactionTime less than or equal to '{stateTime.ToString(postgreSQLTimestampFormat)}'.", nameof(stateTime)));
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            throw new NotImplementedException();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns all users in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all users in the database valid at the specified time.</returns>
        protected IEnumerable<TUser> GetUsers(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetUsersQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                (String cellValue) => { return userStringifier.FromString(cellValue); }
            );
        }

        /// <summary>
        /// Returns all groups in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all groups in the database valid at the specified time.</returns>
        protected IEnumerable<TGroup> GetGroups(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "Group",
                (String cellValue) => { return groupStringifier.FromString(cellValue); }
            );
        }

        /// <summary>
        /// Returns all user to group mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all user to group mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TUser, TGroup>> GetUserToGroupMappings(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetUserToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                "Group",
                (String cell1Value) => { return userStringifier.FromString(cell1Value); },
                (String cell2Value) => { return groupStringifier.FromString(cell2Value); }
            );
        }

        /// <summary>
        /// Returns all group to group mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all group to group mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TGroup, TGroup>> GetGroupToGroupMappings(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "FromGroup",
                "ToGroup",
                (String cell1Value) => { return groupStringifier.FromString(cell1Value); },
                (String cell2Value) => { return groupStringifier.FromString(cell2Value); }
            );
        }

        /// <summary>
        /// Returns all user to application component and access level mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all user to application component and access level mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TUser, TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                "ApplicationComponent",
                "AccessLevel",
                (String cell1Value) => { return userStringifier.FromString(cell1Value); },
                (String cell2Value) => { return applicationComponentStringifier.FromString(cell2Value); },
                (String cell3Value) => { return accessLevelStringifier.FromString(cell3Value); }
            );
        }

        /// <summary>
        /// Returns all group to application component and access level mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all group to application component and access level mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TGroup, TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "Group",
                "ApplicationComponent",
                "AccessLevel",
                (String cell1Value) => { return groupStringifier.FromString(cell1Value); },
                (String cell2Value) => { return applicationComponentStringifier.FromString(cell2Value); },
                (String cell3Value) => { return accessLevelStringifier.FromString(cell3Value); }
            );
        }

        /// <summary>
        /// Returns all entity types in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all entity types in the database valid at the specified time.</returns>
        protected IEnumerable<String> GetEntityTypes(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EntityType",
                (String cellValue) => { return cellValue; }
            );
        }

        /// <summary>
        /// Returns all entities in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all entities in the database valid at the specified state time. Each tuple contains: the type of the entity, and the entity itself.</returns>
        protected IEnumerable<Tuple<String, String>> GetEntities(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EntityType",
                "Entity",
                (String cell1Value) => { return cell1Value; },
                (String cell2Value) => { return cell2Value; }
            );
        }

        /// <summary>
        /// Returns all user to entity mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all user to entity mappings in the database valid at the specified state time.  Each tuple contains: the user, the type of the entity, and the entity.</returns>
        protected IEnumerable<Tuple<TUser, String, String>> GetUserToEntityMappings(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                "EntityType",
                "Entity",
                (String cell1Value) => { return userStringifier.FromString(cell1Value); },
                (String cell2Value) => { return cell2Value; },
                (String cell3Value) => { return cell3Value; }
            );
        }

        /// <summary>
        /// Returns all group to entity mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all group to entity mappings in the database valid at the specified state time.  Each tuple contains: the group, the type of the entity, and the entity.</returns>
        protected IEnumerable<Tuple<TGroup, String, String>> GetGroupToEntityMappings(DateTime stateTime)
        {
            String query = queryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "Group",
                "EntityType",
                "Entity",
                (String cell1Value) => { return groupStringifier.FromString(cell1Value); },
                (String cell2Value) => { return cell2Value; },
                (String cell3Value) => { return cell3Value; }
            );
        }

        /// <summary>
        /// Creates an <see cref="NpgsqlParameter"/>
        /// </summary>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        /// <returns>The created parameter.</returns>
        protected NpgsqlParameter CreateNpgsqlParameterWithValue(NpgsqlDbType parameterType, Object parameterValue)
        {
            var returnParameter = new NpgsqlParameter();
            returnParameter.NpgsqlDbType = parameterType;
            returnParameter.Value = parameterValue;

            return returnParameter;
        }

        /// <summary>
        /// Attempts to execute a stored procedure which does not return a result set.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters to pass to the stored procedure.</param>
        protected void ExecuteStoredProcedure(String procedureName, IList<NpgsqlParameter> parameters)
        {
            var parameterStringBuilder = new StringBuilder();
            for (Int32 i = 0; i < parameters.Count; i++)
            {
                parameterStringBuilder.Append($"${i + 1}");
                if (i != parameters.Count - 1)
                {
                    parameterStringBuilder.Append(", ");
                }
            }
            String commandText = $"CALL {procedureName}({parameterStringBuilder.ToString()});";

            try
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                using (NpgsqlDataSource dataSource = dataSourceBuilder.Build())
                using (NpgsqlConnection connection = dataSource.OpenConnection())
                using (var command = new NpgsqlCommand(commandText))
                {
                    command.Connection = connection;
                    command.CommandTimeout = commandTimeout;
                    foreach (NpgsqlParameter currentParameter in parameters)
                    {
                        command.Parameters.Add(currentParameter);
                    }
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute stored procedure '{procedureName}' in PostgreSQL.", e);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query which is expected to return multiple rows, handling any resulting exception.
        /// </summary>
        /// <typeparam name="TReturn">The type of data returned from the query.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert">The name of the column in the results to convert to the specified type.</param>
        /// <param name="conversionFromStringFunction">A function which converts a single string-valued cell in the results to the specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected IEnumerable<TReturn> ExecuteMultiResultQueryAndHandleException<TReturn>(String query, String columnToConvert, Func<String, TReturn> conversionFromStringFunction)
        {
            try
            {
                return ExecuteQueryAndConvertColumn(query, columnToConvert, conversionFromStringFunction);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute query '{query}' in PostgreSQL.", e);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query which is expected to return multiple rows, handling any resulting exception.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item returned from the query.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item returned from the query.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <returns>A collection of tuples of the items returned by the query.</returns>
        protected IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteMultiResultQueryAndHandleException<TReturn1, TReturn2>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction
        )
        {
            try
            {
                return ExecuteQueryAndConvertColumn(query, columnToConvert1, columnToConvert2, returnType1ConversionFromStringFunction, returnType2ConversionFromStringFunction);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute query '{query}' in PostgreSQL.", e);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query which is expected to return multiple rows, handling any resulting exception.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item returned from the query.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item returned from the query.</typeparam>
        /// <typeparam name="TReturn3">The type of the third data item returned from the query.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert3">The name of the third column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <param name="returnType3ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the third specified return type.</param>
        /// <returns>A collection of tuples of the items returned by the query.</returns>
        protected IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteMultiResultQueryAndHandleException<TReturn1, TReturn2, TReturn3>
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
            try
            {
                return ExecuteQueryAndConvertColumn
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
            catch (Exception e)
            {
                throw new Exception($"Failed to execute query '{query}' in PostgreSQL.", e);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query, converting a specified column from each row of the results to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert">The name of the column in the results to convert to the specified type.</param>
        /// <param name="conversionFromStringFunction">A function which converts a single string-valued cell in the results to the specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected IEnumerable<T> ExecuteQueryAndConvertColumn<T>(String query, String columnToConvert, Func<String, T> conversionFromStringFunction)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            using (NpgsqlDataSource dataSource = dataSourceBuilder.Build())
            using (NpgsqlConnection connection = dataSource.OpenConnection())
            using (var command = new NpgsqlCommand(query))
            {
                command.Connection = connection;
                command.CommandTimeout = commandTimeout;
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read() == true)
                    {
                        String currentDataItemAsString = reader.GetString(reader.GetOrdinal(columnToConvert));
                        yield return conversionFromStringFunction(currentDataItemAsString);
                    }
                }
                connection.Close();
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
        protected IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteQueryAndConvertColumn<TReturn1, TReturn2>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction
        )
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            using (NpgsqlDataSource dataSource = dataSourceBuilder.Build())
            using (NpgsqlConnection connection = dataSource.OpenConnection())
            using (var command = new NpgsqlCommand(query))
            {
                command.Connection = connection;
                command.CommandTimeout = commandTimeout;
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
                connection.Close();
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
        protected IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteQueryAndConvertColumn<TReturn1, TReturn2, TReturn3>
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
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            using (NpgsqlDataSource dataSource = dataSourceBuilder.Build())
            using (NpgsqlConnection connection = dataSource.OpenConnection())
            using (var command = new NpgsqlCommand(query))
            {
                command.Connection = connection;
                command.CommandTimeout = commandTimeout;
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
                connection.Close();
            }
        }

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp from persistent storage.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <param name="eventIdToTransactionTimeMapRowDoesntExistException">An exception to throw if no rows exist in the 'EventIdToTransactionTimeMap' table equal to or sequentially before the specified state time.</param>
        /// <returns>Values representing the state of the access manager loaded.  The returned tuple contains 2 values: The id of the most recent event persisted into the access manager at the returned state, and the UTC timestamp the event occurred at.</returns>
        protected Tuple<Guid, DateTime> Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo, Exception eventIdToTransactionTimeMapRowDoesntExistException)
        {
            if (stateTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' must be expressed as UTC.", nameof(stateTime));
            DateTime now = DateTime.UtcNow;
            if (stateTime > now)
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' will value '{stateTime.ToString(postgreSQLTimestampFormat)}' is greater than the current time '{now.ToString(postgreSQLTimestampFormat)}'.", nameof(stateTime));

            // Get the event id and transaction time equal to or immediately before the specified state time
            String query =
            @$" 
            SELECT  EventId::varchar AS EventId,
		            TO_CHAR(TransactionTime, 'YYYY-MM-DD HH24:MI:ss.US') AS TransactionTime
            FROM    EventIdToTransactionTimeMap
            WHERE   TransactionTime <= TO_TIMESTAMP('{stateTime.ToString(postgreSQLTimestampFormat)}', 'YYYY-MM-DD HH24:MI:ss.US')::timestamp
            ORDER   BY TransactionTime DESC
            LIMIT   1;";

            IEnumerable<Tuple<Guid, DateTime>> queryResults = ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EventId",
                "TransactionTime",
                (String cellValue) => { return Guid.Parse(cellValue); },
                (String cellValue) =>
                {
                    var stateTime = DateTime.ParseExact(cellValue, postgreSQLTimestampFormat, DateTimeFormatInfo.InvariantInfo);
                    stateTime = DateTime.SpecifyKind(stateTime, DateTimeKind.Utc);

                    return stateTime;
                }
            );
            Guid eventId = default(Guid);
            DateTime transactionTime = DateTime.MinValue;
            foreach (Tuple<Guid, DateTime> currentResult in queryResults)
            {
                eventId = currentResult.Item1;
                transactionTime = currentResult.Item2;
                break;
            }
            if (transactionTime == DateTime.MinValue)
                throw eventIdToTransactionTimeMapRowDoesntExistException;

            LoadToAccessManager(transactionTime, accessManagerToLoadTo);

            return new Tuple<Guid, DateTime>(eventId, transactionTime);
        }

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp from persistent storage into the specified AccessManager instance.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        protected void LoadToAccessManager(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            accessManagerToLoadTo.Clear();
            foreach (TUser currentUser in GetUsers(stateTime))
            {
                accessManagerToLoadTo.AddUser(currentUser);
            }
            foreach (TGroup currentGroup in GetGroups(stateTime))
            {
                accessManagerToLoadTo.AddGroup(currentGroup);
            }
            foreach (Tuple<TUser, TGroup> currentUserToGroupMapping in GetUserToGroupMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToGroupMapping(currentUserToGroupMapping.Item1, currentUserToGroupMapping.Item2);
            }
            foreach (Tuple<TGroup, TGroup> currentGroupToGroupMapping in GetGroupToGroupMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToGroupMapping(currentGroupToGroupMapping.Item1, currentGroupToGroupMapping.Item2);
            }
            foreach (Tuple<TUser, TComponent, TAccess> currentUserToApplicationComponentAndAccessLevelMapping in GetUserToApplicationComponentAndAccessLevelMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToApplicationComponentAndAccessLevelMapping
                (
                    currentUserToApplicationComponentAndAccessLevelMapping.Item1,
                    currentUserToApplicationComponentAndAccessLevelMapping.Item2,
                    currentUserToApplicationComponentAndAccessLevelMapping.Item3
                );
            }
            foreach (Tuple<TGroup, TComponent, TAccess> currentGroupToApplicationComponentAndAccessLevelMapping in GetGroupToApplicationComponentAndAccessLevelMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToApplicationComponentAndAccessLevelMapping
                (
                    currentGroupToApplicationComponentAndAccessLevelMapping.Item1,
                    currentGroupToApplicationComponentAndAccessLevelMapping.Item2,
                    currentGroupToApplicationComponentAndAccessLevelMapping.Item3
                );
            }
            foreach (String currentEntityType in GetEntityTypes(stateTime))
            {
                accessManagerToLoadTo.AddEntityType(currentEntityType);
            }
            foreach (Tuple<String, String> currentEntityTypeAndEntity in GetEntities(stateTime))
            {
                accessManagerToLoadTo.AddEntity(currentEntityTypeAndEntity.Item1, currentEntityTypeAndEntity.Item2);
            }
            foreach (Tuple<TUser, String, String> currentUserToEntityMapping in GetUserToEntityMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToEntityMapping
                (
                    currentUserToEntityMapping.Item1,
                    currentUserToEntityMapping.Item2,
                    currentUserToEntityMapping.Item3
                );
            }
            foreach (Tuple<TGroup, String, String> currentGroupToEntityMapping in GetGroupToEntityMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToEntityMapping
                (
                    currentGroupToEntityMapping.Item1,
                    currentGroupToEntityMapping.Item2,
                    currentGroupToEntityMapping.Item3
                );
            }
        }

        #endregion
    }
}
