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
using ApplicationAccess.Hosting.Models;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Newtonsoft.Json.Linq;
using ApplicationAccess.Serialization;
using System.Linq;

namespace ApplicationAccess.Hosting.Rest.Utilities.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.HttpErrorResponseJsonSerializer class.
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

        [Test]
        public void Deserialize_JsonDoesntContainErrorProperty()
        {
            var e = Assert.Throws<DeserializationException>(delegate
            {
                testHttpErrorResponseJsonSerializer.Deserialize(new JObject());
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize HttpErrorResponse.  The specified JObject did not contain an 'error' property."));
        }

        [Test]
        public void Deserialize_ErrorPropertyNotJObject()
        {
            var testJsonObject = new JObject();
            testJsonObject.Add("error", new JArray());

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize HttpErrorResponse.  The specified JObject did not contain an 'error' property."));
        }

        [Test]
        public void Deserialize_ErrorObjectDoesntContainCodeProperty()
        {
            var testJsonObject = JObject.Parse(@"
            {
                ""error"" : {
                    ""message"" : ""Failed to add edge to graph.""
                }
            }");

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize HttpErrorResponse 'error' or 'innererror' property.  The specified JObject did not contain a 'code' property."));
        }

        [Test]
        public void Deserialize_ErrorObjectDoesntContainMessageProperty()
        {
            var testJsonObject = JObject.Parse(@"
            {
                ""error"" : {
                    ""code"" : ""ArgumentException""
                }
            }");

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);
            });

            Assert.That(e.Message, Does.StartWith("Failed to deserialize HttpErrorResponse 'error' or 'innererror' property.  The specified JObject did not contain a 'message' property."));
        }

        [Test]
        public void Deserialize_NoInnerError()
        {
            const String testCode = "ArgumentException";
            const String testMessage = "Failed to add edge to graph.";
            const String testTarget = "graph";
            var testAttributes = new List<Tuple<String, String>>()
            {
                Tuple.Create( "fromVertex", "child" ),
                Tuple.Create( "toVertex", "parent" ),
            };
            var testErrorResponse = new HttpErrorResponse(testCode, testMessage, testTarget, testAttributes);
            JObject testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            HttpErrorResponse result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.AreEqual(testTarget, result.Target);
            Assert.AreEqual(testAttributes[0], result.Attributes.ElementAt(0));
            Assert.AreEqual(testAttributes[1], result.Attributes.ElementAt(1));
            Assert.IsNull(result.InnerError);


            testErrorResponse = new HttpErrorResponse(testCode, testMessage, testTarget);
            testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.AreEqual(testTarget, result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);


            testErrorResponse = new HttpErrorResponse(testCode, testMessage, testAttributes);
            testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.IsNull(result.Target);
            Assert.AreEqual(testAttributes[0], result.Attributes.ElementAt(0));
            Assert.AreEqual(testAttributes[1], result.Attributes.ElementAt(1));
            Assert.IsNull(result.InnerError);


            testErrorResponse = new HttpErrorResponse(testCode, testMessage);
            testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.IsNull(result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Deserialize_IncludingInnerError()
        {
            const String testCode = "ArgumentException";
            const String testMessage = "Failed to add edge to graph.";
            const String testTarget = "graph";
            var testAttributes = new List<Tuple<String, String>>()
            {
                Tuple.Create( "fromVertex", "child" ),
                Tuple.Create( "toVertex", "parent" ),
            };
            const String testInnerErrorCode = "Exception";
            const String testInnerErrorMessage = "An edge already exists between vertices 'child' and 'parent'.";
            var testInnerError = new HttpErrorResponse(testInnerErrorCode, testInnerErrorMessage);
            var testErrorResponse = new HttpErrorResponse(testCode, testMessage, testTarget, testAttributes, testInnerError);
            JObject testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            HttpErrorResponse result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.AreEqual(testTarget, result.Target);
            Assert.AreEqual(testAttributes[0], result.Attributes.ElementAt(0));
            Assert.AreEqual(testAttributes[1], result.Attributes.ElementAt(1));
            Assert.IsNotNull(result.InnerError);
            Assert.AreEqual(testInnerErrorCode, result.InnerError.Code);
            Assert.AreEqual(testInnerErrorMessage, result.InnerError.Message);


            testInnerError = new HttpErrorResponse(testInnerErrorCode, testInnerErrorMessage);
            testErrorResponse = new HttpErrorResponse(testCode, testMessage, testTarget, testInnerError);
            testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.AreEqual(testTarget, result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNotNull(result.InnerError);
            Assert.AreEqual(testInnerErrorCode, result.InnerError.Code);
            Assert.AreEqual(testInnerErrorMessage, result.InnerError.Message);


            testInnerError = new HttpErrorResponse(testInnerErrorCode, testInnerErrorMessage);
            testErrorResponse = new HttpErrorResponse(testCode, testMessage, testAttributes, testInnerError);
            testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.IsNull(result.Target);
            Assert.AreEqual(testAttributes[0], result.Attributes.ElementAt(0));
            Assert.AreEqual(testAttributes[1], result.Attributes.ElementAt(1));
            Assert.IsNotNull(result.InnerError);
            Assert.AreEqual(testInnerErrorCode, result.InnerError.Code);
            Assert.AreEqual(testInnerErrorMessage, result.InnerError.Message);


            testInnerError = new HttpErrorResponse(testInnerErrorCode, testInnerErrorMessage);
            testErrorResponse = new HttpErrorResponse(testCode, testMessage, testInnerError);
            testJsonObject = testHttpErrorResponseJsonSerializer.Serialize(testErrorResponse);

            result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual(testCode, result.Code);
            Assert.AreEqual(testMessage, result.Message);
            Assert.IsNull(result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNotNull(result.InnerError);
            Assert.AreEqual(testInnerErrorCode, result.InnerError.Code);
            Assert.AreEqual(testInnerErrorMessage, result.InnerError.Message);
        }

        [Test]
        public void Deserialize_MultiLevelInnerErrors()
        {
            var testJsonObject = JObject.Parse(@"
            {
                ""error"": {
                    ""code"": ""BufferFlushingException"",
                    ""message"": ""Exception occurred on buffer flushing worker thread at 2023-01-29 12:29:12.075 +09:00."",
                    ""target"": ""Throw"",
                    ""innererror"": {
                        ""code"": ""Exception"",
                        ""message"": ""Failed to process buffers and persist flushed events."",
                        ""target"": ""Flush"",
                        ""innererror"": {
                            ""code"": ""Exception"",
                            ""message"": ""Failed to execute stored procedure 'ProcessEvents' in SQL Server."",
                            ""target"": ""ExecuteStoredProcedure"",
                        }
                    }
                }
            }");

            HttpErrorResponse result = testHttpErrorResponseJsonSerializer.Deserialize(testJsonObject);

            Assert.AreEqual("BufferFlushingException", result.Code);
            Assert.AreEqual("Exception occurred on buffer flushing worker thread at 2023-01-29 12:29:12.075 +09:00.", result.Message);
            Assert.AreEqual("Throw", result.Target);
            Assert.AreEqual("Exception", result.InnerError.Code);
            Assert.AreEqual("Failed to process buffers and persist flushed events.", result.InnerError.Message);
            Assert.AreEqual("Flush", result.InnerError.Target);
            Assert.AreEqual("Exception", result.InnerError.InnerError.Code);
            Assert.AreEqual("Failed to execute stored procedure 'ProcessEvents' in SQL Server.", result.InnerError.InnerError.Message);
            Assert.AreEqual("ExecuteStoredProcedure", result.InnerError.InnerError.Target);
        }
    }
}
