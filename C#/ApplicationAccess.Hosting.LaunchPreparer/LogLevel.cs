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

namespace ApplicationAccess.Hosting.LaunchPreparer
{
    /// <summary>
    /// The level of logging to use in the launched component.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>General information logs including details of each HTTP request received.</summary>
        Information, 
        /// <summary>Unexpected/anomalous events, e.g. non-fatal exceptions.</summary>
        Warning,
        /// <summary>Unexpected/anomalous events which impact the continuing operation of the application.</summary>
        Critical
    }
}
