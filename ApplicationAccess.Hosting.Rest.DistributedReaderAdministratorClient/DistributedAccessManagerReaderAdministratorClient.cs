/*
 * Copyright 2026 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Net;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.AsyncClient;

namespace ApplicationAccess.Hosting.Rest.DistributedReaderAdministratorClient
{
    /// <summary>
    /// Client class which interfaces to an <see cref="IDistributedAccessManagerReaderAdministrator"/> instance hosted as a REST web API.
    /// </summary>
    public class DistributedAccessManagerReaderAdministratorClient : AccessManagerAsyncClientBase<String, String, String, String>, IDistributedAccessManagerReaderAdministrator
    {
        // TODO: Could make this client class and IDistributedAccessManagerReaderAdministrator async.  However these methods are likely to be called very infrequently (a handful of times over
        //   the lifetime of a distributed reader node), and are by nature syncronous, in that the caller will always want to wait for their completion before continuing with subsequent 
        //   steps/operations.  Hence for now am going to make the methods synchronous, but that may change in the future.

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedReaderAdministratorClient.DistributedAccessManagerReaderAdministratorClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public DistributedAccessManagerReaderAdministratorClient
        (
            Uri baseUrl,
            Int32 retryCount,
            Int32 retryInterval
        )
            : base(baseUrl, new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), retryCount, retryInterval)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedReaderAdministratorClient.DistributedAccessManagerReaderAdministratorClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public DistributedAccessManagerReaderAdministratorClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            Int32 retryCount,
            Int32 retryInterval
        )
            : base(baseUrl, httpClient, new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), retryCount, retryInterval)
        {
        }

        /// <inheritdoc/>
        public void Load()
        {
            var requestUrl = new Uri(baseUrl, Uri.EscapeDataString("dataElements:load"));
            Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction = async (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (responseStatusCode != HttpStatusCode.OK)
                {
                    await HandleNonSuccessResponseStatusAsync(requestMethod, url, responseStatusCode, responseBody);
                }
            };
            SendRequestAsync(HttpMethod.Post, requestUrl, responseAction).Wait();
        }
    }
}
