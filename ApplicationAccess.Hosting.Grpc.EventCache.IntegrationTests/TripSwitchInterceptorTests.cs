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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Grpc.Net.Client;
using ApplicationAccess.Hosting.Grpc.Client;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Hosting.Rest.EventCache;
using ApplicationAccess.Hosting.Rest.IntegrationTests;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
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

        [Test]
        public async Task TripViaActuatorAndShutdownApplication()
        {
            String whenTrippedExceptionMessage = "The trip switch has been tripped.";
            var whenTrippedException = new Exception(whenTrippedExceptionMessage);
            var testWebApp = new TripSwitchInterceptorTestWebApp(testActuator, mockApplicationLifetime, 2, tripAction);
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
            grpcClient.GetAllEventsSince(Guid.NewGuid());
            mockApplicationLifetime.DidNotReceive().StopApplication();
            await Task.Delay(3000);
            mockApplicationLifetime.Received(1).StopApplication();

            Assert.AreEqual(1, tripActionCallCount);
            grpcClient.Dispose();
            httpHandler.Dispose();
            httpHandler.Dispose();
            testWebApp.Dispose();
        }

        [Test]
        public async Task TripViaExceptionAndThrowExceptionOnSubsequentCalls()
        {
            String whenTrippedExceptionMessage = "The trip switch has been tripped.";
            var tripException = new TripSwitchException("Trip switch exception");
            var whenTrippedException = new Exception(whenTrippedExceptionMessage);
            var testWebApp = new TripSwitchInterceptorTestWebApp<TripSwitchException>(testActuator, whenTrippedException, tripAction, exceptionTripAction);
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
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(Arg.Any<Guid>())).Do((callInfo) => throw tripException);

            var e = Assert.Throws<Exception>(delegate
            {
                grpcClient.GetAllEventsSince(Guid.NewGuid());
            });
            Assert.That(e.Message, Does.StartWith(whenTrippedExceptionMessage));

            await Task.Delay(1000);
            e = Assert.Throws<Exception>(delegate
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
            Assert.IsInstanceOf<TripSwitchException>(exceptionTripActionException);
            Assert.That(exceptionTripActionException.Message, Does.StartWith("Trip switch exception"));
            Assert.AreEqual(1, tripActionCallCount);
            grpcClient.Dispose();
            httpHandler.Dispose();
            httpHandler.Dispose();
            testWebApp.Dispose();
        }
        
        [Test]
        public async Task TripViaExceptionAndShutdownApplication()
        {
            var tripException = new TripSwitchException("Trip switch exception");
            var testWebApp = new TripSwitchInterceptorTestWebApp<TripSwitchException>(testActuator, mockApplicationLifetime, 2, tripAction, exceptionTripAction);
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
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(Arg.Any<Guid>())).Do((callInfo) => throw tripException);

            var e = Assert.Throws<Exception>(delegate
            {
                grpcClient.GetAllEventsSince(Guid.NewGuid());
            });
            Assert.That(e.Message, Does.StartWith("Trip switch exception"));
            await Task.Delay(1000);
            mockApplicationLifetime.DidNotReceive().StopApplication();
            await Task.Delay(2000);
            mockApplicationLifetime.Received(1).StopApplication();

            Assert.AreEqual(1, tripActionCallCount);
            Assert.IsInstanceOf<TripSwitchException>(exceptionTripActionException);
            Assert.That(exceptionTripActionException.Message, Does.StartWith("Trip switch exception"));
            grpcClient.Dispose();
            httpHandler.Dispose();
            httpHandler.Dispose();
            testWebApp.Dispose();
        }

        /// <summary>
        /// Checks that the functionality to trip via the actuator still works properly when initializing the trip switch to trip on an exception.
        /// </summary>
        [Test]
        public async Task TripViaActuatorAndThrowExceptionOnSubsequentCalls_TripswitchSetupToTripViaException()
        {
            String whenTrippedExceptionMessage = "The trip switch has been tripped.";
            var whenTrippedException = new Exception(whenTrippedExceptionMessage);
            var testWebApp = new TripSwitchInterceptorTestWebApp<TripSwitchException>(testActuator, whenTrippedException, tripAction, exceptionTripAction);
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
                    // Register 'holder' classes
                    serviceCollection.AddSingleton(typeof(TemporalEventBulkPersisterHolder));
                    serviceCollection.AddSingleton(typeof(TemporalEventQueryProcessorHolder));

                    // Register ExceptionHandlingInterceptor
                    ErrorHandlingOptions errorHandlingOptions = new()
                    {
                        IncludeInnerExceptions = true,
                        OverrideInternalServerErrors = false,
                        InternalServerErrorMessageOverride = "An internal server error occurred"
                    };
                    ExceptionHandlingInterceptor exceptionHandlingInterceptor = new(errorHandlingOptions, new ExceptionToGrpcStatusConverter());
                    serviceCollection.AddSingleton<ExceptionHandlingInterceptor>(exceptionHandlingInterceptor);

                    // Register TripSwitchInterceptor
                    TripSwitchInterceptor tripSwitchInterceptor;
                    if (whenTrippedException == null)
                    {
                        tripSwitchInterceptor = new TripSwitchInterceptor(actuator, applicationLifeTime, shutdownTimeout, onTripAction);
                    }
                    else
                    {
                        tripSwitchInterceptor = new TripSwitchInterceptor(actuator, whenTrippedException, onTripAction);
                    }
                    serviceCollection.AddSingleton<TripSwitchInterceptor>(tripSwitchInterceptor);

                    // Setup gRPC
                    serviceCollection.AddGrpc(options =>
                    {
                        options.Interceptors.Add<ExceptionHandlingInterceptor>();
                        options.Interceptors.Add<TripSwitchInterceptor>();
                    });
                });
            }
        }

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{TEntryPoint}"/> which instantiates a hosted version of this project for testing a tripswitch that is tripped by an instance of <typeparamref name="TTripException"/>.
        /// </summary>
        protected class TripSwitchInterceptorTestWebApp<TTripException> : WebApplicationFactory<Program> where TTripException : Exception
        {
            protected TripSwitchActuator actuator;
            protected IHostApplicationLifetime applicationLifeTime = null;
            protected Int32 shutdownTimeout = -1;
            protected Exception whenTrippedException = null;
            protected Action onTripAction;
            protected Action<TTripException> exceptionTripAction;

            public TripSwitchInterceptorTestWebApp(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction, Action<TTripException> exceptionTripAction)
                : base()
            {
                this.actuator = actuator;
                this.applicationLifeTime = applicationLifeTime;
                this.shutdownTimeout = shutdownTimeout;
                this.onTripAction = onTripAction;
                this.exceptionTripAction = exceptionTripAction;
            }

            public TripSwitchInterceptorTestWebApp(TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction, Action<TTripException> exceptionTripAction)
                : base()
            {
                this.actuator = actuator;
                this.whenTrippedException = whenTrippedException;
                this.onTripAction = onTripAction;
                this.exceptionTripAction = exceptionTripAction;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureServices((IServiceCollection serviceCollection) =>
                {
                    // Register 'holder' classes
                    serviceCollection.AddSingleton(typeof(TemporalEventBulkPersisterHolder));
                    serviceCollection.AddSingleton(typeof(TemporalEventQueryProcessorHolder));

                    // Register ExceptionHandlingInterceptor
                    ErrorHandlingOptions errorHandlingOptions = new()
                    {
                        IncludeInnerExceptions = true,
                        OverrideInternalServerErrors = false,
                        InternalServerErrorMessageOverride = "An internal server error occurred"
                    };
                    ExceptionHandlingInterceptor exceptionHandlingInterceptor = new(errorHandlingOptions, new ExceptionToGrpcStatusConverter());
                    serviceCollection.AddSingleton<ExceptionHandlingInterceptor>(exceptionHandlingInterceptor);

                    // Register TripSwitchInterceptor
                    TripSwitchInterceptor tripSwitchInterceptor;
                    if (whenTrippedException == null)
                    {
                        tripSwitchInterceptor = new TripSwitchInterceptor<TTripException>(actuator, applicationLifeTime, shutdownTimeout, onTripAction, exceptionTripAction);
                    }
                    else
                    {
                        tripSwitchInterceptor = new TripSwitchInterceptor<TTripException>(actuator, whenTrippedException, onTripAction, exceptionTripAction);
                    }
                    serviceCollection.AddSingleton<TripSwitchInterceptor>(tripSwitchInterceptor);

                    // Setup gRPC
                    serviceCollection.AddGrpc(options =>
                    {
                        options.Interceptors.Add<ExceptionHandlingInterceptor>();
                        options.Interceptors.Add<TripSwitchInterceptor>();
                    });
                });
            }
        }

        #endregion
    }
}
