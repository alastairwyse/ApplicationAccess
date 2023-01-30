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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Rest.Controllers;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.GroupEventProcessorControllerBase class.
    /// </summary>
    public class GroupEventProcessorControllerBaseTests
    {
        private IAccessManagerGroupEventProcessor<String, String, String> mockGroupEventProcessor;
        private ILogger<GroupEventProcessorControllerBase> mockLogger;
        private GroupEventProcessorController testGroupEventProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockGroupEventProcessor = Substitute.For<IAccessManagerGroupEventProcessor<String, String, String>>();
            mockLogger = Substitute.For<ILogger<GroupEventProcessorControllerBase>>();
            var groupEventProcessorHolder = new GroupEventProcessorHolder();
            groupEventProcessorHolder.GroupEventProcessor = mockGroupEventProcessor;
            testGroupEventProcessorController = new GroupEventProcessorController(groupEventProcessorHolder, mockLogger);
        }

        [Test]
        public void AddGroup()
        {
            const String group = "group1";

            StatusCodeResult result = testGroupEventProcessorController.AddGroup(group);

            mockGroupEventProcessor.Received(1).AddGroup(group);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            const String group = "group1";
            const String applicationComponent = "Order";
            const String accessLevel = "Create";

            StatusCodeResult result = testGroupEventProcessorController.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);

            mockGroupEventProcessor.Received(1).AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";

            StatusCodeResult result = testGroupEventProcessorController.AddGroupToEntityMapping(group, entityType, entity);

            mockGroupEventProcessor.Received(1).AddGroupToEntityMapping(group, entityType, entity);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="GroupEventProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class GroupEventProcessorController : GroupEventProcessorControllerBase
        {
            public GroupEventProcessorController(GroupEventProcessorHolder groupEventProcessorHolder, ILogger<GroupEventProcessorControllerBase> logger)
                : base(groupEventProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
