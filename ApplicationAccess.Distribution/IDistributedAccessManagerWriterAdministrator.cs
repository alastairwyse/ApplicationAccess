﻿/*
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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods to perform administrative functions on a writer component in an AccessManager implementation where responsibility for subsets of AccessManager elements is distributed across multiple computers.
    /// </summary>
    public interface IDistributedAccessManagerWriterAdministrator
    {
        /// <summary>
        /// Flushes the event buffer(s) managed by the writer.
        /// </summary>
        void FlushEventBuffers();

        /// <summary>
        /// Returns the number of events currently being processed.
        /// </summary>
        /// <returns>The number of events currently being processed.</returns>
        Int32 GetEventProcessingCount();
    }
}
