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

namespace ApplicationAccess.Serialization
{
    /// <summary>
    /// Defines methods to serialize and deserialize a DirectedGraph.
    /// </summary>
    /// <typeparam name="TSerializedObject">The type of object to serialize to and from.</typeparam>
    public interface IDirectedGraphSerializer<TSerializedObject>
    {
        /// <summary>
        /// Serializes the specified graph.
        /// </summary>
        /// <typeparam name="TLeaf">The type of leaf vertices in the graph.</typeparam>
        /// <typeparam name="TNonLeaf">The type of non-leaf vertices in the graph.</typeparam>
        /// <param name="graph">The directed graph to serialize.</param>
        /// <param name="leafStringifier">A string converter for leaf vertices in the graph.</param>
        /// <param name="nonLeafStringifier">A string converter for non-leaf vertices in the graph.</param>
        /// <returns>An object representing the graph.</returns>
        TSerializedObject Serialize<TLeaf, TNonLeaf>(DirectedGraphBase<TLeaf, TNonLeaf> graph, IUniqueStringifier<TLeaf> leafStringifier, IUniqueStringifier<TNonLeaf> nonLeafStringifier);

        /// <summary>
        /// Deserializes a graph.
        /// </summary>
        /// <typeparam name="TLeaf">The type of leaf vertices in the graph.</typeparam>
        /// <typeparam name="TNonLeaf">The type of non-leaf vertices in the graph.</typeparam>
        /// <param name="serializedGraph">The directed graph to serialize.</param>
        /// <param name="leafStringifier">A string converter for leaf vertices in the graph.</param>
        /// <param name="nonLeafStringifier">A string converter for non-leaf vertices in the graph.</param>
        /// <param name="directionGraphToDeserializeTo">The DirectedGraph instance to deserialize to.</param>
        /// <remarks>
        ///   <para>Any existing items and mappings stored in parameter 'directionGraphToDeserializeTo' will be cleared.</para>
        ///   <para>The DirectedGraph instance is passed as a parameter rather than returned from the method, to allow deserializing into types derived from DirectedGraph aswell as DirectedGraph itself.</para>
        /// </remarks>
        void Deserialize<TLeaf, TNonLeaf>(TSerializedObject serializedGraph, IUniqueStringifier<TLeaf> leafStringifier, IUniqueStringifier<TNonLeaf> nonLeafStringifier, DirectedGraphBase<TLeaf, TNonLeaf> directionGraphToDeserializeTo);
    }
}
