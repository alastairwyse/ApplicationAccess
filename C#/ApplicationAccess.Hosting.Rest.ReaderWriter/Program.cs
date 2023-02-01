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

using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using ApplicationAccess.Hosting.Models.Options;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter
{
    public class Program
    {
        public static readonly String IntegrationTestingEnvironmentName = "IntegrationTesting";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var middlewareUtilities = new MiddlewareUtilities();

            // Add services to the container.
            builder.Services.AddControllers()
            // Override the default model-binding failure behaviour, to return a HttpErrorResponse object rather than the standard ProblemDetails
            .MapModelBindingFailureToHttpErrorResponse()
            // Return HTTP 406 (not acceptable) statuses if the 'Accept' request header does not match the controller's 'ProducesResponseType' attribute (i.e. '*/*' or 'application/json')
            .ReturnHttpNotAcceptableOnUnsupportedAcceptHeader();

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
                middlewareUtilities.AddSwaggerGenerationForAssembly(swaggerGenOptions, typeof(Rest.Controllers.EntityQueryProcessorControllerBase).Assembly);
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
            builder.Services.AddOptions<MetricLoggingOptions>()
                .Bind(builder.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();

            // Validate secondary level coonfiguration items
            ValidateSecondaryConfiguration(builder);

            // Register 'holder' classes for the interfaces that comprise IAccessManager
            //   See notes in remarks of class UserQueryProcessorHolder for an explanation
            builder.Services.AddSingleton<EntityEventProcessorHolder>();
            builder.Services.AddSingleton<EntityQueryProcessorHolder>();
            builder.Services.AddSingleton<GroupEventProcessorHolder>();
            builder.Services.AddSingleton<GroupQueryProcessorHolder>();
            builder.Services.AddSingleton<GroupToGroupEventProcessorHolder>();
            builder.Services.AddSingleton<GroupToGroupQueryProcessorHolder>();
            builder.Services.AddSingleton<UserEventProcessorHolder>();
            builder.Services.AddSingleton<UserQueryProcessorHolder>();
            
            // Register the hosted service wrapper
            if (builder.Environment.EnvironmentName != IntegrationTestingEnvironmentName)
            {
                builder.Services.AddHostedService<ReaderWriterNodeHostedServiceWrapper>();
            }
            
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
            var errorHandlingOptions = new ErrorHandlingOptions();
            app.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName).Bind(errorHandlingOptions);
            var exceptionToHttpStatusCodeConverter = new ExceptionToHttpStatusCodeConverter();
            ExceptionToHttpErrorResponseConverter exceptionToHttpErrorResponseConverter = null;
            if (errorHandlingOptions.IncludeInnerExceptions == true)
            {
                exceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter();
            }
            else
            {
                exceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter(0);
            }
            middlewareUtilities.SetupExceptionHandler(app, errorHandlingOptions, exceptionToHttpStatusCodeConverter, exceptionToHttpErrorResponseConverter);

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
            var middlewareUtilities = new MiddlewareUtilities();
            middlewareUtilities.ValidateMetricLoggingOptions(builder);
        }
    }
}