/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Metrics;
using ApplicationMetrics.MetricLoggers;
using ApplicationLogging;
using ApplicationAccess.Persistence;

namespace ApplicationAccess.Hosting.Metrics
{
    /// <summary>
    /// Factory for instances of <see cref="WorkerThreadBufferProcessorBase"/>.
    /// </summary>
    public class MetricsBufferProcessorFactory
    {
        /// <summary>
        /// Returns a <see cref="WorkerThreadBufferProcessorBase"/> based on the specified metric buffer processing options.
        /// </summary>
        /// <param name="metricBufferProcessingOptions">Metric buffer processing options which specify the subclass of <see cref="WorkerThreadBufferProcessorBase"/> to create.</param>
        /// <param name="bufferProcessingExceptionAction">An action to invoke if an error occurs during buffer processing.  Accepts a single parameter which is the <see cref="Exception"/> containing details of the error.</param>
        /// <param name="rethrowBufferProcessingException">Whether exceptions encountered during buffer processing should be rethrown when the next metric is logged.</param>
        /// <returns>The <see cref="WorkerThreadBufferProcessorBase"/> instance.</returns>
        public WorkerThreadBufferProcessorBase GetBufferProcessor(MetricBufferProcessingOptions metricBufferProcessingOptions, Action<Exception> bufferProcessingExceptionAction, Boolean rethrowBufferProcessingException)
        {
            switch (metricBufferProcessingOptions.BufferProcessingStrategy)
            {
                case MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor:
                    return new SizeLimitedBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit.Value, bufferProcessingExceptionAction, rethrowBufferProcessingException);
                case MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor:
                    return new LoopingWorkerThreadBufferProcessor(metricBufferProcessingOptions.DequeueOperationLoopInterval.Value, bufferProcessingExceptionAction, rethrowBufferProcessingException);
                case MetricBufferProcessingStrategyImplementation.SizeLimitedLoopingWorkerThreadHybridBufferProcessor:
                    return new SizeLimitedLoopingWorkerThreadHybridBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit.Value, metricBufferProcessingOptions.DequeueOperationLoopInterval.Value, bufferProcessingExceptionAction, rethrowBufferProcessingException);
                default:
                    throw new Exception($"Encountered unhandled {nameof(MetricBufferProcessingStrategyImplementation)} '{metricBufferProcessingOptions.BufferProcessingStrategy}' while attempting to create {typeof(WorkerThreadBufferProcessorBase).Name} instance.");
            }
        }

        /// <summary>
        /// Returns a 'bufferProcessingExceptionAction' parameter for a <see cref="WorkerThreadBufferProcessorBase"/> instance based on a specified <see cref="MetricBufferProcessingFailureAction"/>.
        /// </summary>
        /// <param name="processingFailureAction">The action to take if a critical/non-recoverable error occurs whilst attempting to process the buffer(s).</param>
        /// <param name="metricLoggingComponentRetrievalFunction">A func which returns the hosting component which logs the metrics.</param>
        /// <param name="tripSwitchActuator">A <see cref="TripSwitchActuator"/> for the hosting component.</param>
        /// <param name="logger">The logger for the hosting component.</param>W
        /// <returns>An action to invoke if an error occurs during buffer processing.  Accepts a single parameter which is the <see cref="Exception"/> containing details of the error.</returns>
        /// <remarks>In client objects (usually hosted service wrapper classes) this method is called before the IMetricLoggingComponent being hosted has been initialized.  Hence if the IMetricLoggingComponent parameter is passed directly, it results in a NullReferenceException when its accessed.  Instead the IMetricLoggingComponent parameter is wrappd in a Func in parameter <paramref name="metricLoggingComponentRetrievalFunction"/> which will resolve properly when called.</remarks>
        public Action<Exception> GetBufferProcessingExceptionAction
        (
            MetricBufferProcessingFailureAction processingFailureAction, 
            Func<IMetricLoggingComponent> metricLoggingComponentRetrievalFunction, 
            TripSwitchActuator tripSwitchActuator, 
            IApplicationLogger logger
        )
        {
            const String errorLogMessage = "Exception occurred when processing the metrics buffer(s).";

            if (processingFailureAction == MetricBufferProcessingFailureAction.DisableMetricLogging)
            {
                Action<Exception> bufferProcessingExceptionAction = (Exception bufferProcessingException) =>
                {
                    logger.Log(ApplicationLogging.LogLevel.Error, errorLogMessage, bufferProcessingException);
                    logger.Log(ApplicationLogging.LogLevel.Error, "Metric logging has been disabled due to an unrecoverable error whilst processing the metrics buffer(s).");
                    metricLoggingComponentRetrievalFunction.Invoke().MetricLoggingEnabled = false;
                };

                return bufferProcessingExceptionAction;
            }
            else if (processingFailureAction == MetricBufferProcessingFailureAction.ReturnServiceUnavailable)
            {
                Action<Exception> bufferProcessingExceptionAction = (Exception bufferProcessingException) =>
                {
                    logger.Log(ApplicationLogging.LogLevel.Critical, errorLogMessage, bufferProcessingException);
                    logger.Log(ApplicationLogging.LogLevel.Critical, "Tripswitch has been actuated due to an unrecoverable error whilst processing the metrics buffer(s).");
                    tripSwitchActuator.Actuate();
                };

                return bufferProcessingExceptionAction;
            }
            else
            {
                throw new Exception($"Encountered unhandled {nameof(MetricBufferProcessingFailureAction)} '{processingFailureAction}' while attempting to create buffer processing exception action.");
            }
        }
    }
}
