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
using ApplicationLogging;
using ApplicationMetrics;
using Polly;

namespace ApplicationAccess.Hosting.Rest.Client
{
    /// <summary>
    /// Client class which syncronously interfaces to an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instance hosted as a REST web API.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>This class is thread safe and can be used by multiple threads concurrently.  However, clients needs to ensure that custom implementations of <see cref="IUniqueStringifier{T}"/> passed to the constructor are also thread safe.</remarks>
    public class AccessManagerClient<TUser, TGroup, TComponent, TAccess> : AccessManagerSyncClientBase<TUser, TGroup, TComponent, TAccess>, IAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
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
        public AccessManagerClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
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
        public AccessManagerClient
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
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
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
        /// <remarks>When setting parameter 'exceptionHandingPolicy', note that the web API only returns non-success HTTP status errors in the case of persistent, and non-transient errors (e.g. 400 in the case of bad/malformed requests, and 500 in the case of critical server-side errors).  Retrying the same request after receiving these error statuses will result in an identical response, and hence these statuses should not be included as part of a transient exception handling policy.</remarks>
        public AccessManagerClient
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
            : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, exceptionHandingPolicy, logger, metricLogger)
        {
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> Users
        {
            get
            {
                return base.UsersBase;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                return base.GroupsBase;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                return base.EntityTypesBase; ;
            }
        }

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            base.AddUserBase(user);
        }

        /// <inheritdoc/>
        public Boolean ContainsUser(TUser user)
        {
            return base.ContainsUserBase(user);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            base.RemoveUserBase(user);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            base.AddGroupBase(group);
        }

        /// <inheritdoc/>
        public Boolean ContainsGroup(TGroup group)
        {
            return base.ContainsGroupBase(group);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            base.RemoveGroupBase(group);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            base.AddUserToGroupMappingBase(user, group);
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            return base.GetUserToGroupMappingsBase(user, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return base.GetGroupToUserMappingsBase(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            base.RemoveUserToGroupMappingBase(user, group);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            base.AddGroupToGroupMappingBase(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return base.GetGroupToGroupMappingsBase(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return base.GetGroupToGroupReverseMappingsBase(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            base.RemoveGroupToGroupMappingBase(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            base.AddUserToApplicationComponentAndAccessLevelMappingBase(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return base.GetUserToApplicationComponentAndAccessLevelMappingsBase(user);
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return base.GetApplicationComponentAndAccessLevelToUserMappingsBase(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            base.RemoveUserToApplicationComponentAndAccessLevelMappingBase(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            base.AddGroupToApplicationComponentAndAccessLevelMappingBase(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return base.GetGroupToApplicationComponentAndAccessLevelMappingsBase(group);
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return base.GetApplicationComponentAndAccessLevelToGroupMappingsBase(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            base.RemoveGroupToApplicationComponentAndAccessLevelMappingBase(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            base.AddEntityTypeBase(entityType);
        }

        /// <inheritdoc/>
        public Boolean ContainsEntityType(String entityType)
        {
            return base.ContainsEntityTypeBase(entityType);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            base.RemoveEntityTypeBase(entityType);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            base.AddEntityBase(entityType, entity);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            return base.GetEntitiesBase(entityType);
        }

        /// <inheritdoc/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            return base.ContainsEntityBase(entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            base.RemoveEntityBase(entityType, entity);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            base.AddUserToEntityMappingBase(user, entityType, entity);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return base.GetUserToEntityMappingsBase(user);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return base.GetUserToEntityMappingsBase(user, entityType);
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return base.GetEntityToUserMappingsBase(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            base.RemoveUserToEntityMappingBase(user, entityType, entity);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            base.AddGroupToEntityMappingBase(group, entityType, entity);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return base.GetGroupToEntityMappingsBase(group);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return base.GetGroupToEntityMappingsBase(group, entityType);
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetEntityToGroupMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return base.GetEntityToGroupMappingsBase(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            base.RemoveGroupToEntityMappingBase(group, entityType, entity);
        }

        /// <inheritdoc/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return base.HasAccessToApplicationComponentBase(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            return base.HasAccessToEntityBase(user, entityType, entity);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            return base.GetApplicationComponentsAccessibleByUserBase(user);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            return base.GetApplicationComponentsAccessibleByGroupBase(group);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            return base.GetEntitiesAccessibleByUserBase(user);
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            return base.GetEntitiesAccessibleByUserBase(user, entityType);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            return base.GetEntitiesAccessibleByGroupBase(group);
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            return base.GetEntitiesAccessibleByGroupBase(group, entityType);
        }
    }
}
