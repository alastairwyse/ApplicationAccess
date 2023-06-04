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
using System.IO;
using System.Net.Mime;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.SwaggerGen;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Utilities;
using Serilog;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Utility methods for configuring ASP.NET Core middleware.
    /// </summary>
    public class MiddlewareUtilities
    {
        /// <summary>
        /// Validates <see cref="MetricLoggingOptions"/> obtained from the specified <see cref="WebApplicationBuilder"/> instance.
        /// </summary>
        public void ValidateMetricLoggingOptions(WebApplicationBuilder builder)
        {
            var metricLoggingOptions = new MetricLoggingOptions();
            ValidateConfigurationSection(builder, metricLoggingOptions, MetricLoggingOptions.MetricLoggingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricBufferProcessing, MetricBufferProcessingOptions.MetricBufferProcessingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricsSqlServerConnection, MetricsSqlServerConnectionOptions.MetricsSqlServerConnectionOptionsName);
        }

        /// <summary>
        /// Adds swagger documentation generation for an assembly to the specified <see cref="SwaggerGenOptions"/> instance.
        /// </summary>
        /// <param name="swaggerGenOptions">The swagger generation options to add the documentation to.</param>
        /// <param name="assembly">the assembly to add the documentation for.</param>
        /// <remarks>Used to add swagger documentation for an assembly where controllers in that assembly are inherited from or included in the current ASP.NET Core project.  Note that XML documentation generation must be enabled in the project creating the assembly.</remarks>
        public void AddSwaggerGenerationForAssembly(SwaggerGenOptions swaggerGenOptions, Assembly assembly)
        {
            var xmlFilename = $"{assembly.GetName().Name}.xml";
            swaggerGenOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        }

        /// <summary>
        /// Sets up a custom exception handler in the specified application builder, which catches any thrown exceptions and converts them to and returns serialized <see cref="HttpErrorResponse"/> objects.
        /// </summary>
        /// <param name="appBuilder">A class which allows configuration of the application's request pipeline.</param>
        /// <param name="errorHandlingOptions">A set of application error handling options.</param>
        /// <param name="exceptionToHttpStatusCodeConverter">Used to convert types of exceptions to HTTP status codes.</param>
        /// <param name="exceptionToHttpErrorResponseConverter">Used to convert types of exceptions to <see cref="HttpErrorResponse"/> instances.</param>
        public void SetupExceptionHandler
        (
            IApplicationBuilder appBuilder,
            ErrorHandlingOptions errorHandlingOptions,
            ExceptionToHttpStatusCodeConverter exceptionToHttpStatusCodeConverter,
            ExceptionToHttpErrorResponseConverter exceptionToHttpErrorResponseConverter
        )
        {
            // As per https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-5.0#exception-handler-lambda
            appBuilder.UseExceptionHandler((IApplicationBuilder appBuilder) =>
            {
                appBuilder.Run(async (HttpContext context) =>
                {
                    // Get the exception
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    Exception exception = exceptionHandlerPathFeature.Error;

                    if (exception != null)
                    {
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        context.Response.StatusCode = (Int32)exceptionToHttpStatusCodeConverter.Convert(exception);
                        HttpErrorResponse httpErrorResponse = null;
                        if (context.Response.StatusCode == StatusCodes.Status500InternalServerError && errorHandlingOptions.OverrideInternalServerErrors.Value == true)
                        {
                            httpErrorResponse = new HttpErrorResponse("InternalServerError", errorHandlingOptions.InternalServerErrorMessageOverride);
                        }
                        else
                        {
                            httpErrorResponse = exceptionToHttpErrorResponseConverter.Convert(exception);
                        }
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

        // TODO: REMOVE TEMPORARY DEBUGGING CODE
        public void SetupFileLogging(WebApplicationBuilder builder, String logFileNamePrefix)
        {
            var fileLoggingConfiguration = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.RollingFile(@$"C:\Temp\{logFileNamePrefix}.txt");
            ILogger fileLogger = fileLoggingConfiguration.CreateLogger();
            builder.Logging.AddSerilog(fileLogger);
        }

        #region Private/Protected Methods

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

        #endregion
    }
}
