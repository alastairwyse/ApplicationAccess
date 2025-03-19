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
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Base for classes which host an ApplicationMetrics component node as an <see cref="IHostedService"/>, to allow hosting in ASP.NET.
    /// </summary>
    public abstract class NodeHostedServiceWrapperBase
    {
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>The logger for general logging.</summary>
        protected ILogger<NodeHostedServiceWrapperBase> logger;
        /// <summary>The buffer processing for the logger for metrics.</summary>
        protected WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.NodeHostedServiceWrapperBase class.
        /// </summary>
        /// <param name="logger">The logger for general logging.</param>
        public NodeHostedServiceWrapperBase(ILogger<NodeHostedServiceWrapperBase> logger)
        {
            this.logger = logger;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Starts metric logging if the 'metricLogger' member implements <see cref="MetricLoggerBuffer"/>.
        /// </summary>
        protected void StartMetricLogging()
        {
            if (metricLogger.GetType().IsAssignableTo(typeof(MetricLoggerBuffer)) == true)
            {
                logger.LogInformation($"Starting {nameof(metricLogger)}...");
                ((MetricLoggerBuffer)metricLogger).Start();
                logger.LogInformation($"Completed starting {nameof(metricLogger)}.");
            }
        }

        /// <summary>
        /// Stops metric logging if the 'metricLogger' member implements <see cref="MetricLoggerBuffer"/>.
        /// </summary>
        protected void StopMetricLogging()
        {
            if (metricLogger.GetType().IsAssignableTo(typeof(MetricLoggerBuffer)) == true)
            {
                logger.LogInformation($"Stopping {nameof(metricLogger)}...");
                ((MetricLoggerBuffer)metricLogger).Stop();
                logger.LogInformation($"Completed stopping {nameof(metricLogger)}.");
            }
        }

        /// <summary>
        /// Disposes any objects associated with metric logging.
        /// </summary>
        protected void DisposeMetricLogger()
        {
            if (metricLoggerBufferProcessingStrategy != null)
            {
                metricLoggerBufferProcessingStrategy.Dispose();
            }
            if (metricLogger.GetType().IsAssignableTo(typeof(IDisposable)) == true)
            {
                ((IDisposable)metricLogger).Dispose();
            }
        }

        #endregion
    }
}
