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
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.DistributedOperationRouterClient;
using ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests;
using ApplicationAccess.Utilities;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.IntegrationTests
{
    /// <summary>
    /// Base class for DistributedOperationRouter integration test classes.
    /// </summary>
    public class DistributedOperationRouterIntegrationTestsBase : IntegrationTestsBase
    {
        protected TestUtilities testUtilities;
        protected IDistributedAccessManagerOperationRouter mockDistributedAccessManagerOperationRouter;
        protected TripSwitchActuator tripSwitchActuator;
        protected TestDistributedOperationRouter testDistributedOperationRouter;
        protected HttpClient httpClient;
        protected DistributedAccessManagerOperationRouterClient client;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            testUtilities = new TestUtilities();
            mockDistributedAccessManagerOperationRouter = Substitute.For<IDistributedAccessManagerOperationRouter>();
            testDistributedOperationRouter = new TestDistributedOperationRouter();
            testDistributedOperationRouter.Services.GetService<AsyncQueryProcessorHolder>().AsyncQueryProcessor = mockDistributedAccessManagerOperationRouter;
            testDistributedOperationRouter.Services.GetService<AsyncEventProcessorHolder>().AsyncEventProcessor = mockDistributedAccessManagerOperationRouter;
            testDistributedOperationRouter.Services.GetService<DistributedAsyncQueryProcessorHolder>().DistributedAsyncQueryProcessor = mockDistributedAccessManagerOperationRouter;
            testDistributedOperationRouter.Services.GetService<DistributedOperationRouterHolder>().DistributedOperationRouter = mockDistributedAccessManagerOperationRouter;
            tripSwitchActuator = testDistributedOperationRouter.Services.GetService<TripSwitchActuator>();
            httpClient = testDistributedOperationRouter.CreateClient();
            client = new DistributedAccessManagerOperationRouterClient
            (
                httpClient.BaseAddress,
                httpClient,
                0, 
                1
            );
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            client.Dispose();
            httpClient.Dispose();
            testDistributedOperationRouter.Dispose();
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
        /// Checks whether two collections of strings contain the same elements irrespective of their enumeration order.
        /// </summary>
        /// <param name="enumerable1">The first collection.</param>
        /// <param name="enumerable2">The second collection.</param>
        /// <returns>True if the collections contain the same string.  False otherwise.</returns>
        protected Boolean StringEnumerablesContainSameValues(IEnumerable<String> enumerable1, IEnumerable<String> enumerable2)
        {
            return testUtilities.StringEnumerablesContainSameValues(enumerable1, enumerable2);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{TEntryPoint}"/> which instantiates a hosted version of the <see cref="DistributedOperationRouterNode{TClientConfiguration}"/> class for testing.
        /// </summary>
        protected class TestDistributedOperationRouter : WebApplicationFactory<DistributedOperationRouter.Program>
        {
            /// <inheritdoc/>
            protected override IHost CreateHost(IHostBuilder builder)
            {
                builder.ConfigureServices((IServiceCollection services) =>
                {
                });
                builder.UseEnvironment(ApplicationInitializer.IntegrationTestingEnvironmentName);

                return base.CreateHost(builder);
            }
        }

        #endregion
    }
}
