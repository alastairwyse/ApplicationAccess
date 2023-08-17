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
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Models;
using ApplicationAccess.Persistence;
using ApplicationAccess.Serialization;

namespace ApplicationAccess.Hosting.Rest.EventCache
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parameters = new ApplicationInitializerParameters()
            {
                Args = args,
                ConfigureJsonOptionsAction = (JsonOptions options) =>
                {
                    // Add custom serializer for the TemporalEventBufferItemBase class and subclasses
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new TemporalEventBufferItemBaseConverter<String, String, String, String>
                    (
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier()
                    ));
                }, 
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess Event Cache Node",
                SwaggerApplicationDescription = "Node in a distributed/scaled deployment of ApplicationAccess which caches events",
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
                ExceptionToHttpStatusCodeMappings = new List<Tuple<Type, HttpStatusCode>>()
                {
                    // Add a mapping from DeserializationException to HTTP 400 error status
                    new Tuple<Type, HttpStatusCode>(typeof(DeserializationException), HttpStatusCode.BadRequest), 
                    // Add a mapping from EventCacheEmptyException to HTTP 503 error status
                    new Tuple<Type, HttpStatusCode>(typeof(EventCacheEmptyException), HttpStatusCode.ServiceUnavailable)
                },
                ExceptionTypesMappedToStandardHttpErrorResponse = new List<Type>()
                {
                    typeof(DeserializationException),
                    typeof(EventCacheEmptyException)
                }
            };

            var initializer = new ApplicationInitializer();
            WebApplication app = initializer.Initialize<TemporalEventBulkCachingNodeHostedServiceWrapper>(parameters);

            app.Run();
        }
    }
}