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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Grpc.AspNetCore.Server;
using ApplicationAccess.Hosting.Grpc;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.EventCache;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Grpc.EventCache
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // TODO: Needs to be moved to gRPC equivalent of ApplicationInitializer
            //   and that also needs to ensure the ErrorHandlingOptions are in config.
            var errorHandlingOptions = new ErrorHandlingOptions();
            builder.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName).Bind(errorHandlingOptions);
            ExceptionToGrpcStatusConverter exceptionToGrpcStatusConverter = null;
            if (errorHandlingOptions.IncludeInnerExceptions.Value == true)
            {
                exceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter();
            }
            else
            {
                exceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter(0);
            }

            // Add services to the container.
            builder.Services.AddGrpc(options =>
            {
                options.Interceptors.Add<ExceptionHandlingInterceptor>(errorHandlingOptions, exceptionToGrpcStatusConverter);
            });

            GrpcServiceOptions options = new();
            //options.

            builder.Services.AddSingleton(typeof(TemporalEventBulkPersisterHolder));
            builder.Services.AddSingleton(typeof(TemporalEventQueryProcessorHolder));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapGrpcService<EventCacheService>();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            
            app.Run();
        }
    }
}