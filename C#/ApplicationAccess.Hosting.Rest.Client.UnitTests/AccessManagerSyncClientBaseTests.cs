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
        private TestAccessManagerClientWithProtectedMembers<String, String, String, String> testAccessManagerClient;

        [SetUp]
        protected void SetUp()
        {
            baseUrl = new Uri("http://localhost:5170/api/v1/");
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerClient = new TestAccessManagerClientWithProtectedMembers<String, String, String, String>
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
            testAccessManagerClient.Dispose();
        }

        [Test]
        public void Constructor_RetryCountParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testAccessManagerClient = new TestAccessManagerClientWithProtectedMembers<String, String, String, String>
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
                testAccessManagerClient = new TestAccessManagerClientWithProtectedMembers<String, String, String, String>
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
                testAccessManagerClient = new TestAccessManagerClientWithProtectedMembers<String, String, String, String>
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
                testAccessManagerClient = new TestAccessManagerClientWithProtectedMembers<String, String, String, String>
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
            const String errorCode = "ArgumentException";
            const String errorMessage = "User 'abc' does not exist. (Parameter 'user')";
            var responseBodyHttpErrorResponse = new HttpErrorResponse(errorCode, errorMessage);
            using (var responseBody = new MemoryStream())
            {
                WriteSerializedHttpErrorResponseToStream(responseBodyHttpErrorResponse, responseBody);

                var e = Assert.Throws<ArgumentException>(delegate
                {
                    testAccessManagerClient.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.BadRequest, responseBody);
                });

                Assert.That(e.Message, Does.StartWith(errorMessage));
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
                    testAccessManagerClient.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.InternalServerError, responseBody);
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
                    testAccessManagerClient.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.ServiceUnavailable, responseBody);
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
                    testAccessManagerClient.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.ServiceUnavailable, responseBody);
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
                    testAccessManagerClient.HandleNonSuccessResponseStatus(HttpMethod.Post, testUrl, HttpStatusCode.ServiceUnavailable, responseBody);
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
        private class TestAccessManagerClientWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : AccessManagerSyncClientBase<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.UnitTests.TestAccessManagerClientWithProtectedMembers class.
            /// </summary>
            public TestAccessManagerClientWithProtectedMembers
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
