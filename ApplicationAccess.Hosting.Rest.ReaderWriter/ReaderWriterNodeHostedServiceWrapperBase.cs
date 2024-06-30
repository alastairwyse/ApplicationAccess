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
using ApplicationAccess.Hosting.Persistence.Sql;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter
{
    /// <summary>
    /// Base for classes which wrap a subclass instance of <see cref="ReaderWriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> and associated components and initialize them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <typeparam name="TReaderWriterNode">The subclass of <see cref="ReaderWriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> to wrap/host.</typeparam>
    /// <typeparam name="TAccessManager">The instance or subclass <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> which reads and writes the permissions and authorizations in the ReaderWriterNode.</typeparam>
    /// <remarks>StartAsync() constructs a <see cref="ReaderWriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> subclass instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public abstract class ReaderWriterNodeHostedServiceWrapperBase<TReaderWriterNode, TAccessManager> : IHostedService
        where TReaderWriterNode : ReaderWriterNodeBase<String, String, String, String, TAccessManager>
        where TAccessManager : ConcurrentAccessManager<String, String, String, String>, IMetricLoggingComponent
    {
        // Members passed in via dependency injection
        protected AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions;
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
        protected TripSwitchActuator tripSwitchActuator;
        protected ILoggerFactory loggerFactory;
        protected ILogger<ReaderWriterNodeHostedServiceWrapperBase<TReaderWriterNode, TAccessManager>> logger;

        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the ReaderWriterNode.</summary>
        protected SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to persist changes to, and load data from the AccessManager.</summary>
        protected IAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected MetricLoggerBuffer metricLogger;
        /// <summary>The <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected TReaderWriterNode readerWriterNode;

        /// <summary>The category to log metrics generated by this class against.</summary>
        protected abstract String MetricLoggerCategoryName { get; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ReaderWriter.ReaderWriterNodeHostedServiceWrapperBase class.
        /// </summary>
        public ReaderWriterNodeHostedServiceWrapperBase
        (
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
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
            TripSwitchActuator tripSwitchActuator, 
            ILoggerFactory loggerFactory,
            ILogger<ReaderWriterNodeHostedServiceWrapperBase<TReaderWriterNode, TAccessManager>> logger
        )
        {
            this.accessManagerSqlDatabaseConnectionOptions = accessManagerSqlDatabaseConnectionOptions.Value;
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
            this.tripSwitchActuator = tripSwitchActuator;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {this.GetType().Name}...");

            logger.LogInformation($"Constructing ReaderWriterNode instance...");

            // Initialize the ReaderWriterNode constructor parameter members from configuration
            InitializeReaderWriterNodeConstructorParameters
            (
                accessManagerSqlDatabaseConnectionOptions,
                eventBufferFlushingOptions,
                metricLoggingOptions,
                loggerFactory
            );

            // Create the ReaderWriterNode
            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                readerWriterNode = InitializeReaderWriterNode();
            }
            else
            {
                readerWriterNode = InitializeReaderWriterNodeWithMetricLogging();
            }

            // Set the ReaderWriterNode on the 'holder' classes
            SetupHolderClasses();

            logger.LogInformation($"Completed constructing ReaderWriterNode instance.");

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
            // Load the current state of the ReaderWriterNode from storage
            logger.LogInformation($"Loading data into {nameof(readerWriterNode)}...");
            readerWriterNode.Load(false);
            logger.LogInformation($"Completed loading data into {nameof(readerWriterNode)}.");

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
            readerWriterNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'readerWriterNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeReaderWriterNodeConstructorParameters
        (
            AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions,
            EventBufferFlushingOptions eventBufferFlushingOptions,
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

            Action<BufferFlushingException> eventBufferFlushingExceptionAction = (BufferFlushingException bufferFlushingException) =>
            {
                tripSwitchActuator.Actuate();
                try
                {
                    eventPersisterLogger.Log(ApplicationLogging.LogLevel.Critical, "Exception occurred when flushing event buffer.", bufferFlushingException);
                    eventPersisterLogger.Log(ApplicationLogging.LogLevel.Critical, "Tripswitch has been actuated due to an unrecoverable error whilst flushing the event buffer.");
                }
                catch
                {
                }
            };
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
                eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
                (
                    eventBufferFlushingOptions.BufferSizeLimit,
                    eventBufferFlushingOptions.FlushLoopInterval,
                    eventBufferFlushingExceptionAction
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
                    eventPersisterLogger,
                    metricLogger
                );
                eventPersister = eventPersisterFactory.GetPersister(databaseConnectionParameters);
                eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
                (
                    eventBufferFlushingOptions.BufferSizeLimit,
                    eventBufferFlushingOptions.FlushLoopInterval,
                    eventBufferFlushingExceptionAction
                );
            }
        }

        /// <summary>
        /// Initializes and returns the <see cref="ReaderWriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.
        /// </summary>
        /// <returns>The <see cref="ReaderWriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.</returns>
        protected abstract TReaderWriterNode InitializeReaderWriterNode();

        /// <summary>
        /// Initializes and returns the <see cref="ReaderWriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance configured to log metrics.
        /// </summary>
        /// <returns>The <see cref="ReaderWriterNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.</returns>
        protected abstract TReaderWriterNode InitializeReaderWriterNodeWithMetricLogging();

        /// <summary>
        /// Sets the 'readerWriterNode' member on the various 'holder' class instances (e.g. <see cref="EntityEventProcessorHolder"/>).
        /// </summary>
        protected virtual void SetupHolderClasses()
        {
            entityEventProcessorHolder.EntityEventProcessor = readerWriterNode;
            entityQueryProcessorHolder.EntityQueryProcessor = readerWriterNode;
            groupEventProcessorHolder.GroupEventProcessor = readerWriterNode;
            groupQueryProcessorHolder.GroupQueryProcessor = readerWriterNode;
            groupToGroupEventProcessorHolder.GroupToGroupEventProcessor = readerWriterNode;
            groupToGroupQueryProcessorHolder.GroupToGroupQueryProcessor = readerWriterNode;
            userEventProcessorHolder.UserEventProcessor = readerWriterNode;
            userQueryProcessorHolder.UserQueryProcessor = readerWriterNode;
        }

        #endregion
    }
}
