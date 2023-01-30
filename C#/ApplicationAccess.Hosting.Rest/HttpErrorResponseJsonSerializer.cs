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
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Serializes <see cref="HttpErrorResponse"/> instances to JSON documents.
    /// </summary>
    public class HttpErrorResponseJsonSerializer
    {
        protected const String errorPropertyName = "error";
        protected const String codePropertyName = "code";
        protected const String messagePropertyName = "message";
        protected const String targetPropertyName = "target";
        protected const String attributesPropertyName = "attributes";
        protected const String namePropertyName = "name";
        protected const String valuePropertyName = "value";
        protected const String innerErrorPropertyName = "innererror";

        /// <summary>
        /// Serializes the specified HttpErrorResponse to a JSON document.
        /// </summary>
        /// <param name="httpErrorResponse">The HttpErrorResponse object to serialize.</param>
        /// <returns>A JSON document representing the HttpErrorResponse.</returns>
        public JObject Serialize(HttpErrorResponse httpErrorResponse)
        {
            var returnDocument = new JObject();

            returnDocument.Add(errorPropertyName, SerializeError(httpErrorResponse));

            return returnDocument;
        }

        /// <summary>
        /// Serializes the 'error' and 'innererror' properties of the JSON document returned by the Serialize() method.
        /// </summary>
        /// <param name="httpErrorResponse">The HttpErrorResponse object to serialize.</param>
        /// <returns>The 'error' or 'innererror' property of the JSON document.</returns>
        protected JObject SerializeError(HttpErrorResponse httpErrorResponse)
        {
            var returnDocument = new JObject();

            returnDocument.Add(codePropertyName, httpErrorResponse.Code);
            returnDocument.Add(messagePropertyName, httpErrorResponse.Message);
            if (httpErrorResponse.Target != null)
            {
                returnDocument.Add(targetPropertyName, httpErrorResponse.Target);
            }
            var attributesJson = new JArray();
            foreach (Tuple<String, String> currentAttribute in httpErrorResponse.Attributes)
            {
                var currentAttributeJson = new JObject();
                currentAttributeJson.Add(namePropertyName, currentAttribute.Item1);
                currentAttributeJson.Add(valuePropertyName, currentAttribute.Item2);
                attributesJson.Add(currentAttributeJson);
            }
            if (attributesJson.Count > 0)
            {
                returnDocument.Add(attributesPropertyName, attributesJson);
            }
            if (httpErrorResponse.InnerError != null)
            {
                returnDocument.Add(innerErrorPropertyName, SerializeError(httpErrorResponse.InnerError));
            }

            return returnDocument;
        }
    }
}
