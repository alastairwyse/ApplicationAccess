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
using System.Text;

namespace ApplicationAccess
{
    /// <summary>
    /// A directed graph with different definable types for leaf and non-leaf vertices.
    /// </summary>
    /// <typeparam name="TLeaf">The type of leaf vertices.</typeparam>
    /// <typeparam name="TNonLeaf">The type of non-leaf vertices.</typeparam>
    public class DirectedGraph<TLeaf, TNonLeaf> : DirectedGraphBase<TLeaf, TNonLeaf>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DirectedGraph class.
        /// </summary>
        public DirectedGraph()
            : base(new StandardCollectionFactory())
        {
        }
    }
}
