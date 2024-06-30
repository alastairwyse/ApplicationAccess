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
using Microsoft.Extensions.Hosting;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// A middleware that can be activated/tripped by a <see cref="TripSwitchActuator"/> instance, and which either either shuts down the application, or throws a specified exception on receiving any subsequent requests after being activated/tripped.
    /// </summary>
    /// <remarks>When initialized to throw an exception after the switch is tripped, this middleware can be used in conjunction with the <see cref="MiddlewareUtilities.SetupExceptionHandler(IApplicationBuilder, Hosting.Models.Options.ErrorHandlingOptions, Utilities.ExceptionToHttpStatusCodeConverter, Utilities.ExceptionToHttpErrorResponseConverter)"/> method, to convert the thrown exception to a specified HTTP error status (e.g. <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/503">503</see>).</remarks>
    public class TripSwitchMiddleware : TripSwitchMiddlewareBase
    {
        /// <summary>>The exception to throw on receiving any requests after the switch has been tripped.</summary>
        protected readonly Exception whenTrippedException;
        /// <summary>The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</summary>
        protected readonly IHostApplicationLifetime applicationLifeTime;
        /// <summary>The time to wait in seconds after the switch is tripped to shutdown the application.</summary>
        protected readonly Int32 shutdownTimeout = -1;
        /// <summary>The action to perform when processing the request pipeline, where the switch has not been tripped/actuated.  Accepts a single parameter which is the <see cref="HttpContext"/> of the current request.</summary>
        protected Func<HttpContext, Task> switchNotActuatedAction;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <remarks>This constructor initializes the switch to throw the exception in parameter '<paramref name="whenTrippedException"/>' when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, Exception whenTrippedException)
            : base (next, actuator)
        {
            this.whenTrippedException = whenTrippedException;
            applicationLifeTime = null;
            InitializeSwitchNotActuatedAction();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        /// <remarks>This constructor initializes the switch to throw the exception in parameter '<paramref name="whenTrippedException"/>' when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction)
            : base(next, actuator, onTripAction)
        {
            this.whenTrippedException = whenTrippedException;
            applicationLifeTime = null;
            InitializeSwitchNotActuatedAction();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout)
            : base(next, actuator)
        {
            if (shutdownTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), $"Parameter '{nameof(shutdownTimeout)}' with value {shutdownTimeout} must be greater than or equal to 0.");

            whenTrippedException = null;
            this.applicationLifeTime = applicationLifeTime;
            this.shutdownTimeout = shutdownTimeout;
            InitializeSwitchNotActuatedAction();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction)
            : base(next, actuator, onTripAction)
        {
            if (shutdownTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), $"Parameter '{nameof(shutdownTimeout)}' with value {shutdownTimeout} must be greater than or equal to 0.");

            whenTrippedException = null;
            this.applicationLifeTime = applicationLifeTime;
            this.shutdownTimeout = shutdownTimeout;
            InitializeSwitchNotActuatedAction();
        }

        /// <inheritdoc/>
        public async override Task InvokeAsync(HttpContext context)
        {
            if (actuator.IsActuated == true)
            {
                if (onTripActionsRun == false)
                {
                    onTripAction.Invoke();
                    onTripActionsRun = true;
                }
                if (shutdownTimeout > -1 && applicationLifeTime != null)
                {
                    var shutdownThread = new Thread(() =>
                    {
                        if (shutdownTimeout > 0)
                        {
                            Thread.Sleep(shutdownTimeout * 1000);
                        }
                        applicationLifeTime.StopApplication();
                    });
                    shutdownThread.Start();
                }
                else
                {
                    throw whenTrippedException;
                }
            }
            else
            {
                await switchNotActuatedAction.Invoke(context);
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'switchNotActuatedAction' member.
        /// </summary>
        protected virtual void InitializeSwitchNotActuatedAction()
        {
            switchNotActuatedAction = async (HttpContext context) =>
            {
                await next(context);
            };
        }

        #endregion
    }

    /// <summary>
    /// A middleware that can be activated/tripped by either a <see cref="TripSwitchActuator"/> instance, or by catching a specified exception thrown in the request pipeline, and which either shuts down the application, or throws a specified exception on receiving any subsequent requests after being activated/tripped.
    /// </summary>
    /// <typeparam name="TTripException">The type of the exception which activates/trips the switch.</typeparam>
    /// <remarks>When initialized to throw an exception after the switch is tripped, this middleware can be used in conjunction with the <see cref="MiddlewareUtilities.SetupExceptionHandler(IApplicationBuilder, Hosting.Models.Options.ErrorHandlingOptions, Utilities.ExceptionToHttpStatusCodeConverter, Utilities.ExceptionToHttpErrorResponseConverter)"/> method, to convert the thrown exception to a specified HTTP error status (e.g. <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Status/503">503</see>).</remarks>
    public class TripSwitchMiddleware<TTripException> : TripSwitchMiddleware where TTripException : Exception
    {
        /// <summary>An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</summary>
        protected readonly Action<TTripException> onExceptionTripAction;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <remarks>This constructor initializes the switch to throw the exception in parameter '<paramref name="whenTrippedException"/>' when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, Exception whenTrippedException)
            : base(next, actuator, whenTrippedException)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped via the <paramref name="actuator"/> parameter.</param>
        /// <param name="onExceptionTripAction">An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</param>
        /// <remarks>This constructor initializes the switch to throw the exception in parameter '<paramref name="whenTrippedException"/>' when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction, Action<TTripException> onExceptionTripAction)
            : base(next, actuator, whenTrippedException, onTripAction)
        {
            this.onExceptionTripAction = onExceptionTripAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout)
            : base(next, actuator, applicationLifeTime, shutdownTimeout)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TripSwitchMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped via the <paramref name="actuator"/> parameter.</param>
        /// <param name="onExceptionTripAction">An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchMiddleware(RequestDelegate next, TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction, Action<TTripException> onExceptionTripAction)
            : base(next, actuator, applicationLifeTime, shutdownTimeout, onTripAction)
        {
            this.onExceptionTripAction = onExceptionTripAction;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'switchNotActuatedAction' member.
        /// </summary>
        protected override void InitializeSwitchNotActuatedAction()
        {
            switchNotActuatedAction = async (HttpContext context) =>
            {
                try
                {
                    await next(context);
                }
                catch (TTripException e)
                {
                    actuator.Actuate();
                    onExceptionTripAction.Invoke(e);
                    onTripActionsRun = true;
                    if (whenTrippedException != null)
                    {
                        throw whenTrippedException;
                    }
                    else
                    {
                        var shutdownThread = new Thread(() =>
                        {
                            if (shutdownTimeout > 0)
                            {
                                Thread.Sleep(shutdownTimeout * 1000);
                            }
                            applicationLifeTime.StopApplication();
                        });
                        shutdownThread.Start();
                        throw;
                    }
                }
            };
        }

        #endregion
    }

    /// <summary>
    /// Extension methods to add trip switch capabilities to an HTTP application pipeline.
    /// </summary>
    public static class TripSwitcMiddlewareExtensions
    {
        /// <summary>
        /// Adds the specified <see cref="TripSwitchMiddleware"/> to the <see cref="IApplicationBuilder">IApplicationBuilder's</see> request pipeline.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the tripswitch middleware to.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        public static IApplicationBuilder UseTripSwitch(this IApplicationBuilder applicationBuilder, TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction)
        {
            if (shutdownTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), $"Parameter '{nameof(shutdownTimeout)}' with value {shutdownTimeout} must be greater than or equal to 0.");

            return applicationBuilder.UseMiddleware<TripSwitchMiddleware>(actuator, applicationLifeTime, shutdownTimeout, onTripAction);
        }

        /// <summary>
        /// Adds the specified <see cref="TripSwitchMiddleware"/> to the <see cref="IApplicationBuilder">IApplicationBuilder's</see> request pipeline.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the tripswitch middleware to.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseTripSwitch(this IApplicationBuilder applicationBuilder, TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction)
        {
            return applicationBuilder.UseMiddleware<TripSwitchMiddleware >(actuator, whenTrippedException, onTripAction);
        }

        /// <summary>
        /// Adds the specified <see cref="TripSwitchMiddleware{TTripException}"/> to the <see cref="IApplicationBuilder">IApplicationBuilder's</see> request pipeline.
        /// </summary>
        /// <typeparam name="TTripException">The type of the exception which activates/trips the switch.</typeparam>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the tripswitch middleware to.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped via the <paramref name="actuator"/> parameter.</param>
        /// <param name="onExceptionTripAction">An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        public static IApplicationBuilder UseTripSwitch<TTripException>(this IApplicationBuilder applicationBuilder, TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction, Action<TTripException> onExceptionTripAction)
            where TTripException : Exception
        {
            if (shutdownTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), $"Parameter '{nameof(shutdownTimeout)}' with value {shutdownTimeout} must be greater than or equal to 0.");

            return applicationBuilder.UseMiddleware<TripSwitchMiddleware <TTripException>>(actuator, applicationLifeTime, shutdownTimeout, onTripAction, onExceptionTripAction);
        }

        /// <summary>
        /// Adds the specified <see cref="TripSwitchMiddleware{TTripException}"/> to the <see cref="IApplicationBuilder">IApplicationBuilder's</see> request pipeline.
        /// </summary>
        /// <typeparam name="TTripException">The type of the exception which activates/trips the switch.</typeparam>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the tripswitch middleware to.</param>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped via the <paramref name="actuator"/> parameter.</param>
        /// <param name="onExceptionTripAction">An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseTripSwitch<TTripException>(this IApplicationBuilder applicationBuilder, TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction, Action<TTripException> onExceptionTripAction)
            where TTripException : Exception
        {
            return applicationBuilder.UseMiddleware<TripSwitchMiddleware<TTripException>>(actuator, whenTrippedException, onTripAction, onExceptionTripAction);
        }
    }
}