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
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.GroupToGroupEventProcessorControllerBase class.
    /// </summary>
    public class GroupToGroupEventProcessorControllerBaseTests
    {
        private IAccessManagerGroupToGroupEventProcessor<String> mockGroupToGroupEventProcessor;
        private ILogger<GroupToGroupEventProcessorControllerBase> mockLogger;
        private GroupToGroupEventProcessorController testGroupToGroupEventProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockGroupToGroupEventProcessor = Substitute.For<IAccessManagerGroupToGroupEventProcessor<String>>();
            mockLogger = Substitute.For<ILogger<GroupToGroupEventProcessorControllerBase>>();
            var groupToGroupEventProcessorHolder = new GroupToGroupEventProcessorHolder();
            groupToGroupEventProcessorHolder.GroupToGroupEventProcessor = mockGroupToGroupEventProcessor;
            testGroupToGroupEventProcessorController = new GroupToGroupEventProcessorController(groupToGroupEventProcessorHolder, mockLogger);
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";

            StatusCodeResult result = testGroupToGroupEventProcessorController.AddGroupToGroupMapping(fromGroup, toGroup);

            mockGroupToGroupEventProcessor.Received(1).AddGroupToGroupMapping(fromGroup, toGroup);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="GroupToGroupEventProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class GroupToGroupEventProcessorController : GroupToGroupEventProcessorControllerBase
        {
            public GroupToGroupEventProcessorController(GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder, ILogger<GroupToGroupEventProcessorControllerBase> logger)
                : base(groupToGroupEventProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
