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
    public class DistributedOperationRouterNodeHostedServiceWrapper : NodeHostedServiceWrapperBase, IHostedService
    {
        /// <summary>The category to use for metric logging.</summary>
        protected const String metricLoggerCategoryName = "ApplicationAccessDistributedOperationRoutingNode";

        #pragma warning disable 1591

        // Members passed in via dependency injection
        protected ShardRoutingOptions shardRoutingOptions;
        protected ShardConnectionOptions shardConnectionOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected AsyncQueryProcessorHolder asyncQueryProcessorHolder;
        protected AsyncEventProcessorHolder asyncEventProcessorHolder;
        protected DistributedAsyncQueryProcessorHolder distributedAsyncQueryProcessorHolder;
        protected DistributedOperationRouterHolder distributedOperationRouterHolder;
        protected TripSwitchActuator tripSwitchActuator;
        protected ILoggerFactory loggerFactory;

        #pragma warning restore 1591

        /// <summary>The <see cref="HttpClient"/> used by the shard clients.</summary>
        protected HttpClient httpClient;
        /// <summary>Manages the clients which connect to the source and target shards.</summary>
        protected ShardClientManager<AccessManagerRestClientConfiguration> shardClientManager;
        /// <summary>Performs the underlying processing of operations.</summary>
        protected DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> distributedOperationRouter;
        /// <summary>The <see cref="DistributedOperationRouterNode{TClientConfiguration}"/></summary>
        protected DistributedOperationRouterNode<AccessManagerRestClientConfiguration> distributedOperationRouterNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.DistributedOperationRouterNodeHostedServiceWrapper class.
        /// </summary>
        public DistributedOperationRouterNodeHostedServiceWrapper
        (
            IOptions<ShardRoutingOptions> shardRoutingOptions,
            IOptions<ShardConnectionOptions> shardConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            AsyncQueryProcessorHolder asyncQueryProcessorHolder,
            AsyncEventProcessorHolder asyncEventProcessorHolder,
            DistributedAsyncQueryProcessorHolder distributedAsyncQueryProcessorHolder,
            DistributedOperationRouterHolder distributedOperationRouterHolder,
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<DistributedOperationRouterNodeHostedServiceWrapper> logger
        ) : base(logger)
        {
            this.shardRoutingOptions = shardRoutingOptions.Value;
            this.shardConnectionOptions = shardConnectionOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.asyncQueryProcessorHolder = asyncQueryProcessorHolder;
            this.asyncEventProcessorHolder = asyncEventProcessorHolder;
            this.distributedAsyncQueryProcessorHolder = distributedAsyncQueryProcessorHolder;
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

            // Start buffer flushing/processing
            //   Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLoggingOptions.Enabled.Value == true)
            {
                StartMetricLogging();
            }

            logger.LogInformation($"Completed starting {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {this.GetType().Name}...");

            if (metricLoggingOptions.Enabled.Value == true)
            {
                StopMetricLogging();
            }
            logger.LogInformation($"Disposing objects...");
            shardClientManager.Dispose();
            httpClient.Dispose();
            if (metricLoggingOptions.Enabled.Value == true)
            {
                DisposeMetricLogger();
            }
            distributedOperationRouter.Dispose();
            logger.LogInformation($"Completed disposing objects.");

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
            if (metricLoggingOptions.Enabled.Value == false)
            {
                throw new Exception($"Configuration option '{MetricLoggingOptions.MetricLoggingOptionsName}.{nameof(metricLoggingOptions.Enabled)}' must be set true for the DistributedOperationRouterNode.");
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
                    shardRoutingOptions.DataElementType.Value, Operation.Query, shardRoutingOptions.SourceShardHashRangeStart.Value, sourceQueryShardClientConfiguration
                );
                var sourceEventShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.DataElementType.Value, Operation.Event, shardRoutingOptions.SourceShardHashRangeStart.Value, sourceEventShardClientConfiguration
                );
                var targetQueryShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.DataElementType.Value, Operation.Query, shardRoutingOptions.TargetShardHashRangeStart.Value, targetQueryShardClientConfiguration
                );
                var targetEventShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>
                (
                    shardRoutingOptions.DataElementType.Value, Operation.Event, shardRoutingOptions.TargetShardHashRangeStart.Value, targetEventShardClientConfiguration
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
                    shardRoutingOptions.SourceShardHashRangeStart.Value, 
                    shardRoutingOptions.SourceShardHashRangeEnd.Value, 
                    shardRoutingOptions.TargetShardHashRangeStart.Value, 
                    shardRoutingOptions.TargetShardHashRangeEnd.Value,
                    shardRoutingOptions.DataElementType.Value,
                    shardClientManager,
                    hashCodeGenerator,
                    hashCodeGenerator,
                    shardRoutingOptions.RoutingInitiallyOn.Value, 
                    metricLogger
                );
            }
        }

        /// <summary>
        /// Sets the 'distributedOperationRouterNode' member the various 'holder' class instances (e.g. <see cref="AsyncQueryProcessorHolder"/>).
        /// </summary>
        protected virtual void SetupHolderClasses()
        {
            asyncQueryProcessorHolder.AsyncQueryProcessor = distributedOperationRouterNode;
            asyncEventProcessorHolder.AsyncEventProcessor = distributedOperationRouterNode;
            distributedAsyncQueryProcessorHolder.DistributedAsyncQueryProcessor = distributedOperationRouterNode;
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
