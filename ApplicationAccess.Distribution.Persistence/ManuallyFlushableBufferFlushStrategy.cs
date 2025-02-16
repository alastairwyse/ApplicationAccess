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
using System.Threading;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Persistence;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution.Persistence
{
    /// <summary>
    /// A specialization of <see cref="SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy"/> which additionally allows manual flushing of buffered events.
    /// </summary>
    public class ManuallyFlushableBufferFlushStrategy : SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy, IManuallyFlushableBufferFlushStrategy
    {
        /// <summary>Mutual exclusion lock object on raises of event 'OnBufferFlushed'.</summary>
        protected Object onBufferFlushedLockObject;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.ManuallyFlushableBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public ManuallyFlushableBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction)
            : base(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, flushingExceptionAction)
        {
            InitializeBufferFlushingAction();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.ManuallyFlushableBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ManuallyFlushableBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction, IMetricLogger metricLogger)
            : base(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, flushingExceptionAction, metricLogger)
        {
            InitializeBufferFlushingAction();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.ManuallyFlushableBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        public ManuallyFlushableBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction, IDateTimeProvider dateTimeProvider)
            : base(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, flushingExceptionAction, dateTimeProvider)
        {
            InitializeBufferFlushingAction();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.ManuallyFlushableBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ManuallyFlushableBufferFlushStrategy(Int32 bufferSizeLimit, Int32 flushLoopInterval, Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction, IDateTimeProvider dateTimeProvider, IMetricLogger metricLogger)
            : base(bufferSizeLimit, flushLoopInterval, flushRemainingEventsAfterException, flushingExceptionAction, dateTimeProvider, metricLogger)
        {
            InitializeBufferFlushingAction();
        }

        /// <inheritdoc/>
        public void FlushBuffers()
        {
            ImplementBufferFlush();
            metricLogger.Increment(new BufferFlushOperationTriggeredByManualAction());
        }

        #region Private/Protected Methods

        /// <summary>
        /// Immediately flushes any buffered events synchronously (i.e. returns when the flush operation is complete).
        /// </summary>
        protected void ImplementBufferFlush()
        {
            // The implementation below doesn't adhere to the method of flushing used by the looping worker thread and size trigger in this classes' bases.
            //   The correct way to do this would be to bufferProcessSignal.Set(), and let the flushing worker thread actually implement the flush.
            //   The problem with that approach in this context is that it is an async operation, but here we need to wait until the flush process is
            //   complete before returning.  Hence here we override the 'BufferFlushingAction' property and put a lock around the region which raises
            //   event 'OnBufferFlushed'.  Then we call that region from the public FlushBuffer() method.

            lock (onBufferFlushedLockObject)
            {
                isFlushing = true;
                OnBufferFlushed(EventArgs.Empty);
                lock (lastFlushCompleteTimeLockObject)
                {
                    lastFlushCompleteTime = dateTimeProvider.UtcNow();
                }
                isFlushing = false;
            }
        }

        /// <summary>
        /// Initializes property 'BufferFlushingAction'.
        /// </summary>
        protected void InitializeBufferFlushingAction()
        {
            onBufferFlushedLockObject = new Object();

            base.BufferFlushingAction = () =>
            {
                while (stopMethodCalled == false)
                {
                    bufferProcessSignal.WaitOne();
                    if (stopMethodCalled == false)
                    {
                        ImplementBufferFlush();
                        bufferProcessSignal.Reset();
                    }
                }
            };
        }

        #endregion
    }
}
