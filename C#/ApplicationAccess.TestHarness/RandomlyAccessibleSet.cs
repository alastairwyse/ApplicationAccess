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

using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// A set which allows items to be returned randomly.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the set.</typeparam>
    public class RandomlyAccessibleSet<T>
    {
        OrderedDictionary underlyingDictionary;
        Random randomGenerator;

        public Int32 Count
        {
            get { return underlyingDictionary.Count; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.RandomlyAccessibleSet class.
        /// </summary>
        public RandomlyAccessibleSet()
        {
            underlyingDictionary = new OrderedDictionary();
            randomGenerator = new Random();
        }

        public void Add(T item)
        {
            underlyingDictionary.Add(item, item);
        }

        public bool Contains(T item)
        {
            return underlyingDictionary.Contains(item);
        }

        public void Remove(T item)
        {
            underlyingDictionary.Remove(item);
        }

        public T GetRandomItem()
        {
            if (underlyingDictionary.Count == 0)
                throw new InvalidOperationException("The set is empty.");
            return (T)underlyingDictionary[randomGenerator.Next(underlyingDictionary.Count)];
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (DictionaryEntry currentItem in underlyingDictionary)
            {
                yield return (T)currentItem.Key;
            }
        }
    }
}
