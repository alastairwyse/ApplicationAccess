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
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using ApplicationAccess.Serialization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.EventCache.IntegrationTests
{
    /// <summary>
    /// Tests custom routines and settings in the ASP.NET middleware layers (e.g. custom error handling).
    /// </summary>
    public class MiddlewareTests : IntegrationTestsBase
    {
        [Test]
        public void DeserializationException()
        {
            const String requestUrl = "api/v1/eventBufferItems";
            const String invalidPropertyName = "invalidProperty";
            var invalidEvent = new JObject()
            {
                { invalidPropertyName, "Invalid Property" }
            };
            var requestBody = new JArray() { invalidEvent };
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                requestMessage.Content = new StringContent(requestBody.ToString(), Encoding.UTF8, MediaTypeNames.Application.Json);

                using (HttpResponseMessage response = client.SendAsync(requestMessage).Result)
                {

                    JObject jsonResponse = ConvertHttpContentToJson(response.Content);
                    AssertJsonIsHttpErrorResponse(jsonResponse, typeof(DeserializationException).Name, $"Encountered JSON property '{invalidPropertyName}' when expecting to read property 'eventId'.");
                    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
                }
            }
        }
    }
}
