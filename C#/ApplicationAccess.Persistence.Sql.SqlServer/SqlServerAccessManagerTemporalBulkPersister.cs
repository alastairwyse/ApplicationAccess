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
using System.Data;
using Microsoft.Data.SqlClient;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence.Sql.SqlServer
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> and see <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/> which persists access manager events in bulk to and allows reading of <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> objects from a Microsoft SQL Server database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class SqlServerAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> : SqlServerAccessManagerTemporalPersisterBase<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        // TODO:
        //   Move 'storedProcedureExecutor' member to base class

        #pragma warning disable 1591

        protected const String processEventsStoredProcedureName = "ProcessEvents";
        protected const String eventsParameterName = "@Events";

        // These values are used in the 'eventTypeColumn' column in the staging table
        protected const String userEventTypeValue = "user";
        protected const String groupEventTypeValue = "group";
        protected const String userToGroupMappingEventTypeValue = "userToGroupMapping";
        protected const String groupToGroupMappingEventTypeValue = "groupToGroupMapping";
        protected const String userToApplicationComponentAndAccessLevelMappingEventTypeValue = "userToApplicationComponentAndAccessLevelMapping";
        protected const String groupToApplicationComponentAndAccessLevelMappingEventTypeValue = "groupToApplicationComponentAndAccessLevelMapping";
        protected const String entityTypeEventTypeValue = "entityType";
        protected const String entityEventTypeValue = "entity";
        protected const String userToEntityMappingEventTypeValue = "userToEntityMapping";
        protected const String groupToEntityMappingEventTypeValue = "groupToEntityMapping";

        // These values are used in the 'eventActionColumn' column in the staging table
        protected const String addEventActionValue = "add";
        protected const String removeEventActionValue = "remove";

        protected const String idColumnName = "Id";
        protected const String eventTypeColumnName = "EventType";
        protected const String eventIdColumnName = "EventId";
        protected const String eventActionColumnName = "EventAction";
        protected const String occurredTimeColumnName = "OccurredTime";
        protected const String eventData1ColumnName = "EventData1";
        protected const String eventData2ColumnName = "EventData2";
        protected const String eventData3ColumnName = "EventData3";

        #pragma warning restore 1591

        /// <summary>Staging table which is populated with all events from a Flush() operation before passing to SQL Server as a <see href="https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters">table-valued parameter</see>.  As this table is used to pass all events in a single operation, it contains generic columns holding the event data, the content of which varies according to the type of the event.</summary>
        protected DataTable stagingTable;
        /// <summary>Column in the staging table which holds a sequential id for the event.</summary>
        protected DataColumn idColumn;
        /// <summary>Column in the staging table which holds the type of the event.</summary>
        protected DataColumn eventTypeColumn;
        /// <summary>Column in the staging table which holds a unique id for the event.</summary>
        protected DataColumn eventIdColumn;
        /// <summary>Column in the staging table which holds the action of the event.</summary>
        protected DataColumn eventActionColumn;
        /// <summary>Column in the staging table which holds time that the event originally occurred.</summary>
        protected DataColumn occurredTimeColumn;
        /// <summary>Column in the staging table which holds the first piece of data pertaining to the event.</summary>
        protected DataColumn eventData1Column;
        /// <summary>Column in the staging table which holds the second piece of data pertaining to the event.</summary>
        protected DataColumn eventData2Column;
        /// <summary>Column in the staging table which holds the third piece of data pertaining to the event.</summary>
        protected DataColumn eventData3Column;
        /// <summary>Maps types (subclasses of <see cref="TemporalEventBufferItemBase"/>) to actions which populate a row in the staging table with details of an event of that type.</summary>
        protected Dictionary<Type, Action<TemporalEventBufferItemBase, DataRow>> eventTypeToStagingTablePopulationOperationMap;
        /// <summary>Holds the value to put in the 'Id' column in the staging table.</summary>
        protected Int64 idColumnValue;
        /// <summary>Wraps calls to execute stored procedures so that they can be mocked in unit tests.</summary>
        protected IStoredProcedureExecutionWrapper storedProcedureExecutor;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerAccessManagerTemporalPersister class.
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
        public SqlServerAccessManagerTemporalBulkPersister
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
            stagingTable = new DataTable();
            idColumn = new DataColumn(idColumnName, typeof(Int64));
            eventTypeColumn = new DataColumn(eventTypeColumnName, typeof(String));
            eventIdColumn = new DataColumn(eventIdColumnName, typeof(Guid));
            eventActionColumn = new DataColumn(eventActionColumnName, typeof(String));
            occurredTimeColumn = new DataColumn(occurredTimeColumnName, typeof(DateTime));
            eventData1Column = new DataColumn(eventData1ColumnName, typeof(String));
            eventData2Column = new DataColumn(eventData2ColumnName, typeof(String));
            eventData3Column = new DataColumn(eventData3ColumnName, typeof(String));
            stagingTable.Columns.Add(idColumn);
            stagingTable.Columns.Add(eventTypeColumn);
            stagingTable.Columns.Add(eventIdColumn);
            stagingTable.Columns.Add(eventActionColumn);
            stagingTable.Columns.Add(occurredTimeColumn);
            stagingTable.Columns.Add(eventData1Column);
            stagingTable.Columns.Add(eventData2Column);
            stagingTable.Columns.Add(eventData3Column);
            eventTypeToStagingTablePopulationOperationMap = CreateEventTypeToStagingTablePopulationOperationMap();
            idColumnValue = 0;
            storedProcedureExecutor = new StoredProcedureExecutionWrapper((String procedureName, IEnumerable<SqlParameter> parameters) => { ExecuteStoredProcedure(procedureName, parameters); });
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerAccessManagerTemporalPersister class.
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
        public SqlServerAccessManagerTemporalBulkPersister
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
            : this(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
            base.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerAccessManagerTemporalPersister class.
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
        public SqlServerAccessManagerTemporalBulkPersister
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
            : this(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger, metricLogger)
        {
            this.storedProcedureExecutor = storedProcedureExecutor;
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            stagingTable.Rows.Clear();
            foreach (TemporalEventBufferItemBase currentEventBufferItem in events)
            {
                if (eventTypeToStagingTablePopulationOperationMap.ContainsKey(currentEventBufferItem.GetType()) == false)
                    throw new Exception($"Encountered unhandled event buffer item type '{currentEventBufferItem.GetType().Name}'.");

                Action<TemporalEventBufferItemBase, DataRow> stagingTablePopulationOperation = eventTypeToStagingTablePopulationOperationMap[currentEventBufferItem.GetType()];
                var row = stagingTable.NewRow();
                stagingTablePopulationOperation.Invoke(currentEventBufferItem, row);
                stagingTable.Rows.Add(row);
            }
            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(eventsParameterName, SqlDbType.Structured, stagingTable)
            };
            storedProcedureExecutor.Execute(processEventsStoredProcedureName, parameters);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns a dictionary mapping types (subclasses of <see cref="TemporalEventBufferItemBase"/>) to actions which populate a row in the staging table with details of an event of that type.
        /// </summary>
        /// <returns>A dictionary keyed by type, whose value is an action which accepts a subclass of <see cref="TemporalEventBufferItemBase"/> (having the same type as the key), and a <see cref="DataRow"/>, and which populates the row with details of the event.</returns>
        /// <remarks>Traditionally, the 'switch' statement in C# was preferred to multiple 'if / else' as apparently the compiler was able to use branch tables to more quickly move to a matching condition within the statement (instead of having to iterate on average 1/2 the cases each time with 'if / else').  However <see href="https://devblogs.microsoft.com/dotnet/new-features-in-c-7-0/#switch-statements-with-patterns">since C# 7 we're now able to use non-equality / range / pattern conditions within the 'switch' statement</see>.  I haven't been able to find any documentation as to whether this has had a negative impact on performance (although difficult to see how it cannot have), however to mitigate I'm putting all the processing routines for different <see cref="TemporalEventBufferItemBase"/> subclasses into a dictionary... hence the lookup speed should at least scale equivalently to the aforementioned branch tables.</remarks>
        protected Dictionary<Type, Action<TemporalEventBufferItemBase, DataRow>> CreateEventTypeToStagingTablePopulationOperationMap()
        {
            var returnDictionary = new Dictionary<Type, Action<TemporalEventBufferItemBase, DataRow>>()
            {
                { 
                    typeof(UserEventBufferItem<TUser>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) => 
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (UserEventBufferItem<TUser>)eventBufferItem;
                        row[eventTypeColumnName] = userEventTypeValue;
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        row[eventData1ColumnName] = userAsString;
                    }
                },
                {
                    typeof(GroupEventBufferItem<TGroup>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (GroupEventBufferItem<TGroup>)eventBufferItem;
                        row[eventTypeColumnName] = groupEventTypeValue;
                        String groupAsString = groupStringifier.ToString(typedEventBufferItem.Group);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        row[eventData1ColumnName] = groupAsString;
                    }
                },
                {
                    typeof(UserToGroupMappingEventBufferItem<TUser, TGroup>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (UserToGroupMappingEventBufferItem<TUser, TGroup>)eventBufferItem;
                        row[eventTypeColumnName] = userToGroupMappingEventTypeValue;
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        String groupAsString = groupStringifier.ToString(typedEventBufferItem.Group);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        row[eventData1ColumnName] = userAsString;
                        row[eventData2ColumnName] = groupAsString;
                    }
                },
                {
                    typeof(GroupToGroupMappingEventBufferItem<TGroup>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (GroupToGroupMappingEventBufferItem<TGroup>)eventBufferItem;
                        row[eventTypeColumnName] = groupToGroupMappingEventTypeValue;
                        String fromGroupAsString = groupStringifier.ToString(typedEventBufferItem.FromGroup);
                        String toGroupAsString = groupStringifier.ToString(typedEventBufferItem.ToGroup);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.FromGroup), fromGroupAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.ToGroup), toGroupAsString);
                        row[eventData1ColumnName] = fromGroupAsString;
                        row[eventData2ColumnName] = toGroupAsString;
                    }
                },
                {
                    typeof(UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>)eventBufferItem;
                        row[eventTypeColumnName] = userToApplicationComponentAndAccessLevelMappingEventTypeValue;
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        String applicationComponentAsString = applicationComponentStringifier.ToString(typedEventBufferItem.ApplicationComponent);
                        String accessLevelAsString = accessLevelStringifier.ToString(typedEventBufferItem.AccessLevel);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.ApplicationComponent), applicationComponentAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.AccessLevel), accessLevelAsString);
                        row[eventData1ColumnName] = userAsString;
                        row[eventData2ColumnName] = applicationComponentAsString;
                        row[eventData3ColumnName] = accessLevelAsString;
                    }
                },
                {
                    typeof(GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>)eventBufferItem;
                        row[eventTypeColumnName] = groupToApplicationComponentAndAccessLevelMappingEventTypeValue;
                        String groupAsString = groupStringifier.ToString(typedEventBufferItem.Group);
                        String applicationComponentAsString = applicationComponentStringifier.ToString(typedEventBufferItem.ApplicationComponent);
                        String accessLevelAsString = accessLevelStringifier.ToString(typedEventBufferItem.AccessLevel);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.ApplicationComponent), applicationComponentAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.AccessLevel), accessLevelAsString);
                        row[eventData1ColumnName] = groupAsString;
                        row[eventData2ColumnName] = applicationComponentAsString;
                        row[eventData3ColumnName] = accessLevelAsString;
                    }
                },
                {
                    typeof(EntityTypeEventBufferItem), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (EntityTypeEventBufferItem)eventBufferItem;
                        row[eventTypeColumnName] = entityTypeEventTypeValue;
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        row[eventData1ColumnName] = typedEventBufferItem.EntityType;
                    }
                },
                {
                    typeof(EntityEventBufferItem), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (EntityEventBufferItem)eventBufferItem;
                        row[eventTypeColumnName] = entityEventTypeValue;
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Entity), typedEventBufferItem.Entity);
                        row[eventData1ColumnName] = typedEventBufferItem.EntityType;
                        row[eventData2ColumnName] = typedEventBufferItem.Entity;
                    }
                },
                {
                    typeof(UserToEntityMappingEventBufferItem<TUser>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (UserToEntityMappingEventBufferItem<TUser>)eventBufferItem;
                        row[eventTypeColumnName] = userToEntityMappingEventTypeValue;
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Entity), typedEventBufferItem.Entity);
                        row[eventData1ColumnName] = userAsString;
                        row[eventData2ColumnName] = typedEventBufferItem.EntityType;
                        row[eventData3ColumnName] = typedEventBufferItem.Entity;
                    }
                },
                {
                    typeof(GroupToEntityMappingEventBufferItem<TGroup>), (TemporalEventBufferItemBase eventBufferItem, DataRow row) =>
                    {
                        PopulateDataRowWithTemporalEventBufferItemBaseProperties(eventBufferItem, row);
                        var typedEventBufferItem = (GroupToEntityMappingEventBufferItem<TGroup>)eventBufferItem;
                        row[eventTypeColumnName] = groupToEntityMappingEventTypeValue;
                        String groupAsString = groupStringifier.ToString(typedEventBufferItem.Group);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Entity), typedEventBufferItem.Entity);
                        row[eventData1ColumnName] = groupAsString;
                        row[eventData2ColumnName] = typedEventBufferItem.EntityType;
                        row[eventData3ColumnName] = typedEventBufferItem.Entity;
                    }
                }
            };

            return returnDictionary;
        }

        /// <summary>
        /// Populates a row of the staging table with base/common properties of the specified event buffer item.
        /// </summary>
        /// <param name="eventBufferItem">The event buffer item.</param>
        /// <param name="row">The row of the staging table to populate.</param>
        protected void PopulateDataRowWithTemporalEventBufferItemBaseProperties(TemporalEventBufferItemBase eventBufferItem, DataRow row)
        {
            row[idColumnName] = idColumnValue;
            idColumnValue++;
            row[eventIdColumnName] = eventBufferItem.EventId;
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                row[eventActionColumnName] = addEventActionValue;
            }
            else
            {
                row[eventActionColumnName] = removeEventActionValue;
            }
            row[occurredTimeColumnName] = eventBufferItem.OccurredTime;
        }

        #pragma warning disable 1591

        protected void ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(string propertyName, string stringifiedPropertyValue)
        {
            if (stringifiedPropertyValue.Length > columnSizeLimit)
                throw new ArgumentOutOfRangeException(propertyName, $"Event property '{propertyName}' with stringified value '{stringifiedPropertyValue}' is longer than the maximum allowable column size of {columnSizeLimit}.");
        }

        #pragma warning restore 1591

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the SqlServerAccessManagerTemporalBulkPersister.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~SqlServerAccessManagerTemporalBulkPersister()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    idColumn.Dispose();
                    eventTypeColumn.Dispose();
                    eventIdColumn.Dispose();
                    eventActionColumn.Dispose();
                    occurredTimeColumn.Dispose();
                    eventData1Column.Dispose();
                    eventData2Column.Dispose();
                    eventData3Column.Dispose();
                    stagingTable.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion

        #region Inner Classes

        /// <summary>
        /// Implementation of <see cref="IStoredProcedureExecutionWrapper"/> which allows executing stored procedures through a configurable <see cref="Action"/>.
        /// </summary>
        protected class StoredProcedureExecutionWrapper : IStoredProcedureExecutionWrapper
        {
            /// <summary>The action which executes the stored procedures.</summary>
            protected Action<String, IEnumerable<SqlParameter>> executeAction;

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerAccessManagerTemporalBulkPersister+StoredProcedureExecutionWrapper class.
            /// </summary>
            /// <param name="executeAction">The action which executes the stored procedures.</param>
            public StoredProcedureExecutionWrapper(Action<String, IEnumerable<SqlParameter>> executeAction)
            {
                this.executeAction = executeAction;
            }

            /// <summary>
            /// Executes a stored procedure which does not return a result set.
            /// </summary>
            /// <param name="procedureName">The name of the stored procedure.</param>
            /// <param name="parameters">The parameters to pass to the stored procedure.</param>
            public void Execute(String procedureName, IEnumerable<SqlParameter> parameters)
            {
                executeAction.Invoke(procedureName, parameters);
            }
        }

        #endregion
    }
}
