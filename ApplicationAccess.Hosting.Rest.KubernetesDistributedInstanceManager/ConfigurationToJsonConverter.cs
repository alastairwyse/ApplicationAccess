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
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// Converts an <see cref="IConfigurationSection"/> to a JSON document.
    /// </summary>
    public class ConfigurationToJsonConverter
    {
        /// <summary>
        /// Converts an <see cref="IConfigurationSection"/> and all its child sections to a JSON document (as a <see cref="JObject"/>).
        /// </summary>
        /// <param name="configurationSection">The configuration section to convert.</param>
        /// <returns></returns>
        public void Convert(IConfigurationSection configurationSection, JObject returnJObject)
        {
            foreach (IConfigurationSection currentChild in configurationSection.GetChildren())
            {
                if (currentChild.Value == null)
                {
                    JObject value = new();
                    Convert(currentChild, value);
                    returnJObject.Add(currentChild.Key, value);
                }
                else
                {
                    returnJObject.Add(currentChild.Key, currentChild.Value);
                }
            }
        }
    }
}
