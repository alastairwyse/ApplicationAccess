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
using Google.Protobuf.Collections;
using Google.Rpc;
using Grpc.AspNetCore.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Grpc
{
    /// <summary>
    /// Provides common Initialization routines for ApplicationAccess components hosted as gRPC services.
    /// </summary>
    public class ApplicationInitializer : ApplicationInitializerBase<ApplicationInitializerParameters>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ApplicationInitializer class.
        /// </summary>
        public ApplicationInitializer()
            : base()
        {
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        /// <typeparam name="TGrpcService">The type of the gRPC service which interfaces to the hosted component.</typeparam>
        /// <typeparam name="THostedService">The type of the hosted service which underlies the component, and should be registered using the <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)"/> method.</typeparam>
        /// <param name="parameters">A collection of parameters used to initialize the component.</param>
        /// <returns>A <see cref="WebApplication"/> initialized and ready to host the component.</returns>
        public WebApplication Initialize<TGrpcService, THostedService>(ApplicationInitializerParameters parameters)
            where TGrpcService : class
            where THostedService : class, IHostedService
        {
            ThrowExceptionIfParametersPropertyIsNull(parameters.Args, nameof(parameters.Args), nameof(parameters));
            foreach (Tuple<Type, Code> currentMapping in parameters.ExceptionToGrpcStatusCodeMappings)
            {
                if (currentMapping.Item1.IsAssignableTo(typeof(Exception)) == false)
                    throw new ArgumentException($"Property '{nameof(parameters.ExceptionToGrpcStatusCodeMappings)}' of {nameof(parameters)} object contains type '{currentMapping.Item1.FullName}' which does not derive from '{typeof(Exception).FullName}'.", nameof(parameters.ExceptionToGrpcStatusCodeMappings));
            }
            foreach (Tuple<Type, Func<Exception, Status>> currentTuple in parameters.ExceptionToCustomGrpcStatusGeneratorFunctionMappings)
            {
                if (currentTuple.Item1.IsAssignableTo(typeof(Exception)) == false)
                    throw new ArgumentException($"Property '{nameof(parameters.ExceptionToCustomGrpcStatusGeneratorFunctionMappings)}' of {nameof(parameters)} object contains type '{currentTuple.Item1.FullName}' which does not derive from '{typeof(Exception).FullName}'.", nameof(parameters.ExceptionToCustomGrpcStatusGeneratorFunctionMappings));
            }

            WebApplicationBuilder builder = WebApplication.CreateBuilder(parameters.Args);
            var middlewareUtilities = new MiddlewareUtilities();

            // Validate and register top level IOptions configuration items
            parameters.ConfigureOptionsAction.Invoke(builder);

            // Register and validate metric logging options
            ValidateAndRegisterMetricLoggingOptions(builder);

            // Register 'holder' classes for the interfaces that comprise IAccessManager
            RegisterProcessorHolders(builder, parameters.ProcessorHolderTypes);

            // Create and register the TripSwitchActuator
            CreateAndRegisterTripSwitchActuator(builder, parameters);

            parameters.ConfigureServicesAction.Invoke(builder.Services);

            // Register the hosted service wrapper
            RegisterHostedSerice<THostedService>(builder);

            // Setup file logging if configured
            SetupFileLogging(builder, middlewareUtilities);

            // Setup custom exception handler using gRPC interceptors, so that any exceptions are caught and returned from the service as GrpcError objects
            var errorHandlingOptions = new ErrorHandlingOptions();
            builder.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName).Bind(errorHandlingOptions);
            ExceptionToGrpcStatusConverter exceptionToGrpcStatusConverter = null;
            if (errorHandlingOptions.IncludeInnerExceptions.Value == true)
            {
                exceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter();
            }
            else
            {
                exceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter(0);
            }
            // Add configured exception to standard GrpcStatus conversion functions
            foreach (Tuple<Type, Code> currentMapping in parameters.ExceptionToGrpcStatusCodeMappings)
            {
                exceptionToGrpcStatusConverter.AddConversionFunction(currentMapping.Item1, currentMapping.Item2);
            }
            // Add default exception to status conversion functions
            AddElementNotFoundExceptionConversionFunctions(exceptionToGrpcStatusConverter);
            // Add configured exception to custom Status conversion functions
            foreach (Tuple<Type, Func<Exception, Status>> currentMapping in parameters.ExceptionToCustomGrpcStatusGeneratorFunctionMappings)
            {
                exceptionToGrpcStatusConverter.AddConversionFunction(currentMapping.Item1, currentMapping.Item2);
            }
            // TODO: Find a way to construct this with IApplicationLogger parameter... either getting logger/factory here, or by having the interceptor created through dependency injection
            ExceptionHandlingInterceptor exceptionHandlingInterceptor = new(errorHandlingOptions, exceptionToGrpcStatusConverter);
            builder.Services.AddSingleton<ExceptionHandlingInterceptor>(exceptionHandlingInterceptor);

            //   As per https://learn.microsoft.com/en-us/aspnet/core/grpc/interceptors?view=aspnetcore-8.0#configure-server-interceptors, by default interceptors have a per-request lifetime
            //     Want to register both ExceptionHandlingInterceptor and TripSwitchInterceptor as singletons to match functionality of REST middleware equivalents (and TripSwitchInterceptor needs to be a singleton as it holds state)
            //     Hence need to first register instances of both as singletons in standard dependency injection before registering them in gRPC with the Interceptors.Add() method.
            if (parameters.TripSwitchTrippedException != null)
            {
                TripSwitchInterceptor tripSwitchInterceptor = new (tripSwitchActuator, parameters.TripSwitchTrippedException, () => { });
                builder.Services.AddSingleton<TripSwitchInterceptor>(tripSwitchInterceptor);

                // Add gRPC health checks (using the TripSwitch to report the health)
                TripSwitchHealthCheck tripSwitchHealthCheck = new(tripSwitchActuator);
                builder.Services.AddSingleton<TripSwitchHealthCheck>(tripSwitchHealthCheck);
            }

            builder.Services.AddGrpc(options =>
            {
                options.Interceptors.Add<ExceptionHandlingInterceptor>();
                if (parameters.TripSwitchTrippedException != null)
                {
                    options.Interceptors.Add<TripSwitchInterceptor>();
                }
            });
            if (parameters.TripSwitchTrippedException != null)
            {
                builder.Services.AddGrpcHealthChecks().AddCheck<TripSwitchHealthCheck>("TripSwitch actuated health check");
            }

            WebApplication app = builder.Build();

            if (parameters.TripSwitchTrippedException != null)
            {
                app.Lifetime.ApplicationStopped.Register(() => { tripSwitchActuator.Dispose(); });
            }

            parameters.ConfigureApplicationBuilderAction(app);

            app.MapGrpcService<TGrpcService>();
            if (parameters.TripSwitchTrippedException != null)
            {
                app.MapGrpcHealthChecksService();
            }
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

            return app;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Adds functions to convert *NotFoundException instances (e.g. <see cref="UserNotFoundException{T}"/> to <see cref="Status"/> instances, to the specified <see cref="ExceptionToGrpcStatusConverter"/>.
        /// </summary>
        /// <param name="exceptionToGrpcStatusConverter">The <see cref="ExceptionToGrpcStatusConverter"/> to add the conversion functions to.</param>
        protected void AddElementNotFoundExceptionConversionFunctions(ExceptionToGrpcStatusConverter exceptionToGrpcStatusConverter)
        {
            exceptionToGrpcStatusConverter.AddConversionFunction
            (
                typeof(NotFoundException),
                (Exception exception) =>
                {
                    var notFoundException = (NotFoundException)exception;
                    var attributes = new MapField<String, String>();
                    attributes.Add(nameof(notFoundException.ResourceId), $"{notFoundException.ResourceId}");
                    return ConstructStatusFromException(Code.NotFound, nameof(NotFoundException), exception, attributes);
                }
            );
            exceptionToGrpcStatusConverter.AddConversionFunction
            (
                typeof(UserNotFoundException<String>),
                (Exception exception) =>
                {
                    var userNotFoundException = (UserNotFoundException<String>)exception;
                    var attributes = new MapField<String, String>();
                    attributes.Add("ParameterName", $"{userNotFoundException.ParamName}");
                    attributes.Add("User", $"{userNotFoundException.User}");
                    return ConstructStatusFromException(Code.NotFound, "UserNotFoundException", exception, attributes);
                }
            );
            exceptionToGrpcStatusConverter.AddConversionFunction
            (
                typeof(GroupNotFoundException<String>),
                (Exception exception) =>
                {
                    var groupNotFoundException = (GroupNotFoundException<String>)exception;
                    var attributes = new MapField<String, String>();
                    attributes.Add("ParameterName", $"{groupNotFoundException.ParamName}");
                    attributes.Add("Group", $"{groupNotFoundException.Group}");
                    return ConstructStatusFromException(Code.NotFound, "GroupNotFoundException", exception, attributes);
                }
            );
            exceptionToGrpcStatusConverter.AddConversionFunction
            (
                typeof(EntityTypeNotFoundException),
                (Exception exception) =>
                {
                    var entityTypeNotFoundException = (EntityTypeNotFoundException)exception;
                    var attributes = new MapField<String, String>();
                    attributes.Add("ParameterName", $"{entityTypeNotFoundException.ParamName}");
                    attributes.Add("EntityType", $"{entityTypeNotFoundException.EntityType}");
                    return ConstructStatusFromException(Code.NotFound, nameof(EntityTypeNotFoundException), exception, attributes);
                }
            );
            exceptionToGrpcStatusConverter.AddConversionFunction
            (
                typeof(EntityNotFoundException),
                (Exception exception) =>
                {
                    var entityNotFoundException = (EntityNotFoundException)exception;
                    var attributes = new MapField<String, String>();
                    attributes.Add("ParameterName", $"{entityNotFoundException.ParamName}");
                    attributes.Add("EntityType", $"{entityNotFoundException.EntityType}");
                    attributes.Add("Entity", $"{entityNotFoundException.Entity}");
                    return ConstructStatusFromException(Code.NotFound, nameof(EntityNotFoundException), exception, attributes);
                }
            );
        }

        /// <summary>
        /// Creates <see cref="Status"/> from the specified parameters.
        /// </summary>
        /// <param name="statusCode">The value of the <see cref="Status.Code"/> property.</param>
        /// <param name="grpcErrorCode">The value of the <see cref="GrpcError.Code"/> property of the <see cref="GrpcError"/> wrapped by the <see cref="Status"/>.</param>
        /// <param name="exception">The exception to map to the <see cref="Status"/>.</param>
        /// <param name="attributes">The values to map to the <see cref="GrpcError.Attributes"/> property of the <see cref="GrpcError"/> wrapped by the <see cref="Status"/>.</param>
        /// <returns></returns>
        public Status ConstructStatusFromException(Code statusCode, String grpcErrorCode, Exception exception, MapField<String, String> attributes)
        {
            var grpcError = new GrpcError
            {
                Code = grpcErrorCode,
                Message = exception.Message,
                Attributes = { attributes }
            };
            if (exception.TargetSite != null)
            {
                grpcError.Target = exception.TargetSite.Name;
            }
            return new Status
            {
                Code = (Int32)statusCode,
                Message = exception.Message,
                Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
            };
        }

        #endregion
    }
}
