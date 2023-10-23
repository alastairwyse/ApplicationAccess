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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Types of high level operations that can be performed by an AccessManager implementation.
    /// </summary>
    /// <remarks>Naming of operations is consistent with naming in the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> inheritance hierarchy.</remarks>
    public enum Operation
    {
        /// <summary>A read operation.</summary>
        Query, 
        /// <summary>A write operation.</summary>
        Event
    }
}
