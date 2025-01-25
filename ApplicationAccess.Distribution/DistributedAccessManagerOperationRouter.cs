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
using System.Reflection;
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
    /// Distributes operations to two shards in a distributed AccessManager implementation, aggregating and returning their results if required.  Designed to sit in front the 'source' and 'target' nodes of a shard split, and provide routing functionality whilst the splitting process occurs.
    /// </summary>
    /// <remarks>Unlike <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, this class does not coordinate responses to operations across all shards in a distributed AccessManager implementation.  Instead, it's designed to be used 'downstream' of a <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, and front multiple shards of a single type (i.e. user or group shards).  The class simply distributes operations only to the shards of the type defined by the query method, acting as a 'router'.  I.e. in the <see cref="DistributedAccessManagerOperationCoordinator{TClientConfiguration}"/>, queries which should be distributed to indirectly mapped groups first call to the group to group mapping shards to get the indirectly mapped groups, before retrieving mappings from those groups in the individual group shards.  A similar method implemented in this class would not call to the group to group mapping shards.</remarks>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public class DistributedAccessManagerOperationRouter<TClientConfiguration> :
        DistributedAccessManagerOperationProcessorBase<TClientConfiguration>,
        IDistributedAccessManagerOperationRouter<TClientConfiguration>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        // TODO: Exceptions checks in method ThrowExceptionIfMethodNotValidForDataType() are included to help ensure that methods are being called on the class as expected,
        //   in terms of the 'shardDataElement' field value.  I.e. the class can act as a router in front of user shards or gropus shards, but in either mode there are some
        //   methods which should not be being called.  These exception checks help to confirm the allowed method assumptions are correct, and that the calling component is
        //   behaving as expected.  However, calls to ThrowExceptionIfMethodNotValidForDataType() could be removed once integration testing is complete.

        /// <summary>Message to use in <see cref="NotImplementedException"/> messages if group to group mapping methods are called.</summary>
        protected const String groupToGroupMethodsNotImplementedExceptionMessage = "Group to group mapping methods are not support by the DistributedAccessManagerOperationRouter.";

        /// <summary>The first (inclusive) in the range of hash codes of data elements managed by the source shard.</summary>
        protected Int32 sourceShardHashRangeStart;
        /// <summary>The last (inclusive) in the range of hash codes of data elements managed by the source shard.</summary>
        protected Int32 sourceShardHashRangeEnd;
        /// <summary>The first (inclusive) in the range of hash codes of data elements managed by the target shard.</summary>
        protected Int32 targetShardHashRangeStart;
        /// <summary>The last (inclusive) in the range of hash codes of data elements managed by the target shard.</summary>
        protected Int32 targetShardHashRangeEnd;
        /// <summary>The client and its description, used to connect to the source query shard.</summary>
        protected DistributedClientAndShardDescription sourceQueryShardClientAndDescription;
        /// <summary>The client and its description, used to connect to the source event shard.</summary>
        protected DistributedClientAndShardDescription sourceEventShardClientAndDescription;
        /// <summary>The client and its description, used to connect to the target query shard.</summary>
        protected DistributedClientAndShardDescription targetQueryShardClientAndDescription;
        /// <summary>The client and its description, used to connect to the target event shard.</summary>
        protected DistributedClientAndShardDescription targetEventShardClientAndDescription;
        /// <summary>The type of data element the source and target shards manage in the distributed AccessManager implementation.</summary>
        protected DataElement shardDataElement;
        /// <summary>A hash code generator for users.</summary>
        protected IHashCodeGenerator<String> userHashCodeGenerator;
        /// <summary>A hash code generator for groups.</summary>
        protected IHashCodeGenerator<String> groupHashCodeGenerator;
        /// <summary>Whether or not the routing functionality is swicthed on.  If false (off) all operations are routed to the source shard.</summary>
        protected volatile Boolean routingOn;
        /// <summary>Maps each of the classes' public operation-handling methods to a set of <see cref="DataElement">DataElements</see>, denoting for which DataElements the method should be called for.</summary>
        protected Dictionary<String, HashSet<DataElement>> methodNameToSupportedDataTypeMap;

        /// <summary>
        /// Whether or not the routing functionality is switched on.  If false (off) all operations are routed to the source shard.
        /// </summary>
        public Boolean RoutingOn
        {
            get
            {
                return routingOn;
            }
            set
            {
                if (value == true)
                {
                    metricLogger.Increment(new RoutingSwitchedOn());
                }
                else
                {
                    metricLogger.Increment(new RoutingSwitchedOff());
                }

                routingOn = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManagerOperationRouter class.
        /// </summary>
        /// <param name="sourceShardHashRangeStart">The first (inclusive) in the range of hash codes of data elements managed by the source shard.</param>
        /// <param name="sourceShardHashRangeEnd">The last (inclusive) in the range of hash codes of data elements managed by the source shard.</param>
        /// <param name="targetShardHashRangeStart">The first (inclusive) in the range of hash codes of data elements managed by the target shard.</param>
        /// <param name="targetShardHashRangeEnd">The last (inclusive) in the range of hash codes of data elements managed by the target shard.</param>
        /// <param name="shardDataElement">The type of data element the source and target shards manage in the distributed AccessManager implementation.</param>
        /// <param name="shardClientManager">Manages the clients used to connect to the shards.  Must be pre-populated with source and target shard clients which match the hash ranges specified in parameters <paramref name="sourceShardHashRangeStart"/>, <paramref name="sourceShardHashRangeEnd"/>, <paramref name="targetShardHashRangeStart"/>, and <paramref name="targetShardHashRangeEnd"/></param>
        /// <param name="userHashCodeGenerator">A hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">A hash code generator for groups.</param>
        /// <param name="routingOn">Whether or not the routing functionality is initially swicthed on.  If false (off) all operations are routed to the source shard.</param>
        /// <param name="metricLogger">Logger for metrics.</param>
        public DistributedAccessManagerOperationRouter
        (
            Int32 sourceShardHashRangeStart,
            Int32 sourceShardHashRangeEnd,
            Int32 targetShardHashRangeStart,
            Int32 targetShardHashRangeEnd,
            DataElement shardDataElement,
            IShardClientManager<TClientConfiguration> shardClientManager,
            IHashCodeGenerator<String> userHashCodeGenerator,
            IHashCodeGenerator<String> groupHashCodeGenerator,
            Boolean routingOn,
            IMetricLogger metricLogger
        )
            : base(shardClientManager, metricLogger)
        {
            if (sourceShardHashRangeEnd < sourceShardHashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(sourceShardHashRangeEnd), $"Parameter '{nameof(sourceShardHashRangeEnd)}' with value {sourceShardHashRangeEnd} must be greater than or equal to the value {sourceShardHashRangeStart} of parameter '{nameof(sourceShardHashRangeStart)}'.");
            if (targetShardHashRangeEnd < targetShardHashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(targetShardHashRangeEnd), $"Parameter '{nameof(targetShardHashRangeEnd)}' with value {targetShardHashRangeEnd} must be greater than or equal to the value {targetShardHashRangeStart} of parameter '{nameof(targetShardHashRangeStart)}'.");
            if (sourceShardHashRangeEnd + 1 != targetShardHashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(targetShardHashRangeStart), $"Parameter '{nameof(targetShardHashRangeStart)}' with value {targetShardHashRangeStart} must be contiguous with parameter '{nameof(sourceShardHashRangeEnd)}' with value {sourceShardHashRangeEnd}.");
            if (shardDataElement == DataElement.GroupToGroupMapping)
                throw new ArgumentException($"Value '{shardDataElement}' in parameter '{nameof(shardDataElement)}' is not valid.", nameof(shardDataElement));
            
            this.sourceShardHashRangeStart = sourceShardHashRangeStart;
            this.sourceShardHashRangeEnd = sourceShardHashRangeEnd;
            this.targetShardHashRangeStart = targetShardHashRangeStart;
            this.targetShardHashRangeEnd = targetShardHashRangeEnd;
            this.shardDataElement = shardDataElement;
            this.userHashCodeGenerator = userHashCodeGenerator;
            this.groupHashCodeGenerator = groupHashCodeGenerator;
            this.routingOn = routingOn;
            InitializeMethodNameToSupportedDataTypeMap();
            InitializeShardClientField(ref sourceQueryShardClientAndDescription, Operation.Query, sourceShardHashRangeStart, nameof(shardClientManager));
            InitializeShardClientField(ref sourceEventShardClientAndDescription, Operation.Event, sourceShardHashRangeStart, nameof(shardClientManager));
            InitializeShardClientField(ref targetQueryShardClientAndDescription, Operation.Query, targetShardHashRangeStart, nameof(shardClientManager));
            InitializeShardClientField(ref targetEventShardClientAndDescription, Operation.Event, targetShardHashRangeStart, nameof(shardClientManager));
            ValidateHashRangeEndClientSameAsStart(sourceQueryShardClientAndDescription, Operation.Query, sourceShardHashRangeEnd, nameof(sourceShardHashRangeStart), nameof(sourceShardHashRangeEnd));
            ValidateHashRangeEndClientSameAsStart(sourceEventShardClientAndDescription, Operation.Event, sourceShardHashRangeEnd, nameof(sourceShardHashRangeStart), nameof(sourceShardHashRangeEnd));
            ValidateHashRangeEndClientSameAsStart(targetQueryShardClientAndDescription, Operation.Query, targetShardHashRangeEnd, nameof(targetShardHashRangeStart), nameof(targetShardHashRangeEnd));
            ValidateHashRangeEndClientSameAsStart(targetEventShardClientAndDescription, Operation.Event, targetShardHashRangeEnd, nameof(targetShardHashRangeStart), nameof(targetShardHashRangeEnd));
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUsersAsync()
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetUsersAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetUsersAsync();
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return await client.GetUsersAsync();
                };

                return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve users from");
            }
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupsAsync()
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetGroupsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetGroupsAsync();
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return await client.GetGroupsAsync();
                };

                return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve groups from");
            }
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityTypesAsync()
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetEntityTypesAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetEntityTypesAsync();
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return await client.GetEntityTypesAsync();
                };

                return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve entity types from");
            }
        }

        /// <inheritdoc/>
        public Task AddUserAsync(String user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Boolean>> clientAction = async (client) =>
            {
                return await client.ContainsUserAsync(user);
            };

            return await ImplementRoutingAsync(nameof(this.ContainsUserAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task RemoveUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.RemoveUserAsync(user);
            };

            await ImplementRoutingAsync(nameof(this.RemoveUserAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public Task AddGroupAsync(String group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsGroupAsync(String group)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.ContainsGroupAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.ContainsGroupAsync(group);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Tuple<Boolean, Guid>>> shardCheckFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return Tuple.Create(await client.ContainsGroupAsync(group), Guid.NewGuid());
                };

                return await ContainsElementAsync(shardDataElementTypes, shardCheckFunc, $"check for group '{group}' in");
            }
        }

        /// <inheritdoc/>
        public async Task RemoveGroupAsync(String group)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.RemoveGroupAsync));

            if (routingOn == false)
            {
                await sourceEventShardClientAndDescription.Client.RemoveGroupAsync(group);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> shardRemoveFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    await client.RemoveGroupAsync(group);
                };

                await RemoveElementAsync(shardDataElementTypes, shardRemoveFunc, $"remove group '{group}' from");
            }
        }

        /// <inheritdoc/>
        public async Task AddUserToGroupMappingAsync(String user, String group)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.AddUserToGroupMappingAsync(user, group);
            };

            await ImplementRoutingAsync(nameof(this.AddUserToGroupMappingAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToGroupMappingsAsync(String user, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));

            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> clientAction = async (client) =>
            {
                return await client.GetUserToGroupMappingsAsync(user, includeIndirectMappings);
            };

            return await ImplementRoutingAsync(nameof(this.GetUserToGroupMappingsAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));

            return await GetGroupToUserMappingsImplementationAsync(new List<String> { group } , "retrieve group to user mappings from");
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(IEnumerable<String> groups)
        {
            return await GetGroupToUserMappingsImplementationAsync(groups, "retrieve group to user mappings for multiple groups from");
        }

        /// <inheritdoc/>
        public Task RemoveUserToGroupMappingAsync(String user, String group)
        {
            throw new NotImplementedException(groupToGroupMethodsNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task AddGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            throw new NotImplementedException(groupToGroupMethodsNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task<List<String>> GetGroupToGroupMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException(groupToGroupMethodsNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task<List<String>> GetGroupToGroupMappingsAsync(IEnumerable<String> groups)
        {
            throw new NotImplementedException(groupToGroupMethodsNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task<List<String>> GetGroupToGroupReverseMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException(groupToGroupMethodsNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task<List<String>> GetGroupToGroupReverseMappingsAsync(IEnumerable<String> groups)
        {
            throw new NotImplementedException(groupToGroupMethodsNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public Task RemoveGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            throw new NotImplementedException(groupToGroupMethodsNotImplementedExceptionMessage);
        }

        /// <inheritdoc/>
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.AddUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
            };

            await ImplementRoutingAsync(nameof(this.AddUserToApplicationComponentAndAccessLevelMappingAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
            };

            return await ImplementRoutingAsync(nameof(this.GetUserToApplicationComponentAndAccessLevelMappingsAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
            };

            await ImplementRoutingAsync(nameof(this.RemoveUserToApplicationComponentAndAccessLevelMappingAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.AddGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
            };

            await ImplementRoutingAsync(nameof(this.AddGroupToApplicationComponentAndAccessLevelMappingAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(String group)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
            };

            return await ImplementRoutingAsync(nameof(this.GetGroupToApplicationComponentAndAccessLevelMappingsAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetApplicationComponentAndAccessLevelToUserMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetApplicationComponentAndAccessLevelToUserMappingsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return await client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
                };

                return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve application component and access level to user mappings from");
            }
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetApplicationComponentAndAccessLevelToGroupMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetApplicationComponentAndAccessLevelToGroupMappingsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return await client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
                };

                return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, "retrieve application component and access level to group mappings from");
            }
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
            };

            await ImplementRoutingAsync(nameof(this.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public Task AddEntityTypeAsync(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityTypeAsync(String entityType)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.ContainsEntityTypeAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.ContainsEntityTypeAsync(entityType);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Tuple<Boolean, Guid>>> shardCheckFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return Tuple.Create(await client.ContainsEntityTypeAsync(entityType), Guid.NewGuid());
                };

                return await ContainsElementAsync(shardDataElementTypes, shardCheckFunc, $"check for entity type '{entityType}' in");
            }
        }

        /// <inheritdoc/>
        public async Task RemoveEntityTypeAsync(String entityType)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.RemoveEntityTypeAsync));

            if (routingOn == false)
            {
                await sourceEventShardClientAndDescription.Client.RemoveEntityTypeAsync(entityType);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> shardRemoveFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    await client.RemoveEntityTypeAsync(entityType);
                };

                await RemoveElementAsync(shardDataElementTypes, shardRemoveFunc, $"remove entity type '{entityType}' from");
            }
        }

        /// <inheritdoc/>
        public Task AddEntityAsync(String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAsync(String entityType)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetEntitiesAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetEntitiesAsync(entityType);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> shardReadFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return await client.GetEntitiesAsync(entityType);
                };

                return await GetAndMergeElementsAsync(shardDataElementTypes, shardReadFunc, $"retrieve entities of type '{entityType}' from");
            }
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityAsync(String entityType, String entity)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.ContainsEntityAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.ContainsEntityAsync(entityType, entity);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Tuple<Boolean, Guid>>> shardCheckFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    return Tuple.Create(await client.ContainsEntityAsync(entityType, entity), Guid.NewGuid());
                };

                return await ContainsElementAsync(shardDataElementTypes, shardCheckFunc, $"check for entity '{entity}' with type '{entityType}' in");
            }
        }

        /// <inheritdoc/>
        public async Task RemoveEntityAsync(String entityType, String entity)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.RemoveEntityAsync));

            if (routingOn == false)
            {
                await sourceEventShardClientAndDescription.Client.RemoveEntityAsync(entityType, entity);
            }
            else
            {
                var shardDataElementTypes = new List<DataElement>() { DataElement.User, DataElement.Group };
                Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> shardRemoveFunc = async (IDistributedAccessManagerAsyncClient<String, String, String, String> client) =>
                {
                    await client.RemoveEntityAsync(entityType, entity);
                };

                await RemoveElementAsync(shardDataElementTypes, shardRemoveFunc, $"remove entity '{entity}' with type '{entityType}' from");
            }
        }

        /// <inheritdoc/>
        public async Task AddUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.AddUserToEntityMappingAsync(user, entityType, entity);
            };

            await ImplementRoutingAsync(nameof(this.AddUserToEntityMappingAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetUserToEntityMappingsAsync(user);
            };

            return await ImplementRoutingAsync(nameof(this.GetUserToEntityMappingsAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToEntityMappingsAsync(String user, String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> clientAction = async (client) =>
            {
                return await client.GetUserToEntityMappingsAsync(user, entityType);
            };

            return await ImplementRoutingAsync(nameof(this.GetUserToEntityMappingsAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityToUserMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetEntityToUserMappingsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetEntityToUserMappingsAsync(entityType, entity, includeIndirectMappings);
            }
            else
            {
                if (includeIndirectMappings == true)
                    throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));

                return new List<String>(await GetEntityToUserDirectMappingsAsync(entityType, entity));
            }
        }

        /// <inheritdoc/>
        public async Task RemoveUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.RemoveUserToEntityMappingAsync(user, entityType, entity);
            };

            await ImplementRoutingAsync(nameof(this.RemoveUserToEntityMappingAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task AddGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.AddGroupToEntityMappingAsync(group, entityType, entity);
            };

            await ImplementRoutingAsync(nameof(this.AddGroupToEntityMappingAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(String group)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetGroupToEntityMappingsAsync(group);
            };

            return await ImplementRoutingAsync(nameof(this.GetGroupToEntityMappingsAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToEntityMappingsAsync(String group, String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> clientAction = async (client) =>
            {
                return await client.GetGroupToEntityMappingsAsync(group, entityType);
            };

            return await ImplementRoutingAsync(nameof(this.GetGroupToEntityMappingsAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityToGroupMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            if (includeIndirectMappings == true)
                throw new ArgumentException($"Parameter '{nameof(includeIndirectMappings)}' with a value of '{includeIndirectMappings}' is not supported.", nameof(includeIndirectMappings));
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetEntityToGroupMappingsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetEntityToGroupMappingsAsync(entityType, entity, includeIndirectMappings);
            }
            else
            {
                var returnList = new List<String>();
                foreach (String currentGroup in (await GetEntityToGroupDirectMappingsAsync(entityType, entity)).Item1)
                {
                    returnList.Add(currentGroup);
                }

                return returnList;
            }
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction = async (client) =>
            {
                await client.RemoveGroupToEntityMappingAsync(group, entityType, entity);
            };

            await ImplementRoutingAsync(nameof(this.RemoveGroupToEntityMappingAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(String user, String applicationComponent, String accessLevel)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Boolean>> clientAction = async (client) =>
            {
                return await client.HasAccessToApplicationComponentAsync(user, applicationComponent, accessLevel);
            };

            return await ImplementRoutingAsync(nameof(this.HasAccessToApplicationComponentAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(IEnumerable<String> groups, String applicationComponent, String accessLevel)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.HasAccessToApplicationComponentAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.HasAccessToApplicationComponentAsync(groups, applicationComponent, accessLevel);
            }
            else
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
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(String user, String entityType, String entity)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<Boolean>> clientAction = async (client) =>
            {
                return await client.HasAccessToEntityAsync(user, entityType, entity);
            };

            return await ImplementRoutingAsync(nameof(this.HasAccessToEntityAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(IEnumerable<String> groups, String entityType, String entity)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.HasAccessToEntityAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.HasAccessToEntityAsync(groups, entityType, entity);
            }
            else
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
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetApplicationComponentsAccessibleByUserAsync(user);
            };

            return await ImplementRoutingAsync(nameof(this.GetApplicationComponentsAccessibleByUserAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupAsync(String group)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetApplicationComponentsAccessibleByGroupAsync(group);
            };

            return await ImplementRoutingAsync(nameof(this.GetApplicationComponentsAccessibleByGroupAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetApplicationComponentsAccessibleByGroupsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetApplicationComponentsAccessibleByGroupsAsync(groups);
            }
            else
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
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(String user)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetEntitiesAccessibleByUserAsync(user);
            };

            return await ImplementRoutingAsync(nameof(this.GetEntitiesAccessibleByUserAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByUserAsync(String user, String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> clientAction = async (client) =>
            {
                return await client.GetEntitiesAccessibleByUserAsync(user, entityType);
            };

            return await ImplementRoutingAsync(nameof(this.GetEntitiesAccessibleByUserAsync), user, userHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(String group)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<Tuple<String, String>>>> clientAction = async (client) =>
            {
                return await client.GetEntitiesAccessibleByGroupAsync(group);
            };

            return await ImplementRoutingAsync(nameof(this.GetEntitiesAccessibleByGroupAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupAsync(String group, String entityType)
        {
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<List<String>>> clientAction = async (client) =>
            {
                return await client.GetEntitiesAccessibleByGroupAsync(group, entityType);
            };

            return await ImplementRoutingAsync(nameof(this.GetEntitiesAccessibleByGroupAsync), group, groupHashCodeGenerator, clientAction);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetEntitiesAccessibleByGroupsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(groups);
            }
            else
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
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups, String entityType)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetEntitiesAccessibleByGroupsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetEntitiesAccessibleByGroupsAsync(groups, entityType);
            }
            else
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
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes field 'methodNameToSupportedDataTypeMap'.
        /// </summary>
        protected void InitializeMethodNameToSupportedDataTypeMap()
        {
            methodNameToSupportedDataTypeMap = new Dictionary<String, HashSet<DataElement>>
            {
                { "GetUsersAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetGroupsAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "GetEntityTypesAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "AddUserAsync", new HashSet<DataElement>() {  } },
                { "ContainsUserAsync", new HashSet<DataElement>() { DataElement.User } },
                { "RemoveUserAsync", new HashSet<DataElement>() { DataElement.User } },
                { "AddGroupAsync", new HashSet<DataElement>() {  } },
                { "ContainsGroupAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "RemoveGroupAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "AddUserToGroupMappingAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetUserToGroupMappingsAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetGroupToUserMappingsAsync", new HashSet<DataElement>() { DataElement.User } },
                { "RemoveUserToGroupMappingAsync", new HashSet<DataElement>() { DataElement.User } },
                { "AddGroupToGroupMappingAsync", new HashSet<DataElement>() {  } },
                { "GetGroupToGroupMappingsAsync", new HashSet<DataElement>() {  } },
                { "GetGroupToGroupReverseMappingsAsync", new HashSet<DataElement>() {  } },
                { "RemoveGroupToGroupMappingAsync", new HashSet<DataElement>() {  } },
                { "AddUserToApplicationComponentAndAccessLevelMappingAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetUserToApplicationComponentAndAccessLevelMappingsAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetApplicationComponentAndAccessLevelToUserMappingsAsync", new HashSet<DataElement>() { DataElement.User } },
                { "RemoveUserToApplicationComponentAndAccessLevelMappingAsync", new HashSet<DataElement>() { DataElement.User } },
                { "AddGroupToApplicationComponentAndAccessLevelMappingAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "GetGroupToApplicationComponentAndAccessLevelMappingsAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "GetApplicationComponentAndAccessLevelToGroupMappingsAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "RemoveGroupToApplicationComponentAndAccessLevelMappingAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "AddEntityTypeAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "ContainsEntityTypeAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "RemoveEntityTypeAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "AddEntityAsync", new HashSet<DataElement>() {  } },
                { "GetEntitiesAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "ContainsEntityAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "RemoveEntityAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "AddUserToEntityMappingAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetUserToEntityMappingsAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetEntityToUserMappingsAsync", new HashSet<DataElement>() { DataElement.User } },
                { "RemoveUserToEntityMappingAsync", new HashSet<DataElement>() { DataElement.User } },
                { "AddGroupToEntityMappingAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "GetGroupToEntityMappingsAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "GetEntityToGroupMappingsAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "RemoveGroupToEntityMappingAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "HasAccessToApplicationComponentAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "HasAccessToEntityAsync", new HashSet<DataElement>() { DataElement.User, DataElement.Group } },
                { "GetApplicationComponentsAccessibleByUserAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetApplicationComponentsAccessibleByGroupAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "GetApplicationComponentsAccessibleByGroupsAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "GetEntitiesAccessibleByUserAsync", new HashSet<DataElement>() { DataElement.User } },
                { "GetEntitiesAccessibleByGroupAsync", new HashSet<DataElement>() { DataElement.Group } },
                { "GetEntitiesAccessibleByGroupsAsync", new HashSet<DataElement>() { DataElement.Group } }
            };

            // Check that no method are missing from 'methodNameToSupportedDataTypeMap'
            var implementedTypes = new List<Type>
            {
                typeof(IAccessManagerAsyncQueryProcessor<String, String, String, String>),
                typeof(IAccessManagerAsyncEventProcessor<String, String, String, String>),
                typeof(IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String>)
            };
            foreach (Type currentImplementedType in implementedTypes)
            {
                foreach (MethodInfo currentMethodInfo in currentImplementedType.GetMethods())
                {
                    if (methodNameToSupportedDataTypeMap.ContainsKey(currentMethodInfo.Name) == false)
                    {
                        throw new Exception($"Supported data element(s) not defined for method {currentMethodInfo.Name}().");
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a field holding a shard client.
        /// </summary>
        /// <param name="shardClientField">The field to initialize.</param>
        /// <param name="operation">The type of operation of the shard.</param>
        /// <param name="hashCode">A hash code within the range of those handled by the shard.</param>
        /// <param name="shardClientManagerParameterName">The name of the parameter used to pass the 'shardClientManager' field to the class.</param>
        /// <exception cref="Exception"></exception>
        protected void InitializeShardClientField(ref DistributedClientAndShardDescription shardClientField, Operation operation, Int32 hashCode, String shardClientManagerParameterName)
        {
            try
            {
                shardClientField = shardClientManager.GetClient(shardDataElement, operation, hashCode);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to retrieve shard client for {typeof(DataElement).Name} '{shardDataElement}', {typeof(Operation).Name} '{operation}', and hash code {hashCode} from parameter '{shardClientManagerParameterName}'.", shardClientManagerParameterName, e);
            }
        }

        /// <summary>
        /// Validates that the specified shard client contains the same client as that retrieved from the ShardClientManager using the specified has range end parameters.
        /// </summary>
        /// <param name="shardClient">The shard client to compare to.</param>
        /// <param name="operation">The type of operation of the hash range end shard.</param>
        /// <param name="hashRangeEnd">The hash range end value.</param>
        /// <param name="shardClientHashRangeStartParameterName">The name of the parameter used to pass the start hash range start value to the constructor.</param>
        /// <param name="shardClientHashRangeEndParameterName">The name of the parameter used to pass the hash range end value to the constructor.</param>
        protected void ValidateHashRangeEndClientSameAsStart(DistributedClientAndShardDescription shardClient, Operation operation, Int32 hashRangeEnd, String shardClientHashRangeStartParameterName, String shardClientHashRangeEndParameterName)
        {
            DistributedClientAndShardDescription shardRangeEndClient;
            try
            {
                shardRangeEndClient = shardClientManager.GetClient(shardDataElement, operation, hashRangeEnd);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to retrieve shard client for {typeof(DataElement).Name} '{shardDataElement}', {typeof(Operation).Name} '{operation}', and hash code {hashRangeEnd} from parameter '{shardClientHashRangeEndParameterName}'.", shardClientHashRangeEndParameterName, e);
            }
            if (shardClient.ShardConfigurationDescription != shardRangeEndClient.ShardConfigurationDescription)
            {
                throw new ArgumentException($"Parameter '{shardClientHashRangeEndParameterName}' with value {hashRangeEnd} returns shard client with description '{shardRangeEndClient.ShardConfigurationDescription}' for {typeof(DataElement).Name} '{shardDataElement}' and {typeof(Operation).Name} '{operation}', but shard client returned for the equivalent hash range start in parameter '{shardClientHashRangeStartParameterName}' has differing description '{shardClient.ShardConfigurationDescription}'.", shardClientHashRangeEndParameterName);
            }
        }

        /// <summary>
        /// Common method for implementing routing for a method which doesn't return a result.
        /// </summary>
        /// <param name="callingMethodName">The name of the client method (used to check whether that method is valid to be called in the context of the router's current configuration).</param>
        /// <param name="keyElementValue">The key element value of the client method's parameters (indicating the shard that the operation should be routed to).</param>
        /// <param name="hashCodeGenerator">The hash code generator for the <paramref name="keyElementValue"/> parameter.</param>
        /// <param name="clientAction">The action to invoke against the client to route to.  Accepts a single parameter which is the client to route to.</param>
        protected async Task ImplementRoutingAsync
        (
            String callingMethodName, 
            String keyElementValue, 
            IHashCodeGenerator<String> hashCodeGenerator, 
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task> clientAction
        )
        {
            ThrowExceptionIfMethodNotValidForDataType(callingMethodName);
            Int32 hashCode = GetAndValidateHashCode(keyElementValue, hashCodeGenerator);
            // void methods will always be routed to the event shards, hence no need to support query shards
            if (routingOn == true && hashCode >= targetShardHashRangeStart)
            {
                await clientAction(targetEventShardClientAndDescription.Client);
            }
            else
            {
                await clientAction(sourceEventShardClientAndDescription.Client);
            }
        }

        /// <summary>
        /// Common method for implementing routing for a method which returns a result.
        /// </summary>
        /// <typeparam name="T">The type of the result of the method.</typeparam>
        /// <param name="callingMethodName">The name of the client method (used to check whether that method is valid to be called in the context of the router's current configuration).</param>
        /// <param name="keyElementValue">The key element value of the client method's parameters (indicating the shard that the operation should be routed to).</param>
        /// <param name="hashCodeGenerator">The hash code generator for the <paramref name="keyElementValue"/> parameter.</param>
        /// <param name="clientAction">The function to invoke against the client to route to.  Accepts a single parameter which is the client to route to, and returns a task which resolves to the result of the method called against the client.</param>
        protected async Task<T> ImplementRoutingAsync<T>
        (
            String callingMethodName,
            String keyElementValue,
            IHashCodeGenerator<String> hashCodeGenerator,
            Func<IDistributedAccessManagerAsyncClient<String, String, String, String>, Task<T>> clientAction
        )
        {
            ThrowExceptionIfMethodNotValidForDataType(callingMethodName);
            Int32 hashCode = GetAndValidateHashCode(keyElementValue, hashCodeGenerator);
            // Methods with a return valuewill always be routed to the query shards, hence no need to support event shards
            if (routingOn == true && hashCode >= targetShardHashRangeStart)
            {
                return await clientAction(targetQueryShardClientAndDescription.Client);
            }
            else
            {
                return await clientAction(sourceQueryShardClientAndDescription.Client);
            }
        }

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

        /// <summary>
        /// Common implementation of overloads of method GetGroupToUserMappingsAsync().
        /// </summary>
        /// <param name="groups">The group to retrieve the users for.</param>
        /// <param name="exceptionEventDescription">A description of the event to use in an exception message in the case of error.  E.g. "retrieve group to user mappings for multiple groups from".</param>
        /// <returns>A collection of users that are mapped to the specified groups.</returns>
        protected async Task<List<String>> GetGroupToUserMappingsImplementationAsync(IEnumerable<String> groups, String exceptionEventDescription)
        {
            ThrowExceptionIfMethodNotValidForDataType(nameof(this.GetGroupToUserMappingsAsync));

            if (routingOn == false)
            {
                return await sourceQueryShardClientAndDescription.Client.GetGroupToUserMappingsAsync(groups);
            }
            else
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
                    exceptionEventDescription,
                    null,
                    null
                );

                return new List<String>(returnUsers);
            }
        }

        /// <summary>
        /// Throw an <see cref="InvalidOperationException"/> if the specified method is not valid to be called for the data element the router is configured for.
        /// </summary>
        /// <param name="methodName">The name of the method to check.</param>
        protected void ThrowExceptionIfMethodNotValidForDataType(String methodName)
        {
            if (methodNameToSupportedDataTypeMap.ContainsKey(methodName) == false)
                throw new Exception($"No supported data type definition found for method {methodName}().");
            if (methodNameToSupportedDataTypeMap[methodName].Contains(shardDataElement) == false)
                throw new InvalidOperationException($"Method {methodName}() cannot be called on {shardDataElement} shards.");
        }

        /// <summary>
        /// Generates the hash code for the specified element, and checks that it's within the specified range of hash values for the source and target shards.
        /// </summary>
        /// <param name="elementValue">The element to generate the hash code for.</param>
        /// <param name="elementValueHashCodeGenerator">The <see cref="IHashCodeGenerator{T}"/> to use to generate the hash code.</param>
        /// <returns>The hash code.</returns>
        protected Int32 GetAndValidateHashCode(String elementValue, IHashCodeGenerator<String> elementValueHashCodeGenerator)
        {
            Int32 hashCode = elementValueHashCodeGenerator.GetHashCode(elementValue);
            if (hashCode < sourceShardHashRangeStart)
                throw new Exception($"Element value '{elementValue}' with hash code {hashCode} is less than the source shard hash range start value of {sourceShardHashRangeStart}.");
            if (hashCode > targetShardHashRangeEnd)
                throw new Exception($"Element value '{elementValue}' with hash code {hashCode} is greater than the target shard hash range start end of {targetShardHashRangeEnd}.");

            return hashCode;
        }

        #endregion
    }
}
