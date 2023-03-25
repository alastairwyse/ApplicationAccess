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
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Hosting.Rest.AsyncClient;
using ApplicationLogging;
using ApplicationMetrics;
using Newtonsoft.Json;
using Polly;

namespace ApplicationAccess.Hosting.Rest.Client
{
    /// <summary>
    /// Base class for client classes which syncronously interface to <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instances hosted as REST web APIs.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class AccessManagerSyncClientBase<TUser, TGroup, TComponent, TAccess> : AccessManagerClientBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Exception handling policy for HttpClient calls.</summary>
        protected Policy exceptionHandingPolicy;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerSyncClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerSyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerSyncClientBase class.
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
        public AccessManagerSyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerSyncClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerSyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerSyncClientBase class.
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
        public AccessManagerSyncClientBase
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerSyncClientBase class.
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
        public AccessManagerSyncClientBase
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

        #region IAccessManager Methods

        #pragma warning disable 1591

        protected IEnumerable<TUser> UsersBase
        {
            get
            {
                var url = new Uri(baseUrl, "users");

                foreach (String currentUserAsString in SendGetRequest<List<String>>(url))
                {
                    yield return userStringifier.FromString(currentUserAsString);
                }
            }
        }

        protected IEnumerable<TGroup> GroupsBase
        {
            get
            {
                var url = new Uri(baseUrl, "groups");

                foreach (String currentGroupAsString in SendGetRequest<List<String>>(url))
                {
                    yield return groupStringifier.FromString(currentGroupAsString);
                }
            }
        }

        protected IEnumerable<String> EntityTypesBase
        {
            get
            {
                var url = new Uri(baseUrl, "entityTypes");

                return SendGetRequest<List<String>>(url);
            }
        }

        protected void AddUserBase(TUser user)
        {
            var url = new Uri(baseUrl, $"users/{userStringifier.ToString(user)}");
            SendPostRequest(url);
        }

        protected Boolean ContainsUserBase(TUser user)
        {
            var url = new Uri(baseUrl, $"users/{userStringifier.ToString(user)}");

            return SendGetRequestForContainsMethod(url);
        }

        protected void RemoveUserBase(TUser user)
        {
            var url = new Uri(baseUrl, $"users/{userStringifier.ToString(user)}");
            SendDeleteRequest(url);
        }

        protected void AddGroupBase(TGroup group)
        {
            var url = new Uri(baseUrl, $"groups/{groupStringifier.ToString(group)}");
            SendPostRequest(url);
        }

        protected Boolean ContainsGroupBase(TGroup group)
        {
            var url = new Uri(baseUrl, $"groups/{groupStringifier.ToString(group)}");

            return SendGetRequestForContainsMethod(url);
        }

        protected void RemoveGroupBase(TGroup group)
        {
            var url = new Uri(baseUrl, $"groups/{groupStringifier.ToString(group)}");
            SendDeleteRequest(url);
        }

        protected void AddUserToGroupMappingBase(TUser user, TGroup group)
        {
            var url = new Uri(baseUrl, $"userToGroupMappings/user/{userStringifier.ToString(user)}/group/{groupStringifier.ToString(group)}");
            SendPostRequest(url);
        }

        protected HashSet<TGroup> GetUserToGroupMappingsBase(TUser user, Boolean includeIndirectMappings)
        {
            var url = new Uri(baseUrl, $"userToGroupMappings/user/{userStringifier.ToString(user)}?includeIndirectMappings={includeIndirectMappings}");
            var returnHashSet = new HashSet<TGroup>();
            foreach (UserAndGroup<String, String> currentUserAndGroup in SendGetRequest<List<UserAndGroup<String, String>>>(url))
            {
                returnHashSet.Add(groupStringifier.FromString(currentUserAndGroup.Group));
            }

            return returnHashSet;
        }

        protected void RemoveUserToGroupMappingBase(TUser user, TGroup group)
        {
            var url = new Uri(baseUrl, $"userToGroupMappings/user/{userStringifier.ToString(user)}/group/{groupStringifier.ToString(group)}");
            SendDeleteRequest(url);
        }

        protected void AddGroupToGroupMappingBase(TGroup fromGroup, TGroup toGroup)
        {
            var url = new Uri(baseUrl, $"groupToGroupMappings/fromGroup/{groupStringifier.ToString(fromGroup)}/toGroup/{groupStringifier.ToString(toGroup)}");
            SendPostRequest(url);
        }

        protected HashSet<TGroup> GetGroupToGroupMappingsBase(TGroup group, Boolean includeIndirectMappings)
        {
            var url = new Uri(baseUrl, $"groupToGroupMappings/group/{groupStringifier.ToString(group)}?includeIndirectMappings={includeIndirectMappings}");
            var returnHashSet = new HashSet<TGroup>();
            foreach (FromGroupAndToGroup<String> currentFromGroupAndToGroup in SendGetRequest<List<FromGroupAndToGroup<String>>>(url))
            {
                returnHashSet.Add(groupStringifier.FromString(currentFromGroupAndToGroup.ToGroup));
            }

            return returnHashSet;
        }

        protected void RemoveGroupToGroupMappingBase(TGroup fromGroup, TGroup toGroup)
        {
            var url = new Uri(baseUrl, $"groupToGroupMappings/fromGroup/{groupStringifier.ToString(fromGroup)}/toGroup/{groupStringifier.ToString(toGroup)}");
            SendDeleteRequest(url);
        }

        protected void AddUserToApplicationComponentAndAccessLevelMappingBase(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{userStringifier.ToString(user)}/applicationComponent/{applicationComponentStringifier.ToString(applicationComponent)}/accessLevel/{accessLevelStringifier.ToString(accessLevel)}");
            SendPostRequest(url);
        }

        protected IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappingsBase(TUser user)
        {
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{userStringifier.ToString(user)}?includeIndirectMappings=false");

            foreach (UserAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in SendGetRequest<List<UserAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                yield return new Tuple<TComponent, TAccess>
                (
                    applicationComponentStringifier.FromString(currentMapping.ApplicationComponent),
                    accessLevelStringifier.FromString(currentMapping.AccessLevel)
                );
            }
        }

        protected void RemoveUserToApplicationComponentAndAccessLevelMappingBase(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{userStringifier.ToString(user)}/applicationComponent/{applicationComponentStringifier.ToString(applicationComponent)}/accessLevel/{accessLevelStringifier.ToString(accessLevel)}");
            SendDeleteRequest(url);
        }

        protected void AddGroupToApplicationComponentAndAccessLevelMappingBase(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{groupStringifier.ToString(group)}/applicationComponent/{applicationComponentStringifier.ToString(applicationComponent)}/accessLevel/{accessLevelStringifier.ToString(accessLevel)}");
            SendPostRequest(url);
        }

        protected IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappingsBase(TGroup group)
        {
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{groupStringifier.ToString(group)}?includeIndirectMappings=false");

            foreach (GroupAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in SendGetRequest<List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                yield return new Tuple<TComponent, TAccess>
                (
                    applicationComponentStringifier.FromString(currentMapping.ApplicationComponent),
                    accessLevelStringifier.FromString(currentMapping.AccessLevel)
                );
            }
        }

        protected void RemoveGroupToApplicationComponentAndAccessLevelMappingBase(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{groupStringifier.ToString(group)}/applicationComponent/{applicationComponentStringifier.ToString(applicationComponent)}/accessLevel/{accessLevelStringifier.ToString(accessLevel)}");
            SendDeleteRequest(url);
        }

        protected void AddEntityTypeBase(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{entityType}");
            SendPostRequest(url);
        }

        protected Boolean ContainsEntityTypeBase(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{entityType}");

            return SendGetRequestForContainsMethod(url);
        }

        protected void RemoveEntityTypeBase(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{entityType}");
            SendDeleteRequest(url);
        }

        protected void AddEntityBase(String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"entityTypes/{entityType}/entities/{entity}");
            SendPostRequest(url);
        }

        protected IEnumerable<String> GetEntitiesBase(String entityType)
        {
            var url = new Uri(baseUrl, $"entityTypes/{entityType}/entities");

            foreach (EntityTypeAndEntity currentEntityTypeAndEntity in SendGetRequest<List<EntityTypeAndEntity>>(url))
            {
                yield return currentEntityTypeAndEntity.Entity;
            }
        }

        protected Boolean ContainsEntityBase(String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"entityTypes/{entityType}/entities/{entity}");

            return SendGetRequestForContainsMethod(url);
        }

        protected void RemoveEntityBase(String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"entityTypes/{entityType}/entities/{entity}");
            SendDeleteRequest(url);
        }

        protected void AddUserToEntityMappingBase(TUser user, String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{userStringifier.ToString(user)}/entityType/{entityType}/entity/{entity}");
            SendPostRequest(url);
        }

        protected IEnumerable<Tuple<String, String>> GetUserToEntityMappingsBase(TUser user)
        {
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{userStringifier.ToString(user)}?includeIndirectMappings=false");

            foreach (UserAndEntity<String> currentMapping in SendGetRequest<List<UserAndEntity<String>>>(url))
            {
                yield return new Tuple<String, String>
                (
                    currentMapping.EntityType,
                    currentMapping.Entity
                );
            }
        }

        protected IEnumerable<String> GetUserToEntityMappingsBase(TUser user, String entityType)
        {
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{userStringifier.ToString(user)}/entityType/{entityType}?includeIndirectMappings=false");

            foreach (UserAndEntity<String> currentMapping in SendGetRequest<List<UserAndEntity<String>>>(url))
            {
                yield return currentMapping.Entity;
            }
        }

        protected void RemoveUserToEntityMappingBase(TUser user, String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{userStringifier.ToString(user)}/entityType/{entityType}/entity/{entity}");
            SendDeleteRequest(url);
        }

        protected void AddGroupToEntityMappingBase(TGroup group, String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{groupStringifier.ToString(group)}/entityType/{entityType}/entity/{entity}");
            SendPostRequest(url);
        }

        protected IEnumerable<Tuple<String, String>> GetGroupToEntityMappingsBase(TGroup group)
        {
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{groupStringifier.ToString(group)}?includeIndirectMappings=false");

            foreach (GroupAndEntity<String> currentMapping in SendGetRequest<List<GroupAndEntity<String>>>(url))
            {
                yield return new Tuple<String, String>
                (
                    currentMapping.EntityType,
                    currentMapping.Entity
                );
            }
        }

        protected IEnumerable<String> GetGroupToEntityMappingsBase(TGroup group, String entityType)
        {
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{groupStringifier.ToString(group)}/entityType/{entityType}?includeIndirectMappings=false");

            foreach (GroupAndEntity<String> currentMapping in SendGetRequest<List<GroupAndEntity<String>>>(url))
            {
                yield return currentMapping.Entity;
            }
        }

        protected void RemoveGroupToEntityMappingBase(TGroup group, String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{groupStringifier.ToString(group)}/entityType/{entityType}/entity/{entity}");
            SendDeleteRequest(url);
        }

        protected Boolean HasAccessToApplicationComponentBase(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            var url = new Uri(baseUrl, $"dataElementAccess/applicationComponent/user/{userStringifier.ToString(user)}/applicationComponent/{applicationComponentStringifier.ToString(applicationComponent)}/accessLevel/{accessLevelStringifier.ToString(accessLevel)}");

            return SendGetRequest<Boolean>(url);
        }

        protected Boolean HasAccessToEntityBase(TUser user, String entityType, String entity)
        {
            var url = new Uri(baseUrl, $"dataElementAccess/entity/user/{userStringifier.ToString(user)}/entityType/{entityType}/entity/{entity}");

            return SendGetRequest<Boolean>(url);
        }

        protected HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUserBase(TUser user)
        {
            var url = new Uri(baseUrl, $"userToApplicationComponentAndAccessLevelMappings/user/{userStringifier.ToString(user)}?includeIndirectMappings=true");
            var returnHashSet = new HashSet<Tuple<TComponent, TAccess>>();
            foreach (UserAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in SendGetRequest<List<UserAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnHashSet.Add(new Tuple<TComponent, TAccess>(applicationComponentStringifier.FromString(currentMapping.ApplicationComponent), accessLevelStringifier.FromString(currentMapping.AccessLevel)));
            }

            return returnHashSet;
        }

        protected HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroupBase(TGroup group)
        {
            var url = new Uri(baseUrl, $"groupToApplicationComponentAndAccessLevelMappings/group/{groupStringifier.ToString(group)}?includeIndirectMappings=true");
            var returnHashSet = new HashSet<Tuple<TComponent, TAccess>>();
            foreach (GroupAndApplicationComponentAndAccessLevel<String, String, String> currentMapping in SendGetRequest<List<GroupAndApplicationComponentAndAccessLevel<String, String, String>>>(url))
            {
                returnHashSet.Add(new Tuple<TComponent, TAccess>(applicationComponentStringifier.FromString(currentMapping.ApplicationComponent), accessLevelStringifier.FromString(currentMapping.AccessLevel)));
            }

            return returnHashSet;
        }

        protected HashSet<Tuple<String, String>> GetEntitiesAccessibleByUserBase(TUser user)
        {
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{userStringifier.ToString(user)}?includeIndirectMappings=true");
            var returnHashSet = new HashSet<Tuple<String, String>>();
            foreach (UserAndEntity<String> currentMapping in SendGetRequest<List<UserAndEntity<String>>>(url))
            {
                returnHashSet.Add(new Tuple<String, String>(currentMapping.EntityType, currentMapping.Entity));
            }

            return returnHashSet;
        }

        protected HashSet<String> GetEntitiesAccessibleByUserBase(TUser user, String entityType)
        {
            var url = new Uri(baseUrl, $"userToEntityMappings/user/{userStringifier.ToString(user)}/entityType/{entityType}?includeIndirectMappings=true");
            var returnHashSet = new HashSet<String>();
            foreach (UserAndEntity<String> currentMapping in SendGetRequest<List<UserAndEntity<String>>>(url))
            {
                returnHashSet.Add(currentMapping.Entity);
            }

            return returnHashSet;
        }

        protected HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroupBase(TGroup group)
        {
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{groupStringifier.ToString(group)}?includeIndirectMappings=true");
            var returnHashSet = new HashSet<Tuple<String, String>>();
            foreach (GroupAndEntity<String> currentMapping in SendGetRequest<List<GroupAndEntity<String>>>(url))
            {
                returnHashSet.Add(new Tuple<String, String>(currentMapping.EntityType, currentMapping.Entity));
            }

            return returnHashSet;
        }

        protected HashSet<String> GetEntitiesAccessibleByGroupBase(TGroup group, String entityType)
        {
            var url = new Uri(baseUrl, $"groupToEntityMappings/group/{groupStringifier.ToString(group)}/entityType/{entityType}?includeIndirectMappings=true");
            var returnHashSet = new HashSet<String>();
            foreach (GroupAndEntity<String> currentMapping in SendGetRequest<List<GroupAndEntity<String>>>(url))
            {
                returnHashSet.Add(currentMapping.Entity);
            }

            return returnHashSet;
        }

        #pragma warning restore 1591

        #endregion

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override void SetupExceptionHanderPolicies(Int32 retryCount, Int32 retryInterval, Action<Exception, TimeSpan, Int32, Context> onRetryAction)
        {
            exceptionHandingPolicy = Policy.Handle<HttpRequestException>().WaitAndRetry(retryCount, (Int32 currentRetryNumber) => { return TimeSpan.FromSeconds(retryInterval); }, onRetryAction);
        }

        /// <summary>
        /// Sends an HTTP GET request, expecting a 200 status returned to indicate success, and attempting to deserialize the response body to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response body to.</typeparam>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <returns>The response body deserialized to the specified type.</returns>
        protected T SendGetRequest<T>(Uri requestUrl)
        {
            T returnData = default(T);
            Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction = (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (responseStatusCode != HttpStatusCode.OK)
                {
                    HandleNonSuccessResponseStatus(requestMethod, url, responseStatusCode, responseBody);
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
            SendRequest(HttpMethod.Get, requestUrl, responseAction);

            return returnData;
        }

        /// <summary>
        /// Sends an HTTP GET request, expecting either a 200 or 404 status returned, and converting the status to an equivalent boolean value.
        /// </summary>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <returns>True in the case a 200 response status is received, or false in the case a 404 status is received.</returns>
        protected Boolean SendGetRequestForContainsMethod(Uri requestUrl)
        {
            Boolean returnValue = false;
            Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction = (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (!((responseStatusCode == HttpStatusCode.OK) || (responseStatusCode == HttpStatusCode.NotFound)))
                {
                    HandleNonSuccessResponseStatus(requestMethod, url, responseStatusCode, responseBody);
                }
                if (responseStatusCode == HttpStatusCode.OK)
                {
                    returnValue = true;
                }
            };
            SendRequest(HttpMethod.Get, requestUrl, responseAction);

            return returnValue;
        }

        /// <summary>
        /// Sends an HTTP POST request, expecting a 201 status returned to indicate success.
        /// </summary>
        /// <param name="requestUrl">The URL of the request.</param>
        protected void SendPostRequest(Uri requestUrl)
        {
            Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction = (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (responseStatusCode != HttpStatusCode.Created)
                {
                    HandleNonSuccessResponseStatus(requestMethod, url, responseStatusCode, responseBody);
                }
            };
            SendRequest(HttpMethod.Post, requestUrl, responseAction);
        }

        /// <summary>
        /// Sends an HTTP DELETE request, expecting a 200 status returned to indicate success.
        /// </summary>
        /// <param name="requestUrl">The URL of the request.</param>
        protected void SendDeleteRequest(Uri requestUrl)
        {
            Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction = (HttpMethod requestMethod, Uri url, HttpStatusCode responseStatusCode, Stream responseBody) =>
            {
                if (responseStatusCode != HttpStatusCode.OK)
                {
                    HandleNonSuccessResponseStatus(requestMethod, url, responseStatusCode, responseBody);
                }
            };
            SendRequest(HttpMethod.Delete, requestUrl, responseAction);
        }

        /// <summary>
        /// Sends an HTTP request.
        /// </summary>
        /// <param name="method">The HTTP method to use in the request.</param>
        /// <param name="requestUrl">The URL of the request.</param>
        /// <param name="responseAction">An action to perform on receiving a response to the request.  Accepts 4 parameters: the HTTP method used in the request which generated the response, the URL of the request which generated the response, the HTTP status code received as part of the response, and a stream containing the response body.</param>
        /// <remarks>The Stream passed to the 'responseAction' parameter is closed/disposed when this method completes, so it should not be used outside of the context of the 'responseAction' parameter (e.g. set to a variable external to the 'responseAction' parameter).</remarks>
        protected virtual void SendRequest(HttpMethod method, Uri requestUrl, Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction)
        {
            Action httpClientAction = () =>
            {
                using (var request = new HttpRequestMessage(method, requestUrl))
                using (var response = httpClient.Send(request))
                {
                    responseAction.Invoke(method, requestUrl, response.StatusCode, response.Content.ReadAsStream());
                }
            };

            exceptionHandingPolicy.Execute(httpClientAction);
        }

        #endregion
    }
}