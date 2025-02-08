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
using System.Globalization;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution.Persistence.SqlServer
{
    /// <summary>
    /// Persists access manager events in bulk to, and allows reading of events from an AccessManager instance persistent storage, filtered by a shard range and retrieved in batches. 
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class SqlServerAccessManagerTemporalEventBatchReader<TUser, TGroup, TComponent, TAccess> :
        SqlServerAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>,
        IAccessManagerTemporalEventBatchReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerAccessManagerTemporalEventBatchReader class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        public SqlServerAccessManagerTemporalEventBatchReader
        (
            string connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            Int32 operationTimeout,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger
        )
            : base(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerAccessManagerTemporalEventBatchReader class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public SqlServerAccessManagerTemporalEventBatchReader
        (
            string connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            Int32 operationTimeout,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : base(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerAccessManagerTemporalEventBatchReader class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="storedProcedureExecutor">A test (mock) <see cref="IStoredProcedureExecutionWrapper"/> object.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public SqlServerAccessManagerTemporalEventBatchReader
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
            IMetricLogger metricLogger,
            IStoredProcedureExecutionWrapper storedProcedureExecutor
        )
            : base(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger, metricLogger, storedProcedureExecutor)
        {
        }

        /// <inheritdoc/>
        public Guid GetInitialEvent()
        {
            String query =
            @$" 
            SELECT  TOP(1)
                    CONVERT(nvarchar(40), EventId) AS 'EventId'
            FROM    EventIdToTransactionTimeMap 
            WHERE   TransactionTime =
                    (
                        SELECT  MIN(TransactionTime)
                        FROM    EventIdToTransactionTimeMap 
                    )
            ORDER   BY TransactionSequence;";

            return ExecuteAccessManagerEventRetrievalQuery(query, new Exception($"No events were found in the SQL Server database."));
        }

        /// <inheritdoc/>
        public Nullable<Guid> GetNextStateAfter(Guid inputEventId)
        {
            String query =
            $@"
            SELECT  *
            FROM    EventIdToTransactionTimeMap 
            WHERE   TransactionTime >= 
                    (
                        SELECT  TransactionTime
                        FROM    EventIdToTransactionTimeMap
                        WHERE   EventId = '{inputEventId.ToString()}'
                    ) 
            ORDER   BY TransactionTime, 
                        TransactionSequence
            OFFSET  1 ROWS   
            FETCH   NEXT 1 ROWS ONLY;";
            var eventNotFoundException = new ArgumentException($"Parameter '{nameof(inputEventId)}' with value '{inputEventId}' contained an event id which was not found in the SQL Server database.", nameof(inputEventId));

            return ExecuteAccessManagerEventRetrievalQuery(query, eventNotFoundException);
        }

        /// <inheritdoc/>
        public Tuple<IList<TemporalEventBufferItemBase>, Nullable<Guid>> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd, Int32 eventCount)
        {
            // For query where clause can specify something like
            // TranSTime >= specufued trans time
            // AND NOT ( TranSTime == specified transtime AND sequence num < specific seq number )
            // Need to put some fake data in to test this
            // Need to write a new custom version of ExecuteAccessManagerEventRetrievalQuery

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IList<TemporalEventBufferItemBase> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            throw new NotImplementedException();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Executes the specified query which should return a single event id.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="eventNotFoundException">The exception to throw if no rows are returned by the query.</param>
        /// <returns>The event id.</returns>
        protected Guid ExecuteAccessManagerEventRetrievalQuery(String query, Exception eventNotFoundException)
        {
            IEnumerable<Guid> queryResults = persisterUtilities.ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EventId",
                (String cellValue) => { return Guid.Parse(cellValue); }
            );
            foreach (Guid currentResult in queryResults)
            { 
                return currentResult;
            }
            throw eventNotFoundException;
        }

        /// <summary>
        /// Generates a query which returns an ordered series of events.
        /// </summary>
        /// <param name="transactionTime">The transaction time of the first event to return.</param>
        /// <param name="transactionSequence">The transaction sequence number of the first event to return.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to return.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to return.</param>
        /// <param name="eventCount">The (optional) maximum number of events to return.  If not specified, all events in the database are returned.</param>
        /// <returns>The query.</returns>
        protected String GenerateGetEventsQuery(DateTime transactionTime, Int32 transactionSequence, Int32 hashRangeStart, Int32 hashRangeEnd, Int32 eventCount = -1)
        {
            String topStatement = "";
            if (eventCount != -1)
            {
                topStatement =
                $@"TOP({eventCount})
                ";
            }
            String query =
            $@"
            SELECT  {topStatement}
                    EventType, 
                    EventId, 
                    TransactionSequence AS EventSequence, 
                    EventAction, 
                    OccurredTime, 
                    HashCode, 
                    EventData1, 
                    EventData2, 
                    EventData3
            FROM    (
                        SELECT  'user' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                eu.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                NULL AS EventData2, 
                                NULL AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToUserMap eu
                                    ON ettm.EventId = eu.EventId
                                INNER JOIN Users u
                                    ON eu.UserId = u.Id
                                INNER JOIN Actions a
                                    ON eu.ActionId = a.Id
                        UNION ALL
                        SELECT  'group' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                eg.HashCode AS HashCode, 
                                g.[Group] AS EventData1, 
                                NULL AS EventData2, 
                                NULL AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToGroupMap eg
                                    ON ettm.EventId = eg.EventId
                                INNER JOIN Groups g
                                    ON eg.GroupId = g.Id
                                INNER JOIN Actions a
                                    ON eg.ActionId = a.Id
                        UNION ALL
                        SELECT  'userToGroupMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                eug.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                g.[Group]  AS EventData2, 
                                NULL AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToUserToGroupMap eug
                                    ON ettm.EventId = eug.EventId
                                INNER JOIN UserToGroupMappings ug
                                    ON eug.UserToGroupMappingId = ug.Id
                                INNER JOIN Users u
                                    ON ug.UserId = u.Id
                                INNER JOIN Groups g
                                    ON ug.GroupId = g.Id
                                INNER JOIN Actions a
                                    ON eug.ActionId = a.Id
                        UNION ALL
                        SELECT  'userToApplicationComponentAndAccessLevelMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                euaa.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                ac.ApplicationComponent AS EventData2, 
                                al.AccessLevel AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToUserToApplicationComponentAndAccessLevelMap euaa
                                    ON ettm.EventId = euaa.EventId
                                INNER JOIN UserToApplicationComponentAndAccessLevelMappings uaa
                                    ON euaa.UserToApplicationComponentAndAccessLevelMappingId = uaa.Id
                                INNER JOIN Users u
                                    ON uaa.UserId = u.Id
                                INNER JOIN ApplicationComponents ac
                                    ON uaa.ApplicationComponentId = ac.Id
                                INNER JOIN AccessLevels al
                                    ON uaa.AccessLevelId = al.Id
                                INNER JOIN Actions a
                                    ON euaa.ActionId = a.Id
                        UNION ALL
                        SELECT  'groupToApplicationComponentAndAccessLevelMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                egaa.HashCode AS HashCode, 
                                g.[Group] AS EventData1, 
                                ac.ApplicationComponent AS EventData2, 
                                al.AccessLevel AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToGroupToApplicationComponentAndAccessLevelMap egaa
                                    ON ettm.EventId = egaa.EventId
                                INNER JOIN GroupToApplicationComponentAndAccessLevelMappings gaa
                                    ON egaa.GroupToApplicationComponentAndAccessLevelMappingId = gaa.Id
                                INNER JOIN Groups g
                                    ON gaa.GroupId = g.Id
                                INNER JOIN ApplicationComponents ac
                                    ON gaa.ApplicationComponentId = ac.Id
                                INNER JOIN AccessLevels al
                                    ON gaa.AccessLevelId = al.Id
                                INNER JOIN Actions a
                                    ON egaa.ActionId = a.Id
                        UNION ALL 
                        SELECT  'entityType' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                eet.HashCode AS HashCode, 
                                et.EntityType AS EventData1, 
                                NULL AS EventData2, 
                                NULL AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToEntityTypeMap eet
                                    ON ettm.EventId = eet.EventId
                                INNER JOIN EntityTypes et
                                    ON eet.EntityTypeId = et.Id
                                INNER JOIN Actions a
                                    ON eet.ActionId = a.Id
                        UNION ALL 
                        SELECT  'entity' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                ee.HashCode AS HashCode, 
                                et.EntityType AS EventData1, 
                                e.Entity AS EventData2, 
                                NULL AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToEntityMap ee
                                    ON ettm.EventId = ee.EventId
                                INNER JOIN Entities e 
                                    ON ee.EntityId = e.Id
                                INNER JOIN EntityTypes et
                                    ON e.EntityTypeId = et.Id
                                INNER JOIN Actions a
                                    ON ee.ActionId = a.Id
                        UNION ALL
                        SELECT  'userToEntityMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                eue.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                et.EntityType AS EventData2, 
                                e.Entity AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToUserToEntityMap eue
                                    ON ettm.EventId = eue.EventId
                                INNER JOIN UserToEntityMappings ue
                                    ON eue.UserToEntityMappingId = ue.Id
                                INNER JOIN Users u
                                    ON ue.UserId = u.Id
                                INNER JOIN EntityTypes et
                                    ON ue.EntityTypeId = et.Id
                                INNER JOIN Entities e 
                                    ON ue.EntityId = e.Id
                                INNER JOIN Actions a
                                    ON eue.ActionId = a.Id
                        UNION ALL
                        SELECT  'groupToEntityMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                CONVERT(nvarchar(30), ettm.TransactionTime , 126) AS OccurredTime, 
                                ege.HashCode AS HashCode, 
                                g.[Group] AS EventData1, 
                                et.EntityType AS EventData2, 
                                e.Entity AS EventData3, 
                                ettm.TransactionTime AS TransactionTime, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToGroupToEntityMap ege
                                    ON ettm.EventId = ege.EventId
                                INNER JOIN GroupToEntityMappings ge
                                    ON ege.GroupToEntityMappingId = ge.Id
                                INNER JOIN Groups g
                                    ON ge.GroupId = g.Id
                                INNER JOIN EntityTypes et
                                    ON ge.EntityTypeId = et.Id
                                INNER JOIN Entities e 
                                    ON ge.EntityId = e.Id
                                INNER JOIN Actions a
                                    ON ege.ActionId = a.Id
                    ) AS AllEvents
            WHERE   TransactionTime >= CONVERT(datetime2, '{transactionTime.ToString(transactionSql126DateStyle)}', 126) 
              AND   NOT (TransactionTime = CONVERT(datetime2, '{transactionTime.ToString(transactionSql126DateStyle)}', 126) AND TransactionSequence < {transactionSequence})
              AND   HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd}
            ORDER   BY TransactionTime, 
                       TransactionSequence
            ";

            return query;
        }

        #endregion
    }
}
