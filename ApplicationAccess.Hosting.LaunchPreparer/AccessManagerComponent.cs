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
    /// Represents a component within an access manager deployment.
    /// </summary>
    public enum AccessManagerComponent
    {
        /// <summary>An event cache node.</summary>
        EventCacheNode,
        /// <summary>A reader node.</summary>
        ReaderNode,
        /// <summary>A reader/writer node.</summary>
        ReaderWriterNode,
        /// <summary>A writer node.</summary>
        WriterNode,
        /// <summary>A dependency-free reader/writer node.</summary>
        DependencyFreeReaderWriterNode,
        /// <summary>A distributed reader node.</summary>
        DistributedReaderNode,
        /// <summary>A distributed writer node.</summary>
        DistributedWriterNode,
        /// <summary>A distributed operation coordinator node.</summary>
        DistributedOperationCoordinatorNode
    }
}
