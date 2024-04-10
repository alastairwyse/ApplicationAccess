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
using System.Net.Http;
using System.Threading.Tasks;
using ApplicationLogging;
using ApplicationMetrics;
using Polly;

namespace ApplicationAccess.Hosting.Rest.AsyncClient
{
    /// <summary>
    /// Client class which asyncronously interfaces to an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instance hosted as a REST web API.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>This class is thread safe and can be used by multiple threads concurrently.  However, clients needs to ensure that custom implementations of <see cref="IUniqueStringifier{T}"/> passed to the constructor are also thread safe.</remarks>
    public class AccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess> : AccessManagerAsyncClientBase<TUser, TGroup, TComponent, TAccess>, IAccessManagerAsyncQueryProcessor<TUser, TGroup, TComponent, TAccess>, IAccessManagerAsyncEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClient class.
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
        public AccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClient class.
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
        public AccessManagerAsyncClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClient class.
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
        public AccessManagerAsyncClient
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
            : this(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, 1, 1, logger, metricLogger)
        {
            this.exceptionHandingPolicy = exceptionHandingPolicy;
        }

        /// <inheritdoc/>
        public async Task<List<TUser>> GetUsersAsync()
        {
            return await base.GetUsersBaseAsync();
        }

        /// <inheritdoc/>
        public async Task<List<TGroup>> GetGroupsAsync()
        {
            return await base.GetGroupsBaseAsync();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityTypesAsync()
        {
            return await base.GetEntityTypesBaseAsync();
        }

        /// <inheritdoc/>
        public async Task AddUserAsync(TUser user)
        {
            await base.AddUserBaseAsync(user);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsUserAsync(TUser user)
        {
            return await base.ContainsUserBaseAsync(user);
        }

        /// <inheritdoc/>
        public async Task RemoveUserAsync(TUser user)
        {
            await base.RemoveUserBaseAsync(user);
        }

        /// <inheritdoc/>
        public async Task AddGroupAsync(TGroup group)
        {
            await base.AddGroupBaseAsync(group);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsGroupAsync(TGroup group)
        {
            return await base.ContainsGroupBaseAsync(group);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupAsync(TGroup group)
        {
            await base.RemoveGroupBaseAsync(group);
        }

        /// <inheritdoc/>
        public async Task AddUserToGroupMappingAsync(TUser user, TGroup group)
        {
            await base.AddUserToGroupMappingBaseAsync(user, group);
        }

        /// <inheritdoc/>
        public async Task<List<TGroup>> GetUserToGroupMappingsAsync(TUser user, Boolean includeIndirectMappings)
        {
            return await base.GetUserToGroupMappingsBaseAsync(user, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToGroupMappingAsync(TUser user, TGroup group)
        {
            await base.RemoveUserToGroupMappingBaseAsync(user, group);
        }

        /// <inheritdoc/>
        public async Task AddGroupToGroupMappingAsync(TGroup fromGroup, TGroup toGroup)
        {
            await base.AddGroupToGroupMappingBaseAsync(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public async Task<List<TGroup>> GetGroupToGroupMappingsAsync(TGroup group, Boolean includeIndirectMappings)
        {
            return await base.GetGroupToGroupMappingsBaseAsync(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToGroupMappingAsync(TGroup fromGroup, TGroup toGroup)
        {
            await base.RemoveGroupToGroupMappingBaseAsync(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            await base.AddUserToApplicationComponentAndAccessLevelMappingBaseAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<TComponent, TAccess>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(TUser user)
        {
            return await base.GetUserToApplicationComponentAndAccessLevelMappingsBaseAsync(user);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            await base.RemoveUserToApplicationComponentAndAccessLevelMappingBaseAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            await base.AddGroupToApplicationComponentAndAccessLevelMappingBaseAsync(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<TComponent, TAccess>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(TGroup group)
        {
            return await base.GetGroupToApplicationComponentAndAccessLevelMappingsBaseAsync(group);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            await base.RemoveGroupToApplicationComponentAndAccessLevelMappingBaseAsync(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task AddEntityTypeAsync(String entityType)
        {
            await base.AddEntityTypeBaseAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityTypeAsync(String entityType)
        {
            return await base.ContainsEntityTypeBaseAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task RemoveEntityTypeAsync(String entityType)
        {
            await base.RemoveEntityTypeBaseAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task AddEntityAsync(String entityType, String entity)
        {
            await base.AddEntityBaseAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAsync(String entityType)
        {
            return await base.GetEntitiesBaseAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityAsync(String entityType, String entity)
        {
            return await base.ContainsEntityBaseAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task RemoveEntityAsync(String entityType, String entity)
        {
            await base.RemoveEntityBaseAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task AddUserToEntityMappingAsync(TUser user, String entityType, String entity)
        {
            await base.AddUserToEntityMappingBaseAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(TUser user)
        {
            return await base.GetUserToEntityMappingsBaseAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToEntityMappingsAsync(TUser user, String entityType)
        {
            return await base.GetUserToEntityMappingsBaseAsync(user, entityType);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToEntityMappingAsync(TUser user, String entityType, String entity)
        {
            await base.RemoveUserToEntityMappingBaseAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task AddGroupToEntityMappingAsync(TGroup group, String entityType, String entity)
        {
            await base.AddGroupToEntityMappingBaseAsync(group, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(TGroup group)
        {
            return await base.GetGroupToEntityMappingsBaseAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToEntityMappingsAsync(TGroup group, String entityType)
        {
            return await base.GetGroupToEntityMappingsBaseAsync(group, entityType);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToEntityMappingAsync(TGroup group, String entityType, String entity)
        {
            await base.RemoveGroupToEntityMappingBaseAsync(group, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return await base.HasAccessToApplicationComponentBaseAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(TUser user, String entityType, String entity)
        {
            return await base.HasAccessToEntityBaseAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByUserAsync(TUser user)
        {
            return await base.GetApplicationComponentsAccessibleByUserBaseAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByGroupAsync(TGroup group)
        {
            return await base.GetApplicationComponentsAccessibleByGroupBaseAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(TUser user)
        {
            return await base.GetEntitiesAccessibleByUserBaseAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByUserAsync(TUser user, String entityType)
        {
            return await base.GetEntitiesAccessibleByUserBaseAsync(user, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(TGroup group)
        {
            return await base.GetEntitiesAccessibleByGroupBaseAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupAsync(TGroup group, String entityType)
        {
            return await base.GetEntitiesAccessibleByGroupBaseAsync(group, entityType);
        }
    }
}
