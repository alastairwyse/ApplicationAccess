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
    /// Container class holding the constructor parameters for a <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    public class ReaderWriterNodeConstructorParameters
    {
        // TODO: This could possibly go to a more shared namespace... e.g. .models

        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</summary>
        public SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy EventBufferFlushStrategy { get; }

        /// <summary>Used to persist changes and to load data to the AccessManager.</summary>
        public SqlServerAccessManagerTemporalBulkPersister<String, String, String, String> EventPersister { get; }

        /// <summary>The buffer processing for the logger for metrics.</summary>
        public WorkerThreadBufferProcessorBase? MetricLoggerBufferProcessingStrategy { get; }

        /// <summary>The logger for metrics.</summary>
        public SqlServerMetricLogger? MetricLogger { get; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ReaderWriter.ReaderWriterNodeConstructorParameters class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="eventPersister">Used to persist changes and to load data to the AccessManager.</param>
        /// <param name="metricLoggerBufferProcessingStrategy">The buffer processing for the logger for metrics.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ReaderWriterNodeConstructorParameters
        (
            SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy eventBufferFlushStrategy, 
            SqlServerAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister,
            WorkerThreadBufferProcessorBase? metricLoggerBufferProcessingStrategy, 
            SqlServerMetricLogger? metricLogger
        )
        {
            EventBufferFlushStrategy = eventBufferFlushStrategy;
            EventPersister = eventPersister;
            MetricLoggerBufferProcessingStrategy = metricLoggerBufferProcessingStrategy;
            MetricLogger = metricLogger;
        }
    }
}
