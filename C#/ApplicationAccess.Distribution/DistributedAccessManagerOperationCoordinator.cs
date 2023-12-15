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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Metrics;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Coordinates operations in an AccessManager implementation where responsibility for subsets of elements is distributed across multiple computers.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to distributed components.</typeparam>
    public class DistributedAccessManagerOperationCoordinator<TClientConfiguration> : IDistributedAccessManagerOperationCoordinator<TClientConfiguration>, IDisposable
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        // TODO:
        //   RE-REVIEW WHICH SHARDS OPERATIONS ARE SENT TO!!!
        //     ESPECIALLY Things like Remove Group ContainsGroup
        //     ALREADY HAD LOTS OF THINGS WRONG HERE ON FIRST REVIEW...e.g.ContainsGroup should go to all shards of ALL ELEMTN TYPES...INC ALL GORUPTOGROUP!!! 

        // Go through all protected methods and consolidate and make consistent
        //   Also go through all XML doco and make sure terminology is consistent

        // GetUniqueGroupToGroupMappingsAsync() potentially needs its own unique metric specifically for this class
        // Check reference counts or protected methods... remove stuff which is not called

        /// <summary>Manages the clients used to connect to shards managing the subsets of elements in the distributed implementation..</summary>
        protected IShardClientManager<TClientConfiguration> shardClientManager;
        /// <summary>The hash code generator for users.</summary>
        protected IHashCodeGenerator<String> userHashCodeGenerator;
        /// <summary>The hash code generator for groups.</summary>
        protected IHashCodeGenerator<String> groupHashCodeGenerator;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerOperationCoordinator class.
        /// </summary>
        /// <param name="initialShardConfiguration">The initial configuration of the shards managing the subsets of elements in the distributed implementation.</param>
        /// <param name="shardClientFactory">Factory used to create <see cref="IDistributedAccessManagerAsyncClient{String, String, String, String}"/> instances from <see cref="TClientConfiguration"/> objects, which connect to shards managing the subsets of elements in the distributed implementation.</param>
        /// <param name="userHashCodeGenerator">Hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">Hash code generator for groups.</param>
        /// <param name="metricLogger">Logger for metrics.</param>
        public DistributedAccessManagerOperationCoordinator
        (
            ShardConfigurationSet<TClientConfiguration> initialShardConfiguration, 
            IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, String, String, String, String> shardClientFactory, 
            IHashCodeGenerator<String> userHashCodeGenerator, 
            IHashCodeGenerator<String> groupHashCodeGenerator, 
            IMetricLogger metricLogger
        )
        {
            shardClientManager = new ShardClientManager<TClientConfiguration>(initialShardConfiguration, shardClientFactory, userHashCodeGenerator, groupHashCodeGenerator, metricLogger);
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.metricLogger= metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerOperationCoordinator class.
        /// </summary>
        /// <param name="initialShardConfiguration">The initial configuration of the shards managing the subsets of elements in the distributed implementation.</param>
        /// <param name="shardClientFactory">Factory used to create <see cref="IDistributedAccessManagerAsyncClient{String, String, String, String}"/> instances from <see cref="TClientConfiguration"/> objects, which connect to shards managing the subsets of elements in the distributed implementation.</param>
        /// <param name="userHashCodeGenerator">Hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">Hash code generator for groups.</param>
        /// <param name="metricLogger">Logger for metrics.</param>
        /// <param name="shardClientManager">A mock shard client manager.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public DistributedAccessManagerOperationCoordinator
        (
            ShardConfigurationSet<TClientConfiguration> initialShardConfiguration,
            IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, String, String, String, String> shardClientFactory,
            IHashCodeGenerator<String> userHashCodeGenerator,
            IHashCodeGenerator<String> groupHashCodeGenerator,
            IMetricLogger metricLogger,
            IShardClientManager<TClientConfiguration> shardClientManager
        )
            : this(initialShardConfiguration, shardClientFactory, userHashCodeGenerator, groupHashCodeGenerator, metricLogger)
        {
            this.shardClientManager.Dispose();
            this.shardClientManager = shardClientManager;
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUsersAsync()
        {
            Guid beginId = metricLogger.Begin(new UsersPropertyQueryTime());
            IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.User, Operation.Query);
            HashSet<Task<List<String>>> shardReadTasks;
            Dictionary<Task<List<String>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return await clientAndDescription.Client.GetUsersAsync();
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<List<String>>(clients, createTaskFunc);
            var aggregatedUsers = new List<String>();
            Action<List<String>> resultAction = (List<String> users) =>
            {
                aggregatedUsers.AddRange(users);
            };
            Func<List<String>, Boolean> continuePredicate = (List<String> users) => { return true; };
            await AwaitTaskCompletionAsync(shardReadTasks, taskToShardDescriptionMap, resultAction, continuePredicate, "retrieve users from", beginId, new UsersPropertyQueryTime());
            metricLogger.End(beginId, new UsersPropertyQueryTime());
            metricLogger.Increment(new UsersPropertyQuery());

            return aggregatedUsers;
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupsAsync()
        {
            Guid beginId = metricLogger.Begin(new GroupsPropertyQueryTime());
            var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query));
            HashSet<Task<List<String>>> shardReadTasks;
            Dictionary<Task<List<String>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return await clientAndDescription.Client.GetGroupsAsync();
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<List<String>>(clients, createTaskFunc);
            var aggregatedGroups = new HashSet<String>();
            Action<List<String>> resultAction = (List<String> groups) =>
            {
                aggregatedGroups.UnionWith(groups);
            };
            Func<List<String>, Boolean> continuePredicate = (List<String> groups) => { return true; };
            await AwaitTaskCompletionAsync(shardReadTasks, taskToShardDescriptionMap, resultAction, continuePredicate, "retrieve groups from", beginId, new GroupsPropertyQueryTime());
            metricLogger.End(beginId, new GroupsPropertyQueryTime());
            metricLogger.Increment(new GroupsPropertyQuery());

            return new List<String>(aggregatedGroups);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityTypesAsync()
        {
            Guid beginId = metricLogger.Begin(new EntityTypesPropertyQueryTime());
            var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));
            HashSet<Task<List<String>>> shardReadTasks;
            Dictionary<Task<List<String>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return await clientAndDescription.Client.GetEntityTypesAsync();
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<List<String>>(clients, createTaskFunc);
            var aggregatedEntityTypes = new HashSet<String>();
            Action<List<String>> resultAction = (List<String> entityTypes) =>
            {
                aggregatedEntityTypes.UnionWith(entityTypes);
            };
            Func<List<String>, Boolean> continuePredicate = (List<String> entityTypes) => { return true; };
            await AwaitTaskCompletionAsync(shardReadTasks, taskToShardDescriptionMap, resultAction, continuePredicate, "retrieve entity types from", beginId, new EntityTypesPropertyQueryTime());
            metricLogger.End(beginId, new EntityTypesPropertyQueryTime());
            metricLogger.Increment(new EntityTypesPropertyQuery());

            return new List<String>(aggregatedEntityTypes);
        }

        /// <inheritdoc/>
        public async Task AddUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddUserAsync(user);
            };
            await ProcessEventAsync(new UserAddTime(), new UserAdded(), DataElement.User, user, eventAction, $"add user '{user}' to");
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsUserAsync(String user)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () => shardClientManager.GetAllClients(DataElement.User, Operation.Query);
            Func<DistributedClientAndShardDescription, Task<Boolean>> createTaskFunc = (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return clientAndDescription.Client.ContainsUserAsync(user);
            };

            return await ContainsElementAsync(new ContainsUserQueryTime(), new ContainsUserQuery(), getClientsFunc, createTaskFunc, $"user '{user}'");
        }

        /// <inheritdoc/>
        public async Task RemoveUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveUserAsync(user);
            };
            await ProcessEventAsync(new UserRemoveTime(), new UserRemoved(), new List<DataElement>() { DataElement.User }, eventAction, $"remove user '{user}' from");
        }

        /// <inheritdoc/>
        public async Task AddGroupAsync(String group)
        {
            Guid beginId = metricLogger.Begin(new GroupAddTime());
            var clients = new List<DistributedClientAndShardDescription>();
            // Can't call ProcessEventAsync() in this method as we do for AddUserAsync() and AddEntityTypeAsync() as this method requires
            //   a non-standard combination of GetClient*() calls as below (i.e. single group client and all group to group clients)
            clients.Add(shardClientManager.GetClient(DataElement.Group, Operation.Event, group));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event));
            HashSet<Task<Guid>> shardTasks;
            Dictionary<Task<Guid>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<Guid>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                await clientAndDescription.Client.AddGroupAsync(group);
                return Guid.NewGuid();
            };
            (shardTasks, taskToShardDescriptionMap) = CreateTasks<Guid>(clients, createTaskFunc);
            Action<Guid> resultAction = (Guid fakeResult) => { };
            Func<Guid, Boolean> continuePredicate = (Guid fakeResult) => { return true; };
            await AwaitTaskCompletionAsync(shardTasks, taskToShardDescriptionMap, resultAction, continuePredicate, $"add group '{group}' to", beginId, new GroupAddTime());
            metricLogger.End(beginId, new GroupAddTime());
            metricLogger.Increment(new GroupAdded());
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsGroupAsync(String group)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () =>
            {
                var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
                clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));
                clients.AddRange(shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query));

                return clients;
            };
            Func<DistributedClientAndShardDescription, Task<Boolean>> createTaskFunc = (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return clientAndDescription.Client.ContainsGroupAsync(group);
            };

            return await ContainsElementAsync(new ContainsGroupQueryTime(), new ContainsGroupQuery(), getClientsFunc, createTaskFunc, $"group '{group}'");
        }

        /// <inheritdoc/>
        public async Task RemoveGroupAsync(String group)
        {
            var dataElements = new List<DataElement>() { DataElement.User, DataElement.Group, DataElement.GroupToGroupMapping };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveGroupAsync(group);
            };
            await ProcessEventAsync(new GroupRemoveTime(), new GroupRemoved(), dataElements, eventAction, $"remove group '{group}' from");
        }

        /// <inheritdoc/>
        public async Task AddUserToGroupMappingAsync(String user, String group)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddUserToGroupMappingAsync(user, group);
            };
            await ProcessEventAsync
            (
                new UserToGroupMappingAddTime(),
                new UserToGroupMappingAdded(),
                DataElement.User,
                user,
                eventAction,
                $"add a mapping between user '{user}' and group '{group}' to"
            );
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToGroupMappingsAsync(String user, Boolean includeIndirectMappings)
        {
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetUserToGroupMappingsAsync(user, includeIndirectMappings);
            };
            QueryIntervalMetric intervalMetric = null;
            QueryCountMetric countMetric = null;
            if (includeIndirectMappings == false)
            {
                intervalMetric = new GetUserToGroupMappingsQueryTime();
                countMetric = new GetGroupToGroupMappingsForGroupQuery();
            }
            else
            {
                throw new NotImplementedException();
                // Needs to do multiple shard calls
            }

            return await GetElementsAsync
            (
                intervalMetric,
                countMetric,
                DataElement.User,
                user,
                createTaskFunc,
                $"retrieve user to group mappings for user '{user}' from"
            );
        }

        /// <inheritdoc/>
        public async Task RemoveUserToGroupMappingAsync(String user, String group)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveUserToGroupMappingAsync(user, group);
            };
            await ProcessEventAsync
            (
                new UserToGroupMappingRemoveTime(),
                new UserToGroupMappingRemoved(),
                DataElement.User,
                user,
                eventAction,
                $"remove mapping between user '{user}' and group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public async Task AddGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddGroupToGroupMappingAsync(fromGroup, toGroup);
            };
            await ProcessEventAsync
            (
                new GroupToGroupMappingAddTime(),
                new GroupToGroupMappingAdded(),
                DataElement.GroupToGroupMapping,
                fromGroup,
                eventAction,
                $"add a mapping between groups '{fromGroup}' and '{toGroup}' to"
            );
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetGroupToGroupMappingsAsync(group, includeIndirectMappings);
            };
            QueryIntervalMetric intervalMetric = null;
            QueryCountMetric countMetric = null;
            if (includeIndirectMappings == false)
            {
                intervalMetric = new GetGroupToGroupMappingsForGroupQueryTime();
                countMetric = new GetGroupToGroupMappingsForGroupQuery();
            }
            else
            {
                intervalMetric = new GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime();
                countMetric = new GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery();
            }

            return await GetElementsAsync
            (
                intervalMetric,
                countMetric,
                DataElement.GroupToGroupMapping,
                group,
                createTaskFunc,
                $"retrieve group to group mappings for group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveGroupToGroupMappingAsync(fromGroup, toGroup);
            };
            await ProcessEventAsync
            (
                new GroupToGroupMappingRemoveTime(),
                new GroupToGroupMappingRemoved(),
                DataElement.GroupToGroupMapping,
                fromGroup,
                eventAction,
                $"remove mapping between groups '{fromGroup}' and '{toGroup}' from"
            );
        }

        /// <inheritdoc/>
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
            };
            await ProcessEventAsync
            (
                new UserToApplicationComponentAndAccessLevelMappingAddTime(),
                new UserToApplicationComponentAndAccessLevelMappingAdded(),
                DataElement.User,
                user,
                eventAction,
                $"add a mapping between user '{user}' application component '{applicationComponent}' and access level '{accessLevel}' to"
            );
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(String user)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
            };

            return await GetElementsAsync
            (
                new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime(),
                new GetUserToApplicationComponentAndAccessLevelMappingsQuery(),
                DataElement.User,
                user,
                createTaskFunc,
                $"retrieve user to application component and access level mappings for user '{user}' from"
            );
        }

        /// <inheritdoc/>
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
            };
            await ProcessEventAsync
            (
                new UserToApplicationComponentAndAccessLevelMappingRemoveTime(),
                new UserToApplicationComponentAndAccessLevelMappingRemoved(),
                DataElement.User,
                user,
                eventAction,
                $"remove mapping between user '{user}' application component '{applicationComponent}' and access level '{accessLevel}' from"
            );
        }

        /// <inheritdoc/>
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
            };
            await ProcessEventAsync
            (
                new GroupToApplicationComponentAndAccessLevelMappingAddTime(), 
                new GroupToApplicationComponentAndAccessLevelMappingAdded(), 
                DataElement.Group, 
                group, 
                eventAction, 
                $"add a mapping between group '{group}' application component '{applicationComponent}' and access level '{accessLevel}' to"
            );
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(String group)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
            };

            return await GetElementsAsync
            (
                new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime(),
                new GetGroupToApplicationComponentAndAccessLevelMappingsQuery(),
                DataElement.Group,
                group,
                createTaskFunc,
                $"retrieve group to application component and access level mappings for group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
            };
            await ProcessEventAsync
            (
                new GroupToApplicationComponentAndAccessLevelMappingRemoveTime(),
                new GroupToApplicationComponentAndAccessLevelMappingRemoved(),
                DataElement.Group,
                group,
                eventAction,
                $"remove mapping between group '{group}' application component '{applicationComponent}' and access level '{accessLevel}' from"
            );
        }

        /// <inheritdoc/>
        public async Task AddEntityTypeAsync(String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddEntityTypeAsync(entityType);
            };
            await ProcessEventAsync(new EntityTypeAddTime(), new EntityTypeAdded(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"add entity type '{entityType}' to");
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityTypeAsync(String entityType)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () =>
            {
                var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
                clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));

                return clients;
            };
            Func<DistributedClientAndShardDescription, Task<Boolean>> createTaskFunc = (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return clientAndDescription.Client.ContainsEntityTypeAsync(entityType);
            };

            return await ContainsElementAsync(new ContainsEntityTypeQueryTime(), new ContainsEntityTypeQuery(), getClientsFunc, createTaskFunc, $"entity type '{entityType}'");
        }

        /// <inheritdoc/>
        public async Task RemoveEntityTypeAsync(String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveEntityTypeAsync(entityType);
            };
            await ProcessEventAsync(new EntityTypeRemoveTime(), new EntityTypeRemoved(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"remove entity type '{entityType}' from");
        }

        /// <inheritdoc/>
        public async Task AddEntityAsync(String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddEntityAsync(entityType, entity);
            };
            await ProcessEventAsync(new EntityAddTime(), new EntityAdded(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"add entity '{entity}' with type '{entityType}' to");
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAsync(String entityType)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesQueryTime());
            var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));
            HashSet<Task<List<String>>> shardReadTasks;
            Dictionary<Task<List<String>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return await clientAndDescription.Client.GetEntitiesAsync(entityType);
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<List<String>>(clients, createTaskFunc);
            var aggregatedEntities = new HashSet<String>();
            Action<List<String>> resultAction = (List<String> entities) =>
            {
                aggregatedEntities.UnionWith(entities);
            };
            Func<List<String>, Boolean> continuePredicate = (List<String> entities) => { return true; };
            await AwaitTaskCompletionAsync(shardReadTasks, taskToShardDescriptionMap, resultAction, continuePredicate, $"retrieve entities of type '{entityType}' from", beginId, new GetEntitiesQueryTime());
            metricLogger.End(beginId, new GetEntitiesQueryTime());
            metricLogger.Increment(new GetEntitiesQuery());

            return new List<String>(aggregatedEntities);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityAsync(String entityType, String entity)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () =>
            {
                var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
                clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));

                return clients;
            };
            Func<DistributedClientAndShardDescription, Task<Boolean>> createTaskFunc = (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return clientAndDescription.Client.ContainsEntityAsync(entityType, entity);
            };

            return await ContainsElementAsync(new ContainsEntityQueryTime(), new ContainsEntityQuery(), getClientsFunc, createTaskFunc, $"entity '{entity}' with type '{entityType}'");
        }

        /// <inheritdoc/>
        public async Task RemoveEntityAsync(String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveEntityAsync(entityType, entity);
            };
            await ProcessEventAsync(new EntityRemoveTime(), new EntityRemoved(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"remove entity '{entity}' of type '{entityType}' from");
        }

        /// <inheritdoc/>
        public async Task AddUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddUserToEntityMappingAsync(user, entityType, entity);
            };
            await ProcessEventAsync
            (
                new UserToEntityMappingAddTime(),
                new UserToEntityMappingAdded(),
                DataElement.User,
                user,
                eventAction,
                $"add a mapping between user '{user}' entity type '{entityType}' and entity '{entity}' to"
            );
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(String user)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetUserToEntityMappingsAsync(user);
            };

            return await GetElementsAsync
            (
                new GetUserToEntityMappingsForUserQueryTime(),
                new GetUserToEntityMappingsForUserQuery(),
                DataElement.User,
                user,
                createTaskFunc,
                $"retrieve user to entity mappings for user '{user}' from"
            );
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToEntityMappingsAsync(String user, String entityType)
        {
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetUserToEntityMappingsAsync(user, entityType);
            };

            return await GetElementsAsync
            (
                new GetUserToEntityMappingsForUserAndEntityTypeQueryTime(),
                new GetUserToEntityMappingsForUserAndEntityTypeQuery(),
                DataElement.User,
                user,
                createTaskFunc,
                $"retrieve user to entity mappings for user '{user}' and entity type '{entityType}' from"
            );
        }

        /// <inheritdoc/>
        public async Task RemoveUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveUserToEntityMappingAsync(user, entityType, entity);
            };
            await ProcessEventAsync
            (
                new UserToEntityMappingRemoveTime(),
                new UserToEntityMappingRemoved(),
                DataElement.User,
                user,
                eventAction,
                $"remove mapping between user '{user}' entity type '{entityType}' and entity '{entity}' from"
            );
        }

        /// <inheritdoc/>
        public async Task AddGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddGroupToEntityMappingAsync(group, entityType, entity);
            };
            await ProcessEventAsync
            (
                new GroupToEntityMappingAddTime(),
                new GroupToEntityMappingAdded(),
                DataElement.Group,
                group,
                eventAction,
                $"add a mapping between group '{group}' entity type '{entityType}' and entity '{entity}' to"
            );
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(String group)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetGroupToEntityMappingsAsync(group);
            };

            return await GetElementsAsync
            (
                new GetGroupToEntityMappingsForGroupQueryTime(),
                new GetGroupToEntityMappingsForGroupQuery(),
                DataElement.Group,
                group,
                createTaskFunc,
                $"retrieve group to entity mappings for group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToEntityMappingsAsync(String group, String entityType)
        {
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetGroupToEntityMappingsAsync(group, entityType);
            };

            return await GetElementsAsync
            (
                new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime(),
                new GetGroupToEntityMappingsForGroupAndEntityTypeQuery(),
                DataElement.Group,
                group,
                createTaskFunc,
                $"retrieve group to entity mappings for group '{group}' and entity type '{entityType}' from"
            );
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveGroupToEntityMappingAsync(group, entityType, entity);
            };
            await ProcessEventAsync
            (
                new GroupToEntityMappingRemoveTime(),
                new GroupToEntityMappingRemoved(),
                DataElement.Group,
                group,
                eventAction,
                $"remove mapping between group '{group}' entity type '{entityType}' and entity '{entity}' from"
            );
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(String user, String applicationComponent, String accessLevel)
        {
            DistributedClientAndShardDescription userClientAndDescription = shardClientManager.GetClient(DataElement.User, Operation.Query, user);
            // Get the groups mapped directly to the user
            List<String> mappedGroups = null;
            try
            {
                mappedGroups = await userClientAndDescription.Client.GetUserToGroupMappingsAsync(user, false);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to get user to group mappings from shard with configuration '{userClientAndDescription.ShardConfigurationDescription}'.", e);
            }
            // Get the groups and their clients mapped indirectly to the user
            IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> directlyMappedClientsAndGroups = MapGroupsToShardClients(mappedGroups, DataElement.GroupToGroupMapping);
            IEnumerable<String> allMappedGroupd = await GetUniqueGroupToGroupMappingsAsync(directlyMappedClientsAndGroups);
            IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> allClientsAndGroups = MapGroupsToShardClients(allMappedGroupd, DataElement.Group);
            // Create tasks which call HasAccessToApplicationComponent() for all groups
            Func<DistributedClientAndShardDescription, Task<Boolean>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                // TODO: Tricky... can't pass same 'groups' param to every client... hence might need a special version of CreateTasks for this purpose
                //   **AND** Don't forget to add the call to the user client

                return await clientAndDescription.Client.HasAccessToApplicationComponentAsync()
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<List<String>>(clients, createTaskFunc);
            

        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(String user, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByUserAsync(String user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupAsync(String group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(String user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByUserAsync(String user, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(String group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupAsync(String group, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void RefreshShardConfiguration(ShardConfigurationSet<TClientConfiguration> shardConfiguration)
        {
            try
            {
                shardClientManager.RefreshConfiguration(shardConfiguration);
            }
            catch (Exception e)
            {
                throw new ShardConfigurationRefreshException("Failed to refresh shard configuration.", e);
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Processes a specified event against a single shard in the distributed environment.
        /// </summary>
        /// <param name="intervalMetric">An interval metric to log as part of processing.</param>
        /// <param name="countMetric">A count metric to log as part of processing.</param>
        /// <param name="dataElement">The type of the element in the event.</param>
        /// <param name="elementValue">The value of the element.</param>
        /// <param name="eventFunc">An asyncronous function to execute against the client which connects to the shard which manages the element, and which processes the event.  Accepts a single parameter which is the client, and returns a <see cref="Task"/>.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "remove user 'user1' from".</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        protected async Task ProcessEventAsync
        (
            IntervalMetric intervalMetric,
            CountMetric countMetric,
            DataElement dataElement, 
            String elementValue, 
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventFunc, 
            String exceptionEventDescription
        )
        {
            Guid beginId = metricLogger.Begin(intervalMetric);
            DistributedClientAndShardDescription client = shardClientManager.GetClient(dataElement, Operation.Event, elementValue);
            try
            {
                await eventFunc(client.Client);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, intervalMetric);
                throw new Exception($"Failed to {exceptionEventDescription} shard with configuration '{client.ShardConfigurationDescription}'.", e);
            }
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);
        }

        /// <summary>
        /// Processes a specified event against a multiple shards in the distributed environment.
        /// </summary>
        /// <param name="intervalMetric">An interval metric to log as part of processing.</param>
        /// <param name="countMetric">A count metric to log as part of processing.</param>
        /// <param name="dataElements">The types of the element managed by the shards to process the event against.</param>
        /// <param name="eventFunc">A function to execute against the client which connects to each shard which manage the element type, and which processes the event.  Accepts a single parameter which is the current client, and returns a <see cref="Task"/>.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "remove user 'user1' from".</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        protected async Task ProcessEventAsync
        (
            IntervalMetric intervalMetric,
            CountMetric countMetric,
            IEnumerable<DataElement> dataElements,
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventFunc,
            String exceptionEventDescription
        )
        {
            var clients = new List<DistributedClientAndShardDescription>();
            foreach (DataElement currentDataElement in dataElements)
            {
                clients.AddRange(shardClientManager.GetAllClients(currentDataElement, Operation.Event));
            }
            await ProcessEventAsync(intervalMetric, countMetric, clients, eventFunc, exceptionEventDescription);
        }

        /// <summary>
        /// Processes a specified event against a multiple shards in the distributed environment.
        /// </summary>
        /// <param name="intervalMetric">An interval metric to log as part of processing.</param>
        /// <param name="countMetric">A count metric to log as part of processing.</param>
        /// <param name="clients">A collection of clients (and corresponding descriptions of the shards they connect to) to process the event against.</param>
        /// <param name="eventFunc">A function to execute against the client which connects to each shard which manage the element type, and which processes the event.  Accepts a single parameter which is the current client, and returns a <see cref="Task"/>.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "remove user 'user1' from".</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        protected async Task ProcessEventAsync
        (
            IntervalMetric intervalMetric,
            CountMetric countMetric,
            IEnumerable<DistributedClientAndShardDescription> clients,
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventFunc,
            String exceptionEventDescription
        )
        {
            Guid beginId = metricLogger.Begin(intervalMetric);
            // The event method defined in parameter 'eventFunc' is an async method that does not return a value, and hence that parameter is a Func which
            //   returns a non-generic Task.  However, from testing it seems that whenever any non-generic Task that completes successfully is await(ed) within
            //   an async method (or Func) which returns a non-generic Task, that method then returns the static 'CompletedTask' Task, and hence these can't be 
            //   stored distinctly in a HashSet<Task> (since they're the same object).  So to workaround the 'wrappedEventFunc' below returns a generic Task 
            //   where the T value is a Guid (and is populated with a random Guid).  These Task<Guid>s are distinct objects and hence can be held in HashSets 
            //   and as the key in Dictionaries.
            HashSet<Task<Guid>> shardTasks;
            Dictionary<Task<Guid>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<Guid>> wrappedEventFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                await eventFunc(clientAndDescription.Client);
                return Guid.NewGuid();
            };
            (shardTasks, taskToShardDescriptionMap) = CreateTasks<Guid>(clients, wrappedEventFunc);
            Action<Guid> resultAction = (Guid fakeResult) => { };
            Func<Guid, Boolean> continuePredicate = (Guid fakeResult) => { return true; };
            await AwaitTaskCompletionAsync(shardTasks, taskToShardDescriptionMap, resultAction, continuePredicate, exceptionEventDescription, beginId, intervalMetric);
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);
        }

        /// <summary>
        /// Returns true if an element exists in the distributed environment.
        /// </summary>
        /// <param name="intervalMetric">An interval metric to log as part of the check.</param>
        /// <param name="countMetric">A count metric to log after checking for the element.</param>
        /// <param name="getClientsFunc">A function which returns a collection of <see cref="DistributedClientAndShardDescription"/> containing the clients to use to connect to all shards in which to check for the element.</param>
        /// <param name="createTaskFunc">A function which accepts a client which connects to a shard (and associated description of the shard), and returns a task which resolves a boolean and holds whether that shard contains the element.</param>
        /// <param name="exceptionElementAndValue">The name and value of the element to use in exception messages.  E.g. "entity type 'Clients'".</param>
        /// <returns>Whether any of the shards in the distributed environment contained the element.</returns>
        protected async Task<Boolean> ContainsElementAsync
        (
            QueryIntervalMetric intervalMetric,
            QueryCountMetric countMetric,
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc,
            Func<DistributedClientAndShardDescription, Task<Boolean>> createTaskFunc,
            String exceptionElementAndValue
        )
        {
            Guid beginId = metricLogger.Begin(intervalMetric);
            IEnumerable<DistributedClientAndShardDescription> clients = getClientsFunc();
            HashSet<Task<Boolean>> shardTasks;
            Dictionary<Task<Boolean>, String> taskToShardDescriptionMap;
            (shardTasks, taskToShardDescriptionMap) = CreateTasks<Boolean>(clients, createTaskFunc);
            Boolean result = false;
            Action<Boolean> resultAction = (Boolean shardResult) =>
            {
                if (shardResult == true)
                {
                    result = true;
                }
            };
            Func<Boolean, Boolean> continuePredicate = (Boolean shardResult) => { return !shardResult; };
            await AwaitTaskCompletionAsync(shardTasks, taskToShardDescriptionMap, resultAction, continuePredicate, $"check for {exceptionElementAndValue} in", beginId, intervalMetric);
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);

            return result;
        }

        /// <summary>
        /// Returns data from a single shard in the distributed environment.
        /// </summary>
        /// <param name="intervalMetric">An interval metric to log as part of the query.</param>
        /// <param name="countMetric">A count metric to log after returning the data.</param>
        /// <param name="dataElement">The type of the element to retrieve the data for.</param>
        /// <param name="elementValue">The value of the element.</param>
        /// <param name="createTaskFunc">A function which accepts a client which connects to a shard (and associated description of the shard), and returns a task which resolves to the type of data to return.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "retrieve user to group mappings for user 'user1' from".</param>
        /// <returns>The data elements.</returns>
        protected async Task<T> GetElementsAsync<T>
        (
            QueryIntervalMetric intervalMetric,
            QueryCountMetric countMetric,
            DataElement dataElement,
            String elementValue,
            Func<DistributedClientAndShardDescription, Task<T>> createTaskFunc,
            String exceptionEventDescription
        )
        {
            Guid beginId = metricLogger.Begin(intervalMetric);
            DistributedClientAndShardDescription client = shardClientManager.GetClient(dataElement, Operation.Query, elementValue);
            Task<T> shardTask = createTaskFunc(client);
            T result = default(T);
            try
            {
                result = await shardTask;
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, intervalMetric);
                throw new Exception($"Failed to {exceptionEventDescription} shard with configuration '{client.ShardConfigurationDescription}'.", e);
            }
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);

            return result;
        }

        /// <summary>
        /// Creates a set of tasks which perform an operation on each of a collection of shards.
        /// </summary>
        /// <typeparam name="T">The type of data returned by the tasks.</typeparam>
        /// <param name="clients">A collection of <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> and descriptions of the shards they connect to.</param>
        /// <param name="createTaskFunc">The function which creates each task.  Acceots a <see cref="DistributedClientAndShardDescription"/> containing the client to perform the operation against, and returns the task.</param>
        /// <returns>A tuple containing: a HashSet containing the created tasks, and a dictionary which maps each of the created tasks to the description of the shard the client which created the task connected to.</returns>
        protected (HashSet<Task<T>>, Dictionary<Task<T>, String>) CreateTasks<T>(IEnumerable<DistributedClientAndShardDescription> clients, Func<DistributedClientAndShardDescription, Task<T>> createTaskFunc)
        {
            var shardReadTasks = new HashSet<Task<T>>();
            var taskToShardDescriptionMap = new Dictionary<Task<T>, String>();
            foreach (DistributedClientAndShardDescription currentClient in clients)
            {
                Task<T> currentTask = createTaskFunc(currentClient);
                taskToShardDescriptionMap.Add(currentTask, currentClient.ShardConfigurationDescription);
                shardReadTasks.Add(currentTask);
            }

            return (shardReadTasks, taskToShardDescriptionMap);
        }

        /// <summary>
        /// Maps a collection of groups to a collection of <see cref="DistributedClientAndShardDescription">DistributedClientAndShardDescriptions</see> and the subset of the groups that are managed by that client.
        /// </summary>
        /// <param name="groups">The groups to map.</param>
        /// <param name="dataElement">The type of data element managed by the shard client, either <see cref="DataElement.Group"/> or <see cref="DataElement.GroupToGroupMapping"/>.</param>
        /// <returns>A collection of tuples, each containing: a <see cref="DistributedClientAndShardDescription"/>, and the groups that are managed by that client.</returns>
        protected IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> MapGroupsToShardClients(IEnumerable<String> groups, DataElement dataElement)
        {
            if (dataElement == DataElement.User)
                throw new ArgumentException($"Parameter '{nameof(dataElement)}' must contain either '{DataElement.Group}' or '{DataElement.GroupToGroupMapping}'.", nameof(dataElement));

            var returnMap = new Dictionary<DistributedClientAndShardDescription, HashSet<String>>();
            foreach (String currentGroup in groups)
            {
                DistributedClientAndShardDescription clientAndShardDescription = shardClientManager.GetClient(dataElement, Operation.Query, currentGroup);
                if (returnMap.ContainsKey(clientAndShardDescription) == false)
                {
                    returnMap.Add(clientAndShardDescription, new HashSet<String>());
                }
                returnMap[clientAndShardDescription].Add(currentGroup);
            }

            foreach (KeyValuePair<DistributedClientAndShardDescription, HashSet<String>> currentMapEntry in returnMap)
            {
                yield return new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>(currentMapEntry.Key, currentMapEntry.Value);
            }
        }

        /// <summary>
        /// Gets a unique list of groups which are mapped to from the specified collection of <see cref="DistributedClientAndShardDescription">DistributedClientAndShardDescriptions</see> and corresponding groups that are managed by each client.
        /// </summary>
        /// <param name="clientsAndGroups">A collection of tuples, each containing: a <see cref="DistributedClientAndShardDescription"/>, and the groups that are managed by that client, to retrieve</param>
        /// <returns>A unique list of groups which includes both the groups passed in the <paramref name="clientsAndGroups"/> parameter, and the groups those groups are mapped to.</returns>
        protected async Task<IEnumerable<String>> GetUniqueGroupToGroupMappingsAsync(IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> clientsAndGroups)
        {
            var returnGroups = new HashSet<String>();
            // Add all the inputted groups into the list to return
            foreach (Tuple<DistributedClientAndShardDescription, IEnumerable<String>> currentClientAndGroups in clientsAndGroups)
            {
                returnGroups.UnionWith(currentClientAndGroups.Item2);
            }
            // Create the tasks to retrieve the 'mapped to' groups
            var shardReadTasks = new HashSet<Task<List<String>>>();
            var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
            foreach (Tuple<DistributedClientAndShardDescription, IEnumerable<String>> currentClientAndGroups in clientsAndGroups)
            {
                Task<List<String>> currentTask = currentClientAndGroups.Item1.Client.GetGroupToGroupMappingsAsync(currentClientAndGroups.Item2);
                taskToShardDescriptionMap.Add(currentTask, currentClientAndGroups.Item1.ShardConfigurationDescription);
                shardReadTasks.Add(currentTask);
            }
            // Wait for the tasks to complete
            Action<List<String>> resultAction = (List<String> groups) =>
            {
                returnGroups.UnionWith(groups);
            };
            Func<List<String>, Boolean> continuePredicate = (List<String> groups) => { return true; };
            await AwaitTaskCompletionAsync(shardReadTasks, taskToShardDescriptionMap, resultAction, continuePredicate, "retrieve group to group mappings from", null, null);

            return returnGroups;
        }

        /// <summary>
        /// Asyncronously waits for a collection of tasks to complete.
        /// </summary>
        /// <typeparam name="T">The type of object returned by each task.</typeparam>
        /// <param name="shardTasks">The tasks for wait for.</param>
        /// <param name="taskToShardDescriptionMap">A dictionary which maps each task to a description of the shard the client which created the task connected to.</param>
        /// <param name="resultAction">An action to invoke with the results of each task.</param>
        /// <param name="continuePredicate">A function which returns a boolean which is called after the completion of each task and subdequent processing of its results, and which indicates whether further tasks shouled be waited for.  Accepts a single parameter which is the result of each task.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "remove user 'user1' from".</param>
        /// <param name="intervalMetricBeginId">An optional <see cref="Guid"/> returned from a call to <see cref="IMetricLogger.Begin(IntervalMetric)"/> which is used to cancel the interval metric in the case of an error.</param>
        /// <param name="cancelIntervalMetric">An optional <see cref="IntervalMetric"/> to cancel in the case of an error.</param>
        /// <remarks>Parameter <paramref name="continuePredicate"/> could be used to return false, e.g. in the case of distributed operations which return true as soon as a call to a shard returns true (e.g. method ContainsUserAsync())."/></remarks>
        protected async Task AwaitTaskCompletionAsync<T>
        (
            HashSet<Task<T>> shardTasks, 
            Dictionary<Task<T>, String> taskToShardDescriptionMap, 
            Action<T> resultAction, 
            Func<T, Boolean> continuePredicate,
            String exceptionEventDescription,
            Nullable<Guid>intervalMetricBeginId, 
            IntervalMetric cancelIntervalMetric
        )
        {
            if (intervalMetricBeginId.HasValue == true && cancelIntervalMetric == null)
                throw new ArgumentNullException($"Parameter '{nameof(cancelIntervalMetric)}' must be non-null if parameter '{nameof(intervalMetricBeginId)}' is non-null.");

            while (shardTasks.Count > 0)
            {
                Task<T> completedTask = await Task.WhenAny(shardTasks);
                try
                {
                    await completedTask;
                }
                catch (Exception e)
                {
                    if (intervalMetricBeginId.HasValue == true)
                    {
                        metricLogger.CancelBegin(intervalMetricBeginId.Value, cancelIntervalMetric);
                    }
                    throw new Exception($"Failed to {exceptionEventDescription} shard with configuration '{taskToShardDescriptionMap[completedTask]}'.", e);
                }
                resultAction(completedTask.Result);
                shardTasks.Remove(completedTask);
                if (continuePredicate(completedTask.Result) == false)
                {
                    return;
                }
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the DistributedAccessManagerOperationCoordinator.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~DistributedAccessManagerOperationCoordinator()
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
                    shardClientManager.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
