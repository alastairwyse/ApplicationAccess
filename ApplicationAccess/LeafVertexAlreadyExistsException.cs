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
    /// The exception that is thrown when a specified leaf vertex of a DirectedGraph already exists in the graph.
    /// </summary>
    /// <typeparam name="T">The type of leaf vertices in the graph.</typeparam>
    public class LeafVertexAlreadyExistsException<T> : Exception
    {
        /// <summary>The leaf vertex which already exists in the graph.</summary>
        protected T leafVertex;

        /// <summary>
        /// The leaf vertex which already exists in the graph.
        /// </summary>
        public T LeafVertex
        {
            get
            {
                return leafVertex;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.LeafVertexAlreadyExistsException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="leafVertex">The leaf vertex which already exists in the graph.</param>
        public LeafVertexAlreadyExistsException(String message, T leafVertex)
            : base(message)
        {
            this.leafVertex = leafVertex;
        }
    }
}
