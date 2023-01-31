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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Controllers;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.UserQueryProcessorControllerBase class.
    /// </summary>
    public class UserQueryProcessorControllerBaseTests
    {
        private IAccessManagerUserQueryProcessor<String, String, String, String> mockUserQueryProcessor;
        private ILogger<UserQueryProcessorControllerBase> mockLogger;
        private UserQueryProcessorController testUserQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockUserQueryProcessor = Substitute.For<IAccessManagerUserQueryProcessor<String, String, String, String>>();
            mockLogger = Substitute.For<ILogger<UserQueryProcessorControllerBase>>();
            var userQueryProcessorHolder = new UserQueryProcessorHolder();
            userQueryProcessorHolder.UserQueryProcessor = mockUserQueryProcessor;
            testUserQueryProcessorController = new UserQueryProcessorController(userQueryProcessorHolder, mockLogger);
        }

        [Test]
        public void ContainsUser_TrueResult()
        {
            const String user = "user1";
            mockUserQueryProcessor.ContainsUser(user).Returns<Boolean>(true);

            ActionResult<String> result = testUserQueryProcessorController.ContainsUser(user);

            mockUserQueryProcessor.Received(1).ContainsUser(user);
            Assert.AreEqual(user, result.Value);
        }

        [Test]
        public void ContainsUser_FalseResult()
        {
            const String user = "user1";
            mockUserQueryProcessor.ContainsUser(user).Returns<Boolean>(false);

            var e = Assert.Throws<NotFoundException>(delegate
            {
                ActionResult<String> result = testUserQueryProcessorController.ContainsUser(user);
            });

            mockUserQueryProcessor.Received(1).ContainsUser(user);
            Assert.That(e.Message, Does.StartWith($"User '{user}' does not exist."));
            Assert.AreEqual(user, e.ResourceId);
        }

        [Test]
        public void GetUserToGroupMappings_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            var groups = new HashSet<String>()
            {
                "group1",
                "group2"
            };
            mockUserQueryProcessor.GetUserToGroupMappings(user, false).Returns(groups);

            var result = new List<UserAndGroup<String, String>>(testUserQueryProcessorController.GetUserToGroupMappings(user, false));

            mockUserQueryProcessor.Received(1).GetUserToGroupMappings(user, false);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("group1", result[0].Group);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("group2", result[1].Group);
        }

        [Test]
        public void GetUserToGroupMappings_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            var groups = new HashSet<String>()
            {
                "group1",
                "group2",
                "group3"
            };
            mockUserQueryProcessor.GetUserToGroupMappings(user, true).Returns(groups);

            var result = new List<UserAndGroup<String, String>>(testUserQueryProcessorController.GetUserToGroupMappings(user, true));

            mockUserQueryProcessor.Received(1).GetUserToGroupMappings(user, true);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("group1", result[0].Group);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("group2", result[1].Group);
            Assert.AreEqual(user, result[2].User);
            Assert.AreEqual("group3", result[2].Group);
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create")
            };
            mockUserQueryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(user).Returns(testMappings);

            var result = new List<UserAndApplicationComponentAndAccessLevel<String, String, String>>(testUserQueryProcessorController.GetUserToApplicationComponentAndAccessLevelMappings(user, false));

            mockUserQueryProcessor.Received(1).GetUserToApplicationComponentAndAccessLevelMappings(user);
            mockUserQueryProcessor.DidNotReceive().GetApplicationComponentsAccessibleByUser(user);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("Order", result[0].ApplicationComponent);
            Assert.AreEqual("View", result[0].AccessLevel);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("Summary", result[1].ApplicationComponent);
            Assert.AreEqual("Create", result[1].AccessLevel);
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            var testMappings = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create"),
                new Tuple<String, String>("Settings", "Modify")
            };
            mockUserQueryProcessor.GetApplicationComponentsAccessibleByUser(user).Returns(testMappings);

            var result = new List<UserAndApplicationComponentAndAccessLevel<String, String, String>>(testUserQueryProcessorController.GetUserToApplicationComponentAndAccessLevelMappings(user, true));

            mockUserQueryProcessor.Received(1).GetApplicationComponentsAccessibleByUser(user);
            mockUserQueryProcessor.DidNotReceive().GetUserToApplicationComponentAndAccessLevelMappings(user);
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
        public void GetUserToEntityMappings_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing")
            };
            mockUserQueryProcessor.GetUserToEntityMappings(user).Returns(testMappings);

            var result = new List<UserAndEntity<String>>(testUserQueryProcessorController.GetUserToEntityMappings(user, false));

            mockUserQueryProcessor.Received(1).GetUserToEntityMappings(user);
            mockUserQueryProcessor.DidNotReceive().GetEntitiesAccessibleByUser(user);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual("ClientAccount", result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual("BusinessUnit", result[1].EntityType);
            Assert.AreEqual("Marketing", result[1].Entity);
        }

        [Test]
        public void GetUserToEntityMappings_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            var testMappings = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing"),
                new Tuple<String, String>("ClientAccount", "CompanyB")
            };
            mockUserQueryProcessor.GetEntitiesAccessibleByUser(user).Returns(testMappings);

            var result = new List<UserAndEntity<String>>(testUserQueryProcessorController.GetUserToEntityMappings(user, true));

            mockUserQueryProcessor.Received(1).GetEntitiesAccessibleByUser(user);
            mockUserQueryProcessor.DidNotReceive().GetUserToEntityMappings(user);
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
        public void GetUserToEntityMappingsEntityTypeOverload_IncludeIndirectMappingsParameterFalse()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            var testMappings = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockUserQueryProcessor.GetUserToEntityMappings(user, entityType).Returns(testMappings);

            var result = new List<UserAndEntity<String>>(testUserQueryProcessorController.GetUserToEntityMappings(user, entityType, false));

            mockUserQueryProcessor.Received(1).GetUserToEntityMappings(user, entityType);
            mockUserQueryProcessor.DidNotReceive().GetEntitiesAccessibleByUser(user, entityType);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(user, result[0].User);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(user, result[1].User);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
        }

        [Test]
        public void GetUserToEntityMappingsEntityTypeOverload_IncludeIndirectMappingsParameterTrue()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            var testMappings = new HashSet<String>()
            {
                "CompanyA",
                "CompanyB",
                "CompanyC"
            };
            mockUserQueryProcessor.GetEntitiesAccessibleByUser(user, entityType).Returns(testMappings);

            var result = new List<UserAndEntity<String>>(testUserQueryProcessorController.GetUserToEntityMappings(user, entityType, true));

            mockUserQueryProcessor.Received(1).GetEntitiesAccessibleByUser(user, entityType);
            mockUserQueryProcessor.DidNotReceive().GetUserToEntityMappings(user, entityType);
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

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="UserQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class UserQueryProcessorController : UserQueryProcessorControllerBase
        {
            public UserQueryProcessorController(UserQueryProcessorHolder userQueryProcessorHolder, ILogger<UserQueryProcessorControllerBase> logger)
                : base(userQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
