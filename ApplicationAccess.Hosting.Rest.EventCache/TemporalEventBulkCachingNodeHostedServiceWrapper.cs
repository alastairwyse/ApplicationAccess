﻿/*
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
using ApplicationAccess.Hosting.Metrics;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest.EventCache
{
    /// <summary>
    /// Wraps an instance of <see cref="TemporalEventBulkCachingNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() calls constructs an <see cref="AccessManagerTemporalEventBulkCache{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class TemporalEventBulkCachingNodeHostedServiceWrapper : NodeHostedServiceWrapperBase, IHostedService
    {
        /// <summary>The category to use for metric logging.</summary>
        protected const String metricLoggerCategoryName = "ApplicationAccessEventCachingNode";

        #pragma warning disable 1591

        // Members passed in via dependency injection
        protected MetricLoggingOptions metricLoggingOptions;
        protected EventCachingOptions eventCachingOptions;
        protected TemporalEventQueryProcessorHolder temporalEventQueryProcessorHolder;
        protected TemporalEventBulkPersisterHolder temporalEventBulkPersisterHolder;
        protected TripSwitchActuator tripSwitchActuator;
        protected ILoggerFactory loggerFactory;

        #pragma warning restore 1591

        /// <summary>The <see cref="TemporalEventBulkCachingNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected TemporalEventBulkCachingNode<String, String, String, String> cachingNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.EventCache.TemporalEventBulkCachingNodeHostedServiceWrapper class.
        /// </summary>
        public TemporalEventBulkCachingNodeHostedServiceWrapper
        (
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            IOptions<EventCachingOptions> eventCachingOptions,
            TemporalEventQueryProcessorHolder temporalEventQueryProcessorHolder,
            TemporalEventBulkPersisterHolder temporalEventBulkPersisterHolder,
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<TemporalEventBulkCachingNodeHostedServiceWrapper> logger
        ) : base(logger)
        {
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.eventCachingOptions = eventCachingOptions.Value;
            this.temporalEventQueryProcessorHolder = temporalEventQueryProcessorHolder;
            this.temporalEventBulkPersisterHolder = temporalEventBulkPersisterHolder;
            this.tripSwitchActuator = tripSwitchActuator;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {nameof(TemporalEventBulkCachingNodeHostedServiceWrapper)}...");

            logger.LogInformation($"Constructing TemporalEventBulkCachingNode instance...");

            // Initialize the TemporalEventBulkCachingNode constructor parameter members from configuration
            InitializeCachingNodeConstructorParameters(metricLoggingOptions);

            // Create the TemporalEventBulkCachingNode
            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                cachingNode = new TemporalEventBulkCachingNode<String, String, String, String>(eventCachingOptions.CachedEventCount.Value, new NullMetricLogger());
            }
            else
            {
                cachingNode = new TemporalEventBulkCachingNode<String, String, String, String>(eventCachingOptions.CachedEventCount.Value, metricLogger);
            }

            // Set the TemporalEventBulkCachingNode on the 'holder' classes
            temporalEventQueryProcessorHolder.TemporalEventQueryProcessor = cachingNode;
            temporalEventBulkPersisterHolder.TemporalEventBulkPersister = cachingNode;

            logger.LogInformation($"Completed constructing TemporalEventBulkCachingNode instance.");

            // Start buffer flushing/processing
            // Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLogger != null)
            {
                StartMetricLogging();
            }

            logger.LogInformation($"Completed starting {nameof(TemporalEventBulkCachingNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {nameof(TemporalEventBulkCachingNodeHostedServiceWrapper)}...");

            // Stop and clear buffer flushing/processing
            if (metricLogger != null)
            {
                StopMetricLogging();
            }
            logger.LogInformation($"Disposing objects...");
            if (metricLogger != null)
            {
                DisposeMetricLogger();
            }
            cachingNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {nameof(TemporalEventBulkCachingNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the 'cachingNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeCachingNodeConstructorParameters(MetricLoggingOptions metricLoggingOptions)
        {
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
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
            }
        }
    }
}
