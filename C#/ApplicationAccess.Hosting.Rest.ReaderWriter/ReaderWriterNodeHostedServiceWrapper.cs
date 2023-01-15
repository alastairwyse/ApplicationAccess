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

using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.SqlServer;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter
{
    /// <summary>
    /// Wraps an instance of <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() calls methods like Start() and Load(), whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class ReaderWriterNodeHostedServiceWrapper : IHostedService
    {
        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</summary>
        protected SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to persist changes and to load data to the AccessManager.</summary>
        protected SqlServerAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase? metricLoggerBufferProcessingStrategy;
        /// <summary>The logger for metrics.</summary>
        protected SqlServerMetricLogger? metricLogger;
        /// <summary>The <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/>.</summary>
        protected ReaderWriterNode<String, String, String, String> readerWriterNode;
        /// <summary>Logger for any actions which occur during start and stop.</summary>
        protected ILogger<ReaderWriterNodeHostedServiceWrapper> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ReaderWriter.ReaderWriterNodeHostedServiceWrapper class.
        /// </summary>
        /// <param name="readerWriterNodeConstructorParameters">Container class holding the <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> constructor parameters.</param>
        /// <param name="readerWriterNode">The <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/>.</param>
        /// <param name="loggerFactory">Factory for logger objects.</param>
        public ReaderWriterNodeHostedServiceWrapper
        (
            ReaderWriterNodeConstructorParameters readerWriterNodeConstructorParameters,
            ReaderWriterNode<String, String, String, String> readerWriterNode,
            ILoggerFactory loggerFactory
        )
        {
            this.eventBufferFlushStrategy = readerWriterNodeConstructorParameters.EventBufferFlushStrategy;
            this.eventPersister = readerWriterNodeConstructorParameters.EventPersister;
            this.metricLoggerBufferProcessingStrategy = readerWriterNodeConstructorParameters.MetricLoggerBufferProcessingStrategy;
            this.metricLogger = readerWriterNodeConstructorParameters.MetricLogger;
            this.readerWriterNode = readerWriterNode;
            logger = loggerFactory.CreateLogger<ReaderWriterNodeHostedServiceWrapper>();
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {nameof(ReaderWriterNodeHostedServiceWrapper)}...");
            // Start any buffer flushing/processing
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
    }
}
