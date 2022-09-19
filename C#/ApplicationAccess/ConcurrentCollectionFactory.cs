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
using System.Collections.Concurrent;
using System.Text;

namespace ApplicationAccess
{
    /// <summary>
    /// Returns instances of concurrent collections in the System.Collections.Concurrent namespace.
    /// </summary>
    public class ConcurrentCollectionFactory : ICollectionFactory
    {
        /// <summary>
        /// Returns a <see cref="ConcurrentDictionary{TKey, TValue}"/> instance.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <returns>The <see cref="ConcurrentDictionary{TKey, TValue}"/> instance.</returns>
        public IDictionary<TKey, TValue> GetDictionaryInstance<TKey, TValue>()
        {
            return new ConcurrentDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Returns a <see cref="ConcurrentHashSet{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of elements in the set.</typeparam>
        /// <returns>The <see cref="ConcurrentHashSet{T}"/> instance</returns>
        public ISet<T> GetSetInstance<T>()
        {
            return new ConcurrentHashSet<T>();
        }
    }
}
