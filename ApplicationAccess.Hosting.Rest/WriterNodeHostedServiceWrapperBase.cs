/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting;
using ApplicationAccess.Persistence;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Base for classes which host a WriterNode component node as an <see cref="IHostedService"/>, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>Contains common method used in startup and shutdown of the node.</remarks>
    public class WriterNodeHostedServiceWrapperBase<TBufferFlushStrategy> : NodeHostedServiceWrapperBase where TBufferFlushStrategy : WorkerThreadBufferFlushStrategyBase
    {
        /// <summary>Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the WriterNode.</summary>
        protected TBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to persist changes to and load data from the AccessManager.</summary>
        protected IAccessManagerTemporalBulkPersister<String, String, String, String> eventPersister;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.WriterNodeHostedServiceWrapperBase class.
        /// </summary>
        /// <param name="logger">The logger for general logging.</param>
        public WriterNodeHostedServiceWrapperBase(ILogger<WriterNodeHostedServiceWrapperBase<TBufferFlushStrategy>> logger)
            : base (logger)
        {
        }

        #region Private/Protected Methods

        /// <summary>
        /// Starts event buffer flushing/processing.
        /// </summary>
        protected void StartEventBufferProcessing()
        {
            logger.LogInformation($"Starting {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Start();
            logger.LogInformation($"Completed starting {nameof(eventBufferFlushStrategy)}.");
        }

        /// <summary>
        /// Stops event buffer flushing/processing.
        /// </summary>
        protected void StopEventBufferProcessing()
        {
            logger.LogInformation($"Stopping {nameof(eventBufferFlushStrategy)}...");
            eventBufferFlushStrategy.Stop();
            logger.LogInformation($"Completed stopping {nameof(eventBufferFlushStrategy)}.");
        }

        /// <summary>
        /// Disposes any objects associated with event buffer flushing/processing.
        /// </summary>
        protected void DisposeEventBufferProcessors()
        {
            eventBufferFlushStrategy.Dispose();
            eventPersister.Dispose();
        }

        #endregion
    }
}
