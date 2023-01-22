/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using NUnit.Framework;
using NUnit.Framework.Internal;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Utilities.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Utilities.HttpErrorResponseJsonSerializer class.
    /// </summary>
    public class HttpErrorResponseJsonSerializerTests
    {
        private HttpErrorResponseJsonSerializer testHttpErrorResponseJsonSerializer;

        [SetUp]
        protected void SetUp()
        {
            testHttpErrorResponseJsonSerializer = new HttpErrorResponseJsonSerializer();
        }

        [Test]
        public void Serialize_HttpErrorResponseWithCodeAndMessage()
        {
            var errorResponse = new HttpErrorResponse
            (
                typeof(ArgumentException).Name,
                "Argument 'recordCount' must be greater than or equal to 0."
            );
            string expectedJsonString = @"
            {
                ""error"" : {
                    ""code"" : ""ArgumentException"", 
                    ""message"" : ""Argument 'recordCount' must be greater than or equal to 0.""
                }
            }
            ";
            var expectedJson = JObject.Parse(expectedJsonString);

            JObject result = testHttpErrorResponseJsonSerializer.Serialize(errorResponse);

            Assert.AreEqual(expectedJson, result);
        }

        [Test]
        public void Serialize_HttpErrorResponseWithCodeMessageAndTarget()
        {
            var errorResponse = new HttpErrorResponse
            (
                typeof(ArgumentException).Name,
                "Argument 'recordCount' must be greater than or equal to 0.",
                "recordCount"
            );
            string expectedJsonString = @"
            {
                ""error"" : {
                    ""code"" : ""ArgumentException"", 
                    ""message"" : ""Argument 'recordCount' must be greater than or equal to 0."", 
                    ""target"" : ""recordCount"", 
                }
            }
            ";
            var expectedJson = JObject.Parse(expectedJsonString);

            JObject result = testHttpErrorResponseJsonSerializer.Serialize(errorResponse);

            Assert.AreEqual(expectedJson, result);
        }

        [Test]
        public void Serialize_HttpErrorResponseWithCodeMessageAndAttributes()
        {
            var errorResponse = new HttpErrorResponse
            (
                typeof(ArgumentException).Name,
                "A mapping between user 'user1' and group 'group1' already exists.",
                new List<Tuple<String, String>>()
                {
                    new Tuple<String, String>("user", "user1"),
                    new Tuple<String, String>("group", "group1")
                }
            );
            string expectedJsonString = @"
            {
                ""error"" : {
                    ""code"" : ""ArgumentException"", 
                    ""message"" : ""A mapping between user 'user1' and group 'group1' already exists."", 
                    ""attributes"" : 
                    [
                        { ""name"": ""user"", ""value"": ""user1"" }, 
                        { ""name"": ""group"", ""value"": ""group1"" }
                    ]
                }
            }
            ";
            var expectedJson = JObject.Parse(expectedJsonString);

            JObject result = testHttpErrorResponseJsonSerializer.Serialize(errorResponse);

            Assert.AreEqual(expectedJson, result);
        }


        [Test]
        public void Serialize_HttpErrorResponseWithCodeMessageAndInnerError()
        {
            var errorResponse = new HttpErrorResponse
            (
                typeof(ArgumentException).Name,
                "A mapping between user 'user1' and group 'group1' already exists.",
                new HttpErrorResponse
                (
                    typeof(ArgumentException).Name,
                    "An edge already exists between vertices 'child' and 'parent'."
                )
            );
            string expectedJsonString = @"
            {
                ""error"" : {
                    ""code"" : ""ArgumentException"", 
                    ""message"" : ""A mapping between user 'user1' and group 'group1' already exists."", 
                    ""innererror"" : 
                    {
                        ""code"" : ""ArgumentException"", 
                        ""message"" : ""An edge already exists between vertices 'child' and 'parent'."",
                    }
                }
            }
            ";
            var expectedJson = JObject.Parse(expectedJsonString);

            JObject result = testHttpErrorResponseJsonSerializer.Serialize(errorResponse);

            Assert.AreEqual(expectedJson, result);
        }

        [Test]
        public void Serialize_HttpErrorResponseWithAllProperties()
        {
            var errorResponse = new HttpErrorResponse
            (
                typeof(ArgumentException).Name,
                "Failed to add edge to graph.", 
                "graph",
                new List<Tuple<String, String>>()
                {
                    new Tuple<String, String>("fromVertex", "child"),
                    new Tuple<String, String>("toVertex", "parent")
                },
                new HttpErrorResponse
                (
                    typeof(ArgumentException).Name,
                    "An edge already exists between vertices 'child' and 'parent'."
                )
            );
            string expectedJsonString = @"
            {
                ""error"" : {
                    ""code"" : ""ArgumentException"", 
                    ""message"" : ""Failed to add edge to graph."", 
                    ""target"" : ""graph"", 
                    ""attributes"" : 
                    [
                        { ""name"": ""fromVertex"", ""value"": ""child"" }, 
                        { ""name"": ""toVertex"", ""value"": ""parent"" }
                    ], 
                    ""innererror"" : 
                    {
                        ""code"" : ""ArgumentException"", 
                        ""message"" : ""An edge already exists between vertices 'child' and 'parent'."",
                    }
                }
            }
            ";
            var expectedJson = JObject.Parse(expectedJsonString);

            JObject result = testHttpErrorResponseJsonSerializer.Serialize(errorResponse);

            Assert.AreEqual(expectedJson, result);
        }
    }
}
