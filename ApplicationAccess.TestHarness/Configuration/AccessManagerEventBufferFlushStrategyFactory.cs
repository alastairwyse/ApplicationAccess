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
using ApplicationAccess.Persistence;
using ApplicationMetrics;

namespace ApplicationAccess.TestHarness.Configuration
{
    class AccessManagerEventBufferFlushStrategyFactory
    {
        /// <summary>
        /// Creates and instance of <see cref="IAccessManagerEventBufferFlushStrategy"/> based on the specified configuration.
        /// </summary>
        /// <param name="config">The configuration to use to create the buffer flush strategy.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="metricLogger">The metric logger to set on the buffer flush strategy.</param>
        /// <returns>A container class containing the buffer flush strategy and associated actions.</returns>
        public BufferFlushStrategyFactoryResult<IAccessManagerEventBufferFlushStrategy> MakeFlushStrategy
        (
            PersisterBufferFlushStrategyConfiguration config, 
            Action<BufferFlushingException> flushingExceptionAction, 
            IMetricLogger metricLogger
        )
        {
            switch (config.BufferImplementation)
            {
                case AccessManagerEventBufferFlushStrategyImplementation.LoopingWorkerThreadBufferFlushStrategy:
                    var loopingWorkerThreadBufferFlushStrategy = new LoopingWorkerThreadBufferFlushStrategy(config.FlushLoopInterval, metricLogger, flushingExceptionAction);
                    return new BufferFlushStrategyFactoryResult<IAccessManagerEventBufferFlushStrategy>()
                    {
                        BufferFlushStrategy = loopingWorkerThreadBufferFlushStrategy,
                        StartAction = () => { loopingWorkerThreadBufferFlushStrategy.Start(); },
                        StopAction = () => { loopingWorkerThreadBufferFlushStrategy.Stop(); },
                        DisposeAction = () => { loopingWorkerThreadBufferFlushStrategy.Dispose(); },
                    };
                case AccessManagerEventBufferFlushStrategyImplementation.SizeLimitedBufferFlushStrategy:
                    var sizeLimitedBufferFlushStrategy = new SizeLimitedBufferFlushStrategy(config.BufferSizeLimit, metricLogger, flushingExceptionAction);
                    return new BufferFlushStrategyFactoryResult<IAccessManagerEventBufferFlushStrategy>()
                    {
                        BufferFlushStrategy = sizeLimitedBufferFlushStrategy,
                        StartAction = () => { sizeLimitedBufferFlushStrategy.Start(); },
                        StopAction = () => { sizeLimitedBufferFlushStrategy.Stop(); },
                        DisposeAction = () => { sizeLimitedBufferFlushStrategy.Dispose(); },
                    };
                case AccessManagerEventBufferFlushStrategyImplementation.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy:
                    var sizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
                    (
                        config.BufferSizeLimit, 
                        config.FlushLoopInterval,
                        flushingExceptionAction, 
                        metricLogger
                    );
                    return new BufferFlushStrategyFactoryResult<IAccessManagerEventBufferFlushStrategy>()
                    {
                        BufferFlushStrategy = sizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy,
                        StartAction = () => { sizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Start(); },
                        StopAction = () => { sizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Stop(); },
                        DisposeAction = () => { sizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Dispose(); },
                    };
                default:
                    throw new Exception($"Encountered unhandled {typeof(AccessManagerEventBufferFlushStrategyImplementation).Name} '{config.BufferImplementation}'.");
            }
        }
    }
}
