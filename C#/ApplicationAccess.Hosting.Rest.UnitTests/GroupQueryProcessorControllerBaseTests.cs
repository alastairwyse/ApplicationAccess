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
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.GroupQueryProcessorControllerBase class.
    /// </summary>
    public class GroupQueryProcessorControllerBaseTests
    {
        private IAccessManagerGroupQueryProcessor<String, String, String> mockGroupQueryProcessor;
        private ILogger<GroupQueryProcessorControllerBase> mockLogger;
        private GroupQueryProcessorController testGroupQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockGroupQueryProcessor = Substitute.For<IAccessManagerGroupQueryProcessor<String, String, String>>();
            mockLogger = Substitute.For<ILogger<GroupQueryProcessorControllerBase>>();
            var groupQueryProcessorHolder = new GroupQueryProcessorHolder();
            groupQueryProcessorHolder.GroupQueryProcessor = mockGroupQueryProcessor;
            testGroupQueryProcessorController = new GroupQueryProcessorController(groupQueryProcessorHolder, mockLogger);
        }

        [Test]
        public void ContainsGroup_TrueResult()
        {
            const String group = "group1";
            mockGroupQueryProcessor.ContainsGroup(group).Returns<Boolean>(true);

            ActionResult<String> result = testGroupQueryProcessorController.ContainsGroup(group);

            mockGroupQueryProcessor.Received(1).ContainsGroup(group);
            Assert.AreEqual(group, result.Value);
        }

        [Test]
        public void ContainsGroup_FalseResult()
        {
            const String group = "group1";
            mockGroupQueryProcessor.ContainsGroup(group).Returns<Boolean>(false);

            var e = Assert.Throws<NotFoundException>(delegate
            {
                ActionResult<String> result = testGroupQueryProcessorController.ContainsGroup(group);
            });

            mockGroupQueryProcessor.Received(1).ContainsGroup(group);
            Assert.That(e.Message, Does.StartWith($"Group '{group}' does not exist."));
            Assert.AreEqual(group, e.ResourceId);
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings_IncludeIndirectMappingsParameterFalse()
        {
            const String group = "group1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create")
            };
            mockGroupQueryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(group).Returns(testMappings);

            var result = new List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>(testGroupQueryProcessorController.GetGroupToApplicationComponentAndAccessLevelMappings(group, false));

            mockGroupQueryProcessor.Received(1).GetGroupToApplicationComponentAndAccessLevelMappings(group);
            mockGroupQueryProcessor.DidNotReceive().GetApplicationComponentsAccessibleByGroup(group);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual("Order", result[0].ApplicationComponent);
            Assert.AreEqual("View", result[0].AccessLevel);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual("Summary", result[1].ApplicationComponent);
            Assert.AreEqual("Create", result[1].AccessLevel);
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings_IncludeIndirectMappingsParameterTrue()
        {
            const String group = "group1";
            var testMappings = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("Order", "View"),
                new Tuple<String, String>("Summary", "Create"), 
                new Tuple<String, String>("Settings", "Modify")
            };
            mockGroupQueryProcessor.GetApplicationComponentsAccessibleByGroup(group).Returns(testMappings);

            var result = new List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>(testGroupQueryProcessorController.GetGroupToApplicationComponentAndAccessLevelMappings(group, true));

            mockGroupQueryProcessor.Received(1).GetApplicationComponentsAccessibleByGroup(group);
            mockGroupQueryProcessor.DidNotReceive().GetGroupToApplicationComponentAndAccessLevelMappings(group);
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
        public void GetGroupToEntityMappings_IncludeIndirectMappingsParameterFalse()
        {
            const String group = "group1";
            var testMappings = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing")
            };
            mockGroupQueryProcessor.GetGroupToEntityMappings(group).Returns(testMappings);

            var result = new List<GroupAndEntity<String>>(testGroupQueryProcessorController.GetGroupToEntityMappings(group, false));

            mockGroupQueryProcessor.Received(1).GetGroupToEntityMappings(group);
            mockGroupQueryProcessor.DidNotReceive().GetEntitiesAccessibleByGroup(group);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual("ClientAccount", result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual("BusinessUnit", result[1].EntityType);
            Assert.AreEqual("Marketing", result[1].Entity);
        }

        [Test]
        public void GetGroupToEntityMappings_IncludeIndirectMappingsParameterTrue()
        {
            const String group = "group1";
            var testMappings = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("ClientAccount", "CompanyA"),
                new Tuple<String, String>("BusinessUnit", "Marketing"),
                new Tuple<String, String>("ClientAccount", "CompanyB")
            };
            mockGroupQueryProcessor.GetEntitiesAccessibleByGroup(group).Returns(testMappings);

            var result = new List<GroupAndEntity<String>>(testGroupQueryProcessorController.GetGroupToEntityMappings(group, true));

            mockGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroup(group);
            mockGroupQueryProcessor.DidNotReceive().GetGroupToEntityMappings(group);
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
        public void GetGroupToEntityMappingsEntityTypeOverload_IncludeIndirectMappingsParameterFalse()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            var testMappings = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockGroupQueryProcessor.GetGroupToEntityMappings(group, entityType).Returns(testMappings);

            var result = new List<GroupAndEntity<String>>(testGroupQueryProcessorController.GetGroupToEntityMappings(group, entityType, false));

            mockGroupQueryProcessor.Received(1).GetGroupToEntityMappings(group, entityType);
            mockGroupQueryProcessor.DidNotReceive().GetEntitiesAccessibleByGroup(group, entityType);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(group, result[0].Group);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(group, result[1].Group);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
        }

        [Test]
        public void GetGroupToEntityMappingsEntityTypeOverload_IncludeIndirectMappingsParameterTrue()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            var testMappings = new HashSet<String>()
            {
                "CompanyA",
                "CompanyB",
                "CompanyC"
            };
            mockGroupQueryProcessor.GetEntitiesAccessibleByGroup(group, entityType).Returns(testMappings);

            var result = new List<GroupAndEntity<String>>(testGroupQueryProcessorController.GetGroupToEntityMappings(group, entityType, true));

            mockGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroup(group, entityType);
            mockGroupQueryProcessor.DidNotReceive().GetGroupToEntityMappings(group, entityType);
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

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="GroupQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class GroupQueryProcessorController : GroupQueryProcessorControllerBase
        {
            public GroupQueryProcessorController(GroupQueryProcessorHolder groupQueryProcessorHolder, ILogger<GroupQueryProcessorControllerBase> logger)
                : base(groupQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
