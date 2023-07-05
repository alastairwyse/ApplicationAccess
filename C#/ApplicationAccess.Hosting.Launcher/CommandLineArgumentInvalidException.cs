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

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Throw when a launcher command line argument name or value is invalid.
    /// </summary>
    /// <remarks>This can be caught at the top level of the launcher and the message displayed on the command line.  Allows distinction between errors occurring because of user input, and those occurring because of an internal/unexpected error in the launcher.</remarks>
    public class CommandLineArgumentInvalidException : Exception
    {
        /// <summary>
        /// The name of the argument which is invalid.
        /// </summary>
        public String ArgumentName
        {
            get;
            protected set;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Launcher.CommandLineArgumentInvalidException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="argumentName">The name of the argument which is invalid.</param>
        public CommandLineArgumentInvalidException(String message, String argumentName)
            : base(message)
        {
            this.ArgumentName = argumentName;
        }
    }
}
