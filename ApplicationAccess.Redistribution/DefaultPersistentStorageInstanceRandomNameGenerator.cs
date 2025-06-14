/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Text;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Default implementation of <see cref="IPersistentStorageInstanceRandomNameGenerator"/> which generates names using only lower case alphabetic characters.
    /// </summary>
    public class DefaultPersistentStorageInstanceRandomNameGenerator : IPersistentStorageInstanceRandomNameGenerator
    {
        /// <summary>The lengths of names to generate.</summary>
        protected Int32 nameLength;
        /// <summary>Random number generator.</summary>
        protected Random randomGenerator;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.DefaultPersistentStorageInstanceRandomNameGenerator class.
        /// </summary>
        /// <param name="nameLength">The lengths of names to generate.</param>
        public DefaultPersistentStorageInstanceRandomNameGenerator(Int32 nameLength)
        {
            if (nameLength < 8 || nameLength > 128)
                throw new ArgumentOutOfRangeException(nameof(nameLength), $"Parameter '{nameof(nameLength)}' with value {nameLength} must be between 8 and 128 (inclusive).");

            this.nameLength = nameLength;
            randomGenerator = new Random();
        }

        /// <inheritdoc/>
        public String Generate()
        {
            var stringBuilder = new StringBuilder();
            for (Int32 i = 0; i < nameLength; i++)
            {
                Int32 asciiNumber = randomGenerator.Next(26);
                var asciiChar = ((Char)(asciiNumber + 97));
                stringBuilder.Append(asciiChar);
            }

            return stringBuilder.ToString();
        }
    }
}
