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
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.ConcurrentDirectedGraph class.
    /// </summary>
    public class ConcurrentDirectedGraphTests
    {
        private IDictionary<String, ISet<String>> mockDictionary;
        private ISet<String> mockSet;
        private MockCollectionFactory<String, String, ISet<String>> mockCollectionFactory;
        private ConcurrentDirectedGraphWithProtectedMembers<String, String> testConcurrentDirectedGraph;

        [SetUp]
        protected void SetUp()
        {
            mockDictionary = Substitute.For<IDictionary<String, ISet<String>>>();
            mockSet = Substitute.For<ISet<String>>();
            mockCollectionFactory = new MockCollectionFactory<String, String, ISet<String>>(mockDictionary, mockSet);
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, true);
        }

        [Test]
        public void LeafVertices()
        {
            // ConcurrentDirectedGraph methods which returned IEnumerable<T> we just returning the underlying ISet<T> implementation in a previous version of the class
            //   Problem was that the ISet<T> implementation was a ConcurrentHashSet<T> which left several ISet<T> methods not implemented
            //   This would usually be OK, but found cases where the IEnumerable<T> was being up-cast, and then the unimplemented methods attempting to be called and failing
            //   Was suprised that this happens in one of the constructor overloads of List<T> (https://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/clr/src/BCL/System/Collections/Generic/List@cs/2/List@cs)
            //   Hence including this test and other similar to ensure an 'uncastable' IEnumerable<T> is returned

            var testConcurrentDirectedGraph2 = new ConcurrentDirectedGraph<String, String>();
            String testPerson = "Per1";
            testConcurrentDirectedGraph2.AddLeafVertex(testPerson);

            var result = new List<String>(testConcurrentDirectedGraph2.LeafVertices);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(testPerson, result[0]);
        }

        [Test]
        public void NonLeafVertices()
        {
            var testConcurrentDirectedGraph2 = new ConcurrentDirectedGraph<String, String>();
            String testPerson = "Per1";
            testConcurrentDirectedGraph2.AddNonLeafVertex(testPerson);

            var result = new List<String>(testConcurrentDirectedGraph2.NonLeafVertices);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(testPerson, result[0]);
        }

        [Test]
        public void AddLeafVertex_LocksAreSet()
        {
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(false);
            mockSet.When(mockSet => mockSet.Add(testPerson)).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddLeafVertex(testPerson);

            mockSet.Received(1).Add(testPerson);
        }

        [Test]
        public void AddLeafVertex_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(false);
            mockSet.When(mockSet => mockSet.Add(testPerson)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddLeafVertex(testPerson);

            mockSet.Received(1).Add(testPerson);
        }

        [Test]
        public void RemoveLeafVertex_LocksAreSet()
        {
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(true);
            mockSet.When(mockSet => mockSet.Remove(testPerson)).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveLeafVertex(testPerson);

            mockSet.Received(1).Remove(testPerson);
        }

        [Test]
        public void RemoveLeafVertex_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(true);
            mockSet.When(mockSet => mockSet.Remove(testPerson)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveLeafVertex(testPerson);

            mockSet.Received(1).Remove(testPerson);
        }

        [Test]
        public void AddNonLeafVertex_LocksAreSet()
        {
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(false);
            mockSet.When(mockSet => mockSet.Add(testPerson)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddNonLeafVertex(testPerson);

            mockSet.Received(1).Add(testPerson);
        }

        [Test]
        public void AddNonLeafVertex_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(false);
            mockSet.When(mockSet => mockSet.Add(testPerson)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddNonLeafVertex(testPerson);

            mockSet.Received(1).Add(testPerson);
        }

        [Test]
        public void RemoveNonLeafVertex_LocksAreSet()
        {
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(true);
            mockSet.When(mockSet => mockSet.Remove(testPerson)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveNonLeafVertex(testPerson);

            mockSet.Received(1).Remove(testPerson);
        }

        [Test]
        public void RemoveNonLeafVertex_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testPerson = "Per1";

            mockSet.Contains(testPerson).Returns<Boolean>(true);
            mockSet.When(mockSet => mockSet.Remove(testPerson)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveNonLeafVertex(testPerson);

            mockSet.Received(1).Remove(testPerson);
        }

        [Test]
        public void AddLeafToNonLeafEdge_LocksAreSet()
        {
            String testPerson1 = "Per1";
            String testGroup1 = "Grp2";

            mockSet.Contains(testPerson1).Returns<Boolean>(true);
            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testPerson1).Returns(false);
            mockDictionary[testPerson1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Add(testGroup1)).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddLeafToNonLeafEdge(testPerson1, testGroup1);

            mockSet.Received(1).Add(testGroup1);
        }

        [Test]
        public void AddLeafToNonLeafEdge_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testPerson1 = "Per1";
            String testGroup1 = "Grp2";

            mockSet.Contains(testPerson1).Returns<Boolean>(true);
            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testPerson1).Returns(false);
            mockDictionary[testPerson1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Add(testGroup1)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddLeafToNonLeafEdge(testPerson1, testGroup1);

            mockSet.Received(1).Add(testGroup1);
        }

        [Test]
        public void GetLeafEdges()
        {
            var testConcurrentDirectedGraph2 = new ConcurrentDirectedGraph<String, String>();
            String testPerson1 = "Per1";
            String testPerson2 = "Per2";
            testConcurrentDirectedGraph2.AddLeafVertex(testPerson1);
            testConcurrentDirectedGraph2.AddNonLeafVertex(testPerson2);
            testConcurrentDirectedGraph2.AddLeafToNonLeafEdge(testPerson1, testPerson2);

            var result = new List<String>(testConcurrentDirectedGraph2.GetLeafEdges(testPerson1));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(testPerson2, result[0]);
        }

        [Test]
        public void RemoveLeafToNonLeafEdge_LocksAreSet()
        {
            String testPerson1 = "Per1";
            String testGroup1 = "Grp2";

            mockSet.Contains(testPerson1).Returns<Boolean>(true);
            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testPerson1).Returns(true);
            mockDictionary[testPerson1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Remove(testGroup1)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveLeafToNonLeafEdge(testPerson1, testGroup1);

            mockSet.Received(1).Remove(testGroup1);
        }

        [Test]
        public void RemoveLeafToNonLeafEdge_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testPerson1 = "Per1";
            String testGroup1 = "Grp2";

            mockSet.Contains(testPerson1).Returns<Boolean>(true);
            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testPerson1).Returns(true);
            mockDictionary[testPerson1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Remove(testGroup1)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveLeafToNonLeafEdge(testPerson1, testGroup1);

            mockSet.Received(1).Remove(testGroup1);
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_LocksAreSet()
        {
            String testGroup1 = "Grp1";
            String testGroup2 = "Grp2";

            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockSet.Contains(testGroup2).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testGroup1).Returns(false);
            mockDictionary[testGroup1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Add(testGroup2)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddNonLeafToNonLeafEdge(testGroup1, testGroup2);

            mockSet.Received(1).Add(testGroup2);
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testGroup1 = "Grp1";
            String testGroup2 = "Grp2";

            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockSet.Contains(testGroup2).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testGroup1).Returns(false);
            mockDictionary[testGroup1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Add(testGroup2)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.AddNonLeafToNonLeafEdge(testGroup1, testGroup2);

            mockSet.Received(1).Add(testGroup2);
        }

        [Test]
        public void GetNonLeafEdges()
        {
            var testConcurrentDirectedGraph2 = new ConcurrentDirectedGraph<String, String>();
            String testPerson1 = "Per1";
            String testPerson2 = "Per2";
            testConcurrentDirectedGraph2.AddNonLeafVertex(testPerson1);
            testConcurrentDirectedGraph2.AddNonLeafVertex(testPerson2);
            testConcurrentDirectedGraph2.AddNonLeafToNonLeafEdge(testPerson1, testPerson2);

            var result = new List<String>(testConcurrentDirectedGraph2.GetNonLeafEdges(testPerson1));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(testPerson2, result[0]);
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge_LocksAreSet()
        {
            String testGroup1 = "Grp1";
            String testGroup2 = "Grp2";

            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockSet.Contains(testGroup2).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testGroup1).Returns(true);
            mockDictionary[testGroup1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Remove(testGroup2)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveNonLeafToNonLeafEdge(testGroup1, testGroup2);

            mockSet.Received(1).Remove(testGroup2);
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge_AcquireLockFalseLocksNotSet()
        {
            testConcurrentDirectedGraph = new ConcurrentDirectedGraphWithProtectedMembers<String, String>(mockCollectionFactory, false);
            String testGroup1 = "Grp1";
            String testGroup2 = "Grp2";

            mockSet.Contains(testGroup1).Returns<Boolean>(true);
            mockSet.Contains(testGroup2).Returns<Boolean>(true);
            mockDictionary.ContainsKey(testGroup1).Returns(true);
            mockDictionary[testGroup1].Returns<ISet<String>>(mockSet);
            mockSet.When(mockSet => mockSet.Remove(testGroup2)).Do(callInfo =>
            {
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafVerticesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.LeafToNonLeafEdgesLock));
                Assert.IsFalse(Monitor.IsEntered(testConcurrentDirectedGraph.NonLeafToNonLeafEdgesLock));
            });

            testConcurrentDirectedGraph.RemoveNonLeafToNonLeafEdge(testGroup1, testGroup2);

            mockSet.Received(1).Remove(testGroup2);
        }

        #region Nested Classes

        /// <summary>
        /// Version of the ConcurrentDirectedGraph class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TLeaf">The type of leaf vertices.</typeparam>
        /// <typeparam name="TNonLeaf">The type of non-leaf vertices.</typeparam>
        protected class ConcurrentDirectedGraphWithProtectedMembers<TLeaf, TNonLeaf> : ConcurrentDirectedGraph<TLeaf, TNonLeaf>
        {
            /// <summary>Lock object for the leaf vertices collection.</summary>
            public Object LeafVerticesLock
            {
                get { return leafVerticesLock; }
            }

            /// <summary>Lock object for the non-leaf vertices collection.</summary>
            public Object NonLeafVerticesLock
            {
                get { return nonLeafVerticesLock; }
            }

            /// <summary>Lock object for the leaf to non-leaf edges collection.</summary>
            public Object LeafToNonLeafEdgesLock
            {
                get { return leafToNonLeafEdgesLock; }
            }

            /// <summary>Lock object for the non-leaf to non-leaf edges collection.</summary>
            public Object NonLeafToNonLeafEdgesLock
            {
                get { return nonLeafToNonLeafEdgesLock; }
            }

            public ConcurrentDirectedGraphWithProtectedMembers(ICollectionFactory collectionFactory, Boolean acquireLocks)
                : base(collectionFactory, acquireLocks)
            {
            }
        }

        #endregion
    }
}
