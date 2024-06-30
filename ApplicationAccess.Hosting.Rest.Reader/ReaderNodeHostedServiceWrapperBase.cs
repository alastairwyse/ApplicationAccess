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
using ApplicationAccess.Hosting.Metrics;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Persistence.Sql;
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest.Reader
{
    /// <summary>
    /// Base for classes which wrap a subclass instance of <see cref="ReaderNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> and associated components and initialize them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <typeparam name="TReaderNode">The subclass of <see cref="ReaderNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> to wrap/host.</typeparam>
    /// <typeparam name="TAccessManager">The instance or subclass <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> which should be used to store the permissions and authorizations in the ReaderNode.</typeparam>
    /// <remarks>StartAsync() constructs a <see cref="ReaderNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> subclass instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public abstract class ReaderNodeHostedServiceWrapperBase<TReaderNode, TAccessManager> : IHostedService
        where TReaderNode : ReaderNodeBase<String, String, String, String, TAccessManager>
        where TAccessManager : ConcurrentAccessManager<String, String, String, String>, IMetricLoggingComponent
    {
        // Members passed in via dependency injection
        protected AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions;
        protected EventCacheConnectionOptions eventCacheConnectionOptions;
        protected EventCacheRefreshOptions eventCacheRefreshOptions;
        protected MetricLoggingOptions metricLoggingOptions;
        protected EntityQueryProcessorHolder entityQueryProcessorHolder;
        protected GroupQueryProcessorHolder groupQueryProcessorHolder;
        protected GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder;
        protected UserQueryProcessorHolder userQueryProcessorHolder;
        protected TripSwitchActuator tripSwitchActuator;
        protected ILoggerFactory loggerFactory;
        protected ILogger<ReaderNodeHostedServiceWrapperBase<TReaderNode, TAccessManager>> logger;

        /// <summary>Defines how often the reader node will be refreshed.</summary>
        protected LoopingWorkerThreadReaderNodeRefreshStrategy refreshStrategy;
        /// <summary>Interface to a cache for events which change the AccessManager, and which are used to refresh the reader node.</summary>
        protected EventCacheClient<String, String, String, String> eventCacheClient;
        /// <summary>Used to load data from the AccessManager.</summary>
        protected IAccessManagerTemporalBulkPersister<String, String, String, String> persistentReader;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected MetricLoggerBuffer metricLogger;
        /// <summary>The <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected TReaderNode readerNode;

        /// <summary>The category to log metrics generated by this class against.</summary>
        protected abstract String MetricLoggerCategoryName { get; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Reader.ReaderNodeHostedServiceWrapperBase class.
        /// </summary>
        public ReaderNodeHostedServiceWrapperBase
        (
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<EventCacheRefreshOptions> eventCacheRefreshOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityQueryProcessorHolder entityQueryProcessorHolder,
            GroupQueryProcessorHolder groupQueryProcessorHolder,
            GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder,
            UserQueryProcessorHolder userQueryProcessorHolder,
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<ReaderNodeHostedServiceWrapperBase<TReaderNode, TAccessManager>> logger
        )
        {
            this.accessManagerSqlDatabaseConnectionOptions = accessManagerSqlDatabaseConnectionOptions.Value;
            this.eventCacheConnectionOptions = eventCacheConnectionOptions.Value;
            this.eventCacheRefreshOptions = eventCacheRefreshOptions.Value;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.entityQueryProcessorHolder = entityQueryProcessorHolder;
            this.groupQueryProcessorHolder = groupQueryProcessorHolder;
            this.groupToGroupQueryProcessorHolder = groupToGroupQueryProcessorHolder;
            this.userQueryProcessorHolder = userQueryProcessorHolder;
            this.tripSwitchActuator = tripSwitchActuator;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {this.GetType().Name}...");

            logger.LogInformation($"Constructing ReaderNode instance...");

            // Initialize the ReaderNode constructor parameter members from configuration
            InitializeReaderNodeConstructorParameters
            (
                accessManagerSqlDatabaseConnectionOptions,
                eventCacheConnectionOptions,
                eventCacheRefreshOptions,
                metricLoggingOptions,
                loggerFactory
            );

            // Create the ReaderNode
            if (metricLoggingOptions.MetricLoggingEnabled.Value == false)
            {
                readerNode = InitializeReaderNode();
            }
            else
            {
                readerNode = InitializeReaderNodeWithMetricLogging();
            }

            // Set the ReaderWriterNode on the 'holder' classes
            SetupHolderClasses();

            logger.LogInformation($"Completed constructing ReaderNode instance.");

            // Start buffer flushing/processing
            //   Don't need to call metricLoggerBufferProcessingStrategy.Start() it's called by the below call to metricLogger.Start()
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
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

            logger.LogInformation($"Completed starting {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {this.GetType().Name}...");

            // Stop the refresh strategy
            logger.LogInformation($"Stopping {nameof(refreshStrategy)}...");
            refreshStrategy.Stop();
            logger.LogInformation($"Completed stopping {nameof(refreshStrategy)}.");
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                logger.LogInformation($"Stopping {nameof(metricLogger)}...");
                metricLogger.Stop();
                logger.LogInformation($"Completed stopping {nameof(metricLogger)}.");
            }
            logger.LogInformation($"Disposing objects...");
            eventCacheClient.Dispose();
            persistentReader.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
                metricLogger.Dispose();
            }
            readerNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'readerNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeReaderNodeConstructorParameters
        (
            AccessManagerSqlDatabaseConnectionOptions accessManagerSqlDatabaseConnectionOptions,
            EventCacheConnectionOptions eventCacheConnectionOptions,
            EventCacheRefreshOptions eventCacheRefreshOptions,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            String fullMetricLoggerCategoryName = MetricLoggerCategoryName;
            if (metricLoggingOptions.MetricCategorySuffix != "")
            {
                fullMetricLoggerCategoryName = $"{MetricLoggerCategoryName}-{metricLoggingOptions.MetricCategorySuffix}";
            }

            IApplicationLogger refreshStrategyLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
            (
                loggerFactory.CreateLogger<LoopingWorkerThreadReaderNodeRefreshStrategy>()
            );
            Action<ReaderNodeRefreshException> nodeRefreshExceptionAction = (ReaderNodeRefreshException readerNodeRefreshException) =>
            {
                tripSwitchActuator.Actuate();
                try
                {
                    refreshStrategyLogger.Log(ApplicationLogging.LogLevel.Critical, "Exception occurred when refreshing reader node.", readerNodeRefreshException);
                    refreshStrategyLogger.Log(ApplicationLogging.LogLevel.Critical, "Tripswitch has been actuated due to an unrecoverable error whilst refreshing the reader node.");
                }
                catch
                {
                }
            };
            refreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(eventCacheRefreshOptions.RefreshInterval, nodeRefreshExceptionAction);

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
                persistentReader = eventPersisterFactory.GetPersister(databaseConnectionParameters);
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
                persistentReader = eventPersisterFactory.GetPersister(databaseConnectionParameters);
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

        /// <summary>
        /// Initializes and returns the <see cref="ReaderNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.
        /// </summary>
        /// <returns>The <see cref="ReaderNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.</returns>
        protected abstract TReaderNode InitializeReaderNode();

        /// <summary>
        /// Initializes and returns the <see cref="ReaderNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance configured to log metrics.
        /// </summary>
        /// <returns>The <see cref="ReaderNodeBase{TUser, TGroup, TComponent, TAccess, TAccessManager}"/> instance.</returns>
        protected abstract TReaderNode InitializeReaderNodeWithMetricLogging();

        /// <summary>
        /// Sets the 'readerNode' member on the various 'holder' class instances (e.g. <see cref="EntityQueryProcessorHolder"/>).
        /// </summary>
        protected virtual void SetupHolderClasses()
        {
            entityQueryProcessorHolder.EntityQueryProcessor = readerNode;
            groupQueryProcessorHolder.GroupQueryProcessor = readerNode;
            groupToGroupQueryProcessorHolder.GroupToGroupQueryProcessor = readerNode;
            userQueryProcessorHolder.UserQueryProcessor = readerNode;
        }

        #endregion
    }
}
