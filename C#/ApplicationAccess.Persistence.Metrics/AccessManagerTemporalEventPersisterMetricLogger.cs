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
using ApplicationAccess.Persistence;
using ApplicationAccess.Metrics;
using ApplicationMetrics;


namespace ApplicationAccess.Persistence.Metrics
{
    /// <summary>
    /// Logs metric events for an implementation of <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerTemporalEventPersister implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerTemporalEventPersister implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerEventProcessor implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IAccessManagerTemporalEventPersister implementation.</typeparam>
    /// <remarks>Uses a facade pattern to front the IAccessManagerTemporalEventPersister, capturing metrics and forwarding method calls to the IAccessManagerTemporalEventPersister.</remarks>
    public class AccessManagerTemporalEventPersisterMetricLogger<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The IAccessManagerTemporalEventPersister implementation to log metrics for.</summary>
        protected AccessManagerTemporalEventPersisterMetricLogger<TUser, TGroup, TComponent, TAccess> persister;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Metrics.AccessManagerTemporalEventPersisterMetricLogger class.
        /// </summary>
        /// <param name="persister">The IAccessManagerTemporalEventPersister implementation to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalEventPersisterMetricLogger(AccessManagerTemporalEventPersisterMetricLogger<TUser, TGroup, TComponent, TAccess> persister, IMetricLogger metricLogger)
        {
            this.persister = persister;
            this.metricLogger = metricLogger;
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public void AddUser(TUser user)
        {
            Guid beginId = metricLogger.Begin(new UserAddTime());
            try
            {
                persister.AddUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public void RemoveUser(TUser user)
        {
            Guid beginId = metricLogger.Begin(new UserRemoveTime());
            try
            {
                persister.RemoveUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public void AddGroup(TGroup group)
        {
            Guid beginId = metricLogger.Begin(new GroupAddTime());
            try
            {
                persister.AddGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public void RemoveGroup(TGroup group)
        {
            Guid beginId = metricLogger.Begin(new GroupRemoveTime());
            try
            {
                persister.RemoveGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Guid beginId = metricLogger.Begin(new UserToGroupMappingAddTime());
            try
            {
                persister.AddUserToGroupMapping(user, group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToGroupMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToGroupMappingAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Guid beginId = metricLogger.Begin(new UserToGroupMappingRemoveTime());
            try
            {
                persister.RemoveUserToGroupMapping(user, group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToGroupMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToGroupMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid beginId = metricLogger.Begin(new GroupToGroupMappingAddTime());
            try
            {
                persister.AddGroupToGroupMapping(fromGroup, toGroup);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToGroupMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToGroupMappingAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Guid beginId = metricLogger.Begin(new GroupToGroupMappingRemoveTime());
            try
            {
                persister.RemoveGroupToGroupMapping(fromGroup, toGroup);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToGroupMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToGroupMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingAddTime());
            try
            {
                persister.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
            try
            {
                persister.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
            try
            {
                persister.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Guid beginId = metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
            try
            {
                persister.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public void AddEntityType(String entityType)
        {
            Guid beginId = metricLogger.Begin(new EntityTypeAddTime());
            try
            {
                persister.AddEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityTypeAddTime());
                throw;
            }
            metricLogger.End(beginId, new EntityTypeAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public void RemoveEntityType(String entityType)
        {
            Guid beginId = metricLogger.Begin(new EntityTypeRemoveTime());
            try
            {
                persister.RemoveEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityTypeRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new EntityTypeRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public void AddEntity(String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new EntityAddTime());
            try
            {
                persister.AddEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityAddTime());
                throw;
            }
            metricLogger.End(beginId, new EntityAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public void RemoveEntity(String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new EntityRemoveTime());
            try
            {
                persister.RemoveEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new EntityRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new UserToEntityMappingAddTime());
            try
            {
                persister.AddUserToEntityMapping(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToEntityMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToEntityMappingAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new UserToEntityMappingRemoveTime());
            try
            {
                persister.RemoveUserToEntityMapping(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToEntityMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToEntityMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new GroupToEntityMappingAddTime());
            try
            {
                persister.AddGroupToEntityMapping(group, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToEntityMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToEntityMappingAddTime());
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new GroupToEntityMappingRemoveTime());
            try
            {
                persister.RemoveGroupToEntityMapping(group, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToEntityMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToEntityMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserAddTime());
            try
            {
                persister.AddUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserRemoveTime());
            try
            {
                persister.RemoveUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupAddTime());
            try
            {
                persister.AddGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupRemoveTime());
            try
            {
                persister.RemoveGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserToGroupMappingAddTime());
            try
            {
                persister.AddUserToGroupMapping(user, group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToGroupMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToGroupMappingAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserToGroupMappingRemoveTime());
            try
            {
                persister.RemoveUserToGroupMapping(user, group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToGroupMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToGroupMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupToGroupMappingAddTime());
            try
            {
                persister.AddGroupToGroupMapping(fromGroup, toGroup);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToGroupMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToGroupMappingAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupToGroupMappingRemoveTime());
            try
            {
                persister.RemoveGroupToGroupMapping(fromGroup, toGroup);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToGroupMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToGroupMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingAddTime());
            try
            {
                persister.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
            try
            {
                persister.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
            try
            {
                persister.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
            try
            {
                persister.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new EntityTypeAddTime());
            try
            {
                persister.AddEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityTypeAddTime());
                throw;
            }
            metricLogger.End(beginId, new EntityTypeAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new EntityTypeRemoveTime());
            try
            {
                persister.RemoveEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityTypeRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new EntityTypeRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new EntityAddTime());
            try
            {
                persister.AddEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityAddTime());
                throw;
            }
            metricLogger.End(beginId, new EntityAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new EntityRemoveTime());
            try
            {
                persister.RemoveEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new EntityRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new EntityRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserToEntityMappingAddTime());
            try
            {
                persister.AddUserToEntityMapping(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToEntityMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new UserToEntityMappingAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new UserToEntityMappingRemoveTime());
            try
            {
                persister.RemoveUserToEntityMapping(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new UserToEntityMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new UserToEntityMappingRemoveTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupToEntityMappingAddTime());
            try
            {
                persister.AddGroupToEntityMapping(group, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToEntityMappingAddTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToEntityMappingAddTime());
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            Guid beginId = metricLogger.Begin(new GroupToEntityMappingRemoveTime());
            try
            {
                persister.RemoveGroupToEntityMapping(group, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GroupToEntityMappingRemoveTime());
                throw;
            }
            metricLogger.End(beginId, new GroupToEntityMappingRemoveTime());
        }

    }
}
