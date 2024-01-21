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

namespace ApplicationAccess
{
    /// <summary>
    /// The exception that is thrown when a group specified in an parameter/argument does not exist.
    /// </summary>
    /// <typeparam name="T">The type of the group.</typeparam>
    public class GroupNotFoundException<T> : ArgumentException
    {
        /// <summary>The group which does not exist.</summary>
        protected T group;

        /// <summary>
        /// The group which does not exist.
        /// </summary>
        public T Group
        {
            get
            {
                return group;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.GroupNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="parameterName">The name of the parameter containing the invalid group.</param>
        /// <param name="group">The group which does not exist.</param>
        public GroupNotFoundException(String message, String parameterName, T group)
            : base(message, parameterName)
        {
            this.group = group;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.GroupNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="parameterName">The name of the parameter containing the invalid group.</param>
        /// <param name="group">The group which does not exist.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public GroupNotFoundException(String message, String parameterName, T group, Exception innerException)
            : base(message, parameterName, innerException)
        {
            this.group = group;
        }
    }
}
