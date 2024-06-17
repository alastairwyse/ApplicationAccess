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
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting;
using ApplicationAccess.Hosting.Metrics;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Persistence;
using ApplicationAccess.Hosting.Persistence.Sql;
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest.Writer
{
    /// <summary>
    /// Base for classes which wrap a subclass instance of <see cref="WriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> and associated components and initialize them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <typeparam name="TWriterNode">The subclass of <see cref="WriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> to wrap/host.</typeparam>
    /// <typeparam name="TAccessManager">The instance or subclass <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> which reads and writes the permissions and authorizations in the ReaderWriterNode.</typeparam>
    /// <remarks>StartAsync() constructs a <see cref="WriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> subclass instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public abstract class WriterNodeHostedServiceWrapperBase<TWriterNode, TAccessManager> : IHostedService
        where TWriterNode : WriterNodeBase<String, String, String, String, TAccessManager>
        where TAccessManager : ConcurrentAccessManager<String, String, String, String>, IMetricLoggingComponent
    {
        // Members passed in via dependency injection
        protected AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions;
        protected EventBufferFlushingOptions eventBufferFlushingOptions;
        protected EventCacheConnectionOptions eventCacheConnectionOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected EntityEventProcessorHolder entityEventProcessorHolder;
        protected GroupEventProcessorHolder groupEventProcessorHolder;
        protected GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder;
        protected UserEventProcessorHolder userEventProcessorHolder;
        protected ILoggerFactory loggerFactory;
        protected ILogger<WriterNodeHostedServiceWrapperBase<TWriterNode, TAccessManager>> logger;

        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the WriterNode.</summary>
        protected SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to persist changes to and load data from the AccessManager.</summary>
        protected IAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister;
        /// <summary>Interface to a cache for events which change the AccessManager.</summary>
        protected EventCacheClient<String, String, String, String> eventCacheClient;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected MetricLoggerBuffer metricLogger;
        /// <summary>The <see cref="WriterNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected TWriterNode writerNode;

        /// <summary>The category to log metrics generated by this class against.</summary>
        protected abstract String MetricLoggerCategoryName { get; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Writer.WriterNodeHostedServiceWrapperBase class.
        /// </summary>
        public WriterNodeHostedServiceWrapperBase
        (
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
            IOptions<EventBufferFlushingOptions> eventBufferFlushingOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityEventProcessorHolder entityEventProcessorHolder,
            GroupEventProcessorHolder groupEventProcessorHolder,
            GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder,
            UserEventProcessorHolder userEventProcessorHolder,
            ILoggerFactory loggerFactory,
            ILogger<WriterNodeHostedServiceWrapperBase<TWriterNode, TAccessManager>> logger
        )
        {
            this.accessManagerSqlDatabaseConnectionOptions = accessManagerSqlDatabaseConnectionOptions.Value;
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
            logger.LogInformation($"Starting {this.GetType().Name}...");

            logger.LogInformation($"Constructing WriterNode instance...");

            // Initialize the WriterNode constructor parameter members from configuration
            InitializeWriterNodeConstructorParameters
            (
                accessManagerSqlDatabaseConnectionOptions,
                eventBufferFlushingOptions,
                eventCacheConnectionOptions,
                metricLoggingOptions,
                loggerFactory
            );

            // Create the WriterNode
            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                writerNode = InitializeWriterNode();
            }
            else
            {
                writerNode = InitializeWriterNodeWithMetricLogging();
            }

            // Set the WriterNode on the 'holder' classes
            SetupHolderClasses();

            logger.LogInformation($"Completed constructing WriterNode instance.");

            // Start buffer flushing/processing
            logger.LogInformation($"Starting {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Start();
            logger.LogInformation($"Completed starting {nameof(eventBufferFlushStrategy)}.");
            // Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                logger.LogInformation($"Starting {nameof(metricLogger)}...");
                metricLogger.Start();
                logger.LogInformation($"Completed starting {nameof(metricLogger)}.");
            }
            // Load the current state of the WriterNode from storage
            logger.LogInformation($"Loading data into {nameof(writerNode)}...");
            writerNode.Load(false);
            logger.LogInformation($"Completed loading data into {nameof(writerNode)}.");

            logger.LogInformation($"Completed starting {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {this.GetType().Name}...");

            // Stop and clear buffer flushing/processing
            logger.LogInformation($"Stopping {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Stop();
            logger.LogInformation($"Completed stopping {nameof(eventBufferFlushStrategy)}.");
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                logger.LogInformation($"Stopping {nameof(metricLogger)}...");
                metricLogger.Stop();
                logger.LogInformation($"Completed stopping {nameof(metricLogger)}.");
            }
            logger.LogInformation($"Disposing objects...");
            eventBufferFlushStrategy.Dispose();
            eventPersister.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
                metricLogger.Dispose();
            }
            eventCacheClient.Dispose();
            writerNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'writerNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeWriterNodeConstructorParameters
        (
            AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions,
            EventBufferFlushingOptions eventBufferFlushingOptions,
            EventCacheConnectionOptions eventCacheConnectionOptions,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            String fullMetricLoggerCategoryName = MetricLoggerCategoryName;
            if (metricLoggingOptions.MetricCategorySuffix != "")
            {
                fullMetricLoggerCategoryName = $"{MetricLoggerCategoryName}-{metricLoggingOptions.MetricCategorySuffix}";
            }

            IApplicationLogger eventPersisterLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
            (
                loggerFactory.CreateLogger<IAccessManagerTemporalBulkPersister<String, String, String, String>>()
            );

            var databaseConnectionParametersParser = new SqlDatabaseConnectionParametersParser();
            SqlDatabaseConnectionParametersBase databaseConnectionParameters = databaseConnectionParametersParser.Parse
            (
                accessManagerSqlDatabaseConnectionOptions.DatabaseType,
                accessManagerSqlDatabaseConnectionOptions.ConnectionParameters,
                AccessManagerSqlDatabaseConnectionOptions.AccessManagerSqlDatabaseConnectionOptionsOptionsName
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

            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                var eventPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
                (
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    eventPersisterLogger
                );
                eventPersister = eventPersisterFactory.GetPersister(databaseConnectionParameters);
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
                eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
                (
                    eventBufferFlushingOptions.BufferSizeLimit,
                    eventBufferFlushingOptions.FlushLoopInterval
                );
            }
            else
            {
                MetricBufferProcessingOptions metricBufferProcessingOptions = metricLoggingOptions.MetricBufferProcessing;
                var metricsBufferProcessorFactory = new MetricsBufferProcessorFactory();
                metricLoggerBufferProcessingStrategy = metricsBufferProcessorFactory.GetBufferProcessor(metricBufferProcessingOptions);
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
                    fullMetricLoggerCategoryName,
                    metricLoggerBufferProcessingStrategy,
                    IntervalMetricBaseTimeUnit.Nanosecond,
                    true,
                    metricLoggerLogger
                );
                metricLogger = metricLoggerFactory.GetMetricLogger(metricsDatabaseConnectionParameters);
                var eventPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
                (
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    eventPersisterLogger, metricLogger
                );
                eventPersister = eventPersisterFactory.GetPersister(databaseConnectionParameters);
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
                eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
                (
                    eventBufferFlushingOptions.BufferSizeLimit,
                    eventBufferFlushingOptions.FlushLoopInterval,
                    metricLogger
                );
            }
        }

        /// <summary>
        /// Initializes and returns the <see cref="WriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.
        /// </summary>
        /// <returns>The <see cref="WriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.</returns>
        protected abstract TWriterNode InitializeWriterNode();

        /// <summary>
        /// Initializes and returns the <see cref="WriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance configured to log metrics.
        /// </summary>
        /// <returns>The <see cref="WriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.</returns>
        protected abstract TWriterNode InitializeWriterNodeWithMetricLogging();

        /// <summary>
        /// Sets the 'writerNode' member on the various 'holder' class instances (e.g. <see cref="EntityEventProcessorHolder"/>).
        /// </summary>
        protected virtual void SetupHolderClasses()
        {
            entityEventProcessorHolder.EntityEventProcessor = writerNode;
            groupEventProcessorHolder.GroupEventProcessor = writerNode;
            groupToGroupEventProcessorHolder.GroupToGroupEventProcessor = writerNode;
            userEventProcessorHolder.UserEventProcessor = writerNode;
        }

        #endregion
    }
}