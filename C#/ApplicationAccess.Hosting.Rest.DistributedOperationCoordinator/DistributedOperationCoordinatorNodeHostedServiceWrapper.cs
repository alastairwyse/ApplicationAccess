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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence.SqlServer;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator
{
    /// <summary>
    /// Wraps an instance of <see cref="DistributedOperationCoordinatorNode{TClientConfiguration, TClientConfigurationJsonSerializer}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="DistributedOperationCoordinatorNode{TClientConfiguration, TClientConfigurationJsonSerializer}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class DistributedOperationCoordinatorNodeHostedServiceWrapper : IHostedService
    {
        // Members passed in via dependency injection
        protected AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions;
        protected ShardConfigurationRefreshOptions shardConfigurationRefreshOptions;
        protected ShardConnectionOptions shardConnectionOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected DistributedOperationCoordinatorHolder distributedOperationCoordinatorHolder;
        protected ILoggerFactory loggerFactory;
        protected ILogger<DistributedOperationCoordinatorNodeHostedServiceWrapper> logger;

        /// <summary>Manages the clients which connect to shards in the distributed AccessManager implementation.</summary>
        protected ShardClientManager<AccessManagerRestClientConfiguration> shardClientManager;
        /// <summary>>Defines how often the shard configuration will be refreshed.</summary>
        protected LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy shardConfigurationRefreshStrategy;
        /// <summary>Performs the underlying processing of operations.</summary>
        protected DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> distributedOperationCoordinator;
        /// <summary>Used to read shard configuration from a Microsoft SQL Server database.</summary>
        protected SqlServerShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected SqlServerMetricLogger metricLogger;
        /// <summary>The <see cref="DistributedOperationCoordinatorNode{TClientConfiguration, TClientConfigurationJsonSerializer}"/></summary>
        protected DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> distributedOperationCoordinatorNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator.DistributedOperationCoordinatorNodeHostedServiceWrapper class.
        /// </summary>
        public DistributedOperationCoordinatorNodeHostedServiceWrapper
        (
            IOptions<AccessManagerSqlServerConnectionOptions> accessManagerSqlServerConnectionOptions,
            IOptions<ShardConfigurationRefreshOptions> shardConfigurationRefreshOptions,
            IOptions<ShardConnectionOptions> shardConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            DistributedOperationCoordinatorHolder distributedOperationCoordinatorHolder,
            ILoggerFactory loggerFactory,
            ILogger<DistributedOperationCoordinatorNodeHostedServiceWrapper> logger
        )
        {
            this.accessManagerSqlServerConnectionOptions = accessManagerSqlServerConnectionOptions.Value;
            this.shardConfigurationRefreshOptions = shardConfigurationRefreshOptions.Value;
            this.shardConnectionOptions = shardConnectionOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.distributedOperationCoordinatorHolder = distributedOperationCoordinatorHolder;
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
                accessManagerSqlServerConnectionOptions,
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
                logger.LogInformation($"Starting {nameof(metricLogger)}...");
                metricLogger.Start();
                logger.LogInformation($"Completed starting {nameof(metricLogger)}.");
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
                logger.LogInformation($"Stopping {nameof(metricLogger)}...");
                metricLogger.Stop();
                logger.LogInformation($"Completed stopping {nameof(metricLogger)}.");
            }
            logger.LogInformation($"Disposing objects...");
            shardConfigurationSetPersister.Dispose();
            shardClientManager.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
                metricLogger.Dispose();
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
            AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions,
            ShardConfigurationRefreshOptions shardConfigurationRefreshOptions,
            ShardConnectionOptions shardConnectionOptions,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            // Setup the database connection for the shard configuration
            var accessManagerConnectionStringBuilder = new SqlConnectionStringBuilder();
            accessManagerConnectionStringBuilder.DataSource = accessManagerSqlServerConnectionOptions.DataSource;
            // TODO: Need to enable this once I find a way to inject cert details etc into
            accessManagerConnectionStringBuilder.Encrypt = false;
            accessManagerConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            accessManagerConnectionStringBuilder.InitialCatalog = accessManagerSqlServerConnectionOptions.InitialCatalog;
            accessManagerConnectionStringBuilder.UserID = accessManagerSqlServerConnectionOptions.UserId;
            accessManagerConnectionStringBuilder.Password = accessManagerSqlServerConnectionOptions.Password;
            String accessManagerConnectionString = accessManagerConnectionStringBuilder.ConnectionString;

            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                throw new Exception($"Configuration option '{nameof(metricLoggingOptions.MetricLoggingEnabled)}' must be set true for the DistributedOperationCoordinatorNode." );
            }
            else
            {
                // Setup metric logging
                String sqlServerMetricLoggerCategoryName = "DistributedOperationCoordinatorNode";
                if (metricLoggingOptions.MetricCategorySuffix != "")
                {
                    sqlServerMetricLoggerCategoryName = $"{sqlServerMetricLoggerCategoryName}-{metricLoggingOptions.MetricCategorySuffix}";
                }
                MetricBufferProcessingOptions metricBufferProcessingOptions = metricLoggingOptions.MetricBufferProcessing;
                switch (metricBufferProcessingOptions.BufferProcessingStrategy)
                {
                    case MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor:
                        metricLoggerBufferProcessingStrategy = new SizeLimitedBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit);
                        break;
                    case MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor:
                        metricLoggerBufferProcessingStrategy = new LoopingWorkerThreadBufferProcessor(metricBufferProcessingOptions.DequeueOperationLoopInterval);
                        break;
                    case MetricBufferProcessingStrategyImplementation.SizeLimitedLoopingWorkerThreadHybridBufferProcessor:
                        metricLoggerBufferProcessingStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit, metricBufferProcessingOptions.DequeueOperationLoopInterval);
                        break;
                    default:
                        throw new Exception($"Encountered unhandled {nameof(MetricBufferProcessingStrategyImplementation)} '{metricBufferProcessingOptions.BufferProcessingStrategy}' while attempting to create {nameof(DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>)} constructor parameters.");
                }
                MetricsSqlServerConnectionOptions metricsSqlServerConnectionOptions = metricLoggingOptions.MetricsSqlServerConnection;
                var metricsConnectionStringBuilder = new SqlConnectionStringBuilder();
                metricsConnectionStringBuilder.DataSource = metricsSqlServerConnectionOptions.DataSource;
                // TODO: Need to enable this once I find a way to inject cert details etc into
                metricsConnectionStringBuilder.Encrypt = false;
                metricsConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
                metricsConnectionStringBuilder.InitialCatalog = metricsSqlServerConnectionOptions.InitialCatalog;
                metricsConnectionStringBuilder.UserID = metricsSqlServerConnectionOptions.UserId;
                metricsConnectionStringBuilder.Password = metricsSqlServerConnectionOptions.Password;
                String metricsConnectionString = metricsConnectionStringBuilder.ConnectionString;
                IApplicationLogger metricLoggerLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<SqlServerMetricLogger>()
                );
                metricLogger = new SqlServerMetricLogger
                (
                    sqlServerMetricLoggerCategoryName,
                    metricsConnectionString,
                    metricsSqlServerConnectionOptions.RetryCount,
                    metricsSqlServerConnectionOptions.RetryInterval,
                    metricsSqlServerConnectionOptions.OperationTimeout,
                    metricLoggerBufferProcessingStrategy,
                    IntervalMetricBaseTimeUnit.Nanosecond,
                    true,
                    metricLoggerLogger
                );

                // Setup the DistributedAccessManagerAsyncClientFactory (required constructor parameter for ShardClientManager)
                IApplicationLogger clientFactoryLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<DistributedAccessManagerAsyncClientFactory<String, String, String, String>>()
                );
                var clientFactory = new DistributedAccessManagerAsyncClientFactory<String, String, String, String>
                (
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    shardConnectionOptions.RetryCount,
                    shardConnectionOptions.RetryInterval,
                    clientFactoryLogger,
                    metricLogger
                );

                // Setup the DistributedOperationCoordinatorNode constructor parameters
                var clientConfigurationJsonSerializer = new AccessManagerRestClientConfigurationJsonSerializer();
                IApplicationLogger shardConfigurationSetPersisterLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<SqlServerShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>>()
                );
                shardConfigurationSetPersister = new SqlServerShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>
                (
                    accessManagerConnectionString,
                    accessManagerSqlServerConnectionOptions.RetryCount,
                    accessManagerSqlServerConnectionOptions.RetryInterval,
                    accessManagerSqlServerConnectionOptions.OperationTimeout,
                    clientConfigurationJsonSerializer,
                    shardConfigurationSetPersisterLogger
                );
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
                shardConfigurationRefreshStrategy = new LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy(shardConfigurationRefreshOptions.RefreshInterval);
                distributedOperationCoordinator = new DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration>
                (
                    shardClientManager,
                    hashCodeGenerator,
                    hashCodeGenerator,
                    metricLogger
                );
            }
        }

        /// <summary>
        /// Sets the 'distributedOperationCoordinatorNode' member on the 'holder' class instance (e.g. <see cref="EntityEventProcessorHolder"/>).
        /// </summary>
        protected virtual void SetupHolderClasses()
        {
            distributedOperationCoordinatorHolder.DistributedOperationCoordinator = distributedOperationCoordinatorNode;
        }

        #endregion
    }
}
