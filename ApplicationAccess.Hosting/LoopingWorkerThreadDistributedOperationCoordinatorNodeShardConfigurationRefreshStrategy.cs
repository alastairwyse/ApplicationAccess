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
using ApplicationAccess.Persistence;

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
        /// <summary>An action to invoke if an error occurs when refreshing the shard configuration.  Accepts a single parameter which is the <see cref="ShardConfigurationRefreshException"/> containing details of the error.</summary>
        protected Action<ShardConfigurationRefreshException> refreshExceptionAction;
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
        /// <param name="refreshExceptionAction">An action to invoke if an error occurs when refreshing the shard configuration.  Accepts a single parameter which is the <see cref="ShardConfigurationRefreshException"/> containing details of the error.</param>
        public LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy(Int32 refreshLoopInterval, Action<ShardConfigurationRefreshException> refreshExceptionAction)
        {
            if (refreshLoopInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(refreshLoopInterval), $"Parameter '{nameof(refreshLoopInterval)}' with value {refreshLoopInterval} cannot be less than 1.");

            this.refreshLoopInterval = refreshLoopInterval;
            this.refreshExceptionAction = refreshExceptionAction;
            stopMethodCalled = false;
        }

        /// <summary>
        /// Starts the worker thread which performs shard configuration refreshes.
        /// </summary>
        public void Start()
        {
            shardConfigurationRefreshWorkerThread = new Thread(() =>
            {
                try
                {
                    while (stopMethodCalled == false)
                    {
                        Thread.Sleep(refreshLoopInterval);
                        OnShardConfigurationRefreshed(EventArgs.Empty);
                    }
                }
                catch (Exception e)
                {
                    var wrappedException = new ShardConfigurationRefreshException($"Exception occurred on shard configuration refreshing worker thread at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                    refreshExceptionAction.Invoke(wrappedException);
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
            stopMethodCalled = true;
            // Wait for the worker thread to finish
            JoinWorkerThread();
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
        #endregion
    }
}
