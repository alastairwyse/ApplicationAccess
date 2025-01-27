/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Hosting.Rest.Controllers;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Utilities;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.Controllers.DistributedAsyncQueryProcessorControllerBase class.
    /// </summary>
    public class DistributedAsyncQueryProcessorControllerBaseTests
    {
        private TestUtilities testUtilities;
        private IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String> mockDistributedAccessManagerAsyncQueryProcessor;
        private ILogger<DistributedAsyncQueryProcessorControllerBase> mockLogger;
        private DistributedAsyncQueryProcessorController testDistributedAsyncQueryProcessorController;

        [SetUp]
        protected void SetUp()
        {
            testUtilities = new TestUtilities();
            mockDistributedAccessManagerAsyncQueryProcessor = Substitute.For<IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String>>();
            mockLogger = Substitute.For<ILogger<DistributedAsyncQueryProcessorControllerBase>>();
            var distributedAsyncQueryProcessorHolder = new DistributedAsyncQueryProcessorHolder();
            distributedAsyncQueryProcessorHolder.DistributedAsyncQueryProcessor = mockDistributedAccessManagerAsyncQueryProcessor;
            testDistributedAsyncQueryProcessorController = new DistributedAsyncQueryProcessorController(distributedAsyncQueryProcessorHolder, mockLogger);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync()
        {
            var testGroups = new List<String>{ "group1", "group2" };
            var returnUsers = new List<String>
            {
                "user1",
                "user2",
                "user3"
            };
            mockDistributedAccessManagerAsyncQueryProcessor.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(returnUsers);

            List<String> result = await testDistributedAsyncQueryProcessorController.GetGroupToUserMappingsAsync(testGroups);

            Assert.AreSame(returnUsers, result);
            await mockDistributedAccessManagerAsyncQueryProcessor.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync()
        {
            var testGroups = new List<String> { "group1", "group2" };
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockDistributedAccessManagerAsyncQueryProcessor.HasAccessToApplicationComponentAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testApplicationComponent, testAccessLevel).Returns(true);

            ActionResult<Boolean> result = await testDistributedAsyncQueryProcessorController.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.AreEqual(true, result.Value);
            await mockDistributedAccessManagerAsyncQueryProcessor.Received(1).HasAccessToApplicationComponentAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task HasAccessToEntityAsync()
        {
            var testGroups = new List<String> { "group1", "group2" };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockDistributedAccessManagerAsyncQueryProcessor.HasAccessToEntityAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType, testEntity).Returns(true);

            ActionResult<Boolean> result = await testDistributedAsyncQueryProcessorController.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.AreEqual(true, result.Value);
            await mockDistributedAccessManagerAsyncQueryProcessor.Received(1).HasAccessToEntityAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType, testEntity);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync()
        {
            var testGroups = new List<String> { "group1", "group2" };
            var returnApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Create")
            };
            mockDistributedAccessManagerAsyncQueryProcessor.GetApplicationComponentsAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(returnApplicationComponents);

            var result = new List<ApplicationComponentAndAccessLevel<String, String>>(await ConvertToIEnumerable(testDistributedAsyncQueryProcessorController.GetApplicationComponentsAccessibleByGroupsAsync(testGroups)));

            Assert.AreEqual(2, returnApplicationComponents.Count);
            Assert.AreEqual(returnApplicationComponents[0], Tuple.Create("Order", "View"));
            Assert.AreEqual(returnApplicationComponents[1], Tuple.Create("Order", "Create"));
            await mockDistributedAccessManagerAsyncQueryProcessor.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync()
        {
            var testGroups = new List<String> { "group1", "group2" };
            var returnEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            mockDistributedAccessManagerAsyncQueryProcessor.GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(returnEntities);

            var result = new List<EntityTypeAndEntity>(await ConvertToIEnumerable(testDistributedAsyncQueryProcessorController.GetEntitiesAccessibleByGroupsAsync(testGroups)));

            Assert.AreEqual(2, returnEntities.Count);
            Assert.AreEqual(returnEntities[0], Tuple.Create("ClientAccount", "CompanyA"));
            Assert.AreEqual(returnEntities[1], Tuple.Create("ClientAccount", "CompanyB"));
            await mockDistributedAccessManagerAsyncQueryProcessor.Received(1).GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncGroupAndEntityTypeOverload()
        {
            var testGroups = new List<String> { "group1", "group2" };
            var testEntityType = "ClientAccount";
            var returnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockDistributedAccessManagerAsyncQueryProcessor.GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType).Returns(returnEntities);

            var result = new List<String>(await testDistributedAsyncQueryProcessorController.GetEntitiesAccessibleByGroupsAsync(testGroups, testEntityType));

            Assert.AreEqual(2, returnEntities.Count);
            Assert.AreEqual(returnEntities[0], "CompanyA");
            Assert.AreEqual(returnEntities[1], "CompanyB");
            await mockDistributedAccessManagerAsyncQueryProcessor.Received(1).GetEntitiesAccessibleByGroupsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)), testEntityType);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns an <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/> which checks whether a collection of strings matches the collection in parameter <paramref name="expected"/> irrespective of their enumeration order.
        /// </summary>
        /// <param name="expected">The collection of strings the predicate compares to.</param>
        /// <returns>The <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/>.</returns>
        /// <remarks>Designed to be passed to the 'predicate' parameter of the <see cref="Arg.Any{T}"/> argument matcher.</remarks>
        protected Expression<Predicate<IEnumerable<String>>> EqualIgnoringOrder(IEnumerable<String> expected)
        {
            return testUtilities.EqualIgnoringOrder(expected);
        }

        /// <summary>
        /// Converts the specified <see cref="IAsyncEnumerable{T}"/> into an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the <see cref="IAsyncEnumerable{T}"/>.</typeparam>
        /// <param name="asyncEnumerable">The <see cref="IAsyncEnumerable{T}"/> to convert.</param>
        /// <returns>The converted <see cref="IAsyncEnumerable{T}"/>.</returns>
        protected async Task<IEnumerable<T>> ConvertToIEnumerable<T>(IAsyncEnumerable<T> asyncEnumerable)
        {
            var returnList = new List<T>();
            await foreach (T currentItem in asyncEnumerable)
            {
                returnList.Add(currentItem);
            }

            return returnList;
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Derives from <see cref="DistributedAsyncQueryProcessorControllerBase"/> as it's abstract.
        /// </summary>
        private class DistributedAsyncQueryProcessorController : DistributedAsyncQueryProcessorControllerBase
        {
            public DistributedAsyncQueryProcessorController
            (
                DistributedAsyncQueryProcessorHolder distributedAsyncQueryProcessorHolder,
                ILogger<DistributedAsyncQueryProcessorControllerBase> logger
            )
                : base(distributedAsyncQueryProcessorHolder, logger)
            {
            }
        }

        #endregion
    }
}
