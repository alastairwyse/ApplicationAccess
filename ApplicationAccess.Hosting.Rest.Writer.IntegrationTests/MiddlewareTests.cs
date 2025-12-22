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
using System.Linq;
using System.Net.Http;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NSubstitute;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Persistence;

namespace ApplicationAccess.Hosting.Rest.Writer.IntegrationTests
{
    /// <summary>
    /// Tests custom routines and settings in the ASP.NET middleware layers (e.g. custom error handling).
    /// </summary>
    public class MiddlewareTests : WriterIntegrationTestsBase
    {
        // Need to use 'Order' attribute so the TripSwitch() test is run last... after the switch is tripped, nothing works

        /// <summary>
        /// Tests that controller methods return a 405 status when and unsupported HTTP verb is requested.
        /// </summary>
        [Test]
        [Order(0)]
        public void UnsupportedHttpMethod()
        {
            const String requestUrl = "api/v1/users/User1";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Patch, requestUrl))
            {

                using (HttpResponseMessage response = client.SendAsync(requestMessage).Result)
                {

                    String responseBody = response.Content.ReadAsStringAsync().Result;
                    Assert.IsEmpty(responseBody);
                    Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 404 status when a <see cref="UserNotFoundException{T}"/> is thrown.
        /// </summary>
        [Test]
        [Order(0)]
        public void UserNotFoundExceptionMappedToHttpErrorResponse()
        {
            const String user = "invalidUser";
            const String requestUrl = $"api/v1/users/{user}";
            var mockException = new UserNotFoundException<String>($"User '{user}' does not exist.", "user", user);
            mockUserEventProcessor.When((processor) => processor.RemoveUser(user)).Do((callInfo) => throw mockException);

            using (HttpResponseMessage response = client.DeleteAsync(requestUrl).Result)
            {

                mockUserEventProcessor.Received(1).RemoveUser(user);
                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "UserNotFoundException", mockException.Message);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "ParameterName", "user");
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "User", user);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 404 status when a <see cref="GroupNotFoundException{T}"/> is thrown.
        /// </summary>
        [Test]
        [Order(0)]
        public void GroupNotFoundExceptionMappedToHttpErrorResponse()
        {
            const String group = "invalidGroup";
            const String requestUrl = $"api/v1/groups/{group}";
            var mockException = new GroupNotFoundException<String>($"Group '{group}' does not exist.", "group", group);
            mockGroupEventProcessor.When((processor) => processor.RemoveGroup(group)).Do((callInfo) => throw mockException);

            using (HttpResponseMessage response = client.DeleteAsync(requestUrl).Result)
            {

                mockGroupEventProcessor.Received(1).RemoveGroup(group);
                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "GroupNotFoundException", mockException.Message);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "ParameterName", "group");
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "Group", group);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 404 status when a <see cref="EntityTypeNotFoundException"/> is thrown.
        /// </summary>
        [Test]
        [Order(0)]
        public void EntityTypeNotFoundExceptionMappedToHttpErrorResponse()
        {
            const String entityType = "invalidEntityType";
            const String requestUrl = $"api/v1/entityTypes/{entityType}";
            var mockException = new EntityTypeNotFoundException($"Entity type '{entityType}' does not exist.", "entityType", entityType);
            mockEntityEventProcessor.When((processor) => processor.RemoveEntityType(entityType)).Do((callInfo) => throw mockException);

            using (HttpResponseMessage response = client.DeleteAsync(requestUrl).Result)
            {

                mockEntityEventProcessor.Received(1).RemoveEntityType(entityType);
                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, typeof(EntityTypeNotFoundException).Name, mockException.Message);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "ParameterName", "entityType");
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "EntityType", entityType);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 404 status when a <see cref="EntityNotFoundException"/> is thrown.
        /// </summary>
        [Test]
        [Order(0)]
        public void EntityNotFoundExceptionMappedToHttpErrorResponse()
        {
            const String entityType = "ClientAccount";
            const String entity = "InvalidEntity";
            const String requestUrl = $"api/v1/entityTypes/{entityType}/entities/{entity}";
            var mockException = new EntityNotFoundException($"Entity '{entity}' does not exist.", "entityType", entityType, entity);
            mockEntityEventProcessor.When((processor) => processor.RemoveEntity(entityType, entity)).Do((callInfo) => throw mockException);

            using (HttpResponseMessage response = client.DeleteAsync(requestUrl).Result)
            {

                mockEntityEventProcessor.Received(1).RemoveEntity(entityType, entity);
                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, typeof(EntityNotFoundException).Name, mockException.Message);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "ParameterName", "entityType");
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "EntityType", entityType);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "Entity", entity);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 400 status when an invalid API endpoint version is requested.
        /// </summary>
        [Test]
        [Order(0)]
        public void UnsupportedApiVersion()
        {
            const String requestUrl = "api/v0/users/User1";

            using (HttpResponseMessage response = client.PostAsync(requestUrl, null).Result)
            {

                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "UnsupportedApiVersion", "The HTTP resource that matches the request URI 'http://localhost/api/v0/users/User1' does not support the API version '0'.");
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that the <see cref="TripSwitchMiddleware{TTripException}"/> trips correctly.
        /// </summary>
        [Test]
        [Order(Int32.MaxValue)]
        public void TripSwitch()
        {
            const String entityType = "BusinessUnit";
            const String requestUrl = $"api/v1/entityTypes/{entityType}/entities";
            tripSwitchActuator.Actuate();

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {

                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "ServiceUnavailableException", "The service is unavailable due to an internal error.");
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            }
        }
    }
}
