﻿/*
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
    /// Logs metric events for an implementation of <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <remarks>Uses a facade pattern to front the IAccessManagerQueryProcessor, capturing metrics and forwarding method calls to the IAccessManagerQueryProcessor.</remarks>
    public class AccessManagerQueryProcessorMetricLogger<TUser, TGroup, TComponent, TAccess> : IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The IAccessManagerQueryProcessor implementation to log metrics for.</summary>
        protected IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> queryProcessor;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.AccessManagerQueryProcessorMetricLogger class.
        /// </summary>
        /// <param name="eventProcessor">The IAccessManagerQueryProcessor implementation to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerQueryProcessorMetricLogger(IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> queryProcessor, IMetricLogger metricLogger)
        {
            this.queryProcessor = queryProcessor;
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> Users
        {
            get
            {
                IEnumerable<TUser> result;
                Guid beginId = metricLogger.Begin(new UsersPropertyQueryTime());
                try
                {
                    result = queryProcessor.Users;
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new UsersPropertyQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new UsersPropertyQueryTime());
                metricLogger.Increment(new UsersPropertyQuery());

                return result;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                IEnumerable<TGroup> result;
                Guid beginId = metricLogger.Begin(new GroupsPropertyQueryTime());
                try
                {
                    result = queryProcessor.Groups;
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GroupsPropertyQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GroupsPropertyQueryTime());
                metricLogger.Increment(new GroupsPropertyQuery());

                return result;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                IEnumerable<String> result;
                Guid beginId = metricLogger.Begin(new EntityTypesPropertyQueryTime());
                try
                {
                    result = queryProcessor.EntityTypes;
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new EntityTypesPropertyQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new EntityTypesPropertyQueryTime());
                metricLogger.Increment(new EntityTypesPropertyQuery());

                return result;
            }
        }

        /// <inheritdoc/>
        public Boolean ContainsUser(TUser user)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new ContainsUserQueryTime());
            try
            {
                result = queryProcessor.ContainsUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ContainsUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new ContainsUserQueryTime());
            metricLogger.Increment(new ContainsUserQuery());

            return result;
        }

        /// <inheritdoc/>
        public Boolean ContainsGroup(TGroup group)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new ContainsGroupQueryTime());
            try
            {
                result = queryProcessor.ContainsGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ContainsGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new ContainsGroupQueryTime());
            metricLogger.Increment(new ContainsGroupQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetUserToGroupMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetUserToGroupMappings(user, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetUserToGroupMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetUserToGroupMappingsQueryTime());
                metricLogger.Increment(new GetUserToGroupMappingsQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetUserToGroupMappings(user, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetUserToGroupMappingsWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings)
        {
            HashSet<TUser> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetGroupToUserMappingsForGroupQueryTime());
                try
                {
                    result = queryProcessor.GetGroupToUserMappings(group, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToUserMappingsForGroupQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToUserMappingsForGroupQueryTime());
                metricLogger.Increment(new GetGroupToUserMappingsForGroupQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetGroupToUserMappings(group, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetGroupToUserMappingsForGroupWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetGroupToGroupMappingsForGroupQueryTime());
                try
                {
                    result = queryProcessor.GetGroupToGroupMappings(group, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToGroupMappingsForGroupQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToGroupMappingsForGroupQueryTime());
                metricLogger.Increment(new GetGroupToGroupMappingsForGroupQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetGroupToGroupMappings(group, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(TGroup group, Boolean includeIndirectMappings)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetGroupToGroupReverseMappingsForGroupQueryTime());
                try
                {
                    result = queryProcessor.GetGroupToGroupReverseMappings(group, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToGroupReverseMappingsForGroupQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToGroupReverseMappingsForGroupQueryTime());
                metricLogger.Increment(new GetGroupToGroupReverseMappingsForGroupQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetGroupToGroupReverseMappings(group, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            IEnumerable<Tuple<TComponent, TAccess>> result;
            Guid beginId = metricLogger.Begin(new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime());
            try
            {
                result = queryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime());
            metricLogger.Increment(new GetUserToApplicationComponentAndAccessLevelMappingsQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            IEnumerable<TUser> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetApplicationComponentAndAccessLevelToUserMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetApplicationComponentAndAccessLevelToUserMappings(applicationComponent, accessLevel, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetApplicationComponentAndAccessLevelToUserMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetApplicationComponentAndAccessLevelToUserMappingsQueryTime());
                metricLogger.Increment(new GetApplicationComponentAndAccessLevelToUserMappingsQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetApplicationComponentAndAccessLevelToUserMappings(applicationComponent, accessLevel, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            IEnumerable<Tuple<TComponent, TAccess>> result;
            Guid beginId = metricLogger.Begin(new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime());
            try
            {
                result = queryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime());
            metricLogger.Increment(new GetGroupToApplicationComponentAndAccessLevelMappingsQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            IEnumerable<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetApplicationComponentAndAccessLevelToGroupMappings(applicationComponent, accessLevel, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime());
                metricLogger.Increment(new GetApplicationComponentAndAccessLevelToGroupMappingsQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetApplicationComponentAndAccessLevelToGroupMappings(applicationComponent, accessLevel, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public Boolean ContainsEntityType(String entityType)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new ContainsEntityTypeQueryTime());
            try
            {
                result = queryProcessor.ContainsEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ContainsEntityTypeQueryTime());
                throw;
            }
            metricLogger.End(beginId, new ContainsEntityTypeQueryTime());
            metricLogger.Increment(new ContainsEntityTypeQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            IEnumerable<String> result;
            Guid beginId = metricLogger.Begin(new GetEntitiesQueryTime());
            try
            {
                result = queryProcessor.GetEntities(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesQueryTime());
            metricLogger.Increment(new GetEntitiesQuery());

            return result;
        }

        /// <inheritdoc/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new ContainsEntityQueryTime());
            try
            {
                result = queryProcessor.ContainsEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ContainsEntityQueryTime());
                throw;
            }
            metricLogger.End(beginId, new ContainsEntityQueryTime());
            metricLogger.Increment(new ContainsEntityQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            IEnumerable<Tuple<String, String>> result;
            Guid beginId = metricLogger.Begin(new GetUserToEntityMappingsForUserQueryTime());
            try
            {
                result = queryProcessor.GetUserToEntityMappings(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetUserToEntityMappingsForUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetUserToEntityMappingsForUserQueryTime());
            metricLogger.Increment(new GetUserToEntityMappingsForUserQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            IEnumerable<String> result;
            Guid beginId = metricLogger.Begin(new GetUserToEntityMappingsForUserAndEntityTypeQueryTime());
            try
            {
                result = queryProcessor.GetUserToEntityMappings(user, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetUserToEntityMappingsForUserAndEntityTypeQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetUserToEntityMappingsForUserAndEntityTypeQueryTime());
            metricLogger.Increment(new GetUserToEntityMappingsForUserAndEntityTypeQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            IEnumerable<TUser> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetEntityToUserMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetEntityToUserMappings(entityType, entity, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetEntityToUserMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetEntityToUserMappingsQueryTime());
                metricLogger.Increment(new GetEntityToUserMappingsQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetEntityToUserMappingsWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetEntityToUserMappings(entityType, entity, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetEntityToUserMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetEntityToUserMappingsWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetEntityToUserMappingsWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            IEnumerable<Tuple<String, String>> result;
            Guid beginId = metricLogger.Begin(new GetGroupToEntityMappingsForGroupQueryTime());
            try
            {
                result = queryProcessor.GetGroupToEntityMappings(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToEntityMappingsForGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToEntityMappingsForGroupQueryTime());
            metricLogger.Increment(new GetGroupToEntityMappingsForGroupQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            IEnumerable<String> result;
            Guid beginId = metricLogger.Begin(new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime());
            try
            {
                result = queryProcessor.GetGroupToEntityMappings(group, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime());
            metricLogger.Increment(new GetGroupToEntityMappingsForGroupAndEntityTypeQuery());

            return result;
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> GetEntityToGroupMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            IEnumerable<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetEntityToGroupMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetEntityToGroupMappings(entityType, entity, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetEntityToGroupMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetEntityToGroupMappingsQueryTime());
                metricLogger.Increment(new GetEntityToGroupMappingsQuery());
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetEntityToGroupMappingsWithIndirectMappingsQueryTime());
                try
                {
                    result = queryProcessor.GetEntityToGroupMappings(entityType, entity, includeIndirectMappings);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetEntityToGroupMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetEntityToGroupMappingsWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetEntityToGroupMappingsWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <inheritdoc/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new HasAccessToApplicationComponentForUserQueryTime());
            try
            {
                result = queryProcessor.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToApplicationComponentForUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToApplicationComponentForUserQueryTime());
            metricLogger.Increment(new HasAccessToApplicationComponentForUserQuery());

            return result;
        }

        /// <inheritdoc/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new HasAccessToEntityForUserQueryTime());
            try
            {
                result = queryProcessor.HasAccessToEntity(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToEntityForUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToEntityForUserQueryTime());
            metricLogger.Increment(new HasAccessToEntityForUserQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Guid beginId = metricLogger.Begin(new GetApplicationComponentsAccessibleByUserQueryTime());
            try
            {
                result = queryProcessor.GetApplicationComponentsAccessibleByUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetApplicationComponentsAccessibleByUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetApplicationComponentsAccessibleByUserQueryTime());
            metricLogger.Increment(new GetApplicationComponentsAccessibleByUserQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Guid beginId = metricLogger.Begin(new GetApplicationComponentsAccessibleByGroupQueryTime());
            try
            {
                result = queryProcessor.GetApplicationComponentsAccessibleByGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetApplicationComponentsAccessibleByGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetApplicationComponentsAccessibleByGroupQueryTime());
            metricLogger.Increment(new GetApplicationComponentsAccessibleByGroupQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            HashSet<Tuple<String, String>> result;
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = queryProcessor.GetEntitiesAccessibleByUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByUserQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByUserQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            HashSet<String> result;
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = queryProcessor.GetEntitiesAccessibleByUser(user, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByUserQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByUserQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            HashSet<Tuple<String, String>> result;
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = queryProcessor.GetEntitiesAccessibleByGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            HashSet<String> result;
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = queryProcessor.GetEntitiesAccessibleByGroup(group, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupQuery());

            return result;
        }
    }
}
