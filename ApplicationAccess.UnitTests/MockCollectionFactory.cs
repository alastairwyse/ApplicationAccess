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

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// An implementation of ICollectionFactory which returns mocks of IDictionary and ISet.
    /// </summary>
    /// <typeparam name="TSet">The type of elements in ISet instances.</typeparam>
    /// <typeparam name="TDictKey">The type of the keys in IDictionary instances.</typeparam>
    /// <typeparam name="TDIctValue">The type of the values in IDictionary instances.</typeparam>
    /// <remarks>If the class type parameters match the method type parameters, the mock objects will be returned.  If not, a standard Dictionary or Set will be returned.  The purpose of this class is to facilitate testing that locks have been acquired in the ConcurrentAccessManager and ConcurrentDirectedGraph classes, hence granular checking of all underlying dictionaries or sets is not necessary.</remarks>
    public class MockCollectionFactory<TSet, TDictKey, TDIctValue> : ICollectionFactory
    {
        protected IDictionary<TDictKey, TDIctValue> mockDictionary;
        protected ISet<TSet> mockSet;

        public MockCollectionFactory(IDictionary<TDictKey, TDIctValue> mockDictionary, ISet<TSet> mockSet)
        {
            this.mockDictionary = mockDictionary;
            this.mockSet = mockSet;
        }

        public IDictionary<TKey, TValue> GetDictionaryInstance<TKey, TValue>()
        {
            if (typeof(TDictKey) == typeof(TKey) && typeof(TDIctValue) == typeof(TValue))
            {
                return (IDictionary<TKey, TValue>)mockDictionary;
            }
            else
            {
                return new Dictionary<TKey, TValue>();
            }
        }

        public ISet<T> GetSetInstance<T>()
        {
            if (typeof(TSet) == typeof(T))
            {
                return (ISet<T>)mockSet;
            }
            else
            {
                return new HashSet<T>();
            }
        }
    }
}
