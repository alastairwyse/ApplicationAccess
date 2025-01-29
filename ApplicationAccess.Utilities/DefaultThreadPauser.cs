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
    /// Default implementation of <see cref="IThreadPauser"/> which wraps a <see cref="ManualResetEvent"/>.
    /// </summary>
    public class DefaultThreadPauser : IThreadPauser, IDisposable
    {
        /// <summary>The underlying <see cref="ManualResetEvent"/> which implements the pausing.</summary>
        protected ManualResetEvent resetEvent;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Utilities.DefaultThreadPauser class.
        /// </summary>
        public DefaultThreadPauser()
        {
            resetEvent = new ManualResetEvent(true);
            disposed = false;
        }

        /// <inheritdoc/>
        public void TestPaused()
        {
            resetEvent.WaitOne();
        }

        /// <inheritdoc/>
        public void Pause()
        {
            resetEvent.Reset();
        }

        /// <inheritdoc/>
        public void Resume()
        {
            resetEvent.Set();
        }

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the DefaultThreadPauser.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~DefaultThreadPauser()
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
                    resetEvent.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
