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
using System.Text;
using System.Text.Json;
using System.IO;
using ApplicationAccess.Persistence.Sql;
using ApplicationLogging;
using ApplicationMetrics;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Security.Cryptography;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence.Sql.PostgreSql
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> and <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/> which persists access manager events in bulk to and allows reading of <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> objects from a PostgreSQL database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class PostgreSqlAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        #pragma warning disable 1591

        protected const String processEventsStoredProcedureName = "ProcessEvents";

        // These values are used in the 'Type' property in the 'ProcessEvents' stored procedure JSON parameter
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

        // These values are used in the 'Action' property in the 'ProcessEvents' stored procedure JSON parameter
        protected const String addEventActionValue = "add";
        protected const String removeEventActionValue = "remove";

        protected const String typePropertyName = "Type";
        protected const String idPropertyName = "Id";
        protected const String actionPropertyName = "Action";
        protected const String occurredTimePropertyName = "OccurredTime";
        protected const String data1PropertyName = "Data1";
        protected const String data2PropertyName = "Data2";
        protected const String data3PropertyName = "Data3";

        #pragma warning restore 1591

        /// <summary>The maximum size of text columns in the database (restricted by limits on the sizes of index keys... see https://docs.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver16).</summary>
        protected const Int32 columnSizeLimit = 450;
        /// <summary>DateTime format string which can be interpreted by the <see href="https://www.postgresql.org/docs/8.1/functions-formatting.html">PostgreSQL to_timestamp() function</see>.</summary>
        protected const String postgreSQLTimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        /// <summary>The string to use to connect to the PostgreSQL database.</summary>
        protected String connectionString;
        /// <summary>The time in seconds to wait while trying to execute a command, before terminating the attempt and generating an error. Set to zero for infinity.</summary>
        protected Int32 commandTimeout;
        /// <summary>The datasource to use to create connections to PostgreSQL.</summary>
        protected NpgsqlDataSource dataSource;
        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary>Used to execute queries and store procedures against PostgreSQL.</summary>
        protected PostgreSqlPersisterUtilities<TUser, TGroup, TComponent, TAccess> persisterUtilities;
        /// <summary>Maps types (subclasses of <see cref="TemporalEventBufferItemBase"/>) to actions which populate a JSON array element with details of an event of that type.</summary>
        protected Dictionary<Type, Action<TemporalEventBufferItemBase, Utf8JsonWriter>> eventTypeToJsonDocumentPopulationOperationMap;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Wraps calls to execute stored procedures so that they can be mocked in unit tests.</summary>
        protected IStoredProcedureExecutionWrapper storedProcedureExecutor;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

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
            PostgreSqlPersisterUtilities<TUser, TGroup, TComponent, TAccess>.ThrowExceptionIfCommandTimeoutParameterLessThanZero(nameof(commandTimeout), commandTimeout);

            this.connectionString = connectionString;
            this.commandTimeout = commandTimeout;
            NpgsqlDataSourceBuilder dataSourceBuilder;
            try
            {
                dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                dataSource = dataSourceBuilder.Build();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create {typeof(NpgsqlDataSource).Name} from connection string '{connectionString}'.", e);
            }
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            this.persisterUtilities = new PostgreSqlPersisterUtilities<TUser, TGroup, TComponent, TAccess>
            (
                dataSource, 
                commandTimeout, 
                userStringifier, 
                groupStringifier, 
                applicationComponentStringifier, 
                accessLevelStringifier
            );
            this.eventTypeToJsonDocumentPopulationOperationMap = CreateEventTypeToJsonDocumentPopulationOperationMap();
            this.logger = logger;
            this.metricLogger = new NullMetricLogger();
            this.storedProcedureExecutor = new StoredProcedureExecutionWrapper((String procedureName, IList<NpgsqlParameter> parameters) => { ExecuteStoredProcedure(procedureName, parameters); });
            disposed = false;
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
        /// <param name="storedProcedureExecutor">A test (mock) <see cref="IStoredProcedureExecutionWrapper"/> object.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public PostgreSqlAccessManagerTemporalBulkPersister
        (
            String connectionString,
            Int32 commandTimeout,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger,
            IStoredProcedureExecutionWrapper storedProcedureExecutor
        ) : this(connectionString, commandTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger, metricLogger)
        {
            this.storedProcedureExecutor = storedProcedureExecutor;
        }

        /// <inheritdoc/>
        public AccessManagerState Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return persisterUtilities.Load(DateTime.UtcNow, accessManagerToLoadTo, new PersistentStorageEmptyException("The database does not contain any existing events nor data."));
        }

        /// <inheritdoc/>
        public AccessManagerState Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return persisterUtilities.Load(eventId, accessManagerToLoadTo);
        }

        /// <inheritdoc/>
        public AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return persisterUtilities.Load(stateTime, accessManagerToLoadTo, new ArgumentException($"No EventIdToTransactionTimeMap rows were returned with TransactionTime less than or equal to '{stateTime.ToString(postgreSQLTimestampFormat)}'.", nameof(stateTime)));
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            PersistEvents(events, false);
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events, Boolean ignorePreExistingEvents)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new Utf8JsonWriter(memoryStream))
            {
                writer.WriteStartArray();
                foreach (TemporalEventBufferItemBase currentEventBufferItem in events)
                {
                    if (eventTypeToJsonDocumentPopulationOperationMap.ContainsKey(currentEventBufferItem.GetType()) == false)
                        throw new Exception($"Encountered unhandled event buffer item type '{currentEventBufferItem.GetType().Name}'.");

                    writer.WriteStartObject();
                    Action<TemporalEventBufferItemBase, Utf8JsonWriter> jsonDocumentPopulationOperation = eventTypeToJsonDocumentPopulationOperationMap[currentEventBufferItem.GetType()];
                    jsonDocumentPopulationOperation(currentEventBufferItem, writer);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.Flush();
                memoryStream.Position = 0;
                using (var eventsJson = JsonDocument.Parse(memoryStream))
                {
                    var parameters = new List<NpgsqlParameter>()
                    {
                        CreateNpgsqlParameterWithValue(NpgsqlDbType.Json, eventsJson),
                        CreateNpgsqlParameterWithValue(NpgsqlDbType.Boolean, ignorePreExistingEvents)
                    };
                    storedProcedureExecutor.Execute(processEventsStoredProcedureName, parameters);
                }
            }
        }

        #region Private/Protected Methods

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
        /// Returns a dictionary mapping types (subclasses of <see cref="TemporalEventBufferItemBase"/>) to actions which populate a JSON array element with details of an event of that type.
        /// </summary>
        /// <returns>A dictionary keyed by type, whose value is an action which accepts a subclass of <see cref="TemporalEventBufferItemBase"/> (having the same type as the key), and a <see cref="Utf8JsonWriter"/>, and which populates a JSON array element with details of the event.</returns>
        /// <remarks>Traditionally, the 'switch' statement in C# was preferred to multiple 'if / else' as apparently the compiler was able to use branch tables to more quickly move to a matching condition within the statement (instead of having to iterate on average 1/2 the cases each time with 'if / else').  However <see href="https://devblogs.microsoft.com/dotnet/new-features-in-c-7-0/#switch-statements-with-patterns">since C# 7 we're now able to use non-equality / range / pattern conditions within the 'switch' statement</see>.  I haven't been able to find any documentation as to whether this has had a negative impact on performance (although difficult to see how it cannot have), however to mitigate I'm putting all the processing routines for different <see cref="TemporalEventBufferItemBase"/> subclasses into a dictionary... hence the lookup speed should at least scale equivalently to the aforementioned branch tables.</remarks>
        protected Dictionary<Type, Action<TemporalEventBufferItemBase, Utf8JsonWriter>> CreateEventTypeToJsonDocumentPopulationOperationMap()
        {
            var returnDictionary = new Dictionary<Type, Action<TemporalEventBufferItemBase, Utf8JsonWriter>>()
            {
                {
                    typeof(UserEventBufferItem<TUser>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (UserEventBufferItem<TUser>)eventBufferItem;
                        writer.WriteString(typePropertyName, userEventTypeValue);
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        writer.WriteString(data1PropertyName, userAsString);
                    }
                },
                {
                    typeof(GroupEventBufferItem<TGroup>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (GroupEventBufferItem<TGroup>)eventBufferItem;
                        writer.WriteString(typePropertyName, groupEventTypeValue);
                        String groupAsString =  groupStringifier.ToString(typedEventBufferItem.Group);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        writer.WriteString(data1PropertyName, groupAsString);
                    }
                },
                {
                    typeof(UserToGroupMappingEventBufferItem<TUser, TGroup>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (UserToGroupMappingEventBufferItem<TUser, TGroup>)eventBufferItem;
                        writer.WriteString(typePropertyName, userToGroupMappingEventTypeValue);
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        String groupAsString = groupStringifier.ToString(typedEventBufferItem.Group);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        writer.WriteString(data1PropertyName, userAsString);
                        writer.WriteString(data2PropertyName, groupAsString);
                    }
                },
                {
                    typeof(GroupToGroupMappingEventBufferItem<TGroup>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (GroupToGroupMappingEventBufferItem<TGroup>)eventBufferItem;
                        writer.WriteString(typePropertyName, groupToGroupMappingEventTypeValue);
                        String fromGroupAsString = groupStringifier.ToString(typedEventBufferItem.FromGroup);
                        String toGroupAsString = groupStringifier.ToString(typedEventBufferItem.ToGroup);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.FromGroup), fromGroupAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.ToGroup), toGroupAsString);
                        writer.WriteString(data1PropertyName, fromGroupAsString);
                        writer.WriteString(data2PropertyName, toGroupAsString);
                    }
                },
                {
                    typeof(UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>)eventBufferItem;
                        writer.WriteString(typePropertyName, userToApplicationComponentAndAccessLevelMappingEventTypeValue);
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        String applicationComponentAsString = applicationComponentStringifier.ToString(typedEventBufferItem.ApplicationComponent);
                        String accessLevelAsString = accessLevelStringifier.ToString(typedEventBufferItem.AccessLevel);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.ApplicationComponent), applicationComponentAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.AccessLevel), accessLevelAsString);
                        writer.WriteString(data1PropertyName, userAsString);
                        writer.WriteString(data2PropertyName, applicationComponentAsString);
                        writer.WriteString(data3PropertyName, accessLevelAsString);
                    }
                },
                {
                    typeof(GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>)eventBufferItem;
                        writer.WriteString(typePropertyName, groupToApplicationComponentAndAccessLevelMappingEventTypeValue);
                        String groupAsString = groupStringifier.ToString(typedEventBufferItem.Group);
                        String applicationComponentAsString = applicationComponentStringifier.ToString(typedEventBufferItem.ApplicationComponent);
                        String accessLevelAsString = accessLevelStringifier.ToString(typedEventBufferItem.AccessLevel);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.ApplicationComponent), applicationComponentAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.AccessLevel), accessLevelAsString);
                        writer.WriteString(data1PropertyName, groupAsString);
                        writer.WriteString(data2PropertyName, applicationComponentAsString);
                        writer.WriteString(data3PropertyName, accessLevelAsString);
                    }
                },
                {
                    typeof(EntityTypeEventBufferItem), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (EntityTypeEventBufferItem)eventBufferItem;
                        writer.WriteString(typePropertyName, entityTypeEventTypeValue);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        writer.WriteString(data1PropertyName, typedEventBufferItem.EntityType);
                    }
                },
                {
                    typeof(EntityEventBufferItem), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (EntityEventBufferItem)eventBufferItem;
                        writer.WriteString(typePropertyName, entityEventTypeValue);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Entity), typedEventBufferItem.Entity);
                        writer.WriteString(data1PropertyName, typedEventBufferItem.EntityType);
                        writer.WriteString(data2PropertyName, typedEventBufferItem.Entity);
                    }
                },
                {
                    typeof(UserToEntityMappingEventBufferItem<TUser>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (UserToEntityMappingEventBufferItem<TUser>)eventBufferItem;
                        writer.WriteString(typePropertyName, userToEntityMappingEventTypeValue);
                        String userAsString = userStringifier.ToString(typedEventBufferItem.User);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.User), userAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Entity), typedEventBufferItem.Entity);
                        writer.WriteString(data1PropertyName, userAsString);
                        writer.WriteString(data2PropertyName, typedEventBufferItem.EntityType);
                        writer.WriteString(data3PropertyName, typedEventBufferItem.Entity);
                    }
                },
                {
                    typeof(GroupToEntityMappingEventBufferItem<TGroup>), (TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer) =>
                    {
                        PopulateJsonElementWithTemporalEventBufferItemBaseProperties(eventBufferItem, writer);
                        var typedEventBufferItem = (GroupToEntityMappingEventBufferItem<TGroup>)eventBufferItem;
                        writer.WriteString(typePropertyName, groupToEntityMappingEventTypeValue);
                        String groupAsString = groupStringifier.ToString(typedEventBufferItem.Group);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Group), groupAsString);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.EntityType), typedEventBufferItem.EntityType);
                        ThrowExceptionIfStringifiedEventPropertyLargerThanVarCharLimit(nameof(typedEventBufferItem.Entity), typedEventBufferItem.Entity);
                        writer.WriteString(data1PropertyName, groupAsString);
                        writer.WriteString(data2PropertyName, typedEventBufferItem.EntityType);
                        writer.WriteString(data3PropertyName, typedEventBufferItem.Entity);
                    }
                }
            };

            return returnDictionary;
        }

        /// <summary>
        /// Populates a JSON array element with base/common properties of the specified event buffer item.
        /// </summary>
        /// <param name="eventBufferItem">The event buffer item.</param>
        /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write the properties to.</param>
        protected void PopulateJsonElementWithTemporalEventBufferItemBaseProperties(TemporalEventBufferItemBase eventBufferItem, Utf8JsonWriter writer)
        {
            writer.WriteString(idPropertyName, eventBufferItem.EventId);
            if (eventBufferItem.EventAction == EventAction.Add)
            {
                writer.WriteString(actionPropertyName, addEventActionValue);
            }
            else
            {
                writer.WriteString(actionPropertyName, removeEventActionValue);
            }
            writer.WriteString(occurredTimePropertyName, eventBufferItem.OccurredTime.ToString(postgreSQLTimestampFormat));
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
        /// Releases the unmanaged resources used by the PostgreSqlAccessManagerTemporalBulkPersister.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~PostgreSqlAccessManagerTemporalBulkPersister()
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
                    dataSource.Dispose();
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
            protected Action<String, IList<NpgsqlParameter>> executeAction;

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.PostgreSql.PostgreSqlAccessManagerTemporalBulkPersister+StoredProcedureExecutionWrapper class.
            /// </summary>
            /// <param name="executeAction">The action which executes the stored procedures.</param>
            public StoredProcedureExecutionWrapper(Action<String, IList<NpgsqlParameter>> executeAction)
            {
                this.executeAction = executeAction;
            }

            /// <summary>
            /// Executes a stored procedure which does not return a result set.
            /// </summary>
            /// <param name="procedureName">The name of the stored procedure.</param>
            /// <param name="parameters">The parameters to pass to the stored procedure.</param>
            public void Execute(String procedureName, IList<NpgsqlParameter> parameters)
            {
                executeAction.Invoke(procedureName, parameters);
            }
        }

        #endregion
    }
}
