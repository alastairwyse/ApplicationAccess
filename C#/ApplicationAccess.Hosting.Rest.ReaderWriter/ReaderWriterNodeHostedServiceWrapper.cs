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

using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter
{
    /// <summary>
    /// Wraps an instance of <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() calls constructs a <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class ReaderWriterNodeHostedServiceWrapper : IHostedService
    {
        // Members passed in via dependency injection
        protected AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions;
        protected EventBufferFlushingOptions eventBufferFlushingOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected EntityEventProcessorHolder entityEventProcessorHolder;
        protected EntityQueryProcessorHolder entityQueryProcessorHolder;
        protected GroupEventProcessorHolder groupEventProcessorHolder;
        protected GroupQueryProcessorHolder groupQueryProcessorHolder;
        protected GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder;
        protected GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder;
        protected UserEventProcessorHolder userEventProcessorHolder;
        protected UserQueryProcessorHolder userQueryProcessorHolder;
        protected ILoggerFactory loggerFactory;
        protected ILogger<ReaderWriterNodeHostedServiceWrapper> logger;

        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the ReaderWriterNode.</summary>
        protected SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to persist changes load data to/from the AccessManager.</summary>
        protected SqlServerAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected SqlServerMetricLogger metricLogger;
        /// <summary>The <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected ReaderWriterNode<String, String, String, String> readerWriterNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ReaderWriter.ReaderWriterNodeHostedServiceWrapper class.
        /// </summary>
        public ReaderWriterNodeHostedServiceWrapper
        (
            IOptions<AccessManagerSqlServerConnectionOptions> accessManagerSqlServerConnectionOptions,
            IOptions<EventBufferFlushingOptions> eventBufferFlushingOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityEventProcessorHolder entityEventProcessorHolder,
            EntityQueryProcessorHolder entityQueryProcessorHolder,
            GroupEventProcessorHolder groupEventProcessorHolder,
            GroupQueryProcessorHolder groupQueryProcessorHolder,
            GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder,
            GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder,
            UserEventProcessorHolder userEventProcessorHolder,
            UserQueryProcessorHolder userQueryProcessorHolder,
            ILoggerFactory loggerFactory,
            ILogger<ReaderWriterNodeHostedServiceWrapper> logger
        )
        {
            this.accessManagerSqlServerConnectionOptions = accessManagerSqlServerConnectionOptions.Value;
            this.eventBufferFlushingOptions = eventBufferFlushingOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.entityEventProcessorHolder = entityEventProcessorHolder;
            this.entityQueryProcessorHolder = entityQueryProcessorHolder;
            this.groupEventProcessorHolder = groupEventProcessorHolder;
            this.groupQueryProcessorHolder = groupQueryProcessorHolder;
            this.groupToGroupEventProcessorHolder = groupToGroupEventProcessorHolder;
            this.groupToGroupQueryProcessorHolder = groupToGroupQueryProcessorHolder;
            this.userEventProcessorHolder = userEventProcessorHolder;
            this.userQueryProcessorHolder = userQueryProcessorHolder; 
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {nameof(ReaderWriterNodeHostedServiceWrapper)}...");

            logger.LogInformation($"Constructing ReaderWriterNode instance...");

            // Initialize the ReaderWriterNode constructor parameter members from configuration
            InitializeReaderWriterNodeConstructorParameters
            (
                accessManagerSqlServerConnectionOptions,
                eventBufferFlushingOptions,
                metricLoggingOptions,
                loggerFactory
            );

            // Create the ReaderWriterNode
            if (metricLoggingOptions.MetricLoggingEnabled == false)
            {
                readerWriterNode = new ReaderWriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister);
            }
            else
            {
                readerWriterNode = new ReaderWriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister, metricLogger);
            }

            // Set the ReaderWriterNode on the 'holder' classes
            entityEventProcessorHolder.EntityEventProcessor = readerWriterNode;
            entityQueryProcessorHolder.EntityQueryProcessor = readerWriterNode;
            groupEventProcessorHolder.GroupEventProcessor = readerWriterNode;
            groupQueryProcessorHolder.GroupQueryProcessor = readerWriterNode;
            groupToGroupEventProcessorHolder.GroupToGroupEventProcessor = readerWriterNode;
            groupToGroupQueryProcessorHolder.GroupToGroupQueryProcessor = readerWriterNode;
            userEventProcessorHolder.UserEventProcessor = readerWriterNode;
            userQueryProcessorHolder.UserQueryProcessor = readerWriterNode;

            logger.LogInformation($"Completed constructing ReaderWriterNode instance.");

            // Start buffer flushing/processing
            logger.LogInformation($"Starting {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Start();
            logger.LogInformation($"Completed starting {nameof(eventBufferFlushStrategy)}.");
            // Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLogger != null)
            {
                logger.LogInformation($"Starting {nameof(metricLogger)}...");
                metricLogger.Start();
                logger.LogInformation($"Completed starting {nameof(metricLogger)}.");
            }
            // Load the current state of the readerWriterNode from storage
            logger.LogInformation($"Loading data into {nameof(readerWriterNode)}...");
            readerWriterNode.Load(false);
            logger.LogInformation($"Completed loading data into {nameof(readerWriterNode)}.");
            
            logger.LogInformation($"Completed starting {nameof(ReaderWriterNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {nameof(ReaderWriterNodeHostedServiceWrapper)}...");

            // Stop and clear buffer flushing/processing
            logger.LogInformation($"Stopping {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Stop();
            logger.LogInformation($"Completed stopping {nameof(eventBufferFlushStrategy)}.");
            if (metricLogger != null)
            {
                logger.LogInformation($"Stopping {nameof(metricLogger)}...");
                metricLogger.Stop();
                logger.LogInformation($"Completed stopping {nameof(metricLogger)}.");
            }
            logger.LogInformation($"Disposing objects...");
            eventBufferFlushStrategy.Dispose();
            eventPersister.Dispose();
            if (metricLogger != null)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
                metricLogger.Dispose();
            }
            readerWriterNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {nameof(ReaderWriterNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the 'readerWriterNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeReaderWriterNodeConstructorParameters
        (
            AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions,
            EventBufferFlushingOptions eventBufferFlushingOptions,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            const String sqlServerMetricLoggerCategoryName = "ApplicationAccessReaderWriterNode";

            eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit,
                eventBufferFlushingOptions.FlushLoopInterval
            );

            var connectionStringBuilder = new SqlConnectionStringBuilder();
            connectionStringBuilder.DataSource = accessManagerSqlServerConnectionOptions.DataSource;
            // TODO: Need to enable this once I find a way to inject cert details etc into
            connectionStringBuilder.Encrypt = false;
            connectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            connectionStringBuilder.InitialCatalog = accessManagerSqlServerConnectionOptions.InitialCatalog;
            connectionStringBuilder.UserID = accessManagerSqlServerConnectionOptions.UserId;
            connectionStringBuilder.Password = accessManagerSqlServerConnectionOptions.Password;
            String connectionString = connectionStringBuilder.ConnectionString;
            IApplicationLogger eventPersisterLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
            (
                loggerFactory.CreateLogger<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>()
            );
            eventPersister = new SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>
            (
                connectionString,
                accessManagerSqlServerConnectionOptions.RetryCount,
                accessManagerSqlServerConnectionOptions.RetryInterval,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                eventPersisterLogger
            );

            if (metricLoggingOptions.MetricLoggingEnabled == true)
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
                connectionStringBuilder = new SqlConnectionStringBuilder();
                connectionStringBuilder.DataSource = metricsSqlServerConnectionOptions.DataSource;
                // TODO: Need to enable this once I find a way to inject cert details etc into
                connectionStringBuilder.Encrypt = false;
                connectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
                connectionStringBuilder.InitialCatalog = metricsSqlServerConnectionOptions.InitialCatalog;
                connectionStringBuilder.UserID = metricsSqlServerConnectionOptions.UserId;
                connectionStringBuilder.Password = metricsSqlServerConnectionOptions.Password;
                connectionString = connectionStringBuilder.ConnectionString;
                IApplicationLogger metricLoggerLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<SqlServerMetricLogger>()
                );
                metricLogger = new SqlServerMetricLogger
                (
                    sqlServerMetricLoggerCategoryName,
                    connectionString,
                    metricsSqlServerConnectionOptions.RetryCount,
                    metricsSqlServerConnectionOptions.RetryInterval,
                    metricLoggerBufferProcessingStrategy,
                    true,
                    metricLoggerLogger
                );
            }
        }
    }
}
