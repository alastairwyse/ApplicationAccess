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
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.AsyncClient;
using ApplicationAccess.Persistence;
using ApplicationLogging;
using ApplicationMetrics;
using Polly;

namespace ApplicationAccess.Hosting.Rest.Client
{
    /// <summary>
    /// Client class which syncronously interfaces to an <see cref="AccessManagerTemporalEventBulkCache{TUser, TGroup, TComponent, TAccess}"/> instance hosted as a REST web API.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class EventCacheClient<TUser, TGroup, TComponent, TAccess> : AccessManagerClientBase<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Exception handling policy for HttpClient calls.</summary>
        protected Policy exceptionHandingPolicy;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.EventCacheClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public EventCacheClient
        (
            Uri baseUrl,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval
        )
            : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.EventCacheClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public EventCacheClient
        (
            Uri baseUrl,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.EventCacheClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public EventCacheClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval
        )
            : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.EventCacheClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public EventCacheClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.EventCacheClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="exceptionHandingPolicy">Exception handling policy for HttpClient calls.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>When setting parameter 'exceptionHandingPolicy', note that the web API only returns non-success HTTP status errors in the case of persistent, and non-transient errors (e.g. 400 in the case of bad/malformed requests, and 500 in the case of critical server-side errors).  Retrying the same request after receiving these error statuses will result in an identical response, and hence these statuses are not passed to Polly and will be ignored if included as part of a transient exception handling policy.  Exposing of this parameter is designed to allow overriding of the retry policy and actions when encountering <see cref="HttpRequestException">HttpRequestExceptions</see> caused by network errors, etc.</remarks>
        public EventCacheClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Policy exceptionHandingPolicy,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : this(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, 1, 1, logger, metricLogger)
        {
            this.exceptionHandingPolicy = exceptionHandingPolicy;
        }

        /// <inheritdoc/>
        public IList<TemporalEventBufferItemBase> GetAllEventsSince(Guid eventId)
        {
            var httpMethod = HttpMethod.Get;
            var requestUrl = new Uri(baseUrl, $"eventBufferItems?priorEventdId={eventId.ToString()}");
            List<TemporalEventBufferItemBase> returnEvents = null;
            Action httpClientAction = () =>
            {
                using (var request = new HttpRequestMessage(httpMethod, requestUrl))
                using (var response = SendRequestMessage(request))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Stream responseBody = response.Content.ReadAsStream();
                        HttpErrorResponse httpErrorResponse = DeserializeResponseBodyToHttpErrorResponse(responseBody);
                        responseBody.Position = 0;
                        // If a 404 status was returned, throw this as an EventNotCachedException
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            if (httpErrorResponse != null)
                            {
                                throw new EventNotCachedException(httpErrorResponse.Message);
                            }
                        }
                        HandleNonSuccessResponseStatus(httpMethod, requestUrl, response.StatusCode, responseBody);
                    }

                    var serialIzerOptions = new JsonSerializerOptions
                    {
                        Converters =
                        {
                            new TemporalEventBufferItemBaseConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
                        }
                    };
                    returnEvents = JsonSerializer.Deserialize<List<TemporalEventBufferItemBase>>(response.Content.ReadAsStream(), serialIzerOptions);
                }
            };

            exceptionHandingPolicy.Execute(httpClientAction);

            return returnEvents;
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            var httpMethod = HttpMethod.Post;
            var requestUrl = new Uri(baseUrl, $"eventBufferItems");
            var serialIzerOptions = new JsonSerializerOptions
            {
                Converters =
                {
                    new TemporalEventBufferItemBaseConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
                }
            };

            Action httpClientAction = () =>
            {
                using (var request = new HttpRequestMessage(httpMethod, requestUrl))
                {
                    request.Content = JsonContent.Create(events, typeof(List<TemporalEventBufferItemBase>), null, serialIzerOptions);
                    using (var response = SendRequestMessage(request))
                    {
                        if (response.StatusCode != HttpStatusCode.Created)
                        {
                            HandleNonSuccessResponseStatus(httpMethod, requestUrl, response.StatusCode, response.Content.ReadAsStream());
                        }
                    }
                }
            };

            exceptionHandingPolicy.Execute(httpClientAction);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Sends the specified <see cref="HttpRequestMessage"/> using the 'httpClient' member.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <returns>An HTTP response message.</returns>
        /// <remarks>Putting this call in a virtual method allows it to be overridden in a subclass and avoid "The synchronous method is not supported by 'Microsoft.AspNetCore.TestHost.ClientHandler'" errors.</remarks>
        protected virtual HttpResponseMessage SendRequestMessage(HttpRequestMessage request)
        {
            return httpClient.Send(request);
        }

        /// <inheritdoc/>
        protected override void SetupExceptionHanderPolicies(Int32 retryCount, Int32 retryInterval, Action<Exception, TimeSpan, Int32, Context> onRetryAction)
        {
            exceptionHandingPolicy = Policy.Handle<HttpRequestException>().WaitAndRetry(retryCount, (Int32 currentRetryNumber) => { return TimeSpan.FromSeconds(retryInterval); }, onRetryAction);
        }

        #endregion
    }
}
