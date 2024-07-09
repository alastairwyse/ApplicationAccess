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

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// The action to take if a critical/non-recoverable error occurs whilst attempting to process the buffer(s) for metrics.
    /// </summary>
    public enum MetricBufferProcessingFailureAction
    {
        /// <summary>Disable metric logging in the hosting component.</summary>
        DisableMetricLogging,
        /// <summary>Return a 'service unavailable' error on subsequent requests to the hosting component.</summary>
        ReturnServiceUnavailable
    }
}
