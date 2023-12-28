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
    /// Default implementation of <see cref="IHashCodeGenerator{T}"/> for strings.
    /// </summary>
    public class DefaultStringHashCodeGenerator : IHashCodeGenerator<String>
    {
        /// <inheritdoc/>
        public Int32 GetHashCode(String inputValue)
        {
            // Based on these two articles/posts...
            //   https://stackoverflow.com/a/36845864/6375486
            //   https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/#a-deterministic-gethashcode-implementation

            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < inputValue.Length && inputValue[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ inputValue[i];
                    if (i == inputValue.Length - 1 || inputValue[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ inputValue[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
