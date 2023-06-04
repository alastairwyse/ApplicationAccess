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
using System.Collections.Generic;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// A dictionary which allows <see cref="KeyValuePair{TKey, TValue}">KeyValuePairs</see> to be returned randomly.
    /// </summary>
    /// <typeparam name="TKey">The type of keys stored in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values stored in the dictionary.</typeparam>
    public class RandomlyAccessibleDictionary<TKey, TValue>
    {
        Dictionary<TKey, TValue> underlyingDictionary;
        Dictionary<TKey, Int32> listIndexMap;
        List<TKey> keyList;
        Random randomGenerator;

        public Int32 Count
        {
            get { return underlyingDictionary.Count; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.RandomlyAccessibleDictionary class.
        /// </summary>
        public RandomlyAccessibleDictionary()
        {
            underlyingDictionary = new Dictionary<TKey, TValue>();
            listIndexMap = new Dictionary<TKey, Int32>();
            keyList = new List<TKey>();
            randomGenerator = new Random();
        }

        public void Add(TKey item, TValue value)
        {
            underlyingDictionary.Add(item, value);
            keyList.Add(item);
            listIndexMap.Add(item, keyList.Count - 1);
        }

        public bool ContainsKey(TKey item)
        {
            return underlyingDictionary.ContainsKey(item);
        }

        public void Remove(TKey item)
        {
            underlyingDictionary.Remove(item);
            // Copy the last element over the element to remove and remove the last
            keyList[listIndexMap[item]] = keyList[keyList.Count - 1];
            listIndexMap[keyList[keyList.Count - 1]] = listIndexMap[item];
            keyList.RemoveAt(keyList.Count - 1);
            listIndexMap.Remove(item);
        }

        public KeyValuePair<TKey, TValue> GetRandomPair()
        {
            if (underlyingDictionary.Count == 0)
                throw new InvalidOperationException("The dictionary is empty.");

            Int32 elementIndex = randomGenerator.Next(underlyingDictionary.Count);
            TKey key = keyList[elementIndex];
            TValue value = underlyingDictionary[key];

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        public TValue this[TKey i]
        {
            get 
            { 
                return underlyingDictionary[i]; 
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return underlyingDictionary.GetEnumerator();
        }
    }
}
