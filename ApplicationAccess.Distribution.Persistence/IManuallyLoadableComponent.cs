/*
 * Copyright 2026 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Distribution.Persistence
{
    /// <summary>
    /// Defines methods for components which allow their data to be loaded manually from persistent storage.
    /// </summary>
    public interface IManuallyLoadableComponent
    {
        /// <summary>
        /// Loads all data from persistent storage.
        /// </summary>
        /// <param name="throwExceptionIfStorageIsEmpty">When set true an exception will be thrown in the case that the persistent storage is empty.</param>
        void Load(Boolean throwExceptionIfStorageIsEmpty);
    }
}
