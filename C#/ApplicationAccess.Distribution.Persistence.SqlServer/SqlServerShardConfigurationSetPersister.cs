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
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Serialization;
using ApplicationLogging;

namespace ApplicationAccess.Distribution.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> which reads and writes instances of <see cref="ShardConfigurationSet{TClientConfiguration}"/> to and from from a Microsoft SQL Server database.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The implementation of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> embedded within items in the shard configuration set.</typeparam>
    /// <typeparam name="TJsonSerializer">An implementation of <see cref="IDistributedAccessManagerAsyncClientConfigurationJsonSerializer{T}"/> which serializes <typeparamref name="TClientConfiguration"/> instances</typeparam>
    public class SqlServerShardConfigurationSetPersister<TClientConfiguration, TJsonSerializer> : SqlServerPersisterBase, IShardConfigurationSetPersister<TClientConfiguration, TJsonSerializer>, IDisposable
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
        where TJsonSerializer : IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<TClientConfiguration>
    {
        /// <summary>DateTime format string which matches the <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver16#date-and-time-styles">Transact-SQL 126 date and time style</see>.</summary>
        protected const String transactionSql126DateStyle = "yyyy-MM-ddTHH:mm:ss.fffffff";

        #pragma warning disable 1591

        protected const String updateShardConfigurationsStoredProcedureName = "UpdateShardConfiguration";
        protected const String shardConfigurationItemsParameterName = "@ShardConfigurationItems";
        protected const String dataElementTypeColumnName = "DataElementType";
        protected const String operationTypeColumnName = "OperationType";
        protected const String hashRangeStartColumnName = "HashRangeStart";
        protected const String clientConfigurationColumnName = "ClientConfiguration";

        #pragma warning restore 1591

        /// <summary>Staging table which is populated with all events from a Flush() operation before passing to SQL Server as a <see href="https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters">table-valued parameter</see>.  As this table is used to pass all events in a single operation, it contains generic columns holding the event data, the content of which varies according to the type of the event.</summary>
        protected DataTable stagingTable;
        /// <summary>Column in the staging table which holds the type of data element of the shard configuration item.</summary>
        protected DataColumn dataElementTypeColumn;
        /// <summary>Column in the staging table which holds the type of operation of the shard configuration item.</summary>
        protected DataColumn operationTypeColumn;
        /// <summary>Column in the staging table which holds the first in the range of hash codes of data elements managed by the shard.</summary>
        protected DataColumn hashRangeStartColumn;
        /// <summary>Column in the staging table which holds the configuration used to create a client which connects to the shard.</summary>
        protected DataColumn clientConfigurationColumn;
        /// <summary>JSON serializer for <typeparamref name="TClientConfiguration"/> objects.</summary>
        protected TJsonSerializer jsonSerializer;
        /// <summary>Wraps calls to execute stored procedures so that they can be mocked in unit tests.</summary>
        protected IStoredProcedureExecutionWrapper storedProcedureExecutor;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerShardConfigurationSetPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="jsonSerializer">JSON serializer for <typeparamref name="TClientConfiguration"/> objects.</param>
        /// <param name="logger">The logger for general logging.</param>
        public SqlServerShardConfigurationSetPersister
        (
            String connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            Int32 operationTimeout,
            TJsonSerializer jsonSerializer, 
            IApplicationLogger logger
        ) : base(connectionString, retryCount, retryInterval, operationTimeout, logger)
        {
            this.jsonSerializer = jsonSerializer;
            storedProcedureExecutor = new StoredProcedureExecutionWrapper((String procedureName, IEnumerable<SqlParameter> parameters) => { ExecuteStoredProcedure(procedureName, parameters); });
            disposed = false;
            stagingTable = new DataTable();
            dataElementTypeColumn = new DataColumn(dataElementTypeColumnName, typeof(String));
            operationTypeColumn = new DataColumn(operationTypeColumnName, typeof(String));
            hashRangeStartColumn = new DataColumn(hashRangeStartColumnName, typeof(Int32));
            clientConfigurationColumn = new DataColumn(clientConfigurationColumnName, typeof(String));
            stagingTable.Columns.Add(dataElementTypeColumn);
            stagingTable.Columns.Add(operationTypeColumn);
            stagingTable.Columns.Add(hashRangeStartColumn);
            stagingTable.Columns.Add(clientConfigurationColumn);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerShardConfigurationSetPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
        /// <param name="retryInterval">The time in seconds between operation retries.</param>
        /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
        /// <param name="jsonSerializer">JSON serializer for <typeparamref name="TClientConfiguration"/> objects.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="storedProcedureExecutor">A test (mock) <see cref="IStoredProcedureExecutionWrapper"/> object.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public SqlServerShardConfigurationSetPersister
        (
            String connectionString,
            Int32 retryCount,
            Int32 retryInterval,
            Int32 operationTimeout,
            TJsonSerializer jsonSerializer,
            IApplicationLogger logger,
            IStoredProcedureExecutionWrapper storedProcedureExecutor
        ) : this(connectionString, retryCount, retryInterval, operationTimeout, jsonSerializer, logger)
        {
            this.storedProcedureExecutor = storedProcedureExecutor;
        }

        /// <inheritdoc/>
        public void Write(ShardConfigurationSet<TClientConfiguration> shardConfigurationSet)
        {
            stagingTable.Rows.Clear();
            foreach (ShardConfiguration<TClientConfiguration> currentShardConfigurationItem in shardConfigurationSet.Items)
            {
                DataRow row = stagingTable.NewRow();
                row[dataElementTypeColumnName] = currentShardConfigurationItem.DataElementType.ToString();
                row[operationTypeColumnName] = currentShardConfigurationItem.OperationType.ToString();
                row[hashRangeStartColumnName] = currentShardConfigurationItem.HashRangeStart;
                try
                {
                    row[clientConfigurationColumnName] = jsonSerializer.Serialize(currentShardConfigurationItem.ClientConfiguration);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to serialize shard configuration item for data element '{currentShardConfigurationItem.DataElementType.ToString()}', operation '{currentShardConfigurationItem.OperationType.ToString()}', and hash range start {currentShardConfigurationItem.HashRangeStart}.", e);
                }
                stagingTable.Rows.Add(row);
            }
            var parameters = new List<SqlParameter>()
            {
                CreateSqlParameterWithValue(shardConfigurationItemsParameterName, SqlDbType.Structured, stagingTable)
            };
            storedProcedureExecutor.Execute(updateShardConfigurationsStoredProcedureName, parameters);
        }

        /// <inheritdoc/>
        public ShardConfigurationSet<TClientConfiguration> Read()
        {
            var shardConfigurationItems = new List<ShardConfiguration<TClientConfiguration>>();
            DateTime currentDateTime = DateTime.UtcNow;
            String query =
            @$"
            SELECT  DataElementType, 
                    OperationType, 
                    HashRangeStart, 
                    ClientConfiguration 
            FROM    ShardConfiguration 
            WHERE   CONVERT(datetime2, '{currentDateTime.ToString(transactionSql126DateStyle)}', 126) BETWEEN TransactionFrom AND TransactionTo;";

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
                        ShardConfiguration<TClientConfiguration> currentConfigurationItem = ReadShardConfigurationItem(dataReader);
                        shardConfigurationItems.Add(currentConfigurationItem);
                    }
                }
                connection.RetryLogicProvider.Retrying -= connectionRetryAction;
            }

            return new ShardConfigurationSet<TClientConfiguration>(shardConfigurationItems);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Reads a single <see cref="ShardConfiguration{TClientConfiguration}"/> from the current row of the specified <see cref="SqlDataReader"/>.
        /// </summary>
        /// <param name="dataReader">The <see cref="SqlDataReader"/> to read from.</param>
        /// <returns>The <see cref="ShardConfiguration{TClientConfiguration}"/>.</returns>
        protected ShardConfiguration<TClientConfiguration> ReadShardConfigurationItem(SqlDataReader dataReader)
        {
            Boolean parseResult = Enum.TryParse<DataElement>((String)dataReader[dataElementTypeColumnName], out DataElement dataElement);
            if (parseResult == false)
            {
                throw new Exception($"Failed to convert value '{(String)dataReader[dataElementTypeColumnName]}' in column '{dataElementTypeColumnName}' to a {typeof(DataElement).Name}.");
            }
            parseResult = Enum.TryParse<Operation>((String)dataReader[operationTypeColumnName], out Operation operation);
            if (parseResult == false)
            {
                throw new Exception($"Failed to convert value '{(String)dataReader[operationTypeColumnName]}' in column '{operationTypeColumnName}' to an {typeof(Operation).Name}.");
            }
            Int32 hashRangeStart = (Int32)dataReader[hashRangeStartColumnName];
            TClientConfiguration clientConfiguration = default(TClientConfiguration);
            try
            {
                clientConfiguration = jsonSerializer.Deserialize((String)dataReader[clientConfigurationColumnName]);
            }
            catch (Exception e)
            {
                throw new DeserializationException($"Failed to deserialize client configuration of type '{typeof(TClientConfiguration).Name}' from string '{(String)dataReader[clientConfigurationColumnName]}'.", e);
            }

            return new ShardConfiguration<TClientConfiguration>(dataElement, operation, hashRangeStart, clientConfiguration);
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the SqlServerShardConfigurationSetPersister.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~SqlServerShardConfigurationSetPersister()
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
                    dataElementTypeColumn.Dispose();
                    operationTypeColumn.Dispose();
                    hashRangeStartColumn.Dispose();
                    clientConfigurationColumn.Dispose();
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
            /// <summary>An action which executed the stored procedure.</summary>
            protected Action<String, IEnumerable<SqlParameter>> executeAction;

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerShardConfigurationSetPersister+StoredProcedureExecutionWrapper class.
            /// </summary>
            /// <param name="executeAction"></param>
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
