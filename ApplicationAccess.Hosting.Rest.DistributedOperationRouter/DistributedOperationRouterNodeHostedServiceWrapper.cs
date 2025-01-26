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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting;
using ApplicationAccess.Hosting.Metrics;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Utilities;
using ApplicationMetrics.MetricLoggers;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter
{
    /// <summary>
    /// Wraps an instance of <see cref="DistributedOperationRouterNode{TClientConfiguration}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="DistributedOperationRouterNode{TClientConfiguration}"/> instance (and its constructor parameters) from configuration, whist StopAsync() calls Dispose(), etc.</remarks>
    public class DistributedOperationRouterNodeHostedServiceWrapper : IHostedService
    {
        // Members passed in via dependency injection
        protected ShardRoutingOptions shardRoutingOptions;
        protected ShardConnectionOptions shardConnectionOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected DistributedOperationRouterHolder distributedOperationRouterHolder;
        protected TripSwitchActuator tripSwitchActuator;
        protected ILoggerFactory loggerFactory;
        protected ILogger<DistributedOperationRouterNodeHostedServiceWrapper> logger;

        /// <summary>The <see cref="HttpClient"/> used by the shard clients.</summary>
        protected HttpClient httpClient;
        /// <summary>Manages the clients which connect to the source and target shards.</summary>
        protected ShardClientManager<AccessManagerRestClientConfiguration> shardClientManager;
        /// <summary>Performs the underlying processing of operations.</summary>
        protected DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> distributedOperationRouter;
        /// <summary>The <see cref="DistributedOperationRouterNode{TClientConfiguration}"/></summary>
        protected DistributedOperationRouterNode<AccessManagerRestClientConfiguration> distributedOperationRouterNode;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected MetricLoggerBuffer metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.DistributedOperationRouterNodeHostedServiceWrapper class.
        /// </summary>
        public DistributedOperationRouterNodeHostedServiceWrapper
        (
            IOptions<ShardRoutingOptions> shardRoutingOptions,
            IOptions<ShardConnectionOptions> shardConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            DistributedOperationRouterHolder distributedOperationRouterHolder,
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<DistributedOperationRouterNodeHostedServiceWrapper> logger
        )
        {
            this.shardRoutingOptions = shardRoutingOptions.Value;
            this.shardConnectionOptions = shardConnectionOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.distributedOperationRouterHolder = distributedOperationRouterHolder;
            this.tripSwitchActuator = tripSwitchActuator;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {this.GetType().Name}...");

            logger.LogInformation($"Constructing DistributedOperationRouterNode instance...");

            // Initialize the DistributedOperationRouterNode constructor parameter members from configuration
            InitializeDistributedOperationRouterNodeConstructorParameters
            (
                shardRoutingOptions,
                shardConnectionOptions,
                metricLoggingOptions,
                loggerFactory
            );

            // Create the DistributedOperationRouterNode
            distributedOperationRouterNode = new DistributedOperationRouterNode<AccessManagerRestClientConfiguration>(distributedOperationRouter);

            // Set the DistributedOperationRouterNode on the 'holder' classes
            SetupHolderClasses();

            logger.LogInformation($"Completed constructing DistributedOperationRouterNode instance.");

            logger.LogInformation($"Completed starting {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {this.GetType().Name}...");

            shardClientManager.Dispose();
            httpClient.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
                metricLogger.Dispose();
            }

            logger.LogInformation($"Completed stopping {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'distributedOperationRouterNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeDistributedOperationRouterNodeConstructorParameters
        (
            ShardRoutingOptions shardRoutingOptions,
            ShardConnectionOptions shardConnectionOptions,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                throw new Exception($"Configuration option '{nameof(metricLoggingOptions.MetricLoggingEnabled)}' must be set true for the DistributedOperationRouterNode.");
            }
            else
            {
                // Setup metric logging
                String metricLoggerCategoryName = "DistributedOperationRoutingNode";
                if (metricLoggingOptions.MetricCategorySuffix != "")
                {
                    metricLoggerCategoryName = $"{metricLoggerCategoryName}-{metricLoggingOptions.MetricCategorySuffix}";
                }
                MetricBufferProcessingOptions metricBufferProcessingOptions = metricLoggingOptions.MetricBufferProcessing;
                var metricsBufferProcessorFactory = new MetricsBufferProcessorFactory();
                IApplicationLogger metricBufferProcessorLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<WorkerThreadBufferProcessorBase>()
                );
                Action<Exception> bufferProcessingExceptionAction = metricsBufferProcessorFactory.GetBufferProcessingExceptionAction
                (
                    MetricBufferProcessingFailureAction.ReturnServiceUnavailable,
                    // Parameter 'metricLoggingComponentRetrievalFunction' is not used when 'processingFailureAction' is set to 'ReturnServiceUnavailable', hence can set to null
                    () => { return null; },
                    tripSwitchActuator,
                    metricBufferProcessorLogger
                );
                metricLoggerBufferProcessingStrategy = metricsBufferProcessorFactory.GetBufferProcessor(metricBufferProcessingOptions, bufferProcessingExceptionAction, false);
                var databaseConnectionParametersParser = new SqlDatabaseConnectionParametersParser();
                SqlDatabaseConnectionParametersBase metricsDatabaseConnectionParameters = databaseConnectionParametersParser.Parse
                (
                    metricLoggingOptions.MetricsSqlDatabaseConnection.DatabaseType,
                    metricLoggingOptions.MetricsSqlDatabaseConnection.ConnectionParameters,
                    MetricsSqlDatabaseConnectionOptions.MetricsSqlDatabaseConnection
                );
                IApplicationLogger metricLoggerLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<MetricLoggerBuffer>()
                );
                var metricLoggerFactory = new SqlMetricLoggerFactory
                (
                    metricLoggerCategoryName,
                    metricLoggerBufferProcessingStrategy,
                    IntervalMetricBaseTimeUnit.Nanosecond,
                    true,
                    metricLoggerLogger
                );
                metricLogger = metricLoggerFactory.GetMetricLogger(metricsDatabaseConnectionParameters);

                // Setup the DistributedAccessManagerAsyncClientFactory (required constructor parameter for ShardClientManager)
                httpClient = new HttpClient();
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
                    shardConnectionOptions.RetryCount,
                    shardConnectionOptions.RetryInterval,
                    clientFactoryLogger,
                    metricLogger
                );

                // Create the initial shard configuration
                Uri sourceQueryShardUrl = ValidateAndCreateUri(shardRoutingOptions.SourceQueryShardBaseUrl, nameof(shardRoutingOptions.SourceQueryShardBaseUrl));
                Uri sourceEventShardUrl = ValidateAndCreateUri(shardRoutingOptions.SourceEventShardBaseUrl, nameof(shardRoutingOptions.SourceEventShardBaseUrl));
                Uri targetQueryShardUrl = ValidateAndCreateUri(shardRoutingOptions.TargetQueryShardBaseUrl, nameof(shardRoutingOptions.TargetQueryShardBaseUrl));
                Uri targetEventShardUrl = ValidateAndCreateUri(shardRoutingOptions.TargetEventShardBaseUrl, nameof(shardRoutingOptions.TargetEventShardBaseUrl));
                var sourceQueryShardClientConfiguration = new AccessManagerRestClientConfiguration(sourceQueryShardUrl);
                var sourceEventShardClientConfiguration = new AccessManagerRestClientConfiguration(sourceEventShardUrl);
                var targetQueryShardClientConfiguration = new AccessManagerRestClientConfiguration(targetQueryShardUrl);
                var targetEventShardClientConfiguration = new AccessManagerRestClientConfiguration(targetEventShardUrl);
                var sourceQueryShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.DataElementType, Operation.Query, shardRoutingOptions.SourceShardHashRangeStart, sourceQueryShardClientConfiguration
                );
                var sourceEventShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.DataElementType, Operation.Event, shardRoutingOptions.SourceShardHashRangeStart, sourceEventShardClientConfiguration
                );
                var targetQueryShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.DataElementType, Operation.Query, shardRoutingOptions.TargetShardHashRangeStart, targetQueryShardClientConfiguration
                );
                var targetEventShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.DataElementType, Operation.Event, shardRoutingOptions.TargetShardHashRangeStart, targetEventShardClientConfiguration
                );
                var initialConfiguration = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
                (
                    new List<ShardConfiguration<AccessManagerRestClientConfiguration>>
                    {
                        sourceQueryShardConfiguration,
                        sourceEventShardConfiguration,
                        targetQueryShardConfiguration,
                        targetEventShardConfiguration
                    }
                );
                var hashCodeGenerator = new DefaultStringHashCodeGenerator();
                shardClientManager = new ShardClientManager<AccessManagerRestClientConfiguration>
                (
                    initialConfiguration,
                    clientFactory,
                    hashCodeGenerator,
                    hashCodeGenerator,
                    metricLogger
                );
                distributedOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.SourceShardHashRangeStart, 
                    shardRoutingOptions.SourceShardHashRangeEnd, 
                    shardRoutingOptions.TargetShardHashRangeStart, 
                    shardRoutingOptions.TargetShardHashRangeEnd,
                    shardRoutingOptions.DataElementType,
                    shardClientManager,
                    hashCodeGenerator,
                    hashCodeGenerator,
                    shardRoutingOptions.RoutingInitiallyOn.Value, 
                    metricLogger
                );
            }
        }

        /// <summary>
        /// Sets the 'distributedOperationRouterNode' member on the 'holder' class instance (e.g. <see cref="EntityEventProcessorHolder"/>).
        /// </summary>
        protected virtual void SetupHolderClasses()
        {
            distributedOperationRouterHolder.DistributedOperationRouter = distributedOperationRouterNode;
        }

        /// <summary>
        /// Attempts to convert the specified string to a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uriString">The stringified Uri.</param>
        /// <param name="parameterName">The name of the parameter which contained the Uri.</param>
        /// <returns>The <see cref="Uri"/>.</returns>
        /// <exception cref="ArgumentException">If conversion fails.</exception>
        protected Uri ValidateAndCreateUri(String uriString, String parameterName)
        {
            try
            {
                return new Uri(uriString);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to create Uri from value '{uriString}' in parameter '{parameterName}'.", parameterName, e);
            }
        }

        #endregion
    }
}
