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
    /// An implementation of IUniqueStringifier&lt;T&gt; for strings.
    /// </summary>
    /// <remarks>Methods simply return the respective string parameters immediately.</remarks>
    public class StringUniqueStringifier : IUniqueStringifier<String>
    {
        #pragma warning disable 1591

        public string ToString(string inputObject)
        {
            return inputObject;
        }

        public string FromString(string stringifiedObject)
        {
            return stringifiedObject;
        }

        #pragma warning restore 1591
    }
}
