/*
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
using ApplicationAccess.Utilities;

namespace ApplicationAccess
{
    /// <summary>
    /// A thread-safe version of the DirectedGraph class, which can be accessed and modified by multiple threads concurrently.
    /// </summary>
    /// <typeparam name="TLeaf">The type of leaf vertices.</typeparam>
    /// <typeparam name="TNonLeaf">The type of non-leaf vertices.</typeparam>
    /// <remarks>Thread safety is implemented by using concurrent collections internally to represent the graph (allows for concurrent read and enumeration operations), and locks to serialize modification operations.</remarks>
    public class ConcurrentDirectedGraph<TLeaf, TNonLeaf> : DirectedGraphBase<TLeaf, TNonLeaf>
    {
        /// <summary>Whether locks should be acquired before modifying underlying collection objects.</summary>
        protected Boolean acquireLocks;
        /// <summary>Manages acquiring locks on underlying sets and dictionaries.</summary>
        protected LockManager lockManager;
        // In this class we need specific lock objects instead of locking the set/dictionaries directly (unlike ConcurrentAccessManager) as the set/dictionaries are mocked in the unit tests, with common mock objects used for multiple set/dictionaries.
        /// <summary>Lock object for the leaf vertices collection.</summary>
        protected Object leafVerticesLock;
        /// <summary>Lock object for the non-leaf vertices collection.</summary>
        protected Object nonLeafVerticesLock;
        /// <summary>Lock object for the leaf to non-leaf map.</summary>
        protected Object leafToNonLeafEdgesLock;
        /// <summary>Lock object for the non-leaf to non-leaf map.</summary>
        protected Object nonLeafToNonLeafEdgesLock;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings for edges within the graph.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings for edges in the graph are stored in both directions.  This avoids slow scanning of dictionaries which store the edge mappings in certain operations (like RemoveLeafToNonLeafEdge()), at the cost of addition storage and hence memory usage.</remarks>
        public ConcurrentDirectedGraph(Boolean storeBidirectionalMappings)
            : this(true, storeBidirectionalMappings)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings for edges within the graph.</param>
        /// <param name="acquireLocks">Whether locks should be acquired before modifying underlying collection objects.</param>
        /// <remarks>
        /// <para>Parameter 'acquireLocks' should be set false where the ConcurrentDirectedGraph is used/composed within another class which acquires relevant locks before calling modification methods.  In all other cases, 'acquireLocks' should be set true.</para>
        /// <para>If parameter 'storeBidirectionalMappings' is set to True, mappings for edges in the graph are stored in both directions.  This avoids slow scanning of dictionaries which store the edge mappings in certain operations (like RemoveLeafToNonLeafEdge()), at the cost of addition storage and hence memory usage.</para>
        /// </remarks>
        public ConcurrentDirectedGraph(Boolean storeBidirectionalMappings, Boolean acquireLocks)
            : base(new ConcurrentCollectionFactory(), storeBidirectionalMappings)
        {
            this.acquireLocks = acquireLocks;
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings for edges within the graph.</param>
        /// <param name="acquireLocks">Whether locks should be acquired before modifying underlying collection objects.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public ConcurrentDirectedGraph(ICollectionFactory collectionFactory, Boolean storeBidirectionalMappings, Boolean acquireLocks)
            : base(collectionFactory, storeBidirectionalMappings)
        {
            this.acquireLocks = acquireLocks;
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            Action<Action> wrappingAction = (baseAction) =>
            {
                baseAction.Invoke();
            };
            this.Clear(wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddLeafVertex(TLeaf leafVertex)
        {
            Action<TLeaf, Action> wrappingAction = (actionLeaf, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddLeafVertex(leafVertex, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveLeafVertex(TLeaf leafVertex)
        {
            Action<TLeaf, Action> wrappingAction = (actionLeaf, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveLeafVertex(leafVertex, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            Action<TNonLeaf, Action> wrappingAction = (actionNonLeafVertex, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddNonLeafVertex(nonLeafVertex, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            Action<TNonLeaf, Action> wrappingAction = (actionNonLeafVertex, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveNonLeafVertex(nonLeafVertex, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TNonLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddNonLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            Action<TNonLeaf, TNonLeaf, Action> wrappingAction = (actionFromVertex, actiontTVertex, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveNonLeafToNonLeafEdge(fromVertex, toVertex, wrappingAction);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the classes' lock objects and dependencies.
        /// </summary>
        protected void InitializeLockObjects()
        {
            leafVerticesLock = new Object();
            nonLeafVerticesLock = new Object();
            leafToNonLeafEdgesLock = new Object();
            nonLeafToNonLeafEdgesLock = new Object();
            lockManager.RegisterLockObject(leafVerticesLock);
            lockManager.RegisterLockObject(nonLeafVerticesLock);
            lockManager.RegisterLockObject(leafToNonLeafEdgesLock);
            lockManager.RegisterLockObject(nonLeafToNonLeafEdgesLock);
            lockManager.RegisterLockObjectDependency(leafToNonLeafEdgesLock, leafVerticesLock);
            lockManager.RegisterLockObjectDependency(leafToNonLeafEdgesLock, nonLeafVerticesLock);
            lockManager.RegisterLockObjectDependency(nonLeafToNonLeafEdgesLock, nonLeafVerticesLock);
        }

        /// <summary>
        /// Removes all vertices and edges from the graph.
        /// </summary>
        /// <param name="wrappingAction">An action which wraps the operation to clear the graph, allowing arbitrary code to be run before and/or after clearing the graph, but whilst any mutual-exclusion locks are still acquired.  Accepts 1 parameters: the action which actually clears the graph.</param>
        public void Clear(Action<Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(() => { base.Clear(); });
            };
            if (acquireLocks == false)
            {
                baseAction.Invoke();
            }
            else
            {
                lockManager.AcquireAllLocksAndInvokeAction(baseAction);
            }
        }

        /// <summary>
        /// Adds a leaf vertex to the graph.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to add.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the leaf vertex, allowing arbitrary code to be run before and/or after adding the leaf vertex, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the leaf vertex being added, and the action which actually adds the leaf vertex.</param>
        protected void AddLeafVertex(TLeaf leafVertex, Action<TLeaf, Action> wrappingAction)
        {
            Action baseAction = () => 
            { 
                wrappingAction.Invoke(leafVertex, () => { base.AddLeafVertex(leafVertex); });
            };
            AcquireLocksAndInvokeAction(leafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, baseAction);
        }

        /// <summary>
        /// Removes a leaf vertex from the graph.
        /// </summary>
        /// <param name="leafVertex">The leaf vertex to remove.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the leaf vertex, allowing arbitrary code to be run before and/or after removing the leaf vertex, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the leaf vertex being removed, and the action which actually removes the leaf vertex.</param>
        protected void RemoveLeafVertex(TLeaf leafVertex, Action<TLeaf, Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(leafVertex, () => { base.RemoveLeafVertex(leafVertex); });
            };
            AcquireLocksAndInvokeAction(leafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, baseAction);
        }

        /// <summary>
        /// Adds a non-leaf vertex to the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to add.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the non-leaf vertex, allowing arbitrary code to be run before and/or after adding the non-leaf vertex, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the non-leaf vertex being added, and the action which actually adds the non-leaf vertex.</param>
        protected void AddNonLeafVertex(TNonLeaf nonLeafVertex, Action<TNonLeaf, Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(nonLeafVertex, () => { base.AddNonLeafVertex(nonLeafVertex); });
            };
            AcquireLocksAndInvokeAction(nonLeafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, baseAction);
        }

        /// <summary>
        /// Removes a non-leaf vertex from the graph.
        /// </summary>
        /// <param name="nonLeafVertex">The non-leaf vertex to remove.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the non-leaf vertex, allowing arbitrary code to be run before and/or after removing the non-leaf vertex, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the non-leaf vertex being removed, and the action which actually removes the non-leaf vertex.</param>
        protected void RemoveNonLeafVertex(TNonLeaf nonLeafVertex, Action<TNonLeaf, Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(nonLeafVertex, () => { base.RemoveNonLeafVertex(nonLeafVertex); });
            };
            AcquireLocksAndInvokeAction(nonLeafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, baseAction);
        }

        /// <summary>
        /// Adds an edge to the graph between the specified leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the edge, allowing arbitrary code to be run before and/or after adding the edge, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the 'from' vertex the edge connects, the 'to' vertex the edge connects, and the action which actually adds the edge.</param>
        protected void AddLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex, Action<TLeaf, TNonLeaf, Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(fromVertex, toVertex, () => { base.AddLeafToNonLeafEdge(fromVertex, toVertex); });
            };
            AcquireLocksAndInvokeAction(leafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, baseAction);
        }

        /// <summary>
        /// Removes the edge from the graph between the specified leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the edge, allowing arbitrary code to be run before and/or after removing the edge, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the 'from' vertex the edge connects, the 'to' vertex the edge connects, and the action which actually removes the edge.</param>
        protected void RemoveLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex, Action<TLeaf, TNonLeaf, Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(fromVertex, toVertex, () => { base.RemoveLeafToNonLeafEdge(fromVertex, toVertex); });
            };
            AcquireLocksAndInvokeAction(leafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, baseAction);
        }

        /// <summary>
        /// Adds an edge to the graph between the specified non-leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the edge, allowing arbitrary code to be run before and/or after adding the edge, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the 'from' vertex the edge connects, the 'to' vertex the edge connects, and the action which actually adds the edge.</param>
        protected void AddNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex, Action<TNonLeaf, TNonLeaf, Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(fromVertex, toVertex, () => { base.AddNonLeafToNonLeafEdge(fromVertex, toVertex); });
            };
            AcquireLocksAndInvokeAction(nonLeafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, baseAction);
        }

        /// <summary>
        /// Removes the edge from the graph between the specified non-leaf and non-leaf vertices.
        /// </summary>
        /// <param name="fromVertex">The vertex which is the 'from' vertex the edge connects.</param>
        /// <param name="toVertex">The vertex which is the 'to' vertex of the edge connects.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the edge, allowing arbitrary code to be run before and/or after removing the edge, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the 'from' vertex the edge connects, the 'to' vertex the edge connects, and the action which actually removes the edge.</param>
        protected void RemoveNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex, Action<TNonLeaf, TNonLeaf, Action> wrappingAction)
        {
            Action baseAction = () =>
            {
                wrappingAction.Invoke(fromVertex, toVertex, () => { base.RemoveNonLeafToNonLeafEdge(fromVertex, toVertex); });
            };
            AcquireLocksAndInvokeAction(nonLeafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, baseAction);
        }

        /// <summary>
        /// Uses the 'lockManager' member to acquire locks on the specified lock object (and associated objects) depending on the value of member 'acquireLocks', and invokes the specified action.
        /// </summary>
        /// <param name="lockObject">The object to lock (if 'acquireLocks' is true).</param>
        /// <param name="lockObjectDependencyPattern">The dependency pattern to apply to acquire locks on objects associated with the object specified by the 'lockObject' parameter.</param>
        /// <param name="action">The action to invoke.</param>
        protected void AcquireLocksAndInvokeAction(Object lockObject, LockObjectDependencyPattern lockObjectDependencyPattern, Action action)
        {
            if (acquireLocks == false)
            {
                action.Invoke();
            }
            else
            {
                lockManager.AcquireLocksAndInvokeAction(lockObject, lockObjectDependencyPattern, action);
            }
        }

        #endregion
    }
}
