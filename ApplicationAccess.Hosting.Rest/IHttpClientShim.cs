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
    /// Replicates methods on the <see cref="HttpClient"/> class, so that they can be abstracted and mocked in unit tests.
    /// </summary>
    public interface IHttpClientShim
    {
        /// <summary>
        /// Sends an HTTP request with the specified request.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <returns>An HTTP response message.</returns>
        HttpResponseMessage Send(HttpRequestMessage request);

        /// <summary>
        /// Send an HTTP request as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    }
}
