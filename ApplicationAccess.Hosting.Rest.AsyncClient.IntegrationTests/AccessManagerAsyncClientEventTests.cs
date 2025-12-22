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
using System.Threading.Tasks;
using ApplicationAccess.Hosting.Rest.Writer.IntegrationTests;
using ApplicationAccess.UnitTests;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.AsyncClient.IntegrationTests
{
    /// <summary>
    /// Integration tests for event methods in the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClient class.
    /// </summary>
    public class AccessManagerAsyncClientEventTests : WriterIntegrationTestsBase
    {
        private const String urlReservedCharcters = "! * ' ( ) ; : @ & = + $ , / ? % # [ ]";

        private Uri testBaseUrl;
        private MethodCallCountingStringUniqueStringifier userStringifier;
        private MethodCallCountingStringUniqueStringifier groupStringifier;
        private MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        private MethodCallCountingStringUniqueStringifier accessLevelStringifier;
        private IApplicationLogger mockLogger;
        private IMetricLogger mockMetricLogger;
        private IAccessManagerAsyncEventProcessor<String, String, String, String> testAccessManagerAsyncClient;

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
            testAccessManagerAsyncClient = new AccessManagerAsyncClient<String, String, String, String>
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
        }

        [TearDown]
        protected void TearDown()
        {
            ((IDisposable)testAccessManagerAsyncClient).Dispose();
        }

        [Test]
        public async Task AddUserAsync()
        {
            const String testUser = "user1";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserAsync(testUser);

            mockUserEventProcessor.Received(1).AddUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddUserAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserAsync(urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUser(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserAsync()
        {
            const String testUser = "user1";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserAsync(testUser);

            mockUserEventProcessor.Received(1).RemoveUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserAsync(urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUser(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupAsync()
        {
            const String testGroup = "group1";
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupAsync(testGroup);

            mockGroupEventProcessor.Received(1).AddGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupAsync_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupAsync(urlReservedCharcters);

            mockGroupEventProcessor.Received(1).AddGroup(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupAsync()
        {
            const String testGroup = "group1";
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupAsync(testGroup);

            mockGroupEventProcessor.Received(1).RemoveGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupAsync_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupAsync(urlReservedCharcters);

            mockGroupEventProcessor.Received(1).RemoveGroup(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync()
        {
            const String testUser = "user1";
            const String testGroup = "group1";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserToGroupMappingAsync(testUser, testGroup);

            mockUserEventProcessor.Received(1).AddUserToGroupMapping(testUser, testGroup);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserToGroupMappingAsync(urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUserToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync()
        {
            const String testUser = "user1";
            const String testGroup = "group1";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserToGroupMappingAsync(testUser, testGroup);

            mockUserEventProcessor.Received(1).RemoveUserToGroupMapping(testUser, testGroup);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserToGroupMappingAsync(urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUserToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync()
        {
            const String testFromGroup = "group1";
            const String testToGroup = "group2";
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupToGroupMappingAsync(testFromGroup, testToGroup);

            mockGroupToGroupEventProcessor.Received(1).AddGroupToGroupMapping(testFromGroup, testToGroup);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync_UrlEncoding()
        {
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupToGroupMappingAsync(urlReservedCharcters, urlReservedCharcters);

            mockGroupToGroupEventProcessor.Received(1).AddGroupToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupToGroupMappingAsync()
        {
            const String testFromGroup = "group1";
            const String testToGroup = "group2";
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup);

            mockGroupToGroupEventProcessor.Received(1).RemoveGroupToGroupMapping(testFromGroup, testToGroup);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupToGroupMappingAsync_UrlEncoding()
        {
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupToGroupMappingAsync(urlReservedCharcters, urlReservedCharcters);

            mockGroupToGroupEventProcessor.Received(1).RemoveGroupToGroupMapping(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            const String testUser = "user1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockUserEventProcessor.Received(1).AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUserToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            const String testUser = "user1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockUserEventProcessor.Received(1).RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUserToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            const String testGroup = "group1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockGroupEventProcessor.Received(1).AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).AddGroupToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            const String testGroup = "group1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockGroupEventProcessor.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddEntityTypeAsync()
        {
            const String testEntityType = "BusinessUnit";
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddEntityTypeAsync(testEntityType);

            mockEntityEventProcessor.Received(1).AddEntityType(testEntityType);
        }

        [Test]
        public async Task AddEntityTypeAsync_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddEntityTypeAsync(urlReservedCharcters);

            mockEntityEventProcessor.Received(1).AddEntityType(urlReservedCharcters);
        }

        [Test]
        public async Task RemoveEntityTypeAsync()
        {
            const String testEntityType = "BusinessUnit";
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveEntityTypeAsync(testEntityType);

            mockEntityEventProcessor.Received(1).RemoveEntityType(testEntityType);
        }

        [Test]
        public async Task RemoveEntityTypeAsync_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveEntityTypeAsync(urlReservedCharcters);

            mockEntityEventProcessor.Received(1).RemoveEntityType(urlReservedCharcters);
        }

        [Test]
        public async Task AddEntityAsync()
        {
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddEntityAsync(testEntityType, testEntity);

            mockEntityEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
        }

        [Test]
        public async Task AddEntityAsync_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddEntityAsync(urlReservedCharcters, urlReservedCharcters);

            mockEntityEventProcessor.Received(1).AddEntity(urlReservedCharcters, urlReservedCharcters);
        }

        [Test]
        public async Task RemoveEntityAsync()
        {
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveEntityAsync(testEntityType, testEntity);

            mockEntityEventProcessor.Received(1).RemoveEntity(testEntityType, testEntity);
        }

        [Test]
        public async Task RemoveEntityAsync_UrlEncoding()
        {
            mockEntityEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveEntityAsync(urlReservedCharcters, urlReservedCharcters);

            mockEntityEventProcessor.Received(1).RemoveEntity(urlReservedCharcters, urlReservedCharcters);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync()
        {
            const String testUser = "user1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockUserEventProcessor.Received(1).AddUserToEntityMapping(testUser, testEntityType, testEntity);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddUserToEntityMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).AddUserToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync()
        {
            const String testUser = "user1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockUserEventProcessor.Received(1).RemoveUserToEntityMapping(testUser, testEntityType, testEntity);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync_UrlEncoding()
        {
            mockUserEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveUserToEntityMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserEventProcessor.Received(1).RemoveUserToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync()
        {
            const String testGroup = "group1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockGroupEventProcessor.Received(1).AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.AddGroupToEntityMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).AddGroupToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync()
        {
            const String testGroup = "group1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockGroupEventProcessor.Received(1).RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync_UrlEncoding()
        {
            mockGroupEventProcessor.ClearReceivedCalls();

            await testAccessManagerAsyncClient.RemoveGroupToEntityMappingAsync(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockGroupEventProcessor.Received(1).RemoveGroupToEntityMapping(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RetryOnHttpRequestException()
        {
            using (var testClient = new HttpClient())
            {
                testBaseUrl = new Uri("http://www.acd8aac2-cb88-4296-b604-285f6132e449.com/");
                var testAccessManagerAsyncClient = new AccessManagerAsyncClient<String, String, String, String>
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

                var e = Assert.ThrowsAsync<HttpRequestException>(async delegate
                {
                    await testAccessManagerAsyncClient.RemoveEntityTypeAsync(testEntityType);
                });

                mockLogger.Received(1).Log(testAccessManagerAsyncClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 1 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerAsyncClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 2 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerAsyncClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 3 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerAsyncClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 4 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerAsyncClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 5 of 5).", Arg.Any<HttpRequestException>());
                mockMetricLogger.Received(5).Increment(Arg.Any<HttpRequestRetried>());
            }
        }
    }
}
