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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Google.Rpc;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.EventCache;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Persistence;
using ApplicationAccess.Serialization;

namespace ApplicationAccess.Hosting.Grpc.EventCache
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parameters = new ApplicationInitializerParameters()
            {
                Args = args,
                ConfigureOptionsAction = (WebApplicationBuilder builder) =>
                {
                    builder.Services.AddOptions<EventCachingOptions>()
                        .Bind(builder.Configuration.GetSection(EventCachingOptions.EventCachingOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                },
                ProcessorHolderTypes = new List<Type>()
                {
                    typeof(TemporalEventBulkPersisterHolder),
                    typeof(TemporalEventQueryProcessorHolder)
                },
                ExceptionToGrpcStatusCodeMappings = new List<Tuple<Type, Code>>()
                {
                    // Add a mapping from DeserializationException to HTTP 400 equivalent error status
                    new Tuple<Type, Code>(typeof(DeserializationException), Code.InvalidArgument),
                    // Add a mapping from EventCacheEmptyException to HTTP 503 equivalent error status
                    new Tuple<Type, Code>(typeof(EventCacheEmptyException), Code.Unavailable),
                    // Add a mapping from ServiceUnavailableException to HTTP 503 equivalent error status (for TripSwitch)
                    new Tuple<Type, Code>(typeof(EventCacheEmptyException), Code.Unavailable),
                },
                // Setup TripSwitchMiddleware 
                TripSwitchTrippedException = new ServiceUnavailableException("The service is unavailable due to an internal error."),
            };

            var initializer = new ApplicationInitializer();
            WebApplication app = initializer.Initialize<EventCacheService, TemporalEventBulkCachingNodeHostedServiceWrapper>(parameters);

            app.Run();
        }
    }
}