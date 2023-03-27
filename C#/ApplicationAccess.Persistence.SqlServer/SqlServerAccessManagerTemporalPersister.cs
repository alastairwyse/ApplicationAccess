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

namespace ApplicationAccess.Persistence.SqlServer
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> and see <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/> which persists access manager events to and allows reading of <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> objects from a Microsoft SQL Server database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerTemporalEventPersister and IAccessManagerTemporalPersistentReader implementations.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerTemporalEventPersister and IAccessManagerTemporalPersistentReader implementations.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerEventProcessor and IAccessManagerTemporalPersistentReader implementations.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IAccessManagerTemporalEventPersister and IAccessManagerTemporalPersistentReader implementations.</typeparam>
    /// <remarks>Note that <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}">IAccessManagerEventProcessor</see> methods implemented in this class should not be called from concurrent threads.  The class is designed to operate behind a class which manages mutual exclusion such as the <see cref="AccessManagerTemporalEventPersisterBuffer{TUser, TGroup, TComponent, TAccess}">AccessManagerTemporalEventPersisterBuffer</see> or <see cref="ApplicationAccess.Validation.ConcurrentAccessManagerEventValidator{TUser, TGroup, TComponent, TAccess}">ConcurrentAccessManagerEventValidator</see> classes.</remarks>
    public class SqlServerAccessManagerTemporalPersister<TUser, TGroup, TComponent, TAccess> : SqlServerAccessManagerTemporalPersisterBase<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>
    {
        // TODO:
        //   Add additional tests using 'storedProcedureExecutor' member moved to base class (from SqlServerAccessManagerTemporalBulkPersister)

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

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SqlServer.SqlServerAccessManagerTemporalPersister class.
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
        public SqlServerAccessManagerTemporalPersister
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
            :base(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SqlServer.SqlServerAccessManagerTemporalPersister class.
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
        public SqlServerAccessManagerTemporalPersister
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

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            AddUser(user, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            RemoveUser(user, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            AddGroup(group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            RemoveGroup(group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            AddUserToGroupMapping(user, group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            RemoveUserToGroupMapping(user, group, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            AddGroupToGroupMapping(fromGroup, toGroup, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            RemoveGroupToGroupMapping(fromGroup, toGroup, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            AddEntityType(entityType, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            RemoveEntityType(entityType, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            AddEntity(entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            RemoveEntity(entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            AddUserToEntityMapping(user, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            RemoveUserToEntityMapping(user, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            AddGroupToEntityMapping(group, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            RemoveGroupToEntityMapping(group, entityType, entity, Guid.NewGuid(), DateTime.UtcNow);
        }

        /// <inheritdoc/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserStoredProcedure(addUserStoredProcedureName, user, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserStoredProcedure(removeUserStoredProcedureName, user, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupStoredProcedure(addGroupStoredProcedureName, group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupStoredProcedure(removeGroupStoredProcedureName, group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToGroupMappingStoredProcedure(addUserToGroupMappingStoredProcedureName, user, group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToGroupMappingStoredProcedure(removeUserToGroupMappingStoredProcedureName, user, group, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToGroupMappingStoredProcedure(addGroupToGroupMappingProcedureName, fromGroup, toGroup, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToGroupMappingStoredProcedure(removeGroupToGroupMappingProcedureName, fromGroup, toGroup, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure(addUserToApplicationComponentAndAccessLevelMappingProcedureName, user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure(removeUserToApplicationComponentAndAccessLevelMappingProcedureName, user, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure(addGroupToApplicationComponentAndAccessLevelMappingProcedureName, group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure(removeGroupToApplicationComponentAndAccessLevelMappingProcedureName, group, applicationComponent, accessLevel, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityTypeStoredProcedure(addEntityTypeProcedureName, entityType, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityTypeStoredProcedure(removeEntityTypeProcedureName, entityType, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityStoredProcedure(addEntityProcedureName, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteEntityStoredProcedure(removeEntityProcedureName, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToEntityMappingStoredProcedure(addUserToEntityMappingProcedureName, user, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteUserToEntityMappingStoredProcedure(removeUserToEntityMappingProcedureName, user, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToEntityMappingStoredProcedure(addGroupToEntityMappingProcedureName, group, entityType, entity, eventId, occurredTime);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            SetupAndExecuteGroupToEntityMappingStoredProcedure(removeGroupToEntityMappingProcedureName, group, entityType, entity, eventId, occurredTime);
        }

        #region Private/Protected Methods

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
