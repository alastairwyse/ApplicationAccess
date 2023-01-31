﻿/*
 * Copyright 2020 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests
{
    /// <summary>
    /// Base class for integration test classes.
    /// </summary>
    public abstract class IntegrationTestsBase
    {
        protected IAccessManagerEntityEventProcessor mockEntityEventProcessor;
        protected IAccessManagerEntityQueryProcessor mockEntityQueryProcessor;
        protected IAccessManagerGroupEventProcessor<String, String, String> mockGroupEventProcessor;
        protected IAccessManagerGroupQueryProcessor<String, String, String> mockGroupQueryProcessor;
        protected IAccessManagerGroupToGroupEventProcessor<String> mockGroupToGroupEventProcessor;
        protected IAccessManagerGroupToGroupQueryProcessor<String> mockGroupToGroupQueryProcessor;
        protected IAccessManagerUserEventProcessor<String, String, String, String> mockUserEventProcessor;
        protected IAccessManagerUserQueryProcessor<String, String, String, String> mockUserQueryProcessor;
        protected TestReaderWriter testReaderWriter;
        protected HttpClient client;

        [OneTimeSetUp]
        protected void OneTimeSetUp()
        {
            mockEntityEventProcessor = Substitute.For<IAccessManagerEntityEventProcessor>();
            mockEntityQueryProcessor = Substitute.For<IAccessManagerEntityQueryProcessor>();
            mockGroupEventProcessor = Substitute.For<IAccessManagerGroupEventProcessor<String, String, String>>();
            mockGroupQueryProcessor = Substitute.For<IAccessManagerGroupQueryProcessor<String, String, String>>();
            mockGroupToGroupEventProcessor = Substitute.For<IAccessManagerGroupToGroupEventProcessor<String>>();
            mockGroupToGroupQueryProcessor = Substitute.For<IAccessManagerGroupToGroupQueryProcessor<String>>();
            mockUserEventProcessor = Substitute.For<IAccessManagerUserEventProcessor<String, String, String, String>>();
            mockUserQueryProcessor = Substitute.For<IAccessManagerUserQueryProcessor<String, String, String, String>>();
            testReaderWriter = new TestReaderWriter();
            testReaderWriter.Services.GetService<EntityEventProcessorHolder>().EntityEventProcessor = mockEntityEventProcessor;
            testReaderWriter.Services.GetService<EntityQueryProcessorHolder>().EntityQueryProcessor = mockEntityQueryProcessor;
            testReaderWriter.Services.GetService<GroupEventProcessorHolder>().GroupEventProcessor = mockGroupEventProcessor;
            testReaderWriter.Services.GetService<GroupQueryProcessorHolder>().GroupQueryProcessor = mockGroupQueryProcessor;
            testReaderWriter.Services.GetService<GroupToGroupEventProcessorHolder>().GroupToGroupEventProcessor = mockGroupToGroupEventProcessor;
            testReaderWriter.Services.GetService<GroupToGroupQueryProcessorHolder>().GroupToGroupQueryProcessor = mockGroupToGroupQueryProcessor;
            testReaderWriter.Services.GetService<UserEventProcessorHolder>().UserEventProcessor = mockUserEventProcessor;
            testReaderWriter.Services.GetService<UserQueryProcessorHolder>().UserQueryProcessor = mockUserQueryProcessor;
            client = testReaderWriter.CreateClient();
        }

        [OneTimeTearDown]
        protected void OneTimeTearDown()
        {
            client.Dispose();
            testReaderWriter.Dispose();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Attempts to convert the specified <see cref="HttpContent"/> to JSON.
        /// </summary>
        /// <param name="content">The content to convert.</param>
        /// <returns>The content as a JSON object.</returns>
        protected JObject ConvertHttpContentToJson(HttpContent content)
        {
            String contentAsString = content.ReadAsStringAsync().Result;
            JObject returnJson = null;
            try
            {
                returnJson = JObject.Parse(contentAsString);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to convert HttpContent to JSON.", e);
            }

            return returnJson;
        }

        /// <summary>
        /// Asserts that the specified JSON object is a serialized <see cref="HttpErrorResponse"/> with the specified 'code' and 'message' properties.
        /// </summary>
        /// <param name="jsonObject">The JSON object to check.</param>
        /// <param name="expectedCode">The expected value of the 'code' property.</param>
        /// <param name="expectedMessage">The expected value of the 'message' property.</param>
        protected void AssertJsonIsHttpErrorResponse(JObject jsonObject, String expectedCode, String expectedMessage)
        {
            if (jsonObject["error"] == null)
                Assert.Fail("The specified JSON object is not an HttpErrorResponse as it doesn't contain an 'error' property.");
            if (jsonObject["error"]["code"] == null)
                Assert.Fail("The specified JSON object is not an HttpErrorResponse as it doesn't contain a 'code' property.");
            if (jsonObject["error"]["message"] == null)
                Assert.Fail("The specified JSON object is not an HttpErrorResponse as it doesn't contain a 'message' property.");
            String actualCodeValue = jsonObject["error"]["code"].ToString();
            String actualMessageValue = jsonObject["error"]["message"].ToString();
            Assert.AreEqual(expectedCode, actualCodeValue);
            Assert.AreEqual(expectedMessage, actualMessageValue);
        }

        /// <summary>
        /// Asserts that the specified serialized <see cref="HttpErrorResponse"/> JSON object contains a value in the 'attributes' property with the specified name and value.
        /// </summary>
        /// <param name="jsonObject">The serialized <see cref="HttpErrorResponse"/> JSON object to check.</param>
        /// <param name="expectedAttributeName">The expected attribute name.</param>
        /// <param name="expectedAttributeValue">The expected attribute value.</param>
        protected void AssertHttpErrorResponseJsonContainsAttribute(JObject jsonObject, String expectedAttributeName, String expectedAttributeValue)
        {
            if (jsonObject["error"]["attributes"] == null)
                Assert.Fail("The specified JSON object doesn't contain an 'attributes' property.");
            var attributesValue = (JArray)jsonObject["error"]["attributes"];
            Boolean valueFound = false;
            foreach (JObject currentAttribute in attributesValue)
            {
                if (currentAttribute["name"].ToString() == expectedAttributeName && currentAttribute["value"].ToString() == expectedAttributeValue)
                {
                    valueFound = true;
                    break;
                }
            }
            if (valueFound == false)
            {
                Assert.Fail($"Could not find attribute with name '{expectedAttributeName}' and value '{expectedAttributeValue}' in HttpErrorResponse JSON.");
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="ReaderWriter.Program"/> which instantiates a hosted version of the <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        protected class TestReaderWriter : WebApplicationFactory<ReaderWriter.Program>
        {
            /// <inheritdoc/>
            protected override IHost CreateHost(IHostBuilder builder)
            {
                builder.ConfigureServices((IServiceCollection services) =>
                {
                });
                builder.UseEnvironment(ReaderWriter.Program.IntegrationTestingEnvironmentName);

                return base.CreateHost(builder);
            }
        }

        #endregion
    }
}
