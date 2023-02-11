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
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Hosting.Rest.Controllers;
using ApplicationAccess.Hosting.Rest.Utilities;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.EntityQueryProcessorControllerBase class.
    /// </summary>
    public class EntityQueryProcessorControllerBaseTests
    {
        private IAccessManagerEntityQueryProcessor mockEntityQueryProcessor;
        private ILogger<EntityQueryProcessorControllerBase> mockLogger;
        private EntityQueryProcessorController testEntityQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            mockEntityQueryProcessor = Substitute.For<IAccessManagerEntityQueryProcessor>();
            mockLogger = Substitute.For<ILogger<EntityQueryProcessorControllerBase>>();
            var entityQueryProcessorHolder = new EntityQueryProcessorHolder();
            entityQueryProcessorHolder.EntityQueryProcessor = mockEntityQueryProcessor;
            testEntityQueryProcessorController = new EntityQueryProcessorController(entityQueryProcessorHolder, mockLogger);
        }

        [Test]
        public void ContainsEntityType_TrueResult()
        {
            const String entityType = "ClientAccount";
            mockEntityQueryProcessor.ContainsEntityType(entityType).Returns<Boolean>(true);

            ActionResult<String> result = testEntityQueryProcessorController.ContainsEntityType(entityType);

            mockEntityQueryProcessor.Received(1).ContainsEntityType(entityType);
            Assert.AreEqual(entityType, result.Value);
        }

        [Test]
        public void ContainsEntityType_FalseResult()
        {
            const String entityType = "ClientAccount";
            mockEntityQueryProcessor.ContainsEntityType(entityType).Returns<Boolean>(false);

            var e = Assert.Throws<NotFoundException>(delegate
            {
                ActionResult<String> result = testEntityQueryProcessorController.ContainsEntityType(entityType);
            });

            mockEntityQueryProcessor.Received(1).ContainsEntityType(entityType);
            Assert.That(e.Message, Does.StartWith($"Entity type '{entityType}' does not exist."));
            Assert.AreEqual(entityType, e.ResourceId);
        }

        [Test]
        public void GetEntities()
        {
            const String entityType = "ClientAccount";
            var entities = new List<String>() { "CompanyA" , "CompanyB" };
            mockEntityQueryProcessor.GetEntities(entityType).Returns(entities);

            var result = new List<EntityTypeAndEntity>(testEntityQueryProcessorController.GetEntities(entityType));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(entityType, result[0].EntityType);
            Assert.AreEqual("CompanyA", result[0].Entity);
            Assert.AreEqual(entityType, result[1].EntityType);
            Assert.AreEqual("CompanyB", result[1].Entity);
        }

        [Test]
        public void ContainsEntity_TrueResult()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockEntityQueryProcessor.ContainsEntity(entityType, entity).Returns<Boolean>(true);

            ActionResult<EntityTypeAndEntity> result = testEntityQueryProcessorController.ContainsEntity(entityType, entity);

            mockEntityQueryProcessor.Received(1).ContainsEntity(entityType, entity);
            Assert.AreEqual(entityType, result.Value.EntityType);
            Assert.AreEqual(entity, result.Value.Entity);
        }

        [Test]
        public void ContainsEntity_FalseResult()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockEntityQueryProcessor.ContainsEntity(entityType, entity).Returns<Boolean>(false);

            var e = Assert.Throws<NotFoundException>(delegate
            {
                ActionResult<EntityTypeAndEntity> result = testEntityQueryProcessorController.ContainsEntity(entityType, entity);
            });

            mockEntityQueryProcessor.Received(1).ContainsEntity(entityType, entity);
            Assert.That(e.Message, Does.StartWith($"Entity '{entity}' of type '{entityType}' does not exist."));
            Assert.AreEqual(entity, e.ResourceId);
        }

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="EntityQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class EntityQueryProcessorController : EntityQueryProcessorControllerBase
        {
            public EntityQueryProcessorController(EntityQueryProcessorHolder entityQueryProcessorHolder, ILogger<EntityQueryProcessorControllerBase> logger)
                : base(entityQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
