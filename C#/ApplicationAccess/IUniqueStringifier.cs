/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods for converting objects of a specified type to and from strings which uniquely identify the object.
    /// </summary>
    /// <typeparam name="T">The type of objects to convert.</typeparam>
    public interface IUniqueStringifier<T>
    {
        /// <summary>
        /// Converts an object into a string which uniquely identifies that object.
        /// </summary>
        /// <param name="inputObject">The object to convert.</param>
        /// <returns>A string which uniquely identifies that object.</returns>
        String ToString(T inputObject);

        /// <summary>
        /// Converts a string which uniquely identifies an object into the object.
        /// </summary>
        /// <param name="stringifiedObject">The string representing the object.</param>
        /// <returns>The object.</returns>
        T FromString(String stringifiedObject);
    }
}
