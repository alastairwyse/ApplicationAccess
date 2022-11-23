/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.TestHarness.Configuration
{
    class MetricsBufferProcessingStrategyFactory
    {
        /// <summary>
        /// Creates and instance of <see cref="IBufferProcessingStrategy"/> based on the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to use to create the buffer processing strategy.</param>
        /// <returns>A container class containing the buffer processing strategy and associated actions.</returns>
        public BufferFlushStrategyFactoryResult<IBufferProcessingStrategy> MakeProcessingStrategy(MetricsBufferConfiguration config)
        {
            switch (config.BufferImplementation)
            {
                case MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor:
                    var loopingWorkerThreadBufferProcessor = new LoopingWorkerThreadBufferProcessor(config.DequeueOperationLoopInterval);
                    return new BufferFlushStrategyFactoryResult<IBufferProcessingStrategy>()
                    {
                        BufferFlushStrategy = loopingWorkerThreadBufferProcessor,
                        // Start() and Stop() are called by the MetricLoggerBuffer implementation wrapping the flush strategy
                        StartAction = () => { },
                        StopAction = () => { },
                        DisposeAction = () => { loopingWorkerThreadBufferProcessor.Dispose(); },
                    };
                case MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor:
                    var sizeLimitedBufferProcessor = new SizeLimitedBufferProcessor(config.BufferSizeLimit);
                    return new BufferFlushStrategyFactoryResult<IBufferProcessingStrategy>()
                    {
                        BufferFlushStrategy = sizeLimitedBufferProcessor,
                        // Start() and Stop() are called by the MetricLoggerBuffer implementation wrapping the flush strategy
                        StartAction = () => { },
                        StopAction = () => { },
                        DisposeAction = () => { sizeLimitedBufferProcessor.Dispose(); },
                    };
                default:
                    throw new Exception($"Encountered unhandled {typeof(MetricBufferProcessingStrategyImplementation).Name} '{config.BufferImplementation}'.");
            }
        }
    }
}
