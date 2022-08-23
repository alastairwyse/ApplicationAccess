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
using System.Globalization;
using Microsoft.Data.SqlClient;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence.SqlServer
{
    /// <summary>
    /// An implementation of ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister which persists access manager events to a Microsoft SQL Server database.
    /// </summary>
    /// <typeparam name="TUser">The validator to use to validate events.</typeparam>
    /// <typeparam name="TGroup">The strategy to use for flushing the buffers.</typeparam>
    /// <typeparam name="TComponent">The persister to use to write flushed events to permanent storage.</typeparam>
    /// <typeparam name="TAccess">The sequence number used for the last event buffered.</typeparam>
    /// <remarks>Note that <see cref="IAccessManagerEventProcessor&lt;TUser, TGroup, TComponent, TAccess&gt;">IAccessManagerEventProcessor</see> methods implemented in this class should not be called from concurrent threads.  The class is designed to operate behind a class which manages mutual exclusion such as the <see cref="InMemoryEventBuffer&lt;TUser, TGroup, TComponent, TAccess&gt;">InMemoryEventBuffer</see> or <see cref="ApplicationAccess.Validation.ConcurrentAccessManagerEventValidator&lt;TUser, TGroup, TComponent, TAccess&gt;">ConcurrentAccessManagerEventValidator</see> classes.</remarks>
    public class SqlServerAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>
    {
        // TODO:
        //   Change Load() methods to return a Tuple<Guid, DateTime>
        //     Query will likely need to use an order by and top... run this against SQL server query profiler to make sure it doesn't table/index scan

        //   Interval metrics on each of the public methods
        //     We alread have AccessManagerEventProcessorMetricLogger... can that be reused/derived from??
        //     Timings/interval metrics would probably be nice, but do we need counts??  InMemoryEventBuffer already does counts to all event calls it receives... should be able to assume they will all be persisted at some point afterwards.
        //     Start with just timings, and it should only be on the event overloads which include statetime... the ones without will be covered in any case
        //       And also timings and counts on the load methods (again can probably just put in the 'lowest level' Load() overload)
        //   Any other unit tests I can add??



        #pragma warning disable 1591

        protected const String addUserStoredProcedureName = "AddUser";
        protected const String removeUserStoredProcedureName = "RemoveUser";
        protected const String addGroupStoredProcedureName = "AddGroup";
        protected const String removeGroupStoredProcedureName = "RemoveGroup";
        protected const String addUserToGroupMappingStoredProcedureName = "AddUserToGroupMapping";
        protected const String removeUserToGroupMappingStoredProcedureName = "RemoveUserToGroupMapping";
        protected const String addGroupToGroupMappingProcedureName = "AddGroupToGroupMapping";
        protected const String removeGroupToGroupMappingProcedureName = "RemoveGroupToGroupMapping";
        protected const String addUserToApplicationComponentAndAccessLevelMappingProcedureName = "AddUserToApplicationComponentAndAccessLevelMapping";
        protected const String removeUserToApplicationComponentAndAccessLevelMappingProcedureName = "RemoveUserToApplicationComponentAndAccessLevelMapping";
        protected const String addGroupToApplicationComponentAndAccessLevelMappingProcedureName = "AddGroupToApplicationComponentAndAccessLevelMapping";
        protected const String removeGroupToApplicationComponentAndAccessLevelMappingProcedureName = "RemoveGroupToApplicationComponentAndAccessLevelMapping";
        protected const String addEntityTypeProcedureName = "AddEntityType";
        protected const String removeEntityTypeProcedureName = "RemoveEntityType";
        protected const String addEntityProcedureName = "AddEntity";
        protected const String removeEntityProcedureName = "RemoveEntity";
        protected const String addUserToEntityMappingProcedureName = "AddUserToEntityMapping";
        protected const String removeUserToEntityMappingProcedureName = "RemoveUserToEntityMapping";
        protected const String addGroupToEntityMappingProcedureName = "AddGroupToEntityMapping";
        protected const String removeGroupToEntityMappingProcedureName = "RemoveGroupToEntityMapping";

        protected const String userParameterName = "@User";
        protected const String groupParameterName = "@Group";
        protected const String fromGroupParameterName = "@FromGroup";
        protected const String toGroupParameterName = "@ToGroup";
        protected const String applicationComponentParameterName = "@ApplicationComponent";
        protected const String accessLevelParameterName = "@AccessLevel";
        protected const String entityTypeParameterName = "@EntityType";
        protected const String entityParameterName = "@Entity";
        protected const String eventIdParameterName = "@EventId";
        protected const String transactionTimeParameterName = "@TransactionTime";

        #pragma warning restore 1591

        /// <summary>The maximum size of text columns in the database (restricted by limits on the sizes of index keys... see https://docs.microsoft.com/en-us/sql/sql-server/maximum-capacity-specifications-for-sql-server?view=sql-server-ver16).</summary>
        protected const Int32 columnSizeLimit = 450;
        /// <summary>DateTime format string which matches the <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver16#date-and-time-styles">Transact-SQL 126 date and time style</see>.</summary>
        protected const String transactionSql126DateStyle = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <summary>The string to use to connect to the SQL Server database.</summary>
        protected string connectionString;
        /// <summary>The number of times an operation against the SQL Server database should be retried in the case of execution failure.</summary>
        protected Int32 retryCount;
        /// <summary>The time in seconds between operation retries.</summary>
        protected Int32 retryInterval;
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
        /// <summary>The retry logic to use when connecting to and executing against the SQL Server database.</summary>
        protected SqlRetryLogicOption sqlRetryLogicOption;
        /// <summary>A set of SQL Server database engine error numbers which denote a transient fault.</summary>
        /// <see href="https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors?view=sql-server-ver16"/>
        /// <see href="https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues?view=azuresql"/>
        protected List<Int32> sqlServerTransientErrorNumbers;
        /// <summary>The action to invoke if an action is retried due to a transient error.</summary>
        protected EventHandler<SqlRetryingEventArgs> connectionRetryAction;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SqlServer.SqlServerAccessManagerTemporalEventPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        public SqlServerAccessManagerTemporalEventPersister
        (
            string connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger
        )
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{nameof(connectionString)}' must contain a value.", nameof(connectionString));
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be less than 0.");
            if (retryCount > 59)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be greater than 59.");
            if (retryInterval < 0)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be less than 0.");
            if (retryInterval > 120)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be greater than 120.");

            this.connectionString = connectionString;
            this.retryCount = retryCount;
            this.retryInterval = retryInterval;
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
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
                if (typeof(SqlException).IsAssignableFrom(lastException.GetType()) == true)
                {
                    var se = (SqlException)lastException;
                    logger.Log(this, LogLevel.Warning, $"SQL Server error with number {se.Number} occurred when executing command.  Retrying in {retryInterval} seconds (retry {eventArgs.RetryCount} of {retryCount}).", se);
                }
                else
                {
                    logger.Log(this, LogLevel.Warning, $"Exception occurred when executing command.  Retrying in {retryInterval} seconds (retry {eventArgs.RetryCount} of {retryCount}).", lastException);
                }
                metricLogger.Increment(new SqlCommandExecutionsRetried());
            };
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SqlServer.SqlServerAccessManagerTemporalEventPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public SqlServerAccessManagerTemporalEventPersister
        (
            string connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(connectionString, retryCount, retryInterval, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
            this.metricLogger = metricLogger;
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public void AddUser(TUser user)
        {
            AddUser(user, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public void RemoveUser(TUser user)
        {
            RemoveUser(user, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public void AddGroup(TGroup group)
        {
            AddGroup(group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public void RemoveGroup(TGroup group)
        {
            RemoveGroup(group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            AddUserToGroupMapping(user, group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            RemoveUserToGroupMapping(user, group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            AddGroupToGroupMapping(fromGroup, toGroup, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            RemoveGroupToGroupMapping(fromGroup, toGroup, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public void AddEntityType(String entityType)
        {
            AddEntityType(entityType, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public void RemoveEntityType(String entityType)
        {
            RemoveEntityType(entityType, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public void AddEntity(String entityType, String entity)
        {
            AddEntity(entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public void RemoveEntity(String entityType, String entity)
        {
            RemoveEntity(entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            AddUserToEntityMapping(user, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            RemoveUserToEntityMapping(user, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            AddGroupToEntityMapping(group, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            RemoveGroupToEntityMapping(group, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserStoredProcedure(addUserStoredProcedureName, user, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserStoredProcedure(removeUserStoredProcedureName, user, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupStoredProcedure(addGroupStoredProcedureName, group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupStoredProcedure(removeGroupStoredProcedureName, group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToGroupMappingStoredProcedure(addUserToGroupMappingStoredProcedureName, user, group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToGroupMappingStoredProcedure(removeUserToGroupMappingStoredProcedureName, user, group, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToGroupMappingStoredProcedure(addGroupToGroupMappingProcedureName, fromGroup, toGroup, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToGroupMappingStoredProcedure(removeGroupToGroupMappingProcedureName, fromGroup, toGroup, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure(addUserToApplicationComponentAndAccessLevelMappingProcedureName, user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure(removeUserToApplicationComponentAndAccessLevelMappingProcedureName, user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure(addGroupToApplicationComponentAndAccessLevelMappingProcedureName, group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure(removeGroupToApplicationComponentAndAccessLevelMappingProcedureName, group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityTypeStoredProcedure(addEntityTypeProcedureName, entityType, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityTypeStoredProcedure(removeEntityTypeProcedureName, entityType, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityStoredProcedure(addEntityProcedureName, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityStoredProcedure(removeEntityProcedureName, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToEntityMappingStoredProcedure(addUserToEntityMappingProcedureName, user, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToEntityMappingStoredProcedure(removeUserToEntityMappingProcedureName, user, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToEntityMappingStoredProcedure(addGroupToEntityMappingProcedureName, group, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToEntityMappingStoredProcedure(removeGroupToEntityMappingProcedureName, group, entityType, entity, eventId, occurredTime);
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerEventPersister`4.Load(ApplicationAccess.AccessManager{`0,`1,`2,`3})"]/*'/>
        public Tuple<Guid, DateTime> Load(AccessManager<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return Load(DateTime.UtcNow, accessManagerToLoadTo);
        }

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.Load(System.Guid,ApplicationAccess.AccessManager{`0,`1,`2,`3})"]/*'/>
        public Tuple<Guid, DateTime> Load(Guid eventId, AccessManager<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
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

        /// <include file='..\ApplicationAccess.Persistence\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.Load(System.DateTime,ApplicationAccess.AccessManager{`0,`1,`2,`3})"]/*'/>
        public Tuple<Guid, DateTime> Load(DateTime stateTime, AccessManager<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
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
                throw new ArgumentException($"No EventIdToTransactionTimeMap rows were returned with TransactionTime less than or equal to '{stateTime.ToString(transactionSql126DateStyle)}'.", nameof(stateTime));

            LoadToAccessManager(stateTime, accessManagerToLoadTo);

            return new Tuple<Guid, DateTime>(eventId, transactionTime);
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

        #pragma warning disable 1573

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a user.
        /// </summary>
        protected void SetupAndExecuteUserStoredProcedure(string storedProcedureName, TUser user, Guid eventId, DateTime occurredTime)
        {
            String userAsString = userStringifier.ToString(user);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(user), userAsString);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(userParameterName, SqlDbType.NVarChar, userAsString),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName,  SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a group.
        /// </summary>
        protected void SetupAndExecuteGroupStoredProcedure(string storedProcedureName, TGroup group, Guid eventId, DateTime occurredTime)
        {
            String groupAsString = groupStringifier.ToString(group);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(group), groupAsString);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(groupParameterName, SqlDbType.NVarChar, groupAsString),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a user to group mapping.
        /// </summary>
        protected void SetupAndExecuteUserToGroupMappingStoredProcedure(string storedProcedureName, TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            String userAsString = userStringifier.ToString(user);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(user), userAsString);
            String groupAsString = groupStringifier.ToString(group);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(group), groupAsString);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(userParameterName, SqlDbType.NVarChar, userAsString),
                CreateSqlParameterWithValue(groupParameterName, SqlDbType.NVarChar, groupAsString),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a group to group mapping.
        /// </summary>
        protected void SetupAndExecuteGroupToGroupMappingStoredProcedure(string storedProcedureName, TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            String fromGroupAsString = groupStringifier.ToString(fromGroup);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(fromGroup), fromGroupAsString);
            String toGroupAsString = groupStringifier.ToString(toGroup);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(toGroup), toGroupAsString);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(fromGroupParameterName, SqlDbType.NVarChar, fromGroupAsString),
                CreateSqlParameterWithValue(toGroupParameterName, SqlDbType.NVarChar, toGroupAsString),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a user to application component and access level mapping.
        /// </summary>
        protected void SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure(string storedProcedureName, TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            String userAsString = userStringifier.ToString(user);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(user), userAsString);
            String applicationComponentAsString = applicationComponentStringifier.ToString(applicationComponent);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(applicationComponent), applicationComponentAsString);
            String accessLevelAsString = accessLevelStringifier.ToString(accessLevel);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(accessLevel), accessLevelAsString);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(userParameterName, SqlDbType.NVarChar, userAsString),
                CreateSqlParameterWithValue(applicationComponentParameterName, SqlDbType.NVarChar, applicationComponentAsString),
                CreateSqlParameterWithValue(accessLevelParameterName, SqlDbType.NVarChar, accessLevelAsString),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a group to application component and access level mapping.
        /// </summary>
        protected void SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure(string storedProcedureName, TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            String groupAsString = groupStringifier.ToString(group);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(group), groupAsString);
            String applicationComponentAsString = applicationComponentStringifier.ToString(applicationComponent);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(applicationComponent), applicationComponentAsString);
            String accessLevelAsString = accessLevelStringifier.ToString(accessLevel);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(accessLevel), accessLevelAsString);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(groupParameterName, SqlDbType.NVarChar, groupAsString),
                CreateSqlParameterWithValue(applicationComponentParameterName, SqlDbType.NVarChar, applicationComponentAsString),
                CreateSqlParameterWithValue(accessLevelParameterName, SqlDbType.NVarChar, accessLevelAsString),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove an entity type.
        /// </summary>
        protected void SetupAndExecuteEntityTypeStoredProcedure(string storedProcedureName, String entityType, Guid eventId, DateTime occurredTime)
        {
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(entityType), entityType);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(entityTypeParameterName, SqlDbType.NVarChar, entityType),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove an entity.
        /// </summary>
        protected void SetupAndExecuteEntityStoredProcedure(string storedProcedureName, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(entityType), entityType);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(entity), entity);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(entityTypeParameterName, SqlDbType.NVarChar, entityType),
                CreateSqlParameterWithValue(entityParameterName, SqlDbType.NVarChar, entity),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a user to entity mapping.
        /// </summary>
        protected void SetupAndExecuteUserToEntityMappingStoredProcedure(string storedProcedureName, TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            String userAsString = userStringifier.ToString(user);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(user), userAsString);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(entityType), entityType);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(entity), entity);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(userParameterName, SqlDbType.NVarChar, userAsString),
                CreateSqlParameterWithValue(entityTypeParameterName, SqlDbType.NVarChar, entityType),
                CreateSqlParameterWithValue(entityParameterName, SqlDbType.NVarChar, entity),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

        /// <summary>
        /// Sets up parameters on and executes a stored procedure to add or remove a group to entity mapping.
        /// </summary>
        protected void SetupAndExecuteGroupToEntityMappingStoredProcedure(string storedProcedureName, TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            String groupAsString = groupStringifier.ToString(group);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(group), groupAsString);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(entityType), entityType);
            ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(nameof(entity), entity);

            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(groupParameterName, SqlDbType.NVarChar, groupAsString),
                CreateSqlParameterWithValue(entityTypeParameterName, SqlDbType.NVarChar, entityType),
                CreateSqlParameterWithValue(entityParameterName, SqlDbType.NVarChar, entity),
                CreateSqlParameterWithValue(eventIdParameterName, SqlDbType.UniqueIdentifier, eventId),
                CreateSqlParameterWithValue(transactionTimeParameterName, SqlDbType.DateTime2, occurredTime)
            };
            ExecuteStoredProcedure(storedProcedureName, parameters);
        }

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
        /// Attempts to execute a stored procedure which does not return a result set.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters to pass to the stored procedure.</param>
        protected void ExecuteStoredProcedure(string procedureName, IEnumerable<SqlParameter> parameters)
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
                    command.CommandTimeout = retryInterval * retryCount;
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
            catch(Exception e)
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
                command.CommandTimeout = retryInterval * retryCount;
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
        /// Attempts to execute the specified query, converting a specified columns from each row of the results to the specified types.
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
                command.CommandTimeout = retryInterval * retryCount;
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
        /// Attempts to execute the specified query, converting a specified columns from each row of the results to the specified types.
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
                command.CommandTimeout = retryInterval * retryCount;
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
        /// Loads the access manager with state corresponding to the specified timestamp from persistent storage into the specified AccessManager instance.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        protected void LoadToAccessManager(DateTime stateTime, AccessManager<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
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

        #pragma warning disable 1591

        protected void ThrowExceptionIfStringifiedParameterLargerThanVarCharLimit(string parameterName, string parameterValue)
        {
            if (parameterValue.Length > columnSizeLimit)
                throw new ArgumentOutOfRangeException(parameterName, $"Parameter '{parameterName}' with stringified value '{parameterValue}' is longer than the maximum allowable column size of {columnSizeLimit}.");
        }

        protected void ThrowExceptionIfDateTimeParameterInTheFuture(string parameterName, DateTime parameterValue)
        {
            if (parameterValue > DateTime.Now)
                throw new ArgumentOutOfRangeException(parameterName, $"Parameter '{parameterName}' with value '{parameterValue.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}' cannot be greater than the current date.");
        }

        #pragma warning restore 1591

        #endregion
    }
}
