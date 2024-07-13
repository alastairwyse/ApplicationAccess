/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Metrics;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution.Metrics
{
    /// <summary>
    /// Logs metric events for an implementation of <see cref="IDistributedAccessManager{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IDistributedAccessManager implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IDistributedAccessManager implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IDistributedAccessManager implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IDistributedAccessManager implementation.</typeparam>
    /// <remarks>Uses a facade pattern to front the IDistributedAccessManager, capturing metrics and forwarding method calls to the IDistributedAccessManager.</remarks>
    public class DistributedAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess> : 
        AccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess>,
        IDistributedAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The IDistributedAccessManager implementation to log metrics for.</summary>
        protected IDistributedAccessManager<TUser, TGroup, TComponent, TAccess> distributedAccessManager;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.Metrics.DistributedAccessManagerMetricLogger class.
        /// </summary>
        /// <param name="distributedAccessManager">The IDistributedAccessManager implementation to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerMetricLogger(IDistributedAccessManager<TUser, TGroup, TComponent, TAccess> distributedAccessManager, IMetricLogger metricLogger)
            : base(distributedAccessManager, metricLogger)
        {
            this.distributedAccessManager = distributedAccessManager;
        }

        /// <inheritdoc/>
        public HashSet<TUser> GetGroupToUserMappings(IEnumerable<TGroup> groups)
        {
            HashSet<TUser> result;
            Guid beginId = metricLogger.Begin(new GetGroupToUserMappingsForGroupsQueryTime());
            try
            {
                result = distributedAccessManager.GetGroupToUserMappings(groups);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToUserMappingsForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToUserMappingsForGroupsQueryTime());
            metricLogger.Increment(new GetGroupToUserMappingsForGroupsQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupMappings(IEnumerable<TGroup> groups)
        {
            HashSet<TGroup> result;
            Guid beginId = metricLogger.Begin(new GetGroupToGroupMappingsForGroupsQueryTime());
            try
            {
                result = distributedAccessManager.GetGroupToGroupMappings(groups);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToGroupMappingsForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToGroupMappingsForGroupsQueryTime());
            metricLogger.Increment(new GetGroupToGroupMappingsForGroupsQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(IEnumerable<TGroup> groups)
        {
            HashSet<TGroup> result;
            Guid beginId = metricLogger.Begin(new GetGroupToGroupReverseMappingsForGroupsQueryTime());
            try
            {
                result = distributedAccessManager.GetGroupToGroupReverseMappings(groups);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToGroupReverseMappingsForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToGroupReverseMappingsForGroupsQueryTime());
            metricLogger.Increment(new GetGroupToGroupReverseMappingsForGroupsQuery());

            return result;
        }

        /// <inheritdoc/>
        public Boolean HasAccessToApplicationComponent(IEnumerable<TGroup> groups, TComponent applicationComponent, TAccess accessLevel)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new HasAccessToApplicationComponentForGroupsQueryTime());
            try
            {
                result = distributedAccessManager.HasAccessToApplicationComponent(groups, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToApplicationComponentForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToApplicationComponentForGroupsQueryTime());
            metricLogger.Increment(new HasAccessToApplicationComponentForGroupsQuery());

            return result;
        }

        /// <inheritdoc/>
        public Boolean HasAccessToEntity(IEnumerable<TGroup> groups, String entityType, String entity)
        {
            Boolean result;
            Guid beginId = metricLogger.Begin(new HasAccessToEntityForGroupsQueryTime());
            try
            {
                result = distributedAccessManager.HasAccessToEntity(groups, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToEntityForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToEntityForGroupsQueryTime());
            metricLogger.Increment(new HasAccessToEntityForGroupsQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroups(IEnumerable<TGroup> groups)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Guid beginId = metricLogger.Begin(new GetApplicationComponentsAccessibleByGroupsQueryTime());
            try
            {
                result = distributedAccessManager.GetApplicationComponentsAccessibleByGroups(groups);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetApplicationComponentsAccessibleByGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetApplicationComponentsAccessibleByGroupsQueryTime());
            metricLogger.Increment(new GetApplicationComponentsAccessibleByGroupsQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroups(IEnumerable<TGroup> groups)
        {
            HashSet<Tuple<String, String>> result;
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupsQueryTime());
            try
            {
                result = distributedAccessManager.GetEntitiesAccessibleByGroups(groups);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupsQuery());

            return result;
        }

        /// <inheritdoc/>
        public HashSet<String> GetEntitiesAccessibleByGroups(IEnumerable<TGroup> groups, String entityType)
        {
            HashSet<String> result;
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupsQueryTime());
            try
            {
                result = distributedAccessManager.GetEntitiesAccessibleByGroups(groups, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupsQuery());

            return result;
        }
    }
}
