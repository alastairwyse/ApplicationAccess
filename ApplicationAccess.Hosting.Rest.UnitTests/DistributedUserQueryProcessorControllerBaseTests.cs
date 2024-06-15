/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.DistributedUserQueryProcessorControllerBase class.
    /// </summary>
    public class DistributedUserQueryProcessorControllerBaseTests
    {
        private IDistributedAccessManagerUserQueryProcessor<String, String> mockDistributedUserQueryProcessor;
        private ILogger<DistributedUserQueryProcessorControllerBase> mockLogger;
        private DistributedUserQueryProcessorController testDistributedUserQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockDistributedUserQueryProcessor = Substitute.For<IDistributedAccessManagerUserQueryProcessor<String, String>>();
            mockLogger = Substitute.For<ILogger<DistributedUserQueryProcessorControllerBase>>();
            var distributedUserQueryProcessorHolder = new DistributedUserQueryProcessorHolder();
            distributedUserQueryProcessorHolder.DistributedUserQueryProcessor = mockDistributedUserQueryProcessor;
            testDistributedUserQueryProcessorController = new DistributedUserQueryProcessorController(distributedUserQueryProcessorHolder, mockLogger);
        }

        [Test]
        public void GetGroupToUserMappings()
        {
            var groups = new List<String>() { "group4", "group5" };
            var testReturnUsers = new HashSet<String>() { "user1", "user2", "user3" };
            mockDistributedUserQueryProcessor.GetGroupToUserMappings(groups).Returns(testReturnUsers);

            var result = new List<String>(testDistributedUserQueryProcessorController.GetGroupToUserMappings(groups));

            mockDistributedUserQueryProcessor.Received(1).GetGroupToUserMappings(groups);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(testReturnUsers, result);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="DistributedUserQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class DistributedUserQueryProcessorController : DistributedUserQueryProcessorControllerBase
        {
            public DistributedUserQueryProcessorController(DistributedUserQueryProcessorHolder distributedUserQueryProcessorHolder, ILogger<DistributedUserQueryProcessorControllerBase> logger)
                : base(distributedUserQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
