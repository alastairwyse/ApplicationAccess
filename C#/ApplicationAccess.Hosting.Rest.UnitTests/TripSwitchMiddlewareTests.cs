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
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Persistence;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
    /// </summary>
    public class TripSwitchMiddlewareTests
    {
        protected IApplicationBuilder mockApplicationBuilder;
        protected RequestDelegate nextRequestDelegate;
        protected HttpContext testHttpContext;
        protected TripSwitchMiddleware<BufferFlushingException> testTripSwitchMiddleware;

        [SetUp]
        protected void SetUp()
        {
            mockApplicationBuilder = Substitute.For<IApplicationBuilder>();
            testHttpContext = new DefaultHttpContext();
            nextRequestDelegate = (HttpContext httpContext) =>
            {
                throw new Exception();
            };
        }

        [Test]
        public void UseTripSwitch_ShutdownTimeoutParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                mockApplicationBuilder.UseTripSwitch<BufferFlushingException>(-1);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'shutdownTimeout' with value -1 must be greater than or equal to 0."));
        }

        [Test]
        public void UseTripSwitch_OnTripActionsOverloadShutdownTimeoutParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                mockApplicationBuilder.UseTripSwitch<BufferFlushingException>(-1, Enumerable.Empty<Action<Exception>>());
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'shutdownTimeout' with value -1 must be greater than or equal to 0."));
        }

        [Test]
        public void InvokeAsync_NonTripExceptionsDoNotTripTheSwitch()
        {
            String exceptionMessage = "Non-tripping exception";
            nextRequestDelegate = new RequestDelegate((HttpContext httpContext) =>
            {
                throw new Exception(exceptionMessage);
            });
            testTripSwitchMiddleware = new TripSwitchMiddleware<BufferFlushingException>(nextRequestDelegate, new ServiceUnavailableException("Buffer Flushing Exception"), Enumerable.Empty<Action<BufferFlushingException>>());

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testTripSwitchMiddleware.InvokeAsync(testHttpContext);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
        }

        [Test]
        public void InvokeAsync_ThrowWhenTrippedException()
        {
            String exceptionMessage = "Buffer flushing exception";
            nextRequestDelegate = new RequestDelegate((HttpContext httpContext) =>
            {
                throw new BufferFlushingException(exceptionMessage);
            });
            var whenTrippedException = new ServiceUnavailableException("Service unavailble");
            BufferFlushingException capturedWhenTrippedException = null;
            Action<BufferFlushingException> onTripAction = (BufferFlushingException e) =>
            {
                capturedWhenTrippedException = e;
            };
            testTripSwitchMiddleware = new TripSwitchMiddleware<BufferFlushingException>(nextRequestDelegate, whenTrippedException, new List<Action<BufferFlushingException>>() { onTripAction });

            var e = Assert.ThrowsAsync<BufferFlushingException>(async delegate
            {
                await testTripSwitchMiddleware.InvokeAsync(testHttpContext);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.AreEqual(exceptionMessage, capturedWhenTrippedException.Message);
            // Check that subsequent requests throw the 'whenTrippedException'
            var afterTripException = Assert.ThrowsAsync<ServiceUnavailableException>(async delegate
            {
                await testTripSwitchMiddleware.InvokeAsync(testHttpContext);
            });
            Assert.AreEqual(whenTrippedException, afterTripException);
        }

        // TODO: Add test to test the shutdown on trip functionality (can't mock WebApplication, so would need to create a wrapping interface for it)
    }
}
