/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Microsoft.Extensions.Configuration;

namespace ApplicationAccess.TestHarness.Configuration
{
    class DatabaseConnectionConfigurationReader : ConfigurationReaderBase
    {
        protected const String dataSourcePropertyName = "DataSource";
        protected const String initialCataloguePropertyName = "InitialCatalogue";
        protected const String userIdPropertyName = "UserId";
        protected const String passwordPropertyName = "Password";
        protected const String retryCountPropertyName = "RetryCount";
        protected const String retryIntervalPropertyName = "RetryInterval";

        public DatabaseConnectionConfigurationReader()
            : base("Database connection")
        {
        }

        public DatabaseConnectionConfiguration Read(IConfigurationSection configurationSection)
        {
            ThrowExceptionIfPropertyNotFound(dataSourcePropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(initialCataloguePropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(userIdPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(passwordPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(retryCountPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(retryIntervalPropertyName, configurationSection);

            var returnConfiguration = new DatabaseConnectionConfiguration();
            returnConfiguration.DataSource = configurationSection[dataSourcePropertyName];
            returnConfiguration.InitialCatalogue = configurationSection[initialCataloguePropertyName];
            returnConfiguration.UserId = configurationSection[userIdPropertyName];
            returnConfiguration.Password = configurationSection[passwordPropertyName];
            returnConfiguration.RetryCount = GetConfigurationValueAsInteger(retryCountPropertyName, configurationSection);
            returnConfiguration.RetryInterval = GetConfigurationValueAsInteger(retryIntervalPropertyName, configurationSection);

            return returnConfiguration;
        }
    }
}
