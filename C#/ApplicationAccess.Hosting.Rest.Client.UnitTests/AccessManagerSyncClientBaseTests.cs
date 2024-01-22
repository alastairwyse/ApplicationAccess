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
using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.Client.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Client.AccessManagerSyncClientBase class.
    /// </summary>
    public class AccessManagerSyncClientBaseTests
    {
        private const Int32 retryCount = 5;
        private const Int32 retryInterval = 10;
        private Encoding defaultEncoding = Encoding.UTF8;

        private Uri baseUrl;
        private IApplicationLogger mockLogger;
        private IMetricLogger mockMetricLogger;
        private TestAccessManagerClientBaseWithProtectedMembers<String, String, String, String> testAccessManagerClientBase;

        [SetUp]
        protected void SetUp()
        {
            baseUrl = new Uri("http://localhost:5170/api/v1/");
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerClientBase = new TestAccessManagerClientBaseWithProtectedMembers<String, String, String, String>
            (
                baseUrl, 
                new StringUniqueStringifier(), 
                new StringUniqueStringifier(), 
                new StringUniqueStringifier(), 
                new StringUniqueStringifier(), 
                retryCount, 
                retryInterval, 
                mockLogger, 
                mockMetricLogger
            );
        }

        [TearDown]
        protected void TearDown()
        {
            testAccessManagerClientBase.Dispose();
        }

        [Test]
        public void Constructor_RetryCountParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testAccessManagerClientBase = new TestAccessManagerClientBaseWithProtectedMembers<String, String, String, String>
                (
                    baseUrl, 
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    -1,
                    10,
                    mockLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryCount' with value -1 cannot be less than 0."));
            Assert.AreEqual("retryCount", e.ParamName);
        }

        [Test]
        public void Constructor_RetryCountParameterGreaterThan59()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testAccessManagerClientBase = new TestAccessManagerClientBaseWithProtectedMembers<String, String, String, String>
                (
                    baseUrl,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    60,
                    10,
                    mockLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryCount' with value 60 cannot be greater than 59."));
            Assert.AreEqual("retryCount", e.ParamName);
        }

        [Test]
        public void Constructor_RetryIntervalParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testAccessManagerClientBase = new TestAccessManagerClientBaseWithProtectedMembers<String, String, String, String>
                (
                    baseUrl,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    5,
                    -1,
                    mockLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryInterval' with value -1 cannot be less than 0."));
            Assert.AreEqual("retryInterval", e.ParamName);
        }

        [Test]
        public void Constructor_RetryIntervalParameterGreaterThan120()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testAccessManagerClientBase = new TestAccessManagerClientBaseWithProtectedMembers<String, String, String, String>
                (
                    baseUrl,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    5,
                    121,
                    mockLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryInterval' with value 121 cannot be greater than 120."));
            Assert.AreEqual("retryInterval", e.ParamName);
        }

        [Test]
        public void HandleNonSuccessResponseStatus_400StatusCodeWithHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/abc/group/group1");
            const String errorCode = "UnrecognizedCode";
            const String errorMessage = "User 'abc' does not exist. (Parameter 'user')";
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<ArgumentException>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.BadRequest, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_400StatusCodeWithArgumentExceptionHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/abc/group/group1");
            const String errorCode = "ArgumentException";
            const String errorMessage = "User 'abc' does not exist. (Parameter 'user')";
            var attributes = new List<Tuple<String, String>>() { Tuple.Create("ParameterName", "user") };
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage, attributes);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<ArgumentException>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.BadRequest, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
                Assert.AreEqual("user", e.ParamName);
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_400StatusCodeWithArgumentOutOfRangeExceptionHttpErrorResponseBody()
        {
            // Url doesn't relate to error/exception... can't find any use of ArgumentOutOfRangeException outside constructors
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/abc/group/group1");
            const String errorCode = "ArgumentOutOfRangeException";
            const String errorMessage = "Parameter 'cachedEventCount' must be greater than or equal to 1. (Parameter 'cachedEventCount')";
            var attributes = new List<Tuple<String, String>>() { Tuple.Create("ParameterName", "cachedEventCount") };
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage, attributes);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.BadRequest, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
                Assert.AreEqual("cachedEventCount", e.ParamName);
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_400StatusCodeWithArgumentNullExceptionHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/%20/group/group1");
            const String errorCode = "ArgumentNullException";
            const String errorMessage = "Parameter 'user' cannot be null. (Parameter 'user')";
            var attributes = new List<Tuple<String, String>>() { Tuple.Create("ParameterName", "user") };
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage, attributes);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<ArgumentNullException>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.BadRequest, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
                Assert.AreEqual("user", e.ParamName);
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_404StatusCodeWithHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/abc/group/group1");
            const String errorCode = "UnrecognizedCode";
            const String errorMessage = "User 'abc' does not exist. (Parameter 'user')";
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<NotFoundException>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.NotFound, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_404StatusCodeWithUserNotFoundExceptionHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/abc/group/group1");
            const String errorCode = "UserNotFoundException";
            const String errorMessage = "User 'abc' does not exist. (Parameter 'user')";
            var attributes = new List<Tuple<String, String>>() 
            {
                Tuple.Create("ParameterName", "user"),
                Tuple.Create("User", "abc"),
            };
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage, attributes);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<UserNotFoundException<String>>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.NotFound, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
                Assert.AreEqual("user", e.ParamName);
                Assert.AreEqual("abc", e.User);
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_404StatusCodeWithGroupNotFoundExceptionHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/abc/group/group1");
            const String errorCode = "GroupNotFoundException";
            const String errorMessage = "Group 'group1' does not exist. (Parameter 'group')";
            var attributes = new List<Tuple<String, String>>()
            {
                Tuple.Create("ParameterName", "group"),
                Tuple.Create("Group", "group1"),
            };
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage, attributes);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<GroupNotFoundException<String>>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.NotFound, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
                Assert.AreEqual("group", e.ParamName);
                Assert.AreEqual("group1", e.Group);
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_404StatusCodeWithEntityTypeNotFoundExceptionHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "entityTypes/ClientAccount/entities/CompanyA");
            const String errorCode = "EntityTypeNotFoundException";
            const String errorMessage = "Entity type 'ClientAccount' does not exist. (Parameter 'entityType')";
            var attributes = new List<Tuple<String, String>>()
            {
                Tuple.Create("ParameterName", "entityType"),
                Tuple.Create("EntityType", "ClientAccount"),
            };
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage, attributes);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<EntityTypeNotFoundException>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Delete, testUrl, HttpStatusCode.NotFound, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
                Assert.AreEqual("entityType", e.ParamName);
                Assert.AreEqual("ClientAccount", e.EntityType);
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_404StatusCodeWithEntityNotFoundExceptionHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "entityTypes/ClientAccount/entities/CompanyA");
            const String errorCode = "EntityNotFoundException";
            const String errorMessage = "Entity 'CompanyA' does not exist. (Parameter 'entity')";
            var attributes = new List<Tuple<String, String>>()
            {
                Tuple.Create("ParameterName", "entity"),
                Tuple.Create("EntityType", "ClientAccount"),
                Tuple.Create("Entity", "CompanyA"),
            };
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage, attributes);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<EntityNotFoundException>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Delete, testUrl, HttpStatusCode.NotFound, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
                Assert.AreEqual("entity", e.ParamName);
                Assert.AreEqual("ClientAccount", e.EntityType);
                Assert.AreEqual("CompanyA", e.Entity);
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_500StatusCodeWithHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/user1/group/group1");
            const String errorCode = "BufferFlushingException";
            const String errorMessage = "Exception occurred on buffer flushing worker thread at 2023-01-29 12:29:12.075 +09:00.";
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<Exception>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.InternalServerError, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_UnhandledStatusCodeWithHttpErrorResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/user1/group/group1");
            const String errorCode = "ServiceUnavailable";
            const String errorMessage = "The server is not ready to handle the request.";
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<Exception>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.ServiceUnavailable, responseBody);
                });

                Assert.That(e.Message, Does.StartWith($"Failed to call URL '{testUrl.ToString()}' with 'POST' method.  Received non-succces HTTP response status '503', error code '{errorCode}', and error message '{errorMessage}'."));
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_NonJsonResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/user1/group/group1");
            const String errorMessage = "The server is not ready to handle the request.";
            using (var responseBody = new MemoryStream())
            {
                using (var streamWriter = new System.IO.StreamWriter(responseBody, encoding: defaultEncoding, leaveOpen: true))
                {
                    streamWriter.Write(errorMessage);
                }
                responseBody.Position = 0;

                var e = Assert.Throws<Exception>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.ServiceUnavailable, responseBody);
                });

                Assert.That(e.Message, Does.StartWith($"Failed to call URL '{testUrl.ToString()}' with 'POST' method.  Received non-succces HTTP response status '503' and response body '{errorMessage}'."));
            }
        }

        [Test]
        public void HandleNonSuccessResponseStatus_WhitespaceResponseBody()
        {
            var testUrl = new Uri(baseUrl, "userToGroupMappings/user/user1/group/group1");
            using (var responseBody = new MemoryStream())
            {
                using (var streamWriter = new System.IO.StreamWriter(responseBody, encoding: defaultEncoding, leaveOpen: true))
                {
                    streamWriter.Write(" ");
                }
                responseBody.Position = 0;

                var e = Assert.Throws<Exception>(delegate
                {
                    testAccessManagerClientBase.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.ServiceUnavailable, responseBody);
                });

                Assert.That(e.Message, Does.StartWith($"Failed to call URL '{testUrl.ToString()}' with 'POST' method.  Received non-succces HTTP response status '503'."));
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Serializes a <see cref="HttpErrorResponse"/> as JSON and writes it to the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="httpErrorResponse">The HttpErrorResponse to serialize and write.</param>
        /// <param name="stream">The Strem to write to.</param>
        private void WriteSerializedHttpErrorResponseToStream(HttpErrorResponse httpErrorResponse, Stream stream)
        {
            var serializer = new HttpErrorResponseJsonSerializer();
            JObject serializedErrorResponse = serializer.Serialize(httpErrorResponse);
            using (var streamWriter = new System.IO.StreamWriter(stream, encoding: defaultEncoding, leaveOpen: true))
            {
                streamWriter.Write(serializedErrorResponse.ToString());
            }
            stream.Position = 0;
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the <see cref="AccessManagerSyncClientBase{TUser, TGroup, TComponent, TAccess}"/> class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        private class TestAccessManagerClientBaseWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : AccessManagerSyncClientBase<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.UnitTests.TestAccessManagerClientBaseWithProtectedMembers class.
            /// </summary>
            public TestAccessManagerClientBaseWithProtectedMembers
            (
                Uri baseUrl,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
            {
            }

            /// <summary>
            /// Handles receipt of a non-success HTTP response status, by converting the status and response body to an appropriate Exception and throwing that Exception.
            /// </summary>
            /// <param name="method">The HTTP method used in the request which generated the response.</param>
            /// <param name="requestUrl">The URL of the request which generated the response.</param>
            /// <param name="responseStatus">The received HTTP response status.</param>
            /// <param name="responseBody">The received response body.</param>
            public new void HandleNonSuccessResponseStatus(HttpMethod method, Uri requestUrl, HttpStatusCode responseStatus, Stream responseBody)
            {
                base.HandleNonSuccessResponseStatus(method, requestUrl, responseStatus, responseBody);
            }

            /// <summary>
            /// Attempts to deserialize the body of a HTTP response received as a <see cref="Stream"/> to an <see cref="HttpErrorResponse"/> instance.
            /// </summary>
            /// <param name="responseBody">The response body to deserialize.</param>
            /// <returns>The deserialized response body, or null if the reponse could not be deserialized (e.g. was empty, or did not contain JSON).</returns>
            public new HttpErrorResponse DeserializeResponseBodyToHttpErrorResponse(Stream responseBody)
            {
                return base.DeserializeResponseBodyToHttpErrorResponse(responseBody);
            }
        }

        #endregion
    }
}
