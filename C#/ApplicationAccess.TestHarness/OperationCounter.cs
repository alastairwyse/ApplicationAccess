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
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Counts the number of test operations executed and calls Cancel() on an <see cref="CancellationTokenSource"/> instance if a specified number of operations is reached.
    /// </summary>
    public class OperationCounter
    {
        /// <summary>The number of operations executed.</summary>
        protected Int64 operationCount;
        /// <summary>The maximum number of operations to execute,</summary>
        protected Int64 operationLimit;
        /// <summary>The signal to set when the operation limit is reached.</summary>
        protected EventWaitHandle stopSignal;
        /// <summary>Wehether or not the stop signal has been set.</summary>
        protected Boolean stopped;
        /// <summary>Lock object for calls to the Increment() method.</summary>
        protected Object incrementLock;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.OperationCounter class.
        /// </summary>
        /// <param name="operationLimit">The maximum number of operations to generate (set to 0 for no limit).</param>
        /// <param name="stopSignal">The signal to set when the operation limit is reached.</param>
        public OperationCounter(Int64 operationLimit, EventWaitHandle stopSignal)
        {
            if (operationLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(operationLimit), $"Parameter '{nameof(operationLimit)}' with value {operationLimit} must be greater than or equal to 0.");

            operationCount = 0;
            this.operationLimit = operationLimit;
            this.stopSignal = stopSignal;
            stopped = false;
            incrementLock = new Object();
        }

        public void Increment()
        {
            lock (incrementLock)
            {
                operationCount++;
                if (stopped == false && operationLimit > 0 && operationCount >= operationLimit)
                {
                    Console.WriteLine($"Cancelling testing as operation limit of {operationLimit} has been reached.");
                    stopped = true;
                    stopSignal.Set();
                }
            }
        }
    }
}
