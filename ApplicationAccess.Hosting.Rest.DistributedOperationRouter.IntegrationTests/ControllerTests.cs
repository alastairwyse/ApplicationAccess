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
using System.Threading.Tasks;
using ApplicationAccess.Hosting.Rest.Controllers;
using ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator.Controllers;
using ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Controllers;
using ApplicationAccess.Hosting.Rest.DistributedOperationRouterClient;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.IntegrationTests
{
    /// <summary>
    /// Tests controller methods implemented by the DistributedOperationRouter node.
    /// </summary>
    /// <remarks>
    ///   <para>This test class provides complete test coverage for the following controller classes...</para>
    ///   <para><see cref="DistributedOperationProcessorControllerBase"/></para>
    ///   <para><see cref="DistributedAsyncQueryProcessorControllerBase"/></para>
    ///   <para><see cref="DistributedOperationRouterController"/></para>
    ///   <para>...and client class <see cref="DistributedAccessManagerOperationRouterClient"/>.</para>
    ///   <para>The *ControllerBase classes are (and will be) used in other hosted REST components (e.g. <see cref="DistributedOperationProcessorControllerBase"/> is used in DistributedOperationCoordinator), however complete tests are provided in this class.</para>
    /// </remarks> 
    public class ControllerTests : DistributedOperationRouterIntegrationTestsBase
    {
        private const String urlReservedCharcters = "! * ' ( ) ; : @ & = + $ , / ? % # [ ]";
        private String encodedUrlReservedCharacters;

        [SetUp]
        protected void SetUp()
        {
            encodedUrlReservedCharacters = Uri.EscapeDataString(urlReservedCharcters);
        }

        #region DistributedOperationProcessorControllerBase Methods

        [Test]
        public async Task GetUsersAsync()
        {
            var returnUsers = new List<String>{ "user1", "user2", "user3" };
            mockDistributedAccessManagerOperationRouter.GetUsersAsync().Returns(returnUsers);

            List<String> result = await client.GetUsersAsync();

            Assert.IsTrue(StringEnumerablesContainSameValues(returnUsers, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetUsersAsync();
        }

        [Test]
        public async Task GetGroupsAsync()
        {
            var returnGroups = new List<String> { "group1", "group2", "group3" };
            mockDistributedAccessManagerOperationRouter.GetGroupsAsync().Returns(returnGroups);

            List<String> result = await client.GetGroupsAsync();

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupsAsync();
        }

        [Test]
        public async Task GetEntityTypesAsync()
        {
            var returnEntityTypes = new List<String> { "ClientAccount", "BusinessUnit" };
            mockDistributedAccessManagerOperationRouter.GetEntityTypesAsync().Returns(returnEntityTypes);

            List<String> result = await client.GetEntityTypesAsync();

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntityTypes, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntityTypesAsync();
        }

        [Test]
        public async Task ContainsUserAsync()
        {
            mockDistributedAccessManagerOperationRouter.ContainsUserAsync(urlReservedCharcters).Returns(true);

            Boolean result = await client.ContainsUserAsync(urlReservedCharcters);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsUserAsync(urlReservedCharcters);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.ContainsUserAsync(urlReservedCharcters).Returns(false);

            result = await client.ContainsUserAsync(urlReservedCharcters);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsUserAsync(urlReservedCharcters);
        }

        [Test]
        public async Task RemoveUserAsync()
        {
            await client.RemoveUserAsync(urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserAsync(urlReservedCharcters);
        }

        [Test]
        public async Task ContainsGroupAsync()
        {
            mockDistributedAccessManagerOperationRouter.ContainsGroupAsync(urlReservedCharcters).Returns(true);

            Boolean result = await client.ContainsGroupAsync(urlReservedCharcters);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsGroupAsync(urlReservedCharcters);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.ContainsGroupAsync(urlReservedCharcters).Returns(false);

            result = await client.ContainsGroupAsync(urlReservedCharcters);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsGroupAsync(urlReservedCharcters);
        }

        [Test]
        public async Task RemoveGroupAsync()
        {
            await client.RemoveGroupAsync(urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupAsync(urlReservedCharcters);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";

            await client.AddUserToGroupMappingAsync(testUser, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToGroupMappingAsync(testUser, urlReservedCharcters);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddUserToGroupMappingAsync(urlReservedCharcters, testGroup);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToGroupMappingAsync(urlReservedCharcters, testGroup);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync()
        {
            var returnGroups = new List<String>() { "group1", "group2", "group3" };
            mockDistributedAccessManagerOperationRouter.GetUserToGroupMappingsAsync(urlReservedCharcters, true).Returns(returnGroups);

            List<String> result = await client.GetUserToGroupMappingsAsync(urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetUserToGroupMappingsAsync(urlReservedCharcters, true);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync()
        {
            var returnUsers = new List<String> { "user1", "user2", "user3" };
            mockDistributedAccessManagerOperationRouter.GetGroupToUserMappingsAsync(urlReservedCharcters, true).Returns(returnUsers);

            List<String> result = await client.GetGroupToUserMappingsAsync(urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnUsers, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToUserMappingsAsync(urlReservedCharcters, true);
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";

            await client.RemoveUserToGroupMappingAsync(testUser, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToGroupMappingAsync(testUser, urlReservedCharcters);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveUserToGroupMappingAsync(urlReservedCharcters, testGroup);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToGroupMappingAsync(urlReservedCharcters, testGroup);
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync()
        {
            String testGroup = "group1";

            await client.AddGroupToGroupMappingAsync(testGroup, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToGroupMappingAsync(testGroup, urlReservedCharcters);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddGroupToGroupMappingAsync(urlReservedCharcters, testGroup);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToGroupMappingAsync(urlReservedCharcters, testGroup);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync()
        {
            var returnGroups = new List<String>() { "group2", "group3", "group4" };
            mockDistributedAccessManagerOperationRouter.GetGroupToGroupMappingsAsync(urlReservedCharcters, true).Returns(returnGroups);

            List<String> result = await client.GetGroupToGroupMappingsAsync(urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToGroupMappingsAsync(urlReservedCharcters, true);
        }

        [Test]
        public async Task GetGroupToGroupReverseMappingsAsync()
        {
            var returnGroups = new List<String>() { "group2", "group3", "group4" };
            mockDistributedAccessManagerOperationRouter.GetGroupToGroupReverseMappingsAsync(urlReservedCharcters, true).Returns(returnGroups);

            List<String> result = await client.GetGroupToGroupReverseMappingsAsync(urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToGroupReverseMappingsAsync(urlReservedCharcters, true);
        }

        [Test]
        public async Task RemoveGroupToGroupMappingAsync()
        {
            String testGroup = "group1";

            await client.RemoveGroupToGroupMappingAsync(testGroup, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToGroupMappingAsync(testGroup, urlReservedCharcters);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveGroupToGroupMappingAsync(urlReservedCharcters, testGroup);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToGroupMappingAsync(urlReservedCharcters, testGroup);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";

            await client.AddUserToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, urlReservedCharcters, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, urlReservedCharcters, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, urlReservedCharcters);
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync()
        {
            var returnApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Modify")
            };
            mockDistributedAccessManagerOperationRouter.GetUserToApplicationComponentAndAccessLevelMappingsAsync(urlReservedCharcters).Returns(returnApplicationComponentsAndAccessLevels);

            List<Tuple<String, String>> result = await client.GetUserToApplicationComponentAndAccessLevelMappingsAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[0], result[0]);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync()
        {
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";
            var returnUsers = new List<String>() { "user1", "user2", "user3" };
            mockDistributedAccessManagerOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(urlReservedCharcters, testAccessLevel, true).Returns(returnUsers);

            List<String> result = await client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(urlReservedCharcters, testAccessLevel, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnUsers, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(urlReservedCharcters, testAccessLevel, true);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, urlReservedCharcters, true).Returns(returnUsers);

            result = await client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnUsers, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, urlReservedCharcters, true);
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";

            await client.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, urlReservedCharcters, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, urlReservedCharcters, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, urlReservedCharcters);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";

            await client.AddGroupToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, urlReservedCharcters, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, urlReservedCharcters, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, urlReservedCharcters);
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync()
        {
            var returnApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Modify")
            };
            mockDistributedAccessManagerOperationRouter.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(urlReservedCharcters).Returns(returnApplicationComponentsAndAccessLevels);

            List<Tuple<String, String>> result = await client.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[0], result[0]);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync()
        {
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";
            var returnGroups = new List<String>() { "group1", "group2", "group3" };
            mockDistributedAccessManagerOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(urlReservedCharcters, testAccessLevel, true).Returns(returnGroups);

            List<String> result = await client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(urlReservedCharcters, testAccessLevel, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(urlReservedCharcters, testAccessLevel, true);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, urlReservedCharcters, true).Returns(returnGroups);

            result = await client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, urlReservedCharcters, true);
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";

            await client.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, urlReservedCharcters, testAccessLevel);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, urlReservedCharcters, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, urlReservedCharcters);
        }

        [Test]
        public async Task ContainsEntityTypeAsync()
        {
            mockDistributedAccessManagerOperationRouter.ContainsEntityTypeAsync(urlReservedCharcters).Returns(true);

            Boolean result = await client.ContainsEntityTypeAsync(urlReservedCharcters);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsEntityTypeAsync(urlReservedCharcters);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.ContainsEntityTypeAsync(urlReservedCharcters).Returns(false);

            result = await client.ContainsEntityTypeAsync(urlReservedCharcters);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsEntityTypeAsync(urlReservedCharcters);
        }

        [Test]
        public async Task RemoveEntityTypeAsync()
        {
            await client.RemoveEntityTypeAsync(urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveEntityTypeAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetEntitiesAsync()
        {
            var returnEntities = new List<String>() { "CompanyA", "CompanyB", "CompanyC" };
            mockDistributedAccessManagerOperationRouter.GetEntitiesAsync(urlReservedCharcters).Returns(returnEntities);

            List<String> result = await client.GetEntitiesAsync(urlReservedCharcters);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAsync(urlReservedCharcters);
        }

        [Test]
        public async Task ContainsEntityAsync()
        {
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";
            mockDistributedAccessManagerOperationRouter.ContainsEntityAsync(urlReservedCharcters, testEntity).Returns(true);

            Boolean result = await client.ContainsEntityAsync(urlReservedCharcters, testEntity);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsEntityAsync(urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.ContainsEntityAsync(testEntityType, urlReservedCharcters).Returns(false);

            result = await client.ContainsEntityAsync(testEntityType, urlReservedCharcters);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).ContainsEntityAsync(testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task RemoveEntityAsync()
        {
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";
            await client.RemoveEntityAsync(urlReservedCharcters, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveEntityAsync(urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            await client.RemoveEntityAsync(testEntityType, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveEntityAsync(testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";

            await client.AddUserToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddUserToEntityMappingAsync(testUser, urlReservedCharcters, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToEntityMappingAsync(testUser, urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddUserToEntityMappingAsync(testUser, testEntityType, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddUserToEntityMappingAsync(testUser, testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync()
        {
            var returnEntityTypesAndEntities = new List<Tuple<String, String>>()
            { 
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            mockDistributedAccessManagerOperationRouter.GetUserToEntityMappingsAsync(urlReservedCharcters).Returns(returnEntityTypesAndEntities);

            List<Tuple<String, String>> result = await client.GetUserToEntityMappingsAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnEntityTypesAndEntities[0], result[0]);
            Assert.AreEqual(returnEntityTypesAndEntities[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetUserToEntityMappingsAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            var returnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedAccessManagerOperationRouter.GetUserToEntityMappingsAsync(urlReservedCharcters, testEntityType).Returns(returnEntities);

            List<String> result = await client.GetUserToEntityMappingsAsync(urlReservedCharcters, testEntityType);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetUserToEntityMappingsAsync(urlReservedCharcters, testEntityType);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls(); 
            mockDistributedAccessManagerOperationRouter.GetUserToEntityMappingsAsync(testUser, urlReservedCharcters).Returns(returnEntities);

            result = await client.GetUserToEntityMappingsAsync(testUser, urlReservedCharcters);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetUserToEntityMappingsAsync(testUser, urlReservedCharcters);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync()
        {
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";
            var returnUsers = new List<String>() { "user1", "user2", "user3" };
            mockDistributedAccessManagerOperationRouter.GetEntityToUserMappingsAsync(urlReservedCharcters, testEntity, true).Returns(returnUsers);

            List<String> result = await client.GetEntityToUserMappingsAsync(urlReservedCharcters, testEntity, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnUsers, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntityToUserMappingsAsync(urlReservedCharcters, testEntity, true);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.GetEntityToUserMappingsAsync(testEntityType, urlReservedCharcters, true).Returns(returnUsers);

            result = await client.GetEntityToUserMappingsAsync(testEntityType, urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnUsers, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntityToUserMappingsAsync(testEntityType, urlReservedCharcters, true);
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";

            await client.RemoveUserToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveUserToEntityMappingAsync(testUser, urlReservedCharcters, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToEntityMappingAsync(testUser, urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveUserToEntityMappingAsync(testUser, testEntityType, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveUserToEntityMappingAsync(testUser, testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";

            await client.AddGroupToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddGroupToEntityMappingAsync(testGroup, urlReservedCharcters, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToEntityMappingAsync(testGroup, urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.AddGroupToEntityMappingAsync(testGroup, testEntityType, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).AddGroupToEntityMappingAsync(testGroup, testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync()
        {
            var returnEntityTypesAndEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            mockDistributedAccessManagerOperationRouter.GetGroupToEntityMappingsAsync(urlReservedCharcters).Returns(returnEntityTypesAndEntities);

            List<Tuple<String, String>> result = await client.GetGroupToEntityMappingsAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnEntityTypesAndEntities[0], result[0]);
            Assert.AreEqual(returnEntityTypesAndEntities[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToEntityMappingsAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            var returnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedAccessManagerOperationRouter.GetGroupToEntityMappingsAsync(urlReservedCharcters, testEntityType).Returns(returnEntities);

            List<String> result = await client.GetGroupToEntityMappingsAsync(urlReservedCharcters, testEntityType);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToEntityMappingsAsync(urlReservedCharcters, testEntityType);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.GetGroupToEntityMappingsAsync(testGroup, urlReservedCharcters).Returns(returnEntities);

            result = await client.GetGroupToEntityMappingsAsync(testGroup, urlReservedCharcters);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToEntityMappingsAsync(testGroup, urlReservedCharcters);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync()
        {
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";
            var returnGroups = new List<String>() { "group1", "group2", "group3" };
            mockDistributedAccessManagerOperationRouter.GetEntityToGroupMappingsAsync(urlReservedCharcters, testEntity, true).Returns(returnGroups);

            List<String> result = await client.GetEntityToGroupMappingsAsync(urlReservedCharcters, testEntity, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntityToGroupMappingsAsync(urlReservedCharcters, testEntity, true);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, urlReservedCharcters, true).Returns(returnGroups);

            result = await client.GetEntityToGroupMappingsAsync(testEntityType, urlReservedCharcters, true);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnGroups, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntityToGroupMappingsAsync(testEntityType, urlReservedCharcters, true);
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";

            await client.RemoveGroupToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToEntityMappingAsync(urlReservedCharcters, testEntityType, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveGroupToEntityMappingAsync(testGroup, urlReservedCharcters, testEntity);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToEntityMappingAsync(testGroup, urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();

            await client.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, urlReservedCharcters);

            await mockDistributedAccessManagerOperationRouter.Received(1).RemoveGroupToEntityMappingAsync(testGroup, testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";
            mockDistributedAccessManagerOperationRouter.HasAccessToApplicationComponentAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel).Returns(true);

            Boolean result = await client.HasAccessToApplicationComponentAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToApplicationComponentAsync(urlReservedCharcters, testApplicationComponent, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.HasAccessToApplicationComponentAsync(testUser, urlReservedCharcters, testAccessLevel).Returns(false);

            result = await client.HasAccessToApplicationComponentAsync(testUser, urlReservedCharcters, testAccessLevel);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToApplicationComponentAsync(testUser, urlReservedCharcters, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, urlReservedCharcters).Returns(true);

            result = await client.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, urlReservedCharcters);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, urlReservedCharcters);
        }

        [Test]
        public async Task HasAccessToEntityAsync()
        {
            String testUser = "user1";
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";
            mockDistributedAccessManagerOperationRouter.HasAccessToEntityAsync(urlReservedCharcters, testEntityType, testEntity).Returns(true);

            Boolean result = await client.HasAccessToEntityAsync(urlReservedCharcters, testEntityType, testEntity);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToEntityAsync(urlReservedCharcters, testEntityType, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.HasAccessToEntityAsync(testUser, urlReservedCharcters, testEntity).Returns(false);

            result = await client.HasAccessToEntityAsync(testUser, urlReservedCharcters, testEntity);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToEntityAsync(testUser, urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.HasAccessToEntityAsync(testUser, testEntityType, urlReservedCharcters).Returns(true);

            result = await client.HasAccessToEntityAsync(testUser, testEntityType, urlReservedCharcters);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToEntityAsync(testUser, testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByUserAsync()
        {
            var returnApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Modify")
            };
            mockDistributedAccessManagerOperationRouter.GetApplicationComponentsAccessibleByUserAsync(urlReservedCharcters).Returns(returnApplicationComponentsAndAccessLevels);

            List<Tuple<String, String>> result = await client.GetApplicationComponentsAccessibleByUserAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[0], result[0]);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetApplicationComponentsAccessibleByUserAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupAsync()
        {
            var returnApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Modify")
            };
            mockDistributedAccessManagerOperationRouter.GetApplicationComponentsAccessibleByGroupAsync(urlReservedCharcters).Returns(returnApplicationComponentsAndAccessLevels);

            List<Tuple<String, String>> result = await client.GetApplicationComponentsAccessibleByGroupAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[0], result[0]);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetApplicationComponentsAccessibleByGroupAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsync()
        {
            var returnEntityTypesAndEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByUserAsync(urlReservedCharcters).Returns(returnEntityTypesAndEntities);

            List<Tuple<String, String>> result = await client.GetEntitiesAccessibleByUserAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnEntityTypesAndEntities[0], result[0]);
            Assert.AreEqual(returnEntityTypesAndEntities[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByUserAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncUserAndEntityTypeOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            var returnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByUserAsync(urlReservedCharcters, testEntityType).Returns(returnEntities);

            List<String> result = await client.GetEntitiesAccessibleByUserAsync(urlReservedCharcters, testEntityType);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByUserAsync(urlReservedCharcters, testEntityType);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByUserAsync(testUser, urlReservedCharcters).Returns(returnEntities);

            result = await client.GetEntitiesAccessibleByUserAsync(testUser, urlReservedCharcters);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByUserAsync(testUser, urlReservedCharcters);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsync()
        {
            var returnEntityTypesAndEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByGroupAsync(urlReservedCharcters).Returns(returnEntityTypesAndEntities);

            List<Tuple<String, String>> result = await client.GetEntitiesAccessibleByGroupAsync(urlReservedCharcters);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnEntityTypesAndEntities[0], result[0]);
            Assert.AreEqual(returnEntityTypesAndEntities[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByGroupAsync(urlReservedCharcters);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncGroupAndEntityTypeOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            var returnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByGroupAsync(urlReservedCharcters, testEntityType).Returns(returnEntities);

            List<String> result = await client.GetEntitiesAccessibleByGroupAsync(urlReservedCharcters, testEntityType);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByGroupAsync(urlReservedCharcters, testEntityType);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByGroupAsync(testGroup, urlReservedCharcters).Returns(returnEntities);

            result = await client.GetEntitiesAccessibleByGroupAsync(testGroup, urlReservedCharcters);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByGroupAsync(testGroup, urlReservedCharcters);
        }

        #endregion

        #region DistributedAsyncQueryProcessorControllerBase Methods

        [Test]
        public async Task GetGroupToUserMappingsAsyncGroupsOverload()
        {
            var testGroups = new List<String> { "group1", "group2", "group3" };
            var returnUsers = new List<String> { "user1", "user2", "user3" };
            mockDistributedAccessManagerOperationRouter.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(returnUsers);

            List<String> result = await client.GetGroupToUserMappingsAsync(testGroups);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnUsers, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload()
        {
            var testGroups = new List<String> { "group1", "group2", "group3" };
            String testApplicationComponent = "ManageProductsScreen";
            String testAccessLevel = "View";
            mockDistributedAccessManagerOperationRouter.HasAccessToApplicationComponentAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, testAccessLevel).Returns(false);

            Boolean result = await client.HasAccessToApplicationComponentAsync(testGroups, urlReservedCharcters, testAccessLevel);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToApplicationComponentAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, testAccessLevel);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.HasAccessToApplicationComponentAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testApplicationComponent, urlReservedCharcters).Returns(true);

            result = await client.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, urlReservedCharcters);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToApplicationComponentAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testApplicationComponent, urlReservedCharcters);
        }

        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload()
        {
            var testGroups = new List<String> { "group1", "group2", "group3" };
            String testEntityType = "BusinessUnit";
            String testEntity = "Sales";
            mockDistributedAccessManagerOperationRouter.HasAccessToEntityAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, testEntity).Returns(false);

            Boolean result = await client.HasAccessToEntityAsync(testGroups, urlReservedCharcters, testEntity);

            Assert.IsFalse(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToEntityAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters, testEntity);


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            mockDistributedAccessManagerOperationRouter.HasAccessToEntityAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType, urlReservedCharcters).Returns(true);

            result = await client.HasAccessToEntityAsync(testGroups, testEntityType, urlReservedCharcters);

            Assert.IsTrue(result);
            await mockDistributedAccessManagerOperationRouter.Received(1).HasAccessToEntityAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType, urlReservedCharcters);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync()
        {
            var testGroups = new List<String> { "group1", "group2", "group3" };
            var returnApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Modify")
            };
            mockDistributedAccessManagerOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(returnApplicationComponentsAndAccessLevels);

            List<Tuple<String, String>> result = await client.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[0], result[0]);
            Assert.AreEqual(returnApplicationComponentsAndAccessLevels[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync()
        {
            var testGroups = new List<String> { "group1", "group2", "group3" };
            var returnEntityTypesAndEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(returnEntityTypesAndEntities);

            List<Tuple<String, String>> result = await client.GetEntitiesAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(returnEntityTypesAndEntities[0], result[0]);
            Assert.AreEqual(returnEntityTypesAndEntities[1], result[1]);
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncGroupsAndEntityTypeOverload()
        {
            var testGroups = new List<String> { "group1", "group2", "group3" };
            var returnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedAccessManagerOperationRouter.GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters).Returns(returnEntities);

            List<String> result = await client.GetEntitiesAccessibleByGroupsAsync(testGroups, urlReservedCharcters);

            Assert.IsTrue(StringEnumerablesContainSameValues(returnEntities, result));
            await mockDistributedAccessManagerOperationRouter.Received(1).GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), urlReservedCharcters);
        }

        #endregion

        #region DistributedOperationRouterController Methods

        [Test]
        public void RoutingOnProperty()
        {
            client.RoutingOn = true;

            mockDistributedAccessManagerOperationRouter.Received(1).RoutingOn = true;


            mockDistributedAccessManagerOperationRouter.ClearReceivedCalls();
            client.RoutingOn = false;

            mockDistributedAccessManagerOperationRouter.Received(1).RoutingOn = false;

        }

        [Test]
        public void PauseOperations()
        {
            client.PauseOperations();

            mockDistributedAccessManagerOperationRouter.Received(1).PauseOperations();
        }

        [Test]
        public void ResumeOperations()
        {
            client.ResumeOperations();

            mockDistributedAccessManagerOperationRouter.Received(1).ResumeOperations();
        }

        #endregion
    }
}
