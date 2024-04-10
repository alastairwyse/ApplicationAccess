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
    class PersisterBufferFlushStrategyConfigurationReader : ConfigurationReaderBase
    {
        protected const String bufferImplementationPropertyName = "BufferImplementation";
        protected const String bufferSizeLimitPropertyName = "BufferSizeLimit";
        protected const String flushLoopIntervalPropertyName = "FlushLoopInterval";

        public PersisterBufferFlushStrategyConfigurationReader()
            : base("Persister buffer flush strategy")
        {
        }

        public PersisterBufferFlushStrategyConfiguration Read(IConfigurationSection configurationSection)
        {
            ThrowExceptionIfPropertyNotFound(bufferImplementationPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(bufferSizeLimitPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(flushLoopIntervalPropertyName, configurationSection);

            var returnConfiguration = new PersisterBufferFlushStrategyConfiguration();

            AccessManagerEventBufferFlushStrategyImplementation bufferImplementation;
            Boolean parseResult = Enum.TryParse<AccessManagerEventBufferFlushStrategyImplementation>(configurationSection[bufferImplementationPropertyName], false, out bufferImplementation);
            if (parseResult == false)
                throw new Exception($"{configurationTypeName} configuration property '{bufferImplementationPropertyName}' with value '{configurationSection[bufferImplementationPropertyName]}' could not be converted to a {typeof(AccessManagerEventBufferFlushStrategyImplementation).Name}.");
            returnConfiguration.BufferImplementation = bufferImplementation;
            returnConfiguration.BufferSizeLimit = GetConfigurationValueAsInteger(bufferSizeLimitPropertyName, configurationSection);
            returnConfiguration.FlushLoopInterval = GetConfigurationValueAsInteger(flushLoopIntervalPropertyName, configurationSection);

            return returnConfiguration;
        }
    }
}
