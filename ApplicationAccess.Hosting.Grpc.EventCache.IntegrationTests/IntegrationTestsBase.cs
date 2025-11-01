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
        protected MethodCallCountingStringUniqueStringifier userStringifier;
        protected MethodCallCountingStringUniqueStringifier groupStringifier;
        protected MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        protected MethodCallCountingStringUniqueStringifier accessLevelStringifier;
        protected TestEventCache testEventCache;
        protected HttpClient httpClient;
        protected HttpMessageHandler httpHandler;
        protected EventCacheClient<String, String, String, String> grpcClient;

        [OneTimeSetUp]
        protected virtual void OneTimeSetUp()
        {
            mockTemporalEventBulkPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, String, String>>();
            mockTemporalEventQueryProcessor = Substitute.For<IAccessManagerTemporalEventQueryProcessor<String, String, String, String>>(); 
            userStringifier = new MethodCallCountingStringUniqueStringifier();
            groupStringifier = new MethodCallCountingStringUniqueStringifier();
            applicationComponentStringifier = new MethodCallCountingStringUniqueStringifier();
            accessLevelStringifier = new MethodCallCountingStringUniqueStringifier();
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
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier
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

        // TODO: This class is also defined in the ApplicationAccess.Hosting.Rest.Client.IntegrationTests, and ApplicationAccess.Hosting.Rest.EventCache.IntegrationTests namespaces
        //   Could look at moving somewhere common

        /// <summary>
        /// Implementation of <see cref="IUniqueStringifier{T}"/> which counts the number of calls to the FromString() and ToString() methods.
        /// </summary>
        protected class MethodCallCountingStringUniqueStringifier : IUniqueStringifier<String>
        {
            public Int32 FromStringCallCount { get; protected set; }
            public Int32 ToStringCallCount { get; protected set; }

            public MethodCallCountingStringUniqueStringifier()
            {
                FromStringCallCount = 0;
                ToStringCallCount = 0;
            }

            /// <inheritdoc/>
            public String FromString(String stringifiedObject)
            {
                FromStringCallCount++;

                return stringifiedObject;
            }

            /// <inheritdoc/>
            public String ToString(String inputObject)
            {
                ToStringCallCount++;

                return inputObject;
            }
        }

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
