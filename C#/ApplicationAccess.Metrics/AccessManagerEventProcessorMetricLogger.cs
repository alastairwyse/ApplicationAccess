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
using ApplicationMetrics;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// Logs metric events for an implementation of IAccessManagerEventProcessor.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerEventProcessor implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerEventProcessor implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerEventProcessor implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IAccessManagerEventProcessor implementation.</typeparam>
    /// <remarks>Uses a facade pattern to front the IAccessManagerEventProcessor, capturing metrics and forwarding method calls to the IAccessManagerEventProcessor.</remarks>
    public class AccessManagerEventProcessorMetricLogger<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The IAccessManagerEventProcessor implementation to log metrics for.</summary>
        protected IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> eventProcessor;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.AccessManagerEventProcessorMetricLogger class.
        /// </summary>
        /// <param name="eventProcessor">The IAccessManagerEventProcessor implementation to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerEventProcessorMetricLogger(IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> eventProcessor, IMetricLogger metricLogger)
        {
            this.eventProcessor = eventProcessor;
            this.metricLogger = metricLogger;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public void AddUser(TUser user)
        {
            Guid beginId = metricLogger.Begin(new UserAddTime());
            try
            {
                eventProcessor.AddUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserAddTime());
            metricLogger.Increment(new UserAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public void RemoveUser(TUser user)
        {
            Guid beginId = metricLogger.Begin(new UserRemoveTime());
            try
            {
                eventProcessor.RemoveUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserRemoveTime());
            metricLogger.Increment(new UserRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public void AddGroup(TGroup group)
        {
            Guid beginId = metricLogger.Begin(new GroupAddTime());
            try
            {
                eventProcessor.AddGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupAddTime());
            metricLogger.Increment(new GroupAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public void RemoveGroup(TGroup group)
        {
            Guid beginId = metricLogger.Begin(new GroupRemoveTime());
            try
            {
                eventProcessor.RemoveGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupRemoveTime());
            metricLogger.Increment(new GroupRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Guid beginId = metricLogger.Begin(new UserToGroupMappingAddTime());
            try
            {
                eventProcessor.AddUserToGroupMapping(user, group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToGroupMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToGroupMappingAddTime());
            metricLogger.Increment(new UserToGroupMappingAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Guid beginId = metricLogger.Begin(new UserToGroupMappingRemoveTime());
            try
            {
                eventProcessor.RemoveUserToGroupMapping(user, group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToGroupMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToGroupMappingRemoveTime());
            metricLogger.Increment(new UserToGroupMappingRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid beginId = metricLogger.Begin(new GroupToGroupMappingAddTime());
            try
            {
                eventProcessor.AddGroupToGroupMapping(fromGroup, toGroup);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToGroupMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToGroupMappingAddTime());
            metricLogger.Increment(new GroupToGroupMappingAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid beginId = metricLogger.Begin(new GroupToGroupMappingRemoveTime());
            try
            {
                eventProcessor.RemoveGroupToGroupMapping(fromGroup, toGroup);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToGroupMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToGroupMappingRemoveTime());
            metricLogger.Increment(new GroupToGroupMappingRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingAddTime());
            try
            {
                eventProcessor.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
            metricLogger.Increment(new UserToApplicationComponentAndAccessLevelMappingAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
            try
            {
                eventProcessor.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
            metricLogger.Increment(new UserToApplicationComponentAndAccessLevelMappingRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
            try
            {
                eventProcessor.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
            metricLogger.Increment(new GroupToApplicationComponentAndAccessLevelMappingAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
            try
            {
                eventProcessor.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
            metricLogger.Increment(new GroupToApplicationComponentAndAccessLevelMappingRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public void AddEntityType(String entityType)
        {
            Guid beginId = metricLogger.Begin(new EntityTypeAddTime());
            try
            {
                eventProcessor.AddEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityTypeAddTime());
                throw;
            }
            metricLogger.End(beginId, new EntityTypeAddTime());
            metricLogger.Increment(new EntityTypeAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public void RemoveEntityType(String entityType)
        {
            Guid beginId = metricLogger.Begin(new EntityTypeRemoveTime());
            try
            {
                eventProcessor.RemoveEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityTypeRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new EntityTypeRemoveTime());
            metricLogger.Increment(new EntityTypeRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public void AddEntity(String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new EntityAddTime());
            try
            {
                eventProcessor.AddEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityAddTime());
                throw;
            }
            metricLogger.End(beginId, new EntityAddTime());
            metricLogger.Increment(new EntityAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public void RemoveEntity(String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new EntityRemoveTime());
            try
            {
                eventProcessor.RemoveEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new EntityRemoveTime());
            metricLogger.Increment(new EntityRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new UserToEntityMappingAddTime());
            try
            {
                eventProcessor.AddUserToEntityMapping(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToEntityMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToEntityMappingAddTime());
            metricLogger.Increment(new UserToEntityMappingAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new UserToEntityMappingRemoveTime());
            try
            {
                eventProcessor.RemoveUserToEntityMapping(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToEntityMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToEntityMappingRemoveTime());
            metricLogger.Increment(new UserToEntityMappingRemoved());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new GroupToEntityMappingAddTime());
            try
            {
                eventProcessor.AddGroupToEntityMapping(group, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToEntityMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToEntityMappingAddTime());
            metricLogger.Increment(new GroupToEntityMappingAdded());
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new GroupToEntityMappingRemoveTime());
            try
            {
                eventProcessor.RemoveGroupToEntityMapping(group, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToEntityMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToEntityMappingRemoveTime());
            metricLogger.Increment(new GroupToEntityMappingRemoved());
        }
    }
}
