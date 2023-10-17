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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Models;

namespace ApplicationAccess.Hosting.Rest.Reader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parameters = new ApplicationInitializerParameters()
            {
                Args = args,
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess Reader Node",
                SwaggerApplicationDescription = "Node in a multi-reader, single-writer deployment of ApplicationAccess which handles query/read operations",
                SwaggerGenerationAdditionalAssemblies = new List<Assembly>()
                {
                    typeof(Rest.Controllers.EntityQueryProcessorControllerBase).Assembly
                },
                ConfigureOptionsAction = (WebApplicationBuilder builder) =>
                {
                    builder.Services.AddOptions<AccessManagerOptions>()
                        .Bind(builder.Configuration.GetSection(AccessManagerOptions.AccessManagerOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    builder.Services.AddOptions<AccessManagerSqlServerConnectionOptions>()
                        .Bind(builder.Configuration.GetSection(AccessManagerSqlServerConnectionOptions.AccessManagerSqlServerConnectionOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    builder.Services.AddOptions<EventCacheConnectionOptions>()
                        .Bind(builder.Configuration.GetSection(EventCacheConnectionOptions.EventCacheConnectionOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    builder.Services.AddOptions<EventCacheRefreshOptions>()
                        .Bind(builder.Configuration.GetSection(EventCacheRefreshOptions.EventCacheRefreshOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    builder.Services.AddOptions<ErrorHandlingOptions>()
                        .Bind(builder.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                },
                ProcessorHolderTypes = new List<Type>()
                {
                    typeof(EntityQueryProcessorHolder),
                    typeof(GroupQueryProcessorHolder),
                    typeof(GroupToGroupQueryProcessorHolder),
                    typeof(UserQueryProcessorHolder)
                }
                
                , 
                // Optionally setup file logging
                LogFilePath = @"C:\Temp\AppAccess\TestHarness",
                LogFileNamePrefix = "ApplicationAccessReaderNodeLog"
            };

            var initializer = new ApplicationInitializer();
            WebApplication app = initializer.Initialize<ReaderNodeHostedServiceWrapper>(parameters);

            app.Run();
        }
    }
}