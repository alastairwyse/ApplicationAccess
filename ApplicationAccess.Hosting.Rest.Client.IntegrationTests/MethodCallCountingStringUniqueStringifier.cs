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

namespace ApplicationAccess.Hosting.Rest.Client.IntegrationTests
{
    /// <summary>
    /// Implementation of <see cref="IUniqueStringifier{T}"/> which counts the number of calls to the FromString() and ToString() methods.
    /// </summary>
    public class MethodCallCountingStringUniqueStringifier : IUniqueStringifier<String>
    {
        public Int32 FromStringCallCount { get; protected set; }
        public Int32 ToStringCallCount { get; protected set; }

        public MethodCallCountingStringUniqueStringifier()
        {
            Reset();
        }

        /// <inheritdoc/>
        public String FromString(String stringifiedObject)
        {
            FromStringCallCount++;

            return stringifiedObject;
        }

        /// <inheritdoc/>
        public String ToString(String inputObject)
        {
            ToStringCallCount++;

            return inputObject;
        }

        /// <summary>
        /// Resets the method call counts to 0.
        /// </summary>
        public void Reset()
        {
            FromStringCallCount = 0;
            ToStringCallCount = 0;
        }
    }
}
