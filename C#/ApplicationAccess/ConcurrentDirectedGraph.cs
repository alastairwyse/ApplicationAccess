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
        public ConcurrentDirectedGraph()
            : this(true)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="acquireLocks">Whether locks should be acquired before modifying underlying collection objects.</param>
        /// <remarks>Parameter 'acquireLocks' should be set false where the ConcurrentDirectedGraph is used/composed within another class which acquires relevant locks before calling modification methods.  In all other cases, 'acquireLocks' should be set false.</remarks>
        public ConcurrentDirectedGraph(Boolean acquireLocks)
            : base(new ConcurrentCollectionFactory())
        {
            this.acquireLocks = acquireLocks;
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentDirectedGraph class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <param name="acquireLocks">Whether locks should be acquired before modifying underlying collection objects.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public ConcurrentDirectedGraph(ICollectionFactory collectionFactory, Boolean acquireLocks)
            : base(collectionFactory)
        {
            this.acquireLocks = acquireLocks;
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        #pragma warning disable 1591

        public override void AddLeafVertex(TLeaf leafVertex)
        {
            AcquireLocksAndInvokeAction(leafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => { base.AddLeafVertex(leafVertex); }));
        }

        public override void RemoveLeafVertex(TLeaf leafVertex)
        {
            AcquireLocksAndInvokeAction(leafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => { base.RemoveLeafVertex(leafVertex); }));
        }

        public override void AddNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            AcquireLocksAndInvokeAction(nonLeafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => { base.AddNonLeafVertex(nonLeafVertex); }));
        }

        public override void RemoveNonLeafVertex(TNonLeaf nonLeafVertex)
        {
            AcquireLocksAndInvokeAction(nonLeafVerticesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => { base.RemoveNonLeafVertex(nonLeafVertex); }));
        }

        public override void AddLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            AcquireLocksAndInvokeAction(leafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => { base.AddLeafToNonLeafEdge(fromVertex, toVertex); }));
        }

        public override void RemoveLeafToNonLeafEdge(TLeaf fromVertex, TNonLeaf toVertex)
        {
            AcquireLocksAndInvokeAction(leafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => { base.RemoveLeafToNonLeafEdge(fromVertex, toVertex); }));
        }

        public override void AddNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            AcquireLocksAndInvokeAction(nonLeafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => { base.AddNonLeafToNonLeafEdge(fromVertex, toVertex); }));
        }

        public override void RemoveNonLeafToNonLeafEdge(TNonLeaf fromVertex, TNonLeaf toVertex)
        {
            AcquireLocksAndInvokeAction(nonLeafToNonLeafEdgesLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => { base.RemoveNonLeafToNonLeafEdge(fromVertex, toVertex); }));
        }

        #pragma warning restore 1591

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
