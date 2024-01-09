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

namespace ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests
{
    /// <summary>
    /// Base class for ReaderWriter integration test classes.
    /// </summary>
    public abstract class ReaderWriterIntegrationTestsBase : IntegrationTestsBase
    {
        protected IAccessManagerEntityEventProcessor mockEntityEventProcessor;
        protected IAccessManagerEntityQueryProcessor mockEntityQueryProcessor;
        protected IAccessManagerGroupEventProcessor<String, String, String> mockGroupEventProcessor;
        protected IAccessManagerGroupQueryProcessor<String, String, String> mockGroupQueryProcessor;
        protected IAccessManagerGroupToGroupEventProcessor<String> mockGroupToGroupEventProcessor;
        protected IAccessManagerGroupToGroupQueryProcessor<String> mockGroupToGroupQueryProcessor;
        protected IAccessManagerUserEventProcessor<String, String, String, String> mockUserEventProcessor;
        protected IAccessManagerUserQueryProcessor<String, String, String, String> mockUserQueryProcessor;
        protected TestReaderWriter testReaderWriter;
        protected HttpClient client;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            mockEntityEventProcessor = Substitute.For<IAccessManagerEntityEventProcessor>();
            mockEntityQueryProcessor = Substitute.For<IAccessManagerEntityQueryProcessor>();
            mockGroupEventProcessor = Substitute.For<IAccessManagerGroupEventProcessor<String, String, String>>();
            mockGroupQueryProcessor = Substitute.For<IAccessManagerGroupQueryProcessor<String, String, String>>();
            mockGroupToGroupEventProcessor = Substitute.For<IAccessManagerGroupToGroupEventProcessor<String>>();
            mockGroupToGroupQueryProcessor = Substitute.For<IAccessManagerGroupToGroupQueryProcessor<String>>();
            mockUserEventProcessor = Substitute.For<IAccessManagerUserEventProcessor<String, String, String, String>>();
            mockUserQueryProcessor = Substitute.For<IAccessManagerUserQueryProcessor<String, String, String, String>>();
            testReaderWriter = new TestReaderWriter();
            testReaderWriter.Services.GetService<EntityEventProcessorHolder>().EntityEventProcessor = mockEntityEventProcessor;
            testReaderWriter.Services.GetService<EntityQueryProcessorHolder>().EntityQueryProcessor = mockEntityQueryProcessor;
            testReaderWriter.Services.GetService<GroupEventProcessorHolder>().GroupEventProcessor = mockGroupEventProcessor;
            testReaderWriter.Services.GetService<GroupQueryProcessorHolder>().GroupQueryProcessor = mockGroupQueryProcessor;
            testReaderWriter.Services.GetService<GroupToGroupEventProcessorHolder>().GroupToGroupEventProcessor = mockGroupToGroupEventProcessor;
            testReaderWriter.Services.GetService<GroupToGroupQueryProcessorHolder>().GroupToGroupQueryProcessor = mockGroupToGroupQueryProcessor;
            testReaderWriter.Services.GetService<UserEventProcessorHolder>().UserEventProcessor = mockUserEventProcessor;
            testReaderWriter.Services.GetService<UserQueryProcessorHolder>().UserQueryProcessor = mockUserQueryProcessor;
            client = testReaderWriter.CreateClient();
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            client.Dispose();
            testReaderWriter.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory<ReaderWriter.Program>"/> which instantiates a hosted version of the <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        protected class TestReaderWriter : WebApplicationFactory<ReaderWriter.Program>
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
