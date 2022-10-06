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

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A reader node refresh strategy that refreshes/updates a reader node at a regular interval, using a worker thread.
    /// </summary>
    public class LoopingWorkerThreadReaderNodeRefreshStrategy : IReaderNodeRefreshStrategy
    {
        /// <summary>Worker thread which implements the strategy to refresh the contents of reader node.</summary>
        protected Thread readerNodeRefreshWorkerThread;
        /// <summary>Set with any exception which occurrs on the worker thread when refreshing the reader node.  Null if no exception has occurred.</summary>
        protected Exception refreshException;
        /// <summary>The time to wait (in milliseconds) between reader node refreshes.</summary>
        protected Int32 refreshLoopInterval;

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

            refreshException = null;
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
                Thread.Sleep(refreshLoopInterval);
                try
                {
                    OnReaderNodeRefreshed(EventArgs.Empty);
                }
                catch (Exception e)
                {
                    var wrappedException = new ReaderNodeRefreshException($"Exception occurred on reader node refreshing worker thread at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                    Interlocked.Exchange(ref refreshException, wrappedException);
                }
            });
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
            // Wait for the worker thread to finish
            JoinWorkerThread();
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
        /// Checks whether property 'refreshException' has been set, and re-throws the exception in the case that it has.
        /// </summary>
        protected void CheckAndThrowRefreshException()
        {
            if (refreshException != null)
            {
                throw refreshException;
            }
        }

        #endregion
    }
}
