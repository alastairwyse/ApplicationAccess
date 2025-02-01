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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node which operations to two sets of shards in a distributed AccessManager implementation, and aggregates and returns their results.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    public class DistributedOperationRouterNode<TClientConfiguration> 
        : IDistributedAccessManagerOperationRouter
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        // Unlike other *Node classes, this doesn't have any background thread/worker classes like shard config refresh or event persistence.
        //   Hence this class really doesn't do anything except proxy method calls to the underlying IDistributedAccessManagerOperationRouter
        //   instance.  However, will maintain this class in any case to preserve consistency with the hierarchy pattern of other *Node classes.

        /// <summary>The <see cref="IDistributedAccessManagerOperationRouter"/> instance which processes relevant operations received by the node.</summary>
        protected IDistributedAccessManagerOperationRouter distributedOperationRouter;

        /// <inheritdoc/>
        public Boolean RoutingOn
        {
            set
            {
                distributedOperationRouter.RoutingOn = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DistributedOperationRouterNode class.
        /// </summary>
        /// <param name="distributedOperationRouter">The <see cref="IDistributedAccessManagerOperationRouter"/> instance which processes relevant operations received by the node.</param>
        public DistributedOperationRouterNode(IDistributedAccessManagerOperationRouter distributedOperationRouter)
        {
            this.distributedOperationRouter = distributedOperationRouter;
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUsersAsync()
        {
            return await distributedOperationRouter.GetUsersAsync();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupsAsync()
        {
            return await distributedOperationRouter.GetGroupsAsync();
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityTypesAsync()
        {
            return await distributedOperationRouter.GetEntityTypesAsync();
        }

        /// <inheritdoc/>
        public async Task AddUserAsync(String user)
        {
            await distributedOperationRouter.AddUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsUserAsync(String user)
        {
            return await distributedOperationRouter.ContainsUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task RemoveUserAsync(String user)
        {
            await distributedOperationRouter.RemoveUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task AddGroupAsync(String group)
        {
            await distributedOperationRouter.AddGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsGroupAsync(String group)
        {
            return await distributedOperationRouter.ContainsGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupAsync(String group)
        {
            await distributedOperationRouter.RemoveGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task AddUserToGroupMappingAsync(String user, String group)
        {
            await distributedOperationRouter.AddUserToGroupMappingAsync(user, group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToGroupMappingsAsync(String user, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetUserToGroupMappingsAsync(user, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetGroupToUserMappingsAsync(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToUserMappingsAsync(IEnumerable<String> groups)
        {
            return await distributedOperationRouter.GetGroupToUserMappingsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToGroupMappingAsync(String user, String group)
        {
            await distributedOperationRouter.RemoveUserToGroupMappingAsync(user, group);
        }

        /// <inheritdoc/>
        public async Task AddGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            await distributedOperationRouter.AddGroupToGroupMappingAsync(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetGroupToGroupMappingsAsync(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupMappingsAsync(IEnumerable<String> groups)
        {
            return await distributedOperationRouter.GetGroupToGroupMappingsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupReverseMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetGroupToGroupReverseMappingsAsync(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToGroupReverseMappingsAsync(IEnumerable<String> groups)
        {
            return await distributedOperationRouter.GetGroupToGroupReverseMappingsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToGroupMappingAsync(String fromGroup, String toGroup)
        {
            await distributedOperationRouter.RemoveGroupToGroupMappingAsync(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            await distributedOperationRouter.AddUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToApplicationComponentAndAccessLevelMappingsAsync(String user)
        {
            return await distributedOperationRouter.GetUserToApplicationComponentAndAccessLevelMappingsAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetApplicationComponentAndAccessLevelToUserMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync(String user, String applicationComponent, String accessLevel)
        {
            await distributedOperationRouter.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            await distributedOperationRouter.AddGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToApplicationComponentAndAccessLevelMappingsAsync(String group)
        {
            return await distributedOperationRouter.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetApplicationComponentAndAccessLevelToGroupMappingsAsync(String applicationComponent, String accessLevel, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(String group, String applicationComponent, String accessLevel)
        {
            await distributedOperationRouter.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task AddEntityTypeAsync(String entityType)
        {
            await distributedOperationRouter.AddEntityTypeAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityTypeAsync(String entityType)
        {
            return await distributedOperationRouter.ContainsEntityTypeAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task RemoveEntityTypeAsync(String entityType)
        {
            await distributedOperationRouter.RemoveEntityTypeAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task AddEntityAsync(String entityType, String entity)
        {
            await distributedOperationRouter.AddEntityAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAsync(String entityType)
        {
            return await distributedOperationRouter.GetEntitiesAsync(entityType);
        }

        /// <inheritdoc/>
        public async Task<Boolean> ContainsEntityAsync(String entityType, String entity)
        {
            return await distributedOperationRouter.ContainsEntityAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task RemoveEntityAsync(String entityType, String entity)
        {
            await distributedOperationRouter.RemoveEntityAsync(entityType, entity);
        }

        /// <inheritdoc/>
        public async Task AddUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            await distributedOperationRouter.AddUserToEntityMappingAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetUserToEntityMappingsAsync(String user)
        {
            return await distributedOperationRouter.GetUserToEntityMappingsAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetUserToEntityMappingsAsync(String user, String entityType)
        {
            return await distributedOperationRouter.GetUserToEntityMappingsAsync(user, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityToUserMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetEntityToUserMappingsAsync(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveUserToEntityMappingAsync(String user, String entityType, String entity)
        {
            await distributedOperationRouter.RemoveUserToEntityMappingAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task AddGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            await distributedOperationRouter.AddGroupToEntityMappingAsync(group, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetGroupToEntityMappingsAsync(String group)
        {
            return await distributedOperationRouter.GetGroupToEntityMappingsAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetGroupToEntityMappingsAsync(String group, String entityType)
        {
            return await distributedOperationRouter.GetGroupToEntityMappingsAsync(group, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntityToGroupMappingsAsync(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return await distributedOperationRouter.GetEntityToGroupMappingsAsync(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public async Task RemoveGroupToEntityMappingAsync(String group, String entityType, String entity)
        {
            await distributedOperationRouter.RemoveGroupToEntityMappingAsync(group, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(String user, String applicationComponent, String accessLevel)
        {
            return await distributedOperationRouter.HasAccessToApplicationComponentAsync(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToApplicationComponentAsync(IEnumerable<String> groups, String applicationComponent, String accessLevel)
        {
            return await distributedOperationRouter.HasAccessToApplicationComponentAsync(groups, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(String user, String entityType, String entity)
        {
            return await distributedOperationRouter.HasAccessToEntityAsync(user, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<Boolean> HasAccessToEntityAsync(IEnumerable<String> groups, String entityType, String entity)
        {
            return await distributedOperationRouter.HasAccessToEntityAsync(groups, entityType, entity);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByUserAsync(String user)
        {
            return await distributedOperationRouter.GetApplicationComponentsAccessibleByUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupAsync(String group)
        {
            return await distributedOperationRouter.GetApplicationComponentsAccessibleByGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetApplicationComponentsAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            return await distributedOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByUserAsync(String user)
        {
            return await distributedOperationRouter.GetEntitiesAccessibleByUserAsync(user);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByUserAsync(String user, String entityType)
        {
            return await distributedOperationRouter.GetEntitiesAccessibleByUserAsync(user, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupAsync(String group)
        {
            return await distributedOperationRouter.GetEntitiesAccessibleByGroupAsync(group);
        }

        /// <inheritdoc/>
        public async Task<List<Tuple<String, String>>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups)
        {
            return await distributedOperationRouter.GetEntitiesAccessibleByGroupsAsync(groups);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupAsync(String group, String entityType)
        {
            return await distributedOperationRouter.GetEntitiesAccessibleByGroupAsync(group, entityType);
        }

        /// <inheritdoc/>
        public async Task<List<String>> GetEntitiesAccessibleByGroupsAsync(IEnumerable<String> groups, String entityType)
        {
            return await distributedOperationRouter.GetEntitiesAccessibleByGroupsAsync(groups, entityType);
        }

        /// <inheritdoc/>
        public void PauseOperations()
        {
            distributedOperationRouter.PauseOperations();
        }

        /// <inheritdoc/>
        public void ResumeOperations()
        {
            distributedOperationRouter.ResumeOperations();
        }
    }
}
