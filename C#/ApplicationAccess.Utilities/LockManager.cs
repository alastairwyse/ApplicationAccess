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
using System.Threading;

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Allows definition of dependencies between objects used for mutual-exclusion locks, and provides methods to lock these in a consistent order to prevent deadlocks.
    /// </summary>
    public class LockManager
    {
        // TODO: Allow RegisterLockObject() and RegisterLockObjects() to be called after AcquireLocksAndInvokeAction()
        //   Will need to clear the cache for that lock object, and adjust graph or 'registeredObjects' accordingly
        //   AcquireAllLocksAndInvokeAction() 
        //     Will need its own List<Object> to cache the full path (allLockObjectsLockPath?)
        //     Make sure I sort the objects in that list as is done when populating lockObjectDependencyCache
        //     Could possibly populate this by calling TraverseDependencyGraph using each item in dependsOnDepenencies (in sequence) as the start points

        /// <summary>All the lock objects registered in the manager.</summary>
        protected HashSet<Object> registeredObjects;
        /// <summary>Holds the lock order sequence number which is mapped to each registered object.</summary>
        protected Dictionary<Object, Int32> registeredObjectSequenceNumbers;
        /// <summary>The lock object dependencies, where the dictionary key object depends on the value objects.</summary>
        protected Dictionary<Object, HashSet<Object>> dependsOnDepenencies;
        /// <summary>The lock object dependencies, where the dictionary key object is depended on by the value objects.</summary>
        protected Dictionary<Object, HashSet<Object>> dependedOnByDepenencies;
        /// <summary>A sequence number used to denote the order in which a lock should be acquired on an object.</summary>
        protected Int32 nextSequenceNumber;
        /// <summary>For a given lock object and dependency pattern, caches the objects which either depend on, or are dependent on the object, in the order the the locks should be applied.</summary>
        protected Dictionary<LockObjectAndDependencyPattern, List<Object>> lockObjectDependencyCache;
        /// <summary>A list of all objects registered in the manager in the order which locks should be applied when locking all objects.</summary>
        protected List<Object> allObjectsLockOrder;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Utilities.LockManager class.
        /// </summary>
        public LockManager()
        {
            registeredObjects = new HashSet<Object>();
            dependsOnDepenencies = new Dictionary<Object, HashSet<Object>>();
            dependedOnByDepenencies = new Dictionary<Object, HashSet<Object>>();
            nextSequenceNumber = 0;
            registeredObjectSequenceNumbers = new Dictionary<Object, Int32>();
            lockObjectDependencyCache = new Dictionary<LockObjectAndDependencyPattern, List<Object>>();
            allObjectsLockOrder = null;
        }

        /// <summary>
        /// Registers an object in the lock manager.
        /// </summary>
        /// <param name="lockObject">The object to register.</param>
        public void RegisterLockObject(Object lockObject)
        {
            if (lockObject == null)
                throw new ArgumentNullException(nameof(lockObject), $"Parameter '{nameof(lockObject)}' cannot be null.");
            if (registeredObjects.Contains(lockObject) == true)
                throw new ArgumentException($"Parameter '{nameof(lockObject)}' has already been registered as a lock object.", nameof(lockObject));
            if (lockObjectDependencyCache.Count > 0 || allObjectsLockOrder != null)
                throw new InvalidOperationException("Cannot register new lock objects after the AcquireLocksAndInvokeAction() or AcquireAllLocksAndInvokeAction() methods have been called.");

            registeredObjects.Add(lockObject);
            registeredObjectSequenceNumbers.Add(lockObject, nextSequenceNumber);
            nextSequenceNumber++;
        }

        /// <summary>
        /// Registers a collection of objects in the lock manager.
        /// </summary>
        /// <param name="lockObjects">The objects to register.</param>
        public void RegisterLockObjects(IEnumerable<Object> lockObjects)
        {
            foreach (Object currentLockObject in lockObjects)
            {
                RegisterLockObject(currentLockObject);
            }
        }

        /// <summary>
        /// Registers a dependency between two objects previously registered in the lock manager.
        /// </summary>
        /// <param name="dependencyFromObject">The object that the dependency is from.</param>
        /// <param name="dependencyToObject">The object that the dependency is to.</param>
        public void RegisterLockObjectDependency(Object dependencyFromObject, Object dependencyToObject)
        {
            if (dependencyFromObject == null)
                throw new ArgumentNullException(nameof(dependencyFromObject), $"Parameter '{nameof(dependencyFromObject)}' cannot be null.");
            if (dependencyToObject == null)
                throw new ArgumentNullException(nameof(dependencyToObject), $"Parameter '{nameof(dependencyToObject)}' cannot be null.");
            if (lockObjectDependencyCache.Count > 0)
                throw new InvalidOperationException("Cannot register new lock object dependencies after the AcquireLocksAndInvokeAction() method has been called.");
            if (dependencyFromObject == dependencyToObject)
                throw new ArgumentException($"Parameters '{nameof(dependencyFromObject)}' and '{nameof(dependencyToObject)}' cannot contain the same object.", nameof(dependencyToObject));
            if (registeredObjects.Contains(dependencyFromObject) == false)
                throw new ArgumentException($"Parameter '{nameof(dependencyFromObject)}' has not been registered.", nameof(dependencyFromObject));
            if (registeredObjects.Contains(dependencyToObject) == false)
                throw new ArgumentException($"Parameter '{nameof(dependencyToObject)}' has not been registered.", nameof(dependencyToObject));
            if (dependsOnDepenencies.ContainsKey(dependencyFromObject) == true && dependsOnDepenencies[dependencyFromObject].Contains(dependencyToObject) == true)
                throw new ArgumentException($"A dependency already exists from object in parameter '{nameof(dependencyFromObject)}' to object in parameter '{nameof(dependencyToObject)}'.", nameof(dependencyToObject));
            Action<Object> circularReferenceCheckAction = (Object currentLockObject) =>
            {
                if (currentLockObject == dependencyFromObject)
                    throw new ArgumentException($"A dependency between the objects in the specified parameters cannot be created as it would cause a circular reference.", nameof(dependencyToObject));
            };
            TraverseDependencyGraph(dependencyToObject, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new HashSet<Object>(), circularReferenceCheckAction);

            if (dependsOnDepenencies.ContainsKey(dependencyFromObject) == false)
            {
                dependsOnDepenencies.Add(dependencyFromObject, new HashSet<Object>());
            }
            dependsOnDepenencies[dependencyFromObject].Add(dependencyToObject);
            if (dependedOnByDepenencies.ContainsKey(dependencyToObject) == false)
            {
                dependedOnByDepenencies.Add(dependencyToObject, new HashSet<Object>());
            }
            dependedOnByDepenencies[dependencyToObject].Add(dependencyFromObject);
        }

        /// <summary>
        /// Acquires locks on the specified object and objects which it's associated with (either all objects which it depends on, or which depend on it), the invokes the specified action.
        /// </summary>
        /// <param name="lockObject">The object to lock.</param>
        /// <param name="lockObjectDependencyPattern">Whether to additionally lock all objects on which parameter 'lockObject' depends, or all objects which depend on parameter 'lockObject'.</param>
        /// <param name="action">The action to invoke.</param>
        public void AcquireLocksAndInvokeAction(Object lockObject, LockObjectDependencyPattern lockObjectDependencyPattern, Action action)
        {
            ThrowExceptionIsLockObjectParameterNotRegistered(lockObject, nameof(lockObject));

            // Get the set of objects to acquire locks on
            List<Object> lockObjects = null;
            var cacheKey = new LockObjectAndDependencyPattern(lockObject, lockObjectDependencyPattern);
            if (lockObjectDependencyCache.ContainsKey(cacheKey) == true)
            {
                lockObjects = lockObjectDependencyCache[cacheKey];
            }
            else
            {
                lock(lockObjectDependencyCache)
                {
                    if (lockObjectDependencyCache.ContainsKey(cacheKey) == false)
                    {
                        // Traverse the dependency graph to get all the associated objects
                        var lockObjectsWithLockSequence = new List<LockObjectAndSequenceNumber>();
                        var addObjectToLockObjectsGraphTraverseAction = new Action<Object>((Object currentObject) =>
                        {
                            var lockObjectAndSequenceNumber = new LockObjectAndSequenceNumber(currentObject, registeredObjectSequenceNumbers[currentObject]);
                            lockObjectsWithLockSequence.Add(lockObjectAndSequenceNumber);
                        });
                        TraverseDependencyGraph(lockObject, lockObjectDependencyPattern, new HashSet<Object>(), addObjectToLockObjectsGraphTraverseAction);
                        // Sort the objects by sequence number
                        lockObjectsWithLockSequence.Sort();
                        lockObjects = new List<Object>();
                        foreach (LockObjectAndSequenceNumber currentLockObjectAndSequence in lockObjectsWithLockSequence)
                        {
                            lockObjects.Add(currentLockObjectAndSequence.LockObject);
                        }
                        // Add the lock objects to the cache
                        lockObjectDependencyCache.Add(cacheKey, lockObjects);
                    }
                    else
                    {
                        lockObjects = lockObjectDependencyCache[cacheKey];
                    }
                }
            }

            // Acquire locks and invoke the action
            AcquireLocksAndInvokeAction(lockObjects, 0, action);
        }

        /// <summary>
        /// Acquires locks on the specified object and objects which it's associated with (either all objects which it depends on, or which depend on it), the invokes the specified action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        public void AcquireAllLocksAndInvokeAction(Action action)
        {
            if (registeredObjectSequenceNumbers.Count == 0)
                throw new InvalidOperationException("No objects have been registered.");

            // Populate member 'allObjectsLockOrder' if the method is being called for the first time
            if (allObjectsLockOrder == null)
            {
                lock (lockObjectDependencyCache)
                {
                    if (allObjectsLockOrder == null)
                    {
                        // Find the highest sequence number in registeredObjectSequenceNumbers, and store them in a Dictionary keyed by sequence number
                        Int32 maxSequenceNumber = -1;
                        var sequenceNumberToLockObjectMap = new Dictionary<Int32, Object>();
                        foreach (KeyValuePair<Object, Int32> currentKvp in registeredObjectSequenceNumbers)
                        {
                            sequenceNumberToLockObjectMap.Add(currentKvp.Value, currentKvp.Key);
                            if (currentKvp.Value > maxSequenceNumber)
                            {
                                maxSequenceNumber = currentKvp.Value;
                            }
                        }
                        // Populate 'allObjectsLockOrder' with lock objects in order of sequence number
                        allObjectsLockOrder = new List<Object>();
                        for (Int32 currentSequenceNumber = 0; currentSequenceNumber <= maxSequenceNumber; currentSequenceNumber++)
                        {
                            allObjectsLockOrder.Add(sequenceNumberToLockObjectMap[currentSequenceNumber]);
                        }
                    }
                }
            }

            // Acquire locks and invoke the action
            AcquireLocksAndInvokeAction(allObjectsLockOrder, 0, action);
        }

        /// <summary>
        /// Returns true if the specified lock object is locked by the current thread.
        /// </summary>
        /// <param name="lockObject">The object to check.</param>
        /// <returns>True if the object is locked by the current thread.  Otherwise, false.</returns>
        public Boolean LockObjectIsLockedByCurrentThread(Object lockObject)
        {
            ThrowExceptionIsLockObjectParameterNotRegistered(lockObject, nameof(lockObject));

            return Monitor.IsEntered(lockObject);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Recursively acquires locks on the specified objects, the invokes the specified action.
        /// </summary>
        /// <param name="lockObjects">The sequence of objects to acquire locks on.</param>
        /// <param name="nextObjectIndex">The index of the object in the sequence to acquire a lock on next, or equal to the length of parameter 'lockObjects' if all locks have been acquired.</param>
        /// <param name="action">The action to invoke once all locks are acquired.</param>
        protected void AcquireLocksAndInvokeAction(List<Object> lockObjects, Int32 nextObjectIndex, Action action)
        {
            if (nextObjectIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(nextObjectIndex), $"Parameter '{nameof(nextObjectIndex)}' much be greater than or equal to 0.");
            if (nextObjectIndex > lockObjects.Count)
                throw new ArgumentOutOfRangeException(nameof(nextObjectIndex), $"Parameter '{nameof(nextObjectIndex)}' much be less than or equal to the number of elements in parameter '{nameof(lockObjects)}'.");

            if (nextObjectIndex == lockObjects.Count)
            {
                action.Invoke();
            }
            else
            {
                lock(lockObjects[nextObjectIndex])
                {
                    AcquireLocksAndInvokeAction(lockObjects, nextObjectIndex + 1, action);
                }
            }
        }

        /// <summary>
        /// Traverses the graph of object depedencies, invoking the specified action on each object encountered.
        /// </summary>
        /// <param name="nextObject">The next object to traverse to.</param>
        /// <param name="lockObjectDependencyPattern">The 'direction' to traverse the graph, either to objects which 'nextObject' depends on, or objects which depend on it.</param>
        /// <param name="visitedObjects">Objects which have already been visited as part of the traversal.</param>
        /// <param name="objectAction">The action to perform on the object.  Accepts a single parameter which is the object to perform the action on.</param>
        protected void TraverseDependencyGraph(Object nextObject, LockObjectDependencyPattern lockObjectDependencyPattern, HashSet<Object> visitedObjects, Action<Object> objectAction)
        {
            objectAction.Invoke(nextObject);
            visitedObjects.Add(nextObject);
            if (lockObjectDependencyPattern == LockObjectDependencyPattern.ObjectAndObjectsItDependsOn)
            {
                if (dependsOnDepenencies.ContainsKey(nextObject) == true)
                {
                    foreach (Object currentDependsOnObject in dependsOnDepenencies[nextObject])
                    {
                        if (visitedObjects.Contains(currentDependsOnObject) == false)
                        {
                            TraverseDependencyGraph(currentDependsOnObject, lockObjectDependencyPattern, visitedObjects, objectAction);
                        }
                    }
                }
            }
            else if (lockObjectDependencyPattern == LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt)
            {
                if (dependedOnByDepenencies.ContainsKey(nextObject) == true)
                {
                    foreach (Object currentDependsOnObject in dependedOnByDepenencies[nextObject])
                    {
                        if (visitedObjects.Contains(currentDependsOnObject) == false)
                        {
                            TraverseDependencyGraph(currentDependsOnObject, lockObjectDependencyPattern, visitedObjects, objectAction);
                        }
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Parameter '{nameof(lockObjectDependencyPattern)}' contains unhandled {nameof(LockObjectDependencyPattern)} '{lockObjectDependencyPattern}'.");
            }
        }

        #pragma warning disable 1591

        protected void ThrowExceptionIsLockObjectParameterNotRegistered(Object lockObject, String lockObjectParameterName)
        {
            if (registeredObjects.Contains(lockObject) == false)
                throw new ArgumentException($"Object in parameter '{lockObjectParameterName}' has not been registered.", lockObjectParameterName);
        }

        #pragma warning restore 1591

        #endregion

        #region Nested Classes

        #pragma warning disable 1591

        /// <summary>
        /// Container class which holds a lock object and a sequence number used to define the order which a lock is acquired on the object.
        /// </summary>
        protected class LockObjectAndSequenceNumber : IComparable<LockObjectAndSequenceNumber>
        {
            protected Object lockObject;
            protected Int32 sequenceNumber;

            /// <summary>
            /// The object a lock is acquired on.
            /// </summary>
            public Object LockObject
            {
                get { return lockObject; }
            }

            /// <summary>
            /// The order in which a lock should be acquired on the object as compared to other objects.
            /// </summary>
            public Int32 SequenceNumber
            {
                get { return sequenceNumber; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Utilities.LockManager+LockObjectAndSequenceNumber class.
            /// </summary>
            /// <param name="lockObject">The object a lock is acquired on.</param>
            /// <param name="sequenceNumber">The order in which a lock should be acquired on the object as compared to other objects.</param>
            public LockObjectAndSequenceNumber(Object lockObject, Int32 sequenceNumber)
            {
                this.lockObject = lockObject;
                this.sequenceNumber = sequenceNumber;
            }

            public int CompareTo(LockObjectAndSequenceNumber other)
            {
                return this.sequenceNumber.CompareTo(other.sequenceNumber);
            }
        }

        /// <summary>
        /// Container class which holds a lock object and a dependency pattern for the lock object.
        /// </summary>
        protected class LockObjectAndDependencyPattern : IEquatable<LockObjectAndDependencyPattern>
        {
            protected const Int32 prime1 = 7;
            protected const Int32 prime2 = 11;

            protected Object lockObject;
            protected LockObjectDependencyPattern dependencyPattern;

            /// <summary>
            /// The object a lock is acquired on.
            /// </summary>
            public Object LockObject
            {
                get { return lockObject; }
            }

            /// <summary>
            /// The lock object dependency pattern.
            /// </summary>
            public LockObjectDependencyPattern DependencyPattern
            {
                get { return dependencyPattern; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Utilities.LockManager+LockObjectAndDependencyPattern class.
            /// </summary>
            /// <param name="lockObject">The lock object dependency pattern.</param>
            /// <param name="dependencyPattern">The lock object dependency pattern.</param>
            public LockObjectAndDependencyPattern(Object lockObject, LockObjectDependencyPattern dependencyPattern)
            {
                this.lockObject = lockObject;
                this.dependencyPattern = dependencyPattern;
            }

            public bool Equals(LockObjectAndDependencyPattern other)
            {
                return (this.lockObject == other.lockObject && this.dependencyPattern == other.dependencyPattern);
            }

            public override Int32 GetHashCode()
            {
                return (lockObject.GetHashCode() * prime1 + dependencyPattern.GetHashCode() * 11);
            }
        }

        #pragma warning restore 1591

        #endregion
    }
}
