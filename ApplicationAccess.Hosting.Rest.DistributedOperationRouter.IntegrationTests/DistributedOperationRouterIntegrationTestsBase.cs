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
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.IntegrationTests
{
    /// <summary>
    /// Base class for DistributedOperationRouter integration test classes.
    /// </summary>
    public class DistributedOperationRouterIntegrationTestsBase : IntegrationTestsBase
    {
        protected IDistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> mockDistributedAccessManagerOperationRouter;
        protected TripSwitchActuator tripSwitchActuator;
        protected TestDistributedOperationRouter testDistributedOperationRouter;
        protected HttpClient client;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            mockDistributedAccessManagerOperationRouter = Substitute.For<IDistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>>();
            testDistributedOperationRouter = new TestDistributedOperationRouter();
            testDistributedOperationRouter.Services.GetService<AsyncQueryProcessorHolder>().AsyncQueryProcessor = mockDistributedAccessManagerOperationRouter;
            testDistributedOperationRouter.Services.GetService<AsyncEventProcessorHolder>().AsyncEventProcessor = mockDistributedAccessManagerOperationRouter;
            testDistributedOperationRouter.Services.GetService<DistributedAsyncQueryProcessorHolder>().DistributedAsyncQueryProcessor = mockDistributedAccessManagerOperationRouter;
            testDistributedOperationRouter.Services.GetService<DistributedOperationRouterHolder>().DistributedOperationRouter = mockDistributedAccessManagerOperationRouter;
            tripSwitchActuator = testDistributedOperationRouter.Services.GetService<TripSwitchActuator>();
            client = testDistributedOperationRouter.CreateClient();
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            client.Dispose();
            testDistributedOperationRouter.Dispose();
        }

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
