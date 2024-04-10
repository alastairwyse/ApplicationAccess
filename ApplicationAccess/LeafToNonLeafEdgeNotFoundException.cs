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

namespace ApplicationAccess
{
    /// <summary>
    /// The exception that is thrown when a specified leaf to non-leaf edge of a DirectedGraph does not exist in the graph.
    /// </summary>
    /// <typeparam name="TLeaf">The type of leaf vertices in the graph.</typeparam>
    /// <typeparam name="TNonLeaf">The type of non-leaf vertices in the graph.</typeparam>
    public class LeafToNonLeafEdgeNotFoundException<TLeaf, TNonLeaf> : Exception
    {
        /// <summary>The vertex which is the 'from' vertex of the edge which does not exist in the graph.</summary>
        protected TLeaf fromVertex;
        /// <summary>The vertex which is the 'to' vertex of the edge which does not exist in the graph.</summary>
        protected TNonLeaf toVertex;

        /// <summary>
        /// The vertex which is the 'from' vertex of the edge which does not exist in the graph.
        /// </summary>
        public TLeaf FromVertex
        {
            get
            {
                return fromVertex;
            }
        }

        /// <summary>
        /// The vertex which is the 'to' vertex of the edge which does not exist in the graph.
        /// </summary>
        public TNonLeaf ToVertex
        {
            get
            {
                return toVertex;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.LeafToNonLeafEdgeNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="fromVertex">The vertex which is the 'from' vertex of the edge which does not exist in the graph.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge which does not exist in the graph.</param>
        public LeafToNonLeafEdgeNotFoundException(String message, TLeaf fromVertex, TNonLeaf toVertex)
            : base(message)
        {
            this.fromVertex = fromVertex;
            this.toVertex = toVertex;
        }
    }
}
