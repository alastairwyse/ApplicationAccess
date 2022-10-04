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
    /// A reader node refresh strategy that refreshes/updates the reader node at a regular interval, using a worker thread.
    /// </summary>
    public class LoopingWorkerThreadReaderNodeRefreshStrategy : IReaderNodeRefreshStrategy
    {
        /// <summary>Worker thread which implements the strategy to flush/process the contents of the buffers.</summary>
        private Thread readerNodeRefreshWorkerThread;
        /// <summary>Set with any exception which occurrs on the worker thread when refreshing the reader node.  Null if no exception has occurred.</summary>
        private Exception refreshException;
        /// <summary>The time to wait (in milliseconds) between reader node refreshes.</summary>
        protected Int32 refreshLoopInterval;

        /// <inheritdoc/>
        public event EventHandler ReaderNodeRefreshed;

        /// <summary>
        /// Contains an exception which occurred on the worker thread during reader node refreshing.  Null if no exception has occurred.
        /// </summary>
        protected Exception RefreshException
        {
            get { return refreshException; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.LoopingWorkerThreadReaderNodeRefreshStrategy class.
        /// </summary>
        /// <param name="refreshLoopInterval">The time to wait (in milliseconds) between reader node refreshes.</param>
        public LoopingWorkerThreadReaderNodeRefreshStrategy(Int32 refreshLoopInterval)
        {
            if (refreshLoopInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(refreshLoopInterval), $"Parameter '{nameof(refreshLoopInterval)}' with value {refreshLoopInterval} cannot be less than 1.");
        }

        /// <inheritdoc/>
        public void NotifyQueryMethodCalled()
        {
            refreshException = null;
        }
    }
}
