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

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// The exception that is thrown when an error occurs attempting to flush buffered events.
    /// </summary>
    /// <remarks>The flushing would usually be performed by a worker thread (e.g. in the case of classes deriving from WorkerThreadBufferFlushStrategyBase), and hence this exception would usually be thrown from the worker thread.  Additionally this exception would usually result from a critical inner exception (e.g. failure to write to a database), hence this exception will often need to be excplicity caught and acted upon in client code.</remarks>
    public class BufferFlushingException : Exception
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.BufferFlushingException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public BufferFlushingException(String message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.BufferFlushingException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public BufferFlushingException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
