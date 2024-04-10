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
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Persistence;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter
{
    /// <summary>
    /// Variation of <see cref="ReaderWriterNodeHostedServiceWrapper"/> with metrics disabled and using the <see cref="ListAccessManagerTemporalBulkPersister{TUser, TGroup, TComponent, TAccess}"/> class for storing persisted events before writing them to a JSON file when the service is stopped.  For concurrency and/or performance testing.
    /// </summary>
    public class JsonEventWritingReaderWriterNodeHostedServiceWrapper : IHostedService
    {
        // Members passed in via dependency injection
        protected AccessManagerOptions accessManagerOptions;
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
        protected ILoggerFactory loggerFactory;
        protected ILogger<ReaderWriterNodeHostedServiceWrapper> logger;

        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the ReaderWriterNode.</summary>
        protected SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to persist changes to the AccessManager.</summary>
        protected ListAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected NullMetricLogger metricLogger;
        /// <summary>The <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected ReaderWriterNode<String, String, String, String> readerWriterNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ReaderWriter.JsonEventWritingReaderWriterNodeHostedServiceWrapper class.
        /// </summary>
        public JsonEventWritingReaderWriterNodeHostedServiceWrapper
        (
            IOptions<AccessManagerOptions> accessManagerOptions,
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
            ILoggerFactory loggerFactory,
            ILogger<ReaderWriterNodeHostedServiceWrapper> logger
        )
        {
            this.accessManagerOptions = accessManagerOptions.Value;
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
                accessManagerSqlDatabaseConnectionOptions,
                eventBufferFlushingOptions,
                metricLoggingOptions,
                loggerFactory
            );

            // Create the ReaderWriterNode
            readerWriterNode = new ReaderWriterNode<String, String, String, String>(eventBufferFlushStrategy, new NullTemporalPersistentReader<String, String, String, String>(), eventPersister, accessManagerOptions.StoreBidirectionalMappings.Value);

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

            logger.LogInformation($"Disposing objects...");
            eventBufferFlushStrategy.Dispose();
            readerWriterNode.Dispose();
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Writing generated events to storage...");
            var serialIzerOptions = new JsonSerializerOptions
            {
                Converters =
                {
                    new TemporalEventBufferItemBaseConverter<String, String, String, String>
                    (
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier()
                    )
                }
            };
            String serializedEventsFilePath = @"C:\Temp\" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "ReaderWriter Generated Events.json";
            using (var fileStream = new FileStream(serializedEventsFilePath, FileMode.CreateNew))
            {
                JsonSerializer.Serialize<List<TemporalEventBufferItemBase>>(fileStream, eventPersister.Events, serialIzerOptions);
                fileStream.Flush();
                fileStream.Close();
            }
            logger.LogInformation($"Completed writing generated events to storage.");

            logger.LogInformation($"Completed stopping {nameof(ReaderWriterNodeHostedServiceWrapper)}.");

            return Task.CompletedTask;
        }

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
            eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit,
                eventBufferFlushingOptions.FlushLoopInterval
            );
            metricLogger = new NullMetricLogger();
            eventPersister = new ListAccessManagerTemporalBulkPersister<String, String, String, String>();
        }

        #region Nested Classes

        protected class NullTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess>
        {
            /// <inheritdoc/>
            public AccessManagerState Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
            {
                throw new PersistentStorageEmptyException($"Class '{this.GetType().Name}' cannot load.");
            }

            /// <inheritdoc/>
            public AccessManagerState Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
