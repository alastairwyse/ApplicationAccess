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
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Provides common Initialization routines for ApplicationAccess components hosted as REST-based web APIs.
    /// </summary>
    public class ApplicationInitializer
    {
        /// <summary>The ASP.NET core environment variable to use for integration testing of the component.</summary>
        public static readonly String IntegrationTestingEnvironmentName = "IntegrationTesting";

        /// <summary>Action which (optionally) initializes <see cref="TripSwitchMiddleware{TTripException}"/> on the component.  Accepts a single parameter which is the <see cref="WebApplication"/> hosting the component.</summary>
        protected Action<WebApplication> tripSwitchInitializationAction;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ApplicationInitializer class.
        /// </summary>
        public ApplicationInitializer()
        {
            tripSwitchInitializationAction = (WebApplication app) => { };
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        /// <typeparam name="THostedService">The type of the hosted service which underlies the component, and should be registered using the <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)"/> method.</typeparam>
        /// <param name="parameters">A collection of parameters used to initialize the component.</param>
        /// <returns>A <see cref="WebApplication"/> initialized and ready to host the component.</returns>
        public WebApplication Initialize<THostedService>(ApplicationInitializerParameters parameters)
            where THostedService : class, IHostedService
        {
            ThrowExceptionIfParametersPropertyIsNull(parameters.Args, nameof(parameters.Args), nameof(parameters));
            ThrowExceptionIfStringParametersPropertyIsWhitespace(parameters.SwaggerVersionString, nameof(parameters.SwaggerVersionString), nameof(parameters));
            ThrowExceptionIfStringParametersPropertyIsWhitespace(parameters.SwaggerApplicationName, nameof(parameters.SwaggerApplicationName), nameof(parameters));
            ThrowExceptionIfStringParametersPropertyIsWhitespace(parameters.SwaggerApplicationDescription, nameof(parameters.SwaggerApplicationDescription), nameof(parameters));
            foreach (Tuple<Type, System.Net.HttpStatusCode> currentTuple in parameters.ExceptionToHttpStatusCodeMappings)
            {
                if (currentTuple.Item1.IsAssignableTo(typeof(Exception)) == false)
                    throw new ArgumentException($"Property '{nameof(parameters.ExceptionToHttpStatusCodeMappings)}' of {nameof(parameters)} object contains type '{currentTuple.Item1.FullName}' which does not derive from '{typeof(Exception).FullName}'.", nameof(parameters.ExceptionToHttpStatusCodeMappings));
            }
            foreach (Type currentExceptionType in parameters.ExceptionTypesMappedToStandardHttpErrorResponse)
            {
                if (currentExceptionType.IsAssignableTo(typeof(Exception)) == false)
                    throw new ArgumentException($"Property '{nameof(parameters.ExceptionTypesMappedToStandardHttpErrorResponse)}' of {nameof(parameters)} object contains type '{currentExceptionType.FullName}' which does not derive from '{typeof(Exception).FullName}'.", nameof(parameters.ExceptionTypesMappedToStandardHttpErrorResponse));
            }
            foreach (Tuple<Type, Func<Exception, HttpErrorResponse>> currentTuple in parameters.ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings)
            {
                if (currentTuple.Item1.IsAssignableTo(typeof(Exception)) == false)
                    throw new ArgumentException($"Property '{nameof(parameters.ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings)}' of {nameof(parameters)} object contains type '{currentTuple.Item1.FullName}' which does not derive from '{typeof(Exception).FullName}'.", nameof(parameters.ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings));
            }

            WebApplicationBuilder builder = WebApplication.CreateBuilder(parameters.Args);
            var middlewareUtilities = new MiddlewareUtilities();

            // Add services to the container.
            IMvcBuilder mvcBuilder = builder.Services.AddControllers()
            // Override the default model-binding failure behaviour, to return a HttpErrorResponse object rather than the standard ProblemDetails
            .MapModelBindingFailureToHttpErrorResponse()
            // Return HTTP 406 (not acceptable) statuses if the 'Accept' request header does not match the controller's 'ProducesResponseType' attribute (i.e. '*/*' or 'application/json')
            .ReturnHttpNotAcceptableOnUnsupportedAcceptHeader();

            // Add custom JSON serializers 
            if (parameters.ConfigureJsonOptionsAction != null)
            {
                mvcBuilder.AddJsonOptions(parameters.ConfigureJsonOptionsAction);
            }

            // Allow APIs to be versioned
            builder.Services.SetupApiVersioning();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen((SwaggerGenOptions swaggerGenOptions) =>
            {
                swaggerGenOptions.SwaggerDoc(parameters.SwaggerVersionString, new OpenApiInfo
                {
                    Version = parameters.SwaggerVersionString,
                    Title = parameters.SwaggerApplicationName,
                    Description = parameters.SwaggerApplicationDescription
                });

                // This adds swagger generation for controllers outside this project/assembly
                foreach (Assembly currentAssembly in parameters.SwaggerGenerationAdditionalAssemblies)
                {
                    middlewareUtilities.AddSwaggerGenerationForAssembly(swaggerGenOptions, currentAssembly);
                }
            });

            // TODO: REMOVE AFTER CONTAINERIZING
            //
            //middlewareUtilities.SetupFileLogging(builder, @"C:\Temp", "ApplicationAccessReaderWriterNodeLog");

            // Validate and register top level IOptions configuration items
            parameters.ConfigureOptionsAction.Invoke(builder);

            // Validate and register metric logging options
            builder.Services.AddOptions<MetricLoggingOptions>()
                .Bind(builder.Configuration.GetSection(MetricLoggingOptions.MetricLoggingOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();
            // Validate second level metric logging options
            var metricLoggingOptions = new MetricLoggingOptions();
            ValidateConfigurationSection(builder, metricLoggingOptions, MetricLoggingOptions.MetricLoggingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricBufferProcessing, MetricBufferProcessingOptions.MetricBufferProcessingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricsSqlServerConnection, MetricsSqlServerConnectionOptions.MetricsSqlServerConnectionOptionsName);

            // Register 'holder' classes for the interfaces that comprise IAccessManager
            //   See notes in remarks of class UserQueryProcessorHolder for an explanation
            foreach (Type currentProcessorHolderType in parameters.ProcessorHolderTypes)
            {
                builder.Services.AddSingleton(currentProcessorHolderType);
            }

            // Register the hosted service wrapper
            if (builder.Environment.EnvironmentName != IntegrationTestingEnvironmentName)
            {
                builder.Services.AddHostedService<THostedService>();
            }

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            // Setup the Swagger UI
            app.SetupSwaggerUI(true);

            // Setup custom exception handler in the application's pipeline, so that any exceptions are caught and returned from the API as HttpErrorResponse objects
            var errorHandlingOptions = new ErrorHandlingOptions();
            app.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName).Bind(errorHandlingOptions);
            var exceptionToHttpStatusCodeConverter = new ExceptionToHttpStatusCodeConverter();
            ExceptionToHttpErrorResponseConverter exceptionToHttpErrorResponseConverter = null;
            if (errorHandlingOptions.IncludeInnerExceptions.Value == true)
            {
                exceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter();
            }
            else
            {
                exceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter(0);
            }
            // Add configured exception to status code mappings
            foreach (Tuple<Type, System.Net.HttpStatusCode> currentMapping in parameters.ExceptionToHttpStatusCodeMappings)
            {
                exceptionToHttpStatusCodeConverter.AddMapping(currentMapping.Item1, currentMapping.Item2);
            }
            // Add configured exception to standard HttpErrorResponse conversion functions
            foreach (Type currentExceptionType in parameters.ExceptionTypesMappedToStandardHttpErrorResponse)
            {
                exceptionToHttpErrorResponseConverter.AddConversionFunction(currentExceptionType);
            }
            // Add configured exception to custom HttpErrorResponse conversion functions
            foreach (Tuple<Type, Func<Exception, HttpErrorResponse>> currentMapping in parameters.ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings)
            {
                exceptionToHttpErrorResponseConverter.AddConversionFunction(currentMapping.Item1, currentMapping.Item2);
            }
            middlewareUtilities.SetupExceptionHandler(app, errorHandlingOptions, exceptionToHttpStatusCodeConverter, exceptionToHttpErrorResponseConverter);

            // Add TripSwitchMiddleware if configured
            tripSwitchInitializationAction.Invoke(app);

            app.UseAuthorization();

            app.MapControllers();

            return app;
        }

        /// <summary>
        /// Initializes the component with <see cref="TripSwitchMiddleware{TTripException}"/> enabled.
        /// </summary>
        /// <typeparam name="THostedService">The type of the hosted service which underlies the component, and should be registered using the <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)"/> method.</typeparam>
        /// <typeparam name="TTripSwitchTripException">The type of the critical exception which 'trips' the trip switch.</typeparam>
        /// <param name="parameters">A collection of parameters used to initialize the component.</param>
        /// <returns>A <see cref="WebApplication"/> initialized and ready to host the component.</returns>
        public WebApplication Initialize<THostedService, TTripSwitchTripException>(ApplicationInitializerParameters parameters)
            where THostedService : class, IHostedService
            where TTripSwitchTripException : Exception
        {
            if (parameters.TripSwitchTrippedException == null)
                ThrowExceptionIfParametersPropertyIsNull(parameters.TripSwitchTrippedException, nameof(parameters.TripSwitchTrippedException), nameof(parameters));

            tripSwitchInitializationAction = (WebApplication app) => 
            {
                app.UseTripSwitch<TTripSwitchTripException>(parameters.TripSwitchTrippedException);
            };
            return Initialize<THostedService>(parameters);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Validates <see cref="MetricLoggingOptions"/> obtained from the specified <see cref="WebApplicationBuilder"/> instance.
        /// </summary>
        protected void ValidateMetricLoggingOptions(WebApplicationBuilder builder)
        {
            var metricLoggingOptions = new MetricLoggingOptions();
            ValidateConfigurationSection(builder, metricLoggingOptions, MetricLoggingOptions.MetricLoggingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricBufferProcessing, MetricBufferProcessingOptions.MetricBufferProcessingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricsSqlServerConnection, MetricsSqlServerConnectionOptions.MetricsSqlServerConnectionOptionsName);
        }

        /// <summary>
        /// Validates a specific section of the application configuration.
        /// </summary>
        /// <param name="builder">The builder for the application.</param>
        /// <param name="optionsInstance">An instance of the class holding the section of the configuration.</param>
        /// <param name="optionsSectionName">The name of the section within the configuration (e.g. in 'appsettings.json').</param>
        protected void ValidateConfigurationSection(WebApplicationBuilder builder, Object optionsInstance, String optionsSectionName)
        {
            builder.Configuration.GetSection(optionsSectionName).Bind(optionsInstance);
            var context = new ValidationContext(optionsInstance);
            Validator.ValidateObject(optionsInstance, context, true);
        }

        protected void ThrowExceptionIfParametersPropertyIsNull(Object propertyValue, String propertyName, String parametersName)
        {
            if (propertyValue == null)
                throw new ArgumentNullException(propertyName, $"Property '{propertyName}' of {parametersName} object cannot be null.");
        }

        protected void ThrowExceptionIfStringParametersPropertyIsWhitespace(String propertyValue, String propertyName, String parametersName)
        {
            if (String.IsNullOrWhiteSpace(propertyValue) == true)
                throw new ArgumentException($"Property '{propertyName}' of {parametersName} object cannot be null or empty.", propertyName);
        }

        #endregion
    }
}
