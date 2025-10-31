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
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Hosting.Grpc.Client;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Hosting.Rest.EventCache;
using ApplicationAccess.Persistence;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Grpc.EventCache.IntegrationTests
{
    /// <summary>
    /// Base class for integration test classes.
    /// </summary>
    public class IntegrationTestsBase
    {
        protected IAccessManagerTemporalEventBulkPersister<String, String, String, String> mockTemporalEventBulkPersister;
        protected IAccessManagerTemporalEventQueryProcessor<String, String, String, String> mockTemporalEventQueryProcessor;
        protected TestEventCache testEventCache;
        protected HttpClient httpClient;
        protected HttpMessageHandler httpHandler;
        protected EventCacheClient<String, String, String, String> grpcClient;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            mockTemporalEventBulkPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, String, String>>();
            mockTemporalEventQueryProcessor = Substitute.For<IAccessManagerTemporalEventQueryProcessor<String, String, String, String>>();
            testEventCache = new TestEventCache();
            testEventCache.Services.GetService<TemporalEventBulkPersisterHolder>().TemporalEventBulkPersister = mockTemporalEventBulkPersister;
            testEventCache.Services.GetService<TemporalEventQueryProcessorHolder>().TemporalEventQueryProcessor = mockTemporalEventQueryProcessor;
            httpClient = testEventCache.CreateClient();
            httpHandler = testEventCache.Server.CreateHandler();
            GrpcChannelOptions channelOptions = new GrpcChannelOptions()
            {
                HttpHandler = httpHandler
            };
            grpcClient = new EventCacheClient<String, String, String, String>
            (
                new Uri(httpClient.BaseAddress.ToString()),
                channelOptions,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier()
            );
        }

        [OneTimeTearDown]
        protected virtual void OneTimeTearDown()
        {
            grpcClient.Dispose();
            httpHandler.Dispose();
            httpClient.Dispose();
            testEventCache.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="EventCache.Program"/> which instantiates a hosted version of the <see cref="TemporalEventBulkCachingNode{TUser, TGroup, TComponent, TAccess}"/> class for testing.
        /// </summary>
        protected class TestEventCache : WebApplicationFactory<EventCache.Program>
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
