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
using System.Threading.Tasks;
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node which distributes operations to multiple shards in a distributed AccessManager implementation, and aggregates and returns their results.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public class DistributedOperationRouterNode<TClientConfiguration> 
        : IDistributedAccessManagerOperationCoordinator<TClientConfiguration>, IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        // Unlike other *Node classes, this doesn't have any background thread/worker classes like shard config refresh or event persistence.
        //   Hence this class really doesn't do anything except proxy method calls to the underlying IDistributedAccessManagerOperationCoordinator
        //   and IDistributedAccessManagerAsyncQueryProcessor instances.  However, will maintain this class in any case to preserve consistency
        //   with the hierarchy pattern of other *Node classes.

        /// <summary>The <see cref="IDistributedAccessManagerOperationCoordinator{TClientConfiguration}"/> instance which processes relevant operations received by the node.</summary>
        protected IDistributedAccessManagerOperationCoordinator<TClientConfiguration> distributedOperationCoordinator;
        /// <summary>The <see cref="IDistributedAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> instance which processes relevant operations received by the node.</summary>
        protected IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String> distributedAsyncQueryProcessor;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DistributedOperationRouterNode class.
        /// </summary>
        /// <param name="distributedOperationCoordinator">The <see cref="IDistributedAccessManagerOperationCoordinator{TClientConfiguration}"/> instance which processes relevant operations received by the node.</param>
        /// <param name="distributedAsyncQueryProcessor">The <see cref="IDistributedAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> instance which processes relevant operations received by the node.</param>
        public DistributedOperationRouterNode
        (
            IDistributedAccessManagerOperationCoordinator<TClientConfiguration> distributedOperationCoordinator,
            IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String> distributedAsyncQueryProcessor
        )
        {
            this.distributedOperationCoordinator = distributedOperationCoordinator;
            this.distributedAsyncQueryProcessor = distributedAsyncQueryProcessor;
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUsersAsync()
        {
            return await distributedOperationCoordinator.GetUsersAsync();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupsAsync()
        {
            return await distributedOperationCoordinator.GetGroupsAsync();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityTypesAsync()
        {
            return await distributedOperationCoordinator.GetEntityTypesAsync();
        }

        /// <inheritdoc/>
        public async Task AddUserAsync(String user)
        {
            await distributedOperationCoordinator.AddUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsUserAsync(String user)
        {
            return await distributedOperationCoordinator.ContainsUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task RemoveUserAsync(String user)
        {
            await distributedOperationCoordinator.RemoveUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task AddGroupAsync(String group)
        {
            await distributedOperationCoordinator.AddGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsGroupAsync(String group)
        {
            return await distributedOperationCoordinator.ContainsGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupAsync(String group)
        {
            await distributedOperationCoordinator.RemoveGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task AddUserToGroupMappingAsync(String user, String group)
        {
            await distributedOperationCoordinator.AddUserToGroupMappingAsync(user, group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToGroupMappingsAsync(String user, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetUserToGroupMappingsAsync(user, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetGroupToUserMappingsAsync(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(IEnumerable<String> groups)
        {
            return await distributedAsyncQueryProcessor.GetGroupToUserMappingsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToGroupMappingAsync(String user, String group)
        {
            await distributedOperationCoordinator.RemoveUserToGroupMappingAsync(user, group);
        }

        /// <inheritdoc/>
        public async Task AddGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            await distributedOperationCoordinator.AddGroupToGroupMappingAsync(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetGroupToGroupMappingsAsync(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupMappingsAsync(IEnumerable<String> groups)
        {
            return await distributedAsyncQueryProcessor.GetGroupToGroupMappingsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupReverseMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetGroupToGroupReverseMappingsAsync(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupReverseMappingsAsync(IEnumerable<String> groups)
        {
            return await distributedAsyncQueryProcessor.GetGroupToGroupReverseMappingsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            await distributedOperationCoordinator.RemoveGroupToGroupMappingAsync(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            await distributedOperationCoordinator.AddUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(String user)
        {
            return await distributedOperationCoordinator.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetApplicationComponentAndAccessLevelToUserMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetApplicationComponentAndAccessLevelToUserMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            await distributedOperationCoordinator.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            await distributedOperationCoordinator.AddGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(String group)
        {
            return await distributedOperationCoordinator.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetApplicationComponentAndAccessLevelToGroupMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            await distributedOperationCoordinator.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task AddEntityTypeAsync(String entityType)
        {
            await distributedOperationCoordinator.AddEntityTypeAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityTypeAsync(String entityType)
        {
            return await distributedOperationCoordinator.ContainsEntityTypeAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task RemoveEntityTypeAsync(String entityType)
        {
            await distributedOperationCoordinator.RemoveEntityTypeAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task AddEntityAsync(String entityType, String entity)
        {
            await distributedOperationCoordinator.AddEntityAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAsync(String entityType)
        {
            return await distributedOperationCoordinator.GetEntitiesAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityAsync(String entityType, String entity)
        {
            return await distributedOperationCoordinator.ContainsEntityAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task RemoveEntityAsync(String entityType, String entity)
        {
            await distributedOperationCoordinator.RemoveEntityAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task AddUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            await distributedOperationCoordinator.AddUserToEntityMappingAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(String user)
        {
            return await distributedOperationCoordinator.GetUserToEntityMappingsAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToEntityMappingsAsync(String user, String entityType)
        {
            return await distributedOperationCoordinator.GetUserToEntityMappingsAsync(user, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityToUserMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetEntityToUserMappingsAsync(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            await distributedOperationCoordinator.RemoveUserToEntityMappingAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task AddGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            await distributedOperationCoordinator.AddGroupToEntityMappingAsync(group, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(String group)
        {
            return await distributedOperationCoordinator.GetGroupToEntityMappingsAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToEntityMappingsAsync(String group, String entityType)
        {
            return await distributedOperationCoordinator.GetGroupToEntityMappingsAsync(group, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityToGroupMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetEntityToGroupMappingsAsync(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            await distributedOperationCoordinator.RemoveGroupToEntityMappingAsync(group, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(String user, String applicationComponent, String accessLevel)
        {
            return await distributedOperationCoordinator.HasAccessToApplicationComponentAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(IEnumerable<String> groups, String applicationComponent, String accessLevel)
        {
            return await distributedAsyncQueryProcessor.HasAccessToApplicationComponentAsync(groups, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(String user, String entityType, String entity)
        {
            return await distributedOperationCoordinator.HasAccessToEntityAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(IEnumerable<String> groups, String entityType, String entity)
        {
            return await distributedAsyncQueryProcessor.HasAccessToEntityAsync(groups, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByUserAsync(String user)
        {
            return await distributedOperationCoordinator.GetApplicationComponentsAccessibleByUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupAsync(String group)
        {
            return await distributedOperationCoordinator.GetApplicationComponentsAccessibleByGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            return await distributedAsyncQueryProcessor.GetApplicationComponentsAccessibleByGroupsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(String user)
        {
            return await distributedOperationCoordinator.GetEntitiesAccessibleByUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByUserAsync(String user, String entityType)
        {
            return await distributedOperationCoordinator.GetEntitiesAccessibleByUserAsync(user, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(String group)
        {
            return await distributedOperationCoordinator.GetEntitiesAccessibleByGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            return await distributedAsyncQueryProcessor.GetEntitiesAccessibleByGroupsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupAsync(String group, String entityType)
        {
            return await distributedOperationCoordinator.GetEntitiesAccessibleByGroupAsync(group, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups, String entityType)
        {
            return await distributedAsyncQueryProcessor.GetEntitiesAccessibleByGroupsAsync(groups, entityType);
        }
    }
}
