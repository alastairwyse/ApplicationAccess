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
using System.Text;
using ApplicationAccess;
using ApplicationAccess.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Serialization.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Serialization.DirectedGraphJsonSerializer class.
    /// </summary>
    public class DirectedGraphJsonSerializerTests
    {
        private DirectedGraphJsonSerializer testDirectedGraphJsonSerializer;

        [SetUp]
        protected void SetUp()
        {
            testDirectedGraphJsonSerializer = new DirectedGraphJsonSerializer();
        }

        [Test]
        public void Serialize_EmptyGraph()
        {
            var testDirectedGraph = new DirectedGraph<String, String>();
            var comparisonDocument = new JObject(); 
            comparisonDocument.Add("leafVertices", new JArray());
            comparisonDocument.Add("leafToNonLeafEdges", new JArray());
            comparisonDocument.Add("nonLeafVertices", new JArray());
            comparisonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            JObject result = testDirectedGraphJsonSerializer.Serialize<String, String>(testDirectedGraph, new StringUniqueStringifier(), new StringUniqueStringifier());

            Assert.AreEqual(comparisonDocument.ToString(), result.ToString());
        }

        [Test]
        public void Serialize()
        {
            var testDirectedGraph = new DirectedGraph<String, String>();
            CreatePersonGroupGraph(testDirectedGraph);
            var comparisonDocument = new JObject();
            var leafVertices = new JArray();
            var leafEdges = new JArray();
            foreach (String currentPerson in testDirectedGraph.LeafVertices)
            {
                leafVertices.Add(currentPerson);
                var edges = new JArray();
                foreach (String currentGroup in testDirectedGraph.GetLeafEdges(currentPerson))
                {
                    edges.Add(currentGroup);
                }
                if (edges.Count > 0)
                {
                    var currentPersonToGroupMappings = new JObject();
                    currentPersonToGroupMappings.Add("leafVertex", currentPerson);
                    currentPersonToGroupMappings.Add("nonLeafVertices", edges);
                    leafEdges.Add(currentPersonToGroupMappings);
                }
            }
            comparisonDocument.Add("leafVertices", leafVertices);
            comparisonDocument.Add("leafToNonLeafEdges", leafEdges);
            var nonLeafVertices = new JArray();
            var nonLeafEdges = new JArray();
            foreach (String currentGroup in testDirectedGraph.NonLeafVertices)
            {
                nonLeafVertices.Add(currentGroup);
                var edges = new JArray();
                foreach (String currentToGroup in testDirectedGraph.GetNonLeafEdges(currentGroup))
                {
                    edges.Add(currentToGroup);
                }
                if (edges.Count > 0)
                {
                    var currentGroupToGroupMappings = new JObject();
                    currentGroupToGroupMappings.Add("nonLeafVertex", currentGroup);
                    currentGroupToGroupMappings.Add("nonLeafVertices", edges);
                    nonLeafEdges.Add(currentGroupToGroupMappings);
                }
            }
            comparisonDocument.Add("nonLeafVertices", nonLeafVertices);
            comparisonDocument.Add("nonLeafToNonLeafEdges", nonLeafEdges);

            JObject result = testDirectedGraphJsonSerializer.Serialize<String, String>(testDirectedGraph, new StringUniqueStringifier(), new StringUniqueStringifier());

            Assert.AreEqual(comparisonDocument.ToString(), result.ToString());
        }

        [Test]
        public void Deserialize_LeafVerticesPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'leafVertices' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_NonLeafVerticesPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'leafToNonLeafEdges' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_LeafToNonLeafEdgesPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'nonLeafVertices' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_NonLeafToNonLeafEdgesPropertyPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'nonLeafToNonLeafEdges' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_LeafVerticesPropertyIsNotArray()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JObject());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Property 'leafVertices' in JSON document in parameter 'jsonDocument' is not of type 'Newtonsoft.Json.Linq.JArray'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_LeafToNonLeafEdgesPropertyIsNotArray()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JObject());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Property 'leafToNonLeafEdges' in JSON document in parameter 'jsonDocument' is not of type 'Newtonsoft.Json.Linq.JArray'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_NonLeafVerticesPropertyIsNotArray()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JObject());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Property 'nonLeafVertices' in JSON document in parameter 'jsonDocument' is not of type 'Newtonsoft.Json.Linq.JArray'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_NonLeafToNonLeafEdgesPropertyIsNotArray()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JObject());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Property 'nonLeafToNonLeafEdges' in JSON document in parameter 'jsonDocument' is not of type 'Newtonsoft.Json.Linq.JArray'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_FailureToDeserializeLeafVertex()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafVertices"]).Add("InvalidAccessLevel");

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<AccessLevel, String>(testJsonDocument, new EnumUniqueStringifier<AccessLevel>(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize leaf vertex 'InvalidAccessLevel'"));
        }

        [Test]
        public void Deserialize_FailureToDeserializeNonLeafVertex()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafVertices"]).Add("InvalidAccessLevel");

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, AccessLevel>(testJsonDocument, new StringUniqueStringifier(), new EnumUniqueStringifier<AccessLevel>());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize non-leaf vertex 'InvalidAccessLevel'"));
        }

        [Test]
        public void Deserialize_LeafToNonLeafEdgesElementLeafVertexPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertices", new String[] { "Grp1" })
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'leafToNonLeafEdges' in JSON document in parameter 'jsonDocument' does not contain a 'leafVertex' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_LeafToNonLeafEdgesElementNonLeafVerticesPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'leafToNonLeafEdges' in JSON document in parameter 'jsonDocument' does not contain a 'nonLeafVertices' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_LeafToNonLeafEdgesElementLeafVertexPropertyIsNotJValue()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", new String[] { "Per1" }), 
                    new JProperty("nonLeafVertices", new String[] { "Grp1" })
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'leafToNonLeafEdges' in JSON document in parameter 'jsonDocument' contains a 'leafVertex' property which is not of type 'Newtonsoft.Json.Linq.JValue'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_LeafToNonLeafEdgesElementNonLeafVerticesPropertyIsNotArray()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per1"), 
                    new JProperty("nonLeafVertices", "Grp1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'leafToNonLeafEdges' in JSON document in parameter 'jsonDocument' contains a 'nonLeafVertices' property which is not of type 'Newtonsoft.Json.Linq.JArray'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }


        [Test]
        public void Deserialize_FailureToDeserializeLeafToNonLeafEdgesElement()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "InvalidAccessLevel"),
                    new JProperty("nonLeafVertices", new String[] { "Grp1" })
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<AccessLevel, String>(testJsonDocument, new EnumUniqueStringifier<AccessLevel>(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize leaf to non-leaf edge between leaf vertex 'InvalidAccessLevel' and non-leaf vertex 'Grp1'."));


            testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per1"),
                    new JProperty("nonLeafVertices", new String[] { "InvalidAccessLevel" })
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, AccessLevel>(testJsonDocument, new StringUniqueStringifier(), new EnumUniqueStringifier<AccessLevel>());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize leaf to non-leaf edge between leaf vertex 'Per1' and non-leaf vertex 'InvalidAccessLevel'."));


            testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per1"),
                    new JProperty("nonLeafVertices", new String[] { "Grp1" })
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize leaf to non-leaf edge between leaf vertex 'Per1' and non-leaf vertex 'Grp1'."));
            Assert.IsInstanceOf(typeof(LeafVertexNotFoundException<String>), e.InnerException);
        }

        [Test]
        public void Deserialize_NonLeafToNonLeafEdgesElementNonLeafVertexPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertices", new String[] { "Grp2" })
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'nonLeafToNonLeafEdges' in JSON document in parameter 'jsonDocument' does not contain a 'nonLeafVertex' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_NonLeafToNonLeafEdgesElementNonLeafVerticesPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertex", "Grp1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'nonLeafToNonLeafEdges' in JSON document in parameter 'jsonDocument' does not contain a 'nonLeafVertices' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_NonLeafToNonLeafEdgesElementNonLeafVertexPropertyIsNotJValue()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertex", new String[] { "Per1" }),
                    new JProperty("nonLeafVertices", new String[] { "Grp1" })
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'nonLeafToNonLeafEdges' in JSON document in parameter 'jsonDocument' contains a 'nonLeafVertex' property which is not of type 'Newtonsoft.Json.Linq.JValue'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_NonLeafToNonLeafEdgesElementNonLeafVerticesPropertyIsNotArray()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertex", "Per1"),
                    new JProperty("nonLeafVertices", "Grp1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'nonLeafToNonLeafEdges' in JSON document in parameter 'jsonDocument' contains a 'nonLeafVertices' property which is not of type 'Newtonsoft.Json.Linq.JArray'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }


        [Test]
        public void Deserialize_FailureToDeserializeNonLeafToNonLeafEdgesElement()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertex", "InvalidAccessLevel"),
                    new JProperty("nonLeafVertices", new String[] { "View" })
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, AccessLevel>(testJsonDocument, new StringUniqueStringifier(), new EnumUniqueStringifier<AccessLevel>());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize non-leaf to non-leaf edge between non-leaf vertex 'InvalidAccessLevel' and non-leaf vertex 'View'."));


            testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertex", "View"),
                    new JProperty("nonLeafVertices", new String[] { "InvalidAccessLevel" })
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, AccessLevel>(testJsonDocument, new StringUniqueStringifier(), new EnumUniqueStringifier<AccessLevel>());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize non-leaf to non-leaf edge between non-leaf vertex 'View' and non-leaf vertex 'InvalidAccessLevel'."));


            testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["nonLeafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("nonLeafVertex", "Grp1"),
                    new JProperty("nonLeafVertices", new String[] { "Grp2" })
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize non-leaf to non-leaf edge between non-leaf vertex 'Grp1' and non-leaf vertex 'Grp2'."));
            Assert.IsInstanceOf(typeof(NonLeafVertexNotFoundException<String>), e.InnerException);
        }

        [Test]
        public void DeserializeEmptyGraph()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());

            testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
        }

        [Test]
        public void Deserialize()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("leafVertices", new JArray());
            testJsonDocument.Add("leafToNonLeafEdges", new JArray());
            testJsonDocument.Add("nonLeafVertices", new JArray());
            testJsonDocument.Add("nonLeafToNonLeafEdges", new JArray());
            ((JArray)testJsonDocument["leafVertices"]).Add("Per1");
            ((JArray)testJsonDocument["leafVertices"]).Add("Per2");
            ((JArray)testJsonDocument["leafVertices"]).Add("Per3");
            ((JArray)testJsonDocument["leafVertices"]).Add("Per4");
            ((JArray)testJsonDocument["leafVertices"]).Add("Per5");
            ((JArray)testJsonDocument["leafVertices"]).Add("Per6");
            ((JArray)testJsonDocument["leafVertices"]).Add("Per7");
            ((JArray)testJsonDocument["nonLeafVertices"]).Add("Grp1");
            ((JArray)testJsonDocument["nonLeafVertices"]).Add("Grp2");
            ((JArray)testJsonDocument["nonLeafVertices"]).Add("Grp3");
            ((JArray)testJsonDocument["nonLeafVertices"]).Add("Grp4");
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per1"),
                    new JProperty("nonLeafVertices", new String[] { "Grp1" })
                )
            );
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per2"),
                    new JProperty("nonLeafVertices", new String[] { "Grp1" })
                )
            );
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per3"),
                    new JProperty("nonLeafVertices", new String[] { "Grp1", "Grp2" })
                )
            );
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per4"),
                    new JProperty("nonLeafVertices", new String[] { "Grp2" })
                )
            );
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per5"),
                    new JProperty("nonLeafVertices", new String[] { "Grp2" })
                )
            );
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per6"),
                    new JProperty("nonLeafVertices", new String[] { "Grp2" })
                )
            );
            ((JArray)testJsonDocument["leafToNonLeafEdges"]).Add(new JObject
                (
                    new JProperty("leafVertex", "Per7"),
                    new JProperty("nonLeafVertices", new String[] { "Grp3" })
                )
            );

            testDirectedGraphJsonSerializer.Deserialize<String, String>(testJsonDocument, new StringUniqueStringifier(), new StringUniqueStringifier());
        }

        [Test]
        public void SerializeDeserialize()
        {
            var testDirectedGraph = new DirectedGraph<String, String>();
            CreatePersonGroupGraph(testDirectedGraph);

            JObject serializedGraph = testDirectedGraphJsonSerializer.Serialize<String, String>(testDirectedGraph, new StringUniqueStringifier(), new StringUniqueStringifier());
            DirectedGraph<String, String> result = testDirectedGraphJsonSerializer.Deserialize<String, String>(serializedGraph, new StringUniqueStringifier(), new StringUniqueStringifier());

            var leafVertices = new HashSet<String>(result.LeafVertices);
            Assert.AreEqual(7, leafVertices.Count);
            Assert.IsTrue(leafVertices.Contains("Per1"));
            Assert.IsTrue(leafVertices.Contains("Per2"));
            Assert.IsTrue(leafVertices.Contains("Per3"));
            Assert.IsTrue(leafVertices.Contains("Per4"));
            Assert.IsTrue(leafVertices.Contains("Per5"));
            Assert.IsTrue(leafVertices.Contains("Per6"));
            Assert.IsTrue(leafVertices.Contains("Per7"));

            var nonLeafVertices = new HashSet<String>(result.NonLeafVertices);
            Assert.AreEqual(4, nonLeafVertices.Count);
            Assert.IsTrue(nonLeafVertices.Contains("Grp1"));
            Assert.IsTrue(nonLeafVertices.Contains("Grp2"));
            Assert.IsTrue(nonLeafVertices.Contains("Grp3"));
            Assert.IsTrue(nonLeafVertices.Contains("Grp4"));

            Assert.AreEqual(1, result.GetLeafEdges("Per1").Count());
            Assert.IsTrue(result.GetLeafEdges("Per1").Contains("Grp1"));
            Assert.AreEqual(1, result.GetLeafEdges("Per2").Count());
            Assert.IsTrue(result.GetLeafEdges("Per2").Contains("Grp1"));
            Assert.AreEqual(2, result.GetLeafEdges("Per3").Count());
            Assert.IsTrue(result.GetLeafEdges("Per3").Contains("Grp1"));
            Assert.IsTrue(result.GetLeafEdges("Per3").Contains("Grp2"));
            Assert.AreEqual(1, result.GetLeafEdges("Per4").Count());
            Assert.IsTrue(result.GetLeafEdges("Per4").Contains("Grp2"));
            Assert.AreEqual(1, result.GetLeafEdges("Per5").Count());
            Assert.IsTrue(result.GetLeafEdges("Per5").Contains("Grp2"));
            Assert.AreEqual(1, result.GetLeafEdges("Per6").Count());
            Assert.IsTrue(result.GetLeafEdges("Per6").Contains("Grp2"));
            Assert.AreEqual(1, result.GetLeafEdges("Per7").Count());
            Assert.IsTrue(result.GetLeafEdges("Per7").Contains("Grp3"));

            Assert.AreEqual(2, result.GetNonLeafEdges("Grp1").Count());
            Assert.IsTrue(result.GetNonLeafEdges("Grp1").Contains("Grp3"));
            Assert.IsTrue(result.GetNonLeafEdges("Grp1").Contains("Grp4"));
            Assert.AreEqual(1, result.GetNonLeafEdges("Grp2").Count());
            Assert.IsTrue(result.GetNonLeafEdges("Grp2").Contains("Grp3"));
            Assert.AreEqual(0, result.GetNonLeafEdges("Grp3").Count());
            Assert.AreEqual(0, result.GetNonLeafEdges("Grp4").Count());
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
        /// Creates a sample graph representing users and groups of users in the provided graph.
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

        #endregion

        #region Nested Classes

        protected enum AccessLevel
        {
            View,
            Create,
            Modify,
            Delete
        }

        #endregion
    }
}
