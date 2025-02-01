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
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NSubstitute;
using ApplicationAccess.Distribution.Metrics;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.DistributedOperationRouterNode class.
    /// </summary>
    public class DistributedOperationRouterNodeTests
    {
        private TestUtilities testUtilities;
        private IDistributedAccessManagerOperationRouter mockOperationRouter;
        private DistributedOperationRouterNode<AccessManagerRestClientConfiguration> testDistributedOperationRouterNode;

        [SetUp]
        protected void SetUp()
        {
            testUtilities = new TestUtilities();
            mockOperationRouter = Substitute.For<IDistributedAccessManagerOperationRouter>();
            testDistributedOperationRouterNode = new DistributedOperationRouterNode<AccessManagerRestClientConfiguration>
            (
                mockOperationRouter
            );
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void RoutingOnProperty()
        {
            testDistributedOperationRouterNode.RoutingOn = true;

            mockOperationRouter.Received(1).RoutingOn = true;
            Assert.AreEqual(1, mockOperationRouter.ReceivedCalls().Count());


            mockOperationRouter.ClearReceivedCalls();

            testDistributedOperationRouterNode.RoutingOn = false;

            mockOperationRouter.Received(1).RoutingOn = false;
            Assert.AreEqual(1, mockOperationRouter.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetUsersAsync()
        {
            var testUsers = new List<String>() { "user1", "user2" };
            mockOperationRouter.GetUsersAsync().Returns(Task.FromResult<List<String>>(testUsers));

            List<String> result = await testDistributedOperationRouterNode.GetUsersAsync();

            await mockOperationRouter.Received(1).GetUsersAsync();
            Assert.AreSame(testUsers, result);
        }

        [Test]
        public async Task GetGroupsAsync()
        {
            var testGroups = new List<String>() { "group1", "group2" };
            mockOperationRouter.GetGroupsAsync().Returns(Task.FromResult<List<String>>(testGroups));

            List<String> result = await testDistributedOperationRouterNode.GetGroupsAsync();

            await mockOperationRouter.Received(1).GetGroupsAsync();
            Assert.AreSame(testGroups, result);
        }

        [Test]
        public async Task GetEntityTypesAsync()
        {
            var testEntityTypes = new List<String>() { "ClientAccount", "BusinessUnit" };
            mockOperationRouter.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(testEntityTypes));

            List<String> result = await testDistributedOperationRouterNode.GetEntityTypesAsync();

            await mockOperationRouter.Received(1).GetEntityTypesAsync();
            Assert.AreSame(testEntityTypes, result);
        }

        [Test]
        public async Task ContainsUserAsync()
        {
            String testUser = "user1";
            mockOperationRouter.ContainsUserAsync(testUser).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.ContainsUserAsync(testUser);

            await mockOperationRouter.Received(1).ContainsUserAsync(testUser);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveUserAsync()
        {
            String testUser = "user1";

            await testDistributedOperationRouterNode.RemoveUserAsync(testUser);

            await mockOperationRouter.Received(1).RemoveUserAsync(testUser);
        }

        [Test]
        public async Task ContainsGroupAsync()
        {
            String testGroup = "group1";
            mockOperationRouter.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.ContainsGroupAsync(testGroup);

            await mockOperationRouter.Received(1).ContainsGroupAsync(testGroup);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveGroupAsync()
        {
            String testGroup = "group1";

            await testDistributedOperationRouterNode.RemoveGroupAsync(testGroup);

            await mockOperationRouter.Received(1).RemoveGroupAsync(testGroup);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";

            await testDistributedOperationRouterNode.AddUserToGroupMappingAsync(testUser, testGroup);

            await mockOperationRouter.Received(1).AddUserToGroupMappingAsync(testUser, testGroup);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync()
        {
            String testUser = "user1";
            var testGroups = new List<String>() { "group1", "group2" };
            mockOperationRouter.GetUserToGroupMappingsAsync(testUser, false).Returns(Task.FromResult<List<String>>(testGroups));

            List<String> result = await testDistributedOperationRouterNode.GetUserToGroupMappingsAsync(testUser, false);

            await mockOperationRouter.Received(1).GetUserToGroupMappingsAsync(testUser, false);
            Assert.AreSame(testGroups, result);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";
            mockOperationRouter.GetGroupToUserMappingsAsync(testGroup, false).Returns(Task.FromResult<List<String>>(new List<String>() { testUser }));

            List<String> result = await testDistributedOperationRouterNode.GetGroupToUserMappingsAsync(testGroup, false);

            await mockOperationRouter.Received(1).GetGroupToUserMappingsAsync(testGroup, false);
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(testUser, result[0]);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsyncGroupsOverload()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var returnUsers = new List<String> { "user1" };
            mockOperationRouter.GetGroupToUserMappingsAsync(testGroups).Returns(Task.FromResult<List<String>>(returnUsers));

            List<String> result = await testDistributedOperationRouterNode.GetGroupToUserMappingsAsync(testGroups);

            await mockOperationRouter.Received(1).GetGroupToUserMappingsAsync(testGroups);
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(returnUsers[0], result[0]);
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";

            await testDistributedOperationRouterNode.RemoveUserToGroupMappingAsync(testUser, testGroup);

            await mockOperationRouter.Received(1).RemoveUserToGroupMappingAsync(testUser, testGroup);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationRouterNode.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            await mockOperationRouter.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
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
            mockOperationRouter.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedApplicationComponentsAndAccessLevels));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);

            await mockOperationRouter.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
            Assert.AreSame(testMappedApplicationComponentsAndAccessLevels, result);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync()
        {
            String testUser = "user";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { testUser }));

            List<String> result = await testDistributedOperationRouterNode.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);

            await mockOperationRouter.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreSame(testUser, result[0]);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationRouterNode.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            await mockOperationRouter.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationRouterNode.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            await mockOperationRouter.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
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
            mockOperationRouter.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedApplicationComponentsAndAccessLevels));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);

            await mockOperationRouter.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);
            Assert.AreSame(testMappedApplicationComponentsAndAccessLevels, result);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync()
        {
            String testGroup = "group";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(new List<String>() { testGroup }));

            List<String> result = await testDistributedOperationRouterNode.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);

            await mockOperationRouter.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreSame(testGroup, result[0]);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            await testDistributedOperationRouterNode.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            await mockOperationRouter.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task ContainsEntityTypeAsync()
        {
            String testEntityType = "ClientAccount";
            mockOperationRouter.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.ContainsEntityTypeAsync(testEntityType);

            await mockOperationRouter.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveEntityTypeAsync()
        {
            String testEntityType = "ClientAccount";

            await testDistributedOperationRouterNode.RemoveEntityTypeAsync(testEntityType);

            await mockOperationRouter.Received(1).RemoveEntityTypeAsync(testEntityType);
        }

        [Test]
        public async Task GetEntitiesAsync()
        {
            String testEntityType = "ClientAccount";
            var testEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationRouter.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(testEntities));

            List<String> result = await testDistributedOperationRouterNode.GetEntitiesAsync(testEntityType);

            await mockOperationRouter.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreSame(testEntities, result);
        }

        [Test]
        public async Task ContainsEntityAsync()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockOperationRouter.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.ContainsEntityAsync(testEntityType, testEntity);

            await mockOperationRouter.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RemoveEntityAsync()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationRouterNode.RemoveEntityAsync(testEntityType, testEntity);

            await mockOperationRouter.Received(1).RemoveEntityAsync(testEntityType, testEntity);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationRouterNode.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            await mockOperationRouter.Received(1).AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);
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
            mockOperationRouter.GetUserToEntityMappingsAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedEntities));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetUserToEntityMappingsAsync(testUser);

            await mockOperationRouter.Received(1).GetUserToEntityMappingsAsync(testUser);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload()
        {
            String testUser = "user";
            String testEntityType = "ClientAccount";
            var testMappedEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationRouter.GetUserToEntityMappingsAsync(testUser, testEntityType).Returns(Task.FromResult<List<String>>(testMappedEntities));

            List<String> result = await testDistributedOperationRouterNode.GetUserToEntityMappingsAsync(testUser, testEntityType);

            await mockOperationRouter.Received(1).GetUserToEntityMappingsAsync(testUser, testEntityType);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync()
        {
            String testUser = "user";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { testUser }));

            List<String> result = await testDistributedOperationRouterNode.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);

            await mockOperationRouter.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            Assert.AreSame(testUser, result[0]);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationRouterNode.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            await mockOperationRouter.Received(1).RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationRouterNode.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            await mockOperationRouter.Received(1).AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
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
            mockOperationRouter.GetGroupToEntityMappingsAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappedEntities));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetGroupToEntityMappingsAsync(testGroup);

            await mockOperationRouter.Received(1).GetGroupToEntityMappingsAsync(testGroup);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload()
        {
            String testGroup = "group";
            String testEntityType = "ClientAccount";
            var testMappedEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationRouter.GetGroupToEntityMappingsAsync(testGroup, testEntityType).Returns(Task.FromResult<List<String>>(testMappedEntities));

            List<String> result = await testDistributedOperationRouterNode.GetGroupToEntityMappingsAsync(testGroup, testEntityType);

            await mockOperationRouter.Received(1).GetGroupToEntityMappingsAsync(testGroup, testEntityType);
            Assert.AreSame(testMappedEntities, result);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync()
        {
            String testGroup = "group";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(new List<String>() { testGroup }));

            List<String> result = await testDistributedOperationRouterNode.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);

            await mockOperationRouter.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            Assert.AreSame(testGroup, result[0]);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";

            await testDistributedOperationRouterNode.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            await mockOperationRouter.Received(1).RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockOperationRouter.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);

            await mockOperationRouter.Received(1).HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            mockOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            await mockOperationRouter.Received(1).HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HasAccessToEntityAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockOperationRouter.HasAccessToEntityAsync(testUser, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            await mockOperationRouter.Received(1).HasAccessToEntityAsync(testUser, testEntityType, testEntity);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testDistributedOperationRouterNode.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            await mockOperationRouter.Received(1).HasAccessToEntityAsync(testGroups, testEntityType, testEntity);
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
            mockOperationRouter.GetApplicationComponentsAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleApplicationComponents));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetApplicationComponentsAccessibleByUserAsync(testUser);

            await mockOperationRouter.Received(1).GetApplicationComponentsAccessibleByUserAsync(testUser);
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
            mockOperationRouter.GetApplicationComponentsAccessibleByGroupAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleApplicationComponents));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetApplicationComponentsAccessibleByGroupAsync(testGroup);

            await mockOperationRouter.Received(1).GetApplicationComponentsAccessibleByGroupAsync(testGroup);
            Assert.AreSame(testAccessibleApplicationComponents, result);
        }


        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var testAccessibleApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Summary", "View"),
                Tuple.Create("Order", "Delete")
            };
            mockOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(testGroups).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleApplicationComponents));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);

            await mockOperationRouter.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(testGroups);
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
            mockOperationRouter.GetEntitiesAccessibleByUserAsync(testUser).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleEntities));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetEntitiesAccessibleByUserAsync(testUser);

            await mockOperationRouter.Received(1).GetEntitiesAccessibleByUserAsync(testUser);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncEntityTypeOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            var testAccessibleEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationRouter.GetEntitiesAccessibleByUserAsync(testUser, testEntityType).Returns(Task.FromResult<List<String>>(testAccessibleEntities));

            List<String> result = await testDistributedOperationRouterNode.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);

            await mockOperationRouter.Received(1).GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
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
            mockOperationRouter.GetEntitiesAccessibleByGroupAsync(testGroup).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleEntities));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetEntitiesAccessibleByGroupAsync(testGroup);

            await mockOperationRouter.Received(1).GetEntitiesAccessibleByGroupAsync(testGroup);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncEntityTypeOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            var testAccessibleEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationRouter.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType).Returns(Task.FromResult<List<String>>(testAccessibleEntities));

            List<String> result = await testDistributedOperationRouterNode.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);

            await mockOperationRouter.Received(1).GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var testAccessibleEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("BusinessUnit", "Marketing")
            };
            mockOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups).Returns(Task.FromResult<List<Tuple<String, String>>>(testAccessibleEntities));

            List<Tuple<String, String>> result = await testDistributedOperationRouterNode.GetEntitiesAccessibleByGroupsAsync(testGroups);

            await mockOperationRouter.Received(1).GetEntitiesAccessibleByGroupsAsync(testGroups);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncEntityTypeOverload()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            String testEntityType = "ClientAccount";
            var testAccessibleEntities = new List<String>() { "CompanyA", "CompanyB" };
            mockOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups, testEntityType).Returns(Task.FromResult<List<String>>(testAccessibleEntities));

            List<String> result = await testDistributedOperationRouterNode.GetEntitiesAccessibleByGroupsAsync(testGroups, testEntityType);

            await mockOperationRouter.Received(1).GetEntitiesAccessibleByGroupsAsync(testGroups, testEntityType);
            Assert.AreSame(testAccessibleEntities, result);
        }

        [Test]
        public void PauseOperations()
        {
            testDistributedOperationRouterNode.PauseOperations();

            mockOperationRouter.Received(1).PauseOperations();
        }

        [Test]
        public void ResumeOperations()
        {
            testDistributedOperationRouterNode.ResumeOperations();

            mockOperationRouter.Received(1).ResumeOperations();
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
    }
}
