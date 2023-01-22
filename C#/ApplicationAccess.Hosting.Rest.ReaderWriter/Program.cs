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

using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Utilities;
using ApplicationAccess.Hosting.Rest.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions((ApiBehaviorOptions behaviorOptions) =>
            {
                // This overrides the default model-binding failure behaviour, to return a HttpErrorResponse object rather than the standard ProblemDetails
                behaviorOptions.InvalidModelStateResponseFactory = (ActionContext context) =>
                {
                    var errorResponse = ConvertModelStateDictionaryToHttpErrorResponse(context.ModelState);
                    var serializer = new HttpErrorResponseJsonSerializer();
                    JObject serializedErrorResponse = serializer.Serialize(errorResponse);

                    var contentResult = new ContentResult();
                    contentResult.Content = serializedErrorResponse.ToString();
                    contentResult.ContentType = MediaTypeNames.Application.Json;
                    contentResult.StatusCode = StatusCodes.Status400BadRequest;

                    return contentResult;
                };
            })
            // This configures the API to return a HTTP 406 (not acceptable) status if the 'Accept' request header does not contain '*/*' or 'application/json'
            .AddMvcOptions(options =>
            {
                options.RespectBrowserAcceptHeader = true;
                options.ReturnHttpNotAcceptable = true;
            });

            builder.Services.AddApiVersioning((ApiVersioningOptions versioningOptions) =>
            {
                versioningOptions.AssumeDefaultVersionWhenUnspecified = false;
                versioningOptions.ReportApiVersions = true;
                versioningOptions.ErrorResponses = new ApiVersioningErrorResponseProvider();
            });
            builder.Services.AddVersionedApiExplorer(
            options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen((SwaggerGenOptions swaggerGenOptions) =>
            {
                swaggerGenOptions.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ApplicationAccess",
                    Description = "Provides flexible and configurable user permission and authorization management for applications"
                });

                // This adds swagger generation for controllers outside this project/assembly
                //   TODO: This might not be necessary
                AddSwaggerGenerationForTypesAssembly(swaggerGenOptions, typeof(UserQueryProcessorController));
            });

            // Validate and register top level configuration items
            builder.Services.AddOptions<AccessManagerSqlServerConnectionOptions>()
                .Bind(builder.Configuration.GetSection(AccessManagerSqlServerConnectionOptions.AccessManagerSqlServerConnectionOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();
            builder.Services.AddOptions<EventBufferFlushingOptions>()
                .Bind(builder.Configuration.GetSection(EventBufferFlushingOptions.EventBufferFlushingOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();
            builder.Services.AddOptions<MetricLoggingOptions>()
                .Bind(builder.Configuration.GetSection(MetricLoggingOptions.MetricLoggingOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();

            // Validate secondary level coonfiguration items
            ValidateSecondaryConfiguration(builder);

            // Register 'holder' classes for the interfaces that comprise IAccessManager
            //   See notes in remarks of class UserQueryProcessorHolder for an explanation
            builder.Services.AddSingleton<UserQueryProcessorHolder>();
            builder.Services.AddSingleton<UserEventProcessorHolder>();

            // Register the hosted service wrapper
            builder.Services.AddHostedService<ReaderWriterNodeHostedServiceWrapper>();

            // Add controllers from other assemblies which together comprise ReaderWriter functionality
            var assembly = typeof(ApplicationAccess.Hosting.Rest.Controllers.UserQueryProcessorController).Assembly;
            builder.Services.AddControllers()
                .AddApplicationPart(assembly).AddControllersAsServices();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            IApiVersionDescriptionProvider apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            app.UseSwaggerUI((SwaggerUIOptions swaggerUIOptions) =>
            {
                // Hide the 'schemas' section at the bottom of the swagger page as it contains strage definitions for mapping objects returned from controller methods (e.g. containing the generic parameter type in the name)
                swaggerUIOptions.DefaultModelsExpandDepth(-1);
                // Setup the 'definitions' drop-down on the swagger UI
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
                {
                    swaggerUIOptions.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"ApplicationAccess API {description.GroupName}");
                }
            }); 

            // Setup custom exception handler in the application's pipeline, so that any exceptions are caught and returned from the API as HttpErrorResponse objects
            var exceptionToHttpStatusCodeConverter = new ExceptionToHttpStatusCodeConverter();
            var exceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter();
            SetupExceptionHandler(app, exceptionToHttpStatusCodeConverter, exceptionToHttpErrorResponseConverter);

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Validates secondary level application configuration.
        /// </summary>
        /// <param name="builder">The builder for the application.</param>
        /// <remarks>The IConfigurationSection ValidateDataAnnotations() extension method does not recursively validate child sections of the section being validated, hence this is performed explicitly in this method for relevant IOptions pattern objects.</remarks>
        protected static void ValidateSecondaryConfiguration(WebApplicationBuilder builder)
        {
            // TODO: Likely some of this validation will be repeated in other REST hosting process, so these checks could be split up and put in their own 'Validation' project
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
        protected static void ValidateConfigurationSection(WebApplicationBuilder builder, Object optionsInstance, String optionsSectionName)
        {
            builder.Configuration.GetSection(optionsSectionName).Bind(optionsInstance);
            var context = new ValidationContext(optionsInstance);
            Validator.ValidateObject(optionsInstance, context, true);
        }

        /// <summary>
        /// Sets up a custom exception handler in the application's pipeline.
        /// </summary>
        /// <param name="appBuilder">A class which allows configuration of the application's request pipeline.</param>
        protected static void SetupExceptionHandler(IApplicationBuilder appBuilder, ExceptionToHttpStatusCodeConverter exceptionToHttpStatusCodeConverter, ExceptionToHttpErrorResponseConverter exceptionToHttpErrorResponseConverter)
        {
            // As per https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-5.0#exception-handler-lambda
            appBuilder.UseExceptionHandler((IApplicationBuilder appBuilder) => 
            {
                appBuilder.Run(async context =>
                {
                    // Get the exception
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    Exception exception = exceptionHandlerPathFeature.Error;

                    if (exception != null)
                    {
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        context.Response.StatusCode = (Int32)exceptionToHttpStatusCodeConverter.Convert(exception);
                        HttpErrorResponse httpErrorResponse = exceptionToHttpErrorResponseConverter.Convert(exception);
                        var serializer = new HttpErrorResponseJsonSerializer();
                        await context.Response.WriteAsync(serializer.Serialize(httpErrorResponse).ToString());
                    }
                    else
                    {
                        // TODO: Not sure if this situation can arise, but will leave this handler in while testing
                        throw new Exception("'exceptionHandlerPathFeature.Error' was null whilst handling exception.");
                    }
                });
            });
        }

        /// <summary>
        /// Converts a <see cref="ModelStateDictionary"/> in an invalid state to a <see cref="HttpErrorResponse"/>.
        /// </summary>
        /// <param name="modelStateDictionary">The ModelStateDictionary to convert.</param>
        /// <returns>The ModelStateDictionary converted to an HttpErrorResponse.</returns>
        protected static HttpErrorResponse ConvertModelStateDictionaryToHttpErrorResponse(ModelStateDictionary modelStateDictionary)
        {
            if (modelStateDictionary.IsValid == true)
                throw new Exception($"Cannot convert a {nameof(ModelStateDictionary)} in a valid state.");

            var errorAttributes = new List<Tuple<String, String>>();
            foreach (KeyValuePair<String, ModelStateEntry> currentKvp in modelStateDictionary)
            {
                if (currentKvp.Value.ValidationState == ModelValidationState.Invalid)
                {
                    foreach (ModelError currentError in currentKvp.Value.Errors)
                    {
                        errorAttributes.Add(new Tuple<String, String>("Property", currentKvp.Key));
                        errorAttributes.Add(new Tuple<String, String>("Error", currentError.ErrorMessage));
                    }
                }
            }
            var returnErrorResponse = new HttpErrorResponse(HttpStatusCode.BadRequest.ToString(), new ValidationProblemDetails().Title, errorAttributes);

            return returnErrorResponse;
        }

        /// <summary>
        /// Adds swagger documentation generation for the assembly containing the specified type to the specified <see cref="SwaggerGenOptions"/> instance.
        /// </summary>
        /// <param name="swaggerGenOptions">The swagger generation options to add the documentation to.</param>
        /// <param name="type">A type within the assembly to add the documentation for.</param>
        protected static void AddSwaggerGenerationForTypesAssembly(SwaggerGenOptions swaggerGenOptions, Type type)
        {
            var xmlFilename = $"{type.Assembly.GetName().Name}.xml";
            swaggerGenOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        }

        /// <summary>
        /// Implementation of <see cref="IErrorResponseProvider"/> which returns a <see cref="HttpErrorResponse"/> indicating that an API version is not supported.
        /// </summary>
        protected class ApiVersioningErrorResponseProvider : IErrorResponseProvider
        {
            /// <inheritdoc/>
            public IActionResult CreateResponse(ErrorResponseContext context)
            {
                var response = new HttpErrorResponse(context.ErrorCode, context.Message);
                var serializer = new HttpErrorResponseJsonSerializer();
                JObject serializedErrorResponse = serializer.Serialize(response);

                var contentResult = new ContentResult();
                contentResult.Content = serializedErrorResponse.ToString();
                contentResult.ContentType = MediaTypeNames.Application.Json;
                contentResult.StatusCode = StatusCodes.Status400BadRequest;

                return contentResult;
            }
        }
    }
}