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
using System.Threading.Tasks;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Metrics;
using ApplicationAccess.UnitTests;
using NUnit.Framework;
using NSubstitute;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.DistributedAccessManagerOperationCoordinator class.
    /// </summary>
    public class DistributedAccessManagerOperationCoordinatorTests
    {
        // TODO: Probably want to create a big test set of data... maybe 3 shards per type... can base off ShardClientManagerTests.RefreshConfiguration()

        private IShardClientManager<AccessManagerRestClientConfiguration> mockShardClientManager;
        private IDistributedAccessManagerAsyncClientFactory<AccessManagerRestClientConfiguration, String, String, String, String> mockClientFactory;
        private IHashCodeGenerator<String> mockUserHashCodeGenerator;
        private IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> testOperationCoordinator;

        [SetUp]
        protected void SetUp()
        {
            mockShardClientManager = Substitute.For<IShardClientManager<AccessManagerRestClientConfiguration>>();
            mockClientFactory = Substitute.For<IDistributedAccessManagerAsyncClientFactory<AccessManagerRestClientConfiguration, String, String, String, String>>();
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockClientFactory = Substitute.For<IDistributedAccessManagerAsyncClientFactory<AccessManagerRestClientConfiguration, String, String, String, String>>();
            var initialShardConfiguration = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            testOperationCoordinator = new DistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration>
            (
                initialShardConfiguration,
                mockClientFactory,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockMetricLogger,
                mockShardClientManager
            );
        }

        [TearDown]
        protected void TearDown()
        {
            testOperationCoordinator.Dispose();
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
            clientAndDescription.Client.When(client => client.AddUserAsync(testUser)).Do((callInfo) => throw mockException);

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
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            var allClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1], clientsAndDescriptions[2] };
            clientsAndDescriptions[0].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(true));
            clientsAndDescriptions[2].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));
            mockMetricLogger.Begin(Arg.Any<ContainsUserQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(allClients);

            Boolean result = await testOperationCoordinator.ContainsUserAsync(testUser);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsUserAsync(testUser);
            await clientsAndDescriptions[1].Client.Received(1).ContainsUserAsync(testUser);
            await clientsAndDescriptions[2].Client.Received(1).ContainsUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsUserQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(true, result);


            clientsAndDescriptions[0].Client.ClearReceivedCalls();
            clientsAndDescriptions[1].Client.ClearReceivedCalls();
            clientsAndDescriptions[2].Client.ClearReceivedCalls();
            mockMetricLogger.ClearReceivedCalls();
            mockShardClientManager.ClearReceivedCalls();
            clientsAndDescriptions[0].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));

            result = await testOperationCoordinator.ContainsUserAsync(testUser);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsUserAsync(testUser);
            await clientsAndDescriptions[1].Client.Received(1).ContainsUserAsync(testUser);
            await clientsAndDescriptions[2].Client.Received(1).ContainsUserAsync(testUser);
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
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(3);
            var allClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1], clientsAndDescriptions[2] };
            var mockException = new Exception("Mock exception");
            clientsAndDescriptions[0].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsUserAsync(testUser).Returns(Task.FromException<Boolean>(mockException));
            clientsAndDescriptions[2].Client.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(false));
            mockMetricLogger.Begin(Arg.Any<ContainsUserQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(allClients);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.ContainsUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsUserAsync(testUser);
            await clientsAndDescriptions[1].Client.Received(1).ContainsUserAsync(testUser);
            await clientsAndDescriptions[2].Client.Received(1).ContainsUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<ContainsUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ContainsUserQueryTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for user '{testUser}' in shard with configuration 'ShardDescription2'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserAsync()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(2);
            var allClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] };
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(allClients);

            await testOperationCoordinator.RemoveUserAsync(testUser);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveUserAsync(testUser);
            await clientsAndDescriptions[1].Client.Received(1).RemoveUserAsync(testUser);
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
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(2);
            var allClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0], clientsAndDescriptions[1] };
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(allClients);
            clientsAndDescriptions[1].Client.When(client => client.RemoveUserAsync(testUser)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.RemoveUserAsync(testUser);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).RemoveUserAsync(testUser);
            await clientsAndDescriptions[1].Client.Received(1).RemoveUserAsync(testUser);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserRemoveTime>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove user '{testUser}' from shard with configuration 'ShardDescription2'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddGroupAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(2);
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientsAndDescriptions[0]);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1] });

            await testOperationCoordinator.AddGroupAsync(testGroup);

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).AddGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).AddGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task AddGroupAsync_ExceptionWhenAdding()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            List<DistributedClientAndShardDescription> clientsAndDescriptions = GenerateDistributedClientAndShardDescriptions(2);
            var mockException = new Exception("Mock exception");
            String testGroup = "group1";
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, testGroup).Returns(clientsAndDescriptions[0]);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event).Returns(new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1] });
            clientsAndDescriptions[1].Client.When(client => client.AddGroupAsync(testGroup)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.AddGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetClient(DataElement.Group, Operation.Event, testGroup);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Event);
            await clientsAndDescriptions[0].Client.Received(1).AddGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).AddGroupAsync(testGroup);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupAddTime>());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
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
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0] };
            var groupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1], clientsAndDescriptions[2] };
            var groupToGroupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[3] };
            clientsAndDescriptions[0].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            clientsAndDescriptions[3].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            mockMetricLogger.Begin(Arg.Any<ContainsGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupClients);

            Boolean result = await testOperationCoordinator.ContainsGroupAsync(testGroup);

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
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
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
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
            var userClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[0] };
            var groupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[1], clientsAndDescriptions[2] };
            var groupToGroupClients = new List<DistributedClientAndShardDescription>() { clientsAndDescriptions[3] };
            var mockException = new Exception("Mock exception");
            clientsAndDescriptions[0].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[1].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            clientsAndDescriptions[2].Client.ContainsGroupAsync(testGroup).Returns(Task.FromException<Boolean>(mockException));
            clientsAndDescriptions[3].Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            mockMetricLogger.Begin(Arg.Any<ContainsGroupQueryTime>()).Returns(testBeginId);
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            mockShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query).Returns(groupToGroupClients);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testOperationCoordinator.ContainsGroupAsync(testGroup);
            });

            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            await clientsAndDescriptions[0].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[1].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[2].Client.Received(1).ContainsGroupAsync(testGroup);
            await clientsAndDescriptions[3].Client.Received(1).ContainsGroupAsync(testGroup);
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
            clientsAndDescriptions[3].Client.When(client => client.RemoveGroupAsync(testGroup)).Do((callInfo) => throw mockException);

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
        [Ignore("Wait until implemented")]
        public void AddUserToGroupMappingAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void AddUserToGroupMappingAsync_ExceptionWhenAdding()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetUserToGroupMappingsAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetUserToGroupMappingsAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveUserToGroupMappingAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveUserToGroupMappingAsync_ExceptionWhenRemoving()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void AddGroupToGroupMappingAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void AddGroupToGroupMappingAsync_ExceptionWhenAdding()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetGroupToGroupMappingsAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetGroupToGroupMappingsAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveGroupToGroupMappingAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveGroupToGroupMappingAsync_ExceptionWhenRemoving()
        {
            throw new NotImplementedException();
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
            clientAndDescription.Client.When(client => client.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel)).Do((callInfo) => throw mockException);

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
        [Ignore("Wait until implemented")]
        public void GetUserToApplicationComponentAndAccessLevelMappingsAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetUserToApplicationComponentAndAccessLevelMappingsAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
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
            clientAndDescription.Client.When(client => client.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel)).Do((callInfo) => throw mockException);

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
            clientAndDescription.Client.When(client => client.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel)).Do((callInfo) => throw mockException);

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
        [Ignore("Wait until implemented")]
        public void GetGroupToApplicationComponentAndAccessLevelMappingsAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetGroupToApplicationComponentAndAccessLevelMappingsAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
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
            clientAndDescription.Client.When(client => client.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel)).Do((callInfo) => throw mockException);

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
            clientsAndDescriptions[2].Client.When(client => client.AddEntityTypeAsync(testEntityType)).Do((callInfo) => throw mockException);

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
            clientsAndDescriptions[0].Client.When(client => client.RemoveEntityTypeAsync(testEntityType)).Do((callInfo) => throw mockException);

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
            clientsAndDescriptions[0].Client.When(client => client.AddEntityAsync(testEntityType, testEntity)).Do((callInfo) => throw mockException);

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
            await clientsAndDescriptions[0].Client.Received(1).GetEntitiesAsync(testEntityType);
            await clientsAndDescriptions[1].Client.Received(1).GetEntitiesAsync(testEntityType);
            await clientsAndDescriptions[2].Client.Received(1).GetEntitiesAsync(testEntityType);
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
            clientsAndDescriptions[1].Client.When(client => client.RemoveEntityAsync(testEntityType, testEntity)).Do((callInfo) => throw mockException);

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
            Assert.That(e.Message, Does.StartWith($"Failed to remove entity '{testEntity}' of type '{testEntityType}' from shard with configuration 'ShardDescription2'."));
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
            clientAndDescription.Client.When(client => client.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity)).Do((callInfo) => throw mockException);

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
        [Ignore("Wait until implemented")]
        public void GetUserToEntityMappingsAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetUserToEntityMappingsAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetUserToEntityMappingsAsyncUserAndEntityTypeOverload()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetUserToEntityMappingsAsyncUserAndEntityTypeOverload_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveUserToEntityMappingAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveUserToEntityMappingAsync_ExceptionWhenRemoving()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void AddGroupToEntityMappingAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void AddGroupToEntityMappingAsync_ExceptionWhenAdding()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetGroupToEntityMappingsAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetGroupToEntityMappingsAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveGroupToEntityMappingAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void RemoveGroupToEntityMappingAsync_ExceptionWhenRemoving()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void HasAccessToApplicationComponentAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void HasAccessToApplicationComponentAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void HasAccessToEntityAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void HasAccessToEntityAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetApplicationComponentsAccessibleByUserAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetApplicationComponentsAccessibleByUserAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetApplicationComponentsAccessibleByGroupAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetApplicationComponentsAccessibleByGroupAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByUserAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByUserAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByUserAsyncEntityTypeOverload()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByUserAsyncEntityTypeOverload_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByGroupAsync()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByGroupAsync_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByGroupAsyncEntityTypeOverload()
        {
            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public void GetEntitiesAccessibleByGroupAsyncEntityTypeOverload_ExceptionWhenReading()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void RefreshShardConfiguration_ExceptionWhenRefreshing()
        {
            var userQueryClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQueryClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var userQueryShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQueryClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>(){ userQueryShardConfiguration }
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

        #endregion
    }
}
