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
using System.Runtime.ExceptionServices;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A reader node refresh strategy that refreshes/updates a reader node at a regular interval, using a worker thread.
    /// </summary>
    public class LoopingWorkerThreadReaderNodeRefreshStrategy : IReaderNodeRefreshStrategy, IDisposable
    {
        /// <summary>Worker thread which implements the strategy to refresh the contents of reader node.</summary>
        protected Thread readerNodeRefreshWorkerThread;
        /// <summary>Set with any exception which occurrs on the worker thread when refreshing the reader node (including stack trace and context info provided by the <see cref="ExceptionDispatchInfo"/> class).  Null if no exception has occurred.</summary>
        protected ExceptionDispatchInfo refreshExceptionDispatchInfo;
        /// <summary>The time to wait (in milliseconds) between reader node refreshes.</summary>
        protected Int32 refreshLoopInterval;
        /// <summary>Whether request to stop the worker thread has been received via the Stop() method.</summary>
        protected volatile Boolean stopMethodCalled;
        /// <summary>Signal that is set after the worker thread completes, either via explicit stopping or an exception occurring (for unit testing).</summary>
        protected ManualResetEvent workerThreadCompleteSignal;
        /// <summary>The number of iterations of the worker thread to flush/process.</summary>
        protected Int32 flushLoopIterationCount;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected bool disposed;

        /// <inheritdoc/>
        public event EventHandler ReaderNodeRefreshed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.LoopingWorkerThreadReaderNodeRefreshStrategy class.
        /// </summary>
        /// <param name="refreshLoopInterval">The time to wait (in milliseconds) between reader node refreshes.</param>
        public LoopingWorkerThreadReaderNodeRefreshStrategy(Int32 refreshLoopInterval)
        {
            if (refreshLoopInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(refreshLoopInterval), $"Parameter '{nameof(refreshLoopInterval)}' with value {refreshLoopInterval} cannot be less than 1.");

            this.refreshLoopInterval = refreshLoopInterval;
            refreshExceptionDispatchInfo = null;
            stopMethodCalled = false;
            workerThreadCompleteSignal = null;
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.LoopingWorkerThreadReaderNodeRefreshStrategy class.
        /// </summary>
        /// <param name="refreshLoopInterval">The time to wait (in milliseconds) between reader node refreshes.</param>
        /// <param name="workerThreadCompleteSignal">Signal that will be set when the worker thread processing is complete (for unit testing).</param>
        /// <param name="flushLoopIterationCount">The number of iterations of the worker thread to flush/process.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public LoopingWorkerThreadReaderNodeRefreshStrategy(Int32 refreshLoopInterval, ManualResetEvent workerThreadCompleteSignal, Int32 flushLoopIterationCount)
            : this(refreshLoopInterval)
        {
            this.workerThreadCompleteSignal = workerThreadCompleteSignal;
            this.flushLoopIterationCount = flushLoopIterationCount;
        }

        /// <inheritdoc/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public void NotifyQueryMethodCalled()
        {
            CheckAndThrowRefreshException();
        }

        /// <summary>
        /// Starts the worker thread which performs reader node refreshes.
        /// </summary>
        public void Start()
        {
            readerNodeRefreshWorkerThread = new Thread(() =>
            {
                while (stopMethodCalled == false)
                {
                    Thread.Sleep(refreshLoopInterval);
                    try
                    {
                        OnReaderNodeRefreshed(EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        var wrappedException = new ReaderNodeRefreshException($"Exception occurred on reader node refreshing worker thread at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                        Interlocked.Exchange(ref refreshExceptionDispatchInfo, ExceptionDispatchInfo.Capture(wrappedException));
                    }
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
                if (workerThreadCompleteSignal != null)
                {
                    workerThreadCompleteSignal.Set();
                }
            });
            stopMethodCalled = false;
            readerNodeRefreshWorkerThread.Name = $"{this.GetType().FullName} reader node refreshing worker thread.";
            readerNodeRefreshWorkerThread.IsBackground = true;
            readerNodeRefreshWorkerThread.Start();
        }

        /// <summary>
        /// Stops the worker thread which performs reader node refreshes.
        /// </summary>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public void Stop()
        {
            // Check whether any exceptions have occurred on the worker thread and re-throw
            CheckAndThrowRefreshException();
            stopMethodCalled = true;
            // Wait for the worker thread to finish
            JoinWorkerThread();
            // Check for exceptions again incase one occurred after joining the worker thread
            CheckAndThrowRefreshException();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Raises the ReaderNodeRefreshed event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected virtual void OnReaderNodeRefreshed(EventArgs e)
        {
            if (ReaderNodeRefreshed != null)
            {
                ReaderNodeRefreshed(this, e);
            }
        }

        /// <summary>
        /// Calls Join() on the worker thread, waiting until it terminates.
        /// </summary>
        protected void JoinWorkerThread()
        {
            if (readerNodeRefreshWorkerThread != null)
            {
                readerNodeRefreshWorkerThread.Join();
            }
        }

        /// <summary>
        /// Checks whether property 'refreshExceptionDispatchInfo' has been set, and re-throws the exception in the case that it has.
        /// </summary>
        protected void CheckAndThrowRefreshException()
        {
            if (refreshExceptionDispatchInfo != null)
            {
                refreshExceptionDispatchInfo.Throw();
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the LoopingWorkerThreadReaderNodeRefreshStrategy.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591
        ~LoopingWorkerThreadReaderNodeRefreshStrategy()
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
                    if (workerThreadCompleteSignal != null)
                    {
                        workerThreadCompleteSignal.Dispose();
                    }
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
