/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationLogging;
using ApplicationMetrics;
using System.Collections;

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
        IAccessManagerTemporalEventBatchReader,
        IAccessManagerTemporalEventDeleter
    {
        #pragma warning disable 1591

        protected const String eventSequenceColumnName = "EventSequence";

        protected const String deleteUserEventsStoredProcedureName = "DeleteUserEvents";
        protected const String deleteGroupEventsStoredProcedureName = "DeleteGroupEvents";
        protected const String deleteUserToGroupMappingEventsStoredProcedureName = "DeleteUserToGroupMappingEvents";
        protected const String deleteUserToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName = "DeleteUserToApplicationComponentAndAccessLevelMappingEvents";
        protected const String deleteGroupToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName = "DeleteGroupToApplicationComponentAndAccessLevelMappingEvents";
        protected const String deleteUserToEntityMappingEventsStoredProcedureName = "DeleteUserToEntityMappingEvents";
        protected const String deleteGroupToEntityMappingEventsStoredProcedureName = "DeleteGroupToEntityMappingEvents";
        protected const String hashRangeStartParameterName = "@HashRangeStart";
        protected const String hashRangeEndParameterName = "@HashRangeEnd";

        #pragma warning restore 1591

        /// <summary>Maps values from the 'EventType' column in a query returning a sequence of events, to a function which converts a row of that query to a <see cref="TemporalEventBufferItemBase"/>.</summary>
        protected Dictionary<String, Func<SqlDataReader, TemporalEventBufferItemBase>> eventTypeToConversionFunctionMap;

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
            String connectionString,
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
            eventTypeToConversionFunctionMap = CreateEventTypeToConversionFunctionMap();
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
        )
            : base(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger, metricLogger)
        {
            eventTypeToConversionFunctionMap = CreateEventTypeToConversionFunctionMap();
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
            eventTypeToConversionFunctionMap = CreateEventTypeToConversionFunctionMap();
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
        public Nullable<Guid> GetNextEventAfter(Guid inputEventId)
        {
            String validationQuery =
            $@"
            SELECT  CONVERT(nvarchar(40), EventId) AS 'EventId' 
            FROM    EventIdToTransactionTimeMap 
            WHERE   EventId = '{inputEventId.ToString()}';";
            var eventNotFoundException = new ArgumentException($"Parameter '{nameof(inputEventId)}' with value '{inputEventId}' contained an event id which was not found in the SQL Server database.", nameof(inputEventId));
            ExecuteAccessManagerEventRetrievalQuery(validationQuery, eventNotFoundException);

            String retrievalQuery =
            $@"
            SELECT  CONVERT(nvarchar(40), EventId) AS 'EventId' 
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
            IEnumerable<Guid> queryResults = persisterUtilities.ExecuteMultiResultQueryAndHandleException
            (
                retrievalQuery,
                "EventId",
                (String cellValue) => { return Guid.Parse(cellValue); }
            );
            foreach (Guid currentResult in queryResults)
            {
                return currentResult;
            }
            return null;
        }

        /// <inheritdoc/>
        /// <remarks>Parameter <paramref name="filterGroupEventsByHashRange"/> is designed to be used on a database behind a shard in a distributed AccessManager implementation, depending on the type of data element managed by the shard.  For user shards the parameter should be set false, to capture all the group which may be present in user to group mappings.  For group shards it should be set true, to properly filter the returned groups and group mappings.</remarks>
        public IList<TemporalEventBufferItemBase> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd, Boolean filterGroupEventsByHashRange, Int32 eventCount)
        {
            return GetEventsImplementation(initialEventId, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventCount);
        }

        /// <inheritdoc/>
        /// <remarks>Parameter <paramref name="filterGroupEventsByHashRange"/> is designed to be used on a database behind a shard in a distributed AccessManager implementation, depending on the type of data element managed by the shard.  For user shards the parameter should be set false, to capture all the group which may be present in user to group mappings.  For group shards it should be set true, to properly filter the returned groups and group mappings.</remarks>
        public IList<TemporalEventBufferItemBase> GetEvents(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd, Boolean filterGroupEventsByHashRange)
        {
            return GetEventsImplementation(initialEventId, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, null);
        }

        /// <inheritdoc/>
        /// <remarks>Parameter <paramref name="includeGroupEvents"/> is designed to be used on a database behind a shard in a distributed AccessManager implementation, depending on the type of data element managed by the shard.  For user shards the parameter should be set false, to avoid deleting any groups which may be present in user to group mappings.  For group shards it should be set true.</remarks>
        public void DeleteEvents(Int32 hashRangeStart, Int32 hashRangeEnd, Boolean includeGroupEvents)
        {
            DeleteGroupToEntityMappingEvents(hashRangeStart, hashRangeEnd);
            DeleteUserToEntityMappingEvents(hashRangeStart, hashRangeEnd);
            DeleteGroupToApplicationComponentAndAccessLevelMappingEvents(hashRangeStart, hashRangeEnd);
            DeleteUserToApplicationComponentAndAccessLevelMappingEvents(hashRangeStart, hashRangeEnd);
            DeleteUserToGroupMappingEvents(hashRangeStart, hashRangeEnd);
            if (includeGroupEvents == true)
            {
                DeleteGroupEvents(hashRangeStart, hashRangeEnd);
            }
            DeleteUserEvents(hashRangeStart, hashRangeEnd);
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
        /// Retrieves the sequence of events which follow (and potentially include) the specified event.
        /// </summary>
        /// <param name="initialEventId">The id of the earliest event in the sequence.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to retrieve.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will return all group events if set to false.</param>
        /// <param name="eventCount">The number of events to retrieve (including that specified in <paramref name="initialEventId"/>).  Set to null to retrieve all events.</param>
        /// <returns>The sequence of events in order of ascending date/time, and including that specified in <paramref name="initialEventId"/>, or an empty list if the event represented by <paramref name="initialEventId"/> is the latest.</returns>
        protected IList<TemporalEventBufferItemBase> GetEventsImplementation(Guid initialEventId, Int32 hashRangeStart, Int32 hashRangeEnd, Boolean filterGroupEventsByHashRange, Nullable<Int32> eventCount)
        {
            // Get the AccessManagerState corresponding to 'initialEventId'
            AccessManagerState initialEventState;
            try
            {
                initialEventState = persisterUtilities.GetAccessManagerStateForEventId(initialEventId);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to get events for initial event with id '{initialEventId.ToString()}'.", e);
            }
            String query = GenerateGetEventsQuery(initialEventState.StateTime, initialEventState.StateSequence, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventCount);

            // Query and create the sequence of events
            Func<SqlCommand, List<TemporalEventBufferItemBase>> eventsConversionFunction = (SqlCommand command) =>
            {
                var results = new List<TemporalEventBufferItemBase>();
                using (SqlDataReader dataReader = command.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        String currentEventType = (String)dataReader[eventTypeColumnName];
                        if (eventTypeToConversionFunctionMap.ContainsKey(currentEventType) == false)
                        {
                            throw new Exception($"Column '{eventTypeColumnName}' in event query results contained unhandled event type '{currentEventType}'.");
                        }
                        TemporalEventBufferItemBase currentEvent;
                        try
                        {
                            currentEvent = eventTypeToConversionFunctionMap[currentEventType].Invoke(dataReader);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"Failed to convert row in event query results for event type '{currentEventType}' to a {typeof(TemporalEventBufferItemBase).Name}.", e);
                        }
                        results.Add(currentEvent);
                    }
                }

                return results;
            };
            List<TemporalEventBufferItemBase> events;
            try
            {
                events = persisterUtilities.ExecuteQueryAndConvertColumnWithDeadlockRetry(query, eventsConversionFunction);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute event read query and convert results to events.", e);
            }

            return events;
        }

        /// <summary>
        /// Generates a query which returns an ordered series of events.
        /// </summary>
        /// <param name="transactionTime">The transaction time of the first event to return.</param>
        /// <param name="transactionSequence">The transaction sequence number of the first event to return.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to return.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to return.</param>
        /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will return all group events if set to false.</param>
        /// <param name="eventCount">The (optional) maximum number of events to return.  If not specified, all events in the database are returned.</param>
        /// <returns>The query.</returns>
        protected String GenerateGetEventsQuery(DateTime transactionTime, Int32 transactionSequence, Int32 hashRangeStart, Int32 hashRangeEnd, Boolean filterGroupEventsByHashRange, Nullable<Int32> eventCount = null)
        {
            String topStatement = "";
            if (eventCount.HasValue == true)
            {
                topStatement = $"TOP({eventCount.Value})";
            }
            String groupEventsWhereClause = "";
            if (filterGroupEventsByHashRange == true)
            {
                groupEventsWhereClause = $"WHERE   eg.HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd} ";
            }

            String query =
            $@"
            SELECT  {topStatement} 
                    EventType, 
                    CONVERT(nvarchar(40), EventId) AS EventId, 
                    CONVERT(nvarchar(30), TransactionSequence) AS EventSequence, 
                    EventAction, 
                    CONVERT(nvarchar(30), TransactionTime , 126) AS OccurredTime, 
                    CONVERT(nvarchar(30), HashCode , 126) AS HashCode,  
                    EventData1, 
                    EventData2, 
                    EventData3
            FROM    (
                        SELECT  'user' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                ettm.TransactionTime AS TransactionTime, 
                                eu.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                NULL AS EventData2, 
                                NULL AS EventData3, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToUserMap eu
                                    ON ettm.EventId = eu.EventId
                                INNER JOIN Users u
                                    ON eu.UserId = u.Id
                                INNER JOIN Actions a
                                    ON eu.ActionId = a.Id
                        WHERE   eu.HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd} 
                        UNION ALL
                        SELECT  'group' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                ettm.TransactionTime AS TransactionTime, 
                                eg.HashCode AS HashCode, 
                                g.[Group] AS EventData1, 
                                NULL AS EventData2, 
                                NULL AS EventData3, 
                                ettm.TransactionSequence AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap ettm
                                INNER JOIN EventIdToGroupMap eg
                                    ON ettm.EventId = eg.EventId
                                INNER JOIN Groups g
                                    ON eg.GroupId = g.Id
                                INNER JOIN Actions a
                                    ON eg.ActionId = a.Id
                        {groupEventsWhereClause}
                        UNION ALL 
                        SELECT  'userToGroupMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                ettm.TransactionTime AS TransactionTime, 
                                eug.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                g.[Group]  AS EventData2, 
                                NULL AS EventData3, 
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
                        WHERE   eug.HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd} 
                        UNION ALL
                        SELECT  'userToApplicationComponentAndAccessLevelMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                ettm.TransactionTime AS TransactionTime, 
                                euaa.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                ac.ApplicationComponent AS EventData2, 
                                al.AccessLevel AS EventData3, 
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
                        WHERE   euaa.HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd} 
                        UNION ALL
                        SELECT  'groupToApplicationComponentAndAccessLevelMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                ettm.TransactionTime AS TransactionTime, 
                                egaa.HashCode AS HashCode, 
                                g.[Group] AS EventData1, 
                                ac.ApplicationComponent AS EventData2, 
                                al.AccessLevel AS EventData3, 
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
                        WHERE   egaa.HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd} 
                        UNION ALL 
                        SELECT  'entityType' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                ettm.TransactionTime AS TransactionTime, 
                                eet.HashCode AS HashCode, 
                                et.EntityType AS EventData1, 
                                NULL AS EventData2, 
                                NULL AS EventData3, 
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
                                ettm.TransactionTime AS TransactionTime, 
                                ee.HashCode AS HashCode, 
                                et.EntityType AS EventData1, 
                                e.Entity AS EventData2, 
                                NULL AS EventData3, 
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
                                ettm.TransactionTime AS TransactionTime, 
                                eue.HashCode AS HashCode, 
                                u.[User] AS EventData1, 
                                et.EntityType AS EventData2, 
                                e.Entity AS EventData3, 
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
                        WHERE   eue.HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd} 
                        UNION ALL
                        SELECT  'groupToEntityMapping' AS EventType, 
                                ettm.EventId AS EventId, 
                                a.[Action] AS EventAction, 
                                ettm.TransactionTime AS TransactionTime, 
                                ege.HashCode AS HashCode, 
                                g.[Group] AS EventData1, 
                                et.EntityType AS EventData2, 
                                e.Entity AS EventData3, 
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
                        WHERE   ege.HashCode BETWEEN {hashRangeStart} AND {hashRangeEnd} 
                    ) AS AllEvents
            WHERE   TransactionTime >= CONVERT(datetime2, '{transactionTime.ToString(transactionSql126DateStyle)}', 126) 
              AND   NOT (TransactionTime = CONVERT(datetime2, '{transactionTime.ToString(transactionSql126DateStyle)}', 126) AND TransactionSequence < {transactionSequence}) 
            ORDER   BY TransactionTime, 
                       TransactionSequence
            ";

            return query;
        }

        /// <summary>
        /// Returns a dictionary mapping values from the 'EventType' column in a query returning a sequence of events, to a function which converts a row of that query to a <see cref="TemporalEventBufferItemBase"/>.
        /// </summary>
        /// <returns>The dictionary.</returns>
        protected Dictionary<String, Func<SqlDataReader, TemporalEventBufferItemBase>> CreateEventTypeToConversionFunctionMap()
        {
            var returnDictionary = new Dictionary<String, Func<SqlDataReader, TemporalEventBufferItemBase>>();
            returnDictionary.Add
            (
                userEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        TUser user = userStringifier.FromString((String)dataReader[eventData1ColumnName]);
                        var userEvent = new UserEventBufferItem<TUser>(eventId, eventAction, user, occurredTime, hashCode);
                        return userEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                groupEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        TGroup group = groupStringifier.FromString((String)dataReader[eventData1ColumnName]);
                        var groupEvent = new GroupEventBufferItem<TGroup>(eventId, eventAction, group, occurredTime, hashCode);
                        return groupEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                userToGroupMappingEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        TUser user = userStringifier.FromString((String)dataReader[eventData1ColumnName]);
                        TGroup group = groupStringifier.FromString((String)dataReader[eventData2ColumnName]);
                        var usertoGroupMappingEvent = new UserToGroupMappingEventBufferItem<TUser,TGroup>(eventId, eventAction, user, group, occurredTime, hashCode);
                        return usertoGroupMappingEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                userToApplicationComponentAndAccessLevelMappingEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        TUser user = userStringifier.FromString((String)dataReader[eventData1ColumnName]);
                        TComponent applicationComponent = applicationComponentStringifier.FromString((String)dataReader[eventData2ColumnName]);
                        TAccess accessLevel = accessLevelStringifier.FromString((String)dataReader[eventData3ColumnName]);
                        var userToApplicationComponentAndAccessLevelMappingEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>
                        (
                            eventId, 
                            eventAction, 
                            user, 
                            applicationComponent, 
                            accessLevel, 
                            occurredTime, 
                            hashCode
                        );
                        return userToApplicationComponentAndAccessLevelMappingEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                groupToApplicationComponentAndAccessLevelMappingEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        TGroup group = groupStringifier.FromString((String)dataReader[eventData1ColumnName]);
                        TComponent applicationComponent = applicationComponentStringifier.FromString((String)dataReader[eventData2ColumnName]);
                        TAccess accessLevel = accessLevelStringifier.FromString((String)dataReader[eventData3ColumnName]);
                        var groupToApplicationComponentAndAccessLevelMappingEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>
                        (
                            eventId,
                            eventAction,
                            group,
                            applicationComponent,
                            accessLevel,
                            occurredTime,
                            hashCode
                        );
                        return groupToApplicationComponentAndAccessLevelMappingEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                entityTypeEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        String entityType = (String)dataReader[eventData1ColumnName];
                        var entityTypeEvent = new EntityTypeEventBufferItem(eventId, eventAction, entityType, occurredTime, hashCode);
                        return entityTypeEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                entityEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        String entityType = (String)dataReader[eventData1ColumnName];
                        String entity = (String)dataReader[eventData2ColumnName];
                        var entityEvent = new EntityEventBufferItem(eventId, eventAction, entityType, entity, occurredTime, hashCode);
                        return entityEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                userToEntityMappingEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        TUser user = userStringifier.FromString((String)dataReader[eventData1ColumnName]);
                        String entityType = (String)dataReader[eventData2ColumnName];
                        String entity = (String)dataReader[eventData3ColumnName];
                        var userToEntityMappingEvent = new UserToEntityMappingEventBufferItem<TUser>
                        (
                            eventId,
                            eventAction,
                            user,
                            entityType,
                            entity,
                            occurredTime,
                            hashCode
                        );
                        return userToEntityMappingEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );
            returnDictionary.Add
            (
                groupToEntityMappingEventTypeValue,
                (SqlDataReader dataReader) =>
                {
                    Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction =
                    (
                        eventId, eventAction, occurredTime, hashCode, dataReader
                    ) =>
                    {
                        TGroup group = groupStringifier.FromString((String)dataReader[eventData1ColumnName]);
                        String entityType = (String)dataReader[eventData2ColumnName];
                        String entity = (String)dataReader[eventData3ColumnName];
                        var groupToEntityMappingEvent = new GroupToEntityMappingEventBufferItem<TGroup>
                        (
                            eventId,
                            eventAction,
                            group,
                            entityType,
                            entity,
                            occurredTime,
                            hashCode
                        );
                        return groupToEntityMappingEvent;
                    };
                    return ReadCommonEventProperties(dataReader, readSpecificPropertiesFunction);
                }
            );

            return returnDictionary;
        }

        /// <summary>
        /// Reads common <see cref="TemporalEventBufferItemBase"/> properties from the specified data reader, before passing these properties to a function which reads specific properties of the derived event type.
        /// </summary>
        /// <param name="dataReader">The <see cref="SqlDataReader"/> to read the event properties from.</param>
        /// <param name="readSpecificPropertiesFunction">A function which reads specific properties of a type derived from <see cref="TemporalEventBufferItemBase"/>, and returns an instance of that event.  Accepts 5 parameters: The event id, the event action, the event occured time, the hash code of the event's key element, and the data reader to read the specific properties from, and returns the event.</param>
        /// <returns>The event.</returns>
        protected TemporalEventBufferItemBase ReadCommonEventProperties(SqlDataReader dataReader, Func<Guid, EventAction, DateTime, Int32, SqlDataReader, TemporalEventBufferItemBase> readSpecificPropertiesFunction)
        {
            String eventIdAsString = (String)dataReader[eventIdColumnName];
            String eventActionAsString = (String)dataReader[eventActionColumnName];
            String occurredTimeAsString = (String)dataReader[occurredTimeColumnName];
            String hashCodeAsString = (String)dataReader[hashCodeColumnName];
            Guid eventId = Guid.Parse(eventIdAsString);
            EventAction eventAction;
            if (eventActionAsString == addEventActionValue)
            {
                eventAction = EventAction.Add;
            }
            else if (eventActionAsString == removeEventActionValue)
            {
                eventAction = EventAction.Remove;
            }
            else
            {
                throw new Exception($"Column '{eventActionColumnName}' in event query results contained unhandled event action '{eventActionAsString}'.");
            }
            DateTime occurredTime = DateTime.ParseExact(occurredTimeAsString, transactionSql126DateStyle, DateTimeFormatInfo.InvariantInfo);
            occurredTime = DateTime.SpecifyKind(occurredTime, DateTimeKind.Utc);
            Int32 hashCode = Int32.Parse(hashCodeAsString);

            return readSpecificPropertiesFunction(eventId, eventAction, occurredTime, hashCode, dataReader);
        }

        /// <summary>
        /// Permanently deletes all user events from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        protected void DeleteUserEvents(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            List<SqlParameter> parameters = CreateHashCodeRangeSqlParameters(hashRangeStart, hashRangeEnd);
            storedProcedureExecutor.Execute(deleteUserEventsStoredProcedureName, parameters);
        }

        /// <summary>
        /// Permanently deletes all group events from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        protected void DeleteGroupEvents(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            List<SqlParameter> parameters = CreateHashCodeRangeSqlParameters(hashRangeStart, hashRangeEnd);
            storedProcedureExecutor.Execute(deleteGroupEventsStoredProcedureName, parameters);
        }

        /// <summary>
        /// Permanently deletes all user to group mapping events from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        protected void DeleteUserToGroupMappingEvents(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            List<SqlParameter> parameters = CreateHashCodeRangeSqlParameters(hashRangeStart, hashRangeEnd);
            storedProcedureExecutor.Execute(deleteUserToGroupMappingEventsStoredProcedureName, parameters);
        }

        /// <summary>
        /// Permanently deletes all user to application component and access level mapping events from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        protected void DeleteUserToApplicationComponentAndAccessLevelMappingEvents(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            List<SqlParameter> parameters = CreateHashCodeRangeSqlParameters(hashRangeStart, hashRangeEnd);
            storedProcedureExecutor.Execute(deleteUserToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName, parameters);
        }

        /// <summary>
        /// Permanently deletes all group to application component and access level mapping events from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        protected void DeleteGroupToApplicationComponentAndAccessLevelMappingEvents(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            List<SqlParameter> parameters = CreateHashCodeRangeSqlParameters(hashRangeStart, hashRangeEnd);
            storedProcedureExecutor.Execute(deleteGroupToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName, parameters);
        }

        /// <summary>
        /// Permanently deletes all user to entity mapping events from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        protected void DeleteUserToEntityMappingEvents(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            List<SqlParameter> parameters = CreateHashCodeRangeSqlParameters(hashRangeStart, hashRangeEnd);
            storedProcedureExecutor.Execute(deleteUserToEntityMappingEventsStoredProcedureName, parameters);
        }

        /// <summary>
        /// Permanently deletes all group to entity mapping events from persistent storage.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        protected void DeleteGroupToEntityMappingEvents(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            List<SqlParameter> parameters = CreateHashCodeRangeSqlParameters(hashRangeStart, hashRangeEnd);
            storedProcedureExecutor.Execute(deleteGroupToEntityMappingEventsStoredProcedureName, parameters);
        }

        /// <summary>
        /// Creates the parameters for one of the Delete*Events stored procedures.
        /// </summary>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to delete.</param>
        /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to delete.</param>
        /// <returns>The parameters as <see cref="SqlParameter">SqlParameters</see>.</returns>
        protected List<SqlParameter> CreateHashCodeRangeSqlParameters(Int32 hashRangeStart, Int32 hashRangeEnd)
        {
            var returnParameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(hashRangeStartParameterName, SqlDbType.Int, hashRangeStart),
                CreateSqlParameterWithValue(hashRangeEndParameterName, SqlDbType.Int, hashRangeEnd)
            };

            return returnParameters;
        }

        #endregion
    }
}
