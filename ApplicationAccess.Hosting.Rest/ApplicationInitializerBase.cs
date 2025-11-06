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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Models;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// A base for classes which provide common Initialization routines for hosted ApplicationAccess components.
    /// </summary>
    /// <typeparam name="TParameters">The type of parameters used to initialize the component.</typeparam>
    public abstract class ApplicationInitializerBase<TParameters> where TParameters: ApplicationInitializerParametersBase
    {
        /// <summary>The ASP.NET core environment variable to use for integration testing of the component.</summary>
        public static readonly String IntegrationTestingEnvironmentName = "IntegrationTesting";

        /// <summary>Actuator for a trip switch.</summary>
        protected TripSwitchActuator tripSwitchActuator;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ApplicationInitializerBase class.
        /// </summary>
        public ApplicationInitializerBase()
        {
            tripSwitchActuator = null;
        }

        #region Private/Protected Methods

        protected void ThrowExceptionIfParametersPropertyIsNull(Object propertyValue, String propertyName, String parametersName)
        {
            if (propertyValue == null)
                throw new ArgumentNullException(propertyName, $"Property '{propertyName}' of {parametersName} object cannot be null.");
        }

        /// <summary>
        /// Register all 'holder' classes for the interfaces that the hosted component implements within dependency injection.
        /// </summary>
        /// <param name="builder">The web application builder to use to register.</param>
        /// <param name="processorHolderTypes">A collection of the types of processor 'holder'.</param>
        /// <remarks>See notes in remarks of class <see cref="UserQueryProcessorHolder"/> for an explanation of 'holder' classes.</remarks>
        protected void RegisterProcessorHolders(WebApplicationBuilder builder, IEnumerable<Type> processorHolderTypes)
        {
            foreach (Type currentProcessorHolderType in processorHolderTypes)
            {
                builder.Services.AddSingleton(currentProcessorHolderType);
            }
        }

        /// <summary>
        /// Register and validate options for metric logging.
        /// </summary>
        /// <param name="builder">The web application builder to use to register.</param>
        protected void ValidateAndRegisterMetricLoggingOptions(WebApplicationBuilder builder)
        {
            builder.Services.AddOptions<MetricLoggingOptions>()
                .Bind(builder.Configuration.GetSection(MetricLoggingOptions.MetricLoggingOptionsName))
                .ValidateDataAnnotations().ValidateOnStart();
            var metricLoggingOptions = new MetricLoggingOptions();
            builder.Configuration.GetSection(MetricLoggingOptions.MetricLoggingOptionsName).Bind(metricLoggingOptions);
            var validator = new MetricLoggingOptionsValidator();
            validator.Validate(metricLoggingOptions);
        }

        /// <summary>
        /// Creates a <see cref="TripSwitchActuator"/> and registers it within dependency injection.
        /// </summary>
        /// <param name="builder">The web application builder to use to register.</param>
        /// <param name="parameters">The application initialization parameters.</param>
        protected void CreateAndRegisterTripSwitchActuator(WebApplicationBuilder builder, TParameters parameters)
        {
            if (parameters.TripSwitchTrippedException != null)
            {
                tripSwitchActuator = new TripSwitchActuator();
                builder.Services.AddSingleton<TripSwitchActuator>(tripSwitchActuator);
            }
        }

        /// <summary>
        /// Registers the underlying ApplicationAccess wrapper component as a hosted service.
        /// </summary>
        /// <typeparam name="THostedService">The type of the ApplicationAccess wrapper component.</typeparam>
        /// <param name="builder">The web application builder to use to register.</param>
        protected void RegisterHostedSerice<THostedService>(WebApplicationBuilder builder)
            where THostedService : class, IHostedService
        {
            if (builder.Environment.EnvironmentName != IntegrationTestingEnvironmentName)
            {
                builder.Services.AddHostedService<THostedService>();
            }
        }

        /// <summary>
        /// Sets up logging to a file if it's been configured.
        /// </summary>
        /// <param name="builder">The builder used to set up the web application.</param>
        /// <param name="middlewareUtilities">The <see cref="MiddlewareUtilities"/> object to use to setup file logging.</param>
        protected void SetupFileLogging(WebApplicationBuilder builder, MiddlewareUtilities middlewareUtilities)
        {
            String logFilePath = builder.Configuration.GetValue<String>($"{FileLoggingOptions.FileLoggingOptionsName}:{nameof(FileLoggingOptions.LogFilePath)}");
            String logFileNamePrefix = builder.Configuration.GetValue<String>($"{FileLoggingOptions.FileLoggingOptionsName}:{nameof(FileLoggingOptions.LogFileNamePrefix)}");
            if (logFilePath != null && logFileNamePrefix != null)
            {
                middlewareUtilities.SetupFileLogging(builder, logFilePath, logFileNamePrefix);
            }
        }

        #endregion
    }
}
