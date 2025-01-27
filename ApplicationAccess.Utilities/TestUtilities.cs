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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Contains utility methods for unit tests.
    /// </summary>
    public class TestUtilities
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Utilities.TestUtilities class.
        /// </summary>
        public TestUtilities()
        {

        }

        /// <summary>
        /// Returns an <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/> which checks whether a collection of strings matches the collection in parameter <paramref name="expected"/> irrespective of their enumeration order.
        /// </summary>
        /// <param name="expected">The collection of strings the predicate compares to.</param>
        /// <returns>The <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/>.</returns>
        /// <remarks>Designed to be passed to the 'predicate' parameter of the <see cref="Arg.Any{T}"/> argument matcher.</remarks>
        public Expression<Predicate<IEnumerable<String>>> EqualIgnoringOrder(IEnumerable<String> expected)
        {
            return (IEnumerable<String> actual) => StringEnumerablesContainSameValues(expected, actual);
        }

        /// <summary>
        /// Checks whether two collections of strings contain the same elements irrespective of their enumeration order.
        /// </summary>
        /// <param name="enumerable1">The first collection.</param>
        /// <param name="enumerable2">The second collection.</param>
        /// <returns>True if the collections contain the same string.  False otherwise.</returns>
        protected Boolean StringEnumerablesContainSameValues(IEnumerable<String> enumerable1, IEnumerable<String> enumerable2)
        {
            if (enumerable1.Count() != enumerable2.Count())
            {
                return false;
            }
            var sortedExpected = new List<String>(enumerable1);
            var sortedActual = new List<String>(enumerable2);
            sortedExpected.Sort();
            sortedActual.Sort();
            for (Int32 i = 0; i < sortedExpected.Count; i++)
            {
                if (sortedExpected[i] != sortedExpected[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
