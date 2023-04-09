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
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationAccess.Persistence.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;

namespace ApplicationAccess.Hosting.Rest.Reader
{
    /// <summary>
    /// Wraps an instance of <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class ReaderNodeHostedServiceWrapper : IHostedService
    {
        // Members passed in via dependency injection
        protected AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions;
        protected EventCacheConnectionOptions eventCacheConnectionOptions;
        protected EventCacheRefreshOptions eventCacheRefreshOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected EntityQueryProcessorHolder entityQueryProcessorHolder;
        protected GroupQueryProcessorHolder groupQueryProcessorHolder;
        protected GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder;
        protected UserQueryProcessorHolder userQueryProcessorHolder;
        protected ILoggerFactory loggerFactory;
        protected ILogger<ReaderNodeHostedServiceWrapper> logger;

        /// <summary>Defines how often the reader node will be refreshed.</summary>
        protected LoopingWorkerThreadReaderNodeRefreshStrategy refreshStrategy;
        /// <summary>Interface to a cache for events which change the AccessManager, and which are used to refresh the reader node.</summary>
        protected EventCacheClient<String, String, String, String> eventCacheClient;
        /// <summary>Used to load data from the AccessManager.</summary>
        protected SqlServerAccessManagerTemporalBulkPersister<String, String, String, String> persistentReader;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected SqlServerMetricLogger metricLogger;
        /// <summary>The <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected ReaderNode<String, String, String, String> readerNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Reader.ReaderNodeHostedServiceWrapper class.
        /// </summary>
        public ReaderNodeHostedServiceWrapper
        (
            IOptions<AccessManagerSqlServerConnectionOptions> accessManagerSqlServerConnectionOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<EventCacheRefreshOptions> eventCacheRefreshOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityQueryProcessorHolder entityQueryProcessorHolder,
            GroupQueryProcessorHolder groupQueryProcessorHolder,
            GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder,
            UserQueryProcessorHolder userQueryProcessorHolder,
            ILoggerFactory loggerFactory,
            ILogger<ReaderNodeHostedServiceWrapper> logger
        )
        {
            this.accessManagerSqlServerConnectionOptions = accessManagerSqlServerConnectionOptions.Value;
            this.eventCacheConnectionOptions = eventCacheConnectionOptions.Value;
            this.eventCacheRefreshOptions = eventCacheRefreshOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.entityQueryProcessorHolder = entityQueryProcessorHolder;
            this.groupQueryProcessorHolder = groupQueryProcessorHolder;
            this.groupToGroupQueryProcessorHolder = groupToGroupQueryProcessorHolder;
            this.userQueryProcessorHolder = userQueryProcessorHolder;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {nameof(ReaderNodeHostedServiceWrapper)}...");

            logger.LogInformation($"Constructing ReaderNode instance...");

            // Initialize the ReaderNode constructor parameter members from configuration
            InitializeReaderNodeConstructorParameters
            (
                accessManagerSqlServerConnectionOptions,
                eventCacheConnectionOptions,
                eventCacheRefreshOptions,
                metricLoggingOptions,
                loggerFactory
            );

            // Create the ReaderNode
            if (metricLoggingOptions.MetricLoggingEnabled == false)
            {
                readerNode = new ReaderNode<String, String, String, String>(refreshStrategy, eventCacheClient, persistentReader);
            }
            else
            {
                readerNode = new ReaderNode<String, String, String, String>(refreshStrategy, eventCacheClient, persistentReader, metricLogger);
            }

            // Set the ReaderWriterNode on the 'holder' classes
            entityQueryProcessorHolder.EntityQueryProcessor = readerNode;
            groupQueryProcessorHolder.GroupQueryProcessor = readerNode;
            groupToGroupQueryProcessorHolder.GroupToGroupQueryProcessor = readerNode;
            userQueryProcessorHolder.UserQueryProcessor = readerNode;

            logger.LogInformation($"Completed constructing ReaderNode instance.");

            // Start buffer flushing/processing
            //   Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                logger.LogInformation($"Starting {nameof(metricLogger)}...");
                metricLogger.Start();
                logger.LogInformation($"Completed starting {nameof(metricLogger)}.");
            }
            // Load the current state of the readerNode from storage
            logger.LogInformation($"Loading data into {nameof(readerNode)}...");
            readerNode.Load(false);
            logger.LogInformation($"Completed loading data into {nameof(readerNode)}.");
            // Start the refresh strategy
            logger.LogInformation($"Starting {nameof(refreshStrategy)}...");
            refreshStrategy.Start();
            logger.LogInformation($"Completed starting {nameof(refreshStrategy)}.");

            logger.LogInformation($"Completed starting {nameof(ReaderNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {nameof(ReaderNodeHostedServiceWrapper)}...");

            // Stop the refresh strategy
            logger.LogInformation($"Stopping {nameof(refreshStrategy)}...");
            refreshStrategy.Stop();
            logger.LogInformation($"Completed stopping {nameof(refreshStrategy)}.");
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                logger.LogInformation($"Stopping {nameof(metricLogger)}...");
                metricLogger.Stop();
                logger.LogInformation($"Completed stopping {nameof(metricLogger)}.");
            }
            logger.LogInformation($"Disposing objects...");
            eventCacheClient.Dispose();
            persistentReader.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
                metricLogger.Dispose();
            }
            readerNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {nameof(ReaderNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the 'readerNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeReaderNodeConstructorParameters
        (
            AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions,
            EventCacheConnectionOptions eventCacheConnectionOptions,
            EventCacheRefreshOptions eventCacheRefreshOptions,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            String sqlServerMetricLoggerCategoryName = "ApplicationAccessReaderNode"; 
            if (metricLoggingOptions.MetricCategorySuffix != "")
            {
                sqlServerMetricLoggerCategoryName = $"{sqlServerMetricLoggerCategoryName}-{metricLoggingOptions.MetricCategorySuffix}";
            }

            refreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(eventCacheRefreshOptions.RefreshInterval);

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
                persistentReader = new SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>
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
                persistentReader = new SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>
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