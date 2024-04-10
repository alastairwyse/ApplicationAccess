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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// A middleware that catches a specified critical exception and then either shuts down the application, or throws a specified exception on receiving any subsequent requests.
    /// </summary>
    /// <typeparam name="TTripException">The type of the critical exception which 'trips' the switch.</typeparam>
    /// <remarks>When initialized to throw an exception after the switch is tripped, this middleware can be used in conjunction with the <see cref="MiddlewareUtilities.SetupExceptionHandler(IApplicationBuilder, Hosting.Models.Options.ErrorHandlingOptions, Utilities.ExceptionToHttpStatusCodeConverter, Utilities.ExceptionToHttpErrorResponseConverter)"/> method, to convert the thrown exception to a specified HTTP error status (e.g. <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/503">503</see>).</remarks>
    public class TripSwitchMiddleware<TTripException> where TTripException : Exception
    {
        /// <summary>Whether the switch has been tripped.</summary>
        private volatile Boolean isTripped;
        /// <summary>The next middleware in the application middleware pipeline.</summary>
        private readonly RequestDelegate next;
        /// <summary>>The exception to throw on receiving any requests after the switch has been tripped.</summary>
        private readonly Exception whenTrippedException;
        /// <summary>The <see cref="WebApplication"/> to shutdown after the switch has been tripped.</summary>
        private readonly WebApplication application;
        /// <summary>The time to wait in seconds after the switch is tripped to shutdown the application.</summary>
        private readonly Int32 shutdownTimeout;
        /// <summary>A collection of actions to invoke when the switch is tripped.  The actions accept a single parameter which is the critical exception which tripped the switch.</summary>
        private readonly IEnumerable<Action<TTripException>> onTripActions;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripActions">A collection of actions to invoke when the switch is tripped.  The actions accept a single parameter which is the critical exception which tripped the switch.</param>
        /// <remarks>This constructor initializes the switch to throw an exception on receipt of any requests after the switch has been tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, Exception whenTrippedException, IEnumerable<Action<TTripException>> onTripActions)
        {
            isTripped = false;
            this.next = next;
            this.whenTrippedException = whenTrippedException;
            this.onTripActions = onTripActions;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="application">The <see cref="WebApplication"/> to shutdown after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripActions">A collection of actions to invoke when the switch is tripped.  The actions accept a single parameter which is the critical exception which tripped the switch.</param>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, WebApplication application, Int32 shutdownTimeout, IEnumerable<Action<TTripException>> onTripActions)
        {
            isTripped = false;
            this.next = next;
            this.whenTrippedException = null;
            this.application = application;
            this.shutdownTimeout = shutdownTimeout;
            this.onTripActions = onTripActions;
        }

        /// <summary>
        /// Invokes the middleware implementing the trip switch.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/></param>
        public async Task InvokeAsync(HttpContext context)
        {
            if (isTripped == true)
            {
                throw whenTrippedException;
            }
            else
            {
                try
                {
                    await next(context);
                }
                catch (TTripException e)
                {
                    // Invoke the on-trip actions
                    foreach (Action<TTripException> currentOnTripAction in onTripActions)
                    {
                        currentOnTripAction.Invoke(e);
                    }
                    if (whenTrippedException != null)
                    {
                        isTripped = true;
                        throw;
                    }
                    else
                    {
                        var shutdownThread = new Thread(() =>
                        {
                            if (shutdownTimeout > 0)
                            {
                                Thread.Sleep(shutdownTimeout * 1000);
                            }
                            application.Lifetime.StopApplication();
                        });
                        shutdownThread.Start();
                        throw;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extension methods to add trip switch capabilities to an HTTP application pipeline.
    /// </summary>
    public static class TripSwitcMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="TripSwitchMiddleware{TTripException}"/> to the specified <see cref="IApplicationBuilder"/>, which enables the ability to bypass the pipeline and throw an exception on every request, when a specified critical 'trip' exception is thrown.
        /// </summary>
        /// <typeparam name="TTripException">The type of the exception which should 'trip' the switch and bypass the pipeline for future requests.</typeparam>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseTripSwitch<TTripException>(this IApplicationBuilder applicationBuilder, Exception whenTrippedException)
            where TTripException : Exception
        {
            return applicationBuilder.UseMiddleware<TripSwitchMiddleware<TTripException>>(whenTrippedException, Enumerable.Empty<Action<TTripException>>());
        }

        /// <summary>
        /// Adds the <see cref="TripSwitchMiddleware{TTripException}"/> to the specified <see cref="IApplicationBuilder"/>, which enables the ability to bypass the pipeline and throw an exception on every request, when a specified critical 'trip' exception is thrown.
        /// </summary>
        /// <typeparam name="TTripException">The type of the exception which should 'trip' the switch and bypass the pipeline for future requests.</typeparam>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripActions">A collection of actions to invoke when the switch is tripped.  The actions accept a single parameter which is the critical exception which tripped the switch.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseTripSwitch<TTripException>(this IApplicationBuilder applicationBuilder, Exception whenTrippedException, IEnumerable<Action<TTripException>> onTripActions)
            where TTripException : Exception
        {
            return applicationBuilder.UseMiddleware<TripSwitchMiddleware<TTripException>>(whenTrippedException, onTripActions);
        }

        /// <summary>
        /// Adds the <see cref="TripSwitchMiddleware{TTripException}"/> to the specified <see cref="IApplicationBuilder"/>, which enables the ability to shutdown the application after a specified critical 'trip' exception is thrown.
        /// </summary>
        /// <typeparam name="TTripException">The type of the exception which should 'trip' the switch and bypass the pipeline for future requests.</typeparam>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseTripSwitch<TTripException>(this IApplicationBuilder applicationBuilder, Int32 shutdownTimeout)
            where TTripException : Exception
        {
            return applicationBuilder.UseTripSwitch<TTripException>(shutdownTimeout, Enumerable.Empty<Action<TTripException>>());
        }

        /// <summary>
        /// Adds the <see cref="TripSwitchMiddleware{TTripException}"/> to the specified <see cref="IApplicationBuilder"/>, which enables the ability to shutdown the application after a specified critical 'trip' exception is thrown.
        /// </summary>
        /// <typeparam name="TTripException">The type of the exception which should 'trip' the switch and bypass the pipeline for future requests.</typeparam>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripActions">A collection of actions to invoke when the switch is tripped.  The actions accept a single parameter which is the critical exception which tripped the switch.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseTripSwitch<TTripException>(this IApplicationBuilder applicationBuilder, Int32 shutdownTimeout, IEnumerable<Action<TTripException>> onTripActions)
            where TTripException : Exception
        {
            if (shutdownTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), $"Parameter '{nameof(shutdownTimeout)}' with value {shutdownTimeout} must be greater than or equal to 0.");

            return applicationBuilder.UseMiddleware<TripSwitchMiddleware<TTripException>>(applicationBuilder, shutdownTimeout, onTripActions);
        }
    }
}