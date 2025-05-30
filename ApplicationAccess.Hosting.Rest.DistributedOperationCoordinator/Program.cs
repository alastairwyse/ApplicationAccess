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
using System.Reflection;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Models;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationCoordinator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parameters = new ApplicationInitializerParameters()
            {
                Args = args,
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess",
                SwaggerApplicationDescription = "Provides flexible and configurable user permission and authorization management for applications",
                SwaggerGenerationAdditionalAssemblies = new List<Assembly>()
                {
                    typeof(Rest.Controllers.DistributedOperationProcessorControllerBase).Assembly
                },
                ConfigureOptionsAction = (WebApplicationBuilder builder) =>
                {
                    builder.Services.AddOptions<AccessManagerSqlDatabaseConnectionOptions>()
                        .Bind(builder.Configuration.GetSection(AccessManagerSqlDatabaseConnectionOptions.AccessManagerSqlDatabaseConnectionOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    builder.Services.AddOptions<ShardConfigurationRefreshOptions>()
                        .Bind(builder.Configuration.GetSection(ShardConfigurationRefreshOptions.ShardConfigurationRefreshOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    builder.Services.AddOptions<ShardConnectionOptions>()
                        .Bind(builder.Configuration.GetSection(ShardConnectionOptions.ShardConnectionOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    builder.Services.AddOptions<ErrorHandlingOptions>()
                        .Bind(builder.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                },
                ProcessorHolderTypes = new List<Type>()
                {
                    typeof(AsyncQueryProcessorHolder),
                    typeof(AsyncEventProcessorHolder)
                },
                // Add a mapping from ServiceUnavailableException to HTTP 503 error status
                ExceptionToHttpStatusCodeMappings = new List<Tuple<Type, HttpStatusCode>>()
                {
                    new Tuple<Type, HttpStatusCode>(typeof(ServiceUnavailableException), HttpStatusCode.ServiceUnavailable)
                },
                ExceptionTypesMappedToStandardHttpErrorResponse = new List<Type>()
                {
                    typeof(ServiceUnavailableException)
                },
                // Setup TripSwitchMiddleware
                TripSwitchTrippedException = new ServiceUnavailableException("The service is unavailable due to an interal error."),
            };

            var initializer = new ApplicationInitializer();
            WebApplication app = initializer.Initialize<DistributedOperationCoordinatorNodeHostedServiceWrapper>(parameters);

            app.Run();
        }
    }
}