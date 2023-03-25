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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;

namespace ApplicationAccess
{
    /// <summary>
    /// An implementation of a HashSet which follows the same thread-safety practices as other classes in the System.Collections.Concurrent namespace.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    public class ConcurrentHashSet<T> : ISet<T>
    {
        /// <summary>The dictionary used to implement the set.</summary>
        /// <remarks>Note only the key of the dictionary is used.</remarks>
        protected ConcurrentDictionary<T, Byte> dictionary;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentHashSet class.
        /// </summary>
        public ConcurrentHashSet()
        {
            dictionary = new ConcurrentDictionary<T, byte>();
        }

        /// <summary>
        /// The number of elements that are contained in the set.
        /// </summary>
        public Int32 Count
        {
            get { return dictionary.Count; }
        }

        #pragma warning disable 1591

        public bool IsReadOnly => throw new NotImplementedException();

        /// <summary>
        /// Adds the specified element to the set.
        /// </summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>True if the element is added to the set.  False if the element is already present.</returns>
        /// <remarks>True is returned by default here which doesn't strictly match the ISet definition.  In the AccessManager use case Add() should never be called when a key already exists.</remarks>
        public bool Add(T item)
        {
            dictionary[item] = 0;

            return true;
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        /// <summary>
        /// Determines whether the set contains the specified element.
        /// </summary>
        /// <param name="item">The element to locate in theset.</param>
        /// <returns>True if the set contains the specified element.  Otherwise, false.</returns>
        public bool Contains(T item)
        {
            return dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object for the set.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return dictionary.Keys.GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the specified element from the set.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element is successfully found and removed.  Otherwise, false. This method returns false if item is not found in the set.</returns>
        public bool Remove(T item)
        {
            Boolean result = dictionary.TryRemove(item, out Byte tempValue);
            // TODO: REMOVE TEMPORARY DEBUGGING CODE
            if (result == false)
                throw new Exception($"ConcurrentDictionary.TryRemove() method with param '{item}' returned false.");
            return result;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #pragma warning restore
    }
}
