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
using System.Threading.Tasks;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.DistributedAccessManagerOperationCoordinator class.
    /// </summary>
    public class DistributedAccessManagerOperationCoordinatorTests
    {
        private IShardClientManager<AccessManagerRestClientConfiguration> mockShardClientManager;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> testOperationCoordinator;

        [SetUp]
        protected void SetUp()
        {
            mockShardClientManager = Substitute.For<IShardClientManager<AccessManagerRestClientConfiguration>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testOperationCoordinator = new DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration>(mockShardClientManager, mockMetricLogger);
        }

        [Test]
        public async Task GetUsersAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            clientsAndDescriptions[0].Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2", "user3" }));
            clientsAndDescriptions[1].Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(new List<String>()));
            clientsAndDescriptions[2].Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "user4", "user5", "user6" }));
            var allClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1], clientsAndDescriptions[2] };
            mockMetricLogger.Begin(Arg.Any<UsersPropertyQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(allClients);

            var result = new List<String>(await testOperationCoordinator.GetUsersAsync());

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetUsersAsync();
            await clientsAndDescriptions[1].Client.Received(1).GetUsersAsync();
            await clientsAndDescriptions[2].Client.Received(1).GetUsersAsync();
            mockMetricLogger.Received(1).Begin(Arg.Any<UsersPropertyQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UsersPropertyQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UsersPropertyQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
            Assert.IsTrue(result.Contains("user6"));
        }

        [Test]
        public async Task GetUsersAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            clientsAndDescriptions[0].Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2", "user3" }));
            clientsAndDescriptions[1].Client.GetUsersAsync().Returns(Task.FromException<List<String>>(mockException));
            clientsAndDescriptions[2].Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "user4", "user5", "user6" }));
            var allClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1], clientsAndDescriptions[2] };
            mockMetricLogger.Begin(Arg.Any<UsersPropertyQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(allClients);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetUsersAsync();
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetUsersAsync();
            await clientsAndDescriptions[1].Client.Received(1).GetUsersAsync();
            await clientsAndDescriptions[2].Client.Received(1).GetUsersAsync();
            mockMetricLogger.Received(1).Begin(Arg.Any<UsersPropertyQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UsersPropertyQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith("Failed to retrieve users from shard with configuration 'ShardDescription2'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(6);
            clientsAndDescriptions[0].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2", "group3" }));
            clientsAndDescriptions[1].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>()));
            clientsAndDescriptions[2].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group3", "group2" }));
            clientsAndDescriptions[3].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group2" }));
            clientsAndDescriptions[4].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group2" }));
            clientsAndDescriptions[5].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group4" }));
            mockMetricLogger.Begin(Arg.Any<GroupsPropertyQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[4], clientsAndDescriptions[5] });

            var result = new List<String>(await testOperationCoordinator.GetGroupsAsync());

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[1].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[2].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[3].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[4].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[5].Client.Received(1).GetGroupsAsync();
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupsPropertyQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupsPropertyQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupsPropertyQuery>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
        }

        [Test]
        public async Task GetGroupsAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(6);
            clientsAndDescriptions[0].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2", "group3" }));
            clientsAndDescriptions[1].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>()));
            clientsAndDescriptions[2].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group3", "group2" }));
            clientsAndDescriptions[3].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group2" }));
            clientsAndDescriptions[4].Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group2" }));
            clientsAndDescriptions[5].Client.GetGroupsAsync().Returns(Task.FromException<List<String>>(mockException));
            mockMetricLogger.Begin(Arg.Any<GroupsPropertyQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[4], clientsAndDescriptions[5] });

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupsAsync();
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[1].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[2].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[3].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[4].Client.Received(1).GetGroupsAsync();
            await clientsAndDescriptions[5].Client.Received(1).GetGroupsAsync();
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupsPropertyQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupsPropertyQueryTime>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith("Failed to retrieve groups from shard with configuration 'ShardDescription6'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityTypesAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            clientsAndDescriptions[0].Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "ClientAccount", "BusinessUnit" }));
            clientsAndDescriptions[1].Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "ClientAccount" } ));
            clientsAndDescriptions[2].Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "BusinessUnit", "ClientAccount" }));
            mockMetricLogger.Begin(Arg.Any<EntityTypesPropertyQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });

            var result = new List<String>(await testOperationCoordinator.GetEntityTypesAsync());

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetEntityTypesAsync();
            await clientsAndDescriptions[1].Client.Received(1).GetEntityTypesAsync();
            await clientsAndDescriptions[2].Client.Received(1).GetEntityTypesAsync();
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypesPropertyQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypesPropertyQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypesPropertyQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("ClientAccount"));
            Assert.IsTrue(result.Contains("BusinessUnit"));
        }

        [Test]
        public async Task GetEntityTypesAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(6);
            clientsAndDescriptions[0].Client.GetEntityTypesAsync().Returns(Task.FromException<List<String>>(mockException));
            clientsAndDescriptions[1].Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "ClientAccount" }));
            clientsAndDescriptions[2].Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(new List<String>() { "BusinessUnit", "ClientAccount" }));
            mockMetricLogger.Begin(Arg.Any<EntityTypesPropertyQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityTypesAsync();
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetEntityTypesAsync();
            await clientsAndDescriptions[1].Client.Received(1).GetEntityTypesAsync();
            await clientsAndDescriptions[2].Client.Received(1).GetEntityTypesAsync();
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypesPropertyQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityTypesPropertyQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith("Failed to retrieve entity types from shard with configuration 'ShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddUserAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.AddUserAsync(testUser);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddUserAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.AddUserAsync(testUser).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add user '{testUser}' to shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsUserAsync()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            clientAndDescription.Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(true));
            mockMetricLogger.Begin(Arg.Any<ContainsUserQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);

            Boolean result = await testOperationCoordinator.ContainsUserAsync(testUser);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).ContainsUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsUserQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(true, result);


            clientAndDescription.Client.ClearReceivedCalls();
            mockMetricLogger.ClearReceivedCalls();
            mockShardClientManager.ClearReceivedCalls();
            clientAndDescription.Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));

            result = await testOperationCoordinator.ContainsUserAsync(testUser);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).ContainsUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsUserQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(false, result);
        }

        [Test]
        public async Task ContainsUserAsync_ExceptionWhenChecking()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            clientAndDescription.Client.ContainsUserAsync(testUser).Returns(Task.FromException<Boolean>(mockException));
            mockMetricLogger.Begin(Arg.Any<ContainsUserQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.ContainsUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).ContainsUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ContainsUserQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for user '{testUser}' in shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserAsync()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.RemoveUserAsync(testUser);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserRemoved>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveUserAsync_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.RemoveUserAsync(testUser).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove user '{testUser}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddGroupAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientsAndDescriptions[0]);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });

            await testOperationCoordinator.AddGroupAsync(testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).AddGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).AddGroupAsync(testGroup);
            await clientsAndDescriptions[2].Client.Received(1).AddGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddGroupAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            var mockException = new Exception("Mock exception");
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientsAndDescriptions[0]);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });
            clientsAndDescriptions[1].Client.AddGroupAsync(testGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Event);
            await clientsAndDescriptions[1].Client.Received(1).AddGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupAddTime>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add group '{testGroup}' to shard with configuration 'ShardDescription2'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsGroupAsync()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] };
            var groupClient = clientsAndDescriptions[2];
            var groupToGroupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[3] };
            clientsAndDescriptions[0].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            clientsAndDescriptions[3].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            mockMetricLogger.Begin(Arg.Any<ContainsGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(groupClient);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupClients);

            Boolean result = await testOperationCoordinator.ContainsGroupAsync(testGroup);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[2].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[3].Client.Received(1).ContainsGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsGroupQuery>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(true, result);


            clientsAndDescriptions[0].Client.ClearReceivedCalls();
            clientsAndDescriptions[1].Client.ClearReceivedCalls();
            clientsAndDescriptions[2].Client.ClearReceivedCalls();
            clientsAndDescriptions[3].Client.ClearReceivedCalls();
            mockMetricLogger.ClearReceivedCalls();
            mockShardClientManager.ClearReceivedCalls();
            clientsAndDescriptions[0].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[3].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));

            result = await testOperationCoordinator.ContainsGroupAsync(testGroup);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[2].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[3].Client.Received(1).ContainsGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsGroupQuery>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(false, result);
        }

        [Test]
        public async Task ContainsGroupAsync_ExceptionWhenChecking()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] };
            var groupClient = clientsAndDescriptions[2];
            var groupToGroupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[3] };
            var mockException = new Exception("Mock exception");
            clientsAndDescriptions[0].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsGroupAsync(testGroup).Returns(Task.FromException<Boolean>(mockException));
            clientsAndDescriptions[3].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            mockMetricLogger.Begin(Arg.Any<ContainsGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(groupClient);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupClients);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.ContainsGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await clientsAndDescriptions[2].Client.Received(1).ContainsGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ContainsGroupQueryTime>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for group '{testGroup}' in shard with configuration 'ShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupAsync()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(5);
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[3], clientsAndDescriptions[4] });

            await testOperationCoordinator.RemoveGroupAsync(testGroup);
            
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[2].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[3].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[4].Client.Received(1).RemoveGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupAsync_ExceptionWhenRemoving()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(5);
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[3], clientsAndDescriptions[4] });
            clientsAndDescriptions[3].Client.RemoveGroupAsync(testGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[2].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[3].Client.Received(1).RemoveGroupAsync(testGroup);
            await clientsAndDescriptions[4].Client.Received(1).RemoveGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupRemoveTime>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove group '{testGroup}' from shard with configuration 'ShardDescription4'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.AddUserToGroupMappingAsync(testUser, testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserToGroupMappingAsync(testUser, testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingAdded>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddUserToGroupMappingAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testUser = "user1";
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.AddUserToGroupMappingAsync(testUser, testGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddUserToGroupMappingAsync(testUser, testGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserToGroupMappingAsync(testUser, testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToGroupMappingAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add a mapping between user '{testUser}' and group '{testGroup}' to shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2", "group3" }));

            List<String> result = await testOperationCoordinator.GetUserToGroupMappingsAsync(testUser, false);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterFalseUserDoesntExist()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetUserToGroupMappingsAsync(testUser, false);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToGroupMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterFalseExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetUserToGroupMappingsAsync(testUser, false);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToGroupMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to group mappings for user '{testUser}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            // Current (i.e. as of 2023-12) version of ApplicationAccess doesn't support sharding of the group to group mapping node, BUT the DistributedAccessManagerOperationCoordinator
            //   class supports querying multiple group to group mapping shards.  Hence testing that funcionality here.
            var groupToGroupMappingShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShard1Description"
            );
            var groupToGroupMappingShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShard2Description"
            );
            String testUser = "user1";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group5", "group4" };
            var grouptoGroupMappingShard1directlyMappedGroups = new List<String>() { "group2", "group1", "group4" };
            var grouptoGroupMappingShard2directlyMappedGroups = new List<String>() { "group3", "group5" };
            var directlyMappedGroupsAndGroupToGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShard1ClientAndDescription,
                    grouptoGroupMappingShard1directlyMappedGroups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShard2ClientAndDescription,
                    grouptoGroupMappingShard2directlyMappedGroups
                )
            };
            var groupToGroupMappingShard1IndirectlyMappedGroups = new List<String>() { "group6", "group2", "group1", "group4", "group3" };
            var groupToGroupMappingShard2IndirectlyMappedGroups = new List<String>() { "group7", "group3", "group5", "group1" };
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            // Mock the call to the user node to get the directly mapped groups
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClients);
            groupToGroupMappingShard1ClientAndDescription.Client.GetGroupToGroupMappingsAsync(grouptoGroupMappingShard1directlyMappedGroups).Returns(Task.FromResult<List<String>>(groupToGroupMappingShard1IndirectlyMappedGroups));
            groupToGroupMappingShard2ClientAndDescription.Client.GetGroupToGroupMappingsAsync(grouptoGroupMappingShard2directlyMappedGroups).Returns(Task.FromResult<List<String>>(groupToGroupMappingShard2IndirectlyMappedGroups));

            List<String> result = await testOperationCoordinator.GetUserToGroupMappingsAsync(testUser, true);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
            Assert.IsTrue(result.Contains("group6"));
            Assert.IsTrue(result.Contains("group7"));
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShard1ClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(grouptoGroupMappingShard1directlyMappedGroups);
            await groupToGroupMappingShard2ClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(grouptoGroupMappingShard2directlyMappedGroups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetUserToGroupMappingsGroupsMappedToUser>(), 7);
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueUserDoesntExist()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            // Mock the call to the user node to get the directly mapped groups
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetUserToGroupMappingsAsync(testUser, true);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingUserShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            // Mock the call to the user node to get the directly mapped groups
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetUserToGroupMappingsAsync(testUser, true);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to group mappings for user '{testUser}' from shard with configuration 'UserShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingGroupToGroupMappings()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            // Current (i.e. as of 2023-12) version of ApplicationAccess doesn't support sharding of the group to group mapping node, BUT the DistributedAccessManagerOperationCoordinator
            //   class supports querying multiple group to group mapping shards.  Hence testing that funcionality here.
            var groupToGroupMappingShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShard1Description"
            );
            var groupToGroupMappingShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShard2Description"
            );
            String testUser = "user1";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group5", "group4" };
            var grouptoGroupMappingShard1directlyMappedGroups = new List<String>() { "group2", "group1", "group4" };
            var grouptoGroupMappingShard2directlyMappedGroups = new List<String>() { "group3", "group5" };
            var directlyMappedGroupsAndGroupToGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShard1ClientAndDescription,
                    grouptoGroupMappingShard1directlyMappedGroups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShard2ClientAndDescription,
                    grouptoGroupMappingShard2directlyMappedGroups
                )
            };
            var groupToGroupMappingShard1IndirectlyMappedGroups = new List<String>() { "group6", "group2", "group1", "group4", "group3" };
            var groupToGroupMappingShard2IndirectlyMappedGroups = new List<String>() { "group7", "group3", "group5", "group1" };
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            // Mock the call to the user node to get the directly mapped groups
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClients);
            groupToGroupMappingShard1ClientAndDescription.Client.GetGroupToGroupMappingsAsync(grouptoGroupMappingShard1directlyMappedGroups).Returns(Task.FromResult<List<String>>(groupToGroupMappingShard1IndirectlyMappedGroups));
            groupToGroupMappingShard2ClientAndDescription.Client.GetGroupToGroupMappingsAsync(grouptoGroupMappingShard2directlyMappedGroups).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetUserToGroupMappingsAsync(testUser, true);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShard2ClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(grouptoGroupMappingShard2directlyMappedGroups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group mappings from shard with configuration 'GroupToGroupMappingShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testGroup = "group1";
            var testGroupAsList = new List<String>() { testGroup };
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2", "user3" }));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4", "user3" }));

            List<String> result = await testOperationCoordinator.GetGroupToUserMappingsAsync(testGroup, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_IncludeIndirectMappingsParameterFalseExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testGroup = "group1";
            var testGroupAsList = new List<String>() { testGroup };
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2", "user3" }));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4", "user3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToUserMappingsAsync(testGroup, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings from shard with configuration 'UserShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testGroup = "group1";
            var testGroupAsList = new List<String>() { testGroup };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testReverseMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            }; 
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2", "user3", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5", "user6" }));

            List<String> result = await testOperationCoordinator.GetGroupToUserMappingsAsync(testGroup, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
            Assert.IsTrue(result.Contains("user6"));
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromGroupToGroupMappingShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testGroup = "group1";
            var testGroupAsList = new List<String>() { testGroup };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToUserMappingsAsync(testGroup, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group reverse mappings from shard with configuration 'GroupToGroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromUserShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testGroup = "group1";
            var testGroupAsList = new List<String>() { testGroup };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testReverseMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToUserMappingsAsync(testGroup, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testReverseMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings from shard with configuration 'UserShard3Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.RemoveUserToGroupMappingAsync(testUser, testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserToGroupMappingAsync(testUser, testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingRemoved>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testUser = "user1";
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.RemoveUserToGroupMappingAsync(testUser, testGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveUserToGroupMappingAsync(testUser, testGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserToGroupMappingAsync(testUser, testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToGroupMappingRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove mapping between user '{testUser}' and group '{testGroup}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testFromGroup = "group1";
            String testToGroup = "group2";
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup).Returns(clientAndDescription);

            await testOperationCoordinator.AddGroupToGroupMappingAsync(testFromGroup, testToGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup);
            await clientAndDescription.Client.Received(1).AddGroupToGroupMappingAsync(testFromGroup, testToGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingAdded>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync_ArgumentExceptionWhenAdding()
        {
            // This should cover 'fromGroup' and 'toGroup' containing the same group and circular references, as both are thrown as ArgumentExceptions

            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testFromGroup = "group1";
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new ArgumentException($"Parameters '{nameof(testFromGroup)}' and '{nameof(testFromGroup)}' cannot contain the same group.", "toGroup");
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup).Returns(clientAndDescription);
            clientAndDescription.Client.AddGroupToGroupMappingAsync(testFromGroup, testFromGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testOperationCoordinator.AddGroupToGroupMappingAsync(testFromGroup, testFromGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup);
            await clientAndDescription.Client.Received(1).AddGroupToGroupMappingAsync(testFromGroup, testFromGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToGroupMappingAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testFromGroup = "group1";
            String testToGroup = "group2";
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup).Returns(clientAndDescription);
            clientAndDescription.Client.AddGroupToGroupMappingAsync(testFromGroup, testToGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddGroupToGroupMappingAsync(testFromGroup, testToGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup);
            await clientAndDescription.Client.Received(1).AddGroupToGroupMappingAsync(testFromGroup, testToGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToGroupMappingAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add a mapping between groups '{testFromGroup}' and '{testToGroup}' to shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToGroupMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group4" }));

            List<String> result = await testOperationCoordinator.GetGroupToGroupMappingsAsync(testGroup, false);

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(testGroup, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_GroupDoesntExistInGroupToGroupMappingShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testGroup = "group1";
            GroupNotFoundException<String> mockException = GenerateGroupNotFoundException(testGroup);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToGroupMappingsAsync(testGroup, false).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetGroupToGroupMappingsAsync(testGroup, false);

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(testGroup, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToGroupMappingsAsync(testGroup, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToGroupMappingsAsync(testGroup, false);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(testGroup, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group mappings for group '{testGroup}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToGroupMappingsAsync(testGroup, true).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group4" }));

            List<String> result = await testOperationCoordinator.GetGroupToGroupMappingsAsync(testGroup, true);

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(testGroup, true);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueGroupDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testGroup = "group1";
            GroupNotFoundException<String> mockException = GenerateGroupNotFoundException(testGroup);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToGroupMappingsAsync(testGroup, true).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetGroupToGroupMappingsAsync(testGroup, true);

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(testGroup, true);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToGroupMappingsAsync(testGroup, true).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToGroupMappingsAsync(testGroup, true);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(testGroup, true);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group mappings for group '{testGroup}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testGroup = "group1";
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            List<String> result = await testOperationCoordinator.GetGroupToGroupReverseMappingsAsync(testGroup, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(testGroup, false);
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(testGroup, false);
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(testGroup, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync_IncludeIndirectMappingsParameterFalseGroupDoesntExistInGroupToGroupMappingShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testGroup = "group1";
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            GroupNotFoundException<String> mockException = GenerateGroupNotFoundException(testGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            List<String> result = await testOperationCoordinator.GetGroupToGroupReverseMappingsAsync(testGroup, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(testGroup, false);
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(testGroup, false);
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(testGroup, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync_IncludeIndirectMappingsParameterFalseExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testGroup = "group1";
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToGroupReverseMappingsAsync(testGroup, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(testGroup, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group reverse mappings from shard with configuration 'GroupToGroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testGroup = "group1";
            var testGroupAsList = new List<String>() { testGroup };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2", "group4", "group3" }));

            List<String> result = await testOperationCoordinator.GetGroupToGroupReverseMappingsAsync(testGroup, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testGroup = "group1";
            var testGroupAsList = new List<String>() { testGroup };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2", "group3", "group5" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList))).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2", "group4", "group3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToGroupReverseMappingsAsync(testGroup, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroupAsList)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group reverse mappings from shard with configuration 'GroupToGroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupToGroupMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testFromGroup = "group1";
            String testToGroup = "group2";
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup).Returns(clientAndDescription);

            await testOperationCoordinator.RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup);
            await clientAndDescription.Client.Received(1).RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingRemoved>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupToGroupMappingAsync_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testFromGroup = "group1";
            String testToGroup = "group2";
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup).Returns(clientAndDescription);
            clientAndDescription.Client.RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.GroupToGroupMapping, Operation.Event, testFromGroup);
            await clientAndDescription.Client.Received(1).RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToGroupMappingRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove mapping between groups '{testFromGroup}' and '{testToGroup}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAdded>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add a mapping between user '{testUser}' application component '{testApplicationComponent}' and access level '{testAccessLevel}' to shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("Summary", "View"),
                new Tuple<String, String>("Order", "Create")
            }));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("Summary", "View")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("Order", "Create")));
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to application component and access level mappings for user '{testUser}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            List<String> result = await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterFalseExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to user mappings from shard with configuration 'UserShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            List<String> result = await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterTrueNoGroupMappingsExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new ArgumentException("A value for the 'groups' parameter or property was not provided.");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.DidNotReceive().GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count); 
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingEntityMappingsFromUserShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to user mappings from shard with configuration 'UserShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingEntityMappingsFromGroupShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to group mappings from shard with configuration 'groupShard3Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromGroupToGroupMappingShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group reverse mappings from shard with configuration 'GroupToGroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingGroupMappingsFromUserShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.GreaterOrEqual(userShard2ClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.GreaterOrEqual(userShard3ClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings from shard with configuration 'UserShard1Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoved>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove mapping between user '{testUser}' application component '{testApplicationComponent}' and access level '{testAccessLevel}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);

            await testOperationCoordinator.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAdded>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add a mapping between group '{testGroup}' application component '{testApplicationComponent}' and access level '{testAccessLevel}' to shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(new List<Tuple<String, String>>() 
            {
                new Tuple<String, String>("Summary", "View"),
                new Tuple<String, String>("Order", "Create")
            }));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("Summary", "View")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("Order", "Create")));
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync_GroupDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testGroup = "group1";
            GroupNotFoundException<String> mockException = GenerateGroupNotFoundException(testGroup);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to application component and access level mappings for group '{testGroup}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            List<String> result = await testOperationCoordinator.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_IncludeIndirectMappingsParameterFalseExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to group mappings from shard with configuration 'GroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testMappedGroups = new List<String>() { "group2", "group3", "group4", "group5" };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group5", "group6" }));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group7" }));

            List<String> result = await testOperationCoordinator.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
            Assert.IsTrue(result.Contains("group6"));
            Assert.IsTrue(result.Contains("group7"));
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueNoGroupMappingsExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new ArgumentException("A value for the 'groups' parameter or property was not provided.");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testMappedGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockShardClientManager.DidNotReceive().GetAllClients(DataElement.GroupToGroupMapping, Operation.Query); 
            await groupToGroupShard1ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard2ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromGroupShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to group mappings from shard with configuration 'GroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromGroupToGroupMappingShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testMappedGroups = new List<String>() { "group2", "group3", "group4", "group5" };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group5", "group6" }));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group7" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count()); 
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group reverse mappings from shard with configuration 'GroupToGroupShard1Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);

            await testOperationCoordinator.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoved>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove mapping between group '{testGroup}' application component '{testApplicationComponent}' and access level '{testAccessLevel}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddEntityTypeAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            String testEntityType = "ClientAccount";
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });

            await testOperationCoordinator.AddEntityTypeAsync(testEntityType);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).AddEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).AddEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).AddEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[3].Client.Received(1).AddEntityTypeAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddEntityTypeAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            var mockException = new Exception("Mock exception");
            String testEntityType = "ClientAccount";
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });
            clientsAndDescriptions[2].Client.AddEntityTypeAsync(testEntityType).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddEntityTypeAsync(testEntityType);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).AddEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).AddEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).AddEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[3].Client.Received(1).AddEntityTypeAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityTypeAddTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add entity type '{testEntityType}' to shard with configuration 'ShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsEntityTypeAsync()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] };
            var groupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] };
            clientsAndDescriptions[0].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));
            clientsAndDescriptions[1].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));
            mockMetricLogger.Begin(Arg.Any<ContainsEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);

            Boolean result = await testOperationCoordinator.ContainsEntityTypeAsync(testEntityType);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsEntityTypeQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsEntityTypeQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityTypeQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(true, result);


            clientsAndDescriptions[0].Client.ClearReceivedCalls();
            clientsAndDescriptions[1].Client.ClearReceivedCalls();
            clientsAndDescriptions[2].Client.ClearReceivedCalls();
            mockMetricLogger.ClearReceivedCalls();
            mockShardClientManager.ClearReceivedCalls();
            clientsAndDescriptions[0].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));

            result = await testOperationCoordinator.ContainsEntityTypeAsync(testEntityType);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsEntityTypeQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsEntityTypeQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityTypeQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(false, result);
        }

        [Test]
        public async Task ContainsEntityTypeAsync_ExceptionWhenChecking()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] };
            var groupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] };
            var mockException = new Exception("Mock exception");
            clientsAndDescriptions[0].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromException<Boolean>(mockException));
            clientsAndDescriptions[1].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            mockMetricLogger.Begin(Arg.Any<ContainsEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.ContainsEntityTypeAsync(testEntityType);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsEntityTypeQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ContainsEntityTypeQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for entity type '{testEntityType}' in shard with configuration 'ShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveEntityTypeAsync()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });

            await testOperationCoordinator.RemoveEntityTypeAsync(testEntityType);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[3].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityTypeAsync_ExceptionWhenRemoving()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });
            clientsAndDescriptions[0].Client.RemoveEntityTypeAsync(testEntityType).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveEntityTypeAsync(testEntityType);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await clientsAndDescriptions[3].Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityTypeRemoveTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove entity type '{testEntityType}' from shard with configuration 'ShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddEntityAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });

            await testOperationCoordinator.AddEntityAsync(testEntityType, testEntity);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[1].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[2].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[3].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddEntityAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            var mockException = new Exception("Mock exception");
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });
            clientsAndDescriptions[0].Client.AddEntityAsync(testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddEntityAsync(testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[1].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[2].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[3].Client.Received(1).AddEntityAsync(testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityAddTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add entity '{testEntity}' with type '{testEntityType}' to shard with configuration 'ShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32"); 
            String testEntityType = "ClientAccount";
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            clientsAndDescriptions[0].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyA", "CompanyB" }));
            clientsAndDescriptions[1].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyA" }));
            clientsAndDescriptions[2].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyB", "CompanyC" }));
            mockMetricLogger.Begin(Arg.Any<GetEntitiesQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });

            var result = new List<String>(await testOperationCoordinator.GetEntitiesAsync(testEntityType));

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetEntitiesAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).GetEntitiesAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).GetEntitiesAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public async Task GetEntitiesAsync_EntityTypeDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testEntityType = "ClientAccount";
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            clientsAndDescriptions[0].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyA", "CompanyB" }));
            clientsAndDescriptions[1].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromException<List<String>>(mockException));
            clientsAndDescriptions[2].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyB", "CompanyC" }));
            mockMetricLogger.Begin(Arg.Any<GetEntitiesQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });

            var result = new List<String>(await testOperationCoordinator.GetEntitiesAsync(testEntityType));

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).GetEntitiesAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).GetEntitiesAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).GetEntitiesAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public async Task GetEntitiesAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testEntityType = "ClientAccount";
            var mockException = new Exception("Mock exception");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            clientsAndDescriptions[0].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyA", "CompanyB" }));
            clientsAndDescriptions[1].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromException<List<String>>(mockException));
            clientsAndDescriptions[2].Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyB", "CompanyC" }));
            mockMetricLogger.Begin(Arg.Any<GetEntitiesQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2] });

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAsync(testEntityType);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[1].Client.Received(1).GetEntitiesAsync(testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entities of type '{testEntityType}' from shard with configuration 'ShardDescription2'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsEntityAsync()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0] };
            var groupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1], clientsAndDescriptions[2] };
            clientsAndDescriptions[0].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            clientsAndDescriptions[2].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            mockMetricLogger.Begin(Arg.Any<ContainsEntityQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);

            Boolean result = await testOperationCoordinator.ContainsEntityAsync(testEntityType, testEntity);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[1].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[2].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsEntityQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsEntityQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(true, result);


            clientsAndDescriptions[0].Client.ClearReceivedCalls();
            clientsAndDescriptions[1].Client.ClearReceivedCalls();
            clientsAndDescriptions[2].Client.ClearReceivedCalls();
            mockMetricLogger.ClearReceivedCalls();
            mockShardClientManager.ClearReceivedCalls();
            clientsAndDescriptions[0].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            result = await testOperationCoordinator.ContainsEntityAsync(testEntityType, testEntity);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[1].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[2].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsEntityQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsEntityQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(false, result);
        }

        [Test]
        public async Task ContainsEntityAsync_ExceptionWhenChecking()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0] };
            var groupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1], clientsAndDescriptions[2] };
            var mockException = new Exception("Mock exception");
            clientsAndDescriptions[0].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));
            clientsAndDescriptions[1].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            mockMetricLogger.Begin(Arg.Any<ContainsEntityQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.ContainsEntityAsync(testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[1].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[2].Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsEntityQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ContainsEntityQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for entity '{testEntity}' with type '{testEntityType}' in shard with configuration 'ShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveEntityAsync()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });

            await testOperationCoordinator.RemoveEntityAsync(testEntityType, testEntity);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[1].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[2].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[3].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityRemoved>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityAsync_ExceptionWhenRemoving()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(4);
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] });
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[2], clientsAndDescriptions[3] });
            clientsAndDescriptions[1].Client.RemoveEntityAsync(testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveEntityAsync(testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[1].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[2].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await clientsAndDescriptions[3].Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityRemoveTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove entity '{testEntity}' with type '{testEntityType}' from shard with configuration 'ShardDescription2'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingAdded>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddUserToEntityMappingAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToEntityMappingAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add a mapping between user '{testUser}' entity type '{testEntityType}' and entity '{testEntity}' to shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetUserToEntityMappingsForUserQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToEntityMappingsAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing")
            }));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetUserToEntityMappingsAsync(testUser);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToEntityMappingsAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToEntityMappingsForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToEntityMappingsForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetUserToEntityMappingsForUserQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToEntityMappingsAsync(testUser).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetUserToEntityMappingsAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToEntityMappingsAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToEntityMappingsForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToEntityMappingsForUserQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetUserToEntityMappingsForUserQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToEntityMappingsAsync(testUser).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetUserToEntityMappingsAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToEntityMappingsAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToEntityMappingsForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToEntityMappingsForUserQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to entity mappings for user '{testUser}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            mockMetricLogger.Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToEntityMappingsAsync(testUser, testEntityType).Returns(Task.FromResult<List<String>>(new List<String>(){ "CompanyC", "CompanyA", "CompanyB"}));

            List<String> result = await testOperationCoordinator.GetUserToEntityMappingsAsync(testUser, testEntityType);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToEntityMappingsAsync(testUser, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToEntityMappingsAsync(testUser, testEntityType).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetUserToEntityMappingsAsync(testUser, testEntityType);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToEntityMappingsAsync(testUser, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToEntityMappingsAsync(testUser, testEntityType).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetUserToEntityMappingsAsync(testUser, testEntityType);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToEntityMappingsAsync(testUser, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            mockMetricLogger.Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.GetUserToEntityMappingsAsync(testUser, testEntityType).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetUserToEntityMappingsAsync(testUser, testEntityType);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await clientAndDescription.Client.Received(1).GetUserToEntityMappingsAsync(testUser, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to entity mappings for user '{testUser}' and entity type '{testEntityType}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterFalseEntityTypeDoesntExistInUserShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterFalseEntityDoesntExistInUserShard()
        {

            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            EntityNotFoundException mockException = GenerateEntityNotFoundException(testEntityType, testEntity);
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterFalseExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToUserMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to user mappings from shard with configuration 'UserShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueEntityTypeDoesntExistInUserShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueEntityDoesntExistInUserShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            EntityNotFoundException mockException = GenerateEntityNotFoundException(testEntityType, testEntity);
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueEntityTypeDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueEntityDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            EntityNotFoundException mockException = GenerateEntityNotFoundException(testEntityType, testEntity);
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard2ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await userShard3ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            Assert.IsTrue(result.Contains("user4"));
            Assert.IsTrue(result.Contains("user5"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueNoGroupMappingsExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new ArgumentException("A value for the 'groups' parameter or property was not provided.");
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.DidNotReceive().GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingEntityMappingsFromUserShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to user mappings from shard with configuration 'UserShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingEntityMappingsFromGroupShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to group mappings from shard with configuration 'groupShard3Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromGroupToGroupMappingShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group reverse mappings from shard with configuration 'GroupToGroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingGroupMappingsFromUserShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard1Description"
            );
            var userShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard2Description"
            );
            var userShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShard3Description"
            );
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "groupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                userShard1ClientAndDescription,
                userShard2ClientAndDescription,
                userShard3ClientAndDescription
            };
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userShardClientsAndDescriptions);
            userShard1ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user1", "user2" }));
            userShard2ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            userShard3ClientAndDescription.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user3" }));
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group1", "group2" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3" }));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testDirectlyMappedGroups = new List<String>() { "group1", "group2", "group3" };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group3", "group4" }));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group5" }));
            var testMappedGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5" };
            userShard1ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            userShard2ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user2", "user4" }));
            userShard3ClientAndDescription.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "user3", "user4", "user5" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);
            });

            mockShardClientManager.Received(2).GetAllClients(DataElement.User, Operation.Query);
            await userShard1ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard2ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShard3ClientAndDescription.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testDirectlyMappedGroups)));
            await userShard1ClientAndDescription.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(4, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, userShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.GreaterOrEqual(userShard2ClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.GreaterOrEqual(userShard3ClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings from shard with configuration 'UserShard1Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);

            await testOperationCoordinator.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingRemoved>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, testUser).Returns(clientAndDescription);
            clientAndDescription.Client.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Event, testUser);
            await clientAndDescription.Client.Received(1).RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToEntityMappingRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove mapping between user '{testUser}' entity type '{testEntityType}' and entity '{testEntity}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);

            await testOperationCoordinator.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingAdded>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToEntityMappingAddTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to add a mapping between group '{testGroup}' entity type '{testEntityType}' and entity '{testEntity}' to shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToEntityMappingsAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing")
            }));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToEntityMappingsAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync_GroupDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testGroup = "group1";
            GroupNotFoundException<String> mockException = GenerateGroupNotFoundException(testGroup);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToEntityMappingsAsync(testGroup).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToEntityMappingsAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToEntityMappingsAsync(testGroup).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToEntityMappingsAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToEntityMappingsForGroupQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to entity mappings for group '{testGroup}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            mockMetricLogger.Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToEntityMappingsAsync(testGroup, testEntityType).Returns(Task.FromResult<List<String>>(new List<String>() { "CompanyC", "CompanyA", "CompanyB" }));

            List<String> result = await testOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup, testEntityType);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToEntityMappingsAsync(testGroup, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload_GroupDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            GroupNotFoundException<String> mockException = GenerateGroupNotFoundException(testGroup);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToEntityMappingsAsync(testGroup, testEntityType).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup, testEntityType);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToEntityMappingsAsync(testGroup, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToEntityMappingsAsync(testGroup, testEntityType).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup, testEntityType);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToEntityMappingsAsync(testGroup, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            mockMetricLogger.Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.GetGroupToEntityMappingsAsync(testGroup, testEntityType).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup, testEntityType);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Query, testGroup);
            await clientAndDescription.Client.Received(1).GetGroupToEntityMappingsAsync(testGroup, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to entity mappings for group '{testGroup}' and entity type '{testEntityType}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            List<String> result = await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterFalseEntityTypeDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            List<String> result = await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterFalseEntityDoesntExistInGroupShard()
        {

            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            EntityNotFoundException mockException = GenerateEntityNotFoundException(testEntityType, testEntity);
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            List<String> result = await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterFalseExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToGroupMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to group mappings from shard with configuration 'GroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testMappedGroups = new List<String>() { "group2", "group3", "group4", "group5" };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group5", "group6" }));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group7" }));

            List<String> result = await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
            Assert.IsTrue(result.Contains("group6"));
            Assert.IsTrue(result.Contains("group7"));
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueEntityTypeDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testMappedGroups = new List<String>() { "group2", "group3", "group4", "group5" };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group5", "group6" }));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group7" }));

            List<String> result = await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
            Assert.IsTrue(result.Contains("group6"));
            Assert.IsTrue(result.Contains("group7"));
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueEntityDoesntExistInGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            EntityNotFoundException mockException = GenerateEntityNotFoundException(testEntityType, testEntity);
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testMappedGroups = new List<String>() { "group2", "group3", "group4", "group5" };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group5", "group6" }));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group7" }));

            List<String> result = await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard2ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard3ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
            Assert.IsTrue(result.Contains("group5"));
            Assert.IsTrue(result.Contains("group6"));
            Assert.IsTrue(result.Contains("group7"));
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueNoGroupMappingsExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription
            };
            var mockException = new ArgumentException("A value for the 'groups' parameter or property was not provided.");
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            var testMappedGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));

            List<String> result = await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockShardClientManager.DidNotReceive().GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupToGroupShard1ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            await groupToGroupShard2ClientAndDescription.Client.DidNotReceive().GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, groupToGroupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromGroupShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to group mappings from shard with configuration 'GroupShard2Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReadingFromGroupToGroupMappingShards()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard1Description"
            );
            var groupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard2Description"
            );
            var groupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShard3Description"
            );
            var groupToGroupShard1ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard1Description"
            );
            var groupToGroupShard2ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard2Description"
            );
            var groupToGroupShard3ClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupShard3Description"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupShard1ClientAndDescription,
                groupShard2ClientAndDescription,
                groupShard3ClientAndDescription
            };
            mockMetricLogger.Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupShardClientsAndDescriptions);
            groupShard1ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group3", "group5" }));
            groupShard2ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>()));
            groupShard3ClientAndDescription.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { "group2", "group4", "group3" }));
            var testMappedGroups = new List<String>() { "group2", "group3", "group4", "group5" };
            var groupToGroupShardClientsAndDescriptions = new List<DistributedClientAndShardDescription>()
            {
                groupToGroupShard1ClientAndDescription,
                groupToGroupShard2ClientAndDescription,
                groupToGroupShard3ClientAndDescription
            };
            var mockException = new Exception("Mock exception");
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupShardClientsAndDescriptions);
            groupToGroupShard1ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromException<List<String>>(mockException));
            groupToGroupShard2ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group5", "group6" }));
            groupToGroupShard3ClientAndDescription.Client.GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups))).Returns(Task.FromResult<List<String>>(new List<String>() { "group4", "group7" }));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await groupShard1ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard2ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShard3ClientAndDescription.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupToGroupShard1ClientAndDescription.Client.Received(1).GetGroupToGroupReverseMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard2ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShard3ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupShard1ClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group reverse mappings from shard with configuration 'GroupToGroupShard1Description'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);

            await testOperationCoordinator.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingRemoved>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var clientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "ShardDescription"
            );
            var mockException = new Exception("Mock exception");
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientAndDescription);
            clientAndDescription.Client.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            await clientAndDescription.Client.Received(1).RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToEntityMappingRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove mapping between group '{testGroup}' entity type '{testEntityType}' and entity '{testEntity}' from shard with configuration 'ShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, true)]
        [TestCase(true, true, true)]
        public async Task HasAccessToApplicationComponentAsync_ResultTrue(Boolean userShardResult, Boolean groupShard1Result, Boolean groupShard2Result)
        {
            // N.b. there a quite a few permutations of success and failure tests for this method, as it uses the same set of protected methods inside the DistributedAccessManagerOperationCoordinator
            //   class as other similar methods like HasAccessToEntityAsync(), GetApplicationComponentsAccessibleByUserAsync(), GetEntitiesAccessibleByUserAsync(), etc.
            //   Hence full coverage of these protected methods is implemented in tests for the HasAccessToApplicationComponentAsync(), whereas there are just cursory tests for the other similar
            //   methods mentioned.

            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testApplicationComponent = "Order";
            String testAccessLevel= "Create";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(userShardResult));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard1Result));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard2Result));

            Boolean result = await testOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentGroupShardsQueried>(), 2);
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_ResultFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            Boolean result = await testOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentGroupsMappedToUser>(), 0);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToApplicationComponentGroupShardsQueried>(), 0);
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_ExceptionWhenReadingUserToGroupMappings()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            String testUser = "user1";
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to the user node to get the directly mapped groups
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
            });
            
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to group mappings from shard with configuration 'UserShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_ExceptionWhenReadingGroupToGroupMappings()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
            String testUser = "user1";
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to group mappings from shard with configuration 'GroupToGroupMappingShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync_ExceptionWhenCheckingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromException<Boolean>(mockException));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to application component '{testApplicationComponent}' at access level '{testAccessLevel}' in shard with configuration 'GroupShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task HasAccessToEntityAsync_ResultTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupShardsQueried>(), 2);
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_ResultFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).HasAccessToEntityAsync(testUser, testEntityType, testEntity);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to the user node to get the directly mapped groups
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            Boolean result = await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupsMappedToUser>(), 0);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupShardsQueried>(), 0);
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_UserNotMappedToEntityTypeAndResultFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).HasAccessToEntityAsync(testUser, testEntityType, testEntity);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_UserNotMappedToEntityTypeAndResultTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupShardsQueried>(), 2);
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_UserNotMappedToEntityAndResultFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            EntityNotFoundException mockException = GenerateEntityNotFoundException(testEntityType, testEntity);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            Assert.IsFalse(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).HasAccessToEntityAsync(testUser, testEntityType, testEntity);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_UserNotMappedToEntityAndResultTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            EntityNotFoundException mockException = GenerateEntityNotFoundException(testEntityType, testEntity);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<HasAccessToEntityGroupShardsQueried>(), 2);
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsync_ExceptionWhenReadingUserToGroupMappings()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to the user node to get the directly mapped groups
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to group mappings from shard with configuration 'UserShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task HasAccessToEntityAsync_ExceptionWhenCheckingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group2", "group1", "group6", "group4" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);
            // Mock the call to check whether the user has access
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to entity '{testEntity}' with type '{testEntityType}' in shard with configuration 'GroupShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByUserAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4", "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var userClientApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Settings", "Modify")
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
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetApplicationComponentsAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(userClientApplicationComponents));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(testUser);

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "View")));
            Assert.IsTrue(result.Contains(Tuple.Create("Settings", "Modify")));
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "Create")));
            Assert.IsTrue(result.Contains(Tuple.Create("Summary", "View")));
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetApplicationComponentsAccessibleByUserAsync(testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetApplicationComponentsAccessibleByUserGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<GetApplicationComponentsAccessibleByUserGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByUserAsync_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByUserAsync_ExceptionWhenReadingUserToGroupMappings()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to group mappings from shard with configuration 'UserShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByUserAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4", "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var userClientApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Settings", "Modify")
            };
            var groupClient2ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "Create"),
                Tuple.Create("Summary", "View")
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetApplicationComponentsAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(userClientApplicationComponents));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level mappings for user '{testUser}' from shard with configuration 'GroupShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4" };
            var groupClient3Groups = new List<String>() { "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3ApplicationComponents));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetApplicationComponentsAccessibleByGroupAsync(testGroup);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "View")));
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "Create")));
            Assert.IsTrue(result.Contains(Tuple.Create("Summary", "View")));
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByGroupQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetApplicationComponentsAccessibleByGroupGroupsMappedToGroup>(), 5);
            mockMetricLogger.Received(1).Add(Arg.Any<GetApplicationComponentsAccessibleByGroupGroupShardsQueried>(), 3);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupAsync_GroupNotMappedToAnyGroups()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            String testGroup = "group1";
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { };
            var groupClient1Groups = new List<String>() { testGroup };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                )
            };
            var groupClient1ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View")
            };
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            //  Since the groups is not mapped to any other groups, it's expected the parameter below will only contain the parameter group
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetApplicationComponentsAccessibleByGroupAsync(testGroup);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "View")));
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByGroupQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetApplicationComponentsAccessibleByGroupGroupsMappedToGroup>(), 0);
            mockMetricLogger.Received(1).Add(Arg.Any<GetApplicationComponentsAccessibleByGroupGroupShardsQueried>(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4" };
            var groupClient3Groups = new List<String>() { "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetApplicationComponentsAccessibleByGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level mappings for group '{testGroup}' from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4", "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var userClientEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            var groupClient1Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            var groupClient2Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Marketing")
            };
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetEntitiesAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(userClientEntities));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser);

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Sales")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Marketing")));
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetEntitiesAccessibleByUserAsync(testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByUserGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByUserGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsync_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser);
            });
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsync_ExceptionWhenReadingUserToGroupMappings()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            String testUser = "user1";
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser);
            });
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to group mappings from shard with configuration 'UserShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4", "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var userClientEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            var groupClient1Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetEntitiesAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(userClientEntities));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for user '{testUser}' from shard with configuration 'GroupShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncEntityTypeOverload()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4", "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var userClientEntities = new List<String>() { "CompanyA", "CompanyB" };
            var groupClient1Entities = new List<String>() { "CompanyB", "CompanyC" };
            var groupClient2Entities = new List<String>() { "CompanyC", "CompanyD" };
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetEntitiesAccessibleByUserAsync(testUser, testEntityType).Returns(Task.FromResult<List<String>>(userClientEntities));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient2Entities));

            List<String> result = await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            Assert.IsTrue(result.Contains("CompanyD"));
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByUserGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByUserGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncEntityTypeOverload_UserDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            UserNotFoundException<String> mockException = GenerateUserNotFoundException(testUser);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<UserNotFoundException<String>>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncEntityTypeOverload_UserNotMappedToEntityType()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            EntityTypeNotFoundException mockException = GenerateEntityTypeNotFoundException(testEntityType);
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4", "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var groupClient1Entities = new List<String>() { "CompanyB", "CompanyC" };
            var groupClient2Entities = new List<String>() { "CompanyC", "CompanyD" };
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetEntitiesAccessibleByUserAsync(testUser, testEntityType).Returns(Task.FromException<List<String>>(mockException));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient2Entities));

            List<String> result = await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            Assert.IsTrue(result.Contains("CompanyD"));
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByUserGroupsMappedToUser>(), 6);
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByUserGroupShardsQueried>(), 2);
            Assert.AreEqual(2, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncEntityTypeOverload_ExceptionWhenReadingUserToGroupMappings()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.AreEqual(1, userShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve user to group mappings from shard with configuration 'UserShardDescription'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncEntityTypeOverload_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription"
            );
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            var directlyMappedGroups = new List<String>() { "group2", "group3", "group1" };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4", "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var userClientEntities = new List<String>() { "CompanyA" , "CompanyB" };
            var groupClient1Entities = new List<String>() { "CompanyB", "CompanyC" };
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);
            // Mock the call to get the mappings for the user
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, testUser).Returns(userShardClientAndDescription);
            userShardClientAndDescription.Client.GetEntitiesAccessibleByUserAsync(testUser, testEntityType).Returns(Task.FromResult<List<String>>(userClientEntities));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType).Returns(Task.FromException<List<String>>(mockException));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient1Entities));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.GreaterOrEqual(userShardClientAndDescription.Client.ReceivedCalls().Count(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(3, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for user '{testUser}' and entity type '{testEntityType}' from shard with configuration 'GroupShardDescription1'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4" };
            var groupClient3Groups = new List<String>() { "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3Entities));

            List<Tuple<String, String>> result = await testOperationCoordinator.GetEntitiesAccessibleByGroupAsync(testGroup);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Sales")));
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupGroupsMappedToGroup>(), 5);
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupGroupShardsQueried>(), 3);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsync_ExceptionWhenReadingGroupShard()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4" };
            var groupClient3Groups = new List<String>() { "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for group '{testGroup}' from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncEntityTypeOverload()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testEntityType = "ClientAccount";
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4" };
            var groupClient3Groups = new List<String>() { "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var groupClient1Entities = new List<String>() { "CompanyA", "CompanyB" };
            var groupClient2Entities = new List<String>() { "CompanyB", "CompanyC" };
            var groupClient3Entities = new List<String>();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient3Entities));

            List<String> result = await testOperationCoordinator.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType);
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupGroupsMappedToGroup>(), 5);
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupGroupShardsQueried>(), 3);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncEntityTypeOverload_GroupNotMappedToAnyGroups()
        {
            // Test case which covers situation found during bulk/random testing, where if the group in the 'group' parameter is mapped to an entity, but not to any
            //   groups, the entity was not returned.

            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { };
            var groupClient1Groups = new List<String>() { testGroup };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                )
            };
            var groupClient1Entities = new List<String>() { "CompanyA" };
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups)); 
            // Mock the calls the group nodes to get the mappings
            //  Since the groups is not mapped to any other groups, it's expected the parameter below will only contain the parameter group
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient1Entities));

            List<String> result = await testOperationCoordinator.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupGroupsMappedToGroup>(), 0);
            mockMetricLogger.Received(1).Add(Arg.Any<GetEntitiesAccessibleByGroupGroupShardsQueried>(), 1);
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncEntityTypeOverload_ExceptionWhenReading()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Mock exception");
            var groupToGroupMappingShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupToGroupMappingShardDescription"
            );
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
            String testEntityType = "ClientAccount";
            var directlyMappedGroups = new List<String>() { testGroup };
            var directlyMappedGroupsAndGroupToGroupClient = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupToGroupMappingShardClientAndDescription,
                    directlyMappedGroups
                )
            };
            var indirectlyMappedGroups = new List<String>() { "group2", "group3", "group1", "group6", "group5", "group4" };
            var groupClient1Groups = new List<String>() { "group3", "group5" };
            var groupClient2Groups = new List<String>() { "group6", "group4" };
            var groupClient3Groups = new List<String>() { "group2", "group1" };
            var indirectlyMappedGroupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
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
            var groupClient1Entities = new List<String>() { "CompanyA", "CompanyB" };
            var groupClient2Entities = new List<String>() { "CompanyB", "CompanyC" };
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups))).Returns(Task.FromResult<List<String>>(indirectlyMappedGroups));
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, testEntityType).Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, testEntityType).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);
            });

            mockShardClientManager.Received(1).GetClients(DataElement.GroupToGroupMapping, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(directlyMappedGroups)));
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, testEntityType);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            Assert.AreEqual(1, groupToGroupMappingShardClientAndDescription.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for group '{testGroup}' and entity type {testEntityType} from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void RefreshShardConfiguration_ExceptionWhenRefreshing()
        {
            var userQueryClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQueryClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var userQueryShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQueryClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>() { userQueryShardConfiguration }
            );
            var mockException = new Exception("Mock exception");
            mockShardClientManager.When((shardClientManager) => shardClientManager.RefreshConfiguration(testShardConfigurationSet)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<ShardConfigurationRefreshException>(delegate
            {
                testOperationCoordinator.RefreshShardConfiguration(testShardConfigurationSet);
            });

            Assert.That(e.Message, Does.StartWith("Failed to refresh shard configuration."));
            Assert.AreSame(mockException, e.InnerException);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Generates a list of <see cref="DistributedClientAndShardDescription"/> objects containing mock clients, and with unique descriptions.
        /// </summary>
        /// <param name="clientCount">The number of <see cref="DistributedClientAndShardDescription"/> objects to generate.</param>
        /// <returns>A list of <see cref="DistributedClientAndShardDescription"/> objects.</returns>
        protected List<DistributedClientAndShardDescription> GenerateDistributedClientAndShardDescriptions(Int32 clientCount)
        {
            if (clientCount < 1)
                throw new ArgumentOutOfRangeException(nameof(clientCount), $"Parameter '{nameof(clientCount)}' with value {clientCount} cannot be less than 1.");

            var returnList = new List<DistributedClientAndShardDescription>();
            for (Int32 i = 0; i <= clientCount; i++)
            {
                var currentClientAndDescription = new DistributedClientAndShardDescription
                (
                    Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                    $"ShardDescription{i + 1}"
                );
                returnList.Add(currentClientAndDescription);
            }

            return returnList;
        }

        /// <summary>
        /// Generates a mock <see cref="UserNotFoundException{T}"/>.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The <see cref="UserNotFoundException{T}"/>.</returns>
        protected UserNotFoundException<String> GenerateUserNotFoundException(String user)
        {
            return new UserNotFoundException<String>($"User '{user}' does not exist.", "user", user);
        }

        /// <summary>
        /// Generates a mock <see cref="GroupNotFoundException{T}"/>.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns>The <see cref="GroupNotFoundException{T}"/>.</returns>
        protected GroupNotFoundException<String> GenerateGroupNotFoundException(String group)
        {
            return new GroupNotFoundException<String>($"Group '{group}' does not exist.", "group", group);
        }

        /// <summary>
        /// Generates a mock <see cref="EntityTypeNotFoundException"/>.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <returns>The <see cref="EntityTypeNotFoundException"/>.</returns>
        protected EntityTypeNotFoundException GenerateEntityTypeNotFoundException(String entityType)
        {
            return new EntityTypeNotFoundException($"Entity type '{entityType}' does not exist.", "entityType", entityType);
        }

        /// <summary>
        /// Generates a mock <see cref="EntityNotFoundException"/>.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The <see cref="EntityNotFoundException"/>.</returns>
        protected EntityNotFoundException GenerateEntityNotFoundException(String entityType, String entity)
        {
            return new EntityNotFoundException($"Entity '{entity}' does not exist.", "entity", entityType, entity);
        }

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
