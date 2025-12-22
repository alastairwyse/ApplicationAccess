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
using ApplicationAccess.Hosting.Rest.Writer.IntegrationTests;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.Reader.IntegrationTests
{
    /// <summary>
    /// Base class for Reader integration test classes.
    /// </summary>
    public abstract class ReaderIntegrationTestsBase : IntegrationTestsBase
    {
        protected IAccessManagerEntityQueryProcessor mockEntityQueryProcessor;
        protected IAccessManagerGroupQueryProcessor<String, String, String> mockGroupQueryProcessor;
        protected IAccessManagerGroupToGroupQueryProcessor<String> mockGroupToGroupQueryProcessor;
        protected IAccessManagerUserQueryProcessor<String, String, String, String> mockUserQueryProcessor;
        protected TripSwitchActuator tripSwitchActuator;
        protected TestReader testReader;
        protected HttpClient client;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            mockEntityQueryProcessor = Substitute.For<IAccessManagerEntityQueryProcessor>();
            mockGroupQueryProcessor = Substitute.For<IAccessManagerGroupQueryProcessor<String, String, String>>();
            mockGroupToGroupQueryProcessor = Substitute.For<IAccessManagerGroupToGroupQueryProcessor<String>>();
            mockUserQueryProcessor = Substitute.For<IAccessManagerUserQueryProcessor<String, String, String, String>>();
            testReader = new TestReader();
            testReader.Services.GetService<EntityQueryProcessorHolder>().EntityQueryProcessor = mockEntityQueryProcessor;
            testReader.Services.GetService<GroupQueryProcessorHolder>().GroupQueryProcessor = mockGroupQueryProcessor;
            testReader.Services.GetService<GroupToGroupQueryProcessorHolder>().GroupToGroupQueryProcessor = mockGroupToGroupQueryProcessor;
            testReader.Services.GetService<UserQueryProcessorHolder>().UserQueryProcessor = mockUserQueryProcessor;
            tripSwitchActuator = testReader.Services.GetService<TripSwitchActuator>();
            client = testReader.CreateClient();
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            client.Dispose();
            testReader.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{Reader}"/> which instantiates a hosted version of the <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        protected class TestReader : WebApplicationFactory<Reader.Program>
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
