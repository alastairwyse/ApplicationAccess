/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A distributed operation coordinator node shard configuration refresh strategy that refreshes/updates the shard configuration at a regular interval, using a worker thread.
    /// </summary>
    public class LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy : IDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy
    {
        // TODO: A lot of this class is copied verbatim from LoopingWorkerThreadReaderNodeRefreshStrategy.  Could move commonailty into an abstract base class.

        /// <summary>Worker thread which implements the strategy to refresh the shard configuration.</summary>
        protected Thread shardConfigurationRefreshWorkerThread;
        /// <summary>Set with any exception which occurrs on the worker thread when refreshing the shard configuration (including stack trace and context info provided by the <see cref="ExceptionDispatchInfo"/> class).  Null if no exception has occurred.</summary>
        protected ExceptionDispatchInfo refreshExceptionDispatchInfo;
        /// <summary>The time to wait (in milliseconds) between shard configuration refreshes.</summary>
        protected Int32 refreshLoopInterval;
        /// <summary>Whether request to stop the worker thread has been received via the Stop() method.</summary>
        protected volatile Boolean stopMethodCalled;

        /// <inheritdoc/>
        public event EventHandler ShardConfigurationRefreshed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy class.
        /// </summary>
        /// <param name="refreshLoopInterval">The time to wait (in milliseconds) between shard configuration refreshes.</param>
        public LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy(Int32 refreshLoopInterval)
        {
            if (refreshLoopInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(refreshLoopInterval), $"Parameter '{nameof(refreshLoopInterval)}' with value {refreshLoopInterval} cannot be less than 1.");

            this.refreshLoopInterval = refreshLoopInterval;
            refreshExceptionDispatchInfo = null;
            stopMethodCalled = false;
        }

        /// <inheritdoc/>
        public void NotifyOperationProcessed()
        {
            CheckAndThrowRefreshException();
        }

        /// <summary>
        /// Starts the worker thread which performs shard configuration refreshes.
        /// </summary>
        public void Start()
        {
            shardConfigurationRefreshWorkerThread = new Thread(() =>
            {
                while (stopMethodCalled == false)
                {
                    Thread.Sleep(refreshLoopInterval);
                    try
                    {
                        OnShardConfigurationRefreshed(EventArgs.Empty);
                    }
                    catch (Exception e)
                    {
                        var wrappedException = new Exception($"Exception occurred on shard configuration refreshing worker thread at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                        Interlocked.Exchange(ref refreshExceptionDispatchInfo, ExceptionDispatchInfo.Capture(wrappedException));
                    }
                }
            });
            stopMethodCalled = false;
            shardConfigurationRefreshWorkerThread.Name = $"{this.GetType().FullName} shard configuration refreshing worker thread.";
            shardConfigurationRefreshWorkerThread.IsBackground = true;
            shardConfigurationRefreshWorkerThread.Start();
        }

        /// <summary>
        /// Stops the worker thread which performs shard configuration refreshes.
        /// </summary>
        /// <exception cref="ShardConfigurationRefreshException">An exception occurred whilst attempting to refresh/update the shard configuration.</exception>
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
        /// Raises the ShardConfigurationRefreshed event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected virtual void OnShardConfigurationRefreshed(EventArgs e)
        {
            if (ShardConfigurationRefreshed != null)
            {
                ShardConfigurationRefreshed(this, e);
            }
        }

        /// <summary>
        /// Calls Join() on the worker thread, waiting until it terminates.
        /// </summary>
        protected void JoinWorkerThread()
        {
            if (shardConfigurationRefreshWorkerThread != null)
            {
                shardConfigurationRefreshWorkerThread.Join();
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
    }
}
