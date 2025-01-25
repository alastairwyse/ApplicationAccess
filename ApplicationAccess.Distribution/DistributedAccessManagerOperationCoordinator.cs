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
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Coordinates operations in an AccessManager implementation where responsibility for subsets of elements is distributed across multiple computers in shards.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public class DistributedAccessManagerOperationCoordinator<TClientConfiguration> :
        DistributedAccessManagerOperationProcessorBase<TClientConfiguration>, 
        IDistributedAccessManagerOperationCoordinator<TClientConfiguration>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        // A fundamental technique used in this class is to generate a set of Tasks, run them in parallel, and use the Task.WhenAny() to identify when each task
        //   is complete and perform post processing actions.  These Tasks are stored in a HashSet to minimize the time taken to remove them from the set of tasks
        //   once the post processing is complete.  From unit and real-world testing it was discovered that sometimes .NET would return duplicate (i.e. same object)
        //   tasks when generating (behaviour which was confirmed by this article...
        //   https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/task-fromresult-returns-singleton) which caused duplicate key exceptions
        //   when trying to store these in the HashSet (see notes in method HasAccessToApplicationComponentAsync() for further explanation).  So to work around this
        //   any methods which generate multiple Tasks have an additional Guid added to their return type (which is unused in the result of the method) which seems
        //   to prevent the returning of duplicate/cached Task objects.
        //   The downside if that the generic parameters passed to methods AwaitTaskCompletionAsync() have much more complex and less readable generic type parameters...
        //   e.g. method GetUsersAsync() should call all user shards returning a...
        //     Task<List<String>>
        //   ...from each, but due to the additional Guid, the return type becomes...
        //     Task<Tuple<List<String>, Guid>>
        //   ...and the problem is made worse in the parameters of AwaitTaskCompletionAsync() which wrap the result T type in Tuples, Actions, etc...
        //   Would be nice to simplify this at some point.
        //
        // TODO: AwaitTaskCompletionAsync() has some inefficiencies due to O(n^2) time complexity when iterating tasks (see https://devblogs.microsoft.com/pfxteam/processing-tasks-as-they-complete/)
        //   Could improve this by using continuation functions on each Task instead of using the Task.WhenAny() method (some examples linked to in this post https://stackoverflow.com/questions/72271006/task-whenany-alternative-to-list-avoiding-on%C2%B2-issues).
        //   Did some testing with this and running 2000 concurrent tasks, and even in worst recorded case, WhenAny() gave just a 10% performance hit over continuation functions
        //     so the actual performance penality is not nearly as bad as O(n) vs O(n^2) should be.
        //     (for 5000 concurrent tasks the performance hit rose to 28%).
        //   Should improve this at some point, but possibly not as bad (hence not as urgent) as it appears.  2000 tasks would mean a 2000 shard distributed deployment, which is very large. 
        //   Get it working stably with Task.WhenAny() first and then refactor.
        //   Not using Task.WhenAny() might also avoid having to use a HashSet to store Tasks, and hence also allow removing the returning of Guids outlined above.

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerOperationCoordinator class.
        /// </summary>
        /// <param name="shardClientManager">Manages the clients used to connect to shards managing the subsets of elements in the distributed access manager implementation.</param>
        /// <param name="metricLogger">Logger for metrics.</param>
        public DistributedAccessManagerOperationCoordinator(IShardClientManager<TClientConfiguration> shardClientManager, IMetricLogger metricLogger)
            : base(shardClientManager, metricLogger)
        {
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetUsersAsync()
        {
            Guid beginId = metricLogger.Begin(new UsersPropertyQueryTime());
            IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.User, Operation.Query);
            HashSet<Task<Tuple<List<String>, Guid>>> shardReadTasks;
            Dictionary<Task<Tuple<List<String>, Guid>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<Tuple<List<String>, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.GetUsersAsync(), Guid.NewGuid());
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<Tuple<List<String>, Guid>>(clients, createTaskFunc);
            var aggregatedUsers = new List<String>();
            Action<Tuple<List<String>, Guid>> resultAction = (Tuple<List<String>, Guid> users) =>
            {
                aggregatedUsers.AddRange(users.Item1);
            };
            Func<Tuple<List<String>, Guid>, Boolean> continuePredicate = (Tuple<List<String>, Guid> users) => { return true; };
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
                "retrieve users from", 
                beginId, 
                new UsersPropertyQueryTime()
            );
            metricLogger.End(beginId, new UsersPropertyQueryTime());
            metricLogger.Increment(new UsersPropertyQuery());

            return aggregatedUsers;
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetGroupsAsync()
        {
            Guid beginId = metricLogger.Begin(new GroupsPropertyQueryTime());
            var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query));
            HashSet<Task<Tuple<List<String>, Guid>>> shardReadTasks;
            Dictionary<Task<Tuple<List<String>, Guid>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<Tuple<List<String>, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.GetGroupsAsync(), Guid.NewGuid());
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<Tuple<List<String>, Guid>>(clients, createTaskFunc);
            var aggregatedGroups = new HashSet<String>();
            Action<Tuple<List<String>, Guid>> resultAction = (Tuple<List<String>, Guid> groups) =>
            {
                aggregatedGroups.UnionWith(groups.Item1);
            };
            Func<Tuple<List<String>, Guid>, Boolean> continuePredicate = (Tuple<List<String>, Guid> groups) => { return true; };
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
                "retrieve groups from", 
                beginId,
                new GroupsPropertyQueryTime()
            );
            metricLogger.End(beginId, new GroupsPropertyQueryTime());
            metricLogger.Increment(new GroupsPropertyQuery());

            return new List<String>(aggregatedGroups);
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetEntityTypesAsync()
        {
            Guid beginId = metricLogger.Begin(new EntityTypesPropertyQueryTime());
            var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));
            HashSet<Task<Tuple<List<String>, Guid>>> shardReadTasks;
            Dictionary<Task<Tuple<List<String>, Guid>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<Tuple<List<String>, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.GetEntityTypesAsync(), Guid.NewGuid());
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<Tuple<List<String>, Guid>>(clients, createTaskFunc);
            var aggregatedEntityTypes = new HashSet<String>();
            Action<Tuple<List<String>, Guid>> resultAction = (Tuple<List<String>, Guid> entityTypes) =>
            {
                aggregatedEntityTypes.UnionWith(entityTypes.Item1);
            };
            Func<Tuple<List<String>, Guid>, Boolean> continuePredicate = (Tuple<List<String>, Guid> entityTypes) => { return true; };
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
                "retrieve entity types from", 
                beginId, 
                new EntityTypesPropertyQueryTime()
            );
            metricLogger.End(beginId, new EntityTypesPropertyQueryTime());
            metricLogger.Increment(new EntityTypesPropertyQuery());

            return new List<String>(aggregatedEntityTypes);
        }

        /// <inheritdoc/>
        public virtual async Task AddUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddUserAsync(user);
            };
            await ProcessEventAsync(new UserAddTime(), new UserAdded(), DataElement.User, user, eventAction, new HashSet<Type>(), $"add user '{user}' to");
        }

        /// <inheritdoc/>
        public virtual async Task<Boolean> ContainsUserAsync(String user)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () =>
            {
                return new List<DistributedClientAndShardDescription>() { shardClientManager.GetClient(DataElement.User, Operation.Query, user) };
            };
            Func<DistributedClientAndShardDescription, Task<Tuple<Boolean, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.ContainsUserAsync(user), Guid.NewGuid());
            };

            return await ContainsElementAsync(new ContainsUserQueryTime(), new ContainsUserQuery(), getClientsFunc, createTaskFunc, $"user '{user}'");
        }

        /// <inheritdoc/>
        public virtual async Task RemoveUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveUserAsync(user);
            };
            await ProcessEventAsync(new UserRemoveTime(), new UserRemoved(), DataElement.User, user, eventAction, new HashSet<Type>(), $"remove user '{user}' from");
        }

        /// <inheritdoc/>
        public virtual async Task AddGroupAsync(String group)
        {
            Guid beginId = metricLogger.Begin(new GroupAddTime());
            var clients = new List<DistributedClientAndShardDescription>();
            // Can't call ProcessEventAsync() in this method as we do for AddUserAsync() and AddEntityTypeAsync() as this method requires
            //   a non-standard combination of GetClient*() calls as below (i.e. single group client and all group to group and user clients)
            clients.Add(shardClientManager.GetClient(DataElement.Group, Operation.Event, group));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Event));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.User, Operation.Event));
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
            var rethrowExceptions = new HashSet<Type>();
            var ignoreExceptions = new HashSet<Type>();
            await AwaitTaskCompletionAsync
            (
                shardTasks, 
                taskToShardDescriptionMap,
                resultAction, 
                continuePredicate,
                rethrowExceptions,
                ignoreExceptions,
                $"add group '{group}' to", 
                beginId, 
                new GroupAddTime()
            );
            metricLogger.End(beginId, new GroupAddTime());
            metricLogger.Increment(new GroupAdded());
        }

        /// <inheritdoc/>
        public virtual async Task<Boolean> ContainsGroupAsync(String group)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () =>
            {
                var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
                clients.Add(shardClientManager.GetClient(DataElement.Group, Operation.Query, group));
                clients.AddRange(shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query));

                return clients;
            };
            Func<DistributedClientAndShardDescription, Task<Tuple<Boolean, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.ContainsGroupAsync(group), Guid.NewGuid());
            };

            return await ContainsElementAsync(new ContainsGroupQueryTime(), new ContainsGroupQuery(), getClientsFunc, createTaskFunc, $"group '{group}'");
        }

        /// <inheritdoc/>
        public virtual async Task RemoveGroupAsync(String group)
        {
            var dataElements = new List<DataElement>() { DataElement.User, DataElement.Group, DataElement.GroupToGroupMapping };
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveGroupAsync(group);
            };
            await ProcessEventAsync(new GroupRemoveTime(), new GroupRemoved(), dataElements, eventAction, $"remove group '{group}' from");
        }

        /// <inheritdoc/>
        public virtual async Task AddUserToGroupMappingAsync(String user, String group)
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
                new HashSet<Type>(),
                $"add a mapping between user '{user}' and group '{group}' to"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetUserToGroupMappingsAsync(String user, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == false)
            {
                Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
                {
                    return await client.Client.GetUserToGroupMappingsAsync(user, false);
                };
                var rethrowExceptions = new HashSet<Type>() { typeof(UserNotFoundException<String>) };
                var ignoreExceptions = new HashSet<Type>();

                return await GetElementsAsync
                (
                    new GetUserToGroupMappingsQueryTime(),
                    new GetUserToGroupMappingsQuery(),
                    DataElement.User,
                    user,
                    createTaskFunc,
                    rethrowExceptions,
                    ignoreExceptions,
                    new List<String>(), 
                    $"retrieve user to group mappings for user '{user}' from"
                );
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                Int32 groupsMappedToUser = 0;
                var result = new List<String>();
                try
                {
                    DistributedClientAndShardDescription userClientAndDescription = shardClientManager.GetClient(DataElement.User, Operation.Query, user);
                    // Get the groups mapped directly to the user
                    List<String> mappedGroups = null;
                    try
                    {
                        mappedGroups = await userClientAndDescription.Client.GetUserToGroupMappingsAsync(user, false);
                    }
                    catch (UserNotFoundException<String>)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to retrieve user to group mappings for user '{user}' from shard with configuration '{userClientAndDescription.ShardConfigurationDescription}'.", e);
                    }
                    // Get the groups and their clients mapped indirectly to the user
                    IEnumerable<String> allMappedGroupd;
                    (allMappedGroupd, groupsMappedToUser) = await GetUniqueGroupToGroupMappingsAsync(mappedGroups, true);
                    result.AddRange(allMappedGroupd);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetUserToGroupMappingsWithIndirectMappingsQuery());
                metricLogger.Add(new GetUserToGroupMappingsGroupsMappedToUser(), groupsMappedToUser);

                return result;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetGroupToUserMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            IEnumerable<String> returnUsers = null;
            Int32 returnUsersCount = 0;
            if (includeIndirectMappings == false)
            {
                Guid beginId = metricLogger.Begin(new GetGroupToUserMappingsForGroupQueryTime());
                try
                {
                    (returnUsers, returnUsersCount) = await GetGroupToUserMappingsWithCountsAsync(new List<String> { group });
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToUserMappingsForGroupQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToUserMappingsForGroupQueryTime());
                metricLogger.Increment(new GetGroupToUserMappingsForGroupQuery());

                return new List<String>(returnUsers);
            }
            else
            {
                Guid beginId = metricLogger.Begin(new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                var mappedGroups = new HashSet<String>();
                try
                {
                    IEnumerable<String> reverseMappedGroups = await GetGroupToGroupReverseMappingsImplementationAsync(new List<String>() { group });
                    mappedGroups.UnionWith(reverseMappedGroups);
                    if (mappedGroups.Contains(group) == false)
                    {
                        mappedGroups.Add(group);
                    }
                    (returnUsers, returnUsersCount) = await GetGroupToUserMappingsWithCountsAsync(mappedGroups);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetGroupToUserMappingsForGroupWithIndirectMappingsQuery());

                return new List<String>(returnUsers);
            }
        }

        /// <inheritdoc/>
        public virtual async Task RemoveUserToGroupMappingAsync(String user, String group)
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
                new HashSet<Type>(),
                $"remove mapping between user '{user}' and group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task AddGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddGroupToGroupMappingAsync(fromGroup, toGroup);
            };
            var rethrowExceptions = new HashSet<Type>() { typeof(ArgumentException) };
            await ProcessEventAsync
            (
                new GroupToGroupMappingAddTime(),
                new GroupToGroupMappingAdded(),
                DataElement.GroupToGroupMapping,
                fromGroup,
                eventAction,
                rethrowExceptions,
                $"add a mapping between groups '{fromGroup}' and '{toGroup}' to"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetGroupToGroupMappingsAsync(String group, Boolean includeIndirectMappings)
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
            var rethrowExceptions = new HashSet<Type>(); 
            var ignoreExceptions = new HashSet<Type>() { typeof(GroupNotFoundException<String>) };

            return await GetElementsAsync
            (
                intervalMetric,
                countMetric,
                DataElement.GroupToGroupMapping,
                group,
                createTaskFunc,
                rethrowExceptions,
                ignoreExceptions,
                new List<String>(),
                $"retrieve group to group mappings for group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetGroupToGroupReverseMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == false)
            {
                var returnGroups = new HashSet<String>();
                Guid beginId = metricLogger.Begin(new GetGroupToGroupReverseMappingsForGroupQueryTime());
                IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
                // Create the tasks to retrieve the groups
                var shardReadTasks = new HashSet<Task<List<String>>>();
                var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
                foreach (DistributedClientAndShardDescription currentClient in clients)
                {
                    Task<List<String>> currentTask = currentClient.Client.GetGroupToGroupReverseMappingsAsync(group, includeIndirectMappings);
                    taskToShardDescriptionMap.Add(currentTask, currentClient.ShardConfigurationDescription);
                    shardReadTasks.Add(currentTask);
                }
                // Wait for the tasks to complete
                Action<List<String>> resultAction = (List<String> currentShardGroups) =>
                {
                    returnGroups.UnionWith(currentShardGroups);
                };
                Func<List<String>, Boolean> continuePredicate = (List<String> currentShardGroups) => { return true; };
                var rethrowExceptions = new HashSet<Type>();
                var ignoreExceptions = new HashSet<Type>() { typeof(GroupNotFoundException<String>) };
                await AwaitTaskCompletionAsync
                (
                    shardReadTasks,
                    taskToShardDescriptionMap,
                    resultAction,
                    continuePredicate,
                    rethrowExceptions,
                    ignoreExceptions,
                    "retrieve group to group reverse mappings from",
                    beginId,
                    new GetGroupToGroupReverseMappingsForGroupQueryTime()
                );
                metricLogger.End(beginId, new GetGroupToGroupReverseMappingsForGroupQueryTime());
                metricLogger.Increment(new GetGroupToGroupReverseMappingsForGroupQuery());

                return new List<String>(returnGroups);
            }
            else
            {
                IEnumerable<String> returnGroups = null;
                Guid beginId = metricLogger.Begin(new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                try
                {
                    returnGroups = await GetGroupToGroupReverseMappingsImplementationAsync(new List<String>() { group });
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                metricLogger.End(beginId, new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                metricLogger.Increment(new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery());
                var returnList = new List<String>();
                foreach(String currentReturnGroup in returnGroups)
                {
                    if (currentReturnGroup != group)
                    {
                        returnList.Add(currentReturnGroup);
                    }
                }

                return returnList;
            }
        }

        /// <inheritdoc/>
        public virtual async Task RemoveGroupToGroupMappingAsync(String fromGroup, String toGroup)
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
                new HashSet<Type>(),
                $"remove mapping between groups '{fromGroup}' and '{toGroup}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task AddUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
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
                new HashSet<Type>(),
                $"add a mapping between user '{user}' application component '{applicationComponent}' and access level '{accessLevel}' to"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(String user)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
            };
            var rethrowExceptions = new HashSet<Type>() { typeof(UserNotFoundException<String>) };
            var ignoreExceptions = new HashSet<Type>();

            return await GetElementsAsync
            (
                new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime(),
                new GetUserToApplicationComponentAndAccessLevelMappingsQuery(),
                DataElement.User,
                user,
                createTaskFunc,
                rethrowExceptions,
                ignoreExceptions,
                new List<Tuple<String, String>>(),
                $"retrieve user to application component and access level mappings for user '{user}' from"
            ); 
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetApplicationComponentAndAccessLevelToUserMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            IntervalMetric intervalMetric = null;
            CountMetric countMetric = null;
            if (includeIndirectMappings == false)
            {
                intervalMetric = new GetApplicationComponentAndAccessLevelToUserMappingsQueryTime();
                countMetric = new GetApplicationComponentAndAccessLevelToUserMappingsQuery();
            }
            else
            {
                intervalMetric = new GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime();
                countMetric = new GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery();
            }

            return await GetElementToUserMappingsAsync
            (
                applicationComponent,
                accessLevel,
                includeIndirectMappings,
                GetApplicationComponentAndAccessLevelToUserDirectMappingsAsync,
                GetApplicationComponentAndAccessLevelToGroupDirectMappingsAsync,
                intervalMetric,
                countMetric
            );
        }

        /// <inheritdoc/>
        public virtual async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
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
                new HashSet<Type>(),
                $"remove mapping between user '{user}' application component '{applicationComponent}' and access level '{accessLevel}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
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
                new HashSet<Type>(),
                $"add a mapping between group '{group}' application component '{applicationComponent}' and access level '{accessLevel}' to"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(String group)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
            };
            var rethrowExceptions = new HashSet<Type>();
            var ignoreExceptions = new HashSet<Type>() { typeof(GroupNotFoundException<String>) };

            return await GetElementsAsync
            (
                new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime(),
                new GetGroupToApplicationComponentAndAccessLevelMappingsQuery(),
                DataElement.Group,
                group,
                createTaskFunc,
                rethrowExceptions,
                ignoreExceptions,
                new List<Tuple<String, String>>(),
                $"retrieve group to application component and access level mappings for group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetApplicationComponentAndAccessLevelToGroupMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            IntervalMetric intervalMetric = null;
            CountMetric countMetric = null;
            if (includeIndirectMappings == false)
            {
                intervalMetric = new GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime();
                countMetric = new GetApplicationComponentAndAccessLevelToGroupMappingsQuery();
            }
            else
            {
                intervalMetric = new GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime();
                countMetric = new GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery();
            }

            return await GetElementToGroupMappingsAsync
            (
                applicationComponent,
                accessLevel,
                includeIndirectMappings,
                GetApplicationComponentAndAccessLevelToGroupDirectMappingsAsync,
                intervalMetric,
                countMetric
            );
        }

        /// <inheritdoc/>
        public virtual async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
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
                new HashSet<Type>(),
                $"remove mapping between group '{group}' application component '{applicationComponent}' and access level '{accessLevel}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task AddEntityTypeAsync(String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddEntityTypeAsync(entityType);
            };
            await ProcessEventAsync(new EntityTypeAddTime(), new EntityTypeAdded(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"add entity type '{entityType}' to");
        }

        /// <inheritdoc/>
        public virtual async Task<Boolean> ContainsEntityTypeAsync(String entityType)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () =>
            {
                var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
                clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));

                return clients;
            };
            Func<DistributedClientAndShardDescription, Task<Tuple<Boolean, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.ContainsEntityTypeAsync(entityType), Guid.NewGuid());
            };

            return await ContainsElementAsync(new ContainsEntityTypeQueryTime(), new ContainsEntityTypeQuery(), getClientsFunc, createTaskFunc, $"entity type '{entityType}'");
        }

        /// <inheritdoc/>
        public virtual async Task RemoveEntityTypeAsync(String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveEntityTypeAsync(entityType);
            };
            await ProcessEventAsync(new EntityTypeRemoveTime(), new EntityTypeRemoved(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"remove entity type '{entityType}' from");
        }

        /// <inheritdoc/>
        public virtual async Task AddEntityAsync(String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.AddEntityAsync(entityType, entity);
            };
            await ProcessEventAsync(new EntityAddTime(), new EntityAdded(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"add entity '{entity}' with type '{entityType}' to");
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetEntitiesAsync(String entityType)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesQueryTime());
            var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
            clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));
            HashSet<Task<Tuple<List<String>, Guid>>> shardReadTasks;
            Dictionary<Task<Tuple<List<String>, Guid>>, String> taskToShardDescriptionMap;
            Func<DistributedClientAndShardDescription, Task<Tuple<List<String>, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.GetEntitiesAsync(entityType), Guid.NewGuid());
            };
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<Tuple<List<String>, Guid>>(clients, createTaskFunc);
            var aggregatedEntities = new HashSet<String>();
            Action<Tuple<List<String>, Guid>> resultAction = (Tuple<List<String>, Guid> entities) =>
            {
                aggregatedEntities.UnionWith(entities.Item1);
            };
            Func<Tuple<List<String>, Guid>, Boolean> continuePredicate = (Tuple<List<String>, Guid> entities) => { return true; };
            var ignoreExceptions = new HashSet<Type>() { typeof(EntityTypeNotFoundException) };
            await AwaitTaskCompletionAsync
            (
                shardReadTasks, 
                taskToShardDescriptionMap, 
                resultAction, 
                continuePredicate,
                new HashSet<Type>(),
                ignoreExceptions, 
                $"retrieve entities of type '{entityType}' from", 
                beginId, 
                new GetEntitiesQueryTime()
            );
            metricLogger.End(beginId, new GetEntitiesQueryTime());
            metricLogger.Increment(new GetEntitiesQuery());

            return new List<String>(aggregatedEntities);
        }

        /// <inheritdoc/>
        public virtual async Task<Boolean> ContainsEntityAsync(String entityType, String entity)
        {
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc = () =>
            {
                var clients = new List<DistributedClientAndShardDescription>(shardClientManager.GetAllClients(DataElement.User, Operation.Query));
                clients.AddRange(shardClientManager.GetAllClients(DataElement.Group, Operation.Query));

                return clients;
            };
            Func<DistributedClientAndShardDescription, Task<Tuple<Boolean, Guid>>> createTaskFunc = async (DistributedClientAndShardDescription clientAndDescription) =>
            {
                return Tuple.Create(await clientAndDescription.Client.ContainsEntityAsync(entityType, entity), Guid.NewGuid());
            };

            return await ContainsElementAsync(new ContainsEntityQueryTime(), new ContainsEntityQuery(), getClientsFunc, createTaskFunc, $"entity '{entity}' with type '{entityType}'");
        }

        /// <inheritdoc/>
        public virtual async Task RemoveEntityAsync(String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventAction = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
            {
                await client.RemoveEntityAsync(entityType, entity);
            };
            await ProcessEventAsync(new EntityRemoveTime(), new EntityRemoved(), new List<DataElement>() { DataElement.User, DataElement.Group }, eventAction, $"remove entity '{entity}' with type '{entityType}' from");
        }

        /// <inheritdoc/>
        public virtual async Task AddUserToEntityMappingAsync(String user, String entityType, String entity)
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
                new HashSet<Type>(),
                $"add a mapping between user '{user}' entity type '{entityType}' and entity '{entity}' to"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(String user)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetUserToEntityMappingsAsync(user);
            };
            var rethrowExceptions = new HashSet<Type>() { typeof(UserNotFoundException<String>) };
            var ignoreExceptions = new HashSet<Type>();

            return await GetElementsAsync
            (
                new GetUserToEntityMappingsForUserQueryTime(),
                new GetUserToEntityMappingsForUserQuery(),
                DataElement.User,
                user,
                createTaskFunc,
                rethrowExceptions,
                ignoreExceptions,
                new List<Tuple<String, String>>(), 
                $"retrieve user to entity mappings for user '{user}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetUserToEntityMappingsAsync(String user, String entityType)
        {
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetUserToEntityMappingsAsync(user, entityType);
            };
            var rethrowExceptions = new HashSet<Type>() { typeof(UserNotFoundException<String>) };
            var ignoreExceptions = new HashSet<Type>() { typeof(EntityTypeNotFoundException) } ;

            return await GetElementsAsync
            (
                new GetUserToEntityMappingsForUserAndEntityTypeQueryTime(),
                new GetUserToEntityMappingsForUserAndEntityTypeQuery(),
                DataElement.User,
                user,
                createTaskFunc,
                rethrowExceptions,
                ignoreExceptions,
                new List<String>(),
                $"retrieve user to entity mappings for user '{user}' and entity type '{entityType}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetEntityToUserMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            IntervalMetric intervalMetric = null;
            CountMetric countMetric = null;
            if (includeIndirectMappings == false)
            {
                intervalMetric = new GetEntityToUserMappingsQueryTime();
                countMetric = new GetEntityToUserMappingsQuery();
            }
            else
            {
                intervalMetric = new GetEntityToUserMappingsWithIndirectMappingsQueryTime();
                countMetric = new GetEntityToUserMappingsWithIndirectMappingsQuery();
            }

            return await GetElementToUserMappingsAsync
            (
                entityType,
                entity,
                includeIndirectMappings,
                GetEntityToUserDirectMappingsAsync,
                GetEntityToGroupDirectMappingsAsync,
                intervalMetric,
                countMetric
            );
        }

        /// <inheritdoc/>
        public virtual async Task RemoveUserToEntityMappingAsync(String user, String entityType, String entity)
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
                new HashSet<Type>(),
                $"remove mapping between user '{user}' entity type '{entityType}' and entity '{entity}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task AddGroupToEntityMappingAsync(String group, String entityType, String entity)
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
                new HashSet<Type>(),
                $"add a mapping between group '{group}' entity type '{entityType}' and entity '{entity}' to"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(String group)
        {
            Func<DistributedClientAndShardDescription, Task<List<Tuple<String, String>>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetGroupToEntityMappingsAsync(group);
            };
            var rethrowExceptions = new HashSet<Type>();
            var ignoreExceptions = new HashSet<Type>() { typeof(GroupNotFoundException<String>) };

            return await GetElementsAsync
            (
                new GetGroupToEntityMappingsForGroupQueryTime(),
                new GetGroupToEntityMappingsForGroupQuery(),
                DataElement.Group,
                group,
                createTaskFunc,
                rethrowExceptions,
                ignoreExceptions,
                new List<Tuple<String, String>>(),
                $"retrieve group to entity mappings for group '{group}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetGroupToEntityMappingsAsync(String group, String entityType)
        {
            Func<DistributedClientAndShardDescription, Task<List<String>>> createTaskFunc = async (DistributedClientAndShardDescription client) =>
            {
                return await client.Client.GetGroupToEntityMappingsAsync(group, entityType);
            };
            var rethrowExceptions = new HashSet<Type>();
            var ignoreExceptions = new HashSet<Type>() { typeof(GroupNotFoundException<String>), typeof(EntityTypeNotFoundException) };

            return await GetElementsAsync
            (
                new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime(),
                new GetGroupToEntityMappingsForGroupAndEntityTypeQuery(),
                DataElement.Group,
                group,
                createTaskFunc,
                rethrowExceptions,
                ignoreExceptions,
                new List<String>(),
                $"retrieve group to entity mappings for group '{group}' and entity type '{entityType}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetEntityToGroupMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            IntervalMetric intervalMetric = null;
            CountMetric countMetric = null;
            if (includeIndirectMappings == false)
            {
                intervalMetric = new GetEntityToGroupMappingsQueryTime();
                countMetric = new GetEntityToGroupMappingsQuery();
            }
            else
            {
                intervalMetric = new GetEntityToGroupMappingsWithIndirectMappingsQueryTime();
                countMetric = new GetEntityToGroupMappingsWithIndirectMappingsQuery();
            }

            return await GetElementToGroupMappingsAsync
            (
                entityType,
                entity,
                includeIndirectMappings, 
                GetEntityToGroupDirectMappingsAsync,
                intervalMetric,
                countMetric
            );
        }

        /// <inheritdoc/>
        public virtual async Task RemoveGroupToEntityMappingAsync(String group, String entityType, String entity)
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
                new HashSet<Type>(),
                $"remove mapping between group '{group}' entity type '{entityType}' and entity '{entity}' from"
            );
        }

        /// <inheritdoc/>
        public virtual async Task<Boolean> HasAccessToApplicationComponentAsync(String user, String applicationComponent, String accessLevel)
        {
            Guid beginId = metricLogger.Begin(new HasAccessToApplicationComponentForUserQueryTime());
            Boolean result = false;
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                DistributedClientAndShardDescription userClientAndDescription = shardClientManager.GetClient(DataElement.User, Operation.Query, user);
                // Get the groups mapped directly to the user
                List<String> mappedGroups = null;
                try
                {
                    mappedGroups = await userClientAndDescription.Client.GetUserToGroupMappingsAsync(user, false);
                }
                catch (UserNotFoundException<String>)
                {
                    queryMetricData = new ExecuteQueryAgainstGroupShardsMetricData(0, 0);
                    throw;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to retrieve user to group mappings from shard with configuration '{userClientAndDescription.ShardConfigurationDescription}'.", e);
                }
                // Below we're returning a tuple containing a Boolean and Guid from the query methods rather than just the Boolean that the HasAccessToApplicationComponentAsync() method actually returns.
                // As per article https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/task-fromresult-returns-singleton, the Task.FromResult() method returns singleton Task
                // instances for simple types like bools.  The ExecuteQueryAgainstGroupShards() method attempts to store executed tasks as the key in a Dictionary, but was giving duplicate key errors
                // when attempting to insert.  Even though Task.FromResult() is not used in ExecuteQueryAgainstGroupShards(), I'm assuming similar returning of singletons may also occur from the 
                // Task.WhenAny() method which is called within ExecuteQueryAgainstGroupShards().  Hence to circumvent this, the functions below return a Boolean/Guid tuple which generates a unique Task 
                // for each query.  This seems to also occur with other data types, hence same approach is applied to all HasAccessTo*() and Get*AccessibleBy*() methods.
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<Boolean, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    Boolean groupShardresult = await clientAndDescription.Client.HasAccessToApplicationComponentAsync(groups, applicationComponent, accessLevel);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Task<Tuple<Boolean, Guid>> userTask = Task.Run<Tuple<Boolean, Guid>>(async () =>
                {
                    Boolean userShardResult = await userClientAndDescription.Client.HasAccessToApplicationComponentAsync(user, applicationComponent, accessLevel);
                    return Tuple.Create(userShardResult, Guid.NewGuid());
                });
                var userTaskAndShardDescription = new Tuple<Task<Tuple<Boolean, Guid>>, String>(userTask, userClientAndDescription.ShardConfigurationDescription);
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
                    mappedGroups,
                    createQueryTaskFunc, 
                    new List<Tuple<Task<Tuple<Boolean, Guid>>, String>>() { userTaskAndShardDescription },
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    true, 
                    $"check access to application component '{applicationComponent}' at access level '{accessLevel}' in", 
                    true
                );
            }
            catch (UserNotFoundException<String>)
            {
                // Return false in this case
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToApplicationComponentForUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToApplicationComponentForUserQueryTime());
            metricLogger.Increment(new HasAccessToApplicationComponentForUserQuery());
            metricLogger.Add(new HasAccessToApplicationComponentGroupsMappedToUser(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new HasAccessToApplicationComponentGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<Boolean> HasAccessToEntityAsync(String user, String entityType, String entity)
        {
            Guid beginId = metricLogger.Begin(new HasAccessToEntityForUserQueryTime());
            Boolean result = false;
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                DistributedClientAndShardDescription userClientAndDescription = shardClientManager.GetClient(DataElement.User, Operation.Query, user);
                // Get the groups mapped directly to the user
                List<String> mappedGroups = null;
                try
                {
                    mappedGroups = await userClientAndDescription.Client.GetUserToGroupMappingsAsync(user, false);
                }
                catch (UserNotFoundException<String>)
                {
                    queryMetricData = new ExecuteQueryAgainstGroupShardsMetricData(0, 0);
                    throw;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to retrieve user to group mappings from shard with configuration '{userClientAndDescription.ShardConfigurationDescription}'.", e);
                }
                // See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<Boolean, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    Boolean groupShardresult = await clientAndDescription.Client.HasAccessToEntityAsync(groups, entityType, entity);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Task<Tuple<Boolean, Guid>> userTask = Task.Run<Tuple<Boolean, Guid>>(async () =>
                {
                    Boolean userShardResult = await userClientAndDescription.Client.HasAccessToEntityAsync(user, entityType, entity);
                    return Tuple.Create(userShardResult, Guid.NewGuid());
                });
                var userTaskAndShardDescription = new Tuple<Task<Tuple<Boolean, Guid>>, String>(userTask, userClientAndDescription.ShardConfigurationDescription);
                Action<Tuple<Boolean, Guid>> resultAction = (Tuple<Boolean, Guid> hasAccess) =>
                {
                    if (hasAccess.Item1 == true)
                    {
                        result = true;
                    }
                };
                Func<Tuple<Boolean, Guid>, Boolean> continuePredicate = (Tuple<Boolean, Guid> hasAccess) => { return !hasAccess.Item1; };
                var rethrowExceptions = new HashSet<Type>();
                var ignoreExceptions = new HashSet<Type>() { typeof(EntityTypeNotFoundException), typeof(EntityNotFoundException) };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    mappedGroups,
                    createQueryTaskFunc,
                    new List<Tuple<Task<Tuple<Boolean, Guid>>, String>>() { userTaskAndShardDescription },
                    resultAction,
                    continuePredicate,
                    rethrowExceptions,
                    ignoreExceptions,
                    true,
                    $"check access to entity '{entity}' with type '{entityType}' in", 
                    true
                );
            }
            catch (UserNotFoundException<String>)
            {
                // Return false in this case
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new HasAccessToEntityForUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new HasAccessToEntityForUserQueryTime());
            metricLogger.Increment(new HasAccessToEntityForUserQuery());
            metricLogger.Add(new HasAccessToEntityGroupsMappedToUser(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new HasAccessToEntityGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return result;
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByUserAsync(String user)
        {
            Guid beginId = metricLogger.Begin(new GetApplicationComponentsAccessibleByUserQueryTime());
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                DistributedClientAndShardDescription userClientAndDescription = shardClientManager.GetClient(DataElement.User, Operation.Query, user);
                // Get the groups mapped directly to the user
                List<String> mappedGroups = null;
                try
                {
                    mappedGroups = await userClientAndDescription.Client.GetUserToGroupMappingsAsync(user, false);
                }
                catch (UserNotFoundException<String>)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to retrieve user to group mappings from shard with configuration '{userClientAndDescription.ShardConfigurationDescription}'.", e);
                }
                // See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<Tuple<String, String>>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    List<Tuple<String, String>> groupShardresult = await clientAndDescription.Client.GetApplicationComponentsAccessibleByGroupsAsync(groups);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                }; 
                Task<Tuple<List<Tuple<String, String>>, Guid>> userTask = Task.Run<Tuple<List<Tuple<String, String>>, Guid>>(async () =>
                {
                    List<Tuple<String, String>> userShardResult = await userClientAndDescription.Client.GetApplicationComponentsAccessibleByUserAsync(user);
                    return Tuple.Create(userShardResult, Guid.NewGuid());
                });
                var userTaskAndShardDescription = new Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>(userTask, userClientAndDescription.ShardConfigurationDescription);
                Action<Tuple<List<Tuple<String, String>>, Guid>> resultAction = (Tuple<List<Tuple<String, String>>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<Tuple<String, String>>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    mappedGroups,
                    createQueryTaskFunc,
                    new List<Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>> () { userTaskAndShardDescription },
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    true,
                    $"retrieve application component and access level mappings for user '{user}' from", 
                    true
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetApplicationComponentsAccessibleByUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetApplicationComponentsAccessibleByUserQueryTime());
            metricLogger.Increment(new GetApplicationComponentsAccessibleByUserQuery());
            metricLogger.Add(new GetApplicationComponentsAccessibleByUserGroupsMappedToUser(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new GetApplicationComponentsAccessibleByUserGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupAsync(String group)
        {
            Guid beginId = metricLogger.Begin(new GetApplicationComponentsAccessibleByGroupQueryTime());
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                // See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<Tuple<String, String>>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    List<Tuple<String, String>> groupShardresult = await clientAndDescription.Client.GetApplicationComponentsAccessibleByGroupsAsync(groups);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<List<Tuple<String, String>>, Guid>> resultAction = (Tuple<List<Tuple<String, String>>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<Tuple<String, String>>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    new List<String>() { group },
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    true,
                    $"retrieve application component and access level mappings for group '{group}' from",
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetApplicationComponentsAccessibleByGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetApplicationComponentsAccessibleByGroupQueryTime());
            metricLogger.Increment(new GetApplicationComponentsAccessibleByGroupQuery());
            metricLogger.Add(new GetApplicationComponentsAccessibleByGroupGroupsMappedToGroup(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new GetApplicationComponentsAccessibleByGroupGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(String user)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByUserQueryTime());
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                DistributedClientAndShardDescription userClientAndDescription = shardClientManager.GetClient(DataElement.User, Operation.Query, user);
                // Get the groups mapped directly to the user
                List<String> mappedGroups = null;
                try
                {
                    mappedGroups = await userClientAndDescription.Client.GetUserToGroupMappingsAsync(user, false);
                }
                catch (UserNotFoundException<String>)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to retrieve user to group mappings from shard with configuration '{userClientAndDescription.ShardConfigurationDescription}'.", e);
                }
                // See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<Tuple<String, String>>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    List<Tuple<String, String>> groupShardresult = await clientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(groups);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Task<Tuple<List<Tuple<String, String>>, Guid>> userTask = Task.Run<Tuple<List<Tuple<String, String>>, Guid>>(async () =>
                {
                    List<Tuple<String, String>> userShardResult = await userClientAndDescription.Client.GetEntitiesAccessibleByUserAsync(user);
                    return Tuple.Create(userShardResult, Guid.NewGuid());
                });
                var userTaskAndShardDescription = new Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>(userTask, userClientAndDescription.ShardConfigurationDescription);
                Action<Tuple<List<Tuple<String, String>>, Guid>> resultAction = (Tuple<List<Tuple<String, String>>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<Tuple<String, String>>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    mappedGroups,
                    createQueryTaskFunc,
                    new List<Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>>() { userTaskAndShardDescription },
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    true,
                    $"retrieve entity mappings for user '{user}' from", 
                    true
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByUserQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByUserQuery());
            metricLogger.Add(new GetEntitiesAccessibleByUserGroupsMappedToUser(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new GetEntitiesAccessibleByUserGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetEntitiesAccessibleByUserAsync(String user, String entityType)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByUserQueryTime());
            var result = new HashSet<String>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                DistributedClientAndShardDescription userClientAndDescription = shardClientManager.GetClient(DataElement.User, Operation.Query, user);
                // Get the groups mapped directly to the user
                List<String> mappedGroups = null;
                try
                {
                    mappedGroups = await userClientAndDescription.Client.GetUserToGroupMappingsAsync(user, false);
                }
                catch (UserNotFoundException<String>)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to retrieve user to group mappings from shard with configuration '{userClientAndDescription.ShardConfigurationDescription}'.", e);
                }
                // See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<String>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    List<String> groupShardresult = await clientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(groups, entityType);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Task<Tuple<List<String>, Guid>> userTask = Task.Run<Tuple<List<String>, Guid>>(async () =>
                {
                    List<String> userShardResult = await userClientAndDescription.Client.GetEntitiesAccessibleByUserAsync(user, entityType);
                    return Tuple.Create(userShardResult, Guid.NewGuid());
                });
                var userTaskAndShardDescription = new Tuple<Task<Tuple<List<String>, Guid>>, String>(userTask, userClientAndDescription.ShardConfigurationDescription);
                Action<Tuple<List<String>, Guid>> resultAction = (Tuple<List<String>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<String>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                var rethrowExceptions = new HashSet<Type>();
                var ignoreExceptions = new HashSet<Type>() { typeof(EntityTypeNotFoundException) };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    mappedGroups,
                    createQueryTaskFunc,
                    new List<Tuple<Task<Tuple<List<String>, Guid>>, String>>() { userTaskAndShardDescription },
                    resultAction,
                    continuePredicate,
                    rethrowExceptions,
                    ignoreExceptions,
                    true,
                    $"retrieve entity mappings for user '{user}' and entity type '{entityType}' from", 
                    true
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByUserQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByUserQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByUserQuery());
            metricLogger.Add(new GetEntitiesAccessibleByUserGroupsMappedToUser(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new GetEntitiesAccessibleByUserGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<String>(result);
        }

        /// <inheritdoc/>
        public virtual async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(String group)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupQueryTime());
            var result = new HashSet<Tuple<String, String>>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                // See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<Tuple<String, String>>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    List<Tuple<String, String>> groupShardresult = await clientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(groups);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<List<Tuple<String, String>>, Guid>> resultAction = (Tuple<List<Tuple<String, String>>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<Tuple<String, String>>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    new List<String>() { group },
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<List<Tuple<String, String>>, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    true,
                    $"retrieve entity mappings for group '{group}' from", 
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupQuery());
            metricLogger.Add(new GetEntitiesAccessibleByGroupGroupsMappedToGroup(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new GetEntitiesAccessibleByGroupGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<Tuple<String, String>>(result);
        }

        /// <inheritdoc/>
        public virtual async Task<List<String>> GetEntitiesAccessibleByGroupAsync(String group, String entityType)
        {
            Guid beginId = metricLogger.Begin(new GetEntitiesAccessibleByGroupQueryTime());
            var result = new HashSet<String>();
            ExecuteQueryAgainstGroupShardsMetricData queryMetricData = null;
            try
            {
                // See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type
                Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<Tuple<List<String>, Guid>>> createQueryTaskFunc = async (DistributedClientAndShardDescription clientAndDescription, IEnumerable<String> groups) =>
                {
                    List<String> groupShardresult = await clientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(groups, entityType);
                    return Tuple.Create(groupShardresult, Guid.NewGuid());
                };
                Action<Tuple<List<String>, Guid>> resultAction = (Tuple<List<String>, Guid> groupShardResult) =>
                {
                    result.UnionWith(groupShardResult.Item1);
                };
                Func<Tuple<List<String>, Guid>, Boolean> continuePredicate = (groupShardResult) => { return true; };
                queryMetricData = await ExecuteQueryAgainstGroupShards
                (
                    new List<String>() { group },
                    createQueryTaskFunc,
                    Enumerable.Empty<Tuple<Task<Tuple<List<String>, Guid>>, String>>(),
                    resultAction,
                    continuePredicate,
                    new HashSet<Type>(),
                    new HashSet<Type>(),
                    true,
                    $"retrieve entity mappings for group '{group}' and entity type {entityType} from", 
                    false
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new GetEntitiesAccessibleByGroupQueryTime());
                throw;
            }
            metricLogger.End(beginId, new GetEntitiesAccessibleByGroupQueryTime());
            metricLogger.Increment(new GetEntitiesAccessibleByGroupQuery());
            metricLogger.Add(new GetEntitiesAccessibleByGroupGroupsMappedToGroup(), queryMetricData.GroupsMappedToGroups);
            metricLogger.Add(new GetEntitiesAccessibleByGroupGroupShardsQueried(), queryMetricData.GroupShardsQueried);

            return new List<String>(result);
        }

        /// <summary>
        /// Refreshes the internally stored shard configuration with the specified shard configuration if the configurations differ (if they are the same, no refresh is performed).
        /// </summary>
        /// <param name="shardConfiguration">The updated shard configuration.</param>
        /// <exception cref="ShardConfigurationRefreshException">An exception occurred whilst attempting to refresh/update the shard configuration.</exception>
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
    }
}
