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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using ApplicationAccess.Utilities;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.DataTransferObjects;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Serialization;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

namespace ApplicationAccess.Hosting.Rest.Client
{
    /// <summary>
    /// Base class for client classes which interface to <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instances hosted as REST web APIs.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class AccessManagerClientBase<TUser, TGroup, TComponent, TAccess> : IDisposable
    {
        protected Encoding defaultEncoding = Encoding.UTF8;

        /// <summary>The client to use to connect.</summary>
        protected HttpClient httpClient;
        /// <summary>Exception handling policy for HttpClient calls.</summary>
        protected AsyncPolicy exceptionHandingPolicy;
        /// <summary>>The base URL for the hosted Web API.</summary>
        protected Uri baseUrl;
        /// <summary>Deserializer for HttpErrorResponse objects.</summary>
        protected HttpErrorResponseJsonSerializer errorResponseDeserializer;
        /// <summary>Maps a HTTP status code to an action which throws a matching Exception to the status code.  The action accepts 1 parameter: the exception message.</summary>
        protected Dictionary<HttpStatusCode, Action<String>> statusCodeToExceptionThrowingActionMap;
        /// <summary>A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Whether the HttpClient member was instantiated within the class constructor.</summary>
        protected Boolean httpClientInstantiatedInConstructor;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerClientBase
        (
            Uri baseUrl,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier, 
            Int32 retryCount, 
            Int32 retryInterval
        )
        {
            SetBaseConstructorParameters
            (
                baseUrl,
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier,
                retryCount,
                retryInterval
            );
            httpClient = new HttpClient();
            SetHttpClientAcceptHeader(httpClient);
            httpClientInstantiatedInConstructor = true;
            logger = new NullLogger();
            metricLogger = new NullMetricLogger();

        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerClientBase
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
            : this (baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerClientBase
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
        {
            SetBaseConstructorParameters
            (
                baseUrl,
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier,
                retryCount,
                retryInterval
            );
            this.httpClient = httpClient;
            SetHttpClientAcceptHeader(this.httpClient);
            httpClientInstantiatedInConstructor = false;
            logger = new NullLogger();
            metricLogger = new NullMetricLogger();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerClientBase
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
            : this(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
        /// <param name="exceptionHandingPolicy">Exception handling policy for HttpClient calls.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>When setting parameter 'exceptionHandingPolicy', note that the web API only returns non-success HTTP status errors in the case of persistent, and non-transient errors (e.g. 400 in the case of bad/malformed requests, and 500 in the case of critical server-side errors).  Retrying the same request after receiving these error statuses will result in an identical response, and hence these statuses should not be included as part of a transient exception handling policy.</remarks>
        public AccessManagerClientBase
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

        #pragma warning disable 0649

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
        /// <inheritdoc/>
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

        #pragma warning restore 0649

        #endregion

        #region Private/Protected Methods

        /// <summary>
        /// Performs setup for a minimal/common set of constructor parameters.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        protected void SetBaseConstructorParameters
        (
            Uri baseUrl,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval
        )
        {
            SetupExceptionHanderPoliciesFromConstructorParameters(retryCount, retryInterval);
            InitializeBaseUrl(baseUrl);
            errorResponseDeserializer = new HttpErrorResponseJsonSerializer();
            InitializeStatusCodeToExceptionThrowingActionMap();
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier; ;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            disposed = false;
        }

        /// <summary>
        /// Adds an appropriate path suffix to the specified 'baseUrl' constructor parameter.
        /// </summary>
        /// <param name="baseUrl">The base URL to initialize.</param>
        protected void InitializeBaseUrl(Uri baseUrl)
        {
            this.baseUrl = new Uri(baseUrl, "api/v1/");
        }

        /// <summary>
        /// Sets appropriate 'Accept' headers on the specified HTTP client.
        /// </summary>
        /// <param name="httpClient">The HTTP client to set the header(s) on.</param>
        protected void SetHttpClientAcceptHeader(HttpClient httpClient)
        {
            const String acceptHeaderName = "Accept";
            const String acceptHeaderValue = "application/json";
            if (httpClient.DefaultRequestHeaders.Contains(acceptHeaderName) == true)
            {
                httpClient.DefaultRequestHeaders.Remove(acceptHeaderName); 
            }
            httpClient.DefaultRequestHeaders.Add(acceptHeaderName, acceptHeaderValue);
        }

        /// <summary>
        /// Initializes the 'statusCodeToExceptionThrowingActionMap' member.
        /// </summary>
        protected void InitializeStatusCodeToExceptionThrowingActionMap()
        {
            statusCodeToExceptionThrowingActionMap = new Dictionary<HttpStatusCode, Action<String>>()
            {
                { 
                    HttpStatusCode.InternalServerError, 
                    (String exceptionMessage) => { throw new Exception(exceptionMessage); }
                },
                {
                    HttpStatusCode.BadRequest,
                    (String exceptionMessage) => { throw new ArgumentException(exceptionMessage); }
                }
            };
        }

        /// <summary>
        /// Sets up the 'exceptionHanderPolicy' members from the 'retryCount' and 'retryInterval' constructor parameters.
        /// </summary>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        protected void SetupExceptionHanderPoliciesFromConstructorParameters(Int32 retryCount, Int32 retryInterval)
        {
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be less than 0.");
            if (retryCount > 59)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be greater than 59.");
            if (retryInterval < 0)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be less than 0.");
            if (retryInterval > 120)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be greater than 120.");

            Action<Exception, TimeSpan, Int32, Context> onRetryAction = (Exception exception, TimeSpan actionRetryInterval, Int32 currentRetryCount, Context context) =>
            {
                logger.Log(this, LogLevel.Warning, $"Exception occurred when sending HTTP request.  Retrying in {retryInterval} seconds (retry {currentRetryCount} of {retryCount}).", exception);
                metricLogger.Increment(new HttpRequestRetried());
            };
            exceptionHandingPolicy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(retryCount, (Int32 currentRetryNumber) => { return TimeSpan.FromSeconds(retryInterval); }, onRetryAction);
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
        protected void SendRequest(HttpMethod method, Uri requestUrl, Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction)
        {
            Func<Task> httpClientAction = async () =>
            {
                using (var request = new HttpRequestMessage(method, requestUrl))
                using (var response = await httpClient.SendAsync(request))
                {
                    responseAction.Invoke(method, requestUrl, response.StatusCode, response.Content.ReadAsStreamAsync().Result);
                }
            };
            try
            {
                exceptionHandingPolicy.ExecuteAsync(httpClientAction).Wait();
            }
            catch (AggregateException ae)
            {
                ExceptionDispatchInfo.Capture(ae.GetBaseException()).Throw();
            }
        }

        /// <summary>
        /// Handles receipt of a non-success HTTP response status, by converting the status and response body to an appropriate Exception and throwing that Exception.
        /// </summary>
        /// <param name="method">The HTTP method used in the request which generated the response.</param>
        /// <param name="requestUrl">The URL of the request which generated the response.</param>
        /// <param name="responseStatus">The received HTTP response status.</param>
        /// <param name="responseBody">The received response body.</param>
        protected void HandleNonSuccessResponseStatus(HttpMethod method, Uri requestUrl, HttpStatusCode responseStatus, Stream responseBody)
        {
            String baseExceptionMessage = $"Failed to call URL '{requestUrl.ToString()}' with '{method.ToString()}' method.  Received non-succces HTTP response status '{(Int32)responseStatus}'";

            // Attempt to deserialize a HttpErrorResponse from the body
            responseBody.Position = 0;
            HttpErrorResponse httpErrorResponse = DeserializeResponseBodyToHttpErrorResponse(responseBody);
            if (httpErrorResponse != null)
            {
                if (statusCodeToExceptionThrowingActionMap.ContainsKey(responseStatus) == true)
                {
                    statusCodeToExceptionThrowingActionMap[responseStatus].Invoke(httpErrorResponse.Message);
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
                    responseBodyAsString = streamReader.ReadToEnd();
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
        protected HttpErrorResponse DeserializeResponseBodyToHttpErrorResponse(Stream responseBody)
        {
            using (var streamReader = new StreamReader(responseBody, defaultEncoding, false, 1024, true))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JObject bodyAsJson = null;
                try
                {
                    bodyAsJson = (JObject)JToken.ReadFrom(jsonReader);

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

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the ReaderNode.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~AccessManagerClientBase()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    if (httpClientInstantiatedInConstructor == true)
                    {
                        httpClient.Dispose();
                    }
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }
        #endregion
    }
}
