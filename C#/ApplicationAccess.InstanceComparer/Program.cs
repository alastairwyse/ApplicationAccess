/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.IO;
using Microsoft.Extensions.Configuration;
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationAccess.InstanceComparer.Configuration;
using ApplicationLogging;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.InstanceComparer
{
    class Program
    {
        protected static String sourceAccessManagerRestClientConfigurationFileProperty = "SourceAccessManagerRestClientConfiguration";
        protected static String targetAccessManagerRestClientConfigurationFileProperty = "TargetAccessManagerRestClientConfiguration";
        protected static String applicationComponentsConfigurationFileProperty = "ApplicationComponents";
        protected static String accessLevelsConfigurationFileProperty = "AccessLevels";
        protected static String excludeUsersConfigurationFileProperty = "ExcludeUsers";
        protected static String excludeGroupsConfigurationFileProperty = "ExcludeGroups";
        protected static String excludeEntitiesConfigurationFileProperty = "ExcludeEntities";
        protected static String throwExceptionOnDifferenceConfigurationFileProperty = "ThrowExceptionOnDifference";
        protected static String loggingConfigurationFileProperty = "LoggingConfiguration";

        static void Main(string[] args)
        {
            // Get configuration file path from command line parameters
            if (args.Length != 1)
                throw new ArgumentException($"Expected 1 command line parameter, but recevied {args.Length}.");
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(args[0], false, false);
            IConfigurationRoot configurationRoot = null;
            try
            {
                configurationRoot = configurationBuilder.Build();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to load configuration from file '{args[0]}'.", e);
            }

            // Validate and read the configuration
            foreach (String currentConfigurationSection in new String[]
            {
                sourceAccessManagerRestClientConfigurationFileProperty, 
                targetAccessManagerRestClientConfigurationFileProperty, 
                applicationComponentsConfigurationFileProperty, 
                accessLevelsConfigurationFileProperty, 
                excludeUsersConfigurationFileProperty, 
                excludeGroupsConfigurationFileProperty, 
                excludeEntitiesConfigurationFileProperty,
                throwExceptionOnDifferenceConfigurationFileProperty, 
                loggingConfigurationFileProperty
            })
            {
                if (configurationRoot.GetSection(currentConfigurationSection).Exists() == false)
                    throw new Exception($"Could not find section '{currentConfigurationSection}' in configuration file.");
            }
            AccessManagerRestClientConfiguration sourceRestClientConfiguration = new AccessManagerRestClientConfigurationReader().Read(configurationRoot.GetSection(sourceAccessManagerRestClientConfigurationFileProperty));
            AccessManagerRestClientConfiguration targetRestClientConfiguration = new AccessManagerRestClientConfigurationReader().Read(configurationRoot.GetSection(targetAccessManagerRestClientConfigurationFileProperty));
            IList<String> applicationComponents = ReadStringListFromConfiguration(configurationRoot, applicationComponentsConfigurationFileProperty);
            IList<String> accessLevels = ReadStringListFromConfiguration(configurationRoot, accessLevelsConfigurationFileProperty);
            IList<String> excludeUsers = ReadStringListFromConfiguration(configurationRoot, excludeUsersConfigurationFileProperty);
            IList<String> excludeGroups = ReadStringListFromConfiguration(configurationRoot, excludeGroupsConfigurationFileProperty);
            IList<ExcludeEntitiesConfiguration> excludeEntities = new ExcludeEntitiesConfigurationReader().Read(configurationRoot.GetSection(excludeEntitiesConfigurationFileProperty).Get<List<IConfigurationSection>>());
            Boolean throwExceptionOnDifference = GetConfigurationValueAsBoolean("Throw exception on difference", throwExceptionOnDifferenceConfigurationFileProperty, configurationRoot);
            LoggingConfiguration loggingConfiguration = new LoggingConfigurationReader().Read(configurationRoot.GetSection(loggingConfigurationFileProperty));

            // Create the logger and clients
            using (var fileLogger = new FileApplicationLogger(LogLevel.Debug, '|', "  ", loggingConfiguration.LogFilePath))
            using (var sourceClient = CreateClient(sourceRestClientConfiguration, fileLogger))
            using (var targetClient = CreateClient(targetRestClientConfiguration, fileLogger))
            {

                // Create the instance comparer
                var comparer = new AccessManagerInstanceComparer
                (
                    sourceClient,
                    targetClient,
                    applicationComponents,
                    accessLevels,
                    new HashSet<String>(excludeUsers),
                    new HashSet<String>(excludeGroups),
                    excludeEntities,
                    throwExceptionOnDifference, 
                    loggingConfiguration.LogMethodParameters,
                    fileLogger
                );

                // Run the comparison
                comparer.Compare();
            }
        }

        /// <summary>
        /// Attempts to read a <see cref="List{T}"/> of <see cref="String"/> objects from configuration.
        /// </summary>
        /// <param name="configurationRoot">The application configuration represented as an <see cref="IConfigurationRoot"/>.</param>
        /// <param name="configurationKey">The key or property name of the list to read.</param>
        /// <returns>The configuration item as a list of strings.</returns>
        /// <exception cref="Exception">A key with the specified name does not exist, or does not contain a string array.</exception>
        static protected List<String> ReadStringListFromConfiguration(IConfigurationRoot configurationRoot, String configurationKey)
        {
            List<String> returnList = configurationRoot.GetSection(configurationKey).Get<List<String>>();
            if (returnList == null)
            {
                throw new Exception($"Configuration section with key '{configurationKey}' was not found or did not contain a List of Strings.");
            }

            return returnList;
        }

        protected static Boolean GetConfigurationValueAsBoolean(String configurationTypeName, String propertyName, IConfigurationRoot configurationRoot)
        {
            String valueAsString = configurationRoot[propertyName];
            Boolean returnValue = false;
            Boolean parseResult = Boolean.TryParse(valueAsString, out returnValue);
            if (parseResult == false)
                throw new Exception($"{configurationTypeName} configuration property '{propertyName}' with value '{valueAsString}' could not be converted to a boolean.");

            return returnValue;
        }

        /// <summary>
        /// Creates an AccessManagerClient<String, String, String, String> from configuration.
        /// </summary>
        /// <param name="configuration">The client configuration.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>The client.</returns>
        static protected AccessManagerClient<String, String, String, String> CreateClient(AccessManagerRestClientConfiguration configuration, IApplicationLogger logger)
        {
            var returnClient = new AccessManagerClient<String, String, String, String>
            (
                new Uri(configuration.AccessManagerUrl),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                configuration.RetryCount,
                configuration.RetryInterval,
                logger,
                new NullMetricLogger()
            );

            return returnClient;
        }
    }
}