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
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Distribution.Serialization
{
    /// <summary>
    /// Serializes and deserializes <see cref="AccessManagerRestClientConfiguration"/> instances to and from JSON documents.
    /// </summary>
    public class AccessManagerRestClientConfigurationJsonSerializer : IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<AccessManagerRestClientConfiguration>
    {
        #pragma warning disable 1591

        protected const String baseUrlPropertyName = "baseUrl";

        #pragma warning restore 1591

        /// <inheritdoc/>
        public string Serialize(AccessManagerRestClientConfiguration clientConfiguration)
        {
            var serializedClientConfiguration = new JObject();
            serializedClientConfiguration.Add(baseUrlPropertyName, clientConfiguration.BaseUrl);

            return serializedClientConfiguration.ToString(Formatting.None);
        }

        /// <inheritdoc/>
        public AccessManagerRestClientConfiguration Deserialize(string serializedClientConfiguration)
        {
            JObject clientConfigurationJson;
            try
            {
                clientConfigurationJson = JObject.Parse(serializedClientConfiguration);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Parameter '{nameof(serializedClientConfiguration)}' could not be parsed as JSON.", nameof(serializedClientConfiguration), e);
            }
            if (clientConfigurationJson[baseUrlPropertyName] == null)
            {
                throw new ArgumentException($"JSON document in parameter '{nameof(serializedClientConfiguration)}' does not contain a '{baseUrlPropertyName}' property.", nameof(serializedClientConfiguration));
            }
            if (Uri.TryCreate(clientConfigurationJson[baseUrlPropertyName].ToString(), UriKind.RelativeOrAbsolute, out Uri baseUrl) == false)
            {
                throw new DeserializationException($"Property '{baseUrlPropertyName}' with value '{clientConfigurationJson[baseUrlPropertyName].ToString()}' in JSON document in parameter '{nameof(serializedClientConfiguration)}' could not be parsed as a {typeof(Uri).Name}.");
            }

            return new AccessManagerRestClientConfiguration(baseUrl);
        }
    }
}
