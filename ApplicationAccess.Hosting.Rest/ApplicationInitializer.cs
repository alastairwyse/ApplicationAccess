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
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using System.Net;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Provides common Initialization routines for ApplicationAccess components hosted as REST-based web APIs.
    /// </summary>
    public class ApplicationInitializer
    {
        /// <summary>The ASP.NET core environment variable to use for integration testing of the component.</summary>
        public static readonly String IntegrationTestingEnvironmentName = "IntegrationTesting";

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ApplicationInitializer class.
        /// </summary>
        public ApplicationInitializer()
        {
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

                // Group endpoint routes together in the swagger page by their 'ApiExplorerSettings' > 'GroupName' attribute
                //   This allows endpoints defined across multiple files too all appear in the same group
                //   e.g. as occurs for endpoints in the 'UserEventProcessorControllerBase' and 'AddPrimaryUserEventProcessorControllerBase' controller classes
                swaggerGenOptions.TagActionsBy((ApiDescription apiDescription) =>
                {
                    if (apiDescription.GroupName != null)
                    {
                        return new List<String> { apiDescription.GroupName };
                    }
                    else
                    {
                        var controllerActionDescriptor = (ControllerActionDescriptor)apiDescription.ActionDescriptor;
                        if (controllerActionDescriptor != null)
                        {
                            throw new Exception($"'{nameof(apiDescription.GroupName)}' could not be found for controller '{controllerActionDescriptor.ControllerName}'.");
                        }
                        else
                        {
                            throw new Exception($"'{nameof(apiDescription.GroupName)}' could not be found for controller.");
                        }
                    }
                });
                // Omitting this causes only the first encountered 'ApiExplorerSettings' > 'GroupName' attribute to render in swagger
                //   Including it renders groups defined for all controllers
                swaggerGenOptions.DocInclusionPredicate((name, api) => { return true; });
                
                // This adds swagger generation for controllers outside this project/assembly
                foreach (Assembly currentAssembly in parameters.SwaggerGenerationAdditionalAssemblies)
                {
                    middlewareUtilities.AddSwaggerGenerationForAssembly(swaggerGenOptions, currentAssembly);
                }
            });

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
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricsSqlDatabaseConnection, MetricsSqlDatabaseConnectionOptions.MetricsSqlDatabaseConnection);

            // Register 'holder' classes for the interfaces that comprise IAccessManager
            //   See notes in remarks of class UserQueryProcessorHolder for an explanation
            foreach (Type currentProcessorHolderType in parameters.ProcessorHolderTypes)
            {
                builder.Services.AddSingleton(currentProcessorHolderType);
            }

            // Create and register the TripSwitchActuator
            TripSwitchActuator tripSwitchActuator = null;
            if (parameters.TripSwitchTrippedException != null)
            {
                tripSwitchActuator = new TripSwitchActuator();
                builder.Services.AddSingleton<TripSwitchActuator>(tripSwitchActuator);
            }

            // Register the hosted service wrapper
            if (builder.Environment.EnvironmentName != IntegrationTestingEnvironmentName)
            {
                builder.Services.AddHostedService<THostedService>();
            }

            // Setup file logging if configured
            String logFilePath = builder.Configuration.GetValue<String>($"{FileLoggingOptions.FileLoggingOptionsName}:{nameof(FileLoggingOptions.LogFilePath)}");
            String logFileNamePrefix = builder.Configuration.GetValue<String>($"{FileLoggingOptions.FileLoggingOptionsName}:{nameof(FileLoggingOptions.LogFileNamePrefix)}");
            if (logFilePath != null && logFileNamePrefix != null)
            {
                middlewareUtilities.SetupFileLogging(builder, logFilePath, logFileNamePrefix);
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
            // Add default exception to status code mappings
            AddElementNotFoundExceptionHttpStatusCodeMappings(exceptionToHttpStatusCodeConverter);
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
            // Add default exception to custom HttpErrorResponse conversion functions
            AddElementNotFoundExceptionConversionFunctions(exceptionToHttpErrorResponseConverter);
            // Add configured exception to custom HttpErrorResponse conversion functions
            foreach (Tuple<Type, Func<Exception, HttpErrorResponse>> currentMapping in parameters.ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings)
            {
                exceptionToHttpErrorResponseConverter.AddConversionFunction(currentMapping.Item1, currentMapping.Item2);
            }
            middlewareUtilities.SetupExceptionHandler(app, errorHandlingOptions, exceptionToHttpStatusCodeConverter, exceptionToHttpErrorResponseConverter);

            // Add TripSwitchMiddleware
            if (parameters.TripSwitchTrippedException != null)
            {
                app.UseTripSwitch(tripSwitchActuator, parameters.TripSwitchTrippedException, () => { });
                app.Lifetime.ApplicationStopped.Register(() => { tripSwitchActuator.Dispose(); });
            }

            app.UseAuthorization();

            app.MapControllers();

            return app;
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
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricsSqlDatabaseConnection, MetricsSqlDatabaseConnectionOptions.MetricsSqlDatabaseConnection);
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

        /// <summary>
        /// Adds mappings from *NotFoundException instances (e.g. <see cref="UserNotFoundException{T}"/> to HTTP status codes, to the specified <see cref="ExceptionToHttpStatusCodeConverter"/>.
        /// </summary>
        /// <param name="exceptionToHttpStatusCodeConverter">The <see cref="ExceptionToHttpStatusCodeConverter"/> to add the mappings to.</param>
        protected void AddElementNotFoundExceptionHttpStatusCodeMappings(ExceptionToHttpStatusCodeConverter exceptionToHttpStatusCodeConverter)
        {
            exceptionToHttpStatusCodeConverter.AddMapping(typeof(UserNotFoundException<String>), HttpStatusCode.NotFound);
            exceptionToHttpStatusCodeConverter.AddMapping(typeof(GroupNotFoundException<String>), HttpStatusCode.NotFound);
            exceptionToHttpStatusCodeConverter.AddMapping(typeof(EntityTypeNotFoundException), HttpStatusCode.NotFound);
            exceptionToHttpStatusCodeConverter.AddMapping(typeof(EntityNotFoundException), HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Adds conversion functions to <see cref="HttpErrorResponse"/> objects for *NotFoundException instances (e.g. <see cref="UserNotFoundException{T}"/>).
        /// </summary>
        /// <param name="exceptionToHttpErrorResponseConverter">The <see cref="ExceptionToHttpErrorResponseConverter"/> to add the conversion functions to.</param>
        protected void AddElementNotFoundExceptionConversionFunctions(ExceptionToHttpErrorResponseConverter exceptionToHttpErrorResponseConverter)
        {
            exceptionToHttpErrorResponseConverter.AddConversionFunction
            (
                typeof(UserNotFoundException<String>),
                (Exception exception) =>
                {
                    var userNotFoundException = (UserNotFoundException<String>)exception;
                    var attributes = new List<Tuple<String, String>>()
                    {
                        new Tuple<String, String>("ParameterName", $"{userNotFoundException.ParamName}"), 
                        new Tuple<String, String>("User", $"{userNotFoundException.User}")
                    };
                    return ConstructHttpErrorResponseFromException("UserNotFoundException", userNotFoundException, attributes);
                }
            );
            exceptionToHttpErrorResponseConverter.AddConversionFunction
            (
                typeof(GroupNotFoundException<String>),
                (Exception exception) =>
                {
                    var groupNotFoundException = (GroupNotFoundException<String>)exception;
                    var attributes = new List<Tuple<String, String>>()
                    {
                        new Tuple<String, String>("ParameterName", $"{groupNotFoundException.ParamName}"),
                        new Tuple<String, String>("Group", $"{groupNotFoundException.Group}")
                    };
                    return ConstructHttpErrorResponseFromException("GroupNotFoundException", groupNotFoundException, attributes);
                }
            );
            exceptionToHttpErrorResponseConverter.AddConversionFunction
            (
                typeof(EntityTypeNotFoundException),
                (Exception exception) =>
                {
                    var entityTypeNotFoundException = (EntityTypeNotFoundException)exception;
                    var attributes = new List<Tuple<String, String>>()
                    {
                        new Tuple<String, String>("ParameterName", $"{entityTypeNotFoundException.ParamName}"),
                        new Tuple<String, String>("EntityType", $"{entityTypeNotFoundException.EntityType}")
                    };
                    return ConstructHttpErrorResponseFromException(entityTypeNotFoundException.GetType().Name, entityTypeNotFoundException, attributes);
                }
            );
            exceptionToHttpErrorResponseConverter.AddConversionFunction
            (
                typeof(EntityNotFoundException),
                (Exception exception) =>
                {
                    var entityNotFoundException = (EntityNotFoundException)exception;
                    var attributes = new List<Tuple<String, String>>()
                    {
                        new Tuple<String, String>("ParameterName", $"{entityNotFoundException.ParamName}"),
                        new Tuple<String, String>("EntityType", $"{entityNotFoundException.EntityType}"), 
                        new Tuple<String, String>("Entity", $"{entityNotFoundException.Entity}")
                    };
                    return ConstructHttpErrorResponseFromException(entityNotFoundException.GetType().Name, entityNotFoundException, attributes);
                }
            );
        }

        /// <summary>
        /// Creates an <see cref="HttpErrorResponse"/> from the specified parameters.
        /// </summary>
        /// <param name="code">The 'code' property of the <see cref="HttpErrorResponse"/>.</param>
        /// <param name="exception">The exception to map to the <see cref="HttpErrorResponse"/>.</param>
        /// <param name="attributes">The values to map to the 'attributes' property of the <see cref="HttpErrorResponse"/>.</param>
        /// <returns></returns>
        protected HttpErrorResponse ConstructHttpErrorResponseFromException(String code, Exception exception, List<Tuple<String, String>> attributes)
        {
            if (exception.TargetSite == null)
            {
                return new HttpErrorResponse(code, exception.Message, attributes);
            }
            else
            {
                return new HttpErrorResponse(code, exception.Message, exception.TargetSite.Name, attributes);
            }
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
