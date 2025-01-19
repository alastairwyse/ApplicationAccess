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
    /// Default implementation of <see cref="IHttpRequestResponseMessageConverter"/>.
    /// </summary>
    public class HttpRequestResponseMessageConverter : IHttpRequestResponseMessageConverter
    {
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
