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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Utilities;
using MoreComplexDataStructures;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Manages a set of <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> implementations corresponding to a <see cref="ShardConfigurationSet{TClientConfiguration}"/> which connect to shards in a distributed AccessManager implementation.  Provides query methods to return clients based on varying parameters, and the ability to refresh the <see cref="ShardConfigurationSet{TClientConfiguration}"/> to update/rebuild the clients.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration stored in the shard configuration.</typeparam>
    /// <remarks>This class is thread safe for concurrent read opertations (e.g. method <see cref="ShardClientManager{TClientConfiguration}.GetClient(DataElement, Operation, String)">GetClient()</see>).  The <see cref="ShardClientManager{TClientConfiguration}.RefreshConfiguration(ShardConfigurationSet{TClientConfiguration})">RefreshConfiguration()</see> method however should not be called concurrently by multiple threads.</remarks>
    public class ShardClientManager<TClientConfiguration> : IShardClientManager<TClientConfiguration>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        /// <summary>Maps a <see cref="DataElement"/> and <see cref="Operation"/> to all clients (and corresponding shard descriptions) connecting to shards which manage that data element and for operations of that type.</summary>
        protected Dictionary<DataElementAndOperation, HashSet<DistributedClientAndShardDescription>> dataElementAndOperationToClientMap;
        /// <summary>Maps a <see cref="DataElement"/> and <see cref="Operation"/> to a tree which stores clients (and corresponding shard descriptions) connecting to shards for that element/operation, indexed by the range of hash values that client handles.</summary>
        protected Dictionary<DataElementAndOperation, WeightBalancedTree<HashRangeStartClientAndShardDescription>> hashRangeToClientMap;
        /// <summary>The current shard configuration.</summary>
        protected ShardConfigurationSet<TClientConfiguration> currentConfiguration;
        /// <summary>An <see cref="IDistributedAccessManagerAsyncClientFactory{TClientConfiguration, TUser, TGroup, TComponent, TAccess}"/> instance used to create AccessManager client instances from configuration.</summary>
        protected IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, String, String, String, String> clientFactory;
        /// <summary>A hash code generator for users.</summary>
        protected IHashCodeGenerator<String> userHashCodeGenerator;
        /// <summary>A hash code generator for groups.</summary>
        protected IHashCodeGenerator<String> groupHashCodeGenerator;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Lock object for the structures which store the shard configuration.</summary>
        protected ReaderWriterLockSlim configurationLock;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.ShardClientManager class.
        /// </summary>
        /// <param name="initialConfiguration">The initial shard configuration.</param>
        /// <param name="clientFactory">An <see cref="IDistributedAccessManagerAsyncClientFactory{TClientConfiguration, TUser, TGroup, TComponent, TAccess}"/> instance used to create AccessManager client instances from configuration.</param>
        /// <param name="userHashCodeGenerator">A hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">A hash code generator for groups.</param>
        public ShardClientManager
        (
            ShardConfigurationSet<TClientConfiguration> initialConfiguration,
            IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, String, String, String, String> clientFactory, 
            IHashCodeGenerator<String> userHashCodeGenerator,
            IHashCodeGenerator<String> groupHashCodeGenerator
        )
        {
            this.clientFactory = clientFactory;
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            metricLogger = new NullMetricLogger();
            configurationLock = new ReaderWriterLockSlim();
            disposed = false;
            RefreshConfiguration(initialConfiguration);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.ShardClientManager class.
        /// </summary>
        /// <param name="initialConfiguration">The initial shard configuration.</param>
        /// <param name="clientFactory">An <see cref="IDistributedAccessManagerAsyncClientFactory{TClientConfiguration, TUser, TGroup, TComponent, TAccess}"/> instance used to create AccessManager client instances from configuration.</param>
        /// <param name="userHashCodeGenerator">A hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">A hash code generator for groups.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ShardClientManager
        (
            ShardConfigurationSet<TClientConfiguration> initialConfiguration,
            IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, String, String, String, String> clientFactory,
            IHashCodeGenerator<String> userHashCodeGenerator,
            IHashCodeGenerator<String> groupHashCodeGenerator,
            IMetricLogger metricLogger
        )
            : this(initialConfiguration, clientFactory, userHashCodeGenerator, groupHashCodeGenerator)
        {
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public IEnumerable<DistributedClientAndShardDescription> GetAllClients(DataElement dataElement, Operation operation)
        {
            var dataElementAndOperation = new DataElementAndOperation(dataElement, operation);
            configurationLock.EnterReadLock();
            try
            {
                if (dataElementAndOperationToClientMap.ContainsKey(dataElementAndOperation) == false)
                    throw new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{dataElement}' and {typeof(Operation).Name} '{operation}'.");

                return dataElementAndOperationToClientMap[dataElementAndOperation];
            }
            finally
            {
                configurationLock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public DistributedClientAndShardDescription GetClient(DataElement dataElement, Operation operation, String dataElementValue)
        {
            return GetClient(dataElement, operation, dataElementValue, true);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> GetClients(DataElement dataElement, Operation operation, IEnumerable<String> dataElementValues)
        {
            var clientToDataElementMap = new Dictionary<DistributedClientAndShardDescription, HashSet<String>>();
            configurationLock.EnterReadLock();
            try
            {
                foreach (String currentDataElementValue in dataElementValues)
                {
                    DistributedClientAndShardDescription clientAndDescription = GetClient(dataElement, operation, currentDataElementValue, false);
                    if (clientToDataElementMap.ContainsKey(clientAndDescription) == false)
                    {
                        clientToDataElementMap.Add(clientAndDescription, new HashSet<String>());
                    }
                    clientToDataElementMap[clientAndDescription].Add(currentDataElementValue);
                }

                foreach (KeyValuePair<DistributedClientAndShardDescription, HashSet<String>> currentKvp in clientToDataElementMap)
                {
                    yield return new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>(currentKvp.Key, currentKvp.Value);
                }
            }
            finally
            {
                configurationLock.ExitReadLock();
            }
        }

        /// <inheritdoc/>
        public void RefreshConfiguration(ShardConfigurationSet<TClientConfiguration> shardConfiguration)
        {
            if (currentConfiguration == null || (!currentConfiguration.Equals(shardConfiguration)))
            {
                Guid beginId = metricLogger.Begin(new ConfigurationRefreshTime());

                try
                {
                    // Create and populate the new indexing structures
                    var configurationToClientMap = new Dictionary<TClientConfiguration, IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                    var newDataElementAndOperationToClientMap = new Dictionary<DataElementAndOperation, HashSet<DistributedClientAndShardDescription>>();
                    var newHashRangeToClientMap = new Dictionary<DataElementAndOperation, WeightBalancedTree<HashRangeStartClientAndShardDescription>>();

                    foreach (ShardConfiguration<TClientConfiguration> currentShardConfigurationItem in shardConfiguration.Items)
                    {
                        var dataElementAndOperation = new DataElementAndOperation(currentShardConfigurationItem.DataElementType, currentShardConfigurationItem.OperationType);
                        IDistributedAccessManagerAsyncClient<String, String, String, String> client = null;
                        if (configurationToClientMap.ContainsKey(currentShardConfigurationItem.ClientConfiguration) == true)
                        {
                            client = configurationToClientMap[currentShardConfigurationItem.ClientConfiguration];
                        }
                        else
                        {
                            try
                            {
                                client = clientFactory.GetClient(currentShardConfigurationItem.ClientConfiguration);
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"Failed to create '{typeof(IDistributedAccessManagerAsyncClient<String, String, String, String>).Name}' instance from client configuration of type '{typeof(TClientConfiguration).FullName}'.", e);
                            }
                            configurationToClientMap.Add(currentShardConfigurationItem.ClientConfiguration, client);
                        }
                        var clientAndDescription = new DistributedClientAndShardDescription(client, currentShardConfigurationItem.Describe(true));

                        // Populate 'newHashRangeToClientMap'
                        if (newHashRangeToClientMap.ContainsKey(dataElementAndOperation) == false)
                        {
                            newHashRangeToClientMap.Add(dataElementAndOperation, new WeightBalancedTree<HashRangeStartClientAndShardDescription>());
                        }
                        newHashRangeToClientMap[dataElementAndOperation].Add(new HashRangeStartClientAndShardDescription(currentShardConfigurationItem.HashRangeStart, clientAndDescription));

                        // Populate 'newDataElementAndOperationToClientMap'
                        if (newDataElementAndOperationToClientMap.ContainsKey(dataElementAndOperation) == false)
                        {
                            newDataElementAndOperationToClientMap.Add(dataElementAndOperation, new HashSet<DistributedClientAndShardDescription>());
                        }
                        if (newDataElementAndOperationToClientMap[dataElementAndOperation].Contains(clientAndDescription) == false)
                        {
                            newDataElementAndOperationToClientMap[dataElementAndOperation].Add(new DistributedClientAndShardDescription(client, currentShardConfigurationItem.Describe(false)));
                        }
                    }

                    // Overwrite the existing indexing structures and fields
                    configurationLock.EnterWriteLock();
                    try
                    {
                        dataElementAndOperationToClientMap = newDataElementAndOperationToClientMap;
                        hashRangeToClientMap = newHashRangeToClientMap;
                        currentConfiguration = shardConfiguration;
                    }
                    finally
                    {
                        configurationLock.ExitWriteLock();
                    }

                    metricLogger.End(beginId, new ConfigurationRefreshTime());
                    metricLogger.Increment(new ConfigurationRefreshed());
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ConfigurationRefreshTime());
                    throw;
                }
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns a client which connects to the shard managing the specified element and operation type.
        /// </summary>
        /// <param name="dataElement">The type of the element.</param>
        /// <param name="operation">The type of operation to retrieve the clients for.</param>
        /// <param name="dataElementValue">The value of the element.</param>
        /// <param name="acquireLock">Whether a mutual-exclusion read lock should be acquired.</param>
        /// <returns>The client and a description of the configuration of the shard the client connects to (e.g. to identify the client in exception messages).</returns>
        public DistributedClientAndShardDescription GetClient(DataElement dataElement, Operation operation, String dataElementValue, Boolean acquireLock)
        {
            var dataElementAndOperation = new DataElementAndOperation(dataElement, operation);
            if (acquireLock == true)
            {
                configurationLock.EnterReadLock();
            }
            try
            {
                if (hashRangeToClientMap.ContainsKey(dataElementAndOperation) == false)
                    throw new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{dataElement}' and {typeof(Operation).Name} '{operation}'.");

                if (dataElement == DataElement.User)
                {
                    return GetClientForHashCode(hashRangeToClientMap[dataElementAndOperation], userHashCodeGenerator.GetHashCode(dataElementValue));
                }
                else
                {
                    return GetClientForHashCode(hashRangeToClientMap[dataElementAndOperation], groupHashCodeGenerator.GetHashCode(dataElementValue));
                }
            }
            finally
            {
                if (acquireLock == true)
                {
                    configurationLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the client and shard description corresponding to a given hash code from the specified tree.
        /// </summary>
        /// <param name="tree">The tree to search.</param>
        /// <param name="hashCode">The hash code to search for.</param>
        /// <returns>The client which handles the given hash code, and its corresponding shard description.</returns>
        protected DistributedClientAndShardDescription GetClientForHashCode(WeightBalancedTree<HashRangeStartClientAndShardDescription> tree, Int32 hashCode)
        {
            if (tree.Contains(new HashRangeStartClientAndShardDescription(hashCode, null)) == true)
            {
                return tree.Get(new HashRangeStartClientAndShardDescription(hashCode, null)).ClientAndDescription;
            }
            else
            {
                Tuple<Boolean, HashRangeStartClientAndShardDescription> nextLess = tree.GetNextLessThan(new HashRangeStartClientAndShardDescription(hashCode, null));
                if (nextLess.Item1 == true)
                {
                    return nextLess.Item2.ClientAndDescription;
                }
                else
                {
                    if (tree.Contains(new HashRangeStartClientAndShardDescription(Int32.MaxValue, null)) == true)
                    {
                        return tree.Get(new HashRangeStartClientAndShardDescription(Int32.MaxValue, null)).ClientAndDescription;
                    }
                    else
                    {
                        return tree.GetNextLessThan(new HashRangeStartClientAndShardDescription(Int32.MaxValue, null)).Item2.ClientAndDescription;
                    }
                }
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the ShardClientManager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~ShardClientManager()
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
                    configurationLock.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
