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
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Distribution.Persistence;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.DistributedOperationCoordinatorNode class.
    /// </summary>
    public class DistributedOperationCoordinatorNodeTests
    {
        private IShardClientManager<AccessManagerRestClientConfiguration> mockShardClientManager;
        private IDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy mockShardConfigurationRefreshStrategy;
        private IDistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> mockOperationCoordinator;
        private IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> mockPersister;
        private DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> testDistributedOperationCoordinatorNode;

        [SetUp]
        protected void SetUp()
        {
            mockShardClientManager = Substitute.For<IShardClientManager<AccessManagerRestClientConfiguration>>();
            mockShardConfigurationRefreshStrategy = Substitute.For<IDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy>();
            mockOperationCoordinator = Substitute.For<IDistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration>>();
            mockPersister = Substitute.For<IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>>();
            testDistributedOperationCoordinatorNode = new DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>
            (
                mockShardClientManager,
                mockShardConfigurationRefreshStrategy,
                mockOperationCoordinator,
                mockPersister
            );
        }

        [TearDown]
        public void TearDown()
        {
            testDistributedOperationCoordinatorNode.Dispose();
        }

        [Test]
        public async Task GetUsersAsync()
        {
            var testUsers = new List<String>() { "user1", "user2" };
            mockOperationCoordinator.GetUsersAsync().Returns(Task.FromResult<List<String>>(testUsers));

            List<String> result = await testDistributedOperationCoordinatorNode.GetUsersAsync();

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetUsersAsync();
            Assert.AreSame(testUsers, result);
        }

        [Test]
        public void GetUsersAsync_ShardConfigurationRefreshException()
        {
            // Note: Exceptions can be thrown by method NotifyOperationProcessed() from any of the public methods on DistributedOperationCoordinatorNode, 
            //   however the mechanism is the same in all cases... hence exception case will only be tested here... other success tests will just confirm 
            //   that NotifyOperationProcessed() is called.

            var mockException = new ShardConfigurationRefreshException("Failed to refresh shard configuration in shard client manager.");
            mockShardConfigurationRefreshStrategy.When((refreshStrategy) => refreshStrategy.NotifyOperationProcessed()).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<ShardConfigurationRefreshException>(async delegate
            {
                await testDistributedOperationCoordinatorNode.GetUsersAsync();
            });

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            Assert.AreSame(mockException, e);
        }

        [Test]
        public async Task GetGroupsAsync()
        {
            var testGroups = new List<String>() { "group1", "group2" };
            mockOperationCoordinator.GetGroupsAsync().Returns(Task.FromResult<List<String>>(testGroups));

            List<String> result = await testDistributedOperationCoordinatorNode.GetGroupsAsync();

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetGroupsAsync();
            Assert.AreSame(testGroups, result);
        }

        [Test]
        public async Task GetEntityTypesAsync()
        {
            var testEntityTypes = new List<String>() { "ClientAccount", "BusinessUnit" };
            mockOperationCoordinator.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(testEntityTypes));

            List<String> result = await testDistributedOperationCoordinatorNode.GetEntityTypesAsync();

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetEntityTypesAsync();
            Assert.AreSame(testEntityTypes, result);
        }

        [Test]
        public async Task AddUserAsync()
        {
            String testUser = "user1";

            await testDistributedOperationCoordinatorNode.AddUserAsync(testUser);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddUserAsync(testUser);
        }

        [Test]
        public async Task ContainsUserAsync()
        {
            String testUser = "user1";
            mockOperationCoordinator.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationCoordinatorNode.ContainsUserAsync(testUser);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).ContainsUserAsync(testUser);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveUserAsync()
        {
            String testUser = "user1";

            await testDistributedOperationCoordinatorNode.RemoveUserAsync(testUser);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveUserAsync(testUser);
        }

        [Test]
        public async Task AddGroupAsync()
        {
            String testGroup = "group1";

            await testDistributedOperationCoordinatorNode.AddGroupAsync(testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddGroupAsync(testGroup);
        }

        [Test]
        public async Task ContainsGroupAsync()
        {
            String testGroup = "group1";
            mockOperationCoordinator.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationCoordinatorNode.ContainsGroupAsync(testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).ContainsGroupAsync(testGroup);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveGroupAsync()
        {
            String testGroup = "group1";

            await testDistributedOperationCoordinatorNode.RemoveGroupAsync(testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveGroupAsync(testGroup);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";

            await testDistributedOperationCoordinatorNode.AddUserToGroupMappingAsync(testUser, testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddUserToGroupMappingAsync(testUser, testGroup);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync()
        {
            String testUser = "user1";
            var testGroups = new List<String>() { "group1", "group2" };
            mockOperationCoordinator.GetUserToGroupMappingsAsync(testUser, true).Returns(Task.FromResult<List<String>>(testGroups));

            List<String> result = await testDistributedOperationCoordinatorNode.GetUserToGroupMappingsAsync(testUser, true);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetUserToGroupMappingsAsync(testUser, true);
            Assert.AreSame(testGroups, result);
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";

            await testDistributedOperationCoordinatorNode.RemoveUserToGroupMappingAsync(testUser, testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveUserToGroupMappingAsync(testUser, testGroup);
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";

            await testDistributedOperationCoordinatorNode.AddGroupToGroupMappingAsync(testFromGroup, testToGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddGroupToGroupMappingAsync(testFromGroup, testToGroup);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync()
        {
            String testGroup = "group1";
            var testMappedGroups = new List<String>() { "group2", "group3" };
            mockOperationCoordinator.GetGroupToGroupMappingsAsync(testGroup, true).Returns(Task.FromResult<List<String>>(testMappedGroups));

            List<String> result = await testDistributedOperationCoordinatorNode.GetGroupToGroupMappingsAsync(testGroup, true);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetGroupToGroupMappingsAsync(testGroup, true);
            Assert.AreSame(testMappedGroups, result);
        }

        [Test]
        public async Task RemoveGroupToGroupMappingAsync()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";

            await testDistributedOperationCoordinatorNode.RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveGroupToGroupMappingAsync(testFromGroup, testToGroup);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationCoordinatorNode.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync()
        {
            String testUser = "user";
            var testMappedApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                Tuple.Create("Summary", "View"),
                Tuple.Create("Order", "Create")
            };
            mockOperationCoordinator.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedApplicationComponentsAndAccessLevels));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
            Assert.AreSame(testMappedApplicationComponentsAndAccessLevels, result);
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationCoordinatorNode.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationCoordinatorNode.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync()
        {
            String testGroup = "group";
            var testMappedApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                Tuple.Create("Summary", "View"),
                Tuple.Create("Order", "Create")
            };
            mockOperationCoordinator.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedApplicationComponentsAndAccessLevels));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);
            Assert.AreSame(testMappedApplicationComponentsAndAccessLevels, result);
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationCoordinatorNode.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task AddEntityTypeAsync()
        {
            String testEntityType = "ClientAccount";

            await testDistributedOperationCoordinatorNode.AddEntityTypeAsync(testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddEntityTypeAsync(testEntityType);
        }

        [Test]
        public async Task ContainsEntityTypeAsync()
        {
            String testEntityType = "ClientAccount";
            mockOperationCoordinator.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationCoordinatorNode.ContainsEntityTypeAsync(testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveEntityTypeAsync()
        {
            String testEntityType = "ClientAccount";

            await testDistributedOperationCoordinatorNode.RemoveEntityTypeAsync(testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveEntityTypeAsync(testEntityType);
        }

        [Test]
        public async Task AddEntityAsync()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationCoordinatorNode.AddEntityAsync(testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddEntityAsync(testEntityType, testEntity);
        }

        [Test]
        public async Task GetEntitiesAsync()
        {
            String testEntityType = "ClientAccount";
            var testEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationCoordinator.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(testEntities));

            List<String> result = await testDistributedOperationCoordinatorNode.GetEntitiesAsync(testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreSame(testEntities, result);
        }

        [Test]
        public async Task ContainsEntityAsync()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockOperationCoordinator.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationCoordinatorNode.ContainsEntityAsync(testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveEntityAsync()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationCoordinatorNode.RemoveEntityAsync(testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveEntityAsync(testEntityType, testEntity);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationCoordinatorNode.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync()
        {
            String testUser = "user";
            var testMappedEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            mockOperationCoordinator.GetUserToEntityMappingsAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedEntities));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetUserToEntityMappingsAsync(testUser);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetUserToEntityMappingsAsync(testUser);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload()
        {
            String testUser = "user";
            String testEntityType = "ClientAccount";
            var testMappedEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationCoordinator.GetUserToEntityMappingsAsync(testUser, testEntityType).Returns(Task.FromResult<List<String>>(testMappedEntities));

            List<String> result = await testDistributedOperationCoordinatorNode.GetUserToEntityMappingsAsync(testUser, testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetUserToEntityMappingsAsync(testUser, testEntityType);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationCoordinatorNode.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationCoordinatorNode.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync()
        {
            String testGroup = "group";
            var testMappedEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            mockOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedEntities));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetGroupToEntityMappingsAsync(testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetGroupToEntityMappingsAsync(testGroup);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload()
        {
            String testGroup = "group";
            String testEntityType = "ClientAccount";
            var testMappedEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationCoordinator.GetGroupToEntityMappingsAsync(testGroup, testEntityType).Returns(Task.FromResult<List<String>>(testMappedEntities));

            List<String> result = await testDistributedOperationCoordinatorNode.GetGroupToEntityMappingsAsync(testGroup, testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetGroupToEntityMappingsAsync(testGroup, testEntityType);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationCoordinatorNode.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockOperationCoordinator.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationCoordinatorNode.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HasAccessToEntityAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockOperationCoordinator.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationCoordinatorNode.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).HasAccessToEntityAsync(testUser, testEntityType, testEntity);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByUserAsync()
        {
            String testUser = "user1";
            var testAccessibleApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Summary", "View"),
                Tuple.Create("Order", "Delete")
            };
            mockOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleApplicationComponents));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetApplicationComponentsAccessibleByUserAsync(testUser);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetApplicationComponentsAccessibleByUserAsync(testUser);
            Assert.AreSame(testAccessibleApplicationComponents, result);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupAsync()
        {
            String testGroup = "group1";
            var testAccessibleApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Summary", "View"),
                Tuple.Create("Order", "Delete")
            };
            mockOperationCoordinator.GetApplicationComponentsAccessibleByGroupAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleApplicationComponents));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetApplicationComponentsAccessibleByGroupAsync(testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetApplicationComponentsAccessibleByGroupAsync(testGroup);
            Assert.AreSame(testAccessibleApplicationComponents, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsync()
        {
            String testUser = "user1";
            var testAccessibleEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("BusinessUnit", "Marketing")
            };
            mockOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleEntities));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetEntitiesAccessibleByUserAsync(testUser);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetEntitiesAccessibleByUserAsync(testUser);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncEntityTypeOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            var testAccessibleEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationCoordinator.GetEntitiesAccessibleByUserAsync(testUser, testEntityType).Returns(Task.FromResult<List<String>>(testAccessibleEntities));

            List<String> result = await testDistributedOperationCoordinatorNode.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsync()
        {
            String testGroup = "group1";
            var testAccessibleEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("BusinessUnit", "Marketing")
            };
            mockOperationCoordinator.GetEntitiesAccessibleByGroupAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleEntities));

            List<Tuple<String, String>> result = await testDistributedOperationCoordinatorNode.GetEntitiesAccessibleByGroupAsync(testGroup);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetEntitiesAccessibleByGroupAsync(testGroup);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncEntityTypeOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            var testAccessibleEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationCoordinator.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType).Returns(Task.FromResult<List<String>>(testAccessibleEntities));

            List<String> result = await testDistributedOperationCoordinatorNode.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);

            mockShardConfigurationRefreshStrategy.Received(1).NotifyOperationProcessed();
            await mockOperationCoordinator.Received(1).GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public void RefreshShardConfiguration_ReadFromPersisterFails()
        {
            var mockException = new Exception("Mock exception.");
            EventHandler capturedSubscriberMethod = null;
            mockShardConfigurationRefreshStrategy.ShardConfigurationRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockPersister.When((persister) => persister.Read()).Do((callInfo) => throw mockException);
            testDistributedOperationCoordinatorNode.Dispose();
            testDistributedOperationCoordinatorNode = new DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>
            (
                mockShardClientManager,
                mockShardConfigurationRefreshStrategy,
                mockOperationCoordinator,
                mockPersister
            );

            var e = Assert.Throws<ShardConfigurationRefreshException>(delegate
            {
                capturedSubscriberMethod.Invoke(mockShardConfigurationRefreshStrategy, EventArgs.Empty);
            });

            mockPersister.Received(1).Read();
            Assert.That(e.Message, Does.StartWith($"Failed to read shard configuration from persistent storage."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void RefreshShardConfiguration_RefreshOfShardClientManagerFails()
        {
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            var mockException = new Exception("Mock exception.");
            EventHandler capturedSubscriberMethod = null;
            mockShardConfigurationRefreshStrategy.ShardConfigurationRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockPersister.Read().Returns(testShardConfigurationSet);
            mockShardClientManager.When((shardClientManager) => shardClientManager.RefreshConfiguration(testShardConfigurationSet)).Do((callInfo) => throw mockException);
            testDistributedOperationCoordinatorNode.Dispose();
            testDistributedOperationCoordinatorNode = new DistributedOperationCoordinatorNode<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>
            (
                mockShardClientManager,
                mockShardConfigurationRefreshStrategy,
                mockOperationCoordinator,
                mockPersister
            );

            var e = Assert.Throws<ShardConfigurationRefreshException>(delegate
            {
                capturedSubscriberMethod.Invoke(mockShardConfigurationRefreshStrategy, EventArgs.Empty);
            });

            mockPersister.Received(1).Read();
            mockShardClientManager.Received(1).RefreshConfiguration(testShardConfigurationSet);
            Assert.That(e.Message, Does.StartWith($"Failed to refresh shard configuration in shard client manager."));
            Assert.AreSame(mockException, e.InnerException);
        }
    }
}
