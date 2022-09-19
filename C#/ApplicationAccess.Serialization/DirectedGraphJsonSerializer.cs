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
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Serialization
{
    /// <summary>
    /// Serializes and deserializes a <see cref="DirectedGraph{TLeaf, TNonLeaf}"/> to and from a JSON document.
    /// </summary>
    public class DirectedGraphJsonSerializer : IDirectedGraphSerializer<JObject>
    {
        #pragma warning disable 1591

        protected const String leafVertexPropertyName = "leafVertex";
        protected const String leafVerticesPropertyName = "leafVertices";
        protected const String nonLeafVertexPropertyName = "nonLeafVertex";
        protected const String nonLeafVerticesPropertyName = "nonLeafVertices";
        protected const String leafToNonLeafEdgesPropertyName = "leafToNonLeafEdges";
        protected const String nonLeafToNonLeafEdgesPropertyName = "nonLeafToNonLeafEdges";

        #pragma warning restore 1591

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Serialization.DirectedGraphJsonSerializer class.
        /// </summary>
        public DirectedGraphJsonSerializer()
        {
        }

        /// <summary>
        /// Serializes the specified graph to a JSON document.
        /// </summary>
        /// <typeparam name="TLeaf">The type of leaf vertices in the graph.</typeparam>
        /// <typeparam name="TNonLeaf">The type of non-leaf vertices in the graph.</typeparam>
        /// <param name="graph">The directed graph to serialize.</param>
        /// <param name="leafStringifier">A string converter for leaf vertices in the graph.</param>
        /// <param name="nonLeafStringifier">A string converter for non-leaf vertices in the graph.</param>
        /// <returns>A JSON document representing the graph.</returns>
        public JObject Serialize<TLeaf, TNonLeaf>(DirectedGraphBase<TLeaf, TNonLeaf> graph, IUniqueStringifier<TLeaf> leafStringifier, IUniqueStringifier<TNonLeaf> nonLeafStringifier)
        {
            var returnDocument = new JObject();

            var leafVertices = new JArray();
            var leafToNonLeafEdges = new JArray();
            foreach (TLeaf currentLeafVertex in graph.LeafVertices)
            {
                leafVertices.Add(leafStringifier.ToString(currentLeafVertex));
                var edges = new JArray();
                foreach (TNonLeaf currentToVertex in graph.GetLeafEdges(currentLeafVertex))
                {
                    edges.Add(nonLeafStringifier.ToString(currentToVertex));
                }
                if (edges.Count > 0)
                {
                    var currentLeafEdges = new JObject();
                    currentLeafEdges.Add(leafVertexPropertyName, leafStringifier.ToString(currentLeafVertex));
                    currentLeafEdges.Add(nonLeafVerticesPropertyName, edges);
                    leafToNonLeafEdges.Add(currentLeafEdges);
                }
            }
            returnDocument.Add(leafVerticesPropertyName, leafVertices);
            returnDocument.Add(leafToNonLeafEdgesPropertyName, leafToNonLeafEdges);

            var nonLeafVertices = new JArray();
            var nonLeafToNonLeafEdges = new JArray();
            foreach (TNonLeaf currentNonLeafVertex in graph.NonLeafVertices)
            {
                nonLeafVertices.Add(nonLeafStringifier.ToString(currentNonLeafVertex));

                var edges = new JArray();
                foreach (TNonLeaf currentToVertex in graph.GetNonLeafEdges(currentNonLeafVertex))
                {
                    edges.Add(nonLeafStringifier.ToString(currentToVertex));
                }
                if (edges.Count > 0)
                {
                    var currentNonLeafEdges = new JObject();
                    currentNonLeafEdges.Add(nonLeafVertexPropertyName, nonLeafStringifier.ToString(currentNonLeafVertex));
                    currentNonLeafEdges.Add(nonLeafVerticesPropertyName, edges);
                    nonLeafToNonLeafEdges.Add(currentNonLeafEdges);
                }
            }
            returnDocument.Add(nonLeafVerticesPropertyName, nonLeafVertices);
            returnDocument.Add(nonLeafToNonLeafEdgesPropertyName, nonLeafToNonLeafEdges);

            return returnDocument;
        }

        /// <summary>
        /// Deserializes a graph from the specified JSON document.
        /// </summary>
        /// <typeparam name="TLeaf">The type of leaf vertices in the graph.</typeparam>
        /// <typeparam name="TNonLeaf">The type of non-leaf vertices in the graph.</typeparam>
        /// <param name="jsonDocument">The JSON document to deserialize the graph from.</param>
        /// <param name="leafStringifier">A string converter for leaf vertices in the graph.</param>
        /// <param name="nonLeafStringifier">A string converter for non-leaf vertices in the graph.</param>
        /// <param name="directionGraphToDeserializeTo">The DirectedGraph instance to deserialize to.</param>
        /// <remarks>
        /// <para>Any existing items and mappings stored in parameter 'directionGraphToDeserializeTo' will be cleared.</para>
        /// <para>The DirectedGraph instance is passed as a parameter rather than returned from the method, to allow deserializing into types derived from DirectedGraph aswell as DirectedGraph itself.</para>
        /// </remarks>
        public void Deserialize<TLeaf, TNonLeaf>(JObject jsonDocument, IUniqueStringifier<TLeaf> leafStringifier, IUniqueStringifier<TNonLeaf> nonLeafStringifier, DirectedGraphBase<TLeaf, TNonLeaf> directionGraphToDeserializeTo)
        {
            foreach (String currentPropertyName in new String[] { leafVerticesPropertyName, nonLeafVerticesPropertyName, leafToNonLeafEdgesPropertyName, nonLeafToNonLeafEdgesPropertyName })
            {
                if (jsonDocument.ContainsKey(currentPropertyName) == false)
                    throw new ArgumentException($"JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                if (!(jsonDocument[currentPropertyName] is JArray))
                    throw new ArgumentException($"Property '{currentPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' is not of type '{typeof(JArray)}'.", nameof(jsonDocument));
            }

            directionGraphToDeserializeTo.Clear();

            // Deserialize leaf vertices
            foreach (String currentLeafVertex in (JArray)jsonDocument[leafVerticesPropertyName])
            {
                try
                {
                    directionGraphToDeserializeTo.AddLeafVertex(leafStringifier.FromString(currentLeafVertex));
                }
                catch (Exception e)
                {
                    throw new DeserializationException($"Failed to deserialize leaf vertex '{currentLeafVertex}'.", e);
                }
            }

            // Deserialize non-leaf vertices
            foreach (String currentNonLeafVertex in (JArray)jsonDocument[nonLeafVerticesPropertyName])
            {
                try
                {
                    directionGraphToDeserializeTo.AddNonLeafVertex(nonLeafStringifier.FromString(currentNonLeafVertex));
                }
                catch (Exception e)
                {
                    throw new DeserializationException($"Failed to deserialize non-leaf vertex '{currentNonLeafVertex}'.", e);
                }
            }

            // Deserialize leaf to non-leaf edges
            foreach (JObject currentLeafEdgeSet in (JArray)jsonDocument[leafToNonLeafEdgesPropertyName])
            {
                foreach (String currentPropertyName in new String[] { leafVertexPropertyName, nonLeafVerticesPropertyName })
                {
                    if (currentLeafEdgeSet.ContainsKey(currentPropertyName) == false)
                        throw new ArgumentException($"Element of property '{leafToNonLeafEdgesPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                }
                if (!(currentLeafEdgeSet[leafVertexPropertyName] is JValue))
                    throw new ArgumentException($"Element of property '{leafToNonLeafEdgesPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{leafVertexPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                if (!(currentLeafEdgeSet[nonLeafVerticesPropertyName] is JArray))
                    throw new ArgumentException($"Element of property '{leafToNonLeafEdgesPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{nonLeafVerticesPropertyName}' property which is not of type '{typeof(JArray)}'.", nameof(jsonDocument));

                foreach (String currentNonLeafVertex in (JArray)currentLeafEdgeSet[nonLeafVerticesPropertyName])
                {
                    // TODO: Could have more granular exception handling here... separate exception handlers for stringifying each of from vertext and to vertext, and separate one for adding to graph
                    try
                    {
                        directionGraphToDeserializeTo.AddLeafToNonLeafEdge(leafStringifier.FromString(currentLeafEdgeSet[leafVertexPropertyName].ToString()), nonLeafStringifier.FromString(currentNonLeafVertex));
                    }
                    catch (Exception e)
                    {
                        throw new DeserializationException($"Failed to deserialize leaf to non-leaf edge between leaf vertex '{currentLeafEdgeSet[leafVertexPropertyName]}' and non-leaf vertex '{currentNonLeafVertex}'.", e);
                    }
                }
            }

            // Deserialize non-leaf to non-leaf edges
            foreach (JObject currentNonLeafEdgeSet in (JArray)jsonDocument[nonLeafToNonLeafEdgesPropertyName])
            {
                foreach (String currentPropertyName in new String[] { nonLeafVertexPropertyName, nonLeafVerticesPropertyName })
                {
                    if (currentNonLeafEdgeSet.ContainsKey(currentPropertyName) == false)
                        throw new ArgumentException($"Element of property '{nonLeafToNonLeafEdgesPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                }
                if (!(currentNonLeafEdgeSet[nonLeafVertexPropertyName] is JValue))
                    throw new ArgumentException($"Element of property '{nonLeafToNonLeafEdgesPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{nonLeafVertexPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                if (!(currentNonLeafEdgeSet[nonLeafVerticesPropertyName] is JArray))
                    throw new ArgumentException($"Element of property '{nonLeafToNonLeafEdgesPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{nonLeafVerticesPropertyName}' property which is not of type '{typeof(JArray)}'.", nameof(jsonDocument));

                foreach (String currentToNonLeafVertex in (JArray)currentNonLeafEdgeSet[nonLeafVerticesPropertyName])
                {
                    // TODO: Could have more granular exception handling here... separate exception handlers for de-stringifying each of from vertext and to vertext, and separate one for adding to graph
                    try
                    {
                        directionGraphToDeserializeTo.AddNonLeafToNonLeafEdge(nonLeafStringifier.FromString(currentNonLeafEdgeSet[nonLeafVertexPropertyName].ToString()), nonLeafStringifier.FromString(currentToNonLeafVertex));
                    }
                    catch (Exception e)
                    {
                        throw new DeserializationException($"Failed to deserialize non-leaf to non-leaf edge between non-leaf vertex '{currentNonLeafEdgeSet[nonLeafVertexPropertyName]}' and non-leaf vertex '{currentToNonLeafVertex}'.", e);
                    }
                }
            }
        }
    }
}