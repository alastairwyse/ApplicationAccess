/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
    /// </summary>
    public class TripSwitchMiddlewareTests
    {
        private const String endpointRoutePrefix = "/TripSwitchTest";
        private const String returnStringEndpointRoute = "ReturnString";
        private const String throwTripExceptionEndpointRoute = "ThrowTripException";
        private const String returnStringEndpointResponse = "TestString";

        private IHostApplicationLifetime mockApplicationLifetime;
        private TripSwitchActuator testActuator;
        private Int32 tripActionCallCount;
        private Action tripAction;
        private TripSwitchException exceptionTripActionException;
        private Action<TripSwitchException> exceptionTripAction;

        [SetUp]
        protected void SetUp()
        {
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
            var testWebApp = new TripSwitchMiddlewareTestWebApp(testActuator, whenTrippedException, tripAction);
            var client = testWebApp.CreateClient();
            HttpResponseMessage response = await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            Assert.AreEqual(returnStringEndpointResponse, await response.Content.ReadAsStringAsync());
            
            testActuator.Actuate();
            await Task.Delay(1000);
            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            });
            Assert.AreSame(whenTrippedException, e);
            
            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            });
            Assert.AreSame(whenTrippedException, e);

            Assert.AreEqual(1, tripActionCallCount);
            client.Dispose();
            testWebApp.Dispose();
        }

        [Test]
        public async Task TripViaActuatorAndShutdownApplication()
        {
            var testWebApp = new TripSwitchMiddlewareTestWebApp(testActuator, mockApplicationLifetime, 2, tripAction);
            var client = testWebApp.CreateClient();
            HttpResponseMessage response = await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            Assert.AreEqual(returnStringEndpointResponse, await response.Content.ReadAsStringAsync());

            testActuator.Actuate();
            await Task.Delay(1000);
            await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            mockApplicationLifetime.DidNotReceive().StopApplication();
            await Task.Delay(3000);
            mockApplicationLifetime.Received(1).StopApplication();

            Assert.AreEqual(1, tripActionCallCount);
            client.Dispose();
            testWebApp.Dispose();
        }

        [Test]
        public async Task TripViaExceptionAndThrowExceptionOnSubsequentCalls()
        {
            String whenTrippedExceptionMessage = "The trip switch has been tripped.";
            var whenTrippedException = new Exception(whenTrippedExceptionMessage);
            var testWebApp = new TripSwitchMiddlewareTestWebApp<TripSwitchException>(testActuator, whenTrippedException, tripAction, exceptionTripAction);
            var client = testWebApp.CreateClient();
            HttpResponseMessage response = await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            Assert.AreEqual(returnStringEndpointResponse, await response.Content.ReadAsStringAsync());

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{throwTripExceptionEndpointRoute}");
            });
            Assert.AreSame(whenTrippedException, e);

            await Task.Delay(1000);
            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            });
            Assert.AreSame(whenTrippedException, e);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            });
            Assert.AreSame(whenTrippedException, e);

            Assert.AreEqual(1, tripActionCallCount);
            Assert.IsInstanceOf<TripSwitchException>(exceptionTripActionException);
            Assert.That(exceptionTripActionException.Message, Does.StartWith("Trip switch exception"));
            client.Dispose();
            testWebApp.Dispose();
        }


        [Test]
        public async Task TripViaExceptionAndShutdownApplication()
        {
            var testWebApp = new TripSwitchMiddlewareTestWebApp<TripSwitchException>(testActuator, mockApplicationLifetime, 2, tripAction, exceptionTripAction);
            var client = testWebApp.CreateClient();
            HttpResponseMessage response = await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            Assert.AreEqual(returnStringEndpointResponse, await response.Content.ReadAsStringAsync());

            var tripException = Assert.ThrowsAsync<TripSwitchException>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{throwTripExceptionEndpointRoute}");
            });
            Assert.That(tripException.Message, Does.StartWith("Trip switch exception"));
            await Task.Delay(1000);
            mockApplicationLifetime.DidNotReceive().StopApplication();
            await Task.Delay(2000);
            mockApplicationLifetime.Received(1).StopApplication();

            Assert.AreEqual(1, tripActionCallCount);
            Assert.IsInstanceOf<TripSwitchException>(exceptionTripActionException);
            Assert.That(exceptionTripActionException.Message, Does.StartWith("Trip switch exception"));
            client.Dispose();
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
            var testWebApp = new TripSwitchMiddlewareTestWebApp<TripSwitchException>(testActuator, whenTrippedException, tripAction, exceptionTripAction);
            var client = testWebApp.CreateClient();
            HttpResponseMessage response = await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            Assert.AreEqual(returnStringEndpointResponse, await response.Content.ReadAsStringAsync());

            testActuator.Actuate();
            await Task.Delay(1000);
            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            });
            Assert.AreSame(whenTrippedException, e);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await client.GetAsync($"{endpointRoutePrefix}/{returnStringEndpointRoute}");
            });
            Assert.AreSame(whenTrippedException, e);

            Assert.AreEqual(1, tripActionCallCount);
            client.Dispose();
            testWebApp.Dispose();
        }


        #region Nested Classes

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{TEntryPoint}"/> which instantiates a hosted version of this project for testing a tripswitch that is tripped by a <see cref="TripSwitchActuator"/>.
        /// </summary>
        protected class TripSwitchMiddlewareTestWebApp: WebApplicationFactory<Program>
        {
            protected TripSwitchActuator actuator;
            protected IHostApplicationLifetime applicationLifeTime = null;
            protected Int32 shutdownTimeout = -1;
            protected Exception whenTrippedException = null;
            protected Action onTripAction;
            
            public TripSwitchMiddlewareTestWebApp(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction)
                : base()
            {
                this.actuator = actuator;
                this.applicationLifeTime = applicationLifeTime;
                this.shutdownTimeout = shutdownTimeout;
                this.onTripAction = onTripAction;
            }

            public TripSwitchMiddlewareTestWebApp(TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction)
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
                    serviceCollection.AddSingleton<TripSwitchActuator>(actuator);
                });
                
                builder.Configure((IApplicationBuilder applicationBuilder) =>
                {
                    if (whenTrippedException == null)
                    {
                        applicationBuilder.UseTripSwitch(actuator, applicationLifeTime, shutdownTimeout, onTripAction);
                    }
                    else
                    {
                        applicationBuilder.UseTripSwitch(actuator, whenTrippedException, onTripAction);
                    }
                    // Thanks to https://blog.markvincze.com/overriding-configuration-in-asp-net-core-integration-tests/ for showing how to get this working
                    applicationBuilder.UseRouting();
                    applicationBuilder.UseEndpoints((IEndpointRouteBuilder routeBuilder) =>
                    {
                        routeBuilder.MapControllers();
                    });
                });
            }
        }

        /// <summary>
        /// Subclass of <see cref="WebApplicationFactory{TEntryPoint}"/> which instantiates a hosted version of this project for testing a tripswitch that is tripped by an instance of <typeparamref name="TTripException"/>.
        /// </summary>
        protected class TripSwitchMiddlewareTestWebApp<TTripException> : WebApplicationFactory<Program>
            where TTripException : Exception
        {
            protected TripSwitchActuator actuator;
            protected IHostApplicationLifetime applicationLifeTime = null;
            protected Int32 shutdownTimeout = -1;
            protected Exception whenTrippedException = null;
            protected Action onTripAction;
            protected Action<TTripException> exceptionTripAction;

            public TripSwitchMiddlewareTestWebApp(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction, Action<TTripException> exceptionTripAction)
                : base()
            {
                this.actuator = actuator;
                this.applicationLifeTime = applicationLifeTime;
                this.shutdownTimeout = shutdownTimeout;
                this.onTripAction = onTripAction;
                this.exceptionTripAction = exceptionTripAction;
            }

            public TripSwitchMiddlewareTestWebApp(TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction, Action<TTripException> exceptionTripAction)
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
                    serviceCollection.AddSingleton<TripSwitchActuator>(actuator);
                });

                builder.Configure((IApplicationBuilder applicationBuilder) =>
                {
                    if (whenTrippedException == null)
                    {
                        applicationBuilder.UseTripSwitch<TTripException>(actuator, applicationLifeTime, shutdownTimeout, onTripAction, exceptionTripAction);
                    }
                    else
                    {
                        applicationBuilder.UseTripSwitch<TTripException>(actuator, whenTrippedException, onTripAction, exceptionTripAction);
                    }
                    // Thanks to https://blog.markvincze.com/overriding-configuration-in-asp-net-core-integration-tests/ for showing how to get this working
                    applicationBuilder.UseRouting();
                    applicationBuilder.UseEndpoints((IEndpointRouteBuilder routeBuilder) =>
                    {
                        routeBuilder.MapControllers();
                    });
                });
            }
        }

        #endregion
    }
}
