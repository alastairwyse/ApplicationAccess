﻿/*
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

namespace ApplicationAccess.Hosting.Rest.IntegrationTests
{
    /// <summary>
    /// Exception that is configured to trip a trip switch.
    /// </summary>
    public class TripSwitchException : Exception
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.IntegrationTests.TripSwitchException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TripSwitchException(String message)
            : base(message)
        {
        }
    }
}
