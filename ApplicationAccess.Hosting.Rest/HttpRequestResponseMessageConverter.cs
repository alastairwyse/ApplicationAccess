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
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Converts a <see cref="HttpRequest">Microsoft.AspNetCore.Http.HttpRequest</see> (i.e. received in an ASP.NET Core Web API application) to a <see cref="HttpRequestMessage">System.Net.Http.HttpRequestMessage</see> with a different host destination, so the <see cref="HttpRequestMessage"/> can be sent via an <see cref="HttpClient"/>.  Similarly converts the resulting <see cref="HttpResponseMessage">System.Net.Http.HttpResponseMessage</see> from the client into a <see cref="HttpResponse">Microsoft.AspNetCore.Http.HttpResponse</see> which can be returned as an ASP.NET Core Web API application response.
    /// </summary>
    /// <remarks>Used to proxy ASP.NET Core Web API application requests to a different host.</remarks>
    public class HttpRequestResponseMessageConverter
    {
        /// <summary>
        /// Converts an <see cref="HttpRequest"/> to an <see cref="HttpRequestMessage"/>, updating the target host using the specified parameters.
        /// </summary>
        /// <param name="sourceRequest">The <see cref="HttpRequest"/> to convert.</param>
        /// <param name="targetRequest">The <see cref="HttpRequestMessage"/> to convert to.</param>
        /// <param name="targetScheme">The updated target scheme.</param>
        /// <param name="targetHost">The updated target host.</param>
        /// <param name="targetPort">The updated yarget port.</param>
        public void ConvertRequest(HttpRequest sourceRequest, HttpRequestMessage targetRequest, String targetScheme, String targetHost, UInt16 targetPort)
        {
            // Copy the request headers (based on https://github.com/twitchax/AspNetCore.Proxy/blob/master/src/Core/Extensions/Http.cs)
            foreach (KeyValuePair<String, StringValues> currentSourceHeader in sourceRequest.Headers)
            {
                if (currentSourceHeader.Key != HeaderNames.ContentLength)
                {
                    targetRequest.Headers.TryAddWithoutValidation(currentSourceHeader.Key, currentSourceHeader.Value.ToArray());
                    // No need to copy content headers as none of the supported endpoints have body/content
                }
            }

            // Copy the method
            targetRequest.Method = new HttpMethod(sourceRequest.Method);

            // Create the target URL
            var targetUriBuilder = new UriBuilder();
            targetUriBuilder.Scheme = targetScheme;
            targetUriBuilder.Host = targetHost;
            targetUriBuilder.Port = targetPort;
            targetUriBuilder.Path = sourceRequest.Path;
            targetUriBuilder.Query = sourceRequest.QueryString.ToString();
            targetRequest.RequestUri = targetUriBuilder.Uri;
        }

        /// <summary>
        /// Converts an <see cref="HttpResponseMessage"/> to an <see cref="HttpResponse"/>.
        /// </summary>
        /// <param name="targetResponse">The <see cref="HttpResponseMessage"/> to convert (returned from an <see cref="HttpClient"/> call).</param>
        /// <param name="sourceHttpResponse">>The <see cref="HttpResponse"/> to convert to.</param>
        public async Task ConvertResponseAsync(HttpResponseMessage targetResponse, HttpResponse sourceHttpResponse)
        {
            sourceHttpResponse.StatusCode = ((Int32)targetResponse.StatusCode);
            if (targetResponse.Content.Headers.ContentType != null)
            {
                sourceHttpResponse.ContentType = targetResponse.Content.Headers.ContentType.ToString();
            }
            await targetResponse.Content.CopyToAsync(sourceHttpResponse.Body);
        }
    }
}
