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
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
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
        protected IHostApplicationLifetime mockHostApplicationLifetime;
        protected TripSwitchActuator testActuator;
        protected TripSwitchMiddleware<BufferFlushingException> testTripSwitchMiddleware;

        [SetUp]
        protected void SetUp()
        {
            mockApplicationBuilder = Substitute.For<IApplicationBuilder>();
            mockHostApplicationLifetime = Substitute.For<IHostApplicationLifetime>();
            testActuator = new TripSwitchActuator();
        }

        [TearDown]
        public void TearDown()
        {
            testActuator.Dispose();
        }

        [Test]
        public void UseTripSwitch_ShutdownTimeoutParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                mockApplicationBuilder.UseTripSwitch(testActuator, mockHostApplicationLifetime, -1, () => { });
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'shutdownTimeout' with value -1 must be greater than or equal to 0."));


            e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                mockApplicationBuilder.UseTripSwitch<BufferFlushingException>(testActuator, mockHostApplicationLifetime , -1, () => { }, (BufferFlushingException bufferFlushingException) => { });
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'shutdownTimeout' with value -1 must be greater than or equal to 0."));
        }
    }
}
