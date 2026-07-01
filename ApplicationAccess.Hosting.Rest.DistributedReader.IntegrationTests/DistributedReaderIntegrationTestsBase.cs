/*
 * Copyright 2026 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Hosting.Rest.DistributedReaderAdministratorClient;
using ApplicationAccess.Hosting.Rest.Writer.IntegrationTests;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.DistributedReader.IntegrationTests
{
    /// <summary>
    /// Base class for DistributedReader integration test classes.
    /// </summary>
    public class DistributedReaderIntegrationTestsBase : IntegrationTestsBase
    {
        protected TestUtilities testUtilities;
        protected IManuallyLoadableComponent mockManuallyLoadableComponent;
        protected TripSwitchActuator tripSwitchActuator;
        protected TestDistributedReader testDistributedReader;
        protected HttpClient httpClient;
        protected DistributedAccessManagerReaderAdministratorClient administratorClient;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            testUtilities = new TestUtilities();
            mockManuallyLoadableComponent = Substitute.For<IManuallyLoadableComponent>();
            testDistributedReader = new TestDistributedReader();
            // TODO: Could mock/override other holder class contents here if required (event processor holders)
            testDistributedReader.Services.GetService<ManuallyLoadableComponentHolder>().ManuallyLoadableComponent = mockManuallyLoadableComponent;
            tripSwitchActuator = testDistributedReader.Services.GetService<TripSwitchActuator>();
            httpClient = testDistributedReader.CreateClient();
            administratorClient = new DistributedAccessManagerReaderAdministratorClient
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
            administratorClient.Dispose();
            httpClient.Dispose();
            testDistributedReader.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{TEntryPoint}"/> which instantiates a hosted version of the <see cref="DistributedReaderNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        protected class TestDistributedReader : WebApplicationFactory<DistributedReader.Program>
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
