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

using System.Net.Http;
using System.Threading.Tasks;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Default implementation of <see cref="IHttpClientShim"/>.
    /// </summary>
    public class HttpClientShim : IHttpClientShim
    {
        /// <summary>The underlying <see cref="HttpClient"/> instance used by the shim.</summary>
        protected HttpClient httpClient;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.HttpClientShim class.
        /// </summary>
        /// <param name="httpClient">The underlying <see cref="HttpClient"/> instance used by the shim.</param>
        public HttpClientShim(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <inheritdoc/>
        public HttpResponseMessage Send(HttpRequestMessage request)
        {
            return httpClient.Send(request);
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return await httpClient.SendAsync(request);
        }
    }
}
