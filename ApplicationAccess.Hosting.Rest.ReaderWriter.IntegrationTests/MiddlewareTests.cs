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

namespace ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests
{
    /// <summary>
    /// Tests custom routines and settings in the ASP.NET middleware layers (e.g. custom error handling).
    /// </summary>
    public class MiddlewareTests : ReaderWriterIntegrationTestsBase
    {
        // Need to use 'Order' attribute so the TripSwitch() test is run last... after the switch is tripped, nothing works

        /// <summary>
        /// Tests that controller methods return a 406 status when the request 'Accept' is not '*/*' or 'application/json'.
        /// </summary>
        [Test]
        [Order(0)]
        public void NonJsonAcceptHeader()
        {
            const String requestUrl = "api/v1/users";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl))
            {
                requestMessage.Headers.Add("Accept", "text/html");

                using (HttpResponseMessage response = client.SendAsync(requestMessage).Result)
                {

                    String responseBody = response.Content.ReadAsStringAsync().Result;
                    Assert.IsEmpty(responseBody);
                    Assert.AreEqual(HttpStatusCode.NotAcceptable, response.StatusCode);
                }
            }
        }

        /// <summary>
        /// Tests that controller methods return a 405 status when and unsupported HTTP verb is requested.
        /// </summary>
        [Test]
        [Order(0)]
        public void UnsupportedHttpMethod()
        {
            const String requestUrl = "api/v1/users";
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
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 400 status when an <see cref="ArgumentException"/> is thrown.
        /// </summary>
        [Test]
        [Order(0)]
        public void ArgumentExceptionMappedToHttpErrorResponse()
        {
            const String entityType = "invalidEntityType";
            const String requestUrl = $"api/v1/entityTypes/{entityType}/entities";
            var mockException = new ArgumentException($"Entity type '{entityType}' does not exist.", "entityType");
            mockEntityQueryProcessor.When((processor) => processor.GetEntities(entityType)).Do((callInfo) => throw mockException);

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {

                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "ArgumentException", mockException.Message);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "ParameterName", "entityType");
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 500 status when an <see cref="Exception"/> is thrown.
        /// </summary>
        /// <remarks>Note that this test is dependent on appsettings setting 'ErrorHandling.OverrideInternalServerErrors' being set true.</remarks>
        [Test]
        [Order(0)]
        public void ExceptionMappedToHttpErrorResponse()
        {
            const String entityType = "ClientAccounts";
            const String requestUrl = $"api/v1/entityTypes/{entityType}/entities";
            var mockException = new Exception($"An internal error occurred reading from persistent storage");
            mockEntityQueryProcessor.When((processor) => processor.GetEntities(entityType)).Do((callInfo) => throw mockException);

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {

                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "InternalServerError", "An internal server error occurred");
                Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 404 status when a <see cref="NotFoundException"/> is thrown.
        /// </summary>
        [Test]
        [Order(0)]
        public void NotFoundExceptionMappedToHttpErrorResponse()
        {
            const String entityType = "invalidEntityType";
            const String requestUrl = $"api/v1/entityTypes/{entityType}";
            mockEntityQueryProcessor.ContainsEntityType(entityType).Returns(false);

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {

                mockEntityQueryProcessor.Received(1).ContainsEntityType(entityType);
                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "NotFoundException", $"Entity type '{entityType}' does not exist.");
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "ResourceId", entityType);
                Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
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
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 400 status when a required query parameter (e.g. 'includeIndirectMappings') is not provided.
        /// </summary>
        [Test]
        [Order(0)]
        public void RequiredQueryParameterNotProvided()
        {
            const String requestUrl = "api/v1/userToGroupMappings/user/user1";

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {

                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, HttpStatusCode.BadRequest.ToString(), new ValidationProblemDetails().Title);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "Property", "includeIndirectMappings");
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "Error", "A value for the 'includeIndirectMappings' parameter or property was not provided.");
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 400 status when a required query parameter (e.g. 'includeIndirectMappings') contains an invalid value.
        /// </summary>
        [Test]
        [Order(0)]
        public void RequiredQueryParameterInvalid()
        {
            const String requestUrl = "api/v1/userToGroupMappings/user/user1?includeIndirectMappings=truu";

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {

                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, HttpStatusCode.BadRequest.ToString(), new ValidationProblemDetails().Title);
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "Property", "includeIndirectMappings");
                AssertHttpErrorResponseJsonContainsAttribute(jsonResponse, "Error", "The value 'truu' is not valid.");
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            }
        }

        /// <summary>
        /// Tests that controller methods return a <see cref="HttpErrorResponse"/> with 400 status when an invalid API endpoint version is requested.
        /// </summary>
        [Test]
        [Order(0)]
        public void UnsupportedApiVersion()
        {
            const String requestUrl = "api/v0/users";

            using (HttpResponseMessage response = client.GetAsync(requestUrl).Result)
            {

                JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                AssertJsonIsHttpErrorResponse(jsonResponse, "UnsupportedApiVersion", "The HTTP resource that matches the request URI 'http://localhost/api/v0/users' does not support the API version '0'.");
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
