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
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Serialization;

namespace ApplicationAccess.Hosting.Rest.EventCache
{
    public class Program
    {
        public static readonly String IntegrationTestingEnvironmentName = "IntegrationTesting";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var middlewareUtilities = new MiddlewareUtilities();

            // Add services to the container.
            builder.Services.AddControllers()
            // Override the default model-binding failure behaviour, to return a HttpErrorResponse object rather than the standard ProblemDetails
            .MapModelBindingFailureToHttpErrorResponse()
            // Return HTTP 406 (not acceptable) statuses if the 'Accept' request header does not match the controller's 'ProducesResponseType' attribute (i.e. '*/*' or 'application/json')
            .ReturnHttpNotAcceptableOnUnsupportedAcceptHeader()
            // Add custom serializer for the TemporalEventBufferItemBase class and subclasses
            .AddJsonOptions(options => 
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new TemporalEventBufferItemBaseConverter<String, String, String, String>
                (
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier()
                ));
            });
            // Allow APIs to be versioned
            builder.Services.SetupApiVersioning();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen((SwaggerGenOptions swaggerGenOptions) =>
            {
                swaggerGenOptions.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ApplicationAccess Event Cache",
                    Description = "An in-memory cache for AccessManager events"
                });
            });

            // Validate and register top level configuration items
            builder.Services.AddOptions<MetricLoggingOptions>()
                .Bind(builder.Configuration.GetSection(MetricLoggingOptions.MetricLoggingOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();
            builder.Services.AddOptions<EventCachingOptions>()
                .Bind(builder.Configuration.GetSection(EventCachingOptions.EventCachingOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();

            // Validate secondary level coonfiguration items
            ValidateSecondaryConfiguration(builder);

            // Register 'holder' classes for the interfaces that comprise IAccessManager
            builder.Services.AddSingleton<TemporalEventBulkPersisterHolder>();
            builder.Services.AddSingleton<TemporalEventQueryProcessorHolder>();

            // Register the hosted service wrapper
            if (builder.Environment.EnvironmentName != IntegrationTestingEnvironmentName)
            {
                builder.Services.AddHostedService<TemporalEventBulkCachingNodeHostedServiceWrapper>();
            }

            var app = builder.Build();

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
            // Add a mapping from DeserializationException to HTTP 400 error status
            exceptionToHttpStatusCodeConverter.AddMapping(typeof(DeserializationException), System.Net.HttpStatusCode.BadRequest);
            exceptionToHttpErrorResponseConverter.AddConversionFunction(typeof(DeserializationException), (Exception exception) =>
            {
                if (exception.TargetSite == null)
                {
                    return new HttpErrorResponse(exception.GetType().Name, exception.Message);
                }
                else
                {
                    return new HttpErrorResponse(exception.GetType().Name, exception.Message, exception.TargetSite.Name);
                }
            });
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