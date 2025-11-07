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
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.UnitTests;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Grpc.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
    /// </summary>
    public class TripSwitchInterceptorTests
    {
        [Test]
        public void Constructors()
        {
            TripSwitchActuator testActuator = new();
            Exception testWhenTrippedException = new("When tripped exception.");
            Action testOnTripAction = () => { };
            IHostApplicationLifetime testApplicationLifeTime = Substitute.For<IHostApplicationLifetime>();

            TripSwitchInterceptor testTripSwitchInterceptor = new(testActuator, testWhenTrippedException);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "whenTrippedException" }, testWhenTrippedException, testTripSwitchInterceptor);


            testTripSwitchInterceptor = new(testActuator, testWhenTrippedException, testOnTripAction);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "whenTrippedException" }, testWhenTrippedException, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "onTripAction" }, testOnTripAction, testTripSwitchInterceptor);


            testTripSwitchInterceptor = new(testActuator, testApplicationLifeTime, 19);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "shutdownTimeout" }, 19, testTripSwitchInterceptor);


            testTripSwitchInterceptor = new(testActuator, testApplicationLifeTime, 21, testOnTripAction);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "shutdownTimeout" }, 21, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "onTripAction" }, testOnTripAction, testTripSwitchInterceptor);
        }

        [Test]
        public void Constructors_GenericClassVersion()
        {
            TripSwitchActuator testActuator = new();
            Exception testWhenTrippedException = new("When tripped exception.");
            Action testOnTripAction = () => { };
            Action<CircularReferenceException> testOnExceptionTripAction = (exception) => { };
            IHostApplicationLifetime testApplicationLifeTime = Substitute.For<IHostApplicationLifetime>();

            TripSwitchInterceptor<CircularReferenceException> testTripSwitchInterceptor = new(testActuator, testWhenTrippedException);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "whenTrippedException" }, testWhenTrippedException, testTripSwitchInterceptor);


            testTripSwitchInterceptor = new(testActuator, testWhenTrippedException, testOnTripAction, testOnExceptionTripAction);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "whenTrippedException" }, testWhenTrippedException, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "onTripAction" }, testOnTripAction, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "onExceptionTripAction" }, testOnExceptionTripAction, testTripSwitchInterceptor);


            testTripSwitchInterceptor = new(testActuator, testApplicationLifeTime, 19);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "shutdownTimeout" }, 19, testTripSwitchInterceptor);


            testTripSwitchInterceptor = new(testActuator, testApplicationLifeTime, 21, testOnTripAction, testOnExceptionTripAction);

            NonPublicFieldAssert.HasValue(new List<String> { "actuator" }, testActuator, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "shutdownTimeout" }, 21, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "onTripAction" }, testOnTripAction, testTripSwitchInterceptor);
            NonPublicFieldAssert.HasValue(new List<String> { "onExceptionTripAction" }, testOnExceptionTripAction, testTripSwitchInterceptor);
        }
    }
}
