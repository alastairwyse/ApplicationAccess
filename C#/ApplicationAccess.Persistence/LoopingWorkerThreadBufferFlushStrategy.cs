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
using System.Text;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// A buffer flush strategy that flushes/processes the buffered events at a regular interval.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class LoopingWorkerThreadBufferFlushStrategy<TUser, TGroup, TComponent, TAccess> : WorkerThreadBufferFlushStrategyBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</summary>
        protected Int32 flushLoopInterval;
        /// <summary>The number of iterations of the worker thread to flush/process.</summary>
        protected Int32 loopIterationCount;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
        /// </summary>
        /// <param name="flushLoopInterval">The time to wait (in milliseconds) between iterations of the worker thread which flushes/processes buffered events.</param>
        public LoopingWorkerThreadBufferFlushStrategy(Int32 flushLoopInterval)
            : base()
        {
            if (flushLoopInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(flushLoopInterval), $"Parameter '{nameof(flushLoopInterval)}' with value {flushLoopInterval} cannot be less than 1.");

            this.flushLoopInterval = flushLoopInterval;
            workerThreadCompleteSignal = null;
        }
    }
}
