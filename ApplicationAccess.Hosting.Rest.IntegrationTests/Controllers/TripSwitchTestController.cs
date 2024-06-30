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
using Microsoft.AspNetCore.Mvc;

namespace ApplicationAccess.Hosting.Rest.IntegrationTests.Controllers
{
    /// <summary>
    /// Controller which tests the functionality of <see cref="TripSwitchMiddleware"/>.
    /// </summary>
    [ApiController]
    [Route("TripSwitchTest")]
    public class TripSwitchTestController : ControllerBase
    {
        /// <summary>The actuator for the trip switch.</summary>
        protected TripSwitchActuator tripSwitchActuator;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.IntegrationTests.Controllers.TripSwitchTestController class.
        /// </summary>
        /// <param name="tripSwitchActuator">The actuator for the trip switch.</param>
        public TripSwitchTestController(TripSwitchActuator tripSwitchActuator)
        {
            this.tripSwitchActuator = tripSwitchActuator;
        }

        /// <summary>
        /// Controller method which returns a fixed string 'TestString'.
        /// </summary>
        [HttpGet]
        [Route("ReturnString")]
        public String ReturnString()
        {
            return "TestString";
        }

        /// <summary>
        /// Controller method which throws a <see cref="TripSwitchException"/>.
        /// </summary>
        /// <exception cref="TripSwitchException"></exception>
        [HttpGet]
        [Route("ThrowTripException")]
        public void ThrowTripException()
        {
            throw new TripSwitchException("Trip switch exception");
        }
    }
}
