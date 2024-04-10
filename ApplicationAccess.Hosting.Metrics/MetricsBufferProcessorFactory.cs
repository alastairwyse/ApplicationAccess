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
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Metrics
{
    /// <summary>
    /// Factory for instances of <see cref="WorkerThreadBufferProcessorBase/>.
    /// </summary>
    public class MetricsBufferProcessorFactory
    {
        /// <summary>
        /// Returns a <see cref="WorkerThreadBufferProcessorBase"/> based on the specified metric buffer processing options.
        /// </summary>
        /// <param name="metricBufferProcessingOptions">Metric buffer processing options which specify the subclass of <see cref="WorkerThreadBufferProcessorBase"/> to create.</param>
        /// <returns>The <see cref="WorkerThreadBufferProcessorBase"/> instance.</returns>
        public WorkerThreadBufferProcessorBase GetBufferProcessor(MetricBufferProcessingOptions metricBufferProcessingOptions)
        {
            switch (metricBufferProcessingOptions.BufferProcessingStrategy)
            {
                case MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor:
                    return new SizeLimitedBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit);
                case MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor:
                    return new LoopingWorkerThreadBufferProcessor(metricBufferProcessingOptions.DequeueOperationLoopInterval);
                case MetricBufferProcessingStrategyImplementation.SizeLimitedLoopingWorkerThreadHybridBufferProcessor:
                    return new SizeLimitedLoopingWorkerThreadHybridBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit, metricBufferProcessingOptions.DequeueOperationLoopInterval);
                default:
                    throw new Exception($"Encountered unhandled {nameof(MetricBufferProcessingStrategyImplementation)} '{metricBufferProcessingOptions.BufferProcessingStrategy}' while attempting to create {typeof(WorkerThreadBufferProcessorBase).Name} instance.");
            }
        }
    }
}
