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
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Base for classes which coordinate operations in an AccessManager implementation where responsibility for subsets of elements is distributed across multiple computers in shards.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public abstract class DistributedAccessManagerOperationCoordinatorBase<TClientConfiguration>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        /// <summary>Manages the clients used to connect to shards managing the subsets of elements in the distributed access manager implementation.</summary>
        protected IShardClientManager<TClientConfiguration> shardClientManager;
        /// <summary>The hash code generator for users.</summary>
        protected IHashCodeGenerator<String> userHashCodeGenerator;
        /// <summary>The hash code generator for groups.</summary>
        protected IHashCodeGenerator<String> groupHashCodeGenerator;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerOperationCoordinatorBase class.
        /// </summary>
        /// <param name="shardClientManager">Manages the clients used to connect to shards managing the subsets of elements in the distributed access manager implementation.</param>
        /// <param name="userHashCodeGenerator">Hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">Hash code generator for groups.</param>
        /// <param name="metricLogger">Logger for metrics.</param>
        public DistributedAccessManagerOperationCoordinatorBase
        (
            IShardClientManager<TClientConfiguration> shardClientManager,
            IHashCodeGenerator<String> userHashCodeGenerator,
            IHashCodeGenerator<String> groupHashCodeGenerator,
            IMetricLogger metricLogger
        )
        {
            this.shardClientManager = shardClientManager;
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.metricLogger = metricLogger;
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
        /// <param name="rethrowExceptions">A set of exceptions which should be rethrown directly if caught when processing the event against the shard.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "remove user 'user1' from".</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        /// <remarks>Used by methods which execute an event against a single, specific shard, e.g. AddUserToApplicationComponentAndAccessLevelMappingAsync().</remarks>
        protected async Task ProcessEventAsync
        (
            IntervalMetric intervalMetric,
            CountMetric countMetric,
            DataElement dataElement,
            String elementValue,
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> eventFunc,
            HashSet<Type> rethrowExceptions,
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
                if (rethrowExceptions.Contains(e.GetType()) == true)
                {
                    throw;
                }
                throw new Exception($"Failed to {exceptionEventDescription} shard with configuration '{client.ShardConfigurationDescription}'.", e);
            }
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);
        }

        /// <summary>
        /// Processes a specified event against multiple shards in the distributed environment.
        /// </summary>
        /// <param name="intervalMetric">An interval metric to log as part of processing.</param>
        /// <param name="countMetric">A count metric to log as part of processing.</param>
        /// <param name="dataElements">The types of the element managed by the shards to process the event against.</param>
        /// <param name="eventFunc">A function to execute against the client which connects to each shard which manage the element type, and which processes the event.  Accepts a single parameter which is the current client, and returns a <see cref="Task"/>.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "remove user 'user1' from".</param>
        /// <returns>The task object representing the asynronous operation.</returns>
        /// <remarks>Used by methods which execute and event against multiple shards, e.g. RemoveUserAsync().</remarks>
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
                exceptionEventDescription,
                beginId,
                intervalMetric
            );
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);
        }

        /// <summary>
        /// Returns true if an element exists in the distributed environment.
        /// </summary>
        /// <param name="intervalMetric">An interval metric to log as part of the check.</param>
        /// <param name="countMetric">A count metric to log after checking for the element.</param>
        /// <param name="getClientsFunc">A function which returns a collection of <see cref="DistributedClientAndShardDescription"/> containing the clients to use to connect to all shards in which to check for the element.</param>
        /// <param name="createTaskFunc">A function which accepts a client which connects to a shard (and associated description of the shard), and returns a task which resolves to a boolean (and a GUID) where the boolean holds whether that shard contains the element.</param>
        /// <param name="exceptionElementAndValue">The name and value of the element to use in exception messages.  E.g. "entity type 'Clients'".</param>
        /// <returns>Whether any of the shards in the distributed environment contained the element.</returns>
        /// <remarks>
        ///   <para>Used by methods like ContainsUserAsync().</para>
        ///   <para>See comment in method HasAccessToApplicationComponentAsync() explaining need for Tuple with Boolean in return type.</para>
        /// </remarks>
        protected async Task<Boolean> ContainsElementAsync
        (
            QueryIntervalMetric intervalMetric,
            QueryCountMetric countMetric,
            Func<IEnumerable<DistributedClientAndShardDescription>> getClientsFunc,
            Func<DistributedClientAndShardDescription, Task<Tuple<Boolean, Guid>>> createTaskFunc,
            String exceptionElementAndValue
        )
        {
            Guid beginId = metricLogger.Begin(intervalMetric);
            IEnumerable<DistributedClientAndShardDescription> clients = getClientsFunc();
            HashSet<Task<Tuple<Boolean, Guid>>> shardTasks;
            Dictionary<Task<Tuple<Boolean, Guid>>, String> taskToShardDescriptionMap;
            (shardTasks, taskToShardDescriptionMap) = CreateTasks<Tuple<Boolean, Guid>>(clients, createTaskFunc);
            Boolean result = false;
            Action<Tuple<Boolean, Guid>> resultAction = (Tuple<Boolean, Guid> shardResult) =>
            {
                if (shardResult.Item1 == true)
                {
                    result = true;
                }
            };
            Func<Tuple<Boolean, Guid>, Boolean> continuePredicate = (Tuple<Boolean, Guid> shardResult) => { return !(shardResult.Item1); };
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
                $"check for {exceptionElementAndValue} in",
                beginId,
                intervalMetric
            );
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);

            return result;
        }

        /// <summary>
        /// Returns data from a single shard in the distributed environment.
        /// </summary>
        /// <typeparam name="T">The type of data returned from the shard.</typeparam>
        /// <param name="intervalMetric">An interval metric to log as part of the query.</param>
        /// <param name="countMetric">A count metric to log after returning the data.</param>
        /// <param name="dataElement">The type of the element to retrieve the data for.</param>
        /// <param name="elementValue">The value of the element.</param>
        /// <param name="createTaskFunc">A function which accepts a client which connects to a shard (and associated description of the shard), and returns a task which resolves to the type of data to return.</param>
        /// <param name="rethrowExceptions">A set of exceptions which should be rethrown directly if caught when retrieving data from the shard.</param>
        /// <param name="ignoreExceptions">A set of exceptions which should be ignored if caught when retrieving data from the shard.</param>
        /// <param name="defaultReturnValue">The default return value, returned when an ignored exception occurs.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "retrieve user to group mappings for user 'user1' from".</param>
        /// <returns>The data elements.</returns>
        /// <remarks>Used by methods like GetUserToEntityMappingsAsync().</remarks>
        protected async Task<T> GetElementsAsync<T>
        (
            QueryIntervalMetric intervalMetric,
            QueryCountMetric countMetric,
            DataElement dataElement,
            String elementValue,
            Func<DistributedClientAndShardDescription, Task<T>> createTaskFunc,
            HashSet<Type> rethrowExceptions,
            HashSet<Type> ignoreExceptions,
            T defaultReturnValue,
            String exceptionEventDescription
        )
        {
            Guid beginId = metricLogger.Begin(intervalMetric);
            DistributedClientAndShardDescription client = shardClientManager.GetClient(dataElement, Operation.Query, elementValue);
            Task<T> shardTask = createTaskFunc(client);
            T result = defaultReturnValue;
            try
            {
                result = await shardTask;
            }
            catch (Exception e)
            {
                if (ignoreExceptions.Contains(e.GetType()) == false)
                {
                    metricLogger.CancelBegin(beginId, intervalMetric);
                    if (rethrowExceptions.Contains(e.GetType()) == true)
                    {
                        throw;
                    }
                    throw new Exception($"Failed to {exceptionEventDescription} shard with configuration '{client.ShardConfigurationDescription}'.", e);
                }
            }
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);

            return result;
        }

        /// <summary>
        /// Executes a specified query against the shards which manage sets of groups.
        /// </summary>
        /// <typeparam name="T">The type of data returned by the query.</typeparam>
        /// <param name="groups">The groups to run the query for.</param>
        /// <param name="createQueryTaskFunc">A function which creates a task which executes the query and returns the results from a single group node.  Accepts 2 parameters: the client (and its description) which connects to the group shard, and the subset of the <paramref name="groups"/> parameter which are managed by that shard, and returns a task which returns the results of the query.</param>
        /// <param name="additionalTasksAndShardDescriptions">A collection of additional functions and descriptions of the shards they're executed against, which are executed alongside those in parameter <paramref name="createQueryTaskFunc"/>, and which returns the same type as the queries against the group nodes.  This can be used for queries which must also be executed against a user node to give a complete result (e.g. in method GetEntitiesAccessibleByUserAsync()).</param>
        /// <param name="resultAction">An action to invoke with the results of each query task.</param>
        /// <param name="continuePredicate">A function which returns a boolean which is called after the completion of each query task and subdequent processing of its results, and which indicates whether further tasks shouled be waited for.  Accepts a single parameter which is the result of each task.</param>
        /// <param name="rethrowExceptions">A set of exceptions which should be rethrown directly if caught when executing a query against a shard.</param>
        /// <param name="ignoreExceptions">A set of exceptions which should be ignored if caught when executing a query against a shard.</param>
        /// <param name="includeIndirectlyMappedGroups">Whether to lookup and include groups which are indirectly mapped to the groups in the <paramref name="groups"/> parameter, as part of the query.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error when executing the query.  E.g. "retrieve application components and access level for user 'user1' from".</param>
        /// <param name="includeParameterGroupsInMappingCount">Whether the groups in the <paramref name="groups"/> parameter should be included in the count of mapped groups in the returned metric data.</param>
        /// <returns>An <see cref="ExecuteQueryAgainstGroupShardsMetricData"/> instance.</returns>
        /// <remarks>Used by methods which execute queries against multiple group shards, e.g. HasAccessToApplicationComponentAsync(), GetEntitiesAccessibleByGroupAsync().</remarks>
        protected async Task<ExecuteQueryAgainstGroupShardsMetricData> ExecuteQueryAgainstGroupShards<T>
        (
            IEnumerable<String> groups,
            Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<T>> createQueryTaskFunc,
            IEnumerable<Tuple<Task<T>, String>> additionalTasksAndShardDescriptions,
            Action<T> resultAction,
            Func<T, Boolean> continuePredicate,
            HashSet<Type> rethrowExceptions,
            HashSet<Type> ignoreExceptions,
            Boolean includeIndirectlyMappedGroups, 
            String exceptionEventDescription,
            Boolean includeParameterGroupsInMappingCount
        )
        {
            Int32 groupsMappedToGroups = 0;
            Int32 groupShardsQueried = 0;
            // Get the groups and their clients mapped indirectly to the inputted groups
            IEnumerable<String> allMappedGroups;
            if (includeIndirectlyMappedGroups == true)
            {
                (allMappedGroups, groupsMappedToGroups) = await GetUniqueGroupToGroupMappingsAsync(groups, includeParameterGroupsInMappingCount);
            }
            else
            {
                allMappedGroups = groups;
            }
            IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> allClientsAndGroups = shardClientManager.GetClients(DataElement.Group, Operation.Query, allMappedGroups);
            // TODO: This is an O(n) operation, as it's not expected there would be a huge number of group shards.  However may want to look at improving this.
            groupShardsQueried = allClientsAndGroups.Count();
            // Create tasks which query the group shards
            HashSet<Task<T>> shardReadTasks;
            Dictionary<Task<T>, String> taskToShardDescriptionMap;
            (shardReadTasks, taskToShardDescriptionMap) = CreateTasks<T>(allClientsAndGroups, createQueryTaskFunc);
            // Add the specified additional tasks to the collection of tasks
            foreach (Tuple<Task<T>, String> currentAdditionalTaskAndShardDescription in additionalTasksAndShardDescriptions)
            {
                shardReadTasks.Add(currentAdditionalTaskAndShardDescription.Item1);
                taskToShardDescriptionMap.Add(currentAdditionalTaskAndShardDescription.Item1, currentAdditionalTaskAndShardDescription.Item2);
            }
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

            return new ExecuteQueryAgainstGroupShardsMetricData(groupsMappedToGroups, groupShardsQueried);
        }

        /// <summary>
        /// Creates a set of tasks which perform an operation on each of a collection of shards.
        /// </summary>
        /// <typeparam name="T">The type of data returned by the tasks.</typeparam>
        /// <param name="clients">A collection of <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> (and descriptions of the shards they connect to) to execute the operation against.</param>
        /// <param name="createTaskFunc">The function which creates each task.  Accepts a <see cref="DistributedClientAndShardDescription"/> containing the client to perform the operation against, and returns the task.</param>
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
        /// Creates a set of tasks which perform an operation on each of a collection of shards, and where that operation accepts a collection of strings as its parameter.
        /// </summary>
        /// <typeparam name="T">The type of data returned by the tasks.</typeparam>
        /// <param name="clientsAndStringParameters">A collection of tuples containing: a <see cref="IDistributedAccessManagerAsyncClient{TUser, TGroup, TComponent, TAccess}"/> (and descriptions of the shards they connect to), and a collection of strings to pass as a parameter to the operation performed against that client.</param>
        /// <param name="createTaskFunc">The function which creates each task.  Accepts 2 parameters: a <see cref="DistributedClientAndShardDescription"/> containing the client to perform the operation against, and the string collection parameter for the operation, and returns the task.</param>
        /// <returns>A tuple containing: a HashSet containing the created tasks, and a dictionary which maps each of the created tasks to the description of the shard the client which created the task connected to.</returns>
        protected (HashSet<Task<T>>, Dictionary<Task<T>, String>) CreateTasks<T>(IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> clientsAndStringParameters, Func<DistributedClientAndShardDescription, IEnumerable<String>, Task<T>> createTaskFunc)
        {
            var shardReadTasks = new HashSet<Task<T>>();
            var taskToShardDescriptionMap = new Dictionary<Task<T>, String>();
            foreach (Tuple<DistributedClientAndShardDescription, IEnumerable<String>> currentClientAndParameter in clientsAndStringParameters)
            {
                Task<T> currentTask = createTaskFunc(currentClientAndParameter.Item1, currentClientAndParameter.Item2);
                taskToShardDescriptionMap.Add(currentTask, currentClientAndParameter.Item1.ShardConfigurationDescription);
                shardReadTasks.Add(currentTask);
            }

            return (shardReadTasks, taskToShardDescriptionMap);
        }

        /// <summary>
        /// Gets a unique collection of groups which are mapped both directly and indirectly a specified collection of groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mappings for.</param>
        /// <param name="includeParameterGroupsInMappingCount">Whether the groups in the <paramref name="groups"/> parameter should be included in the returned count of mapped groups.</param>
        /// <returns>A tuple containing: a unique collection of groups which includes both the groups passed in the <paramref name="groups"/> parameter and the groups those groups are mapped to, and a count of those groups.</returns>
        protected async Task<(IEnumerable<String>, Int32)> GetUniqueGroupToGroupMappingsAsync(IEnumerable<String> groups, Boolean includeParameterGroupsInMappingCount)
        {
            var returnGroups = new HashSet<String>(groups);
            // Get the groups and their clients mapped indirectly to the user
            IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> directlyMappedClientsAndGroups = shardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, groups);
            // Create the tasks to retrieve the 'mapped to' groups
            var shardReadTasks = new HashSet<Task<List<String>>>();
            var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
            foreach (Tuple<DistributedClientAndShardDescription, IEnumerable<String>> currentClientAndGroups in directlyMappedClientsAndGroups)
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
                "retrieve group to group mappings from",
                null,
                null
            );

            if (includeParameterGroupsInMappingCount == true)
            {
                return (returnGroups, returnGroups.Count);
            }
            else
            {
                return (returnGroups, returnGroups.Count - groups.Count());
            }
        }

        /// <summary>
        /// Gets a unique collection of all users which are directly mapped to the specified groups, by querying against all user shards in the distributed environment.
        /// </summary>
        /// <param name="groups">The groups to retrieve the users for.</param>
        /// <returns>A tuple containing: a unique collection of users which are directly mapped to the specified groups, and a count of those users.</returns>
        protected async Task<(IEnumerable<String>, Int32)> GetGroupToUserMappingsWithCountsAsync(IEnumerable<String> groups)
        {
            var returnUsers = new HashSet<String>();
            IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.User, Operation.Query);
            // Create the tasks to retrieve the users
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
                "retrieve group to user mappings from",
                null,
                null
            );

            return (returnUsers, returnUsers.Count);
        }

        /// <summary>
        /// Gets a unique collection of all groups which are directly and indirectly mapped to the specified groups, by querying against all group to group mapping shards in the distributed environment.
        /// </summary>
        /// <param name="groups">The group to retrieve the mapped groups for.</param>
        /// <returns>A tuple containing: a unique collection of groups which are directly and indirectly mapped to the specified groups.</returns>
        protected async Task<IEnumerable<String>> GetGroupToGroupReverseMappingsImplementationAsync(IEnumerable<String> groups)
        {
            var returnGroups = new HashSet<String>();
            IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query);
            // Create the tasks to retrieve the groups
            var shardReadTasks = new HashSet<Task<List<String>>>();
            var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
            foreach (DistributedClientAndShardDescription currentClient in clients)
            {
                Task<List<String>> currentTask = currentClient.Client.GetGroupToGroupReverseMappingsAsync(groups);
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
            var ignoreExceptions = new HashSet<Type>();
            await AwaitTaskCompletionAsync
            (
                shardReadTasks,
                taskToShardDescriptionMap,
                resultAction,
                continuePredicate,
                rethrowExceptions,
                ignoreExceptions,
                "retrieve group to group reverse mappings from",
                null,
                null
            );

            return returnGroups;
        }

        /// <summary>
        /// Gets the users that are directly mapped to the specified application component and access level pair, by querying against all user shards in the distributed environment.
        /// </summary>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <returns>A collection of users that are directly mapped to the specified application component and access level.</returns>
        protected async Task<IEnumerable<String>> GetApplicationComponentAndAccessLevelToUserDirectMappingsAsync(String applicationComponent, String accessLevel)
        {
            return await GetElementToUserMappingsAsync
            (
                applicationComponent,
                accessLevel,
                (
                    IAccessManagerAsyncQueryProcessor<String, String, String, String> userClient,
                    String funcApplicationComponent,
                    String funcAccessLevel
                ) =>
                {
                    return userClient.GetApplicationComponentAndAccessLevelToUserMappingsAsync(funcApplicationComponent, funcAccessLevel, false);
                },
                new HashSet<Type>(),
                "retrieve application component and access level to user mappings from"
            );
        }

        /// <summary>
        /// Gets the users that are directly mapped to the specified entity, by querying against all user shards in the distributed environment.
        /// </summary>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <returns>A collection of users that are directly mapped to the specified entity.</returns>
        protected async Task<IEnumerable<String>> GetEntityToUserDirectMappingsAsync(String entityType, String entity)
        {
            return await GetElementToUserMappingsAsync
            (
                entityType,
                entity,
                (
                    IAccessManagerAsyncQueryProcessor<String, String, String, String> userClient,
                    String funcEntityType,
                    String funcEntity
                ) =>
                {
                    return userClient.GetEntityToUserMappingsAsync(funcEntityType, funcEntity, false);
                },
                new HashSet<Type>() { typeof(EntityTypeNotFoundException), typeof(EntityNotFoundException) },
                "retrieve entity to user mappings from"
            );
        }

        /// <summary>
        /// Gets the groups that are directly mapped to the specified application component and access level pair, by querying against all group shards in the distributed environment.
        /// </summary>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <returns>A collection of groups that are directly mapped to the specified application component and access level, and a count of those groups.</returns>
        protected async Task<(IEnumerable<String>, Int32)> GetApplicationComponentAndAccessLevelToGroupDirectMappingsAsync(String applicationComponent, String accessLevel)
        {
            return await GetElementToGroupMappingsAsync
            (
                applicationComponent,
                accessLevel,
                (
                    IAccessManagerAsyncQueryProcessor<String, String, String, String> groupClient,
                    String funcApplicationComponent,
                    String funcAccessLevel
                ) =>
                {
                    return groupClient.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(funcApplicationComponent, funcAccessLevel, false);
                },
                new HashSet<Type>(),
                "retrieve application component and access level to group mappings from"
            );
        }

        /// <summary>
        /// Gets the groups that are directly mapped to the specified entity, by querying against all group shards in the distributed environment.
        /// </summary>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <returns>A collection of groups that are directly mapped to the specified entity, and a count of those groups.</returns>
        protected async Task<(IEnumerable<String>, Int32)> GetEntityToGroupDirectMappingsAsync(String entityType, String entity)
        {
            return await GetElementToGroupMappingsAsync
            (
                entityType,
                entity,
                (
                    IAccessManagerAsyncQueryProcessor<String, String, String, String> groupClient,
                    String funcEntityType,
                    String funcEntity
                ) =>
                {
                    return groupClient.GetEntityToGroupMappingsAsync(funcEntityType, funcEntity, false);
                },
                new HashSet<Type>() { typeof(EntityTypeNotFoundException), typeof(EntityNotFoundException) },
                "retrieve entity to group mappings from"
            );
        }

        /// <summary>
        /// Gets the users that are mapped to a specified pair of elements.
        /// </summary>
        /// <param name="element1Value">The first element in the mapped pair.</param>
        /// <param name="element2Value">The second element in the mapped pair.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to the elements via groups).</param>
        /// <param name="getElementToUserMappingsFunc">A function which returns the user that are directly mapped to the specified pair of elements.  Accepts two parameters: the first element in the mapped pair, and the second element in the mapped pair, and returns a collection of the users directly mapped to the elements.</param>
        /// <param name="getElementToGroupMappingsFunc">A function which returns the groups that are directly mapped to the specified pair of elements.  Accepts two parameters: the first element in the mapped pair, and the second element in the mapped pair, and returns a collection of the groups directly mapped to the elements.</param>
        /// <param name="intervalMetric">An <see cref="IntervalMetric"/> to log as part of querying.</param>
        /// <param name="countMetric">A <see cref="CountMetric"/> to log as part of querying.</param>
        /// <returns>The users that are mapped to the pair of elements.</returns>
        /// <remarks>This methods provides a common base for the implementation of methods GetApplicationComponentAndAccessLevelToUserMappingsAsync() and GetEntityToUserMappingsAsync().</remarks>
        protected async Task<List<String>> GetElementToUserMappingsAsync
        (
            String element1Value,
            String element2Value,
            Boolean includeIndirectMappings,
            Func<String, String, Task<IEnumerable<String>>> getElementToUserMappingsFunc,
            Func<String, String, Task<(IEnumerable<String>, Int32)>> getElementToGroupMappingsFunc,
            IntervalMetric intervalMetric,
            CountMetric countMetric
        )
        {
            List<String> returnUsers = null;
            Guid beginId = metricLogger.Begin(intervalMetric);
            try
            {
                IEnumerable<String> directlyMappedUsers = await getElementToUserMappingsFunc(element1Value, element2Value);
                if (includeIndirectMappings == false)
                {
                    returnUsers = new List<String>(directlyMappedUsers);
                }
                else
                {
                    (IEnumerable<String> directlyMappedGroups, Int32 directlyMappedGroupsCount) = await getElementToGroupMappingsFunc(element1Value, element2Value);
                    IEnumerable<String> indirectlyMappedGroups;
                    IEnumerable<String> indirectlyMappedUsers;
                    Int32 indirectlyMappedUsersCount;
                    if (directlyMappedGroupsCount == 0)
                    {
                        indirectlyMappedGroups = Enumerable.Empty<String>();
                        indirectlyMappedUsers = Enumerable.Empty<String>();
                    }
                    else
                    {
                        indirectlyMappedGroups = await GetGroupToGroupReverseMappingsImplementationAsync(directlyMappedGroups);
                        var allGroups = new HashSet<String>(directlyMappedGroups);
                        allGroups.UnionWith(indirectlyMappedGroups);
                        (indirectlyMappedUsers, indirectlyMappedUsersCount) = await GetGroupToUserMappingsWithCountsAsync(allGroups);
                    }
                    var allUsers = new HashSet<String>(directlyMappedUsers);
                    allUsers.UnionWith(indirectlyMappedUsers);
                    returnUsers = new List<String>(allUsers);
                }
            }
            catch
            {
                metricLogger.CancelBegin(beginId, intervalMetric);
                throw;
            }
            metricLogger.End(beginId, intervalMetric);
            metricLogger.Increment(countMetric);

            return returnUsers;
        }

        /// <summary>
        /// Gets the groups that are mapped to a specified pair of elements.
        /// </summary>
        /// <param name="element1Value">The first element in the mapped pair.</param>
        /// <param name="element2Value">The second element in the mapped pair.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a group is mapped to the elements via other groups).</param>
        /// <param name="getElementToGroupMappingsFunc">A function which returns the groups that are directly mapped to the specified pair of elements.  Accepts two parameters: the first element in the mapped pair, and the second element in the mapped pair, and returns a collection of the groups directly mapped to the elements.</param>
        /// <param name="intervalMetric">An (optional) <see cref="IntervalMetric"/> to log as part of querying.</param>
        /// <param name="countMetric">An (optional) <see cref="CountMetric"/> to log as part of querying.</param>
        /// <returns>The groups that are mapped to the pair of elements.</returns>
        /// <remarks>This methods provides a common base for the implementation of methods GetApplicationComponentAndAccessLevelToGroupMappingsAsync() and GetEntityToGroupMappingsAsync().</remarks>
        protected async Task<List<String>> GetElementToGroupMappingsAsync
        (
            String element1Value,
            String element2Value,
            Boolean includeIndirectMappings,
            Func<String, String, Task<(IEnumerable<String>, Int32)>> getElementToGroupMappingsFunc,
            IntervalMetric intervalMetric = null,
            CountMetric countMetric = null
        )
        {
            List<String> returnGroups = null;
            Guid beginId = default(Guid);
            if (intervalMetric != null)
            {
                beginId = metricLogger.Begin(intervalMetric);
            }
            try
            {
                (IEnumerable<String> directlyMappedGroups, Int32 directlyMappedGroupsCount) = await getElementToGroupMappingsFunc(element1Value, element2Value);
                if (includeIndirectMappings == false)
                {
                    returnGroups = new List<String>(directlyMappedGroups);
                }
                else
                {
                    IEnumerable<String> indirectlyMappedGroups;
                    if (directlyMappedGroupsCount == 0)
                    {
                        indirectlyMappedGroups = Enumerable.Empty<String>();
                    }
                    else
                    {
                        indirectlyMappedGroups = await GetGroupToGroupReverseMappingsImplementationAsync(directlyMappedGroups);
                    }
                    var resultGroups = new HashSet<String>(directlyMappedGroups);
                    resultGroups.UnionWith(indirectlyMappedGroups);
                    returnGroups = new List<String>(resultGroups);
                }
            }
            catch
            {
                if (intervalMetric != null)
                {
                    metricLogger.CancelBegin(beginId, intervalMetric);
                }
                throw;
            }
            if (intervalMetric != null)
            {
                metricLogger.End(beginId, intervalMetric);
                metricLogger.Increment(countMetric);
            }

            return returnGroups;
        }

        /// <summary>
        /// Gets the users that are directly mapped to a specified pair of elements, by querying against all user shards in the distributed environment.
        /// </summary>
        /// <typeparam name="TElement1">The type of the first element in the mapped pair.</typeparam>
        /// <typeparam name="TElement2">The type of the second element in the mapped pair.</typeparam>
        /// <param name="element1Value">The first element in the mapped pair.</param>
        /// <param name="element2Value">The second element in the mapped pair.</param>
        /// <param name="shardQueryFunc">A function which returns a task resolving to a list of users and which is invoked against all user shards in the distributed environment.  Accepts three parameters: an instance of <see cref="IAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> which connects to the user shard, the first element in the mapped pair, and the second element in the mapped pair.</param>
        /// <param name="ignoreExceptions">A set of exceptions which should be ignored if caught when executing a query.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "retrieve entity to user mappings from".</param>
        /// <returns>The users that are directly mapped to the pair of elements.</returns>
        /// <remarks>This methods provides a common base for the implementation of methods GetApplicationComponentAndAccessLevelToUserMappings() and GetEntityToUserMappings() where parameter 'includeIndirectMappings' is set false.</remarks>
        protected async Task<IEnumerable<String>> GetElementToUserMappingsAsync<TElement1, TElement2>
        (
            TElement1 element1Value,
            TElement2 element2Value,
            Func<IAccessManagerAsyncQueryProcessor<String, String, String, String>, TElement1, TElement2, Task<List<String>>> shardQueryFunc,
            HashSet<Type> ignoreExceptions,
            String exceptionEventDescription
        )
        {
            var returnUsers = new HashSet<String>();
            IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.User, Operation.Query);
            // Create the tasks to retrieve the users
            var shardReadTasks = new HashSet<Task<List<String>>>();
            var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
            foreach (DistributedClientAndShardDescription currentClient in clients)
            {
                Task<List<String>> currentTask = shardQueryFunc(currentClient.Client, element1Value, element2Value);
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

            return returnUsers;
        }

        /// <summary>
        /// Gets the groups that are directly mapped to a specified pair of elements, by querying against all groups shards in the distributed environment.
        /// </summary>
        /// <typeparam name="TElement1">The type of the first element in the mapped pair.</typeparam>
        /// <typeparam name="TElement2">The type of the second element in the mapped pair.</typeparam>
        /// <param name="element1Value">The first element in the mapped pair.</param>
        /// <param name="element2Value">The second element in the mapped pair.</param>
        /// <param name="shardQueryFunc">A function which returns a task resolving to a list of groups and which is invoked against all group shards in the distributed environment.  Accepts three parameters: an instance of <see cref="IAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> which connects to the group shard, the first element in the mapped pair, and the second element in the mapped pair.</param>
        /// <param name="ignoreExceptions">A set of exceptions which should be ignored if caught when executing a query.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "retrieve entity to group mappings from".</param>
        /// <returns>The groups that are directly mapped to the pair of elements, and a count of those groups.</returns>
        /// <remarks>This methods provides a common base for the implementation of methods GetApplicationComponentAndAccessLevelToGroupMappings() and GetEntityToGroupMappings() where parameter 'includeIndirectMappings' is set false.</remarks>
        protected async Task<(IEnumerable<String>, Int32)> GetElementToGroupMappingsAsync<TElement1, TElement2>
        (
            TElement1 element1Value,
            TElement2 element2Value,
            Func<IAccessManagerAsyncQueryProcessor<String, String, String, String>, TElement1, TElement2, Task<List<String>>> shardQueryFunc,
            HashSet<Type> ignoreExceptions,
            String exceptionEventDescription
        )
        {
            var returnGroups = new HashSet<String>();
            IEnumerable<DistributedClientAndShardDescription> clients = shardClientManager.GetAllClients(DataElement.Group, Operation.Query);
            // Create the tasks to retrieve the groups
            var shardReadTasks = new HashSet<Task<List<String>>>();
            var taskToShardDescriptionMap = new Dictionary<Task<List<String>>, String>();
            foreach (DistributedClientAndShardDescription currentClient in clients)
            {
                Task<List<String>> currentTask = shardQueryFunc(currentClient.Client, element1Value, element2Value);
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

            return (returnGroups, returnGroups.Count);
        }

        /// <summary>
        /// Asyncronously waits for a collection of tasks to complete.
        /// </summary>
        /// <typeparam name="T">The type of object returned by each task.</typeparam>
        /// <param name="shardTasks">The tasks for wait for.</param>
        /// <param name="taskToShardDescriptionMap">A dictionary which maps each task to a description of the shard the client which created the task connected to.</param>
        /// <param name="resultAction">An action to invoke with the results of each task.</param>
        /// <param name="continuePredicate">A function which returns a boolean which is called after the completion of each task and subdequent processing of its results, and which indicates whether further tasks shouled be waited for.  Accepts a single parameter which is the result of each task.</param>
        /// <param name="rethrowExceptions">A set of exceptions which should be rethrown directly if caught when executing a task.</param>
        /// <param name="ignoreExceptions">A set of exceptions which should be ignored if caught when executing a task.</param>
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
            HashSet<Type> rethrowExceptions,
            HashSet<Type> ignoreExceptions,
            String exceptionEventDescription,
            Nullable<Guid> intervalMetricBeginId,
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
                    if (ignoreExceptions.Contains(e.GetType()) == false)
                    {
                        if (intervalMetricBeginId.HasValue == true)
                        {
                            metricLogger.CancelBegin(intervalMetricBeginId.Value, cancelIntervalMetric);
                        }
                        if (rethrowExceptions.Contains(e.GetType()) == true)
                        {
                            throw;
                        }
                        throw new Exception($"Failed to {exceptionEventDescription} shard with configuration '{taskToShardDescriptionMap[completedTask]}'.", e);
                    }
                }
                if (completedTask.IsFaulted == false)
                {
                    resultAction(completedTask.Result);
                }
                shardTasks.Remove(completedTask);
                if (completedTask.IsFaulted == false && continuePredicate(completedTask.Result) == false)
                {
                    return;
                }
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Model/container class holding metric data recorded by a call to the ExecuteQueryAgainstGroupShards() method.
        /// </summary>
        protected class ExecuteQueryAgainstGroupShardsMetricData
        {
            /// <summary>A count of the total number of unique groups mapped directly and indirectly to the inputted groups (and including the inputted groups).</summary>
            public Int32 GroupsMappedToGroups { get; protected set; }

            /// <summary>A count of the number of group shards that were queried.</summary>
            public Int32 GroupShardsQueried { get; protected set; }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerOperationCoordinator+ExecuteQueryAgainstGroupShardsMetricData class.
            /// </summary>
            /// <param name="groupsMappedToGroups">A count of the total number of unique groups mapped directly and indirectly to the inputted groups (and including the inputted groups).</param>
            /// <param name="groupShardsQueried">A count of the number of group shards that were queried.</param>
            public ExecuteQueryAgainstGroupShardsMetricData(Int32 groupsMappedToGroups, Int32 groupShardsQueried)
            {
                GroupsMappedToGroups = groupsMappedToGroups;
                GroupShardsQueried = groupShardsQueried;
            }
        }

        #endregion
    }
}
