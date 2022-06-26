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
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggingConcurrentDirectedGraph class.
    /// </summary>
    public class MetricLoggingConcurrentDirectedGraphTests
    {
        private MetricLoggingConcurrentDirectedGraph<String, String> testMetricLoggingConcurrentDirectedGraph;
        private IMetricLogger mockMetricLogger;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testMetricLoggingConcurrentDirectedGraph = new MetricLoggingConcurrentDirectedGraph<String, String>(mockMetricLogger);
        }

        [Test]
        public void AddLeafVertex()
        {
            Assert.AreEqual(0, testMetricLoggingConcurrentDirectedGraph.LeafVertices.Count());

            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");

            Assert.AreEqual(1, testMetricLoggingConcurrentDirectedGraph.LeafVertices.Count());
            mockMetricLogger.Received(1).Set(Arg.Any<LeafVerticesStored>(), 1);


            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per2");
            mockMetricLogger.Received(1).Set(Arg.Any<LeafVerticesStored>(), 1);

            Assert.AreEqual(2, testMetricLoggingConcurrentDirectedGraph.LeafVertices.Count());
            mockMetricLogger.Received(1).Set(Arg.Any<LeafVerticesStored>(), 1);


            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per3");

            Assert.AreEqual(3, testMetricLoggingConcurrentDirectedGraph.LeafVertices.Count());
            mockMetricLogger.Received(1).Set(Arg.Any<LeafVerticesStored>(), 3);
        }

        [Test]
        public void AddLeafVertex_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafVerticesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveLeafVertex()
        {
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp4");
            mockMetricLogger.ClearReceivedCalls();
            Assert.AreEqual(3, testMetricLoggingConcurrentDirectedGraph.LeafVertices.Count());

            testMetricLoggingConcurrentDirectedGraph.RemoveLeafVertex("Per2");

            mockMetricLogger.Received(1).Set(Arg.Any<LeafVerticesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 6);
        }

        [Test]
        public void RemoveLeafVertex_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");

            testMetricLoggingConcurrentDirectedGraph.RemoveLeafVertex("Per1");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafVerticesStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafToNonLeafEdgesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddNonLeafVertex()
        {
            Assert.AreEqual(0, testMetricLoggingConcurrentDirectedGraph.NonLeafVertices.Count());

            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");

            Assert.AreEqual(1, testMetricLoggingConcurrentDirectedGraph.NonLeafVertices.Count());
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafVerticesStored>(), 1);


            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");

            Assert.AreEqual(2, testMetricLoggingConcurrentDirectedGraph.NonLeafVertices.Count());
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafVerticesStored>(), 2);


            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");

            Assert.AreEqual(3, testMetricLoggingConcurrentDirectedGraph.NonLeafVertices.Count());
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafVerticesStored>(), 3);
        }

        [Test]
        public void AddNonLeafVertex_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<NonLeafVerticesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveNonLeafVertex()
        {
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp2", "Grp3");
            mockMetricLogger.ClearReceivedCalls();
            Assert.AreEqual(3, testMetricLoggingConcurrentDirectedGraph.NonLeafVertices.Count());

            testMetricLoggingConcurrentDirectedGraph.RemoveNonLeafVertex("Grp2");

            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafVerticesStored>(), 2);


            mockMetricLogger.ClearReceivedCalls();
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp5");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp3", "Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp3", "Grp5");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp4");
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 4);


            mockMetricLogger.ClearReceivedCalls();
            testMetricLoggingConcurrentDirectedGraph.RemoveNonLeafVertex("Grp3");

            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafVerticesStored>(), 3);
        }

        [Test]
        public void RemoveNonLeafVertex_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp2", "Grp3");

            testMetricLoggingConcurrentDirectedGraph.RemoveNonLeafVertex("Grp2");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafToNonLeafEdgesStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<NonLeafVerticesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddLeafToNonLeafEdge()
        {
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp4");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafToNonLeafEdgesStored>(), Arg.Any<Int64>());


            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp4");

            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 5);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 6);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 7);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 8);
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 9);
        }

        [Test]
        public void AddLeafToNonLeafEdge_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");

            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafToNonLeafEdgesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveLeafToNonLeafEdge()
        {
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp4");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafToNonLeafEdgesStored>(), Arg.Any<Int64>());


            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp4");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp4");

            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 9);
            mockMetricLogger.ClearReceivedCalls();


            testMetricLoggingConcurrentDirectedGraph.RemoveLeafToNonLeafEdge("Per1", "Grp2");
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 8);


            testMetricLoggingConcurrentDirectedGraph.RemoveLeafToNonLeafEdge("Per2", "Grp2");
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 7);


            testMetricLoggingConcurrentDirectedGraph.RemoveLeafToNonLeafEdge("Per2", "Grp4");
            mockMetricLogger.Received(1).Set(Arg.Any<LeafToNonLeafEdgesStored>(), 6);
        }

        [Test]
        public void RemoveLeafToNonLeafEdge_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");

            testMetricLoggingConcurrentDirectedGraph.RemoveLeafToNonLeafEdge("Per1", "Grp1");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<LeafToNonLeafEdgesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddNonLeafToNonLeafEdge()
        {
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp2");
            mockMetricLogger.DidNotReceive().Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), Arg.Any<Int64>());


            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp3");
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 1);


            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp2", "Grp3");
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 2);
        }

        [Test]
        public void AddNonLeafToNonLeafEdge_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");

            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp2");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge()
        {
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafVertex("Per3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per1", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per2", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddLeafToNonLeafEdge("Per3", "Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp3");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp2", "Grp3");
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 2);
            mockMetricLogger.ClearReceivedCalls();


            testMetricLoggingConcurrentDirectedGraph.RemoveNonLeafToNonLeafEdge("Grp1", "Grp3");
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 1);


            testMetricLoggingConcurrentDirectedGraph.RemoveNonLeafToNonLeafEdge("Grp2", "Grp3");
            mockMetricLogger.Received(1).Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), 0);
        }

        [Test]
        public void RemoveNonLeafToNonLeafEdge_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentDirectedGraph.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp1");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafVertex("Grp2");
            testMetricLoggingConcurrentDirectedGraph.AddNonLeafToNonLeafEdge("Grp1", "Grp2");

            testMetricLoggingConcurrentDirectedGraph.RemoveNonLeafToNonLeafEdge("Grp1", "Grp2");

            mockMetricLogger.DidNotReceive().Set(Arg.Any<NonLeafToNonLeafEdgesStored>(), Arg.Any<Int64>());
        }
    }
}
