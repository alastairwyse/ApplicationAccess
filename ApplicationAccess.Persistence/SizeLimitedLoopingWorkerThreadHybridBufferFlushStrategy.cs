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
using System.Threading;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// A buffer flush strategy that flushes/processes the buffers when either the total number of buffered events reaches a pre-defined limit or a specified looping interval expires, whichever occurs first.
    /// </summary>
    public class SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy : SizeLimitedBufferFlushStrategy
    {
        /// <summary>The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</summary>
        protected Int32 flushLoopInterval;
        /// <summary>The provider to use for the current date and time.</summary>
        protected IDateTimeProvider dateTimeProvider;
        /// <summary>Indicates whether the worker thread is currently flushing/processing the buffer contents.</summary>
        protected volatile Boolean isFlushing;
        /// <summary>Mutual exclusion lock object for member 'lastFlushCompleteTime'.</summary>
        protected Object lastFlushCompleteTimeLockObject;
        /// <summary>The time at which the last buffer flushing/processing completed.</summary>
        protected DateTime lastFlushCompleteTime;
        /// <summary>Thread which loops, triggering buffer flushing/processing at specified intervals.</summary>
        protected Thread loopingTriggerThread;
        /// <summary>Signal that is waited on each time an iteration of the looping trigger thread completes (for unit testing).</summary>
        protected AutoResetEvent loopingTriggerThreadLoopCompleteSignal;
        /// <summary>The most recent interval that the looping trigger thread waited for between iterations (for unit testing).</summary>
        protected Int32 lastWaitInterval;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction)
            : base(bufferSizeLimit, flushRemainingEventsAfterException, flushingExceptionAction)
        {
            if (flushLoopInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(flushLoopInterval), $"Parameter '{nameof(flushLoopInterval)}' with value {flushLoopInterval} cannot be less than 1.");

            this.flushLoopInterval = flushLoopInterval;
            dateTimeProvider = new StopwatchDateTimeProvider();
            isFlushing = false;
            lastFlushCompleteTimeLockObject = new Object();
            loopingTriggerThreadLoopCompleteSignal = null;

            base.BufferFlushingAction = () =>
            {
                while (stopMethodCalled == false)
                {
                    bufferProcessSignal.WaitOne();
                    if (stopMethodCalled == false)
                    {
                        isFlushing = true; 
                        OnBufferFlushed(EventArgs.Empty);
                        lock (lastFlushCompleteTimeLockObject)
                        {
                            lastFlushCompleteTime = dateTimeProvider.UtcNow();
                        }
                        isFlushing = false;
                        bufferProcessSignal.Reset();
                    }
                }
            };

            loopingTriggerThread = new Thread(() =>
            {
                DateTime previousLoopIterationLastFlushCompleteTime;
                lock (lastFlushCompleteTimeLockObject)
                {
                    previousLoopIterationLastFlushCompleteTime = lastFlushCompleteTime;
                }
               
                while (stopMethodCalled == false)
                {
                    DateTime lastFlushCompleteTimeCopy;
                    lock (lastFlushCompleteTimeLockObject)
                    {
                        lastFlushCompleteTimeCopy = lastFlushCompleteTime;
                    }
                    if (isFlushing == false)
                    {
                        if (lastFlushCompleteTimeCopy != previousLoopIterationLastFlushCompleteTime)
                        {
                            // A flush has occurred since the last loop iteration
                            //   Sleep for the flush loop interval less the time since the last flush completed
                            metricLogger.Increment(new SizeLimitBufferFlushTriggeredDuringLoopInterval());
                            Int32 sleepTime = Convert.ToInt32((lastFlushCompleteTimeCopy.AddMilliseconds(flushLoopInterval) - dateTimeProvider.UtcNow()).TotalMilliseconds);
                            lastWaitInterval = sleepTime;
                            previousLoopIterationLastFlushCompleteTime = lastFlushCompleteTimeCopy;
                            if (sleepTime > 0)
                            {
                                Thread.Sleep(sleepTime);
                                metricLogger.Add(new BufferFlushLoopIntervalSleepTime(), sleepTime);
                            }
                        }
                        else
                        {
                            // No flush has occurred since the last loop iteration so trigger a buffer flush/process
                            previousLoopIterationLastFlushCompleteTime = lastFlushCompleteTimeCopy;
                            bufferProcessSignal.Set();
                            metricLogger.Increment(new BufferFlushOperationTriggeredByLoopIntervalExpiration());
                            lastWaitInterval = flushLoopInterval;
                            Thread.Sleep(flushLoopInterval);
                            metricLogger.Add(new BufferFlushLoopIntervalSleepTime(), lastWaitInterval);
                        }
                    }
                    else
                    {
                        // Buffers are currently being flushed/processed so sleep for the full loop interval
                        metricLogger.Increment(new BufferFlushLoopIntervalExpirationWhileFlushOperationInProgress());
                        previousLoopIterationLastFlushCompleteTime = lastFlushCompleteTimeCopy;
                        lastWaitInterval = flushLoopInterval;
                        Thread.Sleep(flushLoopInterval);
                        metricLogger.Add(new BufferFlushLoopIntervalSleepTime(), lastWaitInterval);
                    }

                    if (loopingTriggerThreadLoopCompleteSignal != null && stopMethodCalled == false)
                    {
                        loopingTriggerThreadLoopCompleteSignal.WaitOne();
                        Thread.Sleep(250);
                    }
                }
            });
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction, IMetricLogger metricLogger)
            : this(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, flushingExceptionAction)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        public SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction, IDateTimeProvider dateTimeProvider)
            : this(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, flushingExceptionAction)
        {
            this.dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction, IDateTimeProvider dateTimeProvider, IMetricLogger metricLogger)
            : this(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, flushingExceptionAction, dateTimeProvider)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <param name="loopingTriggerThreadLoopCompleteSignal">Signal that is waited on each time an iteration of the looping trigger thread completes (for unit testing).</param>
        /// <param name="workerThreadCompleteSignal">Signal that will be set when the worker thread processing is complete (for unit testing).</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, IDateTimeProvider dateTimeProvider, AutoResetEvent loopingTriggerThreadLoopCompleteSignal, ManualResetEvent workerThreadCompleteSignal)
            : this(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, (BufferFlushingException bufferFlushingException) => { }, dateTimeProvider)
        {
            this.loopingTriggerThreadLoopCompleteSignal = loopingTriggerThreadLoopCompleteSignal;
            base.workerThreadCompleteSignal = workerThreadCompleteSignal;
        }

        /// <summary>
        /// Starts the worker thread which performs buffer flushes.
        /// </summary>
        public override void Start()
        {
            lock (lastFlushCompleteTimeLockObject)
            {
                lastFlushCompleteTime = dateTimeProvider.UtcNow();
            }
            base.Start();
            loopingTriggerThread.Name = $"{this.GetType().FullName} looping buffer flush trigger thread.";
            loopingTriggerThread.IsBackground = true;
            loopingTriggerThread.Start();
        }

        /// <summary>
        /// Stops the worker thread which performs buffer flushes.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            loopingTriggerThread.Join();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Checks whether the size limit for the buffers has been reached, and if so signals the worker thread to process/flush the buffers.
        /// </summary>
        protected override void CheckBufferLimitReached()
        {
            if (TotalEventsBuffered >= bufferSizeLimit)
            {
                bufferProcessSignal.Set();
                metricLogger.Increment(new BufferFlushOperationTriggeredBySizeLimit());
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    if (disposing)
                    {
                        // Free other state (managed objects).
                        if (loopingTriggerThreadLoopCompleteSignal != null)
                        {
                            loopingTriggerThreadLoopCompleteSignal.Dispose();
                        }
                    }
                    // Free your own state (unmanaged objects).

                    // Set large fields to null.
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        #endregion
    }
}
