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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Base for a class that partipates in multithreaded 2-way signalling along with another <see cref="TwoWaySignallingParticipantBase"/> subclass.
    /// </summary>
    public abstract class TwoWaySignallingParticipantBase : IDisposable
    {
        /// <summary>The other <see cref="TwoWaySignallingParticipantBase"/> subclass which participates in the 2-way signalling.</summary>
        protected TwoWaySignallingParticipantBase counterpart;
        /// <summary>Whether request to stop the 2-way signalling has been received via the Stop() method.</summary>
        protected volatile Boolean stopMethodCalled;
        /// <summary>Signal which is set when an iteration of the worker thread is completed.</summary>
        protected AutoResetEvent workerThreadIterationCompletedSignal;
        /// <summary>Action which defines a single iteration of the worker thread's processing.</summary>
        protected Action workerThreadIterationAction;
        /// <summary></summary>
        protected Thread workerThread;
        /// <summary>The name to assign to the worker thread.</summary>
        protected String workerThreadName;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>The other <see cref="TwoWaySignallingParticipantBase"/> subclass which participates in the 2-way signalling.</summary>
        public TwoWaySignallingParticipantBase Counterpart
        {
            set
            {
                if (this == value)
                    throw new ArgumentException($"'{nameof(Counterpart)}' property cannot be set with itself.", nameof(Counterpart));

                counterpart = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.TwoWaySignallingParticipantBase class.
        /// </summary>
        public TwoWaySignallingParticipantBase()
        {
            counterpart = null;
            stopMethodCalled = false;
            workerThreadIterationCompletedSignal = new AutoResetEvent(false);
            disposed = false;
        }

        /// <summary>
        /// Starts the 2-way signalling process.
        /// </summary>
        public virtual void Start()
        {
            if (counterpart == null)
                throw new InvalidOperationException($"Cannot call the Start() method before the '{nameof(Counterpart)}' property has been set.");
            if (workerThreadIterationAction == null)
                throw new InvalidOperationException($"Cannot call the Start() method before the '{nameof(workerThreadIterationAction)}' property has been set.");

            Action workerThreadRoutine = () =>
            {
                while (stopMethodCalled == false)
                {
                    workerThreadIterationAction.Invoke();
                    counterpart.SetSignal();
                    workerThreadIterationCompletedSignal.WaitOne();
                }
            };
            workerThread = new Thread(new ThreadStart(workerThreadRoutine));
            workerThread.Name = workerThreadName;
            workerThread.IsBackground = true;
            Console.WriteLine($"Starting {workerThreadName}");
            workerThread.Start();
        }

        /// <summary>
        /// Stops the 2-way signalling process.
        /// </summary>
        public virtual void Stop()
        {
            // Indicate that the Stop() method has been called so that the worker thread will stop after it's current iteration
            stopMethodCalled = true;
            // Set the worker thread signal so that it drops out of the iteration loop
            workerThreadIterationCompletedSignal.Set();
            // Wait for the worker thread to finish
            Console.WriteLine($"Joining {workerThreadName}");
            workerThread.Join();
            counterpart = null;
            Console.WriteLine($"{workerThreadName} stopped");
        }

        /// <summary>
        /// Sets this participant's signal.
        /// </summary>
        public virtual void SetSignal()
        {
            workerThreadIterationCompletedSignal.Set();
        }

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the TwoWaySignallingParticipantBase.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~TwoWaySignallingParticipantBase()
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
                    workerThreadIterationCompletedSignal.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
