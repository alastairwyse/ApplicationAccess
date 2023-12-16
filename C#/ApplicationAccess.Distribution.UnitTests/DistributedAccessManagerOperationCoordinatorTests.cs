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
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Metrics;
using NUnit.Framework;
using NSubstitute;
using ApplicationMetrics;
using System.Text.RegularExpressions;
using ApplicationAccess.Distribution.Metrics;

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
            clientAndDescription.Client.When(client => client.AddUserToGroupMappingAsync(testUser, testGroup)).Do((callInfo) => throw mockException);

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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupQuery>());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
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
        [Ignore("Wait until implemented")]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            //GetUserToGroupMappingsWithIndirectMappingsQueryTime

            throw new NotImplementedException();
        }

        [Test]
        [Ignore("Wait until implemented")]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterTrueExceptionWhenReading()
        {
            throw new NotImplementedException();
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
            clientAndDescription.Client.When(client => client.RemoveUserToGroupMappingAsync(testUser, testGroup)).Do((callInfo) => throw mockException);

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
            clientAndDescription.Client.When(client => client.AddGroupToGroupMappingAsync(testFromGroup, testToGroup)).Do((callInfo) => throw mockException);

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
            clientAndDescription.Client.When(client => client.RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup)).Do((callInfo) => throw mockException);

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
            clientAndDescription.Client.When(client => client.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity)).Do((callInfo) => throw mockException);

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
            clientAndDescription.Client.When(client => client.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity)).Do((callInfo) => throw mockException);

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
            clientAndDescription.Client.When(client => client.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity)).Do((callInfo) => throw mockException);

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

        [Test]
        public async Task HasAccessToApplicationComponentAsync()
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
            userShardClientAndDescription.Client.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            // Mock the call to the user node to get the directly mapped groups
            userShardClientAndDescription.Client.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(directlyMappedGroups));
            // Mock the call to the group to group mapping node to get the indirectly mapped groups
            mockShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, directlyMappedGroups).Returns(directlyMappedGroupsAndGroupToGroupClient);
            groupToGroupMappingShardClientAndDescription.Client.GetGroupToGroupMappingsAsync(directlyMappedGroups).Returns(indirectlyMappedGroups);
            // Mock the calls the group nodes to check whether the user has access
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(indirectlyMappedGroups))).Returns(indirectlyMappedGroupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(result);
            mockShardClientManager.Received(1).GetClient(DataElement.User, Operation.Query, testUser);
            await userShardClientAndDescription.Client.Received(1).HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
            await userShardClientAndDescription.Client.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            await groupToGroupMappingShardClientAndDescription.Client.Received(1).GetGroupToGroupMappingsAsync(directlyMappedGroups);
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


            // TODO: Another test where the user shard HasAccessToApplicationComponentAsync() returns true
            // Quite a few permutations of this... e.g. all clients could return true
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
