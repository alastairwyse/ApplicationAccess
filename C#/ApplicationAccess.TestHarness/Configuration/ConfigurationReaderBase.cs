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
    abstract class ConfigurationReaderBase
    {
        /// <summary>The name of the type of configuration being read (for use in exception messages).</summary>
        protected String configurationTypeName;

        public ConfigurationReaderBase(String configurationTypeName)
        {
            this.configurationTypeName = configurationTypeName;
        }

        protected void ThrowExceptionIfPropertyNotFound(String propertyName, IConfigurationSection configurationSection)
        {
            if (configurationSection[propertyName] == null)
                throw new Exception($"{configurationTypeName} configuration did not contain a '{propertyName}' property.");
        }

        protected void ThrowExceptionIfSectionNotFound(String sectionName, IConfigurationSection configurationSection)
        {
            if (configurationSection.GetSection(sectionName).Exists() == false)
                throw new Exception($"{configurationTypeName} configuration did not contain a '{sectionName}' section.");
        }

        protected Int32 GetConfigurationValueAsInteger(String propertyName, IConfigurationSection configurationSection)
        {
            String valueAsString = configurationSection[propertyName];
            Int32 returnValue = 0;
            Boolean parseResult = Int32.TryParse(valueAsString, out returnValue);
            if (parseResult == false)
                throw new Exception($"{configurationTypeName} configuration property '{propertyName}' with value '{valueAsString}' could not be converted to an integer.");

            return returnValue;
        }

        protected Double GetConfigurationValueAsDouble(String propertyName, IConfigurationSection configurationSection)
        {
            String valueAsString = configurationSection[propertyName];
            Double returnValue = 0;
            Boolean parseResult = Double.TryParse(valueAsString, out returnValue);
            if (parseResult == false)
                throw new Exception($"{configurationTypeName} configuration property '{propertyName}' with value '{valueAsString}' could not be converted to a double.");

            return returnValue;
        }

        protected Boolean GetConfigurationValueAsBoolean(String propertyName, IConfigurationSection configurationSection)
        {
            String valueAsString = configurationSection[propertyName];
            Boolean returnValue = false;
            Boolean parseResult = Boolean.TryParse(valueAsString, out returnValue);
            if (parseResult == false)
                throw new Exception($"{configurationTypeName} configuration property '{propertyName}' with value '{valueAsString}' could not be converted to a boolean.");

            return returnValue;
        }
    }
}
