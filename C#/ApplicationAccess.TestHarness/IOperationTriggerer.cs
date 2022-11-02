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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Defines methods which can be waited on to denote when the next operation should be performed against the AccessManager under test.
    /// </summary>
    public interface IOperationTriggerer : IDisposable
    {

        /// <summary>
        /// Starts the worker thread which performs triggering.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the worker thread which performs triggering.
        /// </summary>
        void Stop();

        /// <summary>
        /// Returns when the next operation should be performed.
        /// </summary>
        void WaitForNextTrigger();

        /// <summary>
        /// Notifies that the operation most recently triggered is being initiated.
        /// </summary>
        void NotifyOperationInitiated();
    }
}
