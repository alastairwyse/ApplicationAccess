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
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.UserEventProcessorControllerBase class.
    /// </summary>
    public class UserEventProcessorControllerBaseTests
    {
        private IAccessManagerUserEventProcessor<String, String, String, String> mockUserEventProcessor;
        private ILogger<UserEventProcessorControllerBase> mockLogger;
        private UserEventProcessorController testUserEventProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockUserEventProcessor = Substitute.For<IAccessManagerUserEventProcessor<String, String, String, String>>();
            mockLogger = Substitute.For<ILogger<UserEventProcessorControllerBase>>();
            var userEventProcessorHolder = new UserEventProcessorHolder();
            userEventProcessorHolder.UserEventProcessor = mockUserEventProcessor;
            testUserEventProcessorController = new UserEventProcessorController(userEventProcessorHolder, mockLogger);
        }

        [Test]
        public void AddUser()
        {
            const String user = "user1";

            StatusCodeResult result = testUserEventProcessorController.AddUser(user);

            mockUserEventProcessor.Received(1).AddUser(user);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            const String user = "user1";
            const String group = "group1";

            StatusCodeResult result = testUserEventProcessorController.AddUserToGroupMapping(user, group);

            mockUserEventProcessor.Received(1).AddUserToGroupMapping(user, group);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            const String user = "user1";
            const String applicationComponent = "Order";
            const String accessLevel = "Create";

            StatusCodeResult result = testUserEventProcessorController.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);

            mockUserEventProcessor.Received(1).AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";

            StatusCodeResult result = testUserEventProcessorController.AddUserToEntityMapping(user, entityType, entity);

            mockUserEventProcessor.Received(1).AddUserToEntityMapping(user, entityType, entity);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="UserEventProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class UserEventProcessorController : UserEventProcessorControllerBase
        {
            public UserEventProcessorController(UserEventProcessorHolder userEventProcessorHolder, ILogger<UserEventProcessorControllerBase> logger)
                : base(userEventProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
