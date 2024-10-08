﻿/*
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
using System.Threading;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// A buffer flush strategy that flushes/processes the buffered events at a regular interval.
    /// </summary>
    public class LoopingWorkerThreadBufferFlushStrategy : WorkerThreadBufferFlushStrategyBase
    {
        /// <summary>The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</summary>
        protected Int32 flushLoopInterval;
        /// <summary>The number of iterations of the worker thread to flush/process.</summary>
        protected Int32 flushLoopIterationCount;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
        /// </summary>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        public LoopingWorkerThreadBufferFlushStrategy(Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException)
            : base(flushRemainingEventsAfterException)
        {
            if (flushLoopInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(flushLoopInterval), $"Parameter '{nameof(flushLoopInterval)}' with value {flushLoopInterval} cannot be less than 1.");

            base.BufferFlushingAction = () =>
            {
                while (stopMethodCalled == false)
                {
                    OnBufferFlushed(EventArgs.Empty);
                    metricLogger.Increment(new BufferFlushOperationTriggeredByLoopIntervalExpiration());
                    Thread.Sleep(flushLoopInterval);
                    metricLogger.Add(new BufferFlushLoopIntervalSleepTime(), flushLoopInterval);
                    // If the code is being tested, break out of processing after the specified number of iterations
                    if (workerThreadCompleteSignal != null)
                    {
                        flushLoopIterationCount--;
                        if (flushLoopIterationCount == 0)
                        {
                            break;
                        }
                    }
                }
            };
            this.flushLoopInterval = flushLoopInterval;
            workerThreadCompleteSignal = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
        /// </summary>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public LoopingWorkerThreadBufferFlushStrategy(Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction)
            : this(flushLoopInterval, flushRemainingEventsAfterException)
        {
            base.flushingExceptionAction = flushingExceptionAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
        /// </summary>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public LoopingWorkerThreadBufferFlushStrategy(Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, IMetricLogger metricLogger)
            : this(flushLoopInterval, flushRemainingEventsAfterException)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
        /// </summary>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public LoopingWorkerThreadBufferFlushStrategy(Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, IMetricLogger metricLogger, Action<BufferFlushingException> flushingExceptionAction)
            : this(flushLoopInterval, flushRemainingEventsAfterException)
        {
            this.metricLogger = metricLogger;
            base.flushingExceptionAction = flushingExceptionAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
        /// </summary>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="workerThreadCompleteSignal">Signal that will be set when the worker thread processing is complete (for unit testing).</param>
        /// <param name="flushLoopIterationCount">The number of iterations of the worker thread to flush/process.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public LoopingWorkerThreadBufferFlushStrategy(Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, IMetricLogger metricLogger, Action<BufferFlushingException> flushingExceptionAction, ManualResetEvent workerThreadCompleteSignal, Int32 flushLoopIterationCount)
            : this(flushLoopInterval, flushRemainingEventsAfterException, metricLogger, flushingExceptionAction)
        {
            if (flushLoopIterationCount < 1)
                throw new ArgumentOutOfRangeException(nameof(flushLoopIterationCount), $"Parameter '{nameof(flushLoopIterationCount)}' with value {flushLoopIterationCount} cannot be less than 1.");

            base.workerThreadCompleteSignal = workerThreadCompleteSignal;
            this.flushLoopIterationCount = flushLoopIterationCount;
        }
    }
}
