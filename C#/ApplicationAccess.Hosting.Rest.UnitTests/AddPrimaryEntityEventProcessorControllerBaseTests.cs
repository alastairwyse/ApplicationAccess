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
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.AddPrimaryEntityEventProcessorControllerBase class.
    /// </summary>
    public class AddPrimaryEntityEventProcessorControllerBaseTests
    {
        private IAccessManagerEntityEventProcessor mockEntityEventProcessor;
        private ILogger<EntityEventProcessorControllerBase> mockLogger;
        private AddPrimaryEntityEventProcessorController testAddPrimaryEntityEventProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockEntityEventProcessor = Substitute.For<IAccessManagerEntityEventProcessor>();
            mockLogger = Substitute.For<ILogger<EntityEventProcessorControllerBase>>();
            var entityEventProcessorHolder = new EntityEventProcessorHolder();
            entityEventProcessorHolder.EntityEventProcessor = mockEntityEventProcessor;
            testAddPrimaryEntityEventProcessorController = new AddPrimaryEntityEventProcessorController(entityEventProcessorHolder, mockLogger);
        }

        [Test]
        public void AddEntityType()
        {
            const String entityType = "ClientAccount";

            StatusCodeResult result = testAddPrimaryEntityEventProcessorController.AddEntityType(entityType);

            mockEntityEventProcessor.Received(1).AddEntityType(entityType);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        [Test]
        public void AddEntity()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";

            StatusCodeResult result = testAddPrimaryEntityEventProcessorController.AddEntity(entityType, entity);

            mockEntityEventProcessor.Received(1).AddEntity(entityType, entity);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="AddPrimaryEntityEventProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class AddPrimaryEntityEventProcessorController : AddPrimaryEntityEventProcessorControllerBase
        {
            public AddPrimaryEntityEventProcessorController(EntityEventProcessorHolder entityEventProcessorHolder, ILogger<EntityEventProcessorControllerBase> logger)
                : base(entityEventProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
