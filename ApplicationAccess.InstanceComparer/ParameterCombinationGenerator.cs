/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.InstanceComparer
{
    /// <summary>
    /// Generates all possible combinations of method parameters.
    /// </summary>
    class ParameterCombinationGenerator
    {
        /// <summary>
        /// Generates all possible combinations of parameters for a method which accepts 2 strings.
        /// </summary>
        /// <param name="firstParameterValues">All possible values for the first parameter.</param>
        /// <param name="secondParameterValues">All possible values for the second parameter.</param>
        /// <returns>The parameter combinations.</returns>
        public IEnumerable<Tuple<String, String>> Generate(IEnumerable<String> firstParameterValues, IEnumerable<String> secondParameterValues)
        {
            foreach (String currentFirstParameterValue in firstParameterValues)
            {
                foreach (String currentSecondParameterValue in secondParameterValues)
                {
                    yield return Tuple.Create(currentFirstParameterValue, currentSecondParameterValue);
                }
            }
        }

        /// <summary>
        /// Generates all possible combinations of parameters for a method which accepts 3 strings.
        /// </summary>
        /// <param name="firstParameterValues">All possible values for the first parameter.</param>
        /// <param name="secondParameterValues">All possible values for the second parameter.</param>
        /// <param name="thirdParameterValues">All possible values for the third parameter.</param>
        /// <returns>The parameter combinations.</returns>
        public IEnumerable<Tuple<String, String, String>> Generate(IEnumerable<String> firstParameterValues, IEnumerable<String> secondParameterValues, IEnumerable<String> thirdParameterValues)
        {
            foreach (String currentFirstParameterValue in firstParameterValues)
            {
                foreach (String currentSecondParameterValue in secondParameterValues)
                {
                    foreach (String currentThirdParameterValue in thirdParameterValues)
                    {
                        yield return Tuple.Create(currentFirstParameterValue, currentSecondParameterValue, currentThirdParameterValue);
                    }
                }
            }
        }

        /// <summary>
        /// Generates all possible combinations of parameters for a method which accepts a string and a boolean.
        /// </summary>
        /// <param name="firstParameterValues">All possible values for the first parameter.</param>
        /// <returns>The parameter combinations.</returns>
        public IEnumerable<Tuple<String, Boolean>> GenerateWithBoolean(IEnumerable<String> firstParameterValues)
        {
            foreach (String currentFirstParameterValue in firstParameterValues)
            {
                yield return Tuple.Create(currentFirstParameterValue, true);
                yield return Tuple.Create(currentFirstParameterValue, false);
            }
        }

        /// <summary>
        /// Generates all possible combinations of parameters for a method which accepts two strings and a boolean.
        /// </summary>
        /// <param name="firstParameterValues">All possible values for the first parameter.</param>
        /// <param name="secondParameterValues">All possible values for the second parameter.</param>
        /// <returns>The parameter combinations.</returns>
        public IEnumerable<Tuple<String, String, Boolean>> GenerateWithBoolean(IEnumerable<String> firstParameterValues, IEnumerable<String> secondParameterValues)
        {
            foreach (String currentFirstParameterValue in firstParameterValues)
            {
                foreach (String currentSecondParameterValues in secondParameterValues)
                {
                    yield return Tuple.Create(currentFirstParameterValue, currentSecondParameterValues, true);
                    yield return Tuple.Create(currentFirstParameterValue, currentSecondParameterValues, false);
                }
            }
        }

        /// <summary>
        /// Generates all possible combinations of parameters for a method which accepts 3 strings.
        /// </summary>
        /// <param name="firstParameterValues">All possible values for the first parameter.</param>
        /// <param name="secondAndThirdParameterValues">A <see cref="Dictionary{TKey, TValue}"/> containing the possible values for the second and third parameters (key contains second parameters, values contain third parameters).</param>
        /// <returns>The parameter combinations.</returns>
        public IEnumerable<Tuple<String, String, String>> Generate(IEnumerable<String> firstParameterValues, Dictionary<String, HashSet<String>> secondAndThirdParameterValues)
        {
            foreach (String currentFirstParameterValue in firstParameterValues)
            {
                foreach (String currentSecondParameterValue in secondAndThirdParameterValues.Keys)
                {
                    foreach (String currentThirdParameterValue in secondAndThirdParameterValues[currentSecondParameterValue])
                    {
                        yield return Tuple.Create(currentFirstParameterValue, currentSecondParameterValue, currentThirdParameterValue);
                    }
                }
            }
        }

        /// <summary>
        /// Generates all possible combinations of parameters for a method which accepts two strings and a boolean.
        /// </summary>
        /// <param name="firstAndSecondParameterValues">A <see cref="Dictionary{TKey, TValue}"/> containing the possible values for the first and second parameters (key contains first parameters, values contain second parameters).</param>
        /// <returns>The parameter combinations.</returns>
        public IEnumerable<Tuple<String, String, Boolean>> GenerateWithBoolean(Dictionary<String, HashSet<String>> firstAndSecondParameterValues)
        {
            foreach (String currentFirstParameterValue in firstAndSecondParameterValues.Keys)
            {
                foreach (String currentSecondParameterValue in firstAndSecondParameterValues[currentFirstParameterValue])
                {
                    yield return Tuple.Create(currentFirstParameterValue, currentSecondParameterValue, true);
                    yield return Tuple.Create(currentFirstParameterValue, currentSecondParameterValue, false);
                }
            }
        }
    }
}
