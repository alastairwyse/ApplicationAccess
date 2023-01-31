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
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Controllers;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.GroupToGroupQueryProcessorControllerBase class.
    /// </summary>
    public class GroupToGroupQueryProcessorControllerBaseTests
    {
        private IAccessManagerGroupToGroupQueryProcessor<String> mockGroupToGroupQueryProcessor;
        private ILogger<GroupToGroupQueryProcessorControllerBase> mockLogger;
        private GroupToGroupQueryProcessorController testGroupToGroupQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockGroupToGroupQueryProcessor = Substitute.For<IAccessManagerGroupToGroupQueryProcessor<String>>();
            mockLogger = Substitute.For<ILogger<GroupToGroupQueryProcessorControllerBase>>();
            var groupToGroupQueryProcessorHolder = new GroupToGroupQueryProcessorHolder();
            groupToGroupQueryProcessorHolder.GroupToGroupQueryProcessor = mockGroupToGroupQueryProcessor;
            testGroupToGroupQueryProcessorController = new GroupToGroupQueryProcessorController(groupToGroupQueryProcessorHolder, mockLogger);
        }

        [Test]
        public void GetGroupToGroupMappings_IncludeIndirectMappingsParameterFalse()
        {
            const String fromGroup = "group1";
            var toGroups = new HashSet<String>()
            {
                "group2",
                "group3"
            };
            mockGroupToGroupQueryProcessor.GetGroupToGroupMappings(fromGroup, false).Returns(toGroups);

            var result = new List<FromGroupAndToGroup<String>>(testGroupToGroupQueryProcessorController.GetGroupToGroupMappings(fromGroup, false));

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(fromGroup, false);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(fromGroup, result[0].FromGroup);
            Assert.AreEqual("group2", result[0].ToGroup);
            Assert.AreEqual(fromGroup, result[1].FromGroup);
            Assert.AreEqual("group3", result[1].ToGroup);
        }

        [Test]
        public void GetGroupToGroupMappings_IncludeIndirectMappingsParameterTrue()
        {
            const String fromGroup = "group1";
            var toGroups = new HashSet<String>()
            {
                "group2",
                "group3",
                "group4"
            };
            mockGroupToGroupQueryProcessor.GetGroupToGroupMappings(fromGroup, true).Returns(toGroups);

            var result = new List<FromGroupAndToGroup<String>>(testGroupToGroupQueryProcessorController.GetGroupToGroupMappings(fromGroup, true));

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(fromGroup, true);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(fromGroup, result[0].FromGroup);
            Assert.AreEqual("group2", result[0].ToGroup);
            Assert.AreEqual(fromGroup, result[1].FromGroup);
            Assert.AreEqual("group3", result[1].ToGroup);
            Assert.AreEqual(fromGroup, result[2].FromGroup);
            Assert.AreEqual("group4", result[2].ToGroup);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="GroupToGroupQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class GroupToGroupQueryProcessorController : GroupToGroupQueryProcessorControllerBase
        {
            public GroupToGroupQueryProcessorController(GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder, ILogger<GroupToGroupQueryProcessorControllerBase> logger)
                : base(groupToGroupQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
