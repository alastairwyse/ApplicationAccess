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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Distributes operations to multiple shards in a distributed AccessManager implementation, and aggregates and returns their results.
    /// </summary>
    /// <remarks>Unlike <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, this class does not coordinate responses to operations across all shards in a distributed AccessManager implementation.  Instead, it's designed to be used 'downstream' of a <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, and front multiple shards of a single type (i.e. user or group shards).  The class simply distributes operations only to the shards of the type defined by the query method, acting as a 'router'.  I.e. in the <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, queries which should be distributed to indirectly mapped groups first call to the group to group mapping shards to get the indirectly mapped groups, before retrieving mappings from those groups in the individual group shards.  A similar method implemented in this class would not call to the group to group mapping shards.</remarks>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public class DistributedAccessManagerOperationRouter<TClientConfiguration> :
        DistributedAccessManagerOperationCoordinator<TClientConfiguration>,
        IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        // This class inherits from DistributedAccessManagerOperationCoordinator<TClientConfiguration> but then ends up overriding many of its methods with a NotImplementedException
        //   which is bad practice from an OO design perspective.  Problem is that it needs to implement many of the methods that DistributedAccessManagerOperationCoordinator does
        //   but these implementations don't adhere cleanly to the underlying interfaces being implemented.  E.g. on IAccessManagerAsyncQueryProcessor, some methods are implemented,
        //   and some are not.  Might have been possible to derive this and DistributedAccessManagerOperationCoordinator from a base class containing the common methods, BUT I was
        //   reluctant to go making DistributedAccessManagerOperationCoordinator more complex, and it's a really key class in the distributed setup, whereas this class whilst
        //   important, only has a very short lifetime in the overall running of a distributed AccessManager instance... i.e. for a period of likely <30 seconds while the final batch
        //   of a shard split is being performed.  Hence decided to leave DistributedAccessManagerOperationCoordinator as it, and derive from it in this class so I can still reuse 
        //   DistributedAccessManagerOperationCoordinator's method where required.
        // Might want to improve this in the future.



        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerOperationRouter class.
        /// </summary>
        /// <param name="shardClientManager">Manages the clients used to connect to shards managing the subsets of elements in the distributed access manager implementation.</param>
        /// <param name="metricLogger">Logger for metrics.</param>
        public DistributedAccessManagerOperationRouter(IShardClientManager<TClientConfiguration> shardClientManager, IMetricLogger metricLogger)
            : base(shardClientManager, metricLogger)
        {
        }

        /// <summary>Exception message for overridden methods which aren't implemented.</summary>
        protected const String methodNotImplementedExceptionMessage = "This method is not implemented in this class.";

        /// <inheritdoc/>
        public override async Task<List<String>> GetUsersAsync()
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return await client.GetUsersAsync();
            };

            return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve users from");
        }

        /// <inheritdoc/>
        public override async Task<List<String>> GetGroupsAsync()
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return await client.GetGroupsAsync();
            };

            return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve groups from");
        }

        /// <inheritdoc/>
        public override async Task<List<String>> GetEntityTypesAsync()
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return await client.GetEntityTypesAsync();
            };

            return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve entity types from");
        }

        /// <inheritdoc/>
        public override async Task<Boolean> ContainsGroupAsync(String group)
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Tuple<Boolean, Guid>>> shardCheckFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return Tuple.Create(await client.ContainsGroupAsync(group), Guid.NewGuid());
            };

            return await ContainsElementAsync(shardDataElementTypes, shardCheckFunc, $"check for group '{group}' in");
        }

        /// <inheritdoc/>
        public override async Task RemoveGroupAsync(String group)
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> shardRemoveFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveGroupAsync(group);
            };

            await RemoveElementAsync(shardDataElementTypes, shardRemoveFunc, $"remove group '{group}' from");
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(IEnumerable<String> groups)
        {
            var returnUsers = new HashSet<String>();
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

            return new List<String>(returnUsers);
        }

        /// <inheritdoc/>
        public override async Task<List<String>> GetApplicationComponentAndAccessLevelToUserMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));

            var shardDataElementTypes = new List<DataElement>() { DataElement.User };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return await client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
            };

            return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve application component and access level to user mappings from");
        }

        /// <inheritdoc/>
        public override async Task<List<String>> GetApplicationComponentAndAccessLevelToGroupMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));

            var shardDataElementTypes = new List<DataElement>() { DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return await client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
            };

            return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve application component and access level to group mappings from");
        }

        /// <inheritdoc/>
        public override async Task<Boolean> ContainsEntityTypeAsync(String entityType)
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Tuple<Boolean, Guid>>> shardCheckFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return Tuple.Create(await client.ContainsEntityTypeAsync(entityType), Guid.NewGuid());
            };

            return await ContainsElementAsync(shardDataElementTypes, shardCheckFunc, $"check for entity type '{entityType}' in");
        }

        /// <inheritdoc/>
        public override async Task RemoveEntityTypeAsync(String entityType)
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> shardRemoveFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveEntityTypeAsync(entityType);
            };

            await RemoveElementAsync(shardDataElementTypes, shardRemoveFunc, $"remove entity type '{entityType}' from");
        }

        /// <inheritdoc/>
        public override async Task<List<String>> GetEntitiesAsync(String entityType)
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return await client.GetEntitiesAsync(entityType);
            };

            return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, $"retrieve entities of type '{entityType}' from");
        }

        /// <inheritdoc/>
        public override async Task<Boolean> ContainsEntityAsync(String entityType, String entity)
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Tuple<Boolean, Guid>>> shardCheckFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                return Tuple.Create(await client.ContainsEntityAsync(entityType, entity), Guid.NewGuid());
            };

            return await ContainsElementAsync(shardDataElementTypes, shardCheckFunc, $"check for entity '{entity}' with type '{entityType}' in");
        }

        /// <inheritdoc/>
        public override async Task RemoveEntityAsync(String entityType, String entity)
        {
            var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> shardRemoveFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveEntityAsync(entityType, entity);
            };

            await RemoveElementAsync(shardDataElementTypes, shardRemoveFunc, $"remove entity '{entity}' with type '{entityType}' from");
        }

        /// <inheritdoc/>
        public override async Task<List<String>> GetEntityToUserMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));

            return new List<String>(await GetEntityToUserDirectMappingsAsync(entityType, entity));
        }

        /// <inheritdoc/>
        public override async Task<List<String>> GetEntityToGroupMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));
            
            var returnList = new List<String>();
            foreach (String currentGroup in (await GetEntityToGroupDirectMappingsAsync(entityType, entity)).Item1)
            {
                returnList.Add(currentGroup);
            }

            return returnList;
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(IEnumerable<String> groups, String applicationComponent, String accessLevel)
        {
            Boolean result = false;
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
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

            return result;
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(IEnumerable<String> groups, String entityType, String entity)
        {
            Boolean result = false;
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
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

            return result;
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
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

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
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

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups, String entityType)
        {
            var result = new HashSet<String>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<List<String>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> funcGroups) =>
            {
                return await clientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(funcGroups, entityType);
            };
            Action<List<String>> resultAction = (List<String> groupShardResult) =>
            {
                result.UnionWith(groupShardResult);
            };
            Func<List<String>, Boolean> continuePredicate = (groupShardResult) => { return true; };
            queryMetricData = await ExecuteQueryAgainstGroupShards
            (
                groups,
                createQueryTaskFunc,
                Enumerable.Empty<Tuple<Task<List<String>>, String>>(),
                resultAction,
                continuePredicate,
                new HashSet<Type>(),
                new HashSet<Type>(),
                false,
                $"retrieve entity mappings for multiple groups and entity type '{entityType}' from",
                false
            );

            return new List<String>(result);
        }

        #region Non-implemented Public Methods

        /// <inheritdoc/>
        public override Task AddUserAsync(String user)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<Boolean> ContainsUserAsync(String user)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task RemoveUserAsync(String user)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddGroupAsync(String group)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddUserToGroupMappingAsync(String user, String group)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetUserToGroupMappingsAsync(String user, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetGroupToUserMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task RemoveUserToGroupMappingAsync(String user, String group)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetGroupToGroupMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task<List<String>> GetGroupToGroupMappingsAsync(IEnumerable<String> groups)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetGroupToGroupReverseMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task<List<String>> GetGroupToGroupReverseMappingsAsync(IEnumerable<String> groups)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task RemoveGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(String user)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddEntityTypeAsync(String entityType)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(String group)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddEntityAsync(String entityType, String entity)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(String user)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetUserToEntityMappingsAsync(String user, String entityType)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task RemoveUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task AddGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(String group)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetGroupToEntityMappingsAsync(String group, String entityType)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task RemoveGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<Boolean> HasAccessToApplicationComponentAsync(String user, String applicationComponent, String accessLevel)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<Boolean> HasAccessToEntityAsync(String user, String entityType, String entity)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByUserAsync(String user)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupAsync(String group)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(String user)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetEntitiesAccessibleByUserAsync(String user, String entityType)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(String group)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public override Task<List<String>> GetEntitiesAccessibleByGroupAsync(String group, String entityType)
        {
            throw new NotImplementedException(methodNotImplementedExceptionMessage);
        }

        #endregion

        #region Private/Protected Methods

        /// <summary>
        /// Retrieves lists of elements returned from Get*() methods in all AccessManager instance shards of specified types, and merges and returns those lists.
        /// </summary>
        /// <typeparam name="TReturn">The type of element in the list to return.</typeparam>
        /// <param name="shardDataElementTypes">The types of data elements managed by the shards to query.</param>
        /// <param name="shardReadFunc">A function which is run against each shard to be queried.  Accepts the client instance, and returns a list of <typeparamref name="TReturn"/>.</param>
        /// <param name="exceptionEventDescription">A description of the query being performed, to use in an exception message in the case of error.  E.g. "retrieve groups from".</param>
        /// <returns>The merged list of elements.</returns>
        protected async Task<List<TReturn>> GetAndMergeElementsAsync<TReturn>
        (
            IEnumerable<DataElement> shardDataElementTypes, 
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<TReturn>>> shardReadFunc, 
            String exceptionEventDescription
        )
        {
            var returnElements = new HashSet<TReturn>();
            var clients = new List<DistributedClientAndShardDescription>();
            foreach (DataElement currentDataElement in shardDataElementTypes)
            {
                try
                {
                    clients.AddRange(shardClientManager.GetAllClients(currentDataElement, Operation.Query));
                }
                catch (ArgumentException)
                {
                    // Method will throw an ArgumentException if no clients of the specified type exist.  This is acceptable if querying for multiple DataElement types, and no clients exist for one of the types.
                    //   E.g. if this method is used to get all entity types, 'shardDataElementTypes' would contain users and groups, but if routing in front of user nodes, no group clients will be returned.
                }
            }
            var shardReadTasks = new HashSet<Task<List<TReturn>>>();
            var taskToShardDescriptionMap = new Dictionary<Task<List<TReturn>>, String>();
            foreach (DistributedClientAndShardDescription currentClient in clients)
            {
                Task<List<TReturn>> currentTask = shardReadFunc(currentClient.Client);
                taskToShardDescriptionMap.Add(currentTask, currentClient.ShardConfigurationDescription);
                shardReadTasks.Add(currentTask);
            }
            Action<List<TReturn>> resultAction = (List<TReturn> currentShardElements) =>
            {
                returnElements.UnionWith(currentShardElements);
            };
            Func<List<TReturn>, Boolean> continuePredicate = (List<TReturn> currentShardElements) => { return true; };
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
                exceptionEventDescription,
                null,
                null
            );

            return new List<TReturn>(returnElements);
        }

        /// <summary>
        /// Retrieves the aggregated result returned from a Contains*() method in all AccessManager instance shards of specified types.
        /// </summary>
        /// <param name="shardDataElementTypes">The types of data elements managed by the shards to query.</param>
        /// <param name="shardCheckFunc">A function containing the Contains*() method to be run against each shard.  Accepts a shard client instance, and returns a boolean containing the result of the Contains*() method call.</param>
        /// <param name="exceptionEventDescription">A description of the query being performed, to use in an exception message in the case of error.  E.g. "check for groups in".</param>
        /// <returns>The result of the Contains*() method.</returns>
        protected async Task<Boolean> ContainsElementAsync
        (
            IEnumerable<DataElement> shardDataElementTypes,
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Tuple<Boolean, Guid>>> shardCheckFunc,
            String exceptionEventDescription
        )
        {
            Boolean result = false;
            var clients = new List<DistributedClientAndShardDescription>();
            foreach (DataElement currentDataElement in shardDataElementTypes)
            {
                try
                {
                    clients.AddRange(shardClientManager.GetAllClients(currentDataElement, Operation.Query));
                }
                catch (ArgumentException)
                {
                    // Method will throw an ArgumentException if no clients of the specified type exist.  This is acceptable if querying for multiple DataElement types, and no clients exist for one of the types.
                    //   E.g. if this method is used to get all entity types, 'shardDataElementTypes' would contain users and groups, but if routing in front of user nodes, no group clients will be returned.
                }
            }
            var shardReadTasks = new HashSet<Task<Tuple<Boolean, Guid>>>();
            var taskToShardDescriptionMap = new Dictionary<Task<Tuple<Boolean, Guid>>, String>();
            foreach (DistributedClientAndShardDescription currentClient in clients)
            {
                Task<Tuple<Boolean, Guid>> currentTask = shardCheckFunc(currentClient.Client);
                taskToShardDescriptionMap.Add(currentTask, currentClient.ShardConfigurationDescription);
                shardReadTasks.Add(currentTask);
            }
            Action<Tuple<Boolean, Guid>> resultAction = (Tuple<Boolean, Guid> currentShardResult) =>
            {
                if (currentShardResult.Item1 == true)
                {
                    result = true;
                }
            };
            Func<Tuple<Boolean, Guid>, Boolean> continuePredicate = (Tuple<Boolean, Guid> shardResult) => { return !(shardResult.Item1); };
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
                exceptionEventDescription,
                null,
                null
            );

            return result;
        }

        /// <summary>
        /// Executes a Remove*() method in all AccessManager instance shards of specified types.
        /// </summary>
        /// <param name="shardDataElementTypes">The types of data elements managed by the shards to execute against.</param>
        /// <param name="shardRemoveFunc">A function containing the Remove*() method to execute against each shard.  Accepts a shard client instance, and returns a <see cref="Task"/>.</param>
        /// <param name="exceptionEventDescription">A description of the remove event being performed, to use in an exception message in the case of error.  E.g. "remove entity type '{entityType}' from".</param>
        protected async Task RemoveElementAsync
        (
            IEnumerable<DataElement> shardDataElementTypes,
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> shardRemoveFunc,
            String exceptionEventDescription
        )
        {
            var clients = new List<DistributedClientAndShardDescription>();
            foreach (DataElement currentDataElement in shardDataElementTypes)
            {
                try
                {
                    clients.AddRange(shardClientManager.GetAllClients(currentDataElement, Operation.Query));
                }
                catch (ArgumentException)
                {
                    // Method will throw an ArgumentException if no clients of the specified type exist.  This is acceptable if querying for multiple DataElement types, and no clients exist for one of the types.
                    //   E.g. if this method is used to get all entity types, 'shardDataElementTypes' would contain users and groups, but if routing in front of user nodes, no group clients will be returned.
                }
            }
            HashSet<Task<Guid>> shardRemoveTasks;
            Dictionary<Task<Guid>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<Guid>> wrappedRemoveFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                await shardRemoveFunc(clientAndDescription.Client);
                return Guid.NewGuid();
            };
            (shardRemoveTasks, taskToShardDescriptionMap) = CreateTasks<Guid>(clients, wrappedRemoveFunc);
            Action<Guid> resultAction = (Guid fakeResult) => { };
            Func<Guid, Boolean> continuePredicate = (Guid fakeResult) => { return true; };
            var rethrowExceptions = new HashSet<Type>();
            var ignoreExceptions = new HashSet<Type>();
            await AwaitTaskCompletionAsync
            (
                shardRemoveTasks,
                taskToShardDescriptionMap,
                resultAction,
                continuePredicate,
                rethrowExceptions,
                ignoreExceptions,
                exceptionEventDescription,
                null,
                null
            );
        }

        #endregion
    }
}
