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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Grpc.Core;
using Grpc.Core.Interceptors;
using ApplicationAccess.Hosting.Rest;

namespace ApplicationAccess.Hosting.Grpc
{
    /// <summary>
    /// An <see cref="Interceptor"/> that can be activated/tripped by a <see cref="TripSwitchActuator"/> instance, and which either either shuts down the application, or throws a specified exception on receiving any subsequent requests after being activated/tripped.
    /// </summary>
    /// <remarks>When initialized to throw an exception after the switch is tripped, this interceptor can be used in conjunction with the <see cref="ExceptionHandlingInterceptor"/> to convert the thrown exception to a specified gRPC <see cref="Status"/>.</remarks>
    public class TripSwitchInterceptor : Interceptor
    {
        /// <summary>The actuator for the trip switch.</summary>
        protected readonly TripSwitchActuator actuator;
        /// <summary>>The exception to throw on receiving any requests after the switch has been tripped.</summary>
        protected readonly Exception whenTrippedException;
        /// <summary>An action to invoke when the switch is tripped.</summary>
        protected readonly Action onTripAction;
        /// <summary>Flag to indicate whether the action in the 'onTripAction' member has been run.</summary>
        protected Boolean onTripActionsRun;
        /// <summary>The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</summary>
        protected readonly IHostApplicationLifetime applicationLifeTime;
        /// <summary>The time to wait in seconds after the switch is tripped to shutdown the application.</summary>
        protected readonly Int32 shutdownTimeout = -1;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        public TripSwitchInterceptor(TripSwitchActuator actuator, Exception whenTrippedException)
        {
            this.actuator = actuator;
            this.whenTrippedException = whenTrippedException;
            this.onTripAction = () => { };
            applicationLifeTime = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        public TripSwitchInterceptor(TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction)
            : this(actuator, whenTrippedException)
        {
            this.onTripAction = onTripAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchInterceptor(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout)
        {
            if (shutdownTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), $"Parameter '{nameof(shutdownTimeout)}' with value {shutdownTimeout} must be greater than or equal to 0.");

            this.actuator = actuator;
            this.whenTrippedException = null;
            this.onTripAction = () => { };
            this.applicationLifeTime = applicationLifeTime;
            this.shutdownTimeout = shutdownTimeout;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchInterceptor(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction)
            : this(actuator, applicationLifeTime, shutdownTimeout)
        {
            if (shutdownTimeout < 0)
                throw new ArgumentOutOfRangeException(nameof(shutdownTimeout), $"Parameter '{nameof(shutdownTimeout)}' with value {shutdownTimeout} must be greater than or equal to 0.");

            this.onTripAction = onTripAction;
        }

        /// <inheritdoc/>
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>
        (
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation
        )
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

                    return await continuation(request, context);
                }
                else
                {
                    throw whenTrippedException;
                }
            }
            else
            {
                return await continuation(request, context);
            }
        }
    }

    /// <summary>
    /// An <see cref="Interceptor"/> that can be activated/tripped by a <see cref="TripSwitchActuator"/> instance, and which either either shuts down the application, or throws a specified exception on receiving any subsequent requests after being activated/tripped.
    /// </summary>
    /// <typeparam name="TTripException">The type of the exception which activates/trips the switch.</typeparam>
    /// <remarks>When initialized to throw an exception after the switch is tripped, this interceptor can be used in conjunction with the <see cref="ExceptionHandlingInterceptor"/> to convert the thrown exception to a specified gRPC <see cref="Status"/>.</remarks>
    public class TripSwitchInterceptor<TTripException> : TripSwitchInterceptor where TTripException : Exception
    {
        /// <summary>An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</summary>
        protected readonly Action<TTripException> onExceptionTripAction;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        public TripSwitchInterceptor(TripSwitchActuator actuator, Exception whenTrippedException)
            : base(actuator, whenTrippedException)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="whenTrippedException">The exception to throw on receiving any requests after the switch has been tripped.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        /// <param name="onExceptionTripAction">An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</param>
        /// <remarks>This constructor initializes the switch to throw the exception in parameter '<paramref name="whenTrippedException"/>' when the switch is tripped.</remarks>
        public TripSwitchInterceptor(TripSwitchActuator actuator, Exception whenTrippedException, Action onTripAction, Action<TTripException> onExceptionTripAction)
            : base(actuator, whenTrippedException, onTripAction)
        {
            this.onExceptionTripAction = onExceptionTripAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchInterceptor(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout)
            : base(actuator, applicationLifeTime, shutdownTimeout)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchInterceptor class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        /// <param name="applicationLifeTime">The <see cref="IHostApplicationLifetime"/> to use to shutdown the application after the switch has been tripped.</param>
        /// <param name="shutdownTimeout">The time to wait in seconds after the switch is tripped to shutdown the application.</param>
        /// <param name="onTripAction">An action to invoke when the switch is tripped.</param>
        /// <param name="onExceptionTripAction">An action to invoke when the switch is tripped by catching an instance of <typeparamref name="TTripException"/>.  Accepts a single parameter which is the exception which tripped the switch.</param>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="shutdownTimeout"/> value is less than 0.</exception>
        /// <remarks>This constructor initializes the switch to shutdown the application when the switch is tripped.</remarks>
        public TripSwitchInterceptor(TripSwitchActuator actuator, IHostApplicationLifetime applicationLifeTime, Int32 shutdownTimeout, Action onTripAction, Action<TTripException> onExceptionTripAction)
            : base(actuator, applicationLifeTime, shutdownTimeout, onTripAction)
        {
            this.onExceptionTripAction = onExceptionTripAction;
        }

        /// <inheritdoc/>
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>
        (
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation
        )
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

                    return await continuation(request, context);
                }
                else
                {
                    throw whenTrippedException;
                }
            }
            else
            {
                try
                {
                    return await continuation(request, context);
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
            }
        }
    }
}
