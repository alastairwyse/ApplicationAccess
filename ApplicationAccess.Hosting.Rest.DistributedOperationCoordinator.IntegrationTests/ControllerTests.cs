﻿/*
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
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator.IntegrationTests
{
    /// <summary>
    /// Tests various patterns of controller method parameter sets and return types in the ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator namespace.
    /// </summary>
    public class ControllerTests : DistributedOperationCoordinatorIntegrationTestsBase
    {
        /// <summary>
        /// Success test for a POST method endpoint that creates a resource.
        /// </summary>
        [Test]
        public async Task PostCreateResourceMethod()
        {
            const String user = "user1";
            const String group = "group1";
            const String requestUrl = $"/api/v1/userToGroupMappings/user/{user}/group/{group}";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {

                using (HttpResponseMessage response = await client.SendAsync(requestMessage))
                {

                    await mockDistributedAccessManagerOperationCoordinator.Received(1).AddUserToGroupMappingAsync(user, group);
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
        public async Task DeleteRemoveResourceMethod()
        {
            const String user = "user1";
            const String group = "group1";
            const String requestUrl = $"/api/v1/userToGroupMappings/user/{user}/group/{group}";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUrl))
            {

                using (HttpResponseMessage response = await client.SendAsync(requestMessage))
                {

                    await mockDistributedAccessManagerOperationCoordinator.Received(1).RemoveUserToGroupMappingAsync(user, group);
                    String responseBody = response.Content.ReadAsStringAsync().Result;
                    Assert.IsEmpty(responseBody);
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Success test for a GET method endpoint that returns a string.
        /// </summary>
        [Test]
        public async Task GetReturnStringMethod()
        {
            const String user = "user1";
            const String requestUrl = $"/api/v1/users/{user}";
            mockDistributedAccessManagerOperationCoordinator.ContainsUserAsync(user).Returns(Task.FromResult<Boolean>(true));

            using (HttpResponseMessage response = await client.GetAsync(requestUrl))
            {

                await mockDistributedAccessManagerOperationCoordinator.Received(1).ContainsUserAsync(user);
                JToken jsonResponse = JValue.Parse(await response.Content.ReadAsStringAsync());
                Assert.AreEqual(user, jsonResponse.ToString());
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Success test for a GET method endpoint that returns an arrray of strings.
        /// </summary>
        [Test]
        public async Task GetReturnStringArrayMethod()
        {
            var users = new List<String>() { "user1", "user2", "user3" };
            const String requestUrl = $"/api/v1/users";
            mockDistributedAccessManagerOperationCoordinator.GetUsersAsync().Returns(Task.FromResult<List<String>>(users));

            using (HttpResponseMessage response = await client.GetAsync(requestUrl))
            {

                await mockDistributedAccessManagerOperationCoordinator.Received(1).GetUsersAsync();
                JArray jsonArrayResponse = ConvertHttpContentToJsonArray(response.Content);
                Assert.AreEqual(3, jsonArrayResponse.Count);
                Assert.AreEqual("user1", jsonArrayResponse[0].ToString());
                Assert.AreEqual("user2", jsonArrayResponse[1].ToString());
                Assert.AreEqual("user3", jsonArrayResponse[2].ToString());
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Success test for a GET method endpoint that returns a single object.
        /// </summary>
        [Test]
        public async Task GetReturnObjectMethod()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String requestUrl = $"/api/v1/entityTypes/{entityType}/entities/{entity}";
            mockDistributedAccessManagerOperationCoordinator.ContainsEntityAsync(entityType, entity).Returns(Task.FromResult<Boolean>(true));

            using (HttpResponseMessage response = await client.GetAsync(requestUrl))
            {

                await mockDistributedAccessManagerOperationCoordinator.Received(1).ContainsEntityAsync(entityType, entity);
                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                if (jsonResponse["entityType"] == null)
                    Assert.Fail("The returned JSON object doesn't contain an 'entityType' property.");
                if (jsonResponse["entity"] == null)
                    Assert.Fail("The returned JSON object doesn't contain an 'entity' property.");
                Assert.AreEqual(entityType, jsonResponse["entityType"].ToString());
                Assert.AreEqual(entity, jsonResponse["entity"].ToString());
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Success test for a GET method endpoint that returns an array of objects.
        /// </summary>
        [Test]
        public async Task GetReturnObjectArrayMethod()
        {
            const String user = "user1";
            var groups = new List<String>() { "group1", "group2", "group3" };
            const String requestUrl = $"/api/v1/userToGroupMappings/user/{user}?includeIndirectMappings=false";
            mockDistributedAccessManagerOperationCoordinator.GetUserToGroupMappingsAsync(user, false).Returns(Task.FromResult<List<String>>(groups));

            using (HttpResponseMessage response = await client.GetAsync(requestUrl))
            {

                await mockDistributedAccessManagerOperationCoordinator.Received(1).GetUserToGroupMappingsAsync(user, false);
                JArray jsonArrayResponse = ConvertHttpContentToJsonArray(response.Content);
                Assert.AreEqual(3, jsonArrayResponse.Count);
                foreach (JObject currentArrayElement in jsonArrayResponse)
                {
                    if (currentArrayElement["user"] == null)
                        Assert.Fail("The returned JSON array element doesn't contain a 'user' property.");
                    if (currentArrayElement["group"] == null)
                        Assert.Fail("The returned JSON array element doesn't contain a 'group' property.");
                    Assert.AreEqual(user, currentArrayElement["user"].ToString());
                }
                Assert.AreEqual("group1", jsonArrayResponse[0]["group"].ToString());
                Assert.AreEqual("group2", jsonArrayResponse[1]["group"].ToString());
                Assert.AreEqual("group3", jsonArrayResponse[2]["group"].ToString());
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        /// <summary>
        /// Success test for a GET method endpoint that returns a boolean value.
        /// </summary>
        [Test]
        public async Task GetReturnBooleanMethod()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String requestUrl = $"/api/v1/dataElementAccess/entity/user/{user}/entityType/{entityType}/entity/{entity}";
            mockDistributedAccessManagerOperationCoordinator.HasAccessToEntityAsync(user, entityType, entity).Returns(Task.FromResult<Boolean>(false));

            using (HttpResponseMessage response = await client.GetAsync(requestUrl))
            {

                await mockDistributedAccessManagerOperationCoordinator.Received(1).HasAccessToEntityAsync(user, entityType, entity);
                JToken jsonResponse = JValue.Parse(response.Content.ReadAsStringAsync().Result);
                Assert.AreEqual(new JValue(false), jsonResponse);
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }
}
