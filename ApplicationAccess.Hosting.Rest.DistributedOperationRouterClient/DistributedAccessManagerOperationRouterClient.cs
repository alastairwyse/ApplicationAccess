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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationLogging;
using ApplicationMetrics;
using Polly;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouterClient
{
    /// <summary>
    /// Client class which asyncronously interfaces to a <see cref="IDistributedAccessManagerOperationRouter{TClientConfiguration}"/> instance hosted as a REST web API.
    /// </summary>
    public class DistributedAccessManagerOperationRouterClient : 
        DistributedAccessManagerAsyncClient<String, String, String, String>, 
        IDistributedAccessManagerOperationRouter
    {
        // TODO: Although this is an 'Async' client by definition, the methods defined are seldom called over the client's lifetime (i.e. likely once each per lifetime).
        //   Hence more useful to make the methods adhere to IDistributedAccessManagerOperationRouter, rather than have to define a whole new async version of the interface.
        //   With only downside that we have to vall Wait() on the underlying async methods called.

        /// <inheritdoc/>
        public Boolean RoutingOn
        {
            set
            {
                if (value == true)
                {
                    var url = new Uri(baseUrl, Uri.EscapeDataString("routing:switchOn"));
                    SendPostRequest(url);
                }
                else
                {
                    var url = new Uri(baseUrl, Uri.EscapeDataString("routing:switchOff"));
                    SendPostRequest(url);
                }
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public DistributedAccessManagerOperationRouterClient
        (
            Uri baseUrl,
            Int32 retryCount,
            Int32 retryInterval
        )
            : base(baseUrl, new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), retryCount, retryInterval)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerOperationRouterClient
        (
            Uri baseUrl,
            Int32 retryCount,
            Int32 retryInterval,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : base(baseUrl, new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), retryCount, retryInterval, logger, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public DistributedAccessManagerOperationRouterClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            Int32 retryCount,
            Int32 retryInterval
        )
            : base(baseUrl, httpClient, new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), retryCount, retryInterval)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerOperationRouterClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            Int32 retryCount,
            Int32 retryInterval,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : base(baseUrl, httpClient, new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), retryCount, retryInterval, logger, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="exceptionHandingPolicy">Exception handling policy for HttpClient calls.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>When setting parameter 'exceptionHandingPolicy', note that the web API only returns non-success HTTP status errors in the case of persistent, and non-transient errors (e.g. 400 in the case of bad/malformed requests, and 500 in the case of critical server-side errors).  Retrying the same request after receiving these error statuses will result in an identical response, and hence these statuses are not passed to Polly and will be ignored if included as part of a transient exception handling policy.  Exposing of this parameter is designed to allow overriding of the retry policy and actions when encountering <see cref="HttpRequestException">HttpRequestExceptions</see> caused by network errors, etc.</remarks>
        public DistributedAccessManagerOperationRouterClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            AsyncPolicy exceptionHandingPolicy,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : base(baseUrl, httpClient, new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), new StringUniqueStringifier(), exceptionHandingPolicy, logger, metricLogger)
        {
        }

        /// <inheritdoc/>
        public void PauseOperations()
        {
            var url = new Uri(baseUrl, Uri.EscapeDataString("operationProcessing:pause"));
            SendPostRequest(url);
        }

        /// <inheritdoc/>
        public void ResumeOperations()
        {
            var url = new Uri(baseUrl, Uri.EscapeDataString("operationProcessing:resume"));
            SendPostRequest(url);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Sends an HTTP POST request, expecting a 200 status returned to indicate success.
        /// </summary>
        /// <param name="requestUrl">The URL of the request.</param>
        protected void SendPostRequest(Uri requestUrl)
        {
            Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction = async (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (responseStatusCode != HttpStatusCode.OK)
                {
                    await HandleNonSuccessResponseStatusAsync(requestMethod, url, responseStatusCode, responseBody);
                }
            };
            SendRequestAsync(HttpMethod.Post, requestUrl, responseAction).Wait();
        }

        #endregion
    }
}
