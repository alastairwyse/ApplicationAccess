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
using ApplicationAccess.Hosting.Rest.Controllers;
using NUnit.Framework;
using NSubstitute;
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.DistributedGroupQueryProcessorControllerBase class.
    /// </summary>
    public class DistributedGroupQueryProcessorControllerBaseTests
    {
        private IDistributedAccessManagerGroupQueryProcessor<String, String, String> mockDistributedGroupQueryProcessor;
        private ILogger<DistributedGroupQueryProcessorControllerBase> mockLogger;
        private DistributedGroupQueryProcessorController testDistributedGroupQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockDistributedGroupQueryProcessor = Substitute.For<IDistributedAccessManagerGroupQueryProcessor<String, String, String>>();
            mockLogger = Substitute.For<ILogger<DistributedGroupQueryProcessorControllerBase>>();
            var distributedGroupQueryProcessorHolder = new DistributedGroupQueryProcessorHolder();
            distributedGroupQueryProcessorHolder.DistributedGroupQueryProcessor = mockDistributedGroupQueryProcessor;
            testDistributedGroupQueryProcessorController = new DistributedGroupQueryProcessorController(distributedGroupQueryProcessorHolder, mockLogger);
        }

        [Test]
        public void HasAccessToApplicationComponent_TrueResult()
        {
            var groups = new List<String>() { "group1", "group2" };
            const String applicationComponent = "Order";
            const String accessLevel = "View";
            mockDistributedGroupQueryProcessor.HasAccessToApplicationComponent(groups, applicationComponent, accessLevel).Returns<Boolean>(true);

            ActionResult<Boolean> result = testDistributedGroupQueryProcessorController.HasAccessToApplicationComponent(groups, applicationComponent, accessLevel);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToApplicationComponent(groups, applicationComponent, accessLevel);
            Assert.AreEqual(true, result.Value);
        }

        [Test]
        public void HasAccessToApplicationComponent_FalseResult()
        {
            var groups = new List<String>() { "group1", "group2" };
            const String applicationComponent = "Order";
            const String accessLevel = "View";
            mockDistributedGroupQueryProcessor.HasAccessToApplicationComponent(groups, applicationComponent, accessLevel).Returns<Boolean>(false);

            ActionResult<Boolean> result = testDistributedGroupQueryProcessorController.HasAccessToApplicationComponent(groups, applicationComponent, accessLevel);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToApplicationComponent(groups, applicationComponent, accessLevel);
            Assert.AreEqual(false, result.Value);
        }

        [Test]
        public void HasAccessToEntity_TrueResult()
        {
            var groups = new List<String>() { "group1", "group2" };
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockDistributedGroupQueryProcessor.HasAccessToEntity(groups, entityType, entity).Returns<Boolean>(true);

            ActionResult<Boolean> result = testDistributedGroupQueryProcessorController.HasAccessToEntity(groups, entityType, entity);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToEntity(groups, entityType, entity);
            Assert.AreEqual(true, result.Value);
        }

        [Test]
        public void HasAccessToEntity_FalseResult()
        {
            var groups = new List<String>() { "group1", "group2" };
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockDistributedGroupQueryProcessor.HasAccessToEntity(groups, entityType, entity).Returns<Boolean>(false);

            ActionResult<Boolean> result = testDistributedGroupQueryProcessorController.HasAccessToEntity(groups, entityType, entity);

            mockDistributedGroupQueryProcessor.Received(1).HasAccessToEntity(groups, entityType, entity);
            Assert.AreEqual(false, result.Value);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="DistributedGroupQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class DistributedGroupQueryProcessorController : DistributedGroupQueryProcessorControllerBase
        {
            public DistributedGroupQueryProcessorController(DistributedGroupQueryProcessorHolder distributedGroupQueryProcessorHolder, ILogger<DistributedGroupQueryProcessorControllerBase> logger)
                : base(distributedGroupQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
