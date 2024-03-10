/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Data;
using Microsoft.Data.SqlClient;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence.Sql.SqlServer
{
    /// <summary>
    /// Base for classes which persist access manager events to and allows reading of <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> objects from a Microsoft SQL Server database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class SqlServerAccessManagerTemporalPersisterBase<TUser, TGroup, TComponent, TAccess> : SqlServerPersisterBase, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The maximum size of text columns in the database (restricted by limits on the sizes of index keys... see https://docs.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver16).</summary>
        protected const Int32 columnSizeLimit = 450;
        /// <summary>DateTime format string which matches the <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver16#date-and-time-styles">Transact-SQL 126 date and time style</see>.</summary>
        protected const String transactionSql126DateStyle = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerAccessManagerTemporalPersisterBase class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating am operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        public SqlServerAccessManagerTemporalPersisterBase
        (
            String connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            Int32 operationTimeout, 
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger
        ) : base(connectionString, retryCount, retryInterval, operationTimeout, logger)
        {
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerAccessManagerTemporalPersisterBase class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating am operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public SqlServerAccessManagerTemporalPersisterBase
        (
            String connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            Int32 operationTimeout,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
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
            SELECT  CONVERT(nvarchar(30), TransactionTime , 126) AS 'TransactionTime'
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
                    stateTime = DateTime.ParseExact(currentResult, transactionSql126DateStyle, DateTimeFormatInfo.InvariantInfo);
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
            return Load(stateTime, accessManagerToLoadTo, new ArgumentException($"No EventIdToTransactionTimeMap rows were returned with TransactionTime less than or equal to '{stateTime.ToString(transactionSql126DateStyle)}'.", nameof(stateTime)));
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns all users in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all users in the database valid at the specified time.</returns>
        protected IEnumerable<TUser> GetUsers(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  [User] 
            FROM    Users 
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN TransactionFrom AND TransactionTo;";

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
            String query =
            @$" 
            SELECT  [Group] 
            FROM    Groups 
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN TransactionFrom AND TransactionTo;";

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
            String query =
            @$" 
            SELECT  u.[User], 
                    g.[Group]
            FROM    UserToGroupMappings ug
                    INNER JOIN Users u
                      ON ug.UserId = u.Id
                    INNER JOIN Groups g
                      ON ug.GroupId = g.Id
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN ug.TransactionFrom AND ug.TransactionTo;";

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
            String query =
            @$" 
            SELECT  gg.Id, 
                    fg.[Group] AS 'FromGroup', 
                    tg.[Group] AS 'ToGroup'
            FROM    GroupToGroupMappings gg
                    INNER JOIN Groups fg
                      ON gg.FromGroupId = fg.Id
                    INNER JOIN Groups tg
                      ON gg.ToGroupId = tg.Id
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN gg.TransactionFrom AND gg.TransactionTo;";

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
            String query =
            @$" 
            SELECT  u.[User], 
                    ac.ApplicationComponent, 
                    al.AccessLevel 
            FROM    UserToApplicationComponentAndAccessLevelMappings uaa
                    INNER JOIN Users u
                      ON uaa.UserId = u.Id
                    INNER JOIN ApplicationComponents ac
                      ON uaa.ApplicationComponentId = ac.Id
                    INNER JOIN AccessLevels al
                      ON uaa.AccessLevelId = al.Id
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN uaa.TransactionFrom AND uaa.TransactionTo;";

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
            String query =
            @$" 
            SELECT  g.[Group], 
                    ac.ApplicationComponent, 
                    al.AccessLevel 
            FROM    GroupToApplicationComponentAndAccessLevelMappings gaa
                    INNER JOIN Groups g
                      ON gaa.GroupId = g.Id
                    INNER JOIN ApplicationComponents ac
                      ON gaa.ApplicationComponentId = ac.Id
                    INNER JOIN AccessLevels al
                      ON gaa.AccessLevelId = al.Id
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN gaa.TransactionFrom AND gaa.TransactionTo;";

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
            String query =
            @$" 
            SELECT  EntityType
            FROM    EntityTypes 
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN TransactionFrom AND TransactionTo;";

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
            String query =
            @$" 
            SELECT  et.EntityType, 
                    e.Entity 
            FROM    Entities e
                    INNER JOIN EntityTypes et
                      ON e.EntityTypeId = et.Id
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN e.TransactionFrom AND e.TransactionTo;";

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
            String query =
            @$" 
            SELECT  u.[User], 
                    et.EntityType, 
                    e.Entity
            FROM    UserToEntityMappings ue
                    INNER JOIN Users u
                      ON ue.UserId = u.Id
                    INNER JOIN EntityTypes et
                      ON ue.EntityTypeId = et.Id
                    INNER JOIN Entities e
                      ON ue.EntityId = e.Id
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN ue.TransactionFrom AND ue.TransactionTo;";

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
            String query =
            @$" 
            SELECT  g.[Group], 
                    et.EntityType, 
                    e.Entity
            FROM    GroupToEntityMappings ge
                    INNER JOIN Groups g
                        ON ge.GroupId = g.Id
                    INNER JOIN EntityTypes et
                        ON ge.EntityTypeId = et.Id
                    INNER JOIN Entities e
                        ON ge.EntityId = e.Id
            WHERE   CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN ge.TransactionFrom AND ge.TransactionTo;";

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

        #pragma warning restore 1573

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
                throw new Exception($"Failed to execute query '{query}' in SQL Server.", e);
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
                throw new Exception($"Failed to execute query '{query}' in SQL Server.", e);
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
                throw new Exception($"Failed to execute query '{query}' in SQL Server.", e);
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
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query))
            {
                connection.RetryLogicProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(sqlRetryLogicOption);
                connection.RetryLogicProvider.Retrying += connectionRetryAction;
                connection.Open();
                command.Connection = connection;
                command.CommandTimeout = operationTimeout;
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String currentDataItemAsString = (String)dataReader[columnToConvert];
                        yield return conversionFromStringFunction.Invoke(currentDataItemAsString);
                    }
                }
                connection.RetryLogicProvider.Retrying -= connectionRetryAction;
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
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query))
            {
                connection.RetryLogicProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(sqlRetryLogicOption);
                connection.RetryLogicProvider.Retrying += connectionRetryAction;
                connection.Open();
                command.Connection = connection;
                command.CommandTimeout = operationTimeout;
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
                connection.RetryLogicProvider.Retrying -= connectionRetryAction;
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
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(query))
            {
                connection.RetryLogicProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(sqlRetryLogicOption);
                connection.RetryLogicProvider.Retrying += connectionRetryAction;
                connection.Open();
                command.Connection = connection;
                command.CommandTimeout = operationTimeout;
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
                connection.RetryLogicProvider.Retrying -= connectionRetryAction;
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
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' will value '{stateTime.ToString(transactionSql126DateStyle)}' is greater than the current time '{now.ToString(transactionSql126DateStyle)}'.", nameof(stateTime));

            // Get the event id and transaction time equal to or immediately before the specified state time
            String query =
            @$" 
            SELECT  TOP(1)
                    CONVERT(nvarchar(40), EventId) AS 'EventId',
                    CONVERT(nvarchar(30), TransactionTime , 126) AS 'TransactionTime'
            FROM    EventIdToTransactionTimeMap
            WHERE   TransactionTime <= CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126)
            ORDER   BY TransactionTime DESC;";

            IEnumerable<Tuple<Guid, DateTime>> queryResults = ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EventId",
                "TransactionTime",
                (String cellValue) => { return Guid.Parse(cellValue); },
                (String cellValue) =>
                {
                    var stateTime = DateTime.ParseExact(cellValue, transactionSql126DateStyle, DateTimeFormatInfo.InvariantInfo);
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
