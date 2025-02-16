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

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// A counter which can be incremented and decremented safely by multiple threads.
    /// </summary>
    /// <remarks>This class is basically a wrapper around the <see cref="Interlocked"/> class.</remarks>
    public class ThreadSafeCounter
    {
        /// <summary>The value of the counter.</summary>
        protected Int32 counterValue;

        /// <summary>
        /// The value of the counter.
        /// </summary>
        public Int32 CounterValue
        {
            get { return counterValue; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Utilities.ThreadSafeCounter class.
        /// </summary>
        public ThreadSafeCounter()
        {
            counterValue = 0;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Utilities.ThreadSafeCounter class.
        /// </summary>
        /// <param name="initialValue">The initial value of the counter.</param>
        public ThreadSafeCounter(Int32 initialValue)
        {
            counterValue = initialValue;
        }

        /// <summary>
        /// Increments the counter;
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref counterValue);
        }

        /// <summary>
        /// Decrements the counter;
        /// </summary>
        public void Decrement()
        {
            Interlocked.Decrement(ref counterValue);
        }
    }
}
