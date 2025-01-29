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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Defines methods to convert an <see cref="HttpRequest"/> to an <see cref="HttpRequestMessage"/>, updating the target host.
    /// </summary>
    /// <remarks>Used to proxy ASP.NET Core Web API application requests to a different host.</remarks>
    public interface IHttpRequestResponseMessageConverter
    {
        // TODO: This interface and implementing class are not used after deciding to implement all routing within DistributedAccessManagerOperationRouter
        //   Could remove these, but feel like they could be useful at some point

        /// <summary>
        /// Converts an <see cref="HttpRequest"/> to an <see cref="HttpRequestMessage"/>, updating the target host using the specified parameters.
        /// </summary>
        /// <param name="sourceRequest">The <see cref="HttpRequest"/> to convert.</param>
        /// <param name="targetRequest">The <see cref="HttpRequestMessage"/> to convert to.</param>
        /// <param name="targetScheme">The updated target scheme.</param>
        /// <param name="targetHost">The updated target host.</param>
        /// <param name="targetPort">The updated yarget port.</param>
        void ConvertRequest(HttpRequest sourceRequest, HttpRequestMessage targetRequest, String targetScheme, String targetHost, UInt16 targetPort);

        /// <summary>
        /// Converts an <see cref="HttpResponseMessage"/> to an <see cref="HttpResponse"/>.
        /// </summary>
        /// <param name="targetResponse">The <see cref="HttpResponseMessage"/> to convert (returned from an <see cref="HttpClient"/> call).</param>
        /// <param name="sourceHttpResponse">>The <see cref="HttpResponse"/> to convert to.</param>
        Task ConvertResponseAsync(HttpResponseMessage targetResponse, HttpResponse sourceHttpResponse);
    }
}
