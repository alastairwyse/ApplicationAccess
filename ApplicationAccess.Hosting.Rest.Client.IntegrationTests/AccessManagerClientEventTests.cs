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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc.Testing;
using ApplicationAccess.Hosting.Rest.AsyncClient;
using ApplicationAccess.Hosting.Rest.Writer.IntegrationTests;
using ApplicationAccess.UnitTests;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;
using Polly;

namespace ApplicationAccess.Hosting.Rest.Client.IntegrationTests
{
    /// <summary>
    /// Integration tests for event methods in the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
    /// </summary>
    public class AccessManagerClientEventTests : WriterIntegrationTestsBase
    {
        private const String urlReservedCharcters = "! * ' ( ) ; : @ & = + $ , / ? % # [ ]";

        private Uri testBaseUrl;
        private MethodCallCountingStringUniqueStringifier userStringifier;
        private MethodCallCountingStringUniqueStringifier groupStringifier;
        private MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        private MethodCallCountingStringUniqueStringifier accessLevelStringifier;
        private IApplicationLogger mockLogger;
        private IMetricLogger mockMetricLogger;
        private IAccessManagerEventProcessor<String, String, String, String> testAccessManagerClient;

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
        }

        [OneTimeTearDown]
        protected override void OneTimeTearDown()
        {
            base.OneTimeTearDown();
        }

        [SetUp]
        protected void SetUp()
        {
            testBaseUrl = client.BaseAddress;
            userStringifier = new MethodCallCountingStringUniqueStringifier();
            groupStringifier = new MethodCallCountingStringUniqueStringifier();
            applicationComponentStringifier = new MethodCallCountingStringUniqueStringifier();
            accessLevelStringifier = new MethodCallCountingStringUniqueStringifier();
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerClient = new TestAccessManagerClient<String, String, String, String>
            (
                testBaseUrl,
                client,
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier,
                5,
                1,
                mockLogger,
                mockMetricLogger
            );
            mockEntityEventProcessor.ClearReceivedCalls();
            mockGroupEventProcessor.ClearReceivedCalls();
            mockGroupToGroupEventProcessor.ClearReceivedCalls();
            mockUserEventProcessor.ClearReceivedCalls();
        }

        [TearDown]
        protected void TearDown()
        {
            ((IDisposable)testAccessManagerClient).Dispose();
        }

        [Test]
        public void AddUser()
        {
            const String testUser = "user1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUser(testUser);

            mockUserEventProcessor.Received(1).AddUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUser_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUser(urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUser(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUser()
        {
            const String testUser = "user1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUser(testUser);

            mockUserEventProcessor.Received(1).RemoveUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUser_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUser(urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUser(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroup()
        {
            const String testGroup = "group1";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroup(testGroup);

            mockGroupEventProcessor.Received(1).AddGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroup_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroup(urlReservedCharcters);

            mockGroupEventProcessor.Received(1).AddGroup(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroup()
        {
            const String testGroup = "group1";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroup(testGroup);

            mockGroupEventProcessor.Received(1).RemoveGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroup_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroup(urlReservedCharcters);

            mockGroupEventProcessor.Received(1).RemoveGroup(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            const String testUser = "user1";
            const String testGroup = "group1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToGroupMapping(testUser, testGroup);

            mockUserEventProcessor.Received(1).AddUserToGroupMapping(testUser, testGroup);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToGroupMapping_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToGroupMapping(urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUserToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            const String testUser = "user1";
            const String testGroup = "group1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToGroupMapping(testUser, testGroup);

            mockUserEventProcessor.Received(1).RemoveUserToGroupMapping(testUser, testGroup);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToGroupMapping_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToGroupMapping(urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUserToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            const String testFromGroup = "group1";
            const String testToGroup = "group2";
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockGroupToGroupEventProcessor.Received(1).AddGroupToGroupMapping(testFromGroup, testToGroup);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToGroupMapping_UrlEncoding()
        {
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToGroupMapping(urlReservedCharcters, urlReservedCharcters);

            mockGroupToGroupEventProcessor.Received(1).AddGroupToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            const String testFromGroup = "group1";
            const String testToGroup = "group2";
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToGroupMapping(testFromGroup, testToGroup);

            mockGroupToGroupEventProcessor.Received(1).RemoveGroupToGroupMapping(testFromGroup, testToGroup);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToGroupMapping_UrlEncoding()
        {
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToGroupMapping(urlReservedCharcters, urlReservedCharcters);

            mockGroupToGroupEventProcessor.Received(1).RemoveGroupToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            const String testUser = "user1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);

            mockUserEventProcessor.Received(1).AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUserToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            const String testUser = "user1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);

            mockUserEventProcessor.Received(1).RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUserToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            const String testGroup = "group1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);

            mockGroupEventProcessor.Received(1).AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).AddGroupToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            const String testGroup = "group1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);

            mockGroupEventProcessor.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddEntityType()
        {
            const String testEntityType = "BusinessUnit";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddEntityType(testEntityType);

            mockEntityEventProcessor.Received(1).AddEntityType(testEntityType);
        }

        [Test]
        public void AddEntityType_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddEntityType(urlReservedCharcters);

            mockEntityEventProcessor.Received(1).AddEntityType(urlReservedCharcters);
        }

        [Test]
        public void RemoveEntityType()
        {
            const String testEntityType = "BusinessUnit";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveEntityType(testEntityType);

            mockEntityEventProcessor.Received(1).RemoveEntityType(testEntityType);
        }

        [Test]
        public void RemoveEntityType_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveEntityType(urlReservedCharcters);

            mockEntityEventProcessor.Received(1).RemoveEntityType(urlReservedCharcters);
        }

        [Test]
        public void AddEntity()
        {
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddEntity(testEntityType, testEntity);

            mockEntityEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
        }

        [Test]
        public void AddEntity_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddEntity(urlReservedCharcters, urlReservedCharcters);

            mockEntityEventProcessor.Received(1).AddEntity(urlReservedCharcters, urlReservedCharcters);
        }

        [Test]
        public void RemoveEntity()
        {
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveEntity(testEntityType, testEntity);

            mockEntityEventProcessor.Received(1).RemoveEntity(testEntityType, testEntity);
        }

        [Test]
        public void RemoveEntity_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveEntity(urlReservedCharcters, urlReservedCharcters);

            mockEntityEventProcessor.Received(1).RemoveEntity(urlReservedCharcters, urlReservedCharcters);
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            const String testUser = "user1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockUserEventProcessor.Received(1).AddUserToEntityMapping(testUser, testEntityType, testEntity);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToEntityMapping_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUserToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            const String testUser = "user1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);

            mockUserEventProcessor.Received(1).RemoveUserToEntityMapping(testUser, testEntityType, testEntity);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToEntityMapping_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUserToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            const String testGroup = "group1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockGroupEventProcessor.Received(1).AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToEntityMapping_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).AddGroupToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            const String testGroup = "group1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockGroupEventProcessor.Received(1).RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToEntityMapping_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).RemoveGroupToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RetryOnHttpRequestException()
        {
            using (var testClient = new HttpClient())
            {
                testBaseUrl = new Uri("http://www.acd8aac2-cb88-4296-b604-285f6132e449.com/");
                var testAccessManagerClient = new AccessManagerClient<String, String, String, String>
                (
                    testBaseUrl,
                    testClient,
                    userStringifier,
                    groupStringifier,
                    applicationComponentStringifier,
                    accessLevelStringifier,
                    5,
                    1,
                    mockLogger,
                    mockMetricLogger
                );
                const String testEntityType = "BusinessUnit";
                mockEntityEventProcessor.ClearReceivedCalls();

                var e = Assert.Throws<HttpRequestException>(delegate
                {
                    testAccessManagerClient.RemoveEntityType(testEntityType);
                });

                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 1 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 2 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 3 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 4 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 5 of 5).", Arg.Any<HttpRequestException>());
                mockMetricLogger.Received(5).Increment(Arg.Any<HttpRequestRetried>());
            }
        }

        #region Nested Classes

        /// <summary>
        /// Test version of the <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> class which overrides the SendRequest() method so the class can be tested synchronously using <see cref="WebApplicationFactory{TEntryPoint}"/>.
        /// </summary>
        /// <remarks>Testing the <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> class directly using <see cref="WebApplicationFactory{TEntryPoint}"/> resulted in error "The synchronous method is not supported by 'Microsoft.AspNetCore.TestHost.ClientHandler'".  Judging by the <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.clienthandler?view=aspnetcore-6.0"> documentation for the clienthandler class</see> (which I assume wraps HttpClient calls), it only supports a SendAsync() method.  Given support for the syncronous <see cref="HttpClient.Send(HttpRequestMessage)">HttpClient.Send()</see> was only ontroduced in .NET 5, I'm assuming this is yet to be supported by clients generated via the <see cref="WebApplicationFactory{TEntryPoint}.CreateClient">WebApplicationFactory.CreateClient()</see> method.  Hence, in order to test the class, this class overrides the SendRequest() method to call the HttpClient using the SendAsync() method and 'Result' property.  Although you wouldn't do this in released code (due to risk of deadlocks in certain run contexts outlined <see href="https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d">here</see>, better to test the other functionality in the class (exception handling, response parsing, etc...) than not to test at all.</remarks>
        private class TestAccessManagerClient<TUser, TGroup, TComponent, TAccess> : AccessManagerClient<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval
            )
                : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public TestAccessManagerClient
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
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="exceptionHandingPolicy">Exception handling policy for HttpClient calls.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <remarks>When setting parameter 'exceptionHandingPolicy', note that the web API only returns non-success HTTP status errors in the case of persistent, and non-transient errors (e.g. 400 in the case of bad/malformed requests, and 500 in the case of critical server-side errors).  Retrying the same request after receiving these error statuses will result in an identical response, and hence these statuses are not passed to Polly and will be ignored if included as part of a transient exception handling policy.  Exposing of this parameter is designed to allow overriding of the retry policy and actions when encountering <see cref="HttpRequestException">HttpRequestExceptions</see> caused by network errors, etc.</remarks>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Policy exceptionHandingPolicy,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, exceptionHandingPolicy, logger, metricLogger)
            {
            }

            /// <inheritdoc/>
            protected override void SendRequest(HttpMethod method, Uri requestUrl, Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction)
            {
                Action httpClientAction = () =>
                {
                    using (var request = new HttpRequestMessage(method, requestUrl))
                        try
                        {
                            using (var response = httpClient.SendAsync(request).Result)
                            {
                                responseAction.Invoke(method, requestUrl, response.StatusCode, response.Content.ReadAsStream());
                            }
                        }
                        catch (AggregateException ae)
                        {
                            // Since the SendAsync() method is used above, it will throw an AggregateException on failure which needs to be rethrown as its base exception to be able to properly test retries with the syncronous version of the Polly.Policy used by AccessManagerClient
                            ExceptionDispatchInfo.Capture(ae.GetBaseException()).Throw();
                        }
                };

                exceptionHandingPolicy.Execute(httpClientAction);
            }
        }

        #endregion
    }
}
