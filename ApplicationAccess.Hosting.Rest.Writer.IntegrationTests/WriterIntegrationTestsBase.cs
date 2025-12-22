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
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.Writer.IntegrationTests
{
    /// <summary>
    /// Base class for Writer integration test classes.
    /// </summary>
    public abstract class WriterIntegrationTestsBase : IntegrationTestsBase
    {
        protected IAccessManagerEntityEventProcessor mockEntityEventProcessor;
        protected IAccessManagerGroupEventProcessor<String, String, String> mockGroupEventProcessor;
        protected IAccessManagerGroupToGroupEventProcessor<String> mockGroupToGroupEventProcessor;
        protected IAccessManagerUserEventProcessor<String, String, String, String> mockUserEventProcessor;
        protected TripSwitchActuator tripSwitchActuator;
        protected TestWriter testWriter;
        protected HttpClient client;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            mockEntityEventProcessor = Substitute.For<IAccessManagerEntityEventProcessor>();
            mockGroupEventProcessor = Substitute.For<IAccessManagerGroupEventProcessor<String, String, String>>();
            mockGroupToGroupEventProcessor = Substitute.For<IAccessManagerGroupToGroupEventProcessor<String>>();
            mockUserEventProcessor = Substitute.For<IAccessManagerUserEventProcessor<String, String, String, String>>();
            testWriter = new TestWriter();
            testWriter.Services.GetService<EntityEventProcessorHolder>().EntityEventProcessor = mockEntityEventProcessor;
            testWriter.Services.GetService<GroupEventProcessorHolder>().GroupEventProcessor = mockGroupEventProcessor;
            testWriter.Services.GetService<GroupToGroupEventProcessorHolder>().GroupToGroupEventProcessor = mockGroupToGroupEventProcessor;
            testWriter.Services.GetService<UserEventProcessorHolder>().UserEventProcessor = mockUserEventProcessor;
            tripSwitchActuator = testWriter.Services.GetService<TripSwitchActuator>();
            client = testWriter.CreateClient();
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            client.Dispose();
            testWriter.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{Writer}"/> which instantiates a hosted version of the <see cref="WriterNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        protected class TestWriter : WebApplicationFactory<Writer.Program>
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
