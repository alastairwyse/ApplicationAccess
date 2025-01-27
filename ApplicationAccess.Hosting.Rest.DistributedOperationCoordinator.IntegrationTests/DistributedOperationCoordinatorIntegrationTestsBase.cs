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
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator.IntegrationTests
{
    /// <summary>
    /// Base class for DistributedOperationCoordinator integration test classes.
    /// </summary>
    public abstract class DistributedOperationCoordinatorIntegrationTestsBase : IntegrationTestsBase
    {
        protected IDistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration> mockDistributedAccessManagerOperationCoordinator;
        protected TripSwitchActuator tripSwitchActuator;
        protected TestDistributedOperationCoordinator testDistributedOperationCoordinator;
        protected HttpClient client;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            mockDistributedAccessManagerOperationCoordinator = Substitute.For< IDistributedAccessManagerOperationCoordinator<AccessManagerRestClientConfiguration>>();
            testDistributedOperationCoordinator = new TestDistributedOperationCoordinator();
            testDistributedOperationCoordinator.Services.GetService<AsyncQueryProcessorHolder>().AsyncQueryProcessor = mockDistributedAccessManagerOperationCoordinator;
            testDistributedOperationCoordinator.Services.GetService<AsyncEventProcessorHolder>().AsyncEventProcessor = mockDistributedAccessManagerOperationCoordinator;
            tripSwitchActuator = testDistributedOperationCoordinator.Services.GetService<TripSwitchActuator>();
            client = testDistributedOperationCoordinator.CreateClient();
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            client.Dispose();
            testDistributedOperationCoordinator.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory<ReaderWriter.Program>"/> which instantiates a hosted version of the <see cref="DistributedOperationCoordinatorNode{TClientConfiguration, TClientConfigurationJsonSerializer}"/> class for testing.
        /// </summary>
        protected class TestDistributedOperationCoordinator : WebApplicationFactory<DistributedOperationCoordinator.Program>
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
