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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Serialization;
using ApplicationLogging;
using ApplicationMetrics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace ApplicationAccess.Hosting.Rest.AsyncClient
{
    /// <summary>
    /// Base class for client classes which asyncronously interface to <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instances hosted as REST web APIs.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class AccessManagerAsyncClientBase<TUser, TGroup, TComponent, TAccess> : AccessManagerClientBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Exception handling policy for HttpClient calls.</summary>
        protected AsyncPolicy exceptionHandingPolicy;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerAsyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClientBase class.
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
        public AccessManagerAsyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerAsyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClientBase class.
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
        public AccessManagerAsyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerAsyncClientBase class.
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
        public AccessManagerAsyncClientBase
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

        #region IAccessManager Methods

        #pragma warning disable 1591

        protected async Task<List<TUser>> GetUsersBaseAsync()
        {
            var url = new Uri(baseUrl, "users");
            var returnList = new List<TUser>();
            foreach (String currentUserAsString in await SendGetRequestAsync<List<String>>(url))
            {
                returnList.Add(userStringifier.FromString(currentUserAsString));
            }

            return returnList;
        }

        protected async Task<List<TGroup>> GetGroupsBaseAsync()
        {
            var url = new Uri(baseUrl, "groups");
            var returnList = new List<TGroup>();
            foreach (String currentGroupAsString in await SendGetRequestAsync<List<String>>(url))
            {
                returnList.Add(groupStringifier.FromString(currentGroupAsString));
            }

            return returnList;
        }

        protected async Task<List<String>> GetEntityTypesBaseAsync()
        {
            var url = new Uri(baseUrl, "entityTypes");
            var returnList = new List<String>();

            return await SendGetRequestAsync<List<String>>(url);
        }

        protected async Task AddUserBaseAsync(TUser user)
        {
            var url = new Uri(baseUrl, $"users/{Uri.EscapeDataString(userStringifier.ToString(user))}");
            await SendPostRequestAsync(url);
        }

        protected async Task<Boolean> ContainsUserBaseAsync(TUser user)
        {
            var url = new Uri(baseUrl, $"users/{Uri.EscapeDataString(userStringifier.ToString(user))}");

            return await SendGetRequestForContainsMethodAsync(url);
        }

        protected async Task RemoveUserBaseAsync(TUser user)
        {
            var url = new Uri(baseUrl, $"users/{Uri.EscapeDataString(userStringifier.ToString(user))}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddGroupBaseAsync(TGroup group)
        {
            var url = new Uri(baseUrl, $"groups/{Uri.EscapeDataString(groupStringifier.ToString(group))}");
            await SendPostRequestAsync(url);
        }

        protected async Task<Boolean> ContainsGroupBaseAsync(TGroup group)
        {
            var url = new Uri(baseUrl, $"groups/{Uri.EscapeDataString(groupStringifier.ToString(group))}");

            return await SendGetRequestForContainsMethodAsync(url);
        }

        protected async Task RemoveGroupBaseAsync(TGroup group)
        {
            var url = new Uri(baseUrl, $"groups/{Uri.EscapeDataString(groupStringifier.ToString(group))}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddUserToGroupMappingBaseAsync(TUser user, TGroup group)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            var url = new Uri(baseUrl, $"userToGroupMappings/user/{encodedUser}/group/{encodedGroup}");
            await SendPostRequestAsync(url);
        }

        protected async Task<List<TGroup>> GetUserToGroupMappingsBaseAsync(TUser user, Boolean includeIndirectMappings)
        {
            var url = new Uri(baseUrl, $"userToGroupMappings/user/{Uri.EscapeDataString(userStringifier.ToString(user))}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TGroup>();
            foreach (UserAndGroup<String, String> currentUserAndGroup in await SendGetRequestAsync<List<UserAndGroup<String, String>>>(url))
            {
                returnList.Add(groupStringifier.FromString(currentUserAndGroup.Group));
            }

            return returnList;
        }

        protected async Task<List<TUser>> GetGroupToUserMappingsBaseAsync(TGroup group, Boolean includeIndirectMappings)
        {
            var url = new Uri(baseUrl, $"userToGroupMappings/group/{Uri.EscapeDataString(groupStringifier.ToString(group))}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TUser>();
            foreach (UserAndGroup<String, String> currentUserAndGroup in await SendGetRequestAsync<List<UserAndGroup<String, String>>>(url))
            {
                returnList.Add(userStringifier.FromString(currentUserAndGroup.User));
            }

            return returnList;
        }

        protected async Task RemoveUserToGroupMappingBaseAsync(TUser user, TGroup group)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            var url = new Uri(baseUrl, $"userToGroupMappings/user/{encodedUser}/group/{encodedGroup}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddGroupToGroupMappingBaseAsync(TGroup fromGroup, TGroup toGroup)
        {
            String encodedFromGroup = Uri.EscapeDataString(groupStringifier.ToString(fromGroup));
            String encodedToGroup = Uri.EscapeDataString(groupStringifier.ToString(toGroup));
            var url = new Uri(baseUrl, $"groupToGroupMappings/fromGroup/{encodedFromGroup}/toGroup/{encodedToGroup}");
            await SendPostRequestAsync(url);
        }

        protected async Task<List<TGroup>> GetGroupToGroupMappingsBaseAsync(TGroup group, Boolean includeIndirectMappings)
        {
            var url = new Uri(baseUrl, $"groupToGroupMappings/group/{Uri.EscapeDataString(groupStringifier.ToString(group))}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TGroup>();
            foreach (FromGroupAndToGroup<String> currentFromGroupAndToGroup in await SendGetRequestAsync<List<FromGroupAndToGroup<String>>>(url))
            {
                returnList.Add(groupStringifier.FromString(currentFromGroupAndToGroup.ToGroup));
            }

            return returnList;
        }

        protected async Task<List<TGroup>> GetGroupToGroupReverseMappingsBaseAsync(TGroup group, Boolean includeIndirectMappings)
        {
            var url = new Uri(baseUrl, $"groupToGroupReverseMappings/group/{Uri.EscapeDataString(groupStringifier.ToString(group))}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TGroup>();
            foreach (FromGroupAndToGroup<String> currentFromGroupAndToGroup in await SendGetRequestAsync<List<FromGroupAndToGroup<String>>>(url))
            {
                returnList.Add(groupStringifier.FromString(currentFromGroupAndToGroup.FromGroup));
            }

            return returnList;
        }

        protected async Task RemoveGroupToGroupMappingBaseAsync(TGroup fromGroup, TGroup toGroup)
        {
            String encodedFromGroup = Uri.EscapeDataString(groupStringifier.ToString(fromGroup));
            String encodedToGroup = Uri.EscapeDataString(groupStringifier.ToString(toGroup));
            var url = new Uri(baseUrl, $"groupToGroupMappings/fromGroup/{encodedFromGroup}/toGroup/{encodedToGroup}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddUserToApplicationComponentAndAccessLevelMappingBaseAsync(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedApplicationComponent = Uri.EscapeDataString(applicationComponentStringifier.ToString(applicationComponent));
            String encodedAccessLevel = Uri.EscapeDataString(accessLevelStringifier.ToString(accessLevel));
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{encodedUser}/applicationComponent/{encodedApplicationComponent}/accessLevel/{encodedAccessLevel}");
            await SendPostRequestAsync(url);
        }

        protected async Task<List<Tuple<TComponent, TAccess>>> GetUserToApplicationComponentAndAccessLevelMappingsBaseAsync(TUser user)
        {
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{Uri.EscapeDataString(userStringifier.ToString(user))}?includeIndirectMappings=false");
            var returnList = new List<Tuple<TComponent, TAccess>>();
            foreach (UserAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in await SendGetRequestAsync<List<UserAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnList.Add(new Tuple<TComponent, TAccess>
                (
                    applicationComponentStringifier.FromString(currentMapping.ApplicationComponent),
                    accessLevelStringifier.FromString(currentMapping.AccessLevel)
                ));
            }

            return returnList;
        }

        protected async Task<List<TUser>> GetApplicationComponentAndAccessLevelToUserMappingsBaseAsync(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            String encodedApplicationComponent = Uri.EscapeDataString(applicationComponentStringifier.ToString(applicationComponent));
            String encodedAccessLevel = Uri.EscapeDataString(accessLevelStringifier.ToString(accessLevel));
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/applicationComponent/{encodedApplicationComponent}/accessLevel/{encodedAccessLevel}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TUser>();
            foreach (UserAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in await SendGetRequestAsync<List<UserAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnList.Add(userStringifier.FromString(currentMapping.User));
            }

            return returnList;
        }

        protected async Task RemoveUserToApplicationComponentAndAccessLevelMappingBaseAsync(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedApplicationComponent = Uri.EscapeDataString(applicationComponentStringifier.ToString(applicationComponent));
            String encodedAccessLevel = Uri.EscapeDataString(accessLevelStringifier.ToString(accessLevel));
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{encodedUser}/applicationComponent/{encodedApplicationComponent}/accessLevel/{encodedAccessLevel}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddGroupToApplicationComponentAndAccessLevelMappingBaseAsync(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            String encodedApplicationComponent = Uri.EscapeDataString(applicationComponentStringifier.ToString(applicationComponent));
            String encodedAccessLevel = Uri.EscapeDataString(accessLevelStringifier.ToString(accessLevel));
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{encodedGroup}/applicationComponent/{encodedApplicationComponent}/accessLevel/{encodedAccessLevel}");
            await SendPostRequestAsync(url);
        }

        protected async Task<List<Tuple<TComponent, TAccess>>> GetGroupToApplicationComponentAndAccessLevelMappingsBaseAsync(TGroup group)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{encodedGroup}?includeIndirectMappings=false");
            var returnList = new List<Tuple<TComponent, TAccess>>();
            foreach (GroupAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in await SendGetRequestAsync<List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnList.Add(new Tuple<TComponent, TAccess>
                (
                    applicationComponentStringifier.FromString(currentMapping.ApplicationComponent),
                    accessLevelStringifier.FromString(currentMapping.AccessLevel)
                ));
            }

            return returnList;
        }

        protected async Task<List<TGroup>> GetApplicationComponentAndAccessLevelToGroupMappingsBaseAsync(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            String encodedApplicationComponent = Uri.EscapeDataString(applicationComponentStringifier.ToString(applicationComponent));
            String encodedAccessLevel = Uri.EscapeDataString(accessLevelStringifier.ToString(accessLevel));
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/applicationComponent/{encodedApplicationComponent}/accessLevel/{encodedAccessLevel}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TGroup>();
            foreach (GroupAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in await SendGetRequestAsync<List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnList.Add(groupStringifier.FromString(currentMapping.Group));
            }

            return returnList;
        }

        protected async Task RemoveGroupToApplicationComponentAndAccessLevelMappingBaseAsync(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            String encodedApplicationComponent = Uri.EscapeDataString(applicationComponentStringifier.ToString(applicationComponent));
            String encodedAccessLevel = Uri.EscapeDataString(accessLevelStringifier.ToString(accessLevel));
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{encodedGroup}/applicationComponent/{encodedApplicationComponent}/accessLevel/{encodedAccessLevel}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddEntityTypeBaseAsync(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{Uri.EscapeDataString(entityType)}");
            await SendPostRequestAsync(url);
        }

        protected async Task<Boolean> ContainsEntityTypeBaseAsync(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{Uri.EscapeDataString(entityType)}");

            return await SendGetRequestForContainsMethodAsync(url);
        }

        protected async Task RemoveEntityTypeBaseAsync(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{Uri.EscapeDataString(entityType)}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddEntityBaseAsync(String entityType, String entity)
        {
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"entityTypes/{encodedEntityType}/entities/{encodedEntity}");
            await SendPostRequestAsync(url);
        }

        protected async Task<List<String>> GetEntitiesBaseAsync(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{Uri.EscapeDataString(entityType)}/entities");
            var returnList = new List<String>();
            foreach (EntityTypeAndEntity currentEntityTypeAndEntity in await SendGetRequestAsync<List<EntityTypeAndEntity>>(url))
            {
                returnList.Add(currentEntityTypeAndEntity.Entity);
            }

            return returnList;
        }

        protected async Task<Boolean> ContainsEntityBaseAsync(String entityType, String entity)
        {
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"entityTypes/{encodedEntityType}/entities/{encodedEntity}");

            return await SendGetRequestForContainsMethodAsync(url);
        }

        protected async Task RemoveEntityBaseAsync(String entityType, String entity)
        {
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"entityTypes/{encodedEntityType}/entities/{encodedEntity}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddUserToEntityMappingBaseAsync(TUser user, String entityType, String entity)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{encodedUser}/entityType/{encodedEntityType}/entity/{encodedEntity}");
            await SendPostRequestAsync(url);
        }

        protected async Task<List<Tuple<String, String>>> GetUserToEntityMappingsBaseAsync(TUser user)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{encodedUser}?includeIndirectMappings=false");
            var returnList = new List<Tuple<String, String>>();
            foreach (UserAndEntity<String> currentMapping in await SendGetRequestAsync<List<UserAndEntity<String>>>(url))
            {
                returnList.Add(new Tuple<String, String>
                (
                    currentMapping.EntityType,
                    currentMapping.Entity
                ));
            }

            return returnList;
        }

        protected async Task<List<String>> GetUserToEntityMappingsBaseAsync(TUser user, String entityType)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{encodedUser}/entityType/{encodedEntityType}?includeIndirectMappings=false");
            var returnList = new List<String>();
            foreach (UserAndEntity<String> currentMapping in await SendGetRequestAsync<List<UserAndEntity<String>>>(url))
            {
                returnList.Add(currentMapping.Entity);
            }

            return returnList;
        }

        protected async Task<List<TUser>> GetEntityToUserMappingsBaseAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"userToEntityMappings/entityType/{encodedEntityType}/entity/{encodedEntity}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TUser>();
            foreach (UserAndEntity<String> currentMapping in await SendGetRequestAsync<List<UserAndEntity<String>>>(url))
            {
                returnList.Add(userStringifier.FromString(currentMapping.User));
            }

            return returnList;
        }

        protected async Task RemoveUserToEntityMappingBaseAsync(TUser user, String entityType, String entity)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{encodedUser}/entityType/{encodedEntityType}/entity/{encodedEntity}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task AddGroupToEntityMappingBaseAsync(TGroup group, String entityType, String entity)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{encodedGroup}/entityType/{encodedEntityType}/entity/{encodedEntity}");
            await SendPostRequestAsync(url);
        }

        protected async Task<List<Tuple<String, String>>> GetGroupToEntityMappingsBaseAsync(TGroup group)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{encodedGroup}?includeIndirectMappings=false");
            var returnList = new List<Tuple<String, String>>();
            foreach (GroupAndEntity<String> currentMapping in await SendGetRequestAsync<List<GroupAndEntity<String>>>(url))
            {
                returnList.Add(new Tuple<String, String>
                (
                    currentMapping.EntityType,
                    currentMapping.Entity
                ));
            }

            return returnList;
        }

        protected async Task<List<String>> GetGroupToEntityMappingsBaseAsync(TGroup group, String entityType)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{encodedGroup}/entityType/{encodedEntityType}?includeIndirectMappings=false");
            var returnList = new List<String>();
            foreach (GroupAndEntity<String> currentMapping in await SendGetRequestAsync<List<GroupAndEntity<String>>>(url))
            {
                returnList.Add(currentMapping.Entity);
            }

            return returnList;
        }

        protected async Task<List<TGroup>> GetEntityToGroupMappingsBaseAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"groupToEntityMappings/entityType/{encodedEntityType}/entity/{encodedEntity}?includeIndirectMappings={includeIndirectMappings}");
            var returnList = new List<TGroup>();
            foreach (GroupAndEntity<String> currentMapping in await SendGetRequestAsync<List<GroupAndEntity<String>>>(url))
            {
                returnList.Add(groupStringifier.FromString(currentMapping.Group));
            }

            return returnList;
        }

        protected async Task RemoveGroupToEntityMappingBaseAsync(TGroup group, String entityType, String entity)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{encodedGroup}/entityType/{encodedEntityType}/entity/{encodedEntity}");
            await SendDeleteRequestAsync(url);
        }

        protected async Task<Boolean> HasAccessToApplicationComponentBaseAsync(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedApplicationComponent = Uri.EscapeDataString(applicationComponentStringifier.ToString(applicationComponent));
            String encodedAccessLevel = Uri.EscapeDataString(accessLevelStringifier.ToString(accessLevel));
            var url = new Uri(baseUrl, $"dataElementAccess/applicationComponent/user/{encodedUser}/applicationComponent/{encodedApplicationComponent}/accessLevel/{encodedAccessLevel}");

            return await SendGetRequestAsync<Boolean>(url);
        }

        protected async Task<Boolean> HasAccessToEntityBaseAsync(TUser user, String entityType, String entity)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            String encodedEntity = Uri.EscapeDataString(entity);
            var url = new Uri(baseUrl, $"dataElementAccess/entity/user/{encodedUser}/entityType/{encodedEntityType}/entity/{encodedEntity}");

            return await SendGetRequestAsync<Boolean>(url);
        }

        protected async Task<List<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByUserBaseAsync(TUser user)
        {
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{Uri.EscapeDataString(userStringifier.ToString(user))}?includeIndirectMappings=true");
            var returnList = new List<Tuple<TComponent, TAccess>>();
            foreach (UserAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in await SendGetRequestAsync<List<UserAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnList.Add(new Tuple<TComponent, TAccess>(applicationComponentStringifier.FromString(currentMapping.ApplicationComponent), accessLevelStringifier.FromString(currentMapping.AccessLevel)));
            }

            return returnList;
        }

        protected async Task<List<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByGroupBaseAsync(TGroup group)
        {
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{Uri.EscapeDataString(groupStringifier.ToString(group))}?includeIndirectMappings=true");
            var returnList = new List<Tuple<TComponent, TAccess>>();
            foreach (GroupAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in await SendGetRequestAsync<List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnList.Add(new Tuple<TComponent, TAccess>(applicationComponentStringifier.FromString(currentMapping.ApplicationComponent), accessLevelStringifier.FromString(currentMapping.AccessLevel)));
            }

            return returnList;
        }

        protected async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserBaseAsync(TUser user)
        {
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{Uri.EscapeDataString(userStringifier.ToString(user))}?includeIndirectMappings=true");
            var returnList = new List<Tuple<String, String>>();
            foreach (UserAndEntity<String> currentMapping in await SendGetRequestAsync<List<UserAndEntity<String>>>(url))
            {
                returnList.Add(new Tuple<String, String>(currentMapping.EntityType, currentMapping.Entity));
            }

            return returnList;
        }

        protected async Task<List<String>> GetEntitiesAccessibleByUserBaseAsync(TUser user, String entityType)
        {
            String encodedUser = Uri.EscapeDataString(userStringifier.ToString(user));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{encodedUser}/entityType/{encodedEntityType}?includeIndirectMappings=true");
            var returnList = new List<String>();
            foreach (UserAndEntity<String> currentMapping in await SendGetRequestAsync<List<UserAndEntity<String>>>(url))
            {
                returnList.Add(currentMapping.Entity);
            }

            return returnList;
        }

        protected async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupBaseAsync(TGroup group)
        {
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{Uri.EscapeDataString(groupStringifier.ToString(group))}?includeIndirectMappings=true");
            var returnHashSet = new List<Tuple<String, String>>();
            foreach (GroupAndEntity<String> currentMapping in await SendGetRequestAsync<List<GroupAndEntity<String>>>(url))
            {
                returnHashSet.Add(new Tuple<String, String>(currentMapping.EntityType, currentMapping.Entity));
            }

            return returnHashSet;
        }

        protected async Task<List<String>> GetEntitiesAccessibleByGroupBaseAsync(TGroup group, String entityType)
        {
            String encodedGroup = Uri.EscapeDataString(groupStringifier.ToString(group));
            String encodedEntityType = Uri.EscapeDataString(entityType);
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{encodedGroup}/entityType/{encodedEntityType}?includeIndirectMappings=true");
            var returnList = new List<String>();
            foreach (GroupAndEntity<String> currentMapping in await SendGetRequestAsync<List<GroupAndEntity<String>>>(url))
            {
                returnList.Add(currentMapping.Entity);
            }

            return returnList;
        }

        #pragma warning restore 1591

        #endregion

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override void SetupExceptionHanderPolicies(Int32 retryCount, Int32 retryInterval, Action<Exception, TimeSpan, Int32, Context> onRetryAction)
        {
            exceptionHandingPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(retryCount, (Int32 currentRetryNumber) => { return TimeSpan.FromSeconds(retryInterval); }, onRetryAction);
        }

        /// <summary>
        /// Sends an HTTP GET request, expecting a 200 status returned to indicate success, and attempting to deserialize the response body to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response body to.</typeparam>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <returns>The response body deserialized to the specified type.</returns>
        protected async Task<T> SendGetRequestAsync<T>(Uri requestUrl)
        {
            T returnData = default(T);
            Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction = async (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (responseStatusCode != HttpStatusCode.OK)
                {
                    await HandleNonSuccessResponseStatusAsync(requestMethod, url, responseStatusCode, responseBody);
                }

                var jsonSerializer = new JsonSerializer();
                using (var streamReader = new StreamReader(responseBody, defaultEncoding, false, 1024, true))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    try
                    {
                        returnData = jsonSerializer.Deserialize<T>(jsonReader);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to call URL '{requestUrl.ToString()}' with '{HttpMethod.Get.ToString()}' method.  Error deserializing response body from JSON to type '{typeof(T).FullName}'.", e);
                    }
                }
            };
            await SendRequestAsync(HttpMethod.Get, requestUrl, responseAction);

            return returnData;
        }

        /// <summary>
        /// Sends an HTTP GET request, expecting a 200 status returned to indicate success, and attempting to deserialize the response body to the specified type.
        /// </summary>
        /// <typeparam name="TRequestBody">The type of data to be serialized to JSON and passed in the body of the request.</typeparam>
        /// <typeparam name="TReturn">The type to deserialize the response body to.</typeparam>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <param name="requestBody">The JSON body of the request.</param>
        /// <returns>The response body deserialized to the specified type.</returns>
        protected async Task<TReturn> SendGetRequestAsync<TRequestBody, TReturn>(Uri requestUrl, TRequestBody requestBody)
        {
            TReturn returnData = default(TReturn);
            using (JsonContent requestBodyAsJson = JsonContent.Create<TRequestBody>(requestBody, new MediaTypeHeaderValue(jsonMimeType)))
            {
                Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction = async (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
                {
                    if (responseStatusCode != HttpStatusCode.OK)
                    {
                        await HandleNonSuccessResponseStatusAsync(requestMethod, url, responseStatusCode, responseBody);
                    }

                    var jsonSerializer = new JsonSerializer();
                    using (var streamReader = new StreamReader(responseBody, defaultEncoding, false, 1024, true))
                    using (var jsonReader = new JsonTextReader(streamReader))
                    {
                        try
                        {
                            returnData = jsonSerializer.Deserialize<TReturn>(jsonReader);
                        }
                        catch (Exception e)
                        {
                            String stringifiedBody = await requestBodyAsJson.ReadAsStringAsync();
                            throw new Exception($"Failed to call URL '{requestUrl.ToString()}' with '{HttpMethod.Get.ToString()}' method, and request body '{stringifiedBody}'.  Error deserializing response body from JSON to type '{typeof(TReturn).FullName}'.", e);
                        }
                    }
                };
                await SendRequestAsync(HttpMethod.Get, requestUrl, requestBodyAsJson, responseAction);

                return returnData;
            }
        }

        /// <summary>
        /// Sends an HTTP GET request, expecting either a 200 or 404 status returned, and converting the status to an equivalent boolean value.
        /// </summary>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <returns>True in the case a 200 response status is received, or false in the case a 404 status is received.</returns>
        protected async Task<Boolean> SendGetRequestForContainsMethodAsync(Uri requestUrl)
        {
            Boolean returnValue = false;
            Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction = async (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (!((responseStatusCode == HttpStatusCode.OK) || (responseStatusCode == HttpStatusCode.NotFound)))
                {
                    await HandleNonSuccessResponseStatusAsync(requestMethod, url, responseStatusCode, responseBody);
                }
                if (responseStatusCode == HttpStatusCode.OK)
                {
                    returnValue = true;
                }
            };
            await SendRequestAsync(HttpMethod.Get, requestUrl, responseAction);

            return returnValue;
        }

        /// <summary>
        /// Sends an HTTP POST request, expecting a 201 status returned to indicate success.
        /// </summary>
        /// <param name="requestUrl">The URL of the request.</param>
        protected async Task SendPostRequestAsync(Uri requestUrl)
        {
            Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction = async (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (responseStatusCode != HttpStatusCode.Created)
                {
                    await HandleNonSuccessResponseStatusAsync(requestMethod, url, responseStatusCode, responseBody);
                }
            };
            await SendRequestAsync(HttpMethod.Post, requestUrl, responseAction);
        }

        /// <summary>
        /// Sends an HTTP DELETE request, expecting a 200 status returned to indicate success.
        /// </summary>
        /// <param name="requestUrl">The URL of the request.</param>
        protected async Task SendDeleteRequestAsync(Uri requestUrl)
        {
            Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction = async (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) => 
            {
                if (responseStatusCode != HttpStatusCode.OK)
                {
                    await HandleNonSuccessResponseStatusAsync(requestMethod, url, responseStatusCode, responseBody);
                }
            };
            await SendRequestAsync(HttpMethod.Delete, requestUrl, responseAction);
        }

        /// <summary>
        /// Sends an HTTP request.
        /// </summary>
        /// <param name="method">The HTTP method to use in the request.</param>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <param name="responseAction">An asyncronous Func to perform on receiving a response to the request.  Accepts 4 parameters: the HTTP method used in the request which generated the response, the URL of the request which generated the response, the HTTP status code received as part of the response, and a stream containing the response body.</param>
        /// <remarks>The Stream passed to the 'responseAction' parameter is closed/disposed when this method completes, so it should not be used outside of the context of the 'responseAction' parameter (e.g. set to a variable external to the 'responseAction' parameter).</remarks>
        protected async Task SendRequestAsync(HttpMethod method, Uri requestUrl, Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction)
        {
            Func<Task> httpClientAction = async () =>
            {
                using (var request = new HttpRequestMessage(method, requestUrl))
                using (var response = await httpClient.SendAsync(request))
                {
                    await responseAction.Invoke(method, requestUrl, response.StatusCode, await response.Content.ReadAsStreamAsync());
                }
            };

            await exceptionHandingPolicy.ExecuteAsync(httpClientAction);
        }

        /// <summary>
        /// Sends an HTTP request.
        /// </summary>
        /// <param name="method">The HTTP method to use in the request.</param>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <param name="requestBody">The JSON body of the request.</param>
        /// <param name="responseAction">An asyncronous Func to perform on receiving a response to the request.  Accepts 4 parameters: the HTTP method used in the request which generated the response, the URL of the request which generated the response, the HTTP status code received as part of the response, and a stream containing the response body.</param>
        /// <remarks>The Stream passed to the 'responseAction' parameter is closed/disposed when this method completes, so it should not be used outside of the context of the 'responseAction' parameter (e.g. set to a variable external to the 'responseAction' parameter).</remarks>
        protected async Task SendRequestAsync(HttpMethod method, Uri requestUrl, JsonContent requestBody, Func<HttpMethod, Uri, HttpStatusCode, Stream, Task> responseAction)
        {
            Func<Task> httpClientAction = async () =>
            {
                using (var request = new HttpRequestMessage(method, requestUrl))
                {
                    request.Content = requestBody;
                    using (var response = await httpClient.SendAsync(request))
                    {
                        await responseAction.Invoke(method, requestUrl, response.StatusCode, await response.Content.ReadAsStreamAsync());
                    }
                }
            };

            await exceptionHandingPolicy.ExecuteAsync(httpClientAction);
        }

        /// <summary>
        /// Handles receipt of a non-success HTTP response status, by converting the status and response body to an appropriate Exception and throwing that Exception.
        /// </summary>
        /// <param name="method">The HTTP method used in the request which generated the response.</param>
        /// <param name="requestUrl">The URL of the request which generated the response.</param>
        /// <param name="responseStatus">The received HTTP response status.</param>
        /// <param name="responseBody">The received response body.</param>
        protected async Task HandleNonSuccessResponseStatusAsync(HttpMethod method, Uri requestUrl, HttpStatusCode responseStatus, Stream responseBody)
        {
            String baseExceptionMessage = $"Failed to call URL '{requestUrl.ToString()}' with '{method.ToString()}' method.  Received non-succces HTTP response status '{(Int32)responseStatus}'";

            // Attempt to deserialize a HttpErrorResponse from the body
            responseBody.Position = 0;
            HttpErrorResponse httpErrorResponse = await DeserializeResponseBodyToHttpErrorResponseAsync(responseBody);
            if (httpErrorResponse != null)
            {
                if (statusCodeToExceptionThrowingActionMap.ContainsKey(responseStatus) == true)
                {
                    statusCodeToExceptionThrowingActionMap[responseStatus].Invoke(httpErrorResponse);
                }
                else
                {
                    String exceptionMessagePostfix = $", error code '{httpErrorResponse.Code}', and error message '{httpErrorResponse.Message}'.";
                    throw new Exception(baseExceptionMessage + exceptionMessagePostfix);
                }
            }
            else
            {
                // Read the response body as a string
                String responseBodyAsString = "";
                responseBody.Position = 0;
                using (var streamReader = new StreamReader(responseBody, defaultEncoding))
                {
                    responseBodyAsString = await streamReader.ReadToEndAsync();
                }
                if (String.IsNullOrEmpty(responseBodyAsString.Trim()) == false)
                {
                    throw new Exception(baseExceptionMessage + $" and response body '{responseBodyAsString}'.");
                }
                else
                {
                    throw new Exception(baseExceptionMessage + ".");
                }
            }
        }

        /// <summary>
        /// Attempts to deserialize the body of a HTTP response received as a <see cref="Stream"/> to an <see cref="HttpErrorResponse"/> instance.
        /// </summary>
        /// <param name="responseBody">The response body to deserialize.</param>
        /// <returns>The deserialized response body, or null if the reponse could not be deserialized (e.g. was empty, or did not contain JSON).</returns>
        protected async Task<HttpErrorResponse> DeserializeResponseBodyToHttpErrorResponseAsync(Stream responseBody)
        {
            using (var streamReader = new StreamReader(responseBody, defaultEncoding, false, 1024, true))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JObject bodyAsJson = null;
                try
                {
                    bodyAsJson = (JObject)await JToken.ReadFromAsync(jsonReader);

                }
                catch (Exception)
                {
                    return null;
                }
                try
                {
                    return errorResponseDeserializer.Deserialize(bodyAsJson);
                }
                catch (DeserializationException)
                {
                    return null;
                }
            }
        }

        #endregion
    }
}
