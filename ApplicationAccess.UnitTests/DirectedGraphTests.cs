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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.DirectedGraph class.
    /// </summary>
    public class DirectedGraphTests
    {
        private DirectedGraphWithProtectedMembers<String, String> testDirectedGraph;

        [SetUp]
        protected void SetUp()
        {
            testDirectedGraph = new DirectedGraphWithProtectedMembers<String, String>();
        }

        [Test]
        public void LeafVertices()
        {
            var allLeafVertices = new HashSet<String>(testDirectedGraph.LeafVertices);

            Assert.AreEqual(0, allLeafVertices.Count);


            CreatePersonGroupGraph(testDirectedGraph);

            allLeafVertices = new HashSet<String>(testDirectedGraph.LeafVertices);

            Assert.AreEqual(7, allLeafVertices.Count);
            Assert.IsTrue(allLeafVertices.Contains("Per1"));
            Assert.IsTrue(allLeafVertices.Contains("Per2"));
            Assert.IsTrue(allLeafVertices.Contains("Per3"));
            Assert.IsTrue(allLeafVertices.Contains("Per4"));
            Assert.IsTrue(allLeafVertices.Contains("Per5"));
            Assert.IsTrue(allLeafVertices.Contains("Per6"));
            Assert.IsTrue(allLeafVertices.Contains("Per7"));
        }

        [Test]
        public void NonLeafVertices()
        {
            var allNonLeafVertices = new HashSet<String>(testDirectedGraph.NonLeafVertices);

            Assert.AreEqual(0, allNonLeafVertices.Count);


            CreatePersonGroupGraph(testDirectedGraph);

            allNonLeafVertices = new HashSet<String>(testDirectedGraph.NonLeafVertices);

            Assert.AreEqual(4, allNonLeafVertices.Count);
            Assert.IsTrue(allNonLeafVertices.Contains("Grp1"));
            Assert.IsTrue(allNonLeafVertices.Contains("Grp2"));
            Assert.IsTrue(allNonLeafVertices.Contains("Grp3"));
            Assert.IsTrue(allNonLeafVertices.Contains("Grp4"));
        }

        [Test]
        public void Clear()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            Assert.AreNotEqual(0, testDirectedGraph.LeafVertices.Count());
            Assert.AreNotEqual(0, testDirectedGraph.NonLeafVertices.Count());
            Assert.AreNotEqual(0, testDirectedGraph.LeafToNonLeafReverseEdges.Count());
            Assert.AreNotEqual(0, testDirectedGraph.NonLeafToNonLeafReverseEdges.Count());

            testDirectedGraph.Clear();

            Assert.AreEqual(0, testDirectedGraph.LeafVertices.Count());
            Assert.AreEqual(0, testDirectedGraph.NonLeafVertices.Count());
            Assert.AreEqual(0, testDirectedGraph.LeafToNonLeafReverseEdges.Count());
            Assert.AreEqual(0, testDirectedGraph.NonLeafToNonLeafReverseEdges.Count());
        }

        [Test]
        public void AddLeafVertex_LeafVertexAlreadyExists()
        {
            testDirectedGraph.AddLeafVertex("abc");

            var e = Assert.Throws<LeafVertexAlreadyExistsException<String>>(delegate
            {
                testDirectedGraph.AddLeafVertex("abc");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'abc' already exists in the graph."));
            Assert.AreEqual("abc", e.LeafVertex);
        }

        [Test]
        public void ContainsLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);

            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per1"));
            Assert.IsFalse(testDirectedGraph.ContainsLeafVertex("Per8"));
        }

        [Test]
        public void RemoveLeafVertex_VertexDoesntExist()
        {
            var e = Assert.Throws<LeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.RemoveLeafVertex("abc");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'abc' does not exist in the graph."));
            Assert.AreEqual("abc", e.LeafVertex);
        }

        [Test]
        public void RemoveLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);

            testDirectedGraph.RemoveLeafVertex("Per1");

            Assert.IsFalse(testDirectedGraph.LeafVertices.Contains("Per1"));
            Assert.IsFalse(testDirectedGraph.LeafToNonLeafEdges.ContainsKey("Per1"));


            testDirectedGraph.Clear();
            CreatePersonGroupGraph(testDirectedGraph);

            testDirectedGraph.RemoveLeafVertex("Per3");

            Assert.IsFalse(testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Contains("Per3"));
            Assert.IsFalse(testDirectedGraph.LeafToNonLeafReverseEdges["Grp2"].Contains("Per3"));
            Assert.AreEqual(2, testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Count);
            Assert.AreEqual(3, testDirectedGraph.LeafToNonLeafReverseEdges["Grp2"].Count);
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Contains("Per1"));
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Contains("Per2"));
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["Grp2"].Contains("Per4"));
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["Grp2"].Contains("Per5"));
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["Grp2"].Contains("Per6"));
        }

        [Test]
        public void AddNonLeafVertex_NonLeafAlreadyExists()
        {
            testDirectedGraph.AddNonLeafVertex("abc");

            var e = Assert.Throws<NonLeafVertexAlreadyExistsException<String>>(delegate
            {
                testDirectedGraph.AddNonLeafVertex("abc");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'abc' already exists in the graph."));
            Assert.AreEqual("abc", e.NonLeafVertex);
        }

        [Test]
        public void ContainsNonLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);

            Assert.IsTrue(testDirectedGraph.ContainsNonLeafVertex("Grp1"));
            Assert.IsFalse(testDirectedGraph.ContainsNonLeafVertex("Grp5"));
        }

        [Test]
        public void RemoveNonLeafVertex_VertexDoesntExist()
        {
            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.RemoveNonLeafVertex("abc");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'abc' does not exist in the graph."));
            Assert.AreEqual("abc", e.NonLeafVertex);
        }

        [Test]
        public void RemoveNonLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            var visitedLeaves = new HashSet<String>();
            var visitedNonLeaves = new HashSet<String>();
            var leafVertexAction = new Func<String, Boolean>((String currentLeafVertex) =>
            {
                visitedLeaves.Add(currentLeafVertex);

                return true;
            });
            var nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                visitedNonLeaves.Add(currentNonLeafVertex);

                return true;
            });

            testDirectedGraph.RemoveNonLeafVertex("Grp2");

            Assert.AreEqual(7, testDirectedGraph.LeafVertices.Count());
            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per1"));
            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per2"));
            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per3"));
            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per4"));
            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per5"));
            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per6"));
            Assert.IsTrue(testDirectedGraph.ContainsLeafVertex("Per7"));
            Assert.AreEqual(3, testDirectedGraph.NonLeafVertices.Count());
            Assert.IsTrue(testDirectedGraph.ContainsNonLeafVertex("Grp1"));
            Assert.IsTrue(testDirectedGraph.ContainsNonLeafVertex("Grp4"));
            Assert.IsTrue(testDirectedGraph.ContainsNonLeafVertex("Grp3"));
            Assert.IsFalse(testDirectedGraph.ContainsNonLeafVertex("Grp2"));
            testDirectedGraph.TraverseGraph(leafVertexAction, nonLeafVertexAction);
            Assert.IsFalse(visitedNonLeaves.Contains("Grp2"));
            Assert.IsFalse(testDirectedGraph.NonLeafToNonLeafEdges.ContainsKey("Grp2"));
            Assert.IsFalse(testDirectedGraph.LeafToNonLeafReverseEdges.ContainsKey("Grp2"));
            Assert.IsFalse(testDirectedGraph.NonLeafToNonLeafReverseEdges["Grp3"].Contains("Grp2"));
            Assert.AreEqual(1, testDirectedGraph.NonLeafToNonLeafReverseEdges["Grp3"].Count);
            Assert.IsTrue(testDirectedGraph.NonLeafToNonLeafReverseEdges["Grp3"].Contains("Grp1"));
        }

        [Test]
        public void AddLeafToNonLeafEdge_EdgeAlreadyExists()
        {
            testDirectedGraph.AddLeafVertex("child");
            testDirectedGraph.AddNonLeafVertex("parent");
            testDirectedGraph.AddLeafToNonLeafEdge("child", "parent");

            var e = Assert.Throws<LeafToNonLeafEdgeAlreadyExistsException<String, String>>(delegate
            {
                testDirectedGraph.AddLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("An edge already exists between vertices 'child' and 'parent'."));
            Assert.AreEqual("child", e.FromVertex);
            Assert.AreEqual("parent", e.ToVertex);
        }

        [Test]
        public void AddLeafToNonLeafEdge_FromVertexDoesntExist()
        {
            testDirectedGraph.AddNonLeafVertex("parent");

            var e = Assert.Throws<LeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.AddLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'child' does not exist in the graph."));
            Assert.AreEqual("child", e.LeafVertex);
        }

        [Test]
        public void AddLeafToNonLeafEdge_ToVertexDoesntExist()
        {
            testDirectedGraph.AddLeafVertex("child");

            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.AddLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent' does not exist in the graph."));
            Assert.AreEqual("parent", e.NonLeafVertex);
        }

        [Test]
        public void AddLeafToNonLeafEdge()
        {
            testDirectedGraph.AddLeafVertex("child");
            testDirectedGraph.AddNonLeafVertex("parent");

            testDirectedGraph.AddLeafToNonLeafEdge("child", "parent");

            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges.ContainsKey("parent"));
            Assert.AreEqual(1, testDirectedGraph.LeafToNonLeafReverseEdges["parent"].Count);
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["parent"].Contains("child"));
        }

        [Test]
        public void GetLeafEdges_VertexDoesntExist()
        {
            var e = Assert.Throws<LeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.GetLeafEdges("child").FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'child' does not exist in the graph."));
            Assert.AreEqual("child", e.LeafVertex);
        }

        [Test]
        public void GetLeafEdges()
        {
            CreatePersonGroupGraph(testDirectedGraph);

            var leafEdges = new HashSet<String>(testDirectedGraph.GetLeafEdges("Per3"));

            Assert.AreEqual(2, leafEdges.Count);
            Assert.IsTrue(leafEdges.Contains("Grp1"));
            Assert.IsTrue(leafEdges.Contains("Grp2"));


            testDirectedGraph.AddLeafVertex("Per8");

            leafEdges = new HashSet<String>(testDirectedGraph.GetLeafEdges("Per8"));

            Assert.AreEqual(0, leafEdges.Count);
        }

        [Test]
        public void GetLeafReverseEdges_VertexDoesntExist()
        {
            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.GetLeafReverseEdges("parent").FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent' does not exist in the graph."));
            Assert.AreEqual("parent", e.NonLeafVertex);
        }

        [Test]
        public void GetLeafReverseEdges()
        {
            CreatePersonGroupGraph2(testDirectedGraph);

            var leafEdges = new HashSet<String>(testDirectedGraph.GetLeafReverseEdges("Grp5"));

            Assert.AreEqual(2, leafEdges.Count);
            Assert.IsTrue(leafEdges.Contains("Per8"));
            Assert.IsTrue(leafEdges.Contains("Per9")); 
            
            
            leafEdges = new HashSet<String>(testDirectedGraph.GetLeafReverseEdges("Grp10"));

            Assert.AreEqual(1, leafEdges.Count);
            Assert.IsTrue(leafEdges.Contains("Per10"));
        }

        [Test]
        public void RemoveLeafToNonLeafEdge_FromVertexDoesntExist()
        {
            testDirectedGraph.AddNonLeafVertex("parent");

            var e = Assert.Throws<LeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.RemoveLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'child' does not exist in the graph."));
            Assert.AreEqual("child", e.LeafVertex);
        }

        [Test]
        public void RemoveLeafToNonLeafEdge_ToVertexDoesntExist()
        {
            testDirectedGraph.AddLeafVertex("child");

            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.RemoveLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent' does not exist in the graph."));
            Assert.AreEqual("parent", e.NonLeafVertex);
        }

        [Test]
        public void RemoveLeafToNonLeafEdge_EdgeDoesntExist()
        {
            testDirectedGraph.AddLeafVertex("child");
            testDirectedGraph.AddNonLeafVertex("parent");

            var e = Assert.Throws<LeafToNonLeafEdgeNotFoundException<String, String>>(delegate
            {
                testDirectedGraph.RemoveLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("An edge does not exist between vertices 'child' and 'parent'."));
            Assert.AreEqual("child", e.FromVertex);
            Assert.AreEqual("parent", e.ToVertex);
        }

        [Test]
        public void RemoveLeafToNonLeafEdge()
        {
            CreatePersonGroupGraph(testDirectedGraph);

            var visitedNonLeaves = new List<String>();
            var vertexAction = new Func<String, Boolean>((String currentVertex) =>
            {
                visitedNonLeaves.Add(currentVertex);

                return true;
            });

            testDirectedGraph.RemoveLeafToNonLeafEdge("Per1", "Grp1");

            testDirectedGraph.TraverseFromLeaf("Per1", vertexAction);
            Assert.AreEqual(0, visitedNonLeaves.Count);
            Assert.IsFalse(testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Contains("Per1"));
            Assert.AreEqual(2, testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Count);
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Contains("Per2"));
            Assert.IsTrue(testDirectedGraph.LeafToNonLeafReverseEdges["Grp1"].Contains("Per3"));
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_ToVertexDoesntExist()
        {
            testDirectedGraph.AddNonLeafVertex("parent1");

            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.AddNonLeafToNonLeafEdge("parent1", "parent2");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent2' does not exist in the graph."));
            Assert.AreEqual("parent2", e.NonLeafVertex);
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_FromVertexDoesntExist()
        {
            testDirectedGraph.AddNonLeafVertex("parent2");

            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.AddNonLeafToNonLeafEdge("parent1", "parent2");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent1' does not exist in the graph."));
            Assert.AreEqual("parent1", e.NonLeafVertex);
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_EdgeAlreadyExists()
        {
            testDirectedGraph.AddNonLeafVertex("parent1");
            testDirectedGraph.AddNonLeafVertex("parent2");
            testDirectedGraph.AddNonLeafToNonLeafEdge("parent1", "parent2");

            var e = Assert.Throws<NonLeafToNonLeafEdgeAlreadyExistsException<String>>(delegate
            {
                testDirectedGraph.AddNonLeafToNonLeafEdge("parent1", "parent2");
            });

            Assert.That(e.Message, Does.StartWith("An edge already exists between vertices 'parent1' and 'parent2'."));
            Assert.AreEqual("parent1", e.FromVertex);
            Assert.AreEqual("parent2", e.ToVertex);
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_ToAndFromVerticesAreSame()
        {
            testDirectedGraph.AddNonLeafVertex("parent1");
            testDirectedGraph.AddNonLeafVertex("parent2");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraph.AddNonLeafToNonLeafEdge("parent2", "parent2");
            });

            Assert.That(e.Message, Does.StartWith("Parameters 'fromVertex' and 'toVertex' cannot contain the same vertex."));
            Assert.AreEqual("toVertex", e.ParamName);
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_AddingCreatesCircularReference()
        {
            testDirectedGraph.AddNonLeafVertex("parent1");
            testDirectedGraph.AddNonLeafVertex("parent2");
            testDirectedGraph.AddNonLeafVertex("parent3");
            testDirectedGraph.AddNonLeafVertex("parent4");
            testDirectedGraph.AddNonLeafToNonLeafEdge("parent1", "parent2");
            testDirectedGraph.AddNonLeafToNonLeafEdge("parent2", "parent3");
            testDirectedGraph.AddNonLeafToNonLeafEdge("parent3", "parent4");

            var e = Assert.Throws<CircularReferenceException>(delegate
            {
                testDirectedGraph.AddNonLeafToNonLeafEdge("parent3", "parent1");
            });

            Assert.That(e.Message, Does.StartWith("An edge between vertices 'parent3' and 'parent1' cannot be created as it would cause a circular reference."));


            e = Assert.Throws<CircularReferenceException>(delegate
            {
                testDirectedGraph.AddNonLeafToNonLeafEdge("parent4", "parent2");
            });

            Assert.That(e.Message, Does.StartWith("An edge between vertices 'parent4' and 'parent2' cannot be created as it would cause a circular reference."));


            e = Assert.Throws<CircularReferenceException>(delegate
            {
                testDirectedGraph.AddNonLeafToNonLeafEdge("parent3", "parent2");
            });

            Assert.That(e.Message, Does.StartWith("An edge between vertices 'parent3' and 'parent2' cannot be created as it would cause a circular reference."));
        }

        [Test]
        public void AddNonLeafToNonLeafEdge()
        {
            testDirectedGraph.AddNonLeafVertex("parent1");
            testDirectedGraph.AddNonLeafVertex("parent2");

            testDirectedGraph.AddNonLeafToNonLeafEdge("parent1", "parent2");

            Assert.IsFalse(testDirectedGraph.NonLeafToNonLeafReverseEdges.ContainsKey("parent1"));
            Assert.IsTrue(testDirectedGraph.NonLeafToNonLeafReverseEdges.ContainsKey("parent2"));
            Assert.AreEqual(1, testDirectedGraph.NonLeafToNonLeafReverseEdges["parent2"].Count);
            Assert.IsTrue(testDirectedGraph.NonLeafToNonLeafReverseEdges["parent2"].Contains("parent1"));
        }

        [Test]
        public void GetNonLeafEdges_VertexDoesntExist()
        {
            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.GetNonLeafEdges("parent1").FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent1' does not exist in the graph."));
            Assert.AreEqual("parent1", e.NonLeafVertex);
        }

        [Test]
        public void GetNonLeafEdges()
        {
            CreatePersonGroupGraph(testDirectedGraph);

            var nonLeafEdges = new HashSet<String>(testDirectedGraph.GetNonLeafEdges("Grp1"));

            Assert.AreEqual(2, nonLeafEdges.Count);
            Assert.IsTrue(nonLeafEdges.Contains("Grp4"));
            Assert.IsTrue(nonLeafEdges.Contains("Grp3"));


            nonLeafEdges = new HashSet<String>(testDirectedGraph.GetNonLeafEdges("Grp3"));

            Assert.AreEqual(0, nonLeafEdges.Count);
        }

        [Test]
        public void GetNonLeafReverseEdges_VertexDoesntExist()
        {
            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.GetNonLeafReverseEdges("parent1").FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent1' does not exist in the graph."));
            Assert.AreEqual("parent1", e.NonLeafVertex);
        }

        [Test]
        public void GetNonLeafReverseEdges()
        {
            CreatePersonGroupGraph2(testDirectedGraph);

            var nonLeafEdges = new HashSet<String>(testDirectedGraph.GetNonLeafReverseEdges("Grp10"));

            Assert.AreEqual(2, nonLeafEdges.Count);
            Assert.IsTrue(nonLeafEdges.Contains("Grp8"));
            Assert.IsTrue(nonLeafEdges.Contains("Grp9"));


            nonLeafEdges = new HashSet<String>(testDirectedGraph.GetNonLeafReverseEdges("Grp8"));

            Assert.AreEqual(2, nonLeafEdges.Count);
            Assert.IsTrue(nonLeafEdges.Contains("Grp5"));
            Assert.IsTrue(nonLeafEdges.Contains("Grp6"));


            nonLeafEdges = new HashSet<String>(testDirectedGraph.GetNonLeafReverseEdges("Grp7"));

            Assert.AreEqual(0, nonLeafEdges.Count);
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge_FromVertexDoesntExist()
        {
            testDirectedGraph.AddNonLeafVertex("parent");

            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.RemoveNonLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'child' does not exist in the graph."));
            Assert.AreEqual("child", e.NonLeafVertex);
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge_ToVertexDoesntExist()
        {
            testDirectedGraph.AddNonLeafVertex("child");

            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.RemoveNonLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent' does not exist in the graph."));
            Assert.AreEqual("parent", e.NonLeafVertex);
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge_EdgeDoesntExist()
        {
            testDirectedGraph.AddNonLeafVertex("child");
            testDirectedGraph.AddNonLeafVertex("parent");

            var e = Assert.Throws<NonLeafToNonLeafEdgeNotFoundException<String>>(delegate
            {
                testDirectedGraph.RemoveNonLeafToNonLeafEdge("child", "parent");
            });

            Assert.That(e.Message, Does.StartWith("An edge does not exist between vertices 'child' and 'parent'."));
            Assert.AreEqual("child", e.FromVertex);
            Assert.AreEqual("parent", e.ToVertex);
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge()
        {
            CreatePersonGroupGraph(testDirectedGraph);

            var visitedNonLeaves = new List<String>();
            var vertexAction = new Func<String, Boolean>((String currentVertex) =>
            {
                visitedNonLeaves.Add(currentVertex);

                return true;
            });

            testDirectedGraph.RemoveNonLeafToNonLeafEdge("Grp2", "Grp3");

            testDirectedGraph.TraverseFromNonLeaf("Grp2", vertexAction);
            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.AreEqual("Grp2", visitedNonLeaves[0]);
            Assert.IsTrue(testDirectedGraph.NonLeafToNonLeafReverseEdges.ContainsKey("Grp3"));
            Assert.AreEqual(1, testDirectedGraph.NonLeafToNonLeafReverseEdges["Grp3"].Count);
            Assert.IsTrue(testDirectedGraph.NonLeafToNonLeafReverseEdges["Grp3"].Contains("Grp1"));
        }

        [Test]
        public void TraverseFromLeaf_StartVertexDoesntExist()
        {
            var e = Assert.Throws<LeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.TraverseFromLeaf("child1", (currentVertex) => { return true; });
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'child1' does not exist in the graph."));
            Assert.AreEqual("child1", e.LeafVertex);
        }

        [Test]
        public void TraverseFromLeaf()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            var visitedNonLeaves = new HashSet<String>();
            var vertexAction = new Func<String, Boolean>((String currentVertex) =>
            {
                visitedNonLeaves.Add(currentVertex);

                return true;
            });

            testDirectedGraph.TraverseFromLeaf("Per1", vertexAction);

            Assert.AreEqual(3, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp4"));


            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseFromLeaf("Per3", vertexAction);

            Assert.AreEqual(4, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp2"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp4"));


            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseFromLeaf("Per7", vertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));
        }

        [Test]
        public void TraverseFromLeaf_StopTraversingAtSpecifiedVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            var visitedNonLeaves = new HashSet<String>();
            var vertexAction = new Func<String, Boolean>((String currentVertex) =>
            {
                visitedNonLeaves.Add(currentVertex);

                if (currentVertex == "Grp1")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });

            testDirectedGraph.TraverseFromLeaf("Per3", vertexAction);

            Assert.GreaterOrEqual(visitedNonLeaves.Count, 1);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.IsFalse(visitedNonLeaves.Contains("Grp4"));
        }

        [Test]
        public void TraverseFromNonLeaf_StartVertexDoesntExist()
        {
            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.TraverseFromNonLeaf("parent1", (currentVertex) => { return true; });
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent1' does not exist in the graph."));
            Assert.AreEqual("parent1", e.NonLeafVertex);
        }

        [Test]
        public void TraverseFromNonLeaf()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            var visitedNonLeaves = new HashSet<String>();
            var vertexAction = new Func<String, Boolean>((String currentVertex) =>
            {
                visitedNonLeaves.Add(currentVertex);

                return true;
            });

            testDirectedGraph.TraverseFromNonLeaf("Grp1", vertexAction);

            Assert.AreEqual(3, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp4"));


            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseFromNonLeaf("Grp2", vertexAction);

            Assert.AreEqual(2, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp2"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));


            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseFromNonLeaf("Grp3", vertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));
        }

        [Test]
        public void TraverseFromNonLeaf_StopTraversingAtSpecifiedVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            var visitedNonLeaves = new HashSet<String>();
            var vertexAction = new Func<String, Boolean>((String currentVertex) =>
            {
                visitedNonLeaves.Add(currentVertex);

                if (currentVertex == "Grp4")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });

            testDirectedGraph.TraverseFromNonLeaf("Grp1", vertexAction);

            Assert.GreaterOrEqual(visitedNonLeaves.Count, 2);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp4"));


            visitedNonLeaves.Clear();
            vertexAction = new Func<String, Boolean>((String currentVertex) =>
            {
                visitedNonLeaves.Add(currentVertex);

                if (currentVertex == "Grp1")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });

            testDirectedGraph.TraverseFromNonLeaf("Grp1", vertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
        }

        [Test]
        public void TraverseReverseFromNonLeaf_StartVertexDoesntExist()
        {
            var e = Assert.Throws<NonLeafVertexNotFoundException<String>>(delegate
            {
                testDirectedGraph.TraverseReverseFromNonLeaf("parent1", (currentVertex) => { return true; }, (currentVertex) => { return true; });
            });

            Assert.That(e.Message, Does.StartWith("Vertex 'parent1' does not exist in the graph."));
            Assert.AreEqual("parent1", e.NonLeafVertex);
        }

        [Test]
        public void TraverseReverseFromNonLeaf()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            CreatePersonGroupGraph2(testDirectedGraph);
            var visitedLeaves = new HashSet<String>();
            var visitedNonLeaves = new HashSet<String>();
            var leafVertexAction = new Func<String, Boolean>((String currentLeafVertex) =>
            {
                Boolean addResult = visitedLeaves.Add(currentLeafVertex);
                if (addResult == false)
                {
                    Assert.Fail($"Attempted to traverse to leaf vertex '{currentLeafVertex}' twice.");
                }

                return true;
            });
            var nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                Boolean addResult = visitedNonLeaves.Add(currentNonLeafVertex);
                if (addResult == false)
                {
                    Assert.Fail($"Attempted to traverse to non-leaf vertex '{currentNonLeafVertex}' twice.");
                }

                return true;
            });

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp5", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp5"));
            Assert.AreEqual(2, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per8"));
            Assert.IsTrue(visitedLeaves.Contains("Per9"));


            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp6", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp6"));
            Assert.AreEqual(1, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per10"));


            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp7", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp7"));
            Assert.AreEqual(0, visitedLeaves.Count);


            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp8", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(3, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp8"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp5"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp6"));
            Assert.AreEqual(3, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per8"));
            Assert.IsTrue(visitedLeaves.Contains("Per9"));
            Assert.IsTrue(visitedLeaves.Contains("Per10"));


            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp9", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(3, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp9"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp6"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp7"));
            Assert.AreEqual(1, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per10"));


            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp10", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(6, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp10"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp8"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp9"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp5"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp6"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp7"));
            Assert.AreEqual(3, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per8"));
            Assert.IsTrue(visitedLeaves.Contains("Per9"));
            Assert.IsTrue(visitedLeaves.Contains("Per10"));


            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp4", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(2, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp4"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.AreEqual(3, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per1"));
            Assert.IsTrue(visitedLeaves.Contains("Per2"));
            Assert.IsTrue(visitedLeaves.Contains("Per3"));


            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp3", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(3, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp2"));
            Assert.AreEqual(7, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per1"));
            Assert.IsTrue(visitedLeaves.Contains("Per2"));
            Assert.IsTrue(visitedLeaves.Contains("Per3"));
            Assert.IsTrue(visitedLeaves.Contains("Per4"));
            Assert.IsTrue(visitedLeaves.Contains("Per5"));
            Assert.IsTrue(visitedLeaves.Contains("Per6"));
            Assert.IsTrue(visitedLeaves.Contains("Per7"));
        }

        [Test]
        public void TraverseReverseFromNonLeaf_StopTraversingAtSpecifiedNonLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            CreatePersonGroupGraph2(testDirectedGraph);
            // If we traverse from 'Grp8' but stop at 'Grp6', we should not reach 'Per10'
            var visitedLeaves = new HashSet<String>();
            var visitedNonLeaves = new HashSet<String>();
            var leafVertexAction = new Func<String, Boolean>((String currentLeafVertex) =>
            {
                Boolean addResult = visitedLeaves.Add(currentLeafVertex);
                if (addResult == false)
                {
                    Assert.Fail($"Attempted to traverse to leaf vertex '{currentLeafVertex}' twice.");
                }

                return true;
            });
            var nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                Boolean addResult = visitedNonLeaves.Add(currentNonLeafVertex);
                if (addResult == false)
                {
                    Assert.Fail($"Attempted to traverse to non-leaf vertex '{currentNonLeafVertex}' twice.");
                }
                if (currentNonLeafVertex == "Grp6")
                {
                    return false;
                }

                return true;
            });

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp8", nonLeafVertexAction, leafVertexAction);

            Assert.GreaterOrEqual(visitedNonLeaves.Count, 2);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp8"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp6"));
            Assert.IsFalse(visitedLeaves.Contains("Per10"));


            // If we traverse from 'Grp10' but stop at 'Grp8', we should not reach 'Grp6'
            visitedLeaves.Clear();
            visitedNonLeaves.Clear();
            nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                Boolean addResult = visitedNonLeaves.Add(currentNonLeafVertex);
                if (addResult == false)
                {
                    Assert.Fail($"Attempted to traverse to non-leaf vertex '{currentNonLeafVertex}' twice.");
                }
                if (currentNonLeafVertex == "Grp8")
                {
                    return false;
                }

                return true;
            });

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp10", nonLeafVertexAction, leafVertexAction);

            Assert.GreaterOrEqual(visitedNonLeaves.Count, 2);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp10"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp8"));
            Assert.IsFalse(visitedNonLeaves.Contains("Grp6"));
            Assert.IsFalse(visitedLeaves.Contains("Per8"));
            Assert.IsFalse(visitedLeaves.Contains("Per9"));
        }

        [Test]
        public void TraverseReverseFromNonLeaf_StopTraversingAtSpecifiedLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            CreatePersonGroupGraph2(testDirectedGraph);
            // If we traverse from 'Grp10' but stop at 'Per10', we should not reach any other vertices (leaf vertices are preocessed before non-leaf)
            var visitedLeaves = new HashSet<String>();
            var visitedNonLeaves = new HashSet<String>();
            var leafVertexAction = new Func<String, Boolean>((String currentLeafVertex) =>
            {
                Boolean addResult = visitedLeaves.Add(currentLeafVertex);
                if (addResult == false)
                {
                    Assert.Fail($"Attempted to traverse to leaf vertex '{currentLeafVertex}' twice.");
                }
                if (currentLeafVertex == "Per10")
                {
                    return false;
                }

                return true;
            });
            var nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                Boolean addResult = visitedNonLeaves.Add(currentNonLeafVertex);
                if (addResult == false)
                {
                    Assert.Fail($"Attempted to traverse to non-leaf vertex '{currentNonLeafVertex}' twice.");
                }

                return true;
            });

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp10", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp10"));
            Assert.AreEqual(1, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per10"));


            // If we traverse from 'Grp9' but stop at 'Per10', we should not reach any other vertices (leaf vertices are preocessed before non-leaf)
            visitedLeaves.Clear();
            visitedNonLeaves.Clear();

            testDirectedGraph.TraverseReverseFromNonLeaf("Grp9", nonLeafVertexAction, leafVertexAction);

            Assert.AreEqual(1, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp9"));
            Assert.AreEqual(1, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per10"));
        }

        [Test]
        public void TraverseGraph()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            var visitedLeaves = new HashSet<String>();
            var visitedNonLeaves = new HashSet<String>();
            var leafVertexAction = new Func<String, Boolean>((String currentLeafVertex) =>
            {
                visitedLeaves.Add(currentLeafVertex);

                return true;
            });
            var nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                visitedNonLeaves.Add(currentNonLeafVertex);

                return true;
            });

            ((DirectedGraphWithProtectedMembers<String, String>)testDirectedGraph).TraverseGraph(leafVertexAction, nonLeafVertexAction);

            Assert.AreEqual(7, visitedLeaves.Count);
            Assert.IsTrue(visitedLeaves.Contains("Per1"));
            Assert.IsTrue(visitedLeaves.Contains("Per2"));
            Assert.IsTrue(visitedLeaves.Contains("Per3"));
            Assert.IsTrue(visitedLeaves.Contains("Per4"));
            Assert.IsTrue(visitedLeaves.Contains("Per5"));
            Assert.IsTrue(visitedLeaves.Contains("Per6"));
            Assert.IsTrue(visitedLeaves.Contains("Per7"));
            Assert.AreEqual(4, visitedNonLeaves.Count);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp1"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp4"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp3"));
            Assert.IsTrue(visitedNonLeaves.Contains("Grp2"));
        }

        [Test]
        public void TraverseGraph_StopTraversingAtSpecifiedLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            // Add a group which is only connected to 'Per3' (hence should not be traversed to if traversal stops at 'Per3')
            testDirectedGraph.AddNonLeafVertex("Grp5");
            testDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp5");
            var visitedLeaves = new HashSet<String>();
            var visitedNonLeaves = new HashSet<String>();
            var leafVertexAction = new Func<String, Boolean>((String currentLeafVertex) =>
            {
                visitedLeaves.Add(currentLeafVertex);

                if (currentLeafVertex == "Per3")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });
            var nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                visitedNonLeaves.Add(currentNonLeafVertex);

                return true;
            });

            ((DirectedGraphWithProtectedMembers<String, String>)testDirectedGraph).TraverseGraph(leafVertexAction, nonLeafVertexAction);

            Assert.GreaterOrEqual(visitedLeaves.Count, 1);
            Assert.IsTrue(visitedLeaves.Contains("Per3"));
            Assert.IsFalse(visitedNonLeaves.Contains("Grp5"));
        }

        [Test]
        public void TraverseGraph_StopTraversingAtSpecifiedNonLeafVertex()
        {
            CreatePersonGroupGraph(testDirectedGraph);
            // Add a group which is only connected to 'Grp2' (hence should not be traversed to if traversal stops at 'Grp2')
            testDirectedGraph.AddNonLeafVertex("Grp5");
            testDirectedGraph.AddNonLeafToNonLeafEdge("Grp2", "Grp5");
            var visitedLeaves = new HashSet<String>();
            var visitedNonLeaves = new HashSet<String>();
            var leafVertexAction = new Func<String, Boolean>((String currentLeafVertex) =>
            {
                visitedLeaves.Add(currentLeafVertex);

                return true;
            });
            var nonLeafVertexAction = new Func<String, Boolean>((String currentNonLeafVertex) =>
            {
                visitedNonLeaves.Add(currentNonLeafVertex);

                if (currentNonLeafVertex == "Grp2")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            });

            ((DirectedGraphWithProtectedMembers<String, String>)testDirectedGraph).TraverseGraph(leafVertexAction, nonLeafVertexAction);

            Assert.GreaterOrEqual(visitedLeaves.Count, 1);
            Assert.IsTrue(visitedLeaves.Contains("Per3") || visitedLeaves.Contains("Per4") || visitedLeaves.Contains("Per5") || visitedLeaves.Contains("Per6"));
            Assert.GreaterOrEqual(visitedNonLeaves.Count, 1);
            Assert.IsTrue(visitedNonLeaves.Contains("Grp2"));
            Assert.IsFalse(visitedNonLeaves.Contains("Grp5"));
        }

        [Test]
        public void AddRemoveAdd()
        {
            // Tests Add*() > Remove*() > Add*() add operations in sequence, to ensure that no residual vertices nor edges are left in the underying structures after Remove*() operations
            CreatePersonGroupGraph(testDirectedGraph);

            testDirectedGraph.RemoveNonLeafToNonLeafEdge("Grp2", "Grp3");
            testDirectedGraph.RemoveNonLeafToNonLeafEdge("Grp1", "Grp3");
            testDirectedGraph.RemoveNonLeafToNonLeafEdge("Grp1", "Grp4");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per7", "Grp3");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per6", "Grp2");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per5", "Grp2");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per4", "Grp2");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per3", "Grp2");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per3", "Grp1");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per2", "Grp1");
            testDirectedGraph.RemoveLeafToNonLeafEdge("Per1", "Grp1");
            testDirectedGraph.RemoveNonLeafVertex("Grp4");
            testDirectedGraph.RemoveNonLeafVertex("Grp3");
            testDirectedGraph.RemoveNonLeafVertex("Grp2");
            testDirectedGraph.RemoveNonLeafVertex("Grp1");
            testDirectedGraph.RemoveLeafVertex("Per7");
            testDirectedGraph.RemoveLeafVertex("Per6");
            testDirectedGraph.RemoveLeafVertex("Per5");
            testDirectedGraph.RemoveLeafVertex("Per4");
            testDirectedGraph.RemoveLeafVertex("Per3");
            testDirectedGraph.RemoveLeafVertex("Per2");
            testDirectedGraph.RemoveLeafVertex("Per1");

            Assert.AreEqual(0, testDirectedGraph.LeafVertices.Count());
            Assert.AreEqual(0, testDirectedGraph.NonLeafVertices.Count());
            Assert.AreEqual(0, ((DirectedGraphWithProtectedMembers<String, String>)testDirectedGraph).LeafToNonLeafEdges.Count);
            Assert.AreEqual(0, ((DirectedGraphWithProtectedMembers<String, String>)testDirectedGraph).NonLeafToNonLeafEdges.Count);


            CreatePersonGroupGraph(testDirectedGraph);
        }

        #region Private/Protected Methods

        // Creates the following graph consisting of groups (non-leaves) and people (leaves)...
        //
        //                  Grp4         Grp3-------------
        //                     \       /       \          \   
        //                      \    /           \         \ 
        // Non-leaf vertices     Grp1      --------Grp2     \
        //                     /  |   \   /      /  |   \    \
        // Leaf vertices   Per1 Per2  Per3 Per4  Per5 Per6  Per7
        //
        /// <summary>
        /// Creates a sample graph representing users and groups of users in the specified graph.
        /// </summary>
        /// <param name="inputGraph">The graph to create the sample structure in.</param>
        protected void CreatePersonGroupGraph(DirectedGraph<String, String> inputGraph)
        {
            inputGraph.AddLeafVertex("Per1");
            inputGraph.AddLeafVertex("Per2");
            inputGraph.AddLeafVertex("Per3");
            inputGraph.AddLeafVertex("Per4");
            inputGraph.AddLeafVertex("Per5");
            inputGraph.AddLeafVertex("Per6");
            inputGraph.AddLeafVertex("Per7");
            inputGraph.AddNonLeafVertex("Grp1");
            inputGraph.AddNonLeafVertex("Grp2");
            inputGraph.AddNonLeafVertex("Grp3");
            inputGraph.AddNonLeafVertex("Grp4");
            inputGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            inputGraph.AddLeafToNonLeafEdge("Per2", "Grp1");
            inputGraph.AddLeafToNonLeafEdge("Per3", "Grp1");
            inputGraph.AddLeafToNonLeafEdge("Per3", "Grp2");
            inputGraph.AddLeafToNonLeafEdge("Per4", "Grp2");
            inputGraph.AddLeafToNonLeafEdge("Per5", "Grp2");
            inputGraph.AddLeafToNonLeafEdge("Per6", "Grp2");
            inputGraph.AddLeafToNonLeafEdge("Per7", "Grp3");
            inputGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp4");
            inputGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp3");
            inputGraph.AddNonLeafToNonLeafEdge("Grp2", "Grp3");
        }

        // Creates the following graph consisting of groups (non-leaves) and people (leaves)...
        //
        //                                  ----------------Grp10
        //                                 /        --------/  |
        //                                /        /           |
        //                             Grp8       Grp9         |
        //                            /    \     / |  \        |
        //                           /      \   /  |   \       |
        // Non-leaf vertices     Grp5        Grp6  |    Grp7   |
        //                     /      \          \ |           |
        // Leaf vertices   Per8        Per9       Per10--------|
        //
        /// <summary>
        /// Creates a sample graph representing users and groups of users in the specified graph.
        /// </summary>
        /// <param name="inputGraph">The graph to create the sample structure in.</param>
        /// <remarks>This graph is designed for testing of reverse mappings.</remarks>
        protected void CreatePersonGroupGraph2(DirectedGraph<String, String> inputGraph)
        {
            inputGraph.AddLeafVertex("Per8");
            inputGraph.AddLeafVertex("Per9");
            inputGraph.AddLeafVertex("Per10");
            inputGraph.AddNonLeafVertex("Grp5");
            inputGraph.AddNonLeafVertex("Grp6");
            inputGraph.AddNonLeafVertex("Grp7");
            inputGraph.AddNonLeafVertex("Grp8");
            inputGraph.AddNonLeafVertex("Grp9");
            inputGraph.AddNonLeafVertex("Grp10");
            inputGraph.AddLeafToNonLeafEdge("Per8", "Grp5");
            inputGraph.AddLeafToNonLeafEdge("Per9", "Grp5");
            inputGraph.AddLeafToNonLeafEdge("Per10", "Grp6");
            inputGraph.AddLeafToNonLeafEdge("Per10", "Grp9");
            inputGraph.AddLeafToNonLeafEdge("Per10", "Grp10");
            inputGraph.AddNonLeafToNonLeafEdge("Grp5", "Grp8");
            inputGraph.AddNonLeafToNonLeafEdge("Grp6", "Grp8");
            inputGraph.AddNonLeafToNonLeafEdge("Grp6", "Grp9");
            inputGraph.AddNonLeafToNonLeafEdge("Grp7", "Grp9");
            inputGraph.AddNonLeafToNonLeafEdge("Grp8", "Grp10");
            inputGraph.AddNonLeafToNonLeafEdge("Grp9", "Grp10");
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the DirectedGraph class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TLeaf">The type of leaf vertices.</typeparam>
        /// <typeparam name="TNonLeaf">The type of non-leaf vertices.</typeparam>
        private class DirectedGraphWithProtectedMembers<TLeaf, TNonLeaf> : DirectedGraph<TLeaf, TNonLeaf> where TLeaf : IEquatable<TLeaf> where TNonLeaf : IEquatable<TNonLeaf>
        {
            public DirectedGraphWithProtectedMembers()
                : base()
            {
            }

            /// <summary>
            /// >The edges which join leaf and non-left vertices within the graph.
            /// </summary>
            public IDictionary<TLeaf, ISet<TNonLeaf>> LeafToNonLeafEdges
            {
                get { return leafToNonLeafEdges; }
            }

            /// <summary>
            /// The edges which join non-leaf and non-left vertices within the graph.
            /// </summary>
            public IDictionary<TNonLeaf, ISet<TNonLeaf>> NonLeafToNonLeafEdges
            {
                get { return nonLeafToNonLeafEdges; }
            }

            /// <summary>
            /// The reverse of the edges in member 'LeafToNonLeafEdges'.
            /// </summary>
            public IDictionary<TNonLeaf, ISet<TLeaf>> LeafToNonLeafReverseEdges
            {
                get { return leafToNonLeafReverseEdges; }
            }

            /// <summary>
            /// The reverse of the edges in member 'NonLeafToNonLeafEdges'.
            /// </summary>
            public IDictionary<TNonLeaf, ISet<TNonLeaf>> NonLeafToNonLeafReverseEdges
            {
                get { return nonLeafToNonLeafReverseEdges; }
            }

            /// <summary>
            /// Traverses the entire graph, invoking the specified actions at each leaf and non-leaf vertex.
            /// </summary>
            /// <param name="leafVertexAction">The action to perform at leaf vertices.  Accepts a single parameter which is the current leaf vertex to perform the action on, and returns a boolean indicating whether traversal should continue.</param>
            /// <param name="nonLeafVertexAction">The action to perform at non-leaf vertices.  Accepts a single parameter which is the current non-leaf vertex to perform the action on, and returns a boolean indicating whether traversal should continue.</param>
            public new void TraverseGraph(Func<TLeaf, Boolean> leafVertexAction, Func<TNonLeaf, Boolean> nonLeafVertexAction)
            {
                base.TraverseGraph(leafVertexAction, nonLeafVertexAction);
            }
        }

        #endregion
    }
}