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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Base for middleware that can either either shut down the application or throw a defined exception on receiving subsequent requests, after being tripped/activated by a specified event.
    /// </summary>
    public abstract class TripSwitchMiddlewareBase
    {
        /// <summary>The next middleware in the application middleware pipeline.</summary>
        protected readonly RequestDelegate next;
        /// <summary>The actuator for the trip switch.</summary>
        protected readonly TripSwitchActuator actuator;
        /// <summary>An action to invoke when the switch is tripped.</summary>
        protected readonly Action onTripAction;
        /// <summary>Flag to indicate whether the action in the 'onTripAction' member has been run.</summary>
        protected Boolean onTripActionsRun;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddlewareBase class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        public TripSwitchMiddlewareBase(RequestDelegate next, TripSwitchActuator actuator)
            : this (next, actuator, () => { })
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddlewareBase class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        public TripSwitchMiddlewareBase(RequestDelegate next, TripSwitchActuator actuator, Action onTripAction)
        {
            this.next = next;
            this.actuator = actuator;
            this.onTripAction = onTripAction;
        }

        /// <summary>
        /// Invokes the middleware implementing the trip switch.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the current request.</param>
        public abstract Task InvokeAsync(HttpContext context);
    }
}
