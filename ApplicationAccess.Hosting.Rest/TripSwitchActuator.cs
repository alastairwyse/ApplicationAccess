/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// A component which actuates/activates/trips a <see cref="TripSwitchMiddlewareBase"/> instance.
    /// </summary>
    public class TripSwitchActuator : IDisposable
    {
        /// <summary>The <see cref="CancellationTokenSource"/> used to implment the actuator.</summary>
        protected CancellationTokenSource cancellationTokenSource;        
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchActuator class.
        /// </summary>
        public TripSwitchActuator()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Whether the trip switched has been actuated/tripped.
        /// </summary>
        public Boolean IsActuated
        {
            get
            {
                return cancellationTokenSource.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Actuates/activates/trips the trip switch.
        /// </summary>
        public void Actuate()
        {
            cancellationTokenSource.Cancel();
        }

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the TripSwitchActuator.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~TripSwitchActuator()
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
                    cancellationTokenSource.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
