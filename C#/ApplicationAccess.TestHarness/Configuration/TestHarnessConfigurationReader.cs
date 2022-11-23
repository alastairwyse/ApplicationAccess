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
    class TestHarnessConfigurationReader : ConfigurationReaderBase
    {
        protected const String loadExistingDataPropertyName = "LoadExistingData";
        protected const String threadCountPropertyName = "ThreadCount";
        protected const String targetOperationsPerSecondPropertyName = "TargetOperationsPerSecond";
        protected const String previousOperationInitiationTimeWindowSizePropertyName = "PreviousOperationInitiationTimeWindowSize";
        protected const String exceptionsPerSecondThresholdPropertyName = "ExceptionsPerSecondThreshold";
        protected const String previousExceptionOccurenceTimeWindowSizePropertyName = "PreviousExceptionOccurenceTimeWindowSize";
        protected const String ignoreKnownAccessManagerExceptionsPropertyName = "IgnoreKnownAccessManagerExceptions";

        public TestHarnessConfigurationReader()
        : base("Test harness")
        {
        }

        public TestHarnessConfiguration Read(IConfigurationSection configurationSection)
        {
            ThrowExceptionIfPropertyNotFound(loadExistingDataPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(threadCountPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(targetOperationsPerSecondPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(previousOperationInitiationTimeWindowSizePropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(exceptionsPerSecondThresholdPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(previousExceptionOccurenceTimeWindowSizePropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(ignoreKnownAccessManagerExceptionsPropertyName, configurationSection);

            var returnConfiguration = new TestHarnessConfiguration();

            returnConfiguration.LoadExistingData = GetConfigurationValueAsBoolean(loadExistingDataPropertyName, configurationSection);
            returnConfiguration.ThreadCount = GetConfigurationValueAsInteger(threadCountPropertyName, configurationSection);
            returnConfiguration.TargetOperationsPerSecond = GetConfigurationValueAsDouble(targetOperationsPerSecondPropertyName, configurationSection);
            returnConfiguration.PreviousOperationInitiationTimeWindowSize = GetConfigurationValueAsInteger(previousOperationInitiationTimeWindowSizePropertyName, configurationSection);
            returnConfiguration.ExceptionsPerSecondThreshold = GetConfigurationValueAsDouble(exceptionsPerSecondThresholdPropertyName, configurationSection);
            returnConfiguration.PreviousExceptionOccurenceTimeWindowSize = GetConfigurationValueAsInteger(previousExceptionOccurenceTimeWindowSizePropertyName, configurationSection);
            returnConfiguration.IgnoreKnownAccessManagerExceptions = GetConfigurationValueAsBoolean(ignoreKnownAccessManagerExceptionsPropertyName, configurationSection);

            return returnConfiguration;
        }
    }
}
