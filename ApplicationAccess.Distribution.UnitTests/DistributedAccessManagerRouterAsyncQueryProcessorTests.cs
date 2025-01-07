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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.DistributedAccessManagerRouterAsyncQueryProcessor class.
    /// </summary>
    public class DistributedAccessManagerRouterAsyncQueryProcessorTests
    {
        private IShardClientManager<AccessManagerRestClientConfiguration> mockShardClientManager;
        private IHashCodeGenerator<String> mockUserHashCodeGenerator;
        private IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerRouterAsyncQueryProcessor<AccessManagerRestClientConfiguration> testRouterQueryProcessor;

        [SetUp]
        protected void SetUp()
        {
            mockShardClientManager = Substitute.For<IShardClientManager<AccessManagerRestClientConfiguration>>();
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testRouterQueryProcessor = new DistributedAccessManagerRouterAsyncQueryProcessor<AccessManagerRestClientConfiguration>
            (
                mockShardClientManager,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockMetricLogger
            );
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user1"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testRouterQueryProcessor.GetGroupToUserMappingsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription2.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupsQuery>());
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_ExceptionWhenReadingUserShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user1"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.GetGroupToUserMappingsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings for multiple groups from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupMappingShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription1"
            );
            var groupToGroupMappingShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription2"
            );
            var groupToGroupMappingShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupToGroupMappingClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupToGroupMappingClient2Groups = new List<String>() { "group4", "group5" };
            var groupToGroupMappingClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription1,
                    groupToGroupMappingClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription2,
                    groupToGroupMappingClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription3,
                    groupToGroupMappingClient3Groups
                )
            };
            var groupToGroupMappingClient1ReturnGroups = new List<String>()
            {
                "group7",
                "group8"
            };
            var groupToGroupMappingClient2ReturnGroups = new List<String>()
            {
                "group8",
                "group9"
            };
            var groupToGroupMappingClient3ReturnGroups = new List<String>();
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupToGroupMappingShardClientAndDescription1.Client.GetGroupToGroupMappingsAsync(groupToGroupMappingClient1Groups).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient1ReturnGroups));
            groupToGroupMappingShardClientAndDescription2.Client.GetGroupToGroupMappingsAsync(groupToGroupMappingClient2Groups).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient2ReturnGroups));
            groupToGroupMappingShardClientAndDescription3.Client.GetGroupToGroupMappingsAsync(groupToGroupMappingClient3Groups).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient3ReturnGroups));

            List<String> result = await testRouterQueryProcessor.GetGroupToGroupMappingsAsync(testGroups);

            Assert.AreEqual(9, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
            Assert.IsTrue(result.Contains("group6"));
            Assert.IsTrue(result.Contains("group7"));
            Assert.IsTrue(result.Contains("group8"));
            Assert.IsTrue(result.Contains("group9"));
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupToGroupMappingShardClientAndDescription1.Client.Received(1).GetGroupToGroupMappingsAsync(groupToGroupMappingClient1Groups);
            await groupToGroupMappingShardClientAndDescription2.Client.Received(1).GetGroupToGroupMappingsAsync(groupToGroupMappingClient2Groups);
            await groupToGroupMappingShardClientAndDescription3.Client.Received(1).GetGroupToGroupMappingsAsync(groupToGroupMappingClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupsQuery>());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_ExceptionWhenReadingGroupToGroupMappingShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupToGroupMappingShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription1"
            );
            var groupToGroupMappingShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription2"
            );
            var groupToGroupMappingShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupToGroupMappingClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupToGroupMappingClient2Groups = new List<String>() { "group4", "group5" };
            var groupToGroupMappingClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription1,
                    groupToGroupMappingClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription2,
                    groupToGroupMappingClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription3,
                    groupToGroupMappingClient3Groups
                )
            };
            var groupToGroupMappingClient1ReturnGroups = new List<String>()
            {
                "group7",
                "group8"
            };
            var groupToGroupMappingClient2ReturnGroups = new List<String>()
            {
                "group8",
                "group9"
            };
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupToGroupMappingShardClientAndDescription1.Client.GetGroupToGroupMappingsAsync(groupToGroupMappingClient1Groups).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient1ReturnGroups));
            groupToGroupMappingShardClientAndDescription2.Client.GetGroupToGroupMappingsAsync(groupToGroupMappingClient2Groups).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient2ReturnGroups));
            groupToGroupMappingShardClientAndDescription3.Client.GetGroupToGroupMappingsAsync(groupToGroupMappingClient3Groups).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.GetGroupToGroupMappingsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupToGroupMappingShardClientAndDescription3.Client.Received(1).GetGroupToGroupMappingsAsync(groupToGroupMappingClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group mappings for multiple groups from shard with configuration 'GroupToGroupShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupMappingShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription1"
            );
            var groupToGroupMappingShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription2"
            );
            var groupToGroupMappingShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription3"
            );
            var testGroups = new List<String> { "group9", "group8", "group7", "group6", "group5", "group4", };
            var groupToGroupClients = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupMappingShardClientAndDescription1,
                groupToGroupMappingShardClientAndDescription2,
                groupToGroupMappingShardClientAndDescription3
            };
            var groupToGroupMappingClient1ReturnGroups = new List<String>()
            {
                "group3",
                "group2"
            };
            var groupToGroupMappingClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group1"
            };
            var groupToGroupMappingClient3ReturnGroups = new List<String>();
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupClients);
            groupToGroupMappingShardClientAndDescription1.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient1ReturnGroups));
            groupToGroupMappingShardClientAndDescription2.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient2ReturnGroups));
            groupToGroupMappingShardClientAndDescription3.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient3ReturnGroups));

            List<String> result = await testRouterQueryProcessor.GetGroupToGroupReverseMappingsAsync(testGroups);

            Assert.AreEqual(9, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
            Assert.IsTrue(result.Contains("group6"));
            Assert.IsTrue(result.Contains("group7"));
            Assert.IsTrue(result.Contains("group8"));
            Assert.IsTrue(result.Contains("group9"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupMappingShardClientAndDescription1.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupToGroupMappingShardClientAndDescription2.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupToGroupMappingShardClientAndDescription3.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQuery>());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync_ExceptionWhenReadingGroupToGroupMappingShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupToGroupMappingShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription1"
            );
            var groupToGroupMappingShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription2"
            );
            var groupToGroupMappingShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShardDescription3"
            );
            var testGroups = new List<String> { "group9", "group8", "group7", "group6", "group5", "group4", };
            var groupToGroupClients = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupMappingShardClientAndDescription1,
                groupToGroupMappingShardClientAndDescription2,
                groupToGroupMappingShardClientAndDescription3
            };
            var groupToGroupMappingClient1ReturnGroups = new List<String>()
            {
                "group3",
                "group2"
            };
            var groupToGroupMappingClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group1"
            };
            var groupToGroupMappingClient3ReturnGroups = new List<String>();
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupClients);
            groupToGroupMappingShardClientAndDescription1.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient1ReturnGroups));
            groupToGroupMappingShardClientAndDescription2.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(groupToGroupMappingClient2ReturnGroups));
            groupToGroupMappingShardClientAndDescription3.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.GetGroupToGroupReverseMappingsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupMappingShardClientAndDescription3.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task HasAccessToApplicationComponentAsync_ResultTrue(Boolean groupShard1Result, Boolean groupShard2Result)
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard1Result));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard2Result));

            Boolean result = await testRouterQueryProcessor.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForGroupsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentForGroupsGroupShardsQueried>(), 2);
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_ResultFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testRouterQueryProcessor.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForGroupsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentForGroupsGroupShardsQueried>(), 2);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromException<Boolean> (mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to application component 'Order' at access level 'Create' for multiple groups in shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        [Test]
        public async Task HasAccessToEntityAsync_ResultTrue(Boolean groupShard1Result, Boolean groupShard2Result)
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(groupShard1Result));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(groupShard2Result));

            Boolean result = await testRouterQueryProcessor.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForGroupsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityForGroupsGroupShardsQueried>(), 2);
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_ResultFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testRouterQueryProcessor.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForGroupsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityForGroupsGroupShardsQueried>(), 2);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to entity 'CompanyA' with type 'ClientAccount' for multiple groups in shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Create")
            };
            var groupClient2ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "Create"),
                Tuple.Create("Summary", "View")
            };
            var groupClient3ApplicationComponents = new List<Tuple<String, String>>();
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3ApplicationComponents));

            List<Tuple<String, String>> result = await testRouterQueryProcessor.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "View")));
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "Create")));
            Assert.IsTrue(result.Contains(Tuple.Create("Summary", "View")));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByGroupsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetApplicationComponentsAccessibleByGroupsGroupShardsQueried>(), 3);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Create")
            };
            var groupClient2ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "Create"),
                Tuple.Create("Summary", "View")
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level mappings for multiple groups from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            var groupClient2Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            var groupClient3Entities = new List<Tuple<String, String>>();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3Entities));

            List<Tuple<String, String>> result = await testRouterQueryProcessor.GetEntitiesAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Sales")));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupsGroupShardsQueried>(), 3);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            var groupClient2Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            var groupClient3Entities = new List<Tuple<String, String>>();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.GetEntitiesAccessibleByGroupsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for multiple groups from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncEntityTypeOverload()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var groupClient2Entities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var groupClient3Entities = new List<String>();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient3Entities));

            List<String> result = await testRouterQueryProcessor.GetEntitiesAccessibleByGroupsAsync(testGroups, "ClientAccount");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount");
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount");
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount");
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupsGroupShardsQueried>(), 3);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncEntityTypeOverload_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var groupClient2Entities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var groupClient3Entities = new List<String>();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount").Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testRouterQueryProcessor.GetEntitiesAccessibleByGroupsAsync(testGroups, "ClientAccount");
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount");
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for multiple groups and entity type 'ClientAccount' from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
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
            return (IEnumerable<String> actual) => StringEnumerablesContainSameValues(expected, actual);
        }

        /// <summary>
        /// Checks whether two collections of strings contain the same elements irrespective of their enumeration order.
        /// </summary>
        /// <param name="enumerable1">The first collection.</param>
        /// <param name="enumerable2">The second collection.</param>
        /// <returns>True if the collections contain the same string.  False otherwise.</returns>
        protected Boolean StringEnumerablesContainSameValues(IEnumerable<String> enumerable1, IEnumerable<String> enumerable2)
        {
            if (enumerable1.Count() != enumerable2.Count())
            {
                return false;
            }
            var sortedExpected = new List<String>(enumerable1);
            var sortedActual = new List<String>(enumerable2);
            sortedExpected.Sort();
            sortedActual.Sort();
            for (Int32 i = 0; i < sortedExpected.Count; i++)
            {
                if (sortedExpected[i] != sortedExpected[i])
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
