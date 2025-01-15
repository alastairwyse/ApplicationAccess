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
    /// Unit tests for the ApplicationAccess.Distribution.DistributedAccessManagerOperationRouter class.
    /// </summary>
    public class DistributedAccessManagerOperationRouterTests
    {
        private IShardClientManager<AccessManagerRestClientConfiguration> mockShardClientManager;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> testOperationRouter;

        [SetUp]
        protected void SetUp()
        {
            mockShardClientManager = Substitute.For<IShardClientManager<AccessManagerRestClientConfiguration>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>(mockShardClientManager, mockMetricLogger);
        }

        [Test]
        public async Task GetUsersAsync()
        {
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
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testOperationRouter.GetUsersAsync();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetUsersAsync();
            await userShardClientAndDescription2.Client.Received(1).GetUsersAsync();
            await userShardClientAndDescription3.Client.Received(1).GetUsersAsync();
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetUsersAsync_ExceptionWhenReadingUserShard()
        {
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
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetUsersAsync().Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetUsersAsync();
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetUsersAsync();
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve users from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupssAsync_ReadingUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var userClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var userClient3ReturnGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnGroups));
            userShardClientAndDescription2.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient2ReturnGroups));
            userShardClientAndDescription3.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient3ReturnGroups));

            List<String> result = await testOperationRouter.GetGroupsAsync();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetGroupsAsync();
            await userShardClientAndDescription2.Client.Received(1).GetGroupsAsync();
            await userShardClientAndDescription3.Client.Received(1).GetGroupsAsync();
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupssAsync_ReadingGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var groupClient3ReturnGroups = new List<String>();
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(userClients);
            groupShardClientAndDescription1.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(groupClient3ReturnGroups));

            List<String> result = await testOperationRouter.GetGroupsAsync();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetGroupsAsync();
            await groupShardClientAndDescription2.Client.Received(1).GetGroupsAsync();
            await groupShardClientAndDescription3.Client.Received(1).GetGroupsAsync();
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupsAsync_ExceptionWhenReadingUserShard()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnGroups));
            userShardClientAndDescription2.Client.GetGroupsAsync().Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetGroupsAsync();
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetGroupsAsync();
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve groups from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityTypessAsync_ReadingUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnEntityTypes = new List<String>()
            {
                "ClientAccount", 
                "BusinessUnit"
            };
            var userClient2ReturnEntityTypes = new List<String>()
            {
                "BusinessUnit"
            };
            var userClient3ReturnEntityTypes = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnEntityTypes));
            userShardClientAndDescription2.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient2ReturnEntityTypes));
            userShardClientAndDescription3.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient3ReturnEntityTypes));

            List<String> result = await testOperationRouter.GetEntityTypesAsync();

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("ClientAccount"));
            Assert.IsTrue(result.Contains("BusinessUnit"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetEntityTypesAsync();
            await userShardClientAndDescription2.Client.Received(1).GetEntityTypesAsync();
            await userShardClientAndDescription3.Client.Received(1).GetEntityTypesAsync();
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntityTypessAsync_ReadingGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnEntityTypes = new List<String>()
            {
                "ClientAccount"
            };
            var groupClient2ReturnEntityTypes = new List<String>()
            {
                "BusinessUnit"
            };
            var groupClient3ReturnEntityTypes = new List<String>();
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(groupClient1ReturnEntityTypes));
            groupShardClientAndDescription2.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(groupClient2ReturnEntityTypes));
            groupShardClientAndDescription3.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(groupClient3ReturnEntityTypes));

            List<String> result = await testOperationRouter.GetEntityTypesAsync();

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("ClientAccount"));
            Assert.IsTrue(result.Contains("BusinessUnit"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetEntityTypesAsync();
            await groupShardClientAndDescription2.Client.Received(1).GetEntityTypesAsync();
            await groupShardClientAndDescription3.Client.Received(1).GetEntityTypesAsync();
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntityTypesAsync_ExceptionWhenReadingUserShard()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnEntityTypes = new List<String>()
            {
                "ClientAccount",
                "BusinessUnit"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnEntityTypes));
            userShardClientAndDescription2.Client.GetEntityTypesAsync().Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetEntityTypesAsync();
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetEntityTypesAsync();
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity types from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsGroupAsync_ReadingUserShardsResultTrue()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            userShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.ContainsGroupAsync(testGroup);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).ContainsGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsGroupAsync_ReadingUserShardsResultFalse()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.ContainsGroupAsync(testGroup);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription2.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription3.Client.Received(1).ContainsGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsGroupAsync_ReadingGroupShardsResultTrue()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testGroup = "group1";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testOperationRouter.ContainsGroupAsync(testGroup);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsGroupAsync_ExceptionWhenChecking()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.ContainsGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription2.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription3.Client.Received(1).ContainsGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count()); 
            Assert.That(e.Message, Does.StartWith($"Failed to check for group '{testGroup}' in shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupAsync_ExecutingAgainstUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);

            await testOperationRouter.RemoveGroupAsync(testGroup);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).RemoveGroupAsync(testGroup);
            await userShardClientAndDescription2.Client.Received(1).RemoveGroupAsync(testGroup);
            await userShardClientAndDescription3.Client.Received(1).RemoveGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupAsync_ExecutingAgainstGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testGroup = "group1";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);

            await testOperationRouter.RemoveGroupAsync(testGroup);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).RemoveGroupAsync(testGroup);
            await groupShardClientAndDescription2.Client.Received(1).RemoveGroupAsync(testGroup);
            await groupShardClientAndDescription3.Client.Received(1).RemoveGroupAsync(testGroup);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupAsync_ExceptionWhenExecuting()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription3.Client.RemoveGroupAsync(testGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.RemoveGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).RemoveGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove group '{testGroup}' from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsyncGroupsOverload()
        {
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
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testOperationRouter.GetGroupToUserMappingsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription2.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupToUserMappingsAsyncGroupsOverload_ExceptionWhenReadingUserShard()
        {
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
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetGroupToUserMappingsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings for multiple groups from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync()
        {
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
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShardClientAndDescription2.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsTrue()
        {
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_ExceptionWhenReadingUserShard()
        {
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
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to user mappings from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync()
        {
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
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var groupClient3ReturnGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient3ReturnGroups));

            List<String> result = await testOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShardClientAndDescription2.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappingsAsync_IncludeIndirectMappingsTrue()
        {
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_ExceptionWhenReadingGroupShard()
        {
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
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to group mappings from shard with configuration 'GroupShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsEntityTypeAsync_ReadingUserShardsResultTrue()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));
            userShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.ContainsEntityTypeAsync(testEntityType);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityTypeAsync_ReadingUserShardsResultFalse()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.ContainsEntityTypeAsync(testEntityType);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityTypeAsync_ReadingGroupShardsResultTrue()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testOperationRouter.ContainsEntityTypeAsync(testEntityType);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityTypeAsync_ExceptionWhenChecking()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.ContainsEntityTypeAsync(testEntityType);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for entity type '{testEntityType}' in shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveEntityTypeAsync_ExecutingAgainstUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);

            await testOperationRouter.RemoveEntityTypeAsync(testEntityType);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityTypeAsync_ExecutingAgainstGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);

            await testOperationRouter.RemoveEntityTypeAsync(testEntityType);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await groupShardClientAndDescription2.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await groupShardClientAndDescription3.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityTypeAsync_ExceptionWhenExecuting()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription3.Client.RemoveEntityTypeAsync(testEntityType).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.RemoveEntityTypeAsync(testEntityType);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove entity type '{testEntityType}' from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAsync_ReadingUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "Clients";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var userClient2ReturnEntities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var userClient3ReturnEntities = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient1ReturnEntities));
            userShardClientAndDescription2.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient2ReturnEntities));
            userShardClientAndDescription3.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient3ReturnEntities));

            List<String> result = await testOperationRouter.GetEntitiesAsync(testEntityType);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetEntitiesAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).GetEntitiesAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAsync_ReadingGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "Clients";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var groupClient2ReturnEntities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var groupClient3ReturnEntities = new List<String>();
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(groupClient1ReturnEntities));
            groupShardClientAndDescription2.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(groupClient2ReturnEntities));
            groupShardClientAndDescription3.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(groupClient3ReturnEntities));

            List<String> result = await testOperationRouter.GetEntitiesAsync(testEntityType);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAsync(testEntityType);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAsync(testEntityType);
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAsync_ExceptionWhenReadingUserShard()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "Clients";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient1ReturnEntities));
            userShardClientAndDescription2.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetEntitiesAsync(testEntityType);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entities of type '{testEntityType}' from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsEntityAsync_ReadingUserShardsResultTrue()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            userShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.ContainsEntityAsync(testEntityType, testEntity);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityAsync_ReadingUserShardsResultFalse()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.ContainsEntityAsync(testEntityType, testEntity);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityAsync_ReadingGroupShardsResultTrue()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testOperationRouter.ContainsEntityAsync(testEntityType, testEntity);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityAsync_ExceptionWhenChecking()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.ContainsEntityAsync(testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for entity '{testEntity}' with type '{testEntityType}' in shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveEntityAsync_ExecutingAgainstUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);

            await testOperationRouter.RemoveEntityAsync(testEntityType, testEntity);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription2.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityAsync_ExecutingAgainstGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);

            await testOperationRouter.RemoveEntityAsync(testEntityType, testEntity);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await groupShardClientAndDescription3.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityAsync_ExceptionWhenExecuting()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
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
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription3.Client.RemoveEntityAsync(testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.RemoveEntityAsync(testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove entity '{testEntity}' with type '{testEntityType}' from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync()
        {
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
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShardClientAndDescription2.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShardClientAndDescription3.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToUserMappingsAsync_IncludeIndirectMappingsTrue()
        {
            String testEntityType = "Clients";
            String testEntity = "CompanyA";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_ExceptionWhenReadingUserShard()
        {
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
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to user mappings from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync()
        {
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
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var groupClient3ReturnGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient3ReturnGroups));

            List<String> result = await testOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShardClientAndDescription2.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShardClientAndDescription3.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToGroupMappingsAsync_IncludeIndirectMappingsTrue()
        {
            String testEntityType = "Clients";
            String testEntity = "CompanyA";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_ExceptionWhenReadingGroupShard()
        {
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
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription2.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to group mappings from shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload_ResultTrue(Boolean groupShard1Result, Boolean groupShard2Result)
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard1Result));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard2Result));

            Boolean result = await testOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload_ResultFalse()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload_ExceptionWhenReadingGroupShard()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromException<Boolean> (mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to application component 'Order' at access level 'Create' for multiple groups in shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload_ResultTrue(Boolean groupShard1Result, Boolean groupShard2Result)
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(groupShard1Result));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(groupShard2Result));

            Boolean result = await testOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload_ResultFalse()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload_ExceptionWhenReadingGroupShard()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to entity 'CompanyA' with type 'ClientAccount' for multiple groups in shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3ApplicationComponents));

            List<Tuple<String, String>> result = await testOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "View")));
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "Create")));
            Assert.IsTrue(result.Contains(Tuple.Create("Summary", "View")));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync_ExceptionWhenReadingGroupShard()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level mappings for multiple groups from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3Entities));

            List<Tuple<String, String>> result = await testOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Sales")));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync_ExceptionWhenReadingGroupShard()
        {
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
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for multiple groups from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncEntityTypeOverload()
        {
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
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient3Entities));

            List<String> result = await testOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups, "ClientAccount");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount");
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount");
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount");
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncEntityTypeOverload_ExceptionWhenReadingGroupShard()
        {
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
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount").Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups, "ClientAccount");
            });

            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount");
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
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
