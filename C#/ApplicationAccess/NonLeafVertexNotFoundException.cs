/*
 * Copyright 2020 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Text;

namespace ApplicationAccess
{
    /// <summary>
    /// The exception that is thrown when a specified non-leaf vertex of a DirectedGraph does not exist in the graph.
    /// </summary>
    /// <typeparam name="T">The type of non-leaf vertices in the graph.</typeparam>
    public class NonLeafVertexNotFoundException<T> : Exception
    {
        /// <summary>The mpm-leaf vertex which does not exist in the graph.</summary>
        protected T nonLeafVertex;

        /// <summary>
        /// The non-leaf vertex which does not exist in the graph.
        /// </summary>
        public T NonLeafVertex
        {
            get
            {
                return nonLeafVertex;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.NonLeafVertexNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="nonLeafVertex">The non-leaf vertex which does not exist in the graph.</param>
        public NonLeafVertexNotFoundException(String message, T nonLeafVertex)
            : base(message)
        {
            this.nonLeafVertex = nonLeafVertex;
        }
    }
}
