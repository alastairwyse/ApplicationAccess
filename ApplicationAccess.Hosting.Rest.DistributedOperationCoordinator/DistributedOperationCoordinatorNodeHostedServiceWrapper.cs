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
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Hosting.Metrics;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Persistence;
using ApplicationAccess.Hosting.Persistence.Sql;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Utilities;
using ApplicationMetrics.MetricLoggers;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator
{
    /// <summary>
    /// Wraps an instance of <see cref="DistributedOperationCoordinatorNode{TClientConfiguration, TClientConfigurationJsonSerializer}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="DistributedOperationCoordinatorNode{TClientConfiguration, TClientConfigurationJsonSerializer}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class DistributedOperationCoordinatorNodeHostedServiceWrapper : NodeHostedServiceWrapperBase, IHostedService
    {
        /// <summary>The category to use for metric logging.</summary>
        protected const String metricLoggerCategoryName = "DistributedOperationCoordinatorNode";

        #pragma warning disable 1591

        // Members passed in via dependency injection
        protected AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions;
        protected ShardConfigurationRefreshOptions shardConfigurationRefreshOptions;
        protected ShardConnectionOptions shardConnectionOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected AsyncQueryProcessorHolder asyncQueryProcessorHolder;
        protected AsyncEventProcessorHolder asyncEventProcessorHolder;
        protected TripSwitchActuator tripSwitchActuator;
        protected ILoggerFactory loggerFactory;

        #pragma warning restore 1591

        /// <summary>The <see cref="HttpClient"/> used by the shard clients.</summary>
        protected HttpClient httpClient;
        /// <summary>Manages the clients which connect to shards in the distributed AccessManager implementation.</summary>
        protected ShardClientManager<AccessManagerRestClientConfiguration> shardClientManager;
        /// <summary>>Defines how often the shard configuration will be refreshed.</summary>
        protected LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy shardConfigurationRefreshStrategy;
        /// <summary>Performs the underlying processing of operations.</summary>
        protected DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> distributedOperationCoordinator;
        /// <summary>The <see cref="DistributedOperationCoordinatorNode{TClientConfiguration, TClientConfigurationJsonSerializer}"/></summary>
        protected DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> distributedOperationCoordinatorNode;
        /// <summary>Used to read shard configuration from a SQL database.</summary>
        protected IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator.DistributedOperationCoordinatorNodeHostedServiceWrapper class.
        /// </summary>
        public DistributedOperationCoordinatorNodeHostedServiceWrapper
        (
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
            IOptions<ShardConfigurationRefreshOptions> shardConfigurationRefreshOptions,
            IOptions<ShardConnectionOptions> shardConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            AsyncQueryProcessorHolder asyncQueryProcessorHolder,
            AsyncEventProcessorHolder asyncEventProcessorHolder,
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<DistributedOperationCoordinatorNodeHostedServiceWrapper> logger
        ) : base(logger)
        {
            this.accessManagerSqlDatabaseConnectionOptions = accessManagerSqlDatabaseConnectionOptions.Value;
            this.shardConfigurationRefreshOptions = shardConfigurationRefreshOptions.Value;
            this.shardConnectionOptions = shardConnectionOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.asyncQueryProcessorHolder = asyncQueryProcessorHolder;
            this.asyncEventProcessorHolder = asyncEventProcessorHolder;
            this.tripSwitchActuator = tripSwitchActuator;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {this.GetType().Name}...");

            logger.LogInformation($"Constructing DistributedOperationCoordinatorNode instance...");

            // Initialize the DistributedOperationCoordinatorNode constructor parameter members from configuration
            InitializeDistributedOperationCoordinatorNodeConstructorParameters
            (
                accessManagerSqlDatabaseConnectionOptions,
                shardConfigurationRefreshOptions,
                shardConnectionOptions, 
                metricLoggingOptions,
                loggerFactory
            );

            // Create the DistributedOperationCoordinatorNode
            distributedOperationCoordinatorNode = new DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>
            (
                shardClientManager, 
                shardConfigurationRefreshStrategy, 
                distributedOperationCoordinator, 
                shardConfigurationSetPersister
            );

            // Set the DistributedOperationCoordinatorNode on the 'holder' classes
            SetupHolderClasses();

            logger.LogInformation($"Completed constructing DistributedOperationCoordinatorNode instance.");

            // Start buffer flushing/processing
            //   Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                StartMetricLogging();
            }
            // Start the refresh strategy
            logger.LogInformation($"Starting {nameof(shardConfigurationRefreshStrategy)}...");
            shardConfigurationRefreshStrategy.Start();
            logger.LogInformation($"Completed starting {nameof(shardConfigurationRefreshStrategy)}.");

            logger.LogInformation($"Completed starting {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {this.GetType().Name}...");

            // Stop the refresh strategy
            logger.LogInformation($"Stopping {nameof(shardConfigurationRefreshStrategy)}...");
            shardConfigurationRefreshStrategy.Stop();
            logger.LogInformation($"Completed stopping {nameof(shardConfigurationRefreshStrategy)}.");
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                StopMetricLogging();
            }
            logger.LogInformation($"Disposing objects...");
            if (shardConfigurationSetPersister is IDisposable)
            {
                ((IDisposable)shardConfigurationSetPersister).Dispose();
            }
            shardClientManager.Dispose();
            httpClient.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                DisposeMetricLogger();
            }
            distributedOperationCoordinatorNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'distributedOperationCoordinatorNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeDistributedOperationCoordinatorNodeConstructorParameters
        (
            AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions,
            ShardConfigurationRefreshOptions shardConfigurationRefreshOptions,
            ShardConnectionOptions shardConnectionOptions,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                throw new Exception($"Configuration option '{nameof(metricLoggingOptions.MetricLoggingEnabled)}' must be set true for the DistributedOperationCoordinatorNode." );
            }
            else
            {
                IApplicationLogger metricBufferProcessorLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter(loggerFactory.CreateLogger<WorkerThreadBufferProcessorBase>());
                IApplicationLogger metricLoggerLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter(loggerFactory.CreateLogger<MetricLoggerBuffer>());
                var metricLoggerFactory = new MetricLoggerFactory();
                (metricLogger, metricLoggerBufferProcessingStrategy) = metricLoggerFactory.CreateMetricLoggerAndBufferProcessor
                (
                    metricLoggingOptions,
                    metricLoggerCategoryName,
                    () => { return null; },
                    tripSwitchActuator,
                    metricBufferProcessorLogger,
                    metricLoggerLogger,
                    IntervalMetricBaseTimeUnit.Nanosecond,
                    true
                );

                // Setup the DistributedAccessManagerAsyncClientFactory (required constructor parameter for ShardClientManager)
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMilliseconds(shardConnectionOptions.ConnectionTimeout.Value);
                IApplicationLogger clientFactoryLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<DistributedAccessManagerAsyncClientFactory<String, String, String, String>>()
                );
                var clientFactory = new DistributedAccessManagerAsyncClientFactory<String, String, String, String>
                (
                    httpClient, 
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    shardConnectionOptions.RetryCount.Value,
                    shardConnectionOptions.RetryInterval.Value,
                    clientFactoryLogger,
                    metricLogger
                );

                // Setup the DistributedOperationCoordinatorNode constructor parameters
                var clientConfigurationJsonSerializer = new AccessManagerRestClientConfigurationJsonSerializer();
                IApplicationLogger shardConfigurationSetPersisterLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>>()
                );

                // Setup the persister to read the shard configuration
                if (accessManagerSqlDatabaseConnectionOptions.DatabaseType != DatabaseType.SqlServer)
                {
                    throw new Exception($"Configuration option '{nameof(accessManagerSqlDatabaseConnectionOptions.DatabaseType)}' must be set to '{DatabaseType.SqlServer}' for the DistributedOperationCoordinatorNode.");
                }
                var databaseConnectionParametersParser = new SqlDatabaseConnectionParametersParser();
                SqlDatabaseConnectionParametersBase databaseConnectionParameters = databaseConnectionParametersParser.Parse
                (
                    accessManagerSqlDatabaseConnectionOptions.DatabaseType.Value,
                    accessManagerSqlDatabaseConnectionOptions.ConnectionParameters,
                    AccessManagerSqlDatabaseConnectionOptions.AccessManagerSqlDatabaseConnectionOptionsName
                );
                var shardConfigurationSetPersisterFactory = new SqlShardConfigurationSetPersisterFactory<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>
                (
                    clientConfigurationJsonSerializer,
                    shardConfigurationSetPersisterLogger
                );
                shardConfigurationSetPersister = shardConfigurationSetPersisterFactory.GetPersister(databaseConnectionParameters);
                var hashCodeGenerator = new DefaultStringHashCodeGenerator();
                // Read the initial shard configuration
                logger.LogInformation($"Reading initial shard configuration...");
                ShardConfigurationSet<AccessManagerRestClientConfiguration> initialConfiguration = null;
                try
                {
                    initialConfiguration = shardConfigurationSetPersister.Read();
                }
                catch (Exception e)
                {
                    throw new ShardConfigurationRefreshException("Failed to read shard configuration from persistent storage.", e);
                }
                logger.LogInformation($"Completed reading initial shard configuration.");
                shardClientManager = new ShardClientManager<AccessManagerRestClientConfiguration>
                (
                    initialConfiguration, 
                    clientFactory,
                    hashCodeGenerator,
                    hashCodeGenerator,
                    metricLogger
                );
                IApplicationLogger refreshStrategyLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy>()
                );
                Action<ShardConfigurationRefreshException> shardConfigurationRefreshExceptionAction = (ShardConfigurationRefreshException shardConfigurationRefreshException) =>
                {
                    tripSwitchActuator.Actuate();
                    try
                    {
                        refreshStrategyLogger.Log(ApplicationLogging.LogLevel.Critical, "Exception occurred when refreshing shard configuration.", shardConfigurationRefreshException);
                        refreshStrategyLogger.Log(ApplicationLogging.LogLevel.Critical, "Tripswitch has been actuated due to an unrecoverable error whilst refreshing the shard configuration.");
                    }
                    catch
                    {
                    }
                };
                shardConfigurationRefreshStrategy = new LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy
                (
                    shardConfigurationRefreshOptions.RefreshInterval.Value,
                    shardConfigurationRefreshExceptionAction
                );
                distributedOperationCoordinator = new DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration>(shardClientManager, metricLogger);
            }
        }

        /// <summary>
        /// Sets the 'distributedOperationCoordinatorNode' member the various 'holder' class instances (e.g. <see cref="AsyncQueryProcessorHolder"/>).
        /// </summary>
        protected virtual void SetupHolderClasses()
        {
            asyncQueryProcessorHolder.AsyncQueryProcessor = distributedOperationCoordinatorNode;
            asyncEventProcessorHolder.AsyncEventProcessor = distributedOperationCoordinatorNode;
        }

        #endregion
    }
}
