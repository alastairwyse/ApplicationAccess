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
using ApplicationAccess.TestHarness.Configuration;
using Microsoft.Extensions.Configuration;

namespace ApplicationAccess.InstanceComparer.Configuration
{
    class LoggingConfigurationReader : ConfigurationReaderBase
    {
        protected const String logFilePathPropertyName = "LogFilePath";
        protected const String logMethodParametersPropertyName = "LogMethodParameters";

        public LoggingConfigurationReader()
            : base("Logging")
        {
        }

        public LoggingConfiguration Read(IConfigurationSection configurationSection)
        {
            ThrowExceptionIfPropertyNotFound(logFilePathPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(logMethodParametersPropertyName, configurationSection);

            var returnConfiguration = new LoggingConfiguration();
            returnConfiguration.LogFilePath = configurationSection[logFilePathPropertyName];
            returnConfiguration.LogMethodParameters = GetConfigurationValueAsBoolean(logMethodParametersPropertyName, configurationSection);

            return returnConfiguration;
        }
    }
}
