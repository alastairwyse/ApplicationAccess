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
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Models;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Controllers;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// Entry point to the application.
    /// </summary>
    public class Program
    {
        /// <summary>The name of the section in the 'appsettings.json' configuration which holds configuration templates for various ApplicationAccess nodes.</summary>
        protected const String appSettingsConfigurationTemplatesPropertyName = "AppSettingsConfigurationTemplates";

        /// <summary>
        /// Entry point to the application.
        /// </summary>
        /// <param name="args">Command line arguments passed to the application.</param>
        public static void Main(string[] args)
        {
            const String readerNodeConfigurationTemplatePropertyName = "ReaderNode";
            const String eventCacheNodeConfigurationTemplatePropertyName = "EventCacheNode";
            const String writerNodeConfigurationTemplatePropertyName = "WriterNode";
            const String distributedOperationCoordinatorNodeConfigurationTemplatePropertyName = "DistributedOperationCoordinatorNode";
            const String distributedOperationRouterNodeConfigurationTemplatePropertyName = "DistributedOperationRouterNode";
            ReaderNodeAppSettingsConfigurationTemplate readerNodeAppSettingsConfigurationTemplate = new();
            EventCacheNodeAppSettingsConfigurationTemplate eventCacheNodeAppSettingsConfigurationTemplate = new();
            WriterNodeAppSettingsConfigurationTemplate writerNodeAppSettingsConfigurationTemplate = new();
            DistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate = new();
            DistributedOperationRouterNodeAppSettingsConfigurationTemplate distributedOperationRouterNodeAppSettingsConfigurationTemplate = new();

            var parameters = new ApplicationInitializerParameters()
            {
                Args = args,
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccessKubernetesDistributedInstanceManager",
                SwaggerApplicationDescription = "Manages a distributed AccessManager implementation hosted in Kubernetes.",
                SwaggerGenerationAdditionalAssemblies = new List<Assembly>()
                {
                    typeof(KubernetesDistributedInstanceManagerController).Assembly
                },
                ConfigureOptionsAction = (WebApplicationBuilder builder) =>
                {
                    // Validate IOptions configuration
                    builder.Services.AddOptions<DistributedAccessManagerInstanceOptions>()
                        .Bind(builder.Configuration.GetSection(DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();
                    var distributedAccessManagerInstanceOptions = new DistributedAccessManagerInstanceOptions();
                    builder.Configuration.GetSection(DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName).Bind(distributedAccessManagerInstanceOptions);
                    var validator = new DistributedAccessManagerInstanceOptionsValidator();
                    validator.Validate(distributedAccessManagerInstanceOptions);
                    builder.Services.AddOptions<ErrorHandlingOptions>()
                        .Bind(builder.Configuration.GetSection(ErrorHandlingOptions.ErrorHandlingOptionsName))
                        .ValidateDataAnnotations().ValidateOnStart();

                    // Read and validate node 'appsettings.json' configuration templates
                    void ReadAndValidateAppSettingsConfigurationTemplate(String nodeConfigurationTemplatePropertyName, JObject configurationTemplate)
                    {
                        ReadAppSettingsConfigurationTemplates(nodeConfigurationTemplatePropertyName, configurationTemplate, builder);
                        if (configurationTemplate.Count == 0)
                            throw new ValidationException($"Error validating {appSettingsConfigurationTemplatesPropertyName} options.  Configuration for '{nodeConfigurationTemplatePropertyName}' is required.");
                    }
                    ReadAndValidateAppSettingsConfigurationTemplate(readerNodeConfigurationTemplatePropertyName, readerNodeAppSettingsConfigurationTemplate);
                    ReadAndValidateAppSettingsConfigurationTemplate(eventCacheNodeConfigurationTemplatePropertyName, eventCacheNodeAppSettingsConfigurationTemplate);
                    ReadAndValidateAppSettingsConfigurationTemplate(writerNodeConfigurationTemplatePropertyName, writerNodeAppSettingsConfigurationTemplate);
                    ReadAndValidateAppSettingsConfigurationTemplate(distributedOperationCoordinatorNodeConfigurationTemplatePropertyName, distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate);
                    ReadAndValidateAppSettingsConfigurationTemplate(distributedOperationRouterNodeConfigurationTemplatePropertyName, distributedOperationRouterNodeAppSettingsConfigurationTemplate);

                    // Register node 'appsettings.json' configuration templates in dependency injection
                    builder.Services.AddSingleton(readerNodeAppSettingsConfigurationTemplate);
                    builder.Services.AddSingleton(eventCacheNodeAppSettingsConfigurationTemplate);
                    builder.Services.AddSingleton(writerNodeAppSettingsConfigurationTemplate);
                    builder.Services.AddSingleton(distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate);
                    builder.Services.AddSingleton(distributedOperationRouterNodeAppSettingsConfigurationTemplate);
                },
                ProcessorHolderTypes = new List<Type>()
                {
                    typeof(KubernetesDistributedInstanceManagerHolder)
                },
                ExceptionToHttpStatusCodeMappings = new List<Tuple<Type, HttpStatusCode>>()
                {
                    // Add a mapping from ServiceUnavailableException to HTTP 503 error status
                    new Tuple<Type, HttpStatusCode>(typeof(ServiceUnavailableException), HttpStatusCode.ServiceUnavailable),
                    // InvalidOperationException is thrown in KubernetesDistributedAccessManagerInstanceManager when manager state doesn't match method call parameters
                    new Tuple<Type, HttpStatusCode>(typeof(InvalidOperationException), HttpStatusCode.BadRequest)
                },
                ExceptionTypesMappedToStandardHttpErrorResponse = new List<Type>()
                {
                    typeof(ServiceUnavailableException),
                    typeof(InvalidOperationException)
                },
                // Setup TripSwitchMiddleware
                TripSwitchTrippedException = new ServiceUnavailableException("The service is unavailable due to an interal error."),
            };

            var initializer = new ApplicationInitializer();
            WebApplication app = initializer.Initialize<KubernetesDistributedInstanceManagerNodeHostedServiceWrapper>(parameters);

            app.Run();
        }

        /// <summary>
        /// Reads 'appsettings.json' configuration template JSON for a node, and writes it to the specified <see cref="JObject"/>.
        /// </summary>
        /// <param name="templatePropertyName">The name of the section within this service's 'appsettings.json' configuration to read the configuration template from.</param>
        /// <param name="template">The <see cref="JObject"/> to write the JSON to.</param>
        /// <param name="builder"><see cref="WebApplicationBuilder"/> object used to access the service's 'appsettings.json' configuration.</param>
        /// <remarks>Booleans and numbers in the source template are written to the <see cref="JObject"/> parameter as strings rather than their correct type.  This is because the <see cref="IConfigurationSection"/> interface used to read the 'appsettings.json' only supports reading of strings (since it's a generic interface which also supports less strongly typed configuration like 'ini' files).  This is not preferrable, but fortunately booleans and numbers are converted back to their proper types when read by the node (reader node, writer node, etc...) their surrounding JSON template is passed to (since the configuration Bind() process converts back to their declared types).</remarks>
        private static void ReadAppSettingsConfigurationTemplates(String templatePropertyName, JObject template, WebApplicationBuilder builder)
        {
            IConfigurationSection configurationTemplateSection = builder.Configuration.GetSection(appSettingsConfigurationTemplatesPropertyName).GetSection(templatePropertyName);

            void WriteJsonTemplateRecurse(IConfigurationSection configurationSection, JObject template)
            {
                foreach (IConfigurationSection currentChild in configurationSection.GetChildren())
                {
                    if (currentChild.Value == null)
                    {
                        JObject value = new();
                        WriteJsonTemplateRecurse(currentChild, value);
                        template.Add(currentChild.Key, value);
                    }
                    else
                    {
                        template.Add(currentChild.Key, currentChild.Value);
                    }
                }
            }
            WriteJsonTemplateRecurse(configurationTemplateSection, template);
        }
    }
}
