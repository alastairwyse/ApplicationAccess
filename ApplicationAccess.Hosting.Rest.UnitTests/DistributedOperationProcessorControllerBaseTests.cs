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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Hosting.Rest.Controllers;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Hosting.Rest.Utilities;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.DistributedOperationProcessorControllerBase class.
    /// </summary>
    public class DistributedOperationProcessorControllerBaseTests
    {
        private IDistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> mockDistributedOperationCoordinator;
        private ILogger<DistributedOperationProcessorControllerBase> mockLogger;
        private DistributedOperationProcessorController testDistributedOperationProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockDistributedOperationCoordinator = Substitute.For<IDistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration>>();
            mockLogger = Substitute.For<ILogger<DistributedOperationProcessorControllerBase>>();
            var asyncQueryProcessorHolder = new AsyncQueryProcessorHolder();
            var asyncEventProcessorHolder = new AsyncEventProcessorHolder();
            asyncQueryProcessorHolder.AsyncQueryProcessor = mockDistributedOperationCoordinator;
            asyncEventProcessorHolder.AsyncEventProcessor = mockDistributedOperationCoordinator;
            testDistributedOperationProcessorController = new DistributedOperationProcessorController(asyncQueryProcessorHolder, asyncEventProcessorHolder, mockLogger);
        }

        [Test]
        public async Task ContainsUserAsync_TrueResult()
        {
            const String user = "user1";
            mockDistributedOperationCoordinator.ContainsUserAsync(user).Returns(Task.FromResult<Boolean>(true));

            ActionResult<String> result = await testDistributedOperationProcessorController.ContainsUserAsync(user);

            await mockDistributedOperationCoordinator.Received(1).ContainsUserAsync(user);
            Assert.AreEqual(user, result.Value);
        }

        [Test]
        public async Task ContainsUserAsync_FalseResult()
        {
            const String user = "user1";
            mockDistributedOperationCoordinator.ContainsUserAsync(user).Returns(Task.FromResult<Boolean>(false));

            var e = Assert.ThrowsAsync<NotFoundException>(async delegate
            {
                ActionResult<String> result = await testDistributedOperationProcessorController.ContainsUserAsync(user);
            });

            await mockDistributedOperationCoordinator.Received(1).ContainsUserAsync(user);
            Assert.That(e.Message, Does.StartWith($"User '{user}' does not exist."));
            Assert.AreEqual(user, e.ResourceId);
        }

        [Test]
        public async Task ContainsGroupAsync_TrueResult()
        {
            const String group = "group1";
            mockDistributedOperationCoordinator.ContainsGroupAsync(group).Returns(Task.FromResult<Boolean>(true));

            ActionResult<String> result = await testDistributedOperationProcessorController.ContainsGroupAsync(group);

            await mockDistributedOperationCoordinator.Received(1).ContainsGroupAsync(group);
            Assert.AreEqual(group, result.Value);
        }

        [Test]
        public async Task ContainsGroupAsync_FalseResult()
        {
            const String group = "group1";
            mockDistributedOperationCoordinator.ContainsGroupAsync(group).Returns(Task.FromResult<Boolean>(false));

            var e = Assert.ThrowsAsync<NotFoundException>(async delegate
            {
                ActionResult<String> result = await testDistributedOperationProcessorController.ContainsGroupAsync(group);
            });

            await mockDistributedOperationCoordinator.Received(1).ContainsGroupAsync(group);
            Assert.That(e.Message, Does.StartWith($"Group '{group}' does not exist."));
            Assert.AreEqual(group, e.ResourceId);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync()
        {
            const String user = "user1";
            const String group = "group1";

            StatusCodeResult result = await testDistributedOperationProcessorController.AddUserToGroupMappingAsync(user, group);

            await mockDistributedOperationCoordinator.Received(1).AddUserToGroupMappingAsync(user, group);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            var groups = new List<String>()
            {
                "group1",
                "group2"
            };
            mockDistributedOperationCoordinator.GetUserToGroupMappingsAsync(user, false).Returns(Task.FromResult<List<String>>(groups));

            var result = new List<UserAndGroup<String, String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToGroupMappingsAsync(user, false)));

            await mockDistributedOperationCoordinator.Received(1).GetUserToGroupMappingsAsync(user, false);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("group1", result[0].Group);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("group2", result[1].Group);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            var groups = new List<String>()
            {
                "group1",
                "group2",
                "group3"
            };
            mockDistributedOperationCoordinator.GetUserToGroupMappingsAsync(user, true).Returns(Task.FromResult<List<String>>(groups));

            var result = new List<UserAndGroup<String, String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToGroupMappingsAsync(user, true)));

            await mockDistributedOperationCoordinator.Received(1).GetUserToGroupMappingsAsync(user, true);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("group1", result[0].Group);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("group2", result[1].Group);
            Assert.AreEqual(user, result[2].User);
            Assert.AreEqual("group3", result[2].Group);
        }

        [Test]
        public async Task AddGroupToGroupMappingAsync()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";

            StatusCodeResult result = await testDistributedOperationProcessorController.AddGroupToGroupMappingAsync(fromGroup, toGroup);

            await mockDistributedOperationCoordinator.Received(1).AddGroupToGroupMappingAsync(fromGroup, toGroup);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            const String fromGroup = "group1";
            var toGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            mockDistributedOperationCoordinator.GetGroupToGroupMappingsAsync(fromGroup, false).Returns(Task.FromResult<List<String>>(toGroups));

            var result = new List<FromGroupAndToGroup<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToGroupMappingsAsync(fromGroup, false)));

            await mockDistributedOperationCoordinator.Received(1).GetGroupToGroupMappingsAsync(fromGroup, false);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(fromGroup, result[0].FromGroup);
            Assert.AreEqual("group2", result[0].ToGroup);
            Assert.AreEqual(fromGroup, result[1].FromGroup);
            Assert.AreEqual("group3", result[1].ToGroup);
        }

        [Test]
        public async Task GetGroupToGroupMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            const String fromGroup = "group1";
            var toGroups = new List<String>()
            {
                "group2",
                "group3",
                "group4"
            };
            mockDistributedOperationCoordinator.GetGroupToGroupMappingsAsync(fromGroup, true).Returns(Task.FromResult<List<String>>(toGroups));

            var result = new List<FromGroupAndToGroup<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToGroupMappingsAsync(fromGroup, true)));

            await mockDistributedOperationCoordinator.Received(1).GetGroupToGroupMappingsAsync(fromGroup, true);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(fromGroup, result[0].FromGroup);
            Assert.AreEqual("group2", result[0].ToGroup);
            Assert.AreEqual(fromGroup, result[1].FromGroup);
            Assert.AreEqual("group3", result[1].ToGroup);
            Assert.AreEqual(fromGroup, result[2].FromGroup);
            Assert.AreEqual("group4", result[2].ToGroup);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingsAsync()
        {
            const String user = "user1";
            const String applicationComponent = "Order";
            const String accessLevel = "Create";

            StatusCodeResult result = await testDistributedOperationProcessorController.AddUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);

            await mockDistributedOperationCoordinator.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create")
            };
            mockDistributedOperationCoordinator.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<UserAndApplicationComponentAndAccessLevel<String, String, String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user, false)));

            await mockDistributedOperationCoordinator.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
            await mockDistributedOperationCoordinator.DidNotReceive().GetApplicationComponentsAccessibleByUserAsync(user);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("Order", result[0].ApplicationComponent);
            Assert.AreEqual("View", result[0].AccessLevel);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("Summary", result[1].ApplicationComponent);
            Assert.AreEqual("Create", result[1].AccessLevel);
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create"),
                new Tuple<String, String>("Settings", "Modify")
            };
            mockDistributedOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(user).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<UserAndApplicationComponentAndAccessLevel<String, String, String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user, true)));

            await mockDistributedOperationCoordinator.Received(1).GetApplicationComponentsAccessibleByUserAsync(user);
            await mockDistributedOperationCoordinator.DidNotReceive().GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("Order", result[0].ApplicationComponent);
            Assert.AreEqual("View", result[0].AccessLevel);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("Summary", result[1].ApplicationComponent);
            Assert.AreEqual("Create", result[1].AccessLevel);
            Assert.AreEqual(user, result[2].User);
            Assert.AreEqual("Settings", result[2].ApplicationComponent);
            Assert.AreEqual("Modify", result[2].AccessLevel);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            const String group = "group1";
            const String applicationComponent = "Order";
            const String accessLevel = "Create";

            StatusCodeResult result = await testDistributedOperationProcessorController.AddGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);

            await mockDistributedOperationCoordinator.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            const String group = "group1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create")
            };
            mockDistributedOperationCoordinator.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group, false)));

            await mockDistributedOperationCoordinator.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
            await mockDistributedOperationCoordinator.DidNotReceive().GetApplicationComponentsAccessibleByGroupAsync(group);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual("Order", result[0].ApplicationComponent);
            Assert.AreEqual("View", result[0].AccessLevel);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual("Summary", result[1].ApplicationComponent);
            Assert.AreEqual("Create", result[1].AccessLevel);
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            const String group = "group1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create"),
                new Tuple<String, String>("Settings", "Modify")
            };
            mockDistributedOperationCoordinator.GetApplicationComponentsAccessibleByGroupAsync(group).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group, true)));

            await mockDistributedOperationCoordinator.Received(1).GetApplicationComponentsAccessibleByGroupAsync(group);
            await mockDistributedOperationCoordinator.DidNotReceive().GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual("Order", result[0].ApplicationComponent);
            Assert.AreEqual("View", result[0].AccessLevel);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual("Summary", result[1].ApplicationComponent);
            Assert.AreEqual("Create", result[1].AccessLevel);
            Assert.AreEqual(group, result[2].Group);
            Assert.AreEqual("Settings", result[2].ApplicationComponent);
            Assert.AreEqual("Modify", result[2].AccessLevel);
        }

        [Test]
        public async Task ContainsEntityTypeAsync_TrueResult()
        {
            const String entityType = "ClientAccount";
            mockDistributedOperationCoordinator.ContainsEntityTypeAsync(entityType).Returns(Task.FromResult<Boolean>(true));

            ActionResult<String> result = await testDistributedOperationProcessorController.ContainsEntityTypeAsync(entityType);

            await mockDistributedOperationCoordinator.Received(1).ContainsEntityTypeAsync(entityType);
            Assert.AreEqual(entityType, result.Value);
        }

        [Test]
        public async Task ContainsEntityTypeAsync_FalseResult()
        {
            const String entityType = "ClientAccount";
            mockDistributedOperationCoordinator.ContainsEntityTypeAsync(entityType).Returns(Task.FromResult<Boolean>(false));

            var e = Assert.ThrowsAsync<NotFoundException>(async delegate
            {
                ActionResult<String> result = await testDistributedOperationProcessorController.ContainsEntityTypeAsync(entityType);
            });

            await mockDistributedOperationCoordinator.Received(1).ContainsEntityTypeAsync(entityType);
            Assert.That(e.Message, Does.StartWith($"Entity type '{entityType}' does not exist."));
            Assert.AreEqual(entityType, e.ResourceId);
        }

        [Test]
        public async Task GetEntitiesAsync()
        {
            const String entityType = "ClientAccount";
            var entities = new List<String>() { "CompanyA", "CompanyB" };
            mockDistributedOperationCoordinator.GetEntitiesAsync(entityType).Returns(Task.FromResult<List<String>>(entities));

            var result = new List<EntityTypeAndEntity>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetEntitiesAsync(entityType)));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
        }

        [Test]
        public async Task ContainsEntityAsync_TrueResult()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockDistributedOperationCoordinator.ContainsEntityAsync(entityType, entity).Returns(Task.FromResult<Boolean>(true));

            ActionResult<EntityTypeAndEntity> result = await testDistributedOperationProcessorController.ContainsEntityAsync(entityType, entity);

            await mockDistributedOperationCoordinator.Received(1).ContainsEntityAsync(entityType, entity);
            Assert.AreEqual(entityType, result.Value.EntityType);
            Assert.AreEqual(entity, result.Value.Entity);
        }

        [Test]
        public async Task ContainsEntityAsync_FalseResult()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockDistributedOperationCoordinator.ContainsEntityAsync(entityType, entity).Returns(Task.FromResult<Boolean>(false));

            var e = Assert.ThrowsAsync<NotFoundException>(async delegate
            {
                ActionResult<EntityTypeAndEntity> result = await testDistributedOperationProcessorController.ContainsEntityAsync(entityType, entity);
            });

            await mockDistributedOperationCoordinator.Received(1).ContainsEntityAsync(entityType, entity);
            Assert.That(e.Message, Does.StartWith($"Entity '{entity}' with type '{entityType}' does not exist."));
            Assert.AreEqual(entity, e.ResourceId);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";

            StatusCodeResult result = await testDistributedOperationProcessorController.AddUserToEntityMappingAsync(user, entityType, entity);

            await mockDistributedOperationCoordinator.Received(1).AddUserToEntityMappingAsync(user, entityType, entity);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing")
            };
            mockDistributedOperationCoordinator.GetUserToEntityMappingsAsync(user).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<UserAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToEntityMappingsAsync(user, false)));

            await mockDistributedOperationCoordinator.Received(1).GetUserToEntityMappingsAsync(user);
            await mockDistributedOperationCoordinator.DidNotReceive().GetEntitiesAccessibleByUserAsync(user);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("ClientAccount", result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("BusinessUnit", result[1].EntityType);
            Assert.AreEqual("Marketing", result[1].Entity);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing"),
                new Tuple<String, String>("ClientAccount", "CompanyB")
            };
            mockDistributedOperationCoordinator.GetEntitiesAccessibleByUserAsync(user).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<UserAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToEntityMappingsAsync(user, true)));

            await mockDistributedOperationCoordinator.Received(1).GetEntitiesAccessibleByUserAsync(user);
            await mockDistributedOperationCoordinator.DidNotReceive().GetUserToEntityMappingsAsync(user);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("ClientAccount", result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("BusinessUnit", result[1].EntityType);
            Assert.AreEqual("Marketing", result[1].Entity);
            Assert.AreEqual(user, result[2].User);
            Assert.AreEqual("ClientAccount", result[2].EntityType);
            Assert.AreEqual("CompanyB", result[2].Entity);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncEntityTypeOverload_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            var testMappings = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedOperationCoordinator.GetUserToEntityMappingsAsync(user, entityType).Returns(Task.FromResult<List<String>>(testMappings));

            var result = new List<UserAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToEntityMappingsAsync(user, entityType, false)));

            await mockDistributedOperationCoordinator.Received(1).GetUserToEntityMappingsAsync(user, entityType);
            await mockDistributedOperationCoordinator.DidNotReceive().GetEntitiesAccessibleByUserAsync(user, entityType);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncEntityTypeOverload_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            var testMappings = new List<String>()
            {
                "CompanyA",
                "CompanyB",
                "CompanyC"
            };
            mockDistributedOperationCoordinator.GetEntitiesAccessibleByUserAsync(user, entityType).Returns(Task.FromResult<List<String>>(testMappings));

            var result = new List<UserAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetUserToEntityMappingsAsync(user, entityType, true)));

            await mockDistributedOperationCoordinator.Received(1).GetEntitiesAccessibleByUserAsync(user, entityType);
            await mockDistributedOperationCoordinator.DidNotReceive().GetUserToEntityMappingsAsync(user, entityType);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
            Assert.AreEqual(user, result[2].User);
            Assert.AreEqual(entityType, result[2].EntityType);
            Assert.AreEqual("CompanyC", result[2].Entity);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";

            StatusCodeResult result = await testDistributedOperationProcessorController.AddGroupToEntityMappingAsync(group, entityType, entity);

            await mockDistributedOperationCoordinator.Received(1).AddGroupToEntityMappingAsync(group, entityType, entity);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync_IncludeIndirectMappingsParameterFalse()
        {
            const String group = "group1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing")
            };
            mockDistributedOperationCoordinator.GetGroupToEntityMappingsAsync(group).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<GroupAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToEntityMappingsAsync(group, false)));

            await mockDistributedOperationCoordinator.Received(1).GetGroupToEntityMappingsAsync(group);
            await mockDistributedOperationCoordinator.DidNotReceive().GetEntitiesAccessibleByGroupAsync(group);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual("ClientAccount", result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual("BusinessUnit", result[1].EntityType);
            Assert.AreEqual("Marketing", result[1].Entity);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync_IncludeIndirectMappingsParameterTrue()
        {
            const String group = "group1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing"),
                new Tuple<String, String>("ClientAccount", "CompanyB")
            };
            mockDistributedOperationCoordinator.GetEntitiesAccessibleByGroupAsync(group).Returns(Task.FromResult<List<Tuple<String, String>>>(testMappings));

            var result = new List<GroupAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToEntityMappingsAsync(group, true)));

            await mockDistributedOperationCoordinator.Received(1).GetEntitiesAccessibleByGroupAsync(group);
            await mockDistributedOperationCoordinator.DidNotReceive().GetGroupToEntityMappingsAsync(group);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual("ClientAccount", result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual("BusinessUnit", result[1].EntityType);
            Assert.AreEqual("Marketing", result[1].Entity);
            Assert.AreEqual(group, result[2].Group);
            Assert.AreEqual("ClientAccount", result[2].EntityType);
            Assert.AreEqual("CompanyB", result[2].Entity);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncEntityTypeOverload_IncludeIndirectMappingsParameterFalse()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            var testMappings = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedOperationCoordinator.GetGroupToEntityMappingsAsync(group, entityType).Returns(Task.FromResult<List<String>>(testMappings));

            var result = new List<GroupAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToEntityMappingsAsync(group, entityType, false)));

            await mockDistributedOperationCoordinator.Received(1).GetGroupToEntityMappingsAsync(group, entityType);
            await mockDistributedOperationCoordinator.DidNotReceive().GetEntitiesAccessibleByGroupAsync(group, entityType);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncEntityTypeOverload_IncludeIndirectMappingsParameterTrue()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            var testMappings = new List<String>()
            {
                "CompanyA",
                "CompanyB",
                "CompanyC"
            };
            mockDistributedOperationCoordinator.GetEntitiesAccessibleByGroupAsync(group, entityType).Returns(Task.FromResult<List<String>>(testMappings));

            var result = new List<GroupAndEntity<String>>(await ConvertToIEnumerable(testDistributedOperationProcessorController.GetGroupToEntityMappingsAsync(group, entityType, true)));

            await mockDistributedOperationCoordinator.Received(1).GetEntitiesAccessibleByGroupAsync(group, entityType);
            await mockDistributedOperationCoordinator.DidNotReceive().GetGroupToEntityMappingsAsync(group, entityType);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
            Assert.AreEqual(group, result[2].Group);
            Assert.AreEqual(entityType, result[2].EntityType);
            Assert.AreEqual("CompanyC", result[2].Entity);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Converts the specified <see cref="IAsyncEnumerable{T}"/> into an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
        /// <param name="asyncEnumerable">The <see cref="IAsyncEnumerable{T}"/> to convert.</param>
        /// <returns>The converted <see cref="IAsyncEnumerable{T}"/>.</returns>
        protected async Task<IEnumerable<T>> ConvertToIEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable)
        {
            var returnList = new List<T>();
            await foreach (T currentItem in asyncEnumerable)
            {
                returnList.Add(currentItem);
            }

            return returnList;
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="DistributedOperationProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class DistributedOperationProcessorController : DistributedOperationProcessorControllerBase
        {
            public DistributedOperationProcessorController
            (
                AsyncQueryProcessorHolder asyncQueryProcessorHolder,
                AsyncEventProcessorHolder asyncEventProcessorHolder,
                ILogger<DistributedOperationProcessorControllerBase> logger
            )
                : base(asyncQueryProcessorHolder, asyncEventProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
