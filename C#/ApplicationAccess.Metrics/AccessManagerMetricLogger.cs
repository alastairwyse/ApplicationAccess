/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationMetrics;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// Logs metric events for an implementation of <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManager implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManager implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManager implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IAccessManager implementation.</typeparam>
    /// <remarks>Uses a facade pattern to front the IAccessManager, capturing metrics and forwarding method calls to the IAccessManager.</remarks>
    public class AccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess> : IAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Logs metrics for queries to the IAccessManager implementation.</summary>
        protected AccessManagerQueryProcessorMetricLogger<TUser, TGroup, TComponent, TAccess> queryProcessorMetricLogger;
        /// <summary>Logs metrics for events in the IAccessManager implementation.</summary>
        protected AccessManagerEventProcessorMetricLogger<TUser, TGroup, TComponent, TAccess> eventProcessorMetricLogger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.AccessManagerMetricLogger class.
        /// </summary>
        /// <param name="eventProcessor">The IAccessManager implementation to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerMetricLogger(IAccessManager<TUser, TGroup, TComponent, TAccess> accessManager, IMetricLogger metricLogger)
        {
            this.metricLogger = metricLogger;
            queryProcessorMetricLogger = new AccessManagerQueryProcessorMetricLogger<TUser, TGroup, TComponent, TAccess>(accessManager, metricLogger);
            eventProcessorMetricLogger = new AccessManagerEventProcessorMetricLogger<TUser, TGroup, TComponent, TAccess>(accessManager, metricLogger);
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> Users
        {
            get
            {
                return queryProcessorMetricLogger.Users;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                return queryProcessorMetricLogger.Groups;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                return queryProcessorMetricLogger.EntityTypes;
            }
        }

        /// <inheritdoc/>
        public Boolean ContainsUser(TUser user)
        {
            return queryProcessorMetricLogger.ContainsUser(user);
        }

        /// <inheritdoc/>
        public Boolean ContainsGroup(TGroup group)
        {
            return queryProcessorMetricLogger.ContainsGroup(group);
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetUserToGroupMappings(TUser user)
        {
            return queryProcessorMetricLogger.GetUserToGroupMappings(user);
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group)
        {
            return queryProcessorMetricLogger.GetGroupToGroupMappings(group);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return queryProcessorMetricLogger.GetUserToApplicationComponentAndAccessLevelMappings(user);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return queryProcessorMetricLogger.GetGroupToApplicationComponentAndAccessLevelMappings(group);
        }

        /// <inheritdoc/>
        public Boolean ContainsEntityType(String entityType)
        {
            return queryProcessorMetricLogger.ContainsEntityType(entityType);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            return queryProcessorMetricLogger.GetEntities(entityType);
        }

        /// <inheritdoc/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            return queryProcessorMetricLogger.ContainsEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return queryProcessorMetricLogger.GetUserToEntityMappings(user);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return queryProcessorMetricLogger.GetUserToEntityMappings(user, entityType);
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return queryProcessorMetricLogger.GetGroupToEntityMappings(group);
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return queryProcessorMetricLogger.GetGroupToEntityMappings(group, entityType);
        }

        /// <inheritdoc/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return queryProcessorMetricLogger.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            return queryProcessorMetricLogger.HasAccessToEntity(user, entityType, entity);
        }


        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            return queryProcessorMetricLogger.GetApplicationComponentsAccessibleByUser(user);
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            return queryProcessorMetricLogger.GetApplicationComponentsAccessibleByGroup(group);
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            return queryProcessorMetricLogger.GetEntitiesAccessibleByUser(user, entityType);
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            return queryProcessorMetricLogger.GetEntitiesAccessibleByGroup(group, entityType);
        }

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            eventProcessorMetricLogger.AddUser(user);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            eventProcessorMetricLogger.RemoveUser(user);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            eventProcessorMetricLogger.AddGroup(group);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            eventProcessorMetricLogger.RemoveGroup(group);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            eventProcessorMetricLogger.AddUserToGroupMapping(user, group);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            eventProcessorMetricLogger.RemoveUserToGroupMapping(user, group);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventProcessorMetricLogger.AddGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventProcessorMetricLogger.RemoveGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            eventProcessorMetricLogger.AddEntityType(entityType);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            eventProcessorMetricLogger.RemoveEntityType(entityType);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            eventProcessorMetricLogger.AddEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            eventProcessorMetricLogger.RemoveEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventProcessorMetricLogger.AddUserToEntityMapping(user, entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventProcessorMetricLogger.RemoveUserToEntityMapping(user, entityType, entity);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventProcessorMetricLogger.AddGroupToEntityMapping(group, entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventProcessorMetricLogger.RemoveGroupToEntityMapping(group, entityType, entity);
        }
    }
}
