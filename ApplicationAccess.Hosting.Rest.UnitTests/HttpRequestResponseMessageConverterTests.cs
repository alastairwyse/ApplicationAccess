/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.HttpRequestResponseMessageConverter class.
    /// </summary>
    public class HttpRequestResponseMessageConverterTests
    {
        protected HttpRequestResponseMessageConverter testHttpRequestResponseMessageConverter;

        [SetUp]
        protected void SetUp()
        {
            testHttpRequestResponseMessageConverter = new HttpRequestResponseMessageConverter();
        }

        [TestCase("GET", "/api/v1/userToGroupMappings/user/user1", "?includeIndirectMappings=true")]
        [TestCase("GET", "/api/v1/userToGroupMappings/user/! * ' ( ) ; : @ & = + $ , / ? % # [ ]", "?includeIndirectMappings=true")]
        [TestCase("POST", "/api/v1/userToGroupMappings/user/user1/group/group1", "")]
        [TestCase("DELETE", "/api/v1/userToGroupMappings/user/user1/group/group1", "")]
        public void ConvertRequest(String sourceRequestMethodAsString, String sourceRequestPath, String queryStringAsString)
        {
            HttpMethod sourceRequestMethod = HttpMethod.Parse(sourceRequestMethodAsString);
            QueryString sourceRequestQueryString = new QueryString(queryStringAsString);
            var testHeaders = new List<KeyValuePair<String, StringValues>>()
            {
                KeyValuePair.Create(HeaderNames.Accept, new StringValues("*/*")),
                KeyValuePair.Create(HeaderNames.KeepAlive, new StringValues("keep-alive")),
                KeyValuePair.Create(HeaderNames.Host, new StringValues("127.0.0.1:5000")),
                KeyValuePair.Create(HeaderNames.UserAgent, new StringValues("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0")),
                KeyValuePair.Create(HeaderNames.AcceptEncoding, new StringValues(new string[] { "gzip", "deflate", "br", "zstd" })),
                KeyValuePair.Create(HeaderNames.AcceptLanguage, new StringValues("en-US,en;q=0.5")),
                KeyValuePair.Create(HeaderNames.Origin, new StringValues("http://127.0.0.1:5000")),
                KeyValuePair.Create(HeaderNames.Referer, new StringValues("http://127.0.0.1:5000/swagger/index.html")),
                KeyValuePair.Create(HeaderNames.ContentLength, new StringValues("0")),
                KeyValuePair.Create("Sec-Fetch-Dest", new StringValues("empty")),
                KeyValuePair.Create("Sec-Fetch-Mode", new StringValues("cors")),
                KeyValuePair.Create("Sec-Fetch-Site", new StringValues("same-origin")),
                KeyValuePair.Create("Priority", new StringValues("u=0"))
            };
            var testHost = new HostString("127.0.0.1", 5000);
            var testHttpRequest = CreateHttpRequest(testHeaders, sourceRequestMethod, "http", testHost, sourceRequestPath, sourceRequestQueryString);
            String testTargetScheme = "http";
            String testTargetHost = "192.168.0.252";
            UInt16 testTargetPort = 6003;
            using (var targetRequest = new HttpRequestMessage())
            {

                testHttpRequestResponseMessageConverter.ConvertRequest(testHttpRequest, targetRequest, testTargetScheme, testTargetHost, testTargetPort);

                Assert.AreEqual("*/*", targetRequest.Headers.Accept.ToString());
                Assert.AreEqual("keep-alive", targetRequest.Headers.GetValues(HeaderNames.KeepAlive).First());
                Assert.AreEqual("127.0.0.1:5000", targetRequest.Headers.Host.ToString());
                Assert.AreEqual("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0", targetRequest.Headers.UserAgent.ToString());
                Assert.AreEqual(4, targetRequest.Headers.AcceptEncoding.Count);
                Assert.AreEqual("gzip", targetRequest.Headers.AcceptEncoding.ToList()[0].ToString());
                Assert.AreEqual("deflate", targetRequest.Headers.AcceptEncoding.ToList()[1].ToString());
                Assert.AreEqual("br", targetRequest.Headers.AcceptEncoding.ToList()[2].ToString());
                Assert.AreEqual("zstd", targetRequest.Headers.AcceptEncoding.ToList()[3].ToString());
                Assert.AreEqual("en-US,en;q=0.5", targetRequest.Headers.AcceptLanguage.ToString());
                Assert.AreEqual("http://127.0.0.1:5000/swagger/index.html", targetRequest.Headers.Referrer.ToString());
                Assert.AreEqual(sourceRequestMethod, targetRequest.Method);
                Assert.AreEqual(testTargetScheme, targetRequest.RequestUri.Scheme);
                Assert.AreEqual(testTargetHost, targetRequest.RequestUri.Host);
                Assert.AreEqual(testTargetPort, targetRequest.RequestUri.Port);
                Assert.AreEqual(sourceRequestPath, targetRequest.RequestUri.LocalPath);
                Assert.AreEqual(queryStringAsString, targetRequest.RequestUri.Query);
            }
        }

        [Test]
        public async Task ConvertRequest_JsonHttpErrorResponse()
        {
            // Effectiveness of this test is questionable.  In the real case, the sourceResponse.Body property is a Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseStream
            //   which is not public/instantiable.  Below test just uses a plain Stream, but this isn't really testing in context.

            String testContenJsonString = @"
            {
              ""error"": {
                ""code"": ""GroupNotFoundException"",
                ""message"": ""Group 'group1' does not exist. (Parameter 'group')"",
                ""target"": ""ThrowGroupDoesntExistException"",
                ""attributes"": [
                  {
                    ""name"": ""ParameterName"",
                    ""value"": ""group""
                  },
                  {
                    ""name"": ""Group"",
                    ""value"": ""group1""
                  }
                ]
              }
            }";
            var testStatusCode = HttpStatusCode.NotFound;
            var testContentType = "application/json";
            var testTargetResponse = new HttpResponseMessage(testStatusCode);
            var sourceResponse = CreateHttpResponse();
            using (var testContent = new StringContent(testContenJsonString))
            {
                testTargetResponse.Content = testContent;
                testTargetResponse.Content.Headers.Remove("content-type");
                testTargetResponse.Content.Headers.Add("content-type", testContentType);

                await testHttpRequestResponseMessageConverter.ConvertResponseAsync(testTargetResponse, sourceResponse);

                Assert.AreEqual((Int32)testStatusCode, sourceResponse.StatusCode);
                Assert.That(sourceResponse.Headers.ContentType.ToString(), Does.StartWith(testContentType));
                String sourceResponseBodyAsString = ConvertStreamToString(sourceResponse.Body);
                Assert.AreEqual(testContenJsonString, sourceResponseBodyAsString);
            };
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates an <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="headers">The request's headers.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        /// <param name="scheme">The scheme.</param>
        /// <param name="host">The host.</param>
        /// <param name="path">The path.</param>
        /// <param name="queryString">The query string.</param>
        /// <returns>The <see cref="HttpRequest"/>.</returns>
        protected HttpRequest CreateHttpRequest(IEnumerable<KeyValuePair<String, StringValues>> headers, HttpMethod httpMethod, String scheme, HostString host, String path, QueryString queryString)
        {
            var httpContext = new DefaultHttpContext();
            foreach (KeyValuePair<String, StringValues> currentHeader in headers)
            {
                httpContext.Request.Headers.Add(currentHeader);
            }
            httpContext.Request.Method = httpMethod.ToString();
            httpContext.Request.Scheme = scheme;
            httpContext.Request.Host = host;
            httpContext.Request.Path = path;
            httpContext.Request.QueryString = queryString;

            var requestUriBuilder = new UriBuilder();
            requestUriBuilder.Scheme = scheme;
            requestUriBuilder.Host = host.Host;
            requestUriBuilder.Port = host.Port.Value;
            requestUriBuilder.Path = path;
            requestUriBuilder.Query = queryString.ToUriComponent();

            return httpContext.Request;
        }

        /// <summary>
        /// Creates an <see cref="HttpResponse"/>.
        /// </summary>
        /// <returns>The <see cref="HttpResponse"/>.</returns>
        protected HttpResponse CreateHttpResponse()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            return httpContext.Response;
        }

        /// <summary>
        /// Converts the specified Stream to a UTF-8 string.
        /// </summary>
        /// <param name="inputStream">The stream to convert.</param>
        /// <returns>The stream converted to a string.</returns>
        protected String ConvertStreamToString(Stream inputStream)
        {
            using (var textReader = new StreamReader(inputStream, Encoding.UTF8))
            {
                inputStream.Position = 0;
                return textReader.ReadToEnd();
            }
        }

        #endregion
    }
}
