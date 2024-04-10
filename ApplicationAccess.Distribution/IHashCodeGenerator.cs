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
using System.Text;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods to generate evenly distributed hash codes for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to generate hash codes for.</typeparam>
    public interface IHashCodeGenerator<T>
    {
        /// <summary>
        /// Generates a hash code for the specified value.
        /// </summary>
        /// <param name="inputValue">The value to generate the hash code for.</param>
        /// <returns>The hash code.</returns>
        Int32 GetHashCode(T inputValue);
    }
}
