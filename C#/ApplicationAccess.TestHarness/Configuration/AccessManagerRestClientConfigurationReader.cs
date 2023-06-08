﻿/*
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
using Microsoft.Extensions.Configuration;

namespace ApplicationAccess.TestHarness.Configuration
{
    class AccessManagerRestClientConfigurationReader : ConfigurationReaderBase
    {
        protected const String accessManagerQueryUrlPropertyName = "AccessManagerQueryUrl";
        protected const String accessManagerEventUrlPropertyName = "AccessManagerEventUrl";
        protected const String retryCountPropertyName = "RetryCount";
        protected const String retryIntervalPropertyName = "RetryInterval";
        protected const String logMetricsPropertyName = "LogMetrics";
        protected const String logIntervalMetricsPropertyName = "LogIntervalMetrics";

        public AccessManagerRestClientConfigurationReader()
            : base("Access manager rest client")
        {
        }

        public AccessManagerRestClientConfiguration Read(IConfigurationSection configurationSection)
        {
            ThrowExceptionIfPropertyNotFound(accessManagerQueryUrlPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(accessManagerEventUrlPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(retryCountPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(retryIntervalPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(logIntervalMetricsPropertyName, configurationSection);

            var returnConfiguration = new AccessManagerRestClientConfiguration();
            returnConfiguration.AccessManagerQueryUrl = configurationSection[accessManagerQueryUrlPropertyName];
            returnConfiguration.AccessManagerEventUrl = configurationSection[accessManagerEventUrlPropertyName];
            returnConfiguration.RetryCount = GetConfigurationValueAsInteger(retryCountPropertyName, configurationSection);
            returnConfiguration.RetryInterval = GetConfigurationValueAsInteger(retryIntervalPropertyName, configurationSection);
            returnConfiguration.LogMetrics = GetConfigurationValueAsBoolean(logMetricsPropertyName, configurationSection);
            returnConfiguration.LogIntervalMetrics = GetConfigurationValueAsBoolean(logIntervalMetricsPropertyName, configurationSection);

            if (returnConfiguration.LogMetrics == false && returnConfiguration.LogIntervalMetrics == true)
            {
                throw new ArgumentException($"If configuration parameter '{logIntervalMetricsPropertyName}' is set {true} paramter '{logMetricsPropertyName}' must also be set {true}.");
            }

            return returnConfiguration;
        }
    }
}
