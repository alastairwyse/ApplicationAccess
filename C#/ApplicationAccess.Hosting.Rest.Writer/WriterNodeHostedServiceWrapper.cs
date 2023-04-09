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
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.SqlServer;
using ApplicationAccess.Hosting.Rest.Client;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;

namespace ApplicationAccess.Hosting.Rest.Writer
{    /// <summary>
     /// Wraps an instance of <see cref="WriterNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
     /// </summary>
     /// <remarks>StartAsync() constructs a <see cref="WriterNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class WriterNodeHostedServiceWrapper : IHostedService
    {
        // Members passed in via dependency injection
        protected AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions;
        protected EventBufferFlushingOptions eventBufferFlushingOptions;
        protected EventCacheConnectionOptions eventCacheConnectionOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected EntityEventProcessorHolder entityEventProcessorHolder;
        protected GroupEventProcessorHolder groupEventProcessorHolder;
        protected GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder;
        protected UserEventProcessorHolder userEventProcessorHolder;
        protected ILoggerFactory loggerFactory;
        protected ILogger<WriterNodeHostedServiceWrapper> logger;

        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the WriterNode.</summary>
        protected SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to persist changes to and load data from the AccessManager.</summary>
        protected SqlServerAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister;
        /// <summary>Interface to a cache for events which change the AccessManager.</summary>
        protected EventCacheClient<String, String, String, String> eventCacheClient;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected SqlServerMetricLogger metricLogger;
        /// <summary>The <see cref="WriterNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected WriterNode<String, String, String, String> writerNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Writer.WriterNodeHostedServiceWrapper class.
        /// </summary>
        public WriterNodeHostedServiceWrapper
        (
            IOptions<AccessManagerSqlServerConnectionOptions> accessManagerSqlServerConnectionOptions,
            IOptions<EventBufferFlushingOptions> eventBufferFlushingOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityEventProcessorHolder entityEventProcessorHolder,
            GroupEventProcessorHolder groupEventProcessorHolder,
            GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder,
            UserEventProcessorHolder userEventProcessorHolder,
            ILoggerFactory loggerFactory,
            ILogger<WriterNodeHostedServiceWrapper> logger
        )
        {
            this.accessManagerSqlServerConnectionOptions = accessManagerSqlServerConnectionOptions.Value;
            this.eventBufferFlushingOptions = eventBufferFlushingOptions.Value;
            this.eventCacheConnectionOptions = eventCacheConnectionOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.entityEventProcessorHolder = entityEventProcessorHolder;
            this.groupEventProcessorHolder = groupEventProcessorHolder;
            this.groupToGroupEventProcessorHolder = groupToGroupEventProcessorHolder;
            this.userEventProcessorHolder = userEventProcessorHolder;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {nameof(WriterNodeHostedServiceWrapper)}...");

            logger.LogInformation($"Constructing WriterNode instance...");

            // Initialize the WriterNode constructor parameter members from configuration
            InitializeWriterNodeConstructorParameters
            (
                accessManagerSqlServerConnectionOptions,
                eventBufferFlushingOptions,
                eventCacheConnectionOptions, 
                metricLoggingOptions,
                loggerFactory
            );


            // Create the WriterNode
            if (metricLoggingOptions.MetricLoggingEnabled == false)
            {
                writerNode = new WriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient);
            }
            else
            {
                writerNode = new WriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient, metricLogger);
            }

            // Set the WriterNode on the 'holder' classes
            entityEventProcessorHolder.EntityEventProcessor = writerNode;
            groupEventProcessorHolder.GroupEventProcessor = writerNode;
            groupToGroupEventProcessorHolder.GroupToGroupEventProcessor = writerNode;
            userEventProcessorHolder.UserEventProcessor = writerNode;

            logger.LogInformation($"Completed constructing WriterNode instance.");

            // Start buffer flushing/processing
            logger.LogInformation($"Starting {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Start();
            logger.LogInformation($"Completed starting {nameof(eventBufferFlushStrategy)}.");
            // Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                logger.LogInformation($"Starting {nameof(metricLogger)}...");
                metricLogger.Start();
                logger.LogInformation($"Completed starting {nameof(metricLogger)}.");
            }
            // Load the current state of the writerNode from storage
            logger.LogInformation($"Loading data into {nameof(writerNode)}...");
            writerNode.Load(false);
            logger.LogInformation($"Completed loading data into {nameof(writerNode)}.");

            logger.LogInformation($"Completed starting {nameof(WriterNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {nameof(WriterNodeHostedServiceWrapper)}...");

            // Stop and clear buffer flushing/processing
            logger.LogInformation($"Stopping {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Stop();
            logger.LogInformation($"Completed stopping {nameof(eventBufferFlushStrategy)}.");
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                logger.LogInformation($"Stopping {nameof(metricLogger)}...");
                metricLogger.Stop();
                logger.LogInformation($"Completed stopping {nameof(metricLogger)}.");
            }
            logger.LogInformation($"Disposing objects...");
            eventBufferFlushStrategy.Dispose();
            eventPersister.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
                metricLogger.Dispose();
            }
            eventCacheClient.Dispose();
            writerNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {nameof(WriterNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the 'writerNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeWriterNodeConstructorParameters
        (
            AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions,
            EventBufferFlushingOptions eventBufferFlushingOptions,
            EventCacheConnectionOptions eventCacheConnectionOptions, 
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            String sqlServerMetricLoggerCategoryName = "ApplicationAccessWriterNode";
            if (metricLoggingOptions.MetricCategorySuffix != "")
            {
                sqlServerMetricLoggerCategoryName = $"{sqlServerMetricLoggerCategoryName}-{metricLoggingOptions.MetricCategorySuffix}";
            }

            eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit,
                eventBufferFlushingOptions.FlushLoopInterval
            );

            var accessManagerConnectionStringBuilder = new SqlConnectionStringBuilder();
            accessManagerConnectionStringBuilder.DataSource = accessManagerSqlServerConnectionOptions.DataSource;
            // TODO: Need to enable this once I find a way to inject cert details etc into
            accessManagerConnectionStringBuilder.Encrypt = false;
            accessManagerConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            accessManagerConnectionStringBuilder.InitialCatalog = accessManagerSqlServerConnectionOptions.InitialCatalog;
            accessManagerConnectionStringBuilder.UserID = accessManagerSqlServerConnectionOptions.UserId;
            accessManagerConnectionStringBuilder.Password = accessManagerSqlServerConnectionOptions.Password;
            String accessManagerConnectionString = accessManagerConnectionStringBuilder.ConnectionString;
            IApplicationLogger eventPersisterLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
            (
                loggerFactory.CreateLogger<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>()
            );
            Uri eventCacheClientBaseUri = null;
            try
            {
                eventCacheClientBaseUri = new Uri(eventCacheConnectionOptions.Host);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to convert event cache host '{eventCacheConnectionOptions.Host}' to a {typeof(Uri).Name}.", e);
            }

            if (metricLoggingOptions.MetricLoggingEnabled == false)
            {
                eventPersister = new SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    accessManagerConnectionString,
                    accessManagerSqlServerConnectionOptions.RetryCount,
                    accessManagerSqlServerConnectionOptions.RetryInterval,
                    accessManagerSqlServerConnectionOptions.OperationTimeout,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    eventPersisterLogger
                );
                eventCacheClient = new EventCacheClient<String, String, String, String>
                (
                    eventCacheClientBaseUri, 
                    new StringUniqueStringifier(), 
                    new StringUniqueStringifier(), 
                    new StringUniqueStringifier(), 
                    new StringUniqueStringifier(), 
                    eventCacheConnectionOptions.RetryCount, 
                    eventCacheConnectionOptions.RetryInterval
                );
            }
            else
            {
                MetricBufferProcessingOptions metricBufferProcessingOptions = metricLoggingOptions.MetricBufferProcessing;
                switch (metricBufferProcessingOptions.BufferProcessingStrategy)
                {
                    case MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor:
                        metricLoggerBufferProcessingStrategy = new SizeLimitedBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit);
                        break;
                    case MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor:
                        metricLoggerBufferProcessingStrategy = new LoopingWorkerThreadBufferProcessor(metricBufferProcessingOptions.DequeueOperationLoopInterval);
                        break;
                    default:
                        throw new Exception($"Encountered unhandled {nameof(MetricBufferProcessingStrategyImplementation)} '{metricBufferProcessingOptions.BufferProcessingStrategy}' while attempting to create {nameof(ReaderWriterNode<String, String, String, String>)} constructor parameters.");
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
                    metricLoggerBufferProcessingStrategy,
                    true,
                    metricLoggerLogger
                );
                eventPersister = new SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    accessManagerConnectionString,
                    accessManagerSqlServerConnectionOptions.RetryCount,
                    accessManagerSqlServerConnectionOptions.RetryInterval,
                    accessManagerSqlServerConnectionOptions.OperationTimeout,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    eventPersisterLogger,
                    metricLogger
                );
                IApplicationLogger eventCacheClientLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<EventCacheClient<String, String, String, String>>()
                );
                eventCacheClient = new EventCacheClient<String, String, String, String>
                (
                    eventCacheClientBaseUri,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    eventCacheConnectionOptions.RetryCount,
                    eventCacheConnectionOptions.RetryInterval,
                    eventCacheClientLogger,
                    metricLogger
                );
            }
        }
    }
}
