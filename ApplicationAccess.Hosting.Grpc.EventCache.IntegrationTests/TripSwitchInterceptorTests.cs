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
using System.Net.Http;
using System.Threading.Tasks;
using ApplicationAccess.Hosting.Grpc.Client;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Hosting.Rest.EventCache;
using ApplicationAccess.Hosting.Rest.IntegrationTests;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Grpc.EventCache.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
    /// </summary>
    public class TripSwitchInterceptorTests
    {
        private IAccessManagerTemporalEventQueryProcessor<String, String, String, String> mockTemporalEventQueryProcessor;
        private IHostApplicationLifetime mockApplicationLifetime;
        private TripSwitchActuator testActuator;
        private Int32 tripActionCallCount;
        private Action tripAction;
        private TripSwitchException exceptionTripActionException;
        private Action<TripSwitchException> exceptionTripAction;
        private List<TemporalEventBufferItemBase> returnEvents;


        [SetUp]
        protected void SetUp()
        {
            mockTemporalEventQueryProcessor = Substitute.For<IAccessManagerTemporalEventQueryProcessor<String, String, String, String>>();
            testActuator = new TripSwitchActuator();
            mockApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            tripActionCallCount = 0;
            tripAction = () => { tripActionCallCount++; };
            exceptionTripActionException = null;
            exceptionTripAction = (TripSwitchException tripException) =>
            {
                tripActionCallCount++;
                exceptionTripActionException = tripException;
            };
            returnEvents = new List<TemporalEventBufferItemBase>()
            {
                new UserEventBufferItem<String>(Guid.Parse("fb378bca-10c0-4833-8b8a-464a571ef249"), EventAction.Add, "user1", DateTime.UtcNow, -1),
            };
            mockTemporalEventQueryProcessor.GetAllEventsSince(Arg.Any<Guid>()).Returns(returnEvents);
        }

        [TearDown]
        public void TearDown()
        {
            testActuator.Dispose();
        }

        [Test]
        public async Task TripViaActuatorAndThrowExceptionOnSubsequentCalls()
        {
            String whenTrippedExceptionMessage = "The trip switch has been tripped.";
            var whenTrippedException = new Exception(whenTrippedExceptionMessage);
            var testWebApp = new TripSwitchInterceptorTestWebApp(testActuator, whenTrippedException, tripAction);
            testWebApp.Services.GetService<TemporalEventQueryProcessorHolder>().TemporalEventQueryProcessor = mockTemporalEventQueryProcessor;
            HttpClient httpClient = testWebApp.CreateClient();
            HttpMessageHandler httpHandler = testWebApp.Server.CreateHandler();
            GrpcChannelOptions channelOptions = new GrpcChannelOptions()
            {
                HttpHandler = httpHandler
            };
            var grpcClient = new EventCacheClient<String, String, String, String>
            (
                new Uri(httpClient.BaseAddress.ToString()),
                channelOptions,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier()
            );
            IList<TemporalEventBufferItemBase> result = grpcClient.GetAllEventsSince(Guid.NewGuid());
            Assert.AreEqual(returnEvents[0].EventId, result[0].EventId);

            testActuator.Actuate();
            await Task.Delay(1000);
            var e = Assert.Throws<Exception>(delegate
            {
                grpcClient.GetAllEventsSince(Guid.NewGuid());
            });
            Assert.That(e.Message, Does.StartWith(whenTrippedExceptionMessage));

            e = Assert.Throws<Exception>(delegate
            {
                grpcClient.GetAllEventsSince(Guid.NewGuid());
            });
            Assert.That(e.Message, Does.StartWith(whenTrippedExceptionMessage));

            Assert.AreEqual(1, tripActionCallCount);
            grpcClient.Dispose();
            httpHandler.Dispose();
            httpHandler.Dispose();
            testWebApp.Dispose();
        }

        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{TEntryPoint}"/> which instantiates a hosted version of this project for testing a tripswitch that is tripped by a <see cref="TripSwitchActuator"/>.
        /// </summary>
        protected class TripSwitchInterceptorTestWebApp : WebApplicationFactory<Program>
        {
            protected TripSwitchActuator actuator;
            protected IHostApplicationLifetime applicationLifeTime = null;
            protected Int32 shutdownTimeout = -1;
            protected Exception whenTrippedException = null;
            protected Action onTripAction;

            public TripSwitchInterceptorTestWebApp(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction)
                : base()
            {
                this.actuator = actuator;
                this.applicationLifeTime = applicationLifeTime;
                this.shutdownTimeout = shutdownTimeout;
                this.onTripAction = onTripAction;
            }

            public TripSwitchInterceptorTestWebApp(TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction)
                : base()
            {
                this.actuator = actuator;
                this.whenTrippedException = whenTrippedException;
                this.onTripAction = onTripAction;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureServices((IServiceCollection serviceCollection) =>
                {
                    serviceCollection.AddSingleton(typeof(TemporalEventBulkPersisterHolder));
                    serviceCollection.AddSingleton(typeof(TemporalEventQueryProcessorHolder));
                    serviceCollection.AddGrpc(options =>
                    {
                        ErrorHandlingOptions errorHandlingOptions = new()
                        {
                            IncludeInnerExceptions = true,
                            OverrideInternalServerErrors = false, 
                            InternalServerErrorMessageOverride = "An internal server error occurred"
                        };
                        options.Interceptors.Add<ExceptionHandlingInterceptor>(errorHandlingOptions, new ExceptionToGrpcStatusConverter());
                        if (whenTrippedException == null)
                        {
                            options.Interceptors.Add<TripSwitchInterceptor>(actuator, applicationLifeTime, shutdownTimeout, onTripAction);
                        }
                        else
                        {
                            options.Interceptors.Add<TripSwitchInterceptor>(actuator, whenTrippedException, onTripAction);
                        }
                    });
                });
            }
        }

        #endregion
    }
}
