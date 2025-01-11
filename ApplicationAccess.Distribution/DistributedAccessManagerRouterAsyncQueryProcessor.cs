/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Linq;
using System.Threading.Tasks;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Distributes queries to multiple shards in a distributed AccessManager implementation, and aggregates and returns their results.
    /// </summary>
    /// <remarks>Unlike <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, this class does not coordinate responses to operations across all shards in a distributed AccessManager implementation.  Instead, it's designed to be used 'downstream' of a <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, and front multiple shards of a single type (i.e. user, group, or group to group).  The class simply distributes queries only to the shards of the type defined by the query method, acting as a 'router'.  I.e. in the <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, queries which should be distributed to indirectly mapped groups first call to the group to group mapping shards to get the indirectly mapped groups, before retrieving mappings from those groups in the individual group shards.  A similar method implemented in this class would not call to the group to group mapping shards.</remarks>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public class DistributedAccessManagerRouterAsyncQueryProcessor<TClientConfiguration> :
        DistributedAccessManagerOperationCoordinatorBase<TClientConfiguration>,
        IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerRouterAsyncQueryProcessor class.
        /// </summary>
        /// <param name="shardClientManager">Manages the clients used to connect to shards managing the subsets of elements in the distributed access manager implementation.</param>
        /// <param name="metricLogger">Logger for metrics.</param>
        public DistributedAccessManagerRouterAsyncQueryProcessor
        (
            IShardClientManager<TClientConfiguration> shardClientManager,
            IMetricLogger metricLogger
        ) : base(shardClientManager, metricLogger)
        {
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(IEnumerable<String> groups)
        {
            Guid beginId = metricLogger.Begin(new GetGroupToUserMappingsForGroupsQueryTime());
            var returnUsers = new HashSet<String>();
            try
            {
                IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.User, Operation.Query);
                // Create the tasks to retrieve the mapped users
                var shardReadTasks = new HashSet<Task<List<String>>>();
                var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
                foreach (DistributedClientAndShardDescription currentClient in clients)
                {
                    Task<List<String>> currentTask = currentClient.Client.GetGroupToUserMappingsAsync(groups);
                    taskToShardDescriptionMap.Add(currentTask, currentClient.ShardConfigurationDescription);
                    shardReadTasks.Add(currentTask);
                }
                // Wait for the tasks to complete
                Action<List<String>> resultAction = (List<String> currentShardUsers) =>
                {
                    returnUsers.UnionWith(currentShardUsers);
                };
                Func<List<String>, Boolean> continuePredicate = (List<String> currentShardUsers) => { return true; };
                var rethrowExceptions = new HashSet<Type>();
                var ignoreExceptions = new HashSet<Type>();
                await AwaitTaskCompletionAsync
                (
                    shardReadTasks,
                    taskToShardDescriptionMap,
                    resultAction,
                    continuePredicate,
                    rethrowExceptions,
                    ignoreExceptions,
                    "retrieve group to user mappings for multiple groups from",
                    null,
                    null
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToUserMappingsForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToUserMappingsForGroupsQueryTime());
            metricLogger.Increment(new GetGroupToUserMappingsForGroupsQuery());

            return new List<String>(returnUsers);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupMappingsAsync(IEnumerable<String> groups)
        {
            Guid beginId = metricLogger.Begin(new GetGroupToGroupMappingsForGroupsQueryTime());
            var returnGroups = new HashSet<String>(groups);
            try
            {
                IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> clientsAndGroups = shardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, groups);
                // Create the tasks to retrieve the mapped groups
                var shardReadTasks = new HashSet<Task<List<String>>>();
                var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
                foreach (Tuple<DistributedClientAndShardDescription, IEnumerable<String>> currentClientAndGroups in clientsAndGroups)
                {
                    Task<List<String>> currentTask = currentClientAndGroups.Item1.Client.GetGroupToGroupMappingsAsync(currentClientAndGroups.Item2);
                    taskToShardDescriptionMap.Add(currentTask, currentClientAndGroups.Item1.ShardConfigurationDescription);
                    shardReadTasks.Add(currentTask);
                }
                // Wait for the tasks to complete
                Action<List<String>> resultAction = (List<String> currentShardGroups) =>
                {
                    returnGroups.UnionWith(currentShardGroups);
                };
                Func<List<String>, Boolean> continuePredicate = (List<String> currentShardGroups) => { return true; };
                var rethrowExceptions = new HashSet<Type>();
                var ignoreExceptions = new HashSet<Type>();
                await AwaitTaskCompletionAsync
                (
                    shardReadTasks,
                    taskToShardDescriptionMap,
                    resultAction,
                    continuePredicate,
                    rethrowExceptions,
                    ignoreExceptions,
                    "retrieve group to group mappings for multiple groups from",
                    null,
                    null
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToGroupMappingsForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToGroupMappingsForGroupsQueryTime());
            metricLogger.Increment(new GetGroupToGroupMappingsForGroupsQuery());
            
            return new List<String>(returnGroups);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupReverseMappingsAsync(IEnumerable<String> groups)
        {
            Guid beginId = metricLogger.Begin(new GetGroupToGroupReverseMappingsForGroupsQueryTime());
            var returnGroups = new HashSet<String>(groups);
            try
            {
                returnGroups.UnionWith(await GetGroupToGroupReverseMappingsImplementationAsync(groups));
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetGroupToGroupReverseMappingsForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetGroupToGroupReverseMappingsForGroupsQueryTime());
            metricLogger.Increment(new GetGroupToGroupReverseMappingsForGroupsQuery());

            return new List<String>(returnGroups);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(IEnumerable<String> groups, String applicationComponent, String accessLevel)
        {
            Guid beginId = metricLogger.Begin(new HasAccessToApplicationComponentForGroupsQueryTime());
            Boolean result = false;
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<Boolean, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> funcGroups) =>
                {
                    Boolean groupShardresult = await clientAndDescription.Client.HasAccessToApplicationComponentAsync(funcGroups, applicationComponent, accessLevel);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<Boolean, Guid>> resultAction = (Tuple<Boolean, Guid> hasAccess) =>
                {
                    if (hasAccess.Item1 == true)
                    {
                        result = true;
                    }
                };
                Func<Tuple<Boolean, Guid>, Boolean> continuePredicate = (Tuple<Boolean, Guid> hasAccess) => { return !hasAccess.Item1; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    groups,
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<Boolean, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    false,
                    $"check access to application component '{applicationComponent}' at access level '{accessLevel}' for multiple groups in",
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToApplicationComponentForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToApplicationComponentForGroupsQueryTime());
            metricLogger.Increment(new HasAccessToApplicationComponentForGroupsQuery());
            metricLogger.Add(new HasAccessToApplicationComponentForGroupsGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return result;
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(IEnumerable<String> groups, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new HasAccessToEntityForGroupsQueryTime());
            Boolean result = false;
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<Boolean, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> funcGroups) =>
                {
                    Boolean groupShardresult = await clientAndDescription.Client.HasAccessToEntityAsync(funcGroups, entityType, entity);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<Boolean, Guid>> resultAction = (Tuple<Boolean, Guid> hasAccess) =>
                {
                    if (hasAccess.Item1 == true)
                    {
                        result = true;
                    }
                };
                Func<Tuple<Boolean, Guid>, Boolean> continuePredicate = (Tuple<Boolean, Guid> hasAccess) => { return !hasAccess.Item1; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    groups,
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<Boolean, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    false,
                    $"check access to entity '{entity}' with type '{entityType}' for multiple groups in",
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToEntityForGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToEntityForGroupsQueryTime());
            metricLogger.Increment(new HasAccessToEntityForGroupsQuery());
            metricLogger.Add(new HasAccessToEntityForGroupsGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return result;
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            Guid beginId = metricLogger.Begin(new GetApplicationComponentsAccessibleByGroupsQueryTime());
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                // See comment in method DistributedAccessManagerOperationCoordinator.HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<Tuple<String, String>>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> funcGroups) =>
                {
                    List<Tuple<String, String>> groupShardresult = await clientAndDescription.Client.GetApplicationComponentsAccessibleByGroupsAsync(funcGroups);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<List<Tuple<String, String>>, Guid>> resultAction = (Tuple<List<Tuple<String, String>>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<Tuple<String, String>>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    groups,
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    false,
                    $"retrieve application component and access level mappings for multiple groups from",
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetApplicationComponentsAccessibleByGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetApplicationComponentsAccessibleByGroupsQueryTime());
            metricLogger.Increment(new GetApplicationComponentsAccessibleByGroupsQuery());
            metricLogger.Add(new GetApplicationComponentsAccessibleByGroupsGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupsQueryTime());
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<Tuple<String, String>>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> funcGroups) =>
                {
                    List<Tuple<String, String>> groupShardresult = await clientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(funcGroups);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<List<Tuple<String, String>>, Guid>> resultAction = (Tuple<List<Tuple<String, String>>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<Tuple<String, String>>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    groups,
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    false,
                    $"retrieve entity mappings for multiple groups from",
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupsQuery());
            metricLogger.Add(new GetEntitiesAccessibleByGroupsGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups, String entityType)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupsQueryTime());
            var result = new HashSet<String>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<String>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> funcGroups) =>
                {
                    List<String> groupShardresult = await clientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(funcGroups, entityType);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<List<String>, Guid>> resultAction = (Tuple<List<String>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<String>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    groups,
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<List<String>, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    false,
                    $"retrieve entity mappings for multiple groups and entity type '{entityType}' from",
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupsQuery());
            metricLogger.Add(new GetEntitiesAccessibleByGroupsGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<String>(result);
        }
    }
}
