/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Implementation of <see cref="IAvailableDataElementCounter"/> for data elements which are enums.
    /// </summary>
    /// <typeparam name="TDataElement">The type of the enum data element.</typeparam>
    public class EnumAvailableDataElementCounter<TDataElement> : IAvailableDataElementCounter<TDataElement> where TDataElement : Enum
    {
        /// <inheritdoc/>
        public int GetAvailableElements()
        {
            return Enum.GetNames(typeof(TDataElement)).Length;
        }
    }
}
