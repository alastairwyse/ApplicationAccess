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
using System.Collections.Generic;

namespace ApplicationAccess.Hosting.LaunchPreparer
{
    /// <summary>
    /// Maps an <see cref="AccessManagerComponent"/> to the dll file use to execute that component.
    /// </summary>
    public static class ComponentToDllNameMap
    {
        /// <summary>Maps an access manager component to the name of its executable.</summary>
        private static Dictionary<AccessManagerComponent, String> underlyingMap = new Dictionary<AccessManagerComponent, String>()
        {
            { AccessManagerComponent.EventCacheNode, "ApplicationAccess.Hosting.Rest.EventCache.dll" },
            { AccessManagerComponent.ReaderNode, "ApplicationAccess.Hosting.Rest.Reader.dll" },
            { AccessManagerComponent.ReaderWriterNode, "ApplicationAccess.Hosting.Rest.ReaderWriter.dll" },
            { AccessManagerComponent.WriterNode, "ApplicationAccess.Hosting.Rest.Writer.dll" },
            { AccessManagerComponent.DependencyFreeReaderWriterNode, "ApplicationAccess.Hosting.Rest.DependencyFreeReaderWriter.dll" },
            { AccessManagerComponent.DistributedReader, "ApplicationAccess.Hosting.Rest.DistributedReader.dll" },
            { AccessManagerComponent.DistributedWriter, "ApplicationAccess.Hosting.Rest.DistributedWriter.dll" },
        };

        /// <summary>
        /// Returns the dll file use to execute the specified component.
        /// </summary>
        /// <param name="component">The component to retrieve the dll name for.</param>
        /// <returns>The dll file use to execute the component.</returns>
        public static String GetDllNameForComponent(AccessManagerComponent component)
        {
            return underlyingMap[component];
        }
    }
}
