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
using ApplicationAccess.Hosting.Rest.Controllers;
using NUnit.Framework;
using NSubstitute;
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.DistributedGroupToGroupQueryProcessorControllerBase class.
    /// </summary>
    public class DistributedGroupToGroupQueryProcessorControllerBaseTests
    {
        private IDistributedAccessManagerGroupToGroupQueryProcessor<String> mockDistributedGroupToGroupQueryProcessor;
        private ILogger<DistributedGroupToGroupQueryProcessorControllerBase> mockLogger;
        private DistributedGroupToGroupQueryProcessorController testDistributedGroupToGroupQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockDistributedGroupToGroupQueryProcessor = Substitute.For<IDistributedAccessManagerGroupToGroupQueryProcessor<String>>();
            mockLogger = Substitute.For<ILogger<DistributedGroupToGroupQueryProcessorControllerBase>>();
            var distributedGroupToGroupQueryProcessorHolder = new DistributedGroupToGroupQueryProcessorHolder();
            distributedGroupToGroupQueryProcessorHolder.DistributedGroupToGroupQueryProcessor = mockDistributedGroupToGroupQueryProcessor;
            testDistributedGroupToGroupQueryProcessorController = new DistributedGroupToGroupQueryProcessorController(distributedGroupToGroupQueryProcessorHolder, mockLogger);
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            var groups = new List<String>() { "group1", "group2" };
            var testReturnGroups = new HashSet<String>() { "group1", "group2", "group3", "group4", "group5" };
            mockDistributedGroupToGroupQueryProcessor.GetGroupToGroupMappings(groups).Returns(testReturnGroups);

            var result = new List<String>(testDistributedGroupToGroupQueryProcessorController.GetGroupToGroupMappings(groups));

            mockDistributedGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(groups);
            Assert.AreEqual(5, result.Count);
            Assert.AreEqual(testReturnGroups, result);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="DistributedGroupToGroupQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class DistributedGroupToGroupQueryProcessorController : DistributedGroupToGroupQueryProcessorControllerBase
        {
            public DistributedGroupToGroupQueryProcessorController(DistributedGroupToGroupQueryProcessorHolder distributedGroupToGroupQueryProcessorHolder, ILogger<DistributedGroupToGroupQueryProcessorControllerBase> logger)
                : base(distributedGroupToGroupQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
