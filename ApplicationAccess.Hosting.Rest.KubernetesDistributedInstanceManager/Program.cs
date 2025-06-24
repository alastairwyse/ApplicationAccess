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
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddOptions<DistributedAccessManagerInstanceOptions>()
                .Bind(builder.Configuration.GetSection(DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();

            IConfigurationSection mySection = builder.Configuration.GetSection(SomeConfigurationOptions.SomeConfigurationOptionsName);
            //var blah = new SomeConfigurationOptions();
            //mySection.Bind(builder.Configuration.GetSection(SomeConfigurationOptions.SomeConfigurationOptionsName));
            //var mySection2 = mySection.GetSection("SomeInnerConfiguration").Get(typeof(JObject));
            var converter = new ConfigurationToJsonConverter();
            var readerNodeAppSettingsConfigurationTemplate = new ReaderNodeAppSettingsConfigurationTemplate();
            converter.Convert(mySection.GetSection("JsonProp"), readerNodeAppSettingsConfigurationTemplate);
            builder.Services.AddSingleton<ReaderNodeAppSettingsConfigurationTemplate>(readerNodeAppSettingsConfigurationTemplate);

            // TODO: Need to do above in here OR action passed to initializer to convert all subparts of 'AppSettingsConfigurationTemplates' to equiv objects deriving from JObject

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
