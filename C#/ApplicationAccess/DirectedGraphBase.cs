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
using System.Linq;

namespace ApplicationAccess
{
    /// <summary>
    /// A base class for directed graphs with different definable types for leaf and non-leaf vertices.
    /// </summary>
    /// <typeparam name="TLeaf">The type of leaf vertices.</typeparam>
    /// <typeparam name="TNonLeaf">The type of non-leaf vertices.</typeparam>
    public abstract class DirectedGraphBase<TLeaf, TNonLeaf>
    {
        /// <summary>Creates instances of collection classes.</summary>
        protected ICollectionFactory collectionFactory;
        /// <summary>The leaf vertices in the graph.</summary>
        protected ISet<TLeaf> leafVertices;
        /// <summary>The non-leaf vertices in the graph.</summary>
        protected ISet<TNonLeaf> nonLeafVertices;
        /// <summary>The edges which join leaf and non-left vertices within the graph.</summary>
        protected IDictionary<TLeaf, ISet<TNonLeaf>> leafToNonLeafEdges;
        /// <summary>The edges which join non-leaf and non-left vertices within the graph.</summary>
        protected IDictionary<TNonLeaf, ISet<TNonLeaf>> nonLeafToNonLeafEdges;

        /// <summary>
        /// Returns a collection of all leaf vertices in the graph.
        /// </summary>
        public IEnumerable<TLeaf> LeafVertices
        {
            get
            {
                foreach (TLeaf currentLeafVertex in leafVertices)
                {
                    yield return currentLeafVertex;
                }
            }
        }

        /// <summary>
        /// Returns a collection of all non-leaf vertices in the graph.
        /// </summary>
        public IEnumerable<TNonLeaf> NonLeafVertices
        {
            get
            {
                foreach(TNonLeaf currentNonLeafVertex in nonLeafVertices)
                {
                    yield return currentNonLeafVertex;
                }
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DirectedGraphBase class.
        /// </summary>
        /// <param name="collectionFactory">Creates instances of collection classes.</param>
        public DirectedGraphBase(ICollectionFactory collectionFactory)
        {
            this.collectionFactory = collectionFactory;
            leafVertices = this.collectionFactory.GetSetInstance<TLeaf>();
            nonLeafVertices = this.collectionFactory.GetSetInstance<TNonLeaf>();
            leafToNonLeafEdges = this.collectionFactory.GetDictionaryInstance<TLeaf, ISet<TNonLeaf>>();
            nonLeafToNonLeafEdges = this.collectionFactory.GetDictionaryInstance<TNonLeaf, ISet<TNonLeaf>>();
        }

        /// <summary>
        /// Removes all vertices and edges from the graph.
        /// </summary>
        /// <remarks>Since the Clear() method on HashSets and Dictionaries underlying the class are O(n) operations, performance will scale roughly with the number of vertices and edges stored in the graph.</remarks>
        public virtual void Clear()
        {
            leafVertices.Clear();
            nonLeafVertices.Clear();
            leafToNonLeafEdges.Clear();
            nonLeafToNonLeafEdges.Clear();
        }

        /// <summary>
        /// Adds a leaf vertex to the graph.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to add.</param>
        public virtual void AddLeafVertex(TLeaf leafVertex)
        {
            if (leafVertices.Contains(leafVertex) == true)
                throw new LeafVertexAlreadyExistsException<TLeaf>($"Vertex '{leafVertex.ToString()}' already exists in the graph.", leafVertex);

            leafVertices.Add(leafVertex);
        }

        /// <summary>
        /// Returns true if the specified leaf vertex exists in the graph.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to check for.</param>
        /// <returns>True if the leaf vertex exists.  False otherwise.</returns>
        public Boolean ContainsLeafVertex(TLeaf leafVertex)
        {
            return leafVertices.Contains(leafVertex);
        }

        /// <summary>
        /// Removes a leaf vertex from the graph.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to remove.</param>
        public virtual void RemoveLeafVertex(TLeaf leafVertex)
        {
            ThrowExceptionIfLeafVertexDoesntExistInGraph(leafVertex, nameof(leafVertex));

            if (leafToNonLeafEdges.ContainsKey(leafVertex) == true)
            {
                leafToNonLeafEdges.Remove(leafVertex);
            }
            leafVertices.Remove(leafVertex);
        }

        /// <summary>
        /// Adds a non-leaf vertex to the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to add.</param>
        public virtual void AddNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            if (nonLeafVertices.Contains(nonLeafVertex) == true)
                throw new NonLeafVertexAlreadyExistsException<TNonLeaf>($"Vertex '{nonLeafVertex.ToString()}' already exists in the graph.", nonLeafVertex);

            nonLeafVertices.Add(nonLeafVertex);
        }

        /// <summary>
        /// Returns true if the specified non-leaf vertex exists in the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to check for.</param>
        /// <returns>True if the non-leaf vertex exists.  False otherwise.</returns>
        public Boolean ContainsNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            return nonLeafVertices.Contains(nonLeafVertex);
        }

        /// <summary>
        /// Removes a non-leaf vertex from the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to remove.</param>
        public virtual void RemoveNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            this.RemoveNonLeafVertex(nonLeafVertex, (fromVertex, toVertex) => { }, (fromVertex, toVertex) => { });
        }

        /// <summary>
        /// Adds an edge to the graph between the specified leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public virtual void AddLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            ThrowExceptionIfLeafVertexDoesntExistInGraph(fromVertex, nameof(fromVertex));
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(toVertex, nameof(toVertex));
            if (leafToNonLeafEdges.ContainsKey(fromVertex) == true)
            {
                if (leafToNonLeafEdges[fromVertex].Contains(toVertex) == true)
                {
                    throw new LeafToNonLeafEdgeAlreadyExistsException<TLeaf, TNonLeaf>($"An edge already exists between vertices '{fromVertex.ToString()}' and '{toVertex.ToString()}'.", fromVertex, toVertex);
                }
            }

            if (leafToNonLeafEdges.ContainsKey(fromVertex) != true)
            {
                leafToNonLeafEdges.Add(fromVertex, collectionFactory.GetSetInstance<TNonLeaf>());
            }
            leafToNonLeafEdges[fromVertex].Add(toVertex);
        }

        /// <summary>
        /// Gets the edges connected from the specified leaf vertex.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to retrieve the edges for.</param>
        /// <returns>A collection of non-leaf vertices the specified leaf vertex is connected to by an edge.</returns>
        public IEnumerable<TNonLeaf> GetLeafEdges(TLeaf leafVertex)
        {
            return GetLeafEdges(leafVertex, true);
        }

        /// <summary>
        /// Removes the edge from the graph between the specified leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public virtual void RemoveLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            ThrowExceptionIfLeafVertexDoesntExistInGraph(fromVertex, nameof(fromVertex));
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(toVertex, nameof(toVertex));
            if (leafToNonLeafEdges.ContainsKey(fromVertex) == false || leafToNonLeafEdges[fromVertex].Contains(toVertex) == false)
                throw new LeafToNonLeafEdgeNotFoundException<TLeaf, TNonLeaf>($"An edge does not exist between vertices '{fromVertex.ToString()}' and '{toVertex.ToString()}'.", fromVertex, toVertex);

            leafToNonLeafEdges[fromVertex].Remove(toVertex);
        }

        /// <summary>
        /// Adds an edge to the graph between the specified non-leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public virtual void AddNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(fromVertex, nameof(fromVertex));
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(toVertex, nameof(toVertex));
            if (fromVertex.Equals(toVertex) == true)
                throw new ArgumentException($"Parameters '{nameof(fromVertex)}' and '{nameof(toVertex)}' cannot contain the same vertex.", nameof(toVertex));
            if (nonLeafToNonLeafEdges.ContainsKey(fromVertex) == true)
            {
                if (nonLeafToNonLeafEdges[fromVertex].Contains(toVertex) == true)
                {
                    throw new NonLeafToNonLeafEdgeAlreadyExistsException<TNonLeaf>($"An edge already exists between vertices '{fromVertex.ToString()}' and '{toVertex.ToString()}'.", fromVertex, toVertex);
                }
            }
            // Check whether adding edge would create a circular reference
            Func<TNonLeaf, Boolean> vertexAction = (TNonLeaf currentVertex) =>
            {
                if (currentVertex.Equals(fromVertex) == true)
                {
                    throw new CircularReferenceException($"An edge between vertices '{fromVertex.ToString()}' and '{toVertex.ToString()}' cannot be created as it would cause a circular reference.");
                }

                return true;
            };
            TraverseFromNonLeaf(toVertex, vertexAction);

            if (nonLeafToNonLeafEdges.ContainsKey(fromVertex) != true)
            {
                nonLeafToNonLeafEdges.Add(fromVertex, collectionFactory.GetSetInstance<TNonLeaf>());
            }
            nonLeafToNonLeafEdges[fromVertex].Add(toVertex);
        }

        /// <summary>
        /// Gets the edges connected from the specified non-leaf vertex.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to retrieve the edges for.</param>
        /// <returns>A collection of non-leaf vertices the specified vertex is connected to by an edge.</returns>
        public IEnumerable<TNonLeaf> GetNonLeafEdges(TNonLeaf nonLeafVertex)
        {
            return GetNonLeafEdges(nonLeafVertex, true);
        }

        /// <summary>
        /// Removes the edge from the graph between the specified non-leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public virtual void RemoveNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(fromVertex, nameof(fromVertex));
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(toVertex, nameof(toVertex));
            if (nonLeafToNonLeafEdges.ContainsKey(fromVertex) == false || nonLeafToNonLeafEdges[fromVertex].Contains(toVertex) == false)
                throw new NonLeafToNonLeafEdgeNotFoundException<TNonLeaf>($"An edge does not exist between vertices '{fromVertex.ToString()}' and '{toVertex.ToString()}'.", fromVertex, toVertex);

            nonLeafToNonLeafEdges[fromVertex].Remove(toVertex);
        }

        /// <summary>
        /// Traverses the graph, invoking the specified action at each vertex (not including the start vertex).
        /// </summary>
        /// <param name="startVertex">The leaf vertex to begin traversing at.</param>
        /// <param name="vertexAction">The action to perform at each non-leaf vertex.  Accepts a single parameter which is the current vertex to perform the action on, and returns a boolean indicating whether traversal should continue.</param>
        public void TraverseFromLeaf(TLeaf startVertex, Func<TNonLeaf, Boolean> vertexAction)
        {
            ThrowExceptionIfLeafVertexDoesntExistInGraph(startVertex, nameof(startVertex));

            if (leafToNonLeafEdges.ContainsKey(startVertex) == true)
            {
                var visitedVertices = new HashSet<TNonLeaf>();
                foreach (TNonLeaf nextEdgeVertex in leafToNonLeafEdges[startVertex])
                {
                    TraverseFromNonLeafRecurse(nextEdgeVertex, visitedVertices, vertexAction);
                }
            }
        }

        /// <summary>
        /// Traverses the graph, invoking the specified action at each vertex (including the start vertex).
        /// </summary>
        /// <param name="startVertex">The non-leaf vertex to begin traversing at.</param>
        /// <param name="vertexAction">The action to perform at each non-leaf vertex.  Accepts a single parameter which is the current vertex to perform the action on, and returns a boolean indicating whether traversal should continue.</param>
        public void TraverseFromNonLeaf(TNonLeaf startVertex, Func<TNonLeaf, Boolean> vertexAction)
        {
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(startVertex, nameof(startVertex));

            // TODO: Will traverse depth-first using recursion as it's not expected that the paths will become very deep.  Might need to change to a stack implementation if this changes.
            TraverseFromNonLeafRecurse(startVertex, new HashSet<TNonLeaf>(), vertexAction);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Removes a non-leaf vertex from the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to remove.</param>
        /// <param name="leafToNonLeafEdgePostRemovalAction">An action which is invoked after removing a leaf to non-leaf edge containing the specified non-leaf vertex.  Accepts 2 parameters: the 'from' vertex in the edge being removed, and the the 'to' vertex in the edge being removed.</param>
        /// <param name="nonLeafToNonLeafEdgePostRemovalAction">An action which is invoked after removing a non-leaf to non-leaf edge containing the specified non-leaf vertex.  Accepts 2 parameters: the 'from' vertex in the edge being removed, and the the 'to' vertex in the edge being removed.</param>
        protected void RemoveNonLeafVertex(TNonLeaf nonLeafVertex, Action<TLeaf, TNonLeaf> leafToNonLeafEdgePostRemovalAction, Action<TNonLeaf, TNonLeaf> nonLeafToNonLeafEdgePostRemovalAction)
        {
            ThrowExceptionIfNonLeafVertexDoesntExistInGraph(nonLeafVertex, nameof(nonLeafVertex));

            // Remove the edges connected 'from' the vertex
            if (nonLeafToNonLeafEdges.ContainsKey(nonLeafVertex) == true)
            {
                nonLeafToNonLeafEdges.Remove(nonLeafVertex);
            }

            // Find the edges connected 'to' the vertex
            var connectedLeafVertices = new HashSet<TLeaf>();
            var connectedNonLeafVertices = new HashSet<TNonLeaf>();
            foreach (KeyValuePair<TLeaf, ISet<TNonLeaf>> currentKvp in leafToNonLeafEdges)
            {
                if (currentKvp.Value.Contains(nonLeafVertex) == true)
                {
                    connectedLeafVertices.Add(currentKvp.Key);
                }
            }
            foreach (KeyValuePair<TNonLeaf, ISet<TNonLeaf>> currentKvp in nonLeafToNonLeafEdges)
            {
                if (currentKvp.Value.Contains(nonLeafVertex) == true)
                {
                    connectedNonLeafVertices.Add(currentKvp.Key);
                }
            }

            // Remove the edges connected 'to' the vertex
            foreach (TLeaf currentConnectedLeafVertex in connectedLeafVertices)
            {
                leafToNonLeafEdges[currentConnectedLeafVertex].Remove(nonLeafVertex);
                leafToNonLeafEdgePostRemovalAction.Invoke(currentConnectedLeafVertex, nonLeafVertex);
            }
            foreach (TNonLeaf currentConnectedNonLeafVertex in connectedNonLeafVertices)
            {
                nonLeafToNonLeafEdges[currentConnectedNonLeafVertex].Remove(nonLeafVertex);
                nonLeafToNonLeafEdgePostRemovalAction.Invoke(currentConnectedNonLeafVertex, nonLeafVertex);
            }

            // Remove the vertex
            nonLeafVertices.Remove(nonLeafVertex);
        }

        /// <summary>
        /// Traverses the entire graph, invoking the specified actions at each leaf and non-leaf vertex.
        /// </summary>
        /// <param name="leafVertexAction">The action to perform at leaf vertices.  Accepts a single parameter which is the current leaf vertex to perform the action on, and returns a boolean indicating whether traversal should continue.</param>
        /// <param name="nonLeafVertexAction">The action to perform at non-leaf vertices.  Accepts a single parameter which is the current non-leaf vertex to perform the action on, and returns a boolean indicating whether traversal should continue.</param>
        protected void TraverseGraph(Func<TLeaf, Boolean> leafVertexAction, Func<TNonLeaf, Boolean> nonLeafVertexAction)
        {
            var visitedVertices = new HashSet<TNonLeaf>();
            var keepTraversing = true;
            foreach (TLeaf currentLeaf in leafVertices)
            {
                if (keepTraversing == true)
                {
                    keepTraversing = leafVertexAction.Invoke(currentLeaf);
                }
                if (keepTraversing == false)
                {
                    break;
                }

                if (leafToNonLeafEdges.ContainsKey(currentLeaf) == true)
                {
                    foreach (TNonLeaf currentNonLeaf in leafToNonLeafEdges[currentLeaf])
                    {
                        if (visitedVertices.Contains(currentNonLeaf) == false)
                        {
                            keepTraversing = TraverseFromNonLeafRecurse(currentNonLeaf, visitedVertices, nonLeafVertexAction);
                        }
                        if (keepTraversing == false)
                        {
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recurses to a non-leaf vertex as part of a traversal, invoking the specified action.
        /// </summary>
        /// <param name="nextVertex">The non-leaf vertex to recurse to.</param>
        /// <param name="visitedVertices">The set of vertices which have already been visited as part of the traversal.</param>
        /// <param name="vertexAction">The action to perform at the vertex.  Accepts a single parameter which is the current non-leaf vertex to perform the action on, and returns a boolean indicating whether traversal should continue.</param>
        /// <returns>Whether or not traversal should continue.</returns>
        protected Boolean TraverseFromNonLeafRecurse(TNonLeaf nextVertex, HashSet<TNonLeaf> visitedVertices, Func<TNonLeaf, Boolean> vertexAction)
        {
            Boolean keepTraversing = vertexAction.Invoke(nextVertex);
            if (keepTraversing == true)
            {
                visitedVertices.Add(nextVertex);
                if (nonLeafToNonLeafEdges.ContainsKey(nextVertex) == true)
                {
                    foreach (TNonLeaf nextEdgeVertex in nonLeafToNonLeafEdges[nextVertex])
                    {
                        if (visitedVertices.Contains(nextEdgeVertex) == false)
                        {
                            keepTraversing = TraverseFromNonLeafRecurse(nextEdgeVertex, visitedVertices, vertexAction);
                        }
                        if (keepTraversing == false)
                        {
                            break;
                        }
                    }
                }
            }

            return keepTraversing;
        }

        /// <summary>
        /// Gets the edges connected from the specified leaf vertex.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to retrieve the edges for.</param>
        /// <param name="checkVertexExists">Whether or not an explicit check should be made as to whether the specified vertex exists.</param>
        /// <returns>A collection of non-leaf vertices the specified leaf vertex is connected to by an edge.</returns>
        protected IEnumerable<TNonLeaf> GetLeafEdges(TLeaf leafVertex, Boolean checkVertexExists)
        {
            // TODO: Would like to not have to copy the items to a List here, but...
            //   1. If I just return the ISet<TNonLeaf> it has problem implications for the ConcurrentAccessManager (since it's using the ConcurrentSet class which has unimplemented methods)
            //   2. If I do a yield in the foreach, the AccessManagerBase class can't catch the LeafVertexNotFoundException and rethrow as something else
            //   Hence copying to list was only option I could find which resolved both of the above
            //   Same applies for GetNonLeafEdges()

            if (checkVertexExists == true)
                ThrowExceptionIfLeafVertexDoesntExistInGraph(leafVertex, nameof(leafVertex));

            if (leafToNonLeafEdges.ContainsKey(leafVertex) == true)
            {
                var returnList = new List<TNonLeaf>(leafToNonLeafEdges[leafVertex].Count);
                foreach (TNonLeaf currentNonLeafVertex in leafToNonLeafEdges[leafVertex])
                {
                    returnList.Add(currentNonLeafVertex);
                }

                return returnList;
            }
            else
            {
                return Enumerable.Empty<TNonLeaf>();
            }
        }

        /// <summary>
        /// Gets the edges connected from the specified non-leaf vertex.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to retrieve the edges for.</param>
        /// <param name="checkVertexExists">Whether or not an explicit check should be made as to whether the specified vertex exists.</param>
        /// <returns>A collection of non-leaf vertices the specified vertex is connected to by an edge.</returns>
        protected IEnumerable<TNonLeaf> GetNonLeafEdges(TNonLeaf nonLeafVertex, Boolean checkVertexExists)
        {
            if (checkVertexExists == true)
                ThrowExceptionIfNonLeafVertexDoesntExistInGraph(nonLeafVertex, nameof(nonLeafVertex));

            if (nonLeafToNonLeafEdges.ContainsKey(nonLeafVertex) == true)
            {
                var returnList = new List<TNonLeaf>(nonLeafToNonLeafEdges[nonLeafVertex].Count);
                foreach (TNonLeaf currentNonLeafVertex in nonLeafToNonLeafEdges[nonLeafVertex])
                {
                    returnList.Add(currentNonLeafVertex);
                }

                return returnList;
            }
            else
            {
                return Enumerable.Empty<TNonLeaf>();
            }
        }

        #pragma warning disable 1591

        protected void ThrowExceptionIfLeafVertexDoesntExistInGraph(TLeaf vertex, String parameterName)
        {
            if (leafVertices.Contains(vertex) == false)
                throw new LeafVertexNotFoundException<TLeaf>($"Vertex '{vertex.ToString()}' does not exist in the graph.", vertex);
        }

        protected void ThrowExceptionIfNonLeafVertexDoesntExistInGraph(TNonLeaf vertex, String parameterName)
        {
            if (nonLeafVertices.Contains(vertex) == false)
                throw new NonLeafVertexNotFoundException<TNonLeaf>($"Vertex '{vertex.ToString()}' does not exist in the graph.", vertex);
        }

        #endregion

        #pragma warning restore 1591
    }
}
