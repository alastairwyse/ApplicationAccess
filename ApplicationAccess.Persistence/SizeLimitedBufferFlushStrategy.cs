/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
    /// A buffer flush strategy that flushes/processes the buffers when the total number of buffered events reaches a pre-defined limit.
    /// </summary>
    public class SizeLimitedBufferFlushStrategy : WorkerThreadBufferFlushStrategyBase
    {
        /// <summary>The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</summary>
        protected Int32 bufferSizeLimit;
        /// <summary>Signal which is used to trigger the worker thread when the specified number of events are buffered.</summary>
        protected ManualResetEvent bufferProcessSignal;

        /// <inheritdoc/>
        public override Int32 UserEventBufferItemCount
        {
            set
            {
                base.UserEventBufferItemCount = value;
                // Since the Flush() method of AccessManagerTemporalEventPersisterBuffer sets item counts to 0, need to ensure those setter calls don't trigger repeated flushes by filtering them out
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 GroupEventBufferItemCount
        {
            set
            {
                base.GroupEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 UserToGroupMappingEventBufferItemCount
        {
            set
            {
                base.UserToGroupMappingEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 GroupToGroupMappingEventBufferItemCount
        {
            set
            {
                base.GroupToGroupMappingEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set
            {
                base.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set
            {
                base.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 EntityTypeEventBufferItemCount
        {
            set
            {
                base.EntityTypeEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 EntityEventBufferItemCount
        {
            set
            {
                base.EntityEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 UserToEntityMappingEventBufferItemCount
        {
            set
            {
                base.UserToEntityMappingEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <inheritdoc/>
        public override Int32 GroupToEntityMappingEventBufferItemCount
        {
            set
            {
                base.GroupToEntityMappingEventBufferItemCount = value;
                if (value > 0)
                {
                    CheckBufferLimitReached();
                }
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        public SizeLimitedBufferFlushStrategy(Int32 bufferSizeLimit)
            : base()
        {
            if (bufferSizeLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSizeLimit), $"Parameter '{nameof(bufferSizeLimit)}' with value {bufferSizeLimit} cannot be less than 1.");

            base.BufferFlushingAction = () =>
            {
                while (stopMethodCalled == false)
                {
                    bufferProcessSignal.WaitOne();
                    if (stopMethodCalled == false)
                    {
                        OnBufferFlushed(EventArgs.Empty);
                        metricLogger.Increment(new BufferFlushOperationTriggeredBySizeLimit());
                    }
                    bufferProcessSignal.Reset();
                }
            };
            this.bufferSizeLimit = bufferSizeLimit;
            bufferProcessSignal = new ManualResetEvent(false);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public SizeLimitedBufferFlushStrategy(Int32 bufferSizeLimit, Action<BufferFlushingException> flushingExceptionAction)
            : this(bufferSizeLimit)
        {
            base.flushingExceptionAction = flushingExceptionAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public SizeLimitedBufferFlushStrategy(Int32 bufferSizeLimit, IMetricLogger metricLogger)
            : this(bufferSizeLimit)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public SizeLimitedBufferFlushStrategy(Int32 bufferSizeLimit, IMetricLogger metricLogger, Action<BufferFlushingException> flushingExceptionAction)
            : this(bufferSizeLimit)
        {
            this.metricLogger = metricLogger;
            base.flushingExceptionAction = flushingExceptionAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SizeLimitedBufferFlushStrategy class.
        /// </summary>
        /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        /// <param name="workerThreadCompleteSignal">Signal that will be set when the worker thread processing is complete (for unit testing).</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public SizeLimitedBufferFlushStrategy(Int32 bufferSizeLimit, IMetricLogger metricLogger, Action<BufferFlushingException> flushingExceptionAction, ManualResetEvent workerThreadCompleteSignal)
            : this(bufferSizeLimit, metricLogger, flushingExceptionAction)
        {
            base.workerThreadCompleteSignal = workerThreadCompleteSignal;
        }

        /// <summary>
        /// Stops the worker thread which performs buffer flushes.
        /// </summary>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        /// <remarks>This is the same as the base class method, but with the addition of a call to bufferProcessSignal.Set()... without this, the call to JoinWorkerThread() will wait forever (since the worker thread is waiting on member 'bufferProcessSignal').</remarks>
        public override void Stop()
        {
            stopMethodCalled = true;
            // Signal the worker thread to start processing
            bufferProcessSignal.Set();
            // Wait for the worker thread to finish
            JoinWorkerThread();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Checks whether the size limit for the buffers has been reached, and if so signals the worker thread to process/flush the buffers.
        /// </summary>
        protected virtual void CheckBufferLimitReached()
        {
            if (TotalEventsBuffered >= bufferSizeLimit)
            {
                bufferProcessSignal.Set();
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
                        bufferProcessSignal.Dispose();
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
