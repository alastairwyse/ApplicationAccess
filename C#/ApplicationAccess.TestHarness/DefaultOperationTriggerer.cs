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
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Default implementation of <see cref="IOperationTriggerer"/>.
    /// </summary>
    public class DefaultOperationTriggerer : IOperationTriggerer
    {
        /// <summary>Whether a request to stop the worker thread has been received via the Stop() method.</summary>
        protected volatile Boolean stopMethodCalled;
        /// <summary>Worker thread which triggers operations.</summary>
        protected Thread operationTriggeringWorkerThread;
        /// <summary>Used to accurately measure frequency of operations triggered.</summary>
        protected Stopwatch stopwatch;
        /// <summary>The timestamp at which the Start() method was called.</summary>
        protected DateTime startTime;
        /// <summary>Signal which is waited on to trigger the next operation.</summary>
        protected AutoResetEvent triggerSignal;
        /// <summary>Signal which notifies when an operation is performed.</summary>
        protected AutoResetEvent operationInitiatedSignal;
        /// <summary>Holds the times of previous operation initiations.  The <see cref="LinkedList{T}.Last">Last</see> property holds the time of the most recently initiated.</summary>
        protected LinkedList<DateTime> previousInitiationTimeWindow;
        /// <summary>The target number of operations per second to trigger.</summary>
        protected Double targetOperationsPerSecond;
        /// <summary>The number of items to keep in member 'previousInitiationTimeWindow'.</summary>
        protected Int32 previousInitiationTimeWindowSize;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.DefaultOperationTriggerer class.
        /// </summary>
        /// <param name="targetOperationsPerSecond">The target number of operations per second to trigger.</param>
        /// <param name="previousInitiationTimeWindowSize">The number of previous operation initiation timestamps to keep, in order to calculate the number of operations triggered per second.</param>
        public DefaultOperationTriggerer(Double targetOperationsPerSecond, Int32 previousInitiationTimeWindowSize)
        {
            if (targetOperationsPerSecond <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(targetOperationsPerSecond), $"Parameter '{nameof(targetOperationsPerSecond)}' with value {targetOperationsPerSecond} cannot be less than or equal to 0.");
            if (previousInitiationTimeWindowSize < 0)
                throw new ArgumentOutOfRangeException(nameof(previousInitiationTimeWindowSize), $"Parameter '{nameof(previousInitiationTimeWindowSize)}' with value {previousInitiationTimeWindowSize} cannot be less than 0.");

            stopMethodCalled = false;
            stopwatch = new Stopwatch();
            triggerSignal = new AutoResetEvent(false);
            operationInitiatedSignal = new AutoResetEvent(false);
            previousInitiationTimeWindow = new LinkedList<DateTime>();
            this.targetOperationsPerSecond = targetOperationsPerSecond;
            this.previousInitiationTimeWindowSize = previousInitiationTimeWindowSize;
            disposed = false;
        }

        /// <inheritdoc/>
        public void Start()
        {
            stopwatch.Reset();
            stopwatch.Start();
            startTime = DateTime.UtcNow;
            operationTriggeringWorkerThread = new Thread(new ThreadStart(WorkerThreadRoutine));
            operationTriggeringWorkerThread.Name = "ApplicationAccess.TestHarness.DefaultOperationTriggerer operation triggering worker thread.";
            operationTriggeringWorkerThread.IsBackground = true;
            operationTriggeringWorkerThread.Start();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            stopMethodCalled = true; 
            triggerSignal.Set();
            operationInitiatedSignal.Set();
            // TODO: Need to make sure the in the worked thread loop, above statements will allow it to step out of the loop without locking up
            operationTriggeringWorkerThread.Join();
            stopwatch.Stop();
        }

        /// <inheritdoc/>
        public void WaitForNextTrigger()
        {
            triggerSignal.WaitOne();
        }

        /// <inheritdoc/>
        public void NotifyOperationInitiated()
        {
            DateTime now = GetStopWatchUtcNow();
            lock (previousInitiationTimeWindow)
            {
                previousInitiationTimeWindow.AddLast(now);
                while (previousInitiationTimeWindow.Count > previousInitiationTimeWindowSize)
                {
                    previousInitiationTimeWindow.RemoveFirst();
                }
            }
            operationInitiatedSignal.Set();
        }

        #region Private/Protected Methods

        protected void WorkerThreadRoutine()
        {
            while (stopMethodCalled == false)
            {
                Int32 waitTime = CalculateWaitTimeForNextOperation();
                if (waitTime > 0)
                {
                    Thread.Sleep(waitTime);
                }
                // Trigger the next operation
                triggerSignal.Set();
                // Wait for the main thread to signal that it's initiated the operation
                operationInitiatedSignal.WaitOne();
            }
        }

        /// <summary>
        /// Calculates the wait time for the next operation in milliseconds.
        /// </summary>
        protected Int32 CalculateWaitTimeForNextOperation()
        {
            Double waitTime = 1000.0 / targetOperationsPerSecond;
            lock(previousInitiationTimeWindow)
            {
                if (previousInitiationTimeWindow.Count > 1)
                {
                    Double timeWindowTotalLength = (previousInitiationTimeWindow.Last.Value - previousInitiationTimeWindow.First.Value).TotalMilliseconds;
                    Double averageOperationInterval = timeWindowTotalLength / Convert.ToDouble(previousInitiationTimeWindow.Count - 1);
                    Double actualOperationsPerSecond = 1000.0 / averageOperationInterval;
                    Double accuracyRatio = actualOperationsPerSecond / targetOperationsPerSecond;
                    // Compensation factor doubles the difference between the accuracy ratio and 1 (to try and move more quickly back to the target rate)
                    Double compensationFactor = accuracyRatio - (1.0 - accuracyRatio);
                    // Multiple the base wait time be the compensation factor to try to get closer to the target rate
                    waitTime = waitTime * compensationFactor;
                }
            }

            if (waitTime < 0.0)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(Math.Round(waitTime));
            }
        }

        protected DateTime GetStopWatchUtcNow()
        {
            return startTime.AddTicks(stopwatch.ElapsedTicks);
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the DefaultOperationTriggerer.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~DefaultOperationTriggerer()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    triggerSignal.Dispose();
                    operationInitiatedSignal.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
