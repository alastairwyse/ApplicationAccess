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
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Persistence.SqlServer;
using Microsoft.Data.SqlClient;

namespace ApplicationAccess.Distribution.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> which reads and writes instances of <see cref="ShardConfigurationSet{TClientConfiguration}"/> to and from from a Microsoft SQL Server database.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The implementation of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> embedded within items in the shard configuration set.</typeparam>
    /// <typeparam name="TJsonSerializer">An implementation of <see cref="IDistributedAccessManagerAsyncClientConfigurationJsonSerializer{T}"/> which serializes <typeparamref name="TClientConfiguration"/> instances</typeparam>
    public class SqlServerShardConfigurationSetPersister<TClientConfiguration, TJsonSerializer> : IShardConfigurationSetPersister<TClientConfiguration, TJsonSerializer>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
        where TJsonSerializer : IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<TClientConfiguration>
    {
        // TODO: Shares many common fields with SqlServerAccessManagerTemporalPersisterBase... could promote into a common abstract base class.

        /// <summary>The string to use to connect to the SQL Server database.</summary>
        protected String connectionString;
        /// <summary>The number of times an operation against the SQL Server database should be retried in the case of execution failure.</summary>
        protected Int32 retryCount;
        /// <summary>The time in seconds between operation retries.</summary>
        protected Int32 retryInterval;
        /// <summary>The timeout in seconds before terminating am operation against the SQL Server database.  A value of 0 indicates no limit.</summary>
        protected Int32 operationTimeout;
        /// <summary>The retry logic to use when connecting to and executing against the SQL Server database.</summary>
        protected SqlRetryLogicOption sqlRetryLogicOption;
        /// <summary>A set of SQL Server database engine error numbers which denote a transient fault.</summary>
        /// <see href="https://docs.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors?view=sql-server-ver16"/>
        /// <see href="https://docs.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues?view=azuresql"/>
        protected List<Int32> sqlServerTransientErrorNumbers;
        /// <summary>The action to invoke if an action is retried due to a transient error.</summary>
        protected EventHandler<SqlRetryingEventArgs> connectionRetryAction;
        /// <summary>JSON serializer for TClientConfiguration objects.</summary>
        protected TJsonSerializer jsonSerializer;
        /// <summary>Wraps calls to execute stored procedures so that they can be mocked in unit tests.</summary>
        protected IStoredProcedureExecutionWrapper storedProcedureExecutor;

        // TODO: Implement as per SqlServerAccessManagerTemporalPersisterBase and SqlServerAccessManagerTemporalPersister
        //   Need to design table, do create scripts, and SP

        /// <inheritdoc/>
        public void Write(ShardConfigurationSet<TClientConfiguration> shardConfigurationSet)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public ShardConfigurationSet<TClientConfiguration> Read()
        {
            throw new NotImplementedException();
        }
    }
}
