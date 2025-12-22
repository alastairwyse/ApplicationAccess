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
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Hosting.Rest.AsyncClient;
using ApplicationAccess.Hosting.Rest.DistributedWriterAdministratorClient;
using ApplicationAccess.Hosting.Rest.Writer.IntegrationTests;
using ApplicationAccess.Utilities;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.DistributedWriter.IntegrationTests
{
    /// <summary>
    /// Base class for DistributedWriter integration test classes.
    /// </summary>
    public class DistributedWriterIntegrationTestsBase : IntegrationTestsBase
    {
        protected TestUtilities testUtilities;
        protected IManuallyFlushableBufferFlushStrategy mockManuallyFlushableBufferFlushStrategy;
        protected TripSwitchActuator tripSwitchActuator;
        protected TestDistributedWriter testDistributedWriter;
        protected HttpClient httpClient;
        protected DistributedAccessManagerWriterAdministratorClient administratorClient;
        protected IAccessManagerAsyncEventProcessor<String, String, String, String> client;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            testUtilities = new TestUtilities();
            mockManuallyFlushableBufferFlushStrategy = Substitute.For<IManuallyFlushableBufferFlushStrategy>();
            testDistributedWriter = new TestDistributedWriter();
            // TODO: Could mock/override other holder class contents here if required (event processor holders)
            testDistributedWriter.Services.GetService<ManuallyFlushableBufferFlushStrategyHolder>().ManuallyFlushableBufferFlushStrategy = mockManuallyFlushableBufferFlushStrategy;
            tripSwitchActuator = testDistributedWriter.Services.GetService<TripSwitchActuator>();
            httpClient = testDistributedWriter.CreateClient();
            administratorClient = new DistributedAccessManagerWriterAdministratorClient
            (
                httpClient.BaseAddress,
                httpClient,
                0,
                1
            );
            client = new AccessManagerAsyncClient
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
            ((IDisposable)client).Dispose();
            administratorClient.Dispose();
            httpClient.Dispose();
            testDistributedWriter.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{TEntryPoint}"/> which instantiates a hosted version of the <see cref="DistributedWriterNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        protected class TestDistributedWriter : WebApplicationFactory<DistributedWriter.Program>
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
