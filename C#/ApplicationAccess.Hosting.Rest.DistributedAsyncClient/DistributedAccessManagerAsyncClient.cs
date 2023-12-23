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
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.AsyncClient;
using ApplicationLogging;
using ApplicationMetrics;
using Polly;
using ApplicationAccess.Hosting.Models.DataTransferObjects;

namespace ApplicationAccess.Hosting.Rest.DistributedAsyncClient
{
    /// <summary>
    /// Client class which asyncronously interfaces to a <see cref="DistributedAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance hosted as a REST web API.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class DistributedAccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess> : AccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess>, IDistributedAccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public DistributedAccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
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
        public DistributedAccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public DistributedAccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
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
        public DistributedAccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClient class.
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
        public DistributedAccessManagerAsyncClient
        (
            Uri baseUrl,
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            AsyncPolicy exceptionHandingPolicy,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, exceptionHandingPolicy, logger, metricLogger)
        {
        }

        /// <inheritdoc/>
        public async Task<List<TGroup>> GetGroupToGroupMappingsAsync(IEnumerable<TGroup> groups)
        {
            String queryString = CreateGroupsParameterQueryString(nameof(groups), groups);
            var url = new Uri(baseUrl, $"groupToGroupMappings?{queryString}");
            var returnList = new List<TGroup>();
            foreach (String currentGroup in await SendGetRequestAsync<List<String>>(url))
            {
                returnList.Add(groupStringifier.FromString(currentGroup));
            }

            return returnList;
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(IEnumerable<TGroup> groups, TComponent applicationComponent, TAccess accessLevel)
        {
            String queryString = CreateGroupsParameterQueryString(nameof(groups), groups);
            var url = new Uri(baseUrl, $"dataElementAccess/applicationComponent/applicationComponent/{applicationComponentStringifier.ToString(applicationComponent)}/accessLevel/{accessLevelStringifier.ToString(accessLevel)}?{queryString}");

            return await SendGetRequestAsync<Boolean>(url);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(IEnumerable<TGroup> groups, String entityType, String entity)
        {
            String queryString = CreateGroupsParameterQueryString(nameof(groups), groups);
            var url = new Uri(baseUrl, $"dataElementAccess/entity/entityType/{entityType}/entity/{entity}?{queryString}");

            return await SendGetRequestAsync<Boolean>(url);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByGroupsAsync(IEnumerable<TGroup> groups)
        {
            String queryString = CreateGroupsParameterQueryString(nameof(groups), groups);
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings?{queryString}");
            var returnList = new List<Tuple<TComponent, TAccess>>();
            foreach (ApplicationComponentAndAccessLevel<String, String> currentApplicationComponent in await SendGetRequestAsync<List<ApplicationComponentAndAccessLevel<String, String>>>(url))
            {
                returnList.Add(new Tuple<TComponent, TAccess>
                (
                    applicationComponentStringifier.FromString(currentApplicationComponent.ApplicationComponent),
                    accessLevelStringifier.FromString(currentApplicationComponent.AccessLevel)
                ));
            }

            return returnList;
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<TGroup> groups)
        {
            String queryString = CreateGroupsParameterQueryString(nameof(groups), groups);
            var url = new Uri(baseUrl, $"groupToEntityMappings?{queryString}");
            var returnList = new List<Tuple<String, String>>();
            foreach (EntityTypeAndEntity currentEntityTypeAndEntity in await SendGetRequestAsync<List<EntityTypeAndEntity>>(url))
            {
                returnList.Add(new Tuple<String, String>
                (
                    currentEntityTypeAndEntity.EntityType, 
                    currentEntityTypeAndEntity.Entity
                ));
            }

            return returnList;
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<TGroup> groups, String entityType)
        {
            String queryString = CreateGroupsParameterQueryString(nameof(groups), groups);
            var url = new Uri(baseUrl, $"groupToEntityMappings/entityType/{entityType}?{queryString}");
            var returnList = new List<String>();

            return await SendGetRequestAsync<List<String>>(url);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a URL query string containing a collection of groups to be passed as a parameter to a REST method.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="groups">The collection of groups to include in the query string.</param>
        /// <returns>The query string.</returns>
        protected String CreateGroupsParameterQueryString(String parameterName, IEnumerable<TGroup> groups)
        {
            var queryStringBuilder = new StringBuilder();
            Boolean firstAppend = true;
            foreach (TGroup currentGroup in groups)
            {
                if (firstAppend == true)
                {
                    firstAppend = false;
                }
                else
                {
                    queryStringBuilder.Append('&');
                }
                queryStringBuilder.Append(parameterName);
                queryStringBuilder.Append('=');
                queryStringBuilder.Append(groupStringifier.ToString(currentGroup));
            }

            return queryStringBuilder.ToString();
        }

        #endregion
    }
}
