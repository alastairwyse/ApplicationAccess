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
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ApplicationAccess.Distribution;
using ApplicationAccess.Utilities; 
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.DistributedAsyncClient.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
    /// </summary>
    public class AccessManagerDistributedAsyncClientTests
    {
        // TODO: The DistributedReaderNode which is created in this test (via TestDistributedReader) will be trying to connect to an event cache, as configured in the 'appsettings.IntegrationTesting.json'
        //   file in the DistributedReader project, however no event cache exists on the specified port.  Hence I think behind the scenes, Polly in the EventCacheClient will be going through retries as 
        //   tests in this class are processed.  Doesn't seem to be causing an issue at the moment as the tests finish quickly (much quicker than the event cache refresh time), BUT if more tests are
        //   added or the tests are slower, OR the WebApplicationFactory waits for graceful shutdown (i.e. waiting until all Polly retries have failed), then these tests could either start becoming very 
        //   slow, or start failing.
        //   Maybe need to see if I can use and start a second subclass of WebApplicationFactory which hosts an event cache.

        private const String urlReservedCharcters = "! * ' ( ) ; : @ & = + $ , / ? % # [ ]";

        private TestUtilities testUtilities;
        private IAccessManagerEntityQueryProcessor mockEntityQueryProcessor;
        private IAccessManagerGroupQueryProcessor<String, String, String> mockGroupQueryProcessor;
        private IDistributedAccessManagerGroupQueryProcessor<String, String, String> mockDistributedGroupQueryProcessor;
        private IAccessManagerGroupToGroupQueryProcessor<String> mockGroupToGroupQueryProcessor;
        private IDistributedAccessManagerGroupToGroupQueryProcessor<String> mockDistributedGroupToGroupQueryProcessor;
        private IAccessManagerUserQueryProcessor<String, String, String, String> mockUserQueryProcessor;
        private IDistributedAccessManagerUserQueryProcessor<String, String> mockDistributedUserQueryProcessor;
        private TestDistributedReader testDistributedReader;
        private HttpClient client;
        private Uri testBaseUrl;
        private MethodCallCountingStringUniqueStringifier userStringifier;
        private MethodCallCountingStringUniqueStringifier groupStringifier;
        private MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        private MethodCallCountingStringUniqueStringifier accessLevelStringifier;
        private IApplicationLogger mockLogger;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerAsyncClient<String, String, String, String> testDistributedAccessManagerAsyncClient;

        [OneTimeSetUp]
        protected void OneTimeSetUp()
        {
            testUtilities = new TestUtilities();
            mockEntityQueryProcessor = Substitute.For<IAccessManagerEntityQueryProcessor>();
            mockGroupQueryProcessor = Substitute.For<IAccessManagerGroupQueryProcessor<String, String, String>>();
            mockDistributedGroupQueryProcessor = Substitute.For<IDistributedAccessManagerGroupQueryProcessor<String, String, String>>();
            mockGroupToGroupQueryProcessor = Substitute.For<IAccessManagerGroupToGroupQueryProcessor<String>>();
            mockDistributedGroupToGroupQueryProcessor = Substitute.For<IDistributedAccessManagerGroupToGroupQueryProcessor<String>>();
            mockUserQueryProcessor = Substitute.For<IAccessManagerUserQueryProcessor<String, String, String, String>>();
            mockDistributedUserQueryProcessor = Substitute.For<IDistributedAccessManagerUserQueryProcessor<String, String>>();
            testDistributedReader = new TestDistributedReader();
            testDistributedReader.Services.GetService<EntityQueryProcessorHolder>().EntityQueryProcessor = mockEntityQueryProcessor;
            testDistributedReader.Services.GetService<GroupQueryProcessorHolder>().GroupQueryProcessor = mockGroupQueryProcessor;
            testDistributedReader.Services.GetService<DistributedGroupQueryProcessorHolder>().DistributedGroupQueryProcessor = mockDistributedGroupQueryProcessor;
            testDistributedReader.Services.GetService<GroupToGroupQueryProcessorHolder>().GroupToGroupQueryProcessor = mockGroupToGroupQueryProcessor;
            testDistributedReader.Services.GetService<DistributedGroupToGroupQueryProcessorHolder>().DistributedGroupToGroupQueryProcessor = mockDistributedGroupToGroupQueryProcessor;
            testDistributedReader.Services.GetService<UserQueryProcessorHolder>().UserQueryProcessor = mockUserQueryProcessor;
            testDistributedReader.Services.GetService<DistributedUserQueryProcessorHolder>().DistributedUserQueryProcessor = mockDistributedUserQueryProcessor;
            client = testDistributedReader.CreateClient();
        }

        [OneTimeTearDown]
        protected void OneTimeTearDown()
        {
            client.Dispose();
            testDistributedReader.Dispose();
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
            testDistributedAccessManagerAsyncClient = new DistributedAccessManagerAsyncClient<String, String, String, String>
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
            testDistributedAccessManagerAsyncClient.Dispose();
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync()
        {
            var testGroups = new List<String>() { "group3", "group4", "group5" };
            var testMappedUsers = new HashSet<String>() { "user1", "user2", "user3", "user4" };
            mockDistributedGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedUserQueryProcessor.GetGroupToUserMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testMappedUsers);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetGroupToUserMappingsAsync(testGroups);

            mockDistributedUserQueryProcessor.Received(1).GetGroupToUserMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.AreEqual(4, userStringifier.FromStringCallCount);
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_SingleGroupInGroupsParameter()
        {
            var testGroups = new List<String>() { "group5" };
            var testMappedUsers = new HashSet<String>() { "user1", "user2", "user3", "user4" };
            mockDistributedGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedUserQueryProcessor.GetGroupToUserMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testMappedUsers);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetGroupToUserMappingsAsync(testGroups);

            mockDistributedUserQueryProcessor.Received(1).GetGroupToUserMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(4, userStringifier.FromStringCallCount);
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_UrlEncoding()
        {
            // Since all 'groups' parameters are passed in the request body, URL encoding should not affect methods specific to DistributedAccessManagerAsyncClient
            //   However including this test incase that changes in the future

            String urlReservedCharcters1 = "! * ' ( ) ; : @ &";
            String urlReservedCharcters2 = "& = + $ , / ? % # [ ]";
            var testGroups = new List<String>() { urlReservedCharcters1, urlReservedCharcters2 };
            var testMappedUsers = new HashSet<String>() { urlReservedCharcters1, urlReservedCharcters2 };
            mockDistributedGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedUserQueryProcessor.GetGroupToUserMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testMappedUsers);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetGroupToUserMappingsAsync(testGroups);

            mockDistributedUserQueryProcessor.Received(1).GetGroupToUserMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, userStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(urlReservedCharcters1));
            Assert.IsTrue(result.Contains(urlReservedCharcters2));
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync()
        {
            var testGroups = new List<String>() { "group1",  "group2", "group3" };
            var testMappedGroups = new HashSet<String>() { "group1", "group2", "group3", "group4", "group5" };
            mockDistributedGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupToGroupQueryProcessor.GetGroupToGroupMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testMappedGroups);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetGroupToGroupMappingsAsync(testGroups);

            mockDistributedGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.AreEqual(5, groupStringifier.FromStringCallCount);
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_SingleGroupInGroupsParameter()
        {
            var testGroups = new List<String>() { "group1" };
            var testMappedGroups = new HashSet<String>() { "group1", "group2", "group3", "group4", "group5" };
            mockDistributedGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupToGroupQueryProcessor.GetGroupToGroupMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testMappedGroups);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetGroupToGroupMappingsAsync(testGroups);

            mockDistributedGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(5, groupStringifier.FromStringCallCount);
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync()
        {
            var testGroups = new List<String>() { "group3", "group4", "group5" };
            var testMappedGroups = new HashSet<String>() { "group1", "group2", "group3", "group4", "group5" };
            mockDistributedGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupToGroupQueryProcessor.GetGroupToGroupReverseMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testMappedGroups);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetGroupToGroupReverseMappingsAsync(testGroups);

            mockDistributedGroupToGroupQueryProcessor.Received(1).GetGroupToGroupReverseMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.AreEqual(5, groupStringifier.FromStringCallCount);
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync_SingleGroupInGroupsParameter()
        {
            var testGroups = new List<String>() { "group5" };
            var testMappedGroups = new HashSet<String>() { "group1", "group2", "group3", "group4", "group5" };
            mockDistributedGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupToGroupQueryProcessor.GetGroupToGroupReverseMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testMappedGroups);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetGroupToGroupReverseMappingsAsync(testGroups);

            mockDistributedGroupToGroupQueryProcessor.Received(1).GetGroupToGroupReverseMappings(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(5, groupStringifier.FromStringCallCount);
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.HasAccessToApplicationComponent(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testApplicationComponent, testAccessLevel).Returns(true);

            Boolean result = await testDistributedAccessManagerAsyncClient.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToApplicationComponent(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testApplicationComponent, testAccessLevel);
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_UrlEncoding()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.HasAccessToApplicationComponent(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, urlReservedCharcters).Returns(false);

            Boolean result = await testDistributedAccessManagerAsyncClient.HasAccessToApplicationComponentAsync(testGroups, urlReservedCharcters, urlReservedCharcters);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToApplicationComponent(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task HasAccessToEntityAsync()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.HasAccessToEntity(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType, testEntity).Returns(false);

            Boolean result = await testDistributedAccessManagerAsyncClient.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToEntity(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType, testEntity);
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task HasAccessToEntityAsync_UrlEncoding()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.HasAccessToEntity(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, urlReservedCharcters).Returns(true);

            Boolean result = await testDistributedAccessManagerAsyncClient.HasAccessToEntityAsync(testGroups, urlReservedCharcters, urlReservedCharcters);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToEntity(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            var testApplicationComponentsAndAccessLevels = new HashSet<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Modify"),
                Tuple.Create("Order", "Create"),
                Tuple.Create("Summary", "View")
            };
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.GetApplicationComponentsAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testApplicationComponentsAndAccessLevels);

            List<Tuple<String, String>> result = await testDistributedAccessManagerAsyncClient.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);

            mockDistributedGroupQueryProcessor.Received(1).GetApplicationComponentsAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(4, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(4, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "View")));
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "Modify")));
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "Create")));
            Assert.IsTrue(result.Contains(Tuple.Create("Summary", "View")));
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncGroupsOverload()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            var testEntityTypesAndEntities = new HashSet<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales"),
                Tuple.Create("BusinessUnit", "Manufacturing")
            };
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.GetEntitiesAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(testEntityTypesAndEntities);

            List<Tuple<String, String>> result = await testDistributedAccessManagerAsyncClient.GetEntitiesAccessibleByGroupsAsync(testGroups);

            mockDistributedGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Sales")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Manufacturing")));
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncGroupsAndEntityTypeOverload()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            var testEntityType = "ClientAccount";
            var testEntities = new HashSet<String>() { "CompanyA", "CompanyB", "CompanyC" };
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.GetEntitiesAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType).Returns(testEntities);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetEntitiesAccessibleByGroupsAsync(testGroups, testEntityType);

            mockDistributedGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncGroupsAndEntityTypeOverload_UrlEncoding()
        {
            var testGroups = new List<String>() { "group1", "group2", "group3" };
            var testEntities = new HashSet<String>() { "CompanyA", "CompanyB", "CompanyC" };
            mockDistributedGroupQueryProcessor.ClearReceivedCalls();
            mockDistributedGroupQueryProcessor.GetEntitiesAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters).Returns(testEntities);

            List<String> result = await testDistributedAccessManagerAsyncClient.GetEntitiesAccessibleByGroupsAsync(testGroups, urlReservedCharcters);

            mockDistributedGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroups(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters);
            Assert.AreEqual(3, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns an <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/> which checks whether a collection of strings matches the collection in parameter <paramref name="expected"/> irrespective of their enumeration order.
        /// </summary>
        /// <param name="expected">The collection of strings the predicate compares to.</param>
        /// <returns>The <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/>.</returns>
        /// <remarks>Designed to be passed to the 'predicate' parameter of the <see cref="Arg.Any{T}"/> argument matcher.</remarks>
        protected Expression<Predicate<IEnumerable<String>>> EqualIgnoringOrder(IEnumerable<String> expected)
        {
            return testUtilities.EqualIgnoringOrder(expected);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory<DistributedReader.Program>"/> which instantiates a hosted version of the <see cref="DistributedReaderNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        private class TestDistributedReader : WebApplicationFactory<DistributedReader.Program>
        {
            /// <inheritdoc/>
            protected override IHost CreateHost(IHostBuilder builder)
            {
                builder.ConfigureServices((IServiceCollection services) =>
                {
                });
                builder.UseEnvironment(ApplicationInitializer.IntegrationTestingEnvironmentName);

                return base.CreateHost(builder);
            }
        }

        /// <summary>
        /// Implementation of <see cref="IUniqueStringifier{T}"/> which counts the number of calls to the FromString() and ToString() methods.
        /// </summary>
        private class MethodCallCountingStringUniqueStringifier : IUniqueStringifier<String>
        {
            public Int32 FromStringCallCount { get; protected set; }
            public Int32 ToStringCallCount { get; protected set; }

            public MethodCallCountingStringUniqueStringifier()
            {
                FromStringCallCount = 0;
                ToStringCallCount = 0;
            }

            /// <inheritdoc/>
            public String FromString(String stringifiedObject)
            {
                FromStringCallCount++;

                return stringifiedObject;
            }

            /// <inheritdoc/>
            public String ToString(String inputObject)
            {
                ToStringCallCount++;

                return inputObject;
            }
        }

        #endregion
    }
}
