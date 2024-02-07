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
using Microsoft.Extensions.Configuration;
using ApplicationAccess.TestHarness.Configuration;
using System.Configuration;

namespace ApplicationAccess.InstanceComparer.Configuration
{
    class ExcludeEntitiesConfigurationReader : ConfigurationReaderBase
    {
        protected const String entityTypePropertyName = "EntityType";
        protected const String entitiesPropertyName = "Entities";

        public ExcludeEntitiesConfigurationReader()
            : base("Entity and entity type")
        {
        }

        public IList<ExcludeEntitiesConfiguration> Read(IEnumerable<IConfigurationSection> configurationSections)
        {
            var returnConfiguration = new List<ExcludeEntitiesConfiguration>();
            foreach (IConfigurationSection currentElement in configurationSections)
            {
                ThrowExceptionIfPropertyNotFound(entityTypePropertyName, currentElement);

                String entityType = currentElement[entityTypePropertyName];
                List<String> entities = currentElement.GetSection(entitiesPropertyName).Get<List<String>>();
                if (entities == null)
                {
                    throw new Exception($"{configurationTypeName} configuration for entity type '{entityType}' rither contained no '{entitiesPropertyName}' property, or contained an '{entitiesPropertyName}' property whose value could not be converted to a List of String objects.");
                }

                returnConfiguration.Add(new ExcludeEntitiesConfiguration() { EntityType = entityType, Entities = entities });
            }

            return returnConfiguration;
        }
    }
}
