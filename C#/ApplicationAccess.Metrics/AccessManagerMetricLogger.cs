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
    /// Logs metric events for an implementation of IAccessManager.
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

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Users"]/*'/>
        public IEnumerable<TUser> Users
        {
            get
            {
                return queryProcessorMetricLogger.Users;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Groups"]/*'/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                return queryProcessorMetricLogger.Groups;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.EntityTypes"]/*'/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                return queryProcessorMetricLogger.EntityTypes;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsUser(`0)"]/*'/>
        public Boolean ContainsUser(TUser user)
        {
            return queryProcessorMetricLogger.ContainsUser(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsGroup(`1)"]/*'/>
        public Boolean ContainsGroup(TGroup group)
        {
            return queryProcessorMetricLogger.ContainsGroup(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToGroupMappings(`0)"]/*'/>
        public IEnumerable<TGroup> GetUserToGroupMappings(TUser user)
        {
            return queryProcessorMetricLogger.GetUserToGroupMappings(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToGroupMappings(`1)"]/*'/>
        public IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group)
        {
            return queryProcessorMetricLogger.GetGroupToGroupMappings(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToApplicationComponentAndAccessLevelMappings(`0)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return queryProcessorMetricLogger.GetUserToApplicationComponentAndAccessLevelMappings(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToApplicationComponentAndAccessLevelMappings(`1)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return queryProcessorMetricLogger.GetGroupToApplicationComponentAndAccessLevelMappings(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntityType(System.String)"]/*'/>
        public Boolean ContainsEntityType(String entityType)
        {
            return queryProcessorMetricLogger.ContainsEntityType(entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntities(System.String)"]/*'/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            return queryProcessorMetricLogger.GetEntities(entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntity(System.String,System.String)"]/*'/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            return queryProcessorMetricLogger.ContainsEntity(entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return queryProcessorMetricLogger.GetUserToEntityMappings(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0,System.String)"]/*'/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return queryProcessorMetricLogger.GetUserToEntityMappings(user, entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return queryProcessorMetricLogger.GetGroupToEntityMappings(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1,System.String)"]/*'/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return queryProcessorMetricLogger.GetGroupToEntityMappings(group, entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccess(`0,`2,`3)"]/*'/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return queryProcessorMetricLogger.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccess(`0,System.String,System.String)"]/*'/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            return queryProcessorMetricLogger.HasAccessToEntity(user, entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetAccessibleEntities(`0,System.String)"]/*'/>
        public HashSet<String> GetAccessibleEntities(TUser user, String entityType)
        {
            return queryProcessorMetricLogger.GetAccessibleEntities(user, entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public void AddUser(TUser user)
        {
            eventProcessorMetricLogger.AddUser(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public void RemoveUser(TUser user)
        {
            eventProcessorMetricLogger.RemoveUser(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public void AddGroup(TGroup group)
        {
            eventProcessorMetricLogger.AddGroup(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public void RemoveGroup(TGroup group)
        {
            eventProcessorMetricLogger.RemoveGroup(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            eventProcessorMetricLogger.AddUserToGroupMapping(user, group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            eventProcessorMetricLogger.RemoveUserToGroupMapping(user, group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventProcessorMetricLogger.AddGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventProcessorMetricLogger.RemoveGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventProcessorMetricLogger.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public void AddEntityType(String entityType)
        {
            eventProcessorMetricLogger.AddEntityType(entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public void RemoveEntityType(String entityType)
        {
            eventProcessorMetricLogger.RemoveEntityType(entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public void AddEntity(String entityType, String entity)
        {
            eventProcessorMetricLogger.AddEntity(entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public void RemoveEntity(String entityType, String entity)
        {
            eventProcessorMetricLogger.RemoveEntity(entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventProcessorMetricLogger.AddUserToEntityMapping(user, entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventProcessorMetricLogger.RemoveUserToEntityMapping(user, entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventProcessorMetricLogger.AddGroupToEntityMapping(group, entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventProcessorMetricLogger.RemoveGroupToEntityMapping(group, entityType, entity);
        }
    }
}
