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
    class MetricsBufferConfigurationReader : ConfigurationReaderBase
    {
        protected const String bufferImplementationPropertyName = "BufferImplementation";
        protected const String bufferSizeLimitPropertyName = "BufferSizeLimit";
        protected const String dequeueOperationLoopIntervalPropertyName = "DequeueOperationLoopInterval";

        public MetricsBufferConfigurationReader()
            : base("Metrics buffer")
        {
        }

        public MetricsBufferConfiguration Read(IConfigurationSection configurationSection)
        {
            ThrowExceptionIfPropertyNotFound(bufferImplementationPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(bufferSizeLimitPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(dequeueOperationLoopIntervalPropertyName, configurationSection);

            var returnConfiguration = new MetricsBufferConfiguration();

            MetricBufferProcessingStrategyImplementation bufferImplementation;
            Boolean parseResult = Enum.TryParse<MetricBufferProcessingStrategyImplementation>(configurationSection[bufferImplementationPropertyName], false, out bufferImplementation);
            if (parseResult == false)
                throw new Exception($"{configurationTypeName} configuration property '{bufferImplementationPropertyName}' with value '{configurationSection[bufferImplementationPropertyName]}' could not be converted to a {typeof(MetricBufferProcessingStrategyImplementation).Name}.");
            returnConfiguration.BufferImplementation = bufferImplementation;
            returnConfiguration.BufferSizeLimit = GetConfigurationValueAsInteger(bufferSizeLimitPropertyName, configurationSection);
            returnConfiguration.DequeueOperationLoopInterval = GetConfigurationValueAsInteger(dequeueOperationLoopIntervalPropertyName, configurationSection);

            return returnConfiguration;
        }
    }
}
