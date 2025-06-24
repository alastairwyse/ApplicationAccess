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
using System.Threading.Tasks;
using System.Net.Http;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Persistence.SqlServer;
using ApplicationAccess.Hosting.Rest.DistributedOperationRouterClient;
using ApplicationAccess.Hosting.Rest.DistributedWriterAdministratorClient;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution.Models;
using ApplicationAccess.Redistribution.Kubernetes;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// A node which manages an instance of a distributed AccessManager implementation hosted in Kubernetes, and using Microsoft SqlServer for persistent storage.
    /// </summary>
    public class KubernetesDistributedInstanceManagerNode : IKubernetesDistributedInstanceManager, IDisposable
    {
        /// <summary>The <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance which manages the distributed AccessManager.</summary>
        protected KubernetesDistributedAccessManagerInstanceManager<SqlServerLoginCredentials> kubernetesDistributedInstanceManager;
        /// <summary>The HTTP client to use to connect to nodes in the distributed instance during split or merge operations.</summary>
        protected HttpClient httpClient;
        /// <summary>The number of times an operation against a SQL Server database should be retried in the case of execution failure during a split or merge operation.</summary>
        protected Int32 sqlServerRetryCount;
        /// <summary>The time in seconds between SQL Server operation retries.</summary>
        protected Int32 sqlServerRetryInterval;
        /// <summary>The timeout in seconds before terminating an operation against a SQL Server database during a split or merge operation.  A value of 0 indicates no limit.</summary>
        protected Int32 sqlServerOperationTimeout;
        /// <summary>The number of times an operation should be retried in the case of a transient error (e.g. network error) when connecting to the writer or distributed operation router nodes via their respective REST clients.</summary>
        protected Int32 restClientRetryCount;
        /// <summary>The time in seconds between REST client retries.</summary>
        protected Int32 restClientRetryInterval;
        /// <summary>The time in milliseconds wait for REST client requests before timing out.</summary>
        protected Int32 restClientTimeout;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.KubernetesDistributedInstanceManagerNode class.
        /// </summary>
        /// <param name="kubernetesDistributedInstanceManager">The <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance which manages the distributed AccessManager.</param>
        /// <param name="sqlServerRetryCount">The number of times an operation against a SQL Server database should be retried in the case of execution failure during a split or merge operation.</param>
        /// <param name="sqlServerRetryInterval">The time in seconds between SQL Server operation retries.</param>
        /// <param name="sqlServerOperationTimeout">The timeout in seconds before terminating an operation against a SQL Server database during a split or merge operation.  A value of 0 indicates no limit.</param>
        /// <param name="restClientRetryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error) when connecting to the writer or distributed operation router nodes via their respective REST clients.</param>
        /// <param name="restClientRetryInterval">The time in seconds between REST client retries.</param>
        /// <param name="restClientTimeout">The time in milliseconds wait for REST client requests before timing out.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedInstanceManagerNode
        (
            KubernetesDistributedAccessManagerInstanceManager<SqlServerLoginCredentials> kubernetesDistributedInstanceManager,
            Int32 sqlServerRetryCount,
            Int32 sqlServerRetryInterval,
            Int32 sqlServerOperationTimeout,
            Int32 restClientRetryCount,
            Int32 restClientRetryInterval,
            Int32 restClientTimeout, 
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            this.kubernetesDistributedInstanceManager = kubernetesDistributedInstanceManager;
            this.sqlServerRetryCount = sqlServerRetryCount;
            this.sqlServerRetryInterval = sqlServerRetryInterval;
            this.sqlServerOperationTimeout = sqlServerOperationTimeout;
            this.restClientRetryCount = restClientRetryCount;
            this.restClientRetryInterval = restClientRetryInterval;
            this.restClientTimeout = restClientTimeout;
            this.logger = logger;
            this.metricLogger = metricLogger;
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMilliseconds(restClientTimeout);
            disposed = false;
        }

        /// <inheritdoc/>
        public Uri DistributedOperationRouterUrl
        {
            set
            {
                kubernetesDistributedInstanceManager.DistributedOperationRouterUrl = value;
            }
        }

        /// <inheritdoc/>
        public Uri Writer1Url
        {
            set
            {
                kubernetesDistributedInstanceManager.Writer1Url = value;
            }
        }

        /// <inheritdoc/>
        public Uri Writer2Url
        {
            set
            {
                kubernetesDistributedInstanceManager.Writer2Url = value;
            }
        }

        /// <inheritdoc/>
        public Uri DistributedOperationCoordinatorUrl
        {
            set
            {
                kubernetesDistributedInstanceManager.DistributedOperationCoordinatorUrl = value;
            }
        }

        /// <inheritdoc/>
        public KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> InstanceConfiguration
        {
            get
            {
                return kubernetesDistributedInstanceManager.InstanceConfiguration;
            }
        }

        /// <inheritdoc/>
        public async Task CreateDistributedAccessManagerInstanceAsync
        (
            IList<ShardGroupConfiguration<SqlServerLoginCredentials>> userShardGroupConfiguration,
            IList<ShardGroupConfiguration<SqlServerLoginCredentials>> groupToGroupMappingShardGroupConfiguration,
            IList<ShardGroupConfiguration<SqlServerLoginCredentials>> groupShardGroupConfiguration
        )
        {
            await kubernetesDistributedInstanceManager.CreateDistributedAccessManagerInstanceAsync
            (
                userShardGroupConfiguration,
                groupToGroupMappingShardGroupConfiguration,
                groupShardGroupConfiguration
            );
        }

        /// <inheritdoc/>
        public async Task DeleteDistributedAccessManagerInstanceAsync(Boolean deletePersistentStorageInstances)
        {
            await DeleteDistributedAccessManagerInstanceAsync(deletePersistentStorageInstances);
        }

        /// <inheritdoc/>
        public async Task SplitShardGroupAsync
        (
            DataElement dataElement,
            Int32 hashRangeStart,
            Int32 splitHashRangeStart,
            Int32 splitHashRangeEnd,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        )
        {
            await kubernetesDistributedInstanceManager.SplitShardGroupAsync
            (
                dataElement,
                hashRangeStart,
                splitHashRangeStart,
                splitHashRangeEnd,
                SqlServerTemporalEventBatchReaderCreationFunction,
                SqlServerTemporalEventBatchReaderCreationFunction,
                SqlServerTemporalEventBatchReaderCreationFunction,
                DistributedOperationRouterClientCreationFunction,
                DistributedWriterAdministratorClientCreationFunction,
                eventBatchSize,
                sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                sourceWriterNodeOperationsCompleteCheckRetryInterval
            );
        }

        /// <inheritdoc/>
        public async Task MergeShardGroupsAsync
        (
            DataElement dataElement,
            Int32 sourceShardGroup1HashRangeStart,
            Int32 sourceShardGroup2HashRangeStart,
            Int32 sourceShardGroup2HashRangeEnd,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        )
        {
            await kubernetesDistributedInstanceManager.MergeShardGroupsAsync
            (
                dataElement,
                sourceShardGroup1HashRangeStart,
                sourceShardGroup2HashRangeStart,
                sourceShardGroup2HashRangeEnd,
                SqlServerTemporalEventBatchReaderCreationFunction,
                SqlServerTemporalEventBatchReaderCreationFunction,
                DistributedOperationRouterClientCreationFunction,
                DistributedWriterAdministratorClientCreationFunction,
                eventBatchSize,
                sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                sourceWriterNodeOperationsCompleteCheckRetryInterval
            );
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns a function which creates a <see cref="SqlServerAccessManagerTemporalEventBatchReader{TUser, TGroup, TComponent, TAccess}"/> to use in calls to <see cref="KubernetesDistributedInstanceManagerNode.SplitShardGroupAsync(DataElement, Int32, Int32, Int32, Int32, Int32, Int32)">SplitShardGroupAsync()</see> and <see cref="KubernetesDistributedInstanceManagerNode.MergeShardGroupsAsync(DataElement, Int32, Int32, Int32, Int32, Int32, Int32)">MergeShardGroupsAsync()</see>.
        /// </summary>
        protected Func<SqlServerLoginCredentials, SqlServerAccessManagerTemporalEventBatchReader<String, String, String, String>> SqlServerTemporalEventBatchReaderCreationFunction
        {
            get
            {
                return (SqlServerLoginCredentials credentials) =>
                {
                    return new SqlServerAccessManagerTemporalEventBatchReader<String, String, String, String>
                    (
                        credentials.ConnectionString,
                        sqlServerRetryCount,
                        sqlServerRetryInterval,
                        sqlServerOperationTimeout,
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        logger, 
                        metricLogger
                    );
                };
            }
        }

        /// <summary>
        /// Returns a function which creates a <see cref="DistributedAccessManagerOperationRouterClient"/> to use in calls to <see cref="KubernetesDistributedInstanceManagerNode.SplitShardGroupAsync(DataElement, Int32, Int32, Int32, Int32, Int32, Int32)">SplitShardGroupAsync()</see> and <see cref="KubernetesDistributedInstanceManagerNode.MergeShardGroupsAsync(DataElement, Int32, Int32, Int32, Int32, Int32, Int32)">MergeShardGroupsAsync()</see>.
        /// </summary>
        protected Func<Uri, IDistributedAccessManagerOperationRouter> DistributedOperationRouterClientCreationFunction
        {
            get
            {
                return (Uri url) =>
                {
                    return new DistributedAccessManagerOperationRouterClient
                    (
                        url,
                        httpClient,
                        restClientRetryCount, 
                        restClientRetryInterval,
                        logger,
                        metricLogger
                    );
                };
            }
        }

        /// <summary>
        /// Returns a function which creates a <see cref="DistributedAccessManagerWriterAdministratorClient"/> to use in calls to <see cref="KubernetesDistributedInstanceManagerNode.SplitShardGroupAsync(DataElement, Int32, Int32, Int32, Int32, Int32, Int32)">SplitShardGroupAsync()</see> and <see cref="KubernetesDistributedInstanceManagerNode.MergeShardGroupsAsync(DataElement, Int32, Int32, Int32, Int32, Int32, Int32)">MergeShardGroupsAsync()</see>.
        /// </summary>
        protected Func<Uri, IDistributedAccessManagerWriterAdministrator> DistributedWriterAdministratorClientCreationFunction
        {
            get
            {
                return (Uri url) =>
                {
                    return new DistributedAccessManagerWriterAdministratorClient
                    (
                        url,
                        httpClient,
                        restClientRetryCount,
                        restClientRetryInterval
                    );
                };
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the KubernetesDistributedInstanceManagerNode.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~KubernetesDistributedInstanceManagerNode()
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
                    httpClient.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
