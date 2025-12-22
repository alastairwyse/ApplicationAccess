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
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.Writer.IntegrationTests
{
    /// <summary>
    /// Tests various patterns of controller method parameter sets and return types in the ApplicationAccess.Hosting.Rest.Writer namespace.
    /// </summary>
    public class ControllerTests : WriterIntegrationTestsBase
    {
        /// <summary>
        /// Success test for a POST method endpoint that creates a resource.
        /// </summary>
        [Test]
        public void PostCreateResourceMethod()
        {
            const String user = "user1";
            const String group = "group1";
            const String requestUrl = $"/api/v1/userToGroupMappings/user/{user}/group/{group}";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {

                using (HttpResponseMessage response = client.SendAsync(requestMessage).Result)
                {

                    mockUserEventProcessor.Received(1).AddUserToGroupMapping(user, group);
                    String responseBody = response.Content.ReadAsStringAsync().Result;
                    Assert.IsEmpty(responseBody);
                    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Success test for a DELETE method endpoint that removes a resource.
        /// </summary>
        [Test]
        public void DeleteRemoveResourceMethod()
        {
            const String user = "user1";
            const String group = "group1";
            const String requestUrl = $"/api/v1/userToGroupMappings/user/{user}/group/{group}";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUrl))
            {

                using (HttpResponseMessage response = client.SendAsync(requestMessage).Result)
                {

                    mockUserEventProcessor.Received(1).RemoveUserToGroupMapping(user, group);
                    String responseBody = response.Content.ReadAsStringAsync().Result;
                    Assert.IsEmpty(responseBody);
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Success test for the node status endpoint.
        /// </summary>
        [Test] 
        public void StatusMethod()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/status"))
            using (var response = client.SendAsync(request).Result)
            {

                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                HttpContent content = response.Content;
                String contentAsString = content.ReadAsStringAsync().Result;
                JObject contentAsJson = JObject.Parse(contentAsString);
                Assert.IsNotNull(contentAsJson["startTime"]);
            }
        }
    }
}
