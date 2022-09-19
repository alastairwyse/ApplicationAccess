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
    /// An implementation of <see cref="IUniqueStringifier{T}"/> for enums.
    /// </summary>
    /// <typeparam name="T">The type of enum to convert.</typeparam>
    public class EnumUniqueStringifier<T> : IUniqueStringifier<T> where T : struct, Enum
    {
        /// <summary>
        /// Converts an enum value into a string.
        /// </summary>
        /// <param name="inputObject">The enum value to convert.</param>
        /// <returns>The enum as a string.</returns>
        public string ToString(T inputObject)
        {
            return inputObject.ToString();
        }

        /// <summary>
        /// Converts a string into an enum value.
        /// </summary>
        /// <param name="stringifiedObject">The string representing the enum value.</param>
        /// <returns>The enum value.</returns>
        public T FromString(string stringifiedObject)
        {
            T returnValue;
            try
            {
                returnValue = (T)Enum.Parse(typeof(T), stringifiedObject);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to convert string '{stringifiedObject}' into an enum of type '{typeof(T).FullName}'.", nameof(stringifiedObject), e);
            }

            return returnValue;
        }
    }
}
