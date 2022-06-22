﻿/*
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
using ApplicationMetrics;

namespace ApplicationAccess.Metrics
{
    public class MetricLoggingConcurrentDirectedGraph<TLeaf, TNonLeaf> : ConcurrentDirectedGraph<TLeaf, TNonLeaf>
    {
        /// <summary>The number of leaf to non-leaf edges in the graph.</summary>
        protected Int32 leafToNonLeafEdgeCount;
        /// <summary>The number of non-leaf to non-leaf edges in the graph.</summary>
        protected Int32 nonLeafToNonLeafEdgeCount;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>The logger for metrics.</summary>
        public IMetricLogger MetricLogger
        {
            get { return metricLogger; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="metricLogger">The logger for metrics.</param>
        public MetricLoggingConcurrentDirectedGraph(IMetricLogger metricLogger)
            : base(true)
        {
            leafToNonLeafEdgeCount = 0;
            nonLeafToNonLeafEdgeCount = 0;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="acquireLocks">Whether locks should be acquired before modifying underlying collection objects.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>Parameter 'acquireLocks' should be set false where the ConcurrentDirectedGraph is used/composed within another class which acquires relevant locks before calling modification methods.  In all other cases, 'acquireLocks' should be set true.</remarks>
        public MetricLoggingConcurrentDirectedGraph(Boolean acquireLocks, IMetricLogger metricLogger)
            : base(acquireLocks)
        {
            leafToNonLeafEdgeCount = 0;
            nonLeafToNonLeafEdgeCount = 0;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <param name="acquireLocks">Whether locks should be acquired before modifying underlying collection objects.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public MetricLoggingConcurrentDirectedGraph(ICollectionFactory collectionFactory, Boolean acquireLocks, IMetricLogger metricLogger)
            : base(collectionFactory, acquireLocks)
        {
            leafToNonLeafEdgeCount = 0;
            nonLeafToNonLeafEdgeCount = 0;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Adds a leaf vertex to the graph.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to add.</param>
        public override void AddLeafVertex(TLeaf leafVertex)
        {
            Action<TLeaf, Action> wrappingAction = (actionLeaf, baseAction) =>
            {
                baseAction.Invoke();
                metricLogger.Set(new LeafVerticesStored(), leafVertices.Count);
            };
            this.AddLeafVertex(leafVertex, wrappingAction);
        }

        /// <summary>
        /// Removes a leaf vertex from the graph.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to remove.</param>
        public override void RemoveLeafVertex(TLeaf leafVertex)
        {
            Action<TLeaf, Action> wrappingAction = (actionLeaf, baseAction) =>
            {
                if (leafToNonLeafEdges.ContainsKey(leafVertex) == true)
                {
                    leafToNonLeafEdgeCount -= leafToNonLeafEdges[leafVertex].Count;
                }
                baseAction.Invoke();
                metricLogger.Set(new LeafVerticesStored(), leafVertices.Count);
                metricLogger.Set(new LeafToNonLeafEdgesStored(), leafToNonLeafEdgeCount);
            };
            this.RemoveLeafVertex(leafVertex, wrappingAction);
        }

        /// <summary>
        /// Adds a non-leaf vertex to the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to add.</param>
        public override void AddNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            Action<TNonLeaf, Action> wrappingAction = (actionNonLeafVertex, baseAction) =>
            {
                baseAction.Invoke();
                metricLogger.Set(new NonLeafVerticesStored(), nonLeafVertices.Count);
            };
            this.AddNonLeafVertex(nonLeafVertex, wrappingAction);
        }

        /// <summary>
        /// Removes a non-leaf vertex from the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to remove.</param>
        public override void RemoveNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            Action<TNonLeaf, Action> wrappingAction = (actionNonLeafVertex, baseAction) =>
            {
                if (nonLeafToNonLeafEdges.ContainsKey(nonLeafVertex) == true)
                {
                    nonLeafToNonLeafEdgeCount -= nonLeafToNonLeafEdges[nonLeafVertex].Count;
                }
                Action<TLeaf, TNonLeaf> leafToNonLeafEdgePostRemovalAction = (fromVertex, toVertex) => { leafToNonLeafEdgeCount--; };
                Action<TNonLeaf, TNonLeaf> nonLeafToNonLeafEdgePostRemovalAction = (fromVertex, toVertex) => { nonLeafToNonLeafEdgeCount--; };
                base.RemoveNonLeafVertex(nonLeafVertex, leafToNonLeafEdgePostRemovalAction, nonLeafToNonLeafEdgePostRemovalAction);
                metricLogger.Set(new LeafToNonLeafEdgesStored(), leafToNonLeafEdgeCount);
                metricLogger.Set(new NonLeafToNonLeafEdgesStored(), nonLeafToNonLeafEdgeCount);
                metricLogger.Set(new NonLeafVerticesStored(), nonLeafVertices.Count);
            };
            this.RemoveNonLeafVertex(nonLeafVertex, wrappingAction);
        }

        /// <summary>
        /// Adds an edge to the graph between the specified leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public override void AddLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
                leafToNonLeafEdgeCount++;
                metricLogger.Set(new LeafToNonLeafEdgesStored(), leafToNonLeafEdgeCount);
            };
            this.AddLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }

        /// <summary>
        /// Removes the edge from the graph between the specified leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public override void RemoveLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
                leafToNonLeafEdgeCount--;
                metricLogger.Set(new LeafToNonLeafEdgesStored(), leafToNonLeafEdgeCount);
            };
            this.RemoveLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }

        /// <summary>
        /// Adds an edge to the graph between the specified non-leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public override void AddNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TNonLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
                nonLeafToNonLeafEdgeCount++;
                metricLogger.Set(new NonLeafToNonLeafEdgesStored(), nonLeafToNonLeafEdgeCount);
            };
            this.AddNonLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }

        /// <summary>
        /// Removes the edge from the graph between the specified non-leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        public override void RemoveNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TNonLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
                nonLeafToNonLeafEdgeCount--;
                metricLogger.Set(new NonLeafToNonLeafEdgesStored(), nonLeafToNonLeafEdgeCount);
            };
            this.RemoveNonLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }
    }
}
