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
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node which coordinates operations in an AccessManager implementation where responsibility for subsets of elements is distributed across multiple computers in shards.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration used to create clients to connect to the shards.</typeparam>
    /// <typeparam name="TClientConfigurationJsonSerializer">An implementation of <see cref="IDistributedAccessManagerAsyncClientConfigurationJsonSerializer{T}"/> which serializes <typeparamref name="TClientConfiguration"/> instances.</typeparam>
    public class DistributedOperationCoordinatorNode<TClientConfiguration, TClientConfigurationJsonSerializer> 
        : IDistributedAccessManagerOperationCoordinator<TClientConfiguration>, IDisposable
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
        where TClientConfigurationJsonSerializer : IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<TClientConfiguration>
    {
        /// <summary>Manages the clients used to connect to shards managing the subsets of elements in the distributed access manager implementation.</summary>
        protected IShardClientManager<TClientConfiguration> shardClientManager;
        /// <summary>The strategy/methodology to use to refresh the shard configuration.</summary>
        protected IDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy shardConfigurationRefreshStrategy;
        /// <summary>The <see cref="IDistributedAccessManagerOperationCoordinator{TClientConfiguration}"/> instance which processes any operations received by the node.</summary>
        protected IDistributedAccessManagerOperationCoordinator<TClientConfiguration> distributedOperationCoordinator;
        /// <summary>Implementation of <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> used to read shard configuration from persistent storage.</summary>
        protected IShardConfigurationSetPersister<TClientConfiguration, TClientConfigurationJsonSerializer> shardConfigurationSetPersister;
        /// <summary>The delegate which handles a <see cref="IDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy.ShardConfigurationRefreshed">ShardConfigurationRefreshed</see> event.</summary>
        protected EventHandler shardConfigurationRefreshedEventHandler;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.DistributedOperationCoordinatorNode class.
        /// </summary>
        /// <param name="shardClientManager">Manages the clients used to connect to shards managing the subsets of elements in the distributed access manager implementation.</param>
        /// <param name="shardConfigurationRefreshStrategy">The strategy/methodology to use to refresh the shard configuration.</param>
        /// <param name="distributedOperationCoordinator">The <see cref="IDistributedAccessManagerOperationCoordinator{TClientConfiguration}"/> instance which processes any operations received by the node.</param>
        /// <param name="shardConfigurationSetPersister">Implementation of <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> used to read shard configuration from persistent storage.</param>
        /// <remarks>The shard client manager in parameter <paramref name="shardClientManager"/> must be the same instance used by the distributed operation coordinator in parameter <paramref name="distributedOperationCoordinator"/>.</remarks>
        public DistributedOperationCoordinatorNode
        (
            IShardClientManager<TClientConfiguration> shardClientManager, 
            IDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy shardConfigurationRefreshStrategy,
            IDistributedAccessManagerOperationCoordinator<TClientConfiguration> distributedOperationCoordinator,
            IShardConfigurationSetPersister<TClientConfiguration, TClientConfigurationJsonSerializer> shardConfigurationSetPersister
        )
        {
            this.shardClientManager = shardClientManager;
            this.shardConfigurationRefreshStrategy = shardConfigurationRefreshStrategy;
            this.distributedOperationCoordinator = distributedOperationCoordinator;
            this.shardConfigurationSetPersister = shardConfigurationSetPersister;
            shardConfigurationRefreshedEventHandler = (Object sender, EventArgs e) => { RefreshShardConfiguration(); };
            shardConfigurationRefreshStrategy.ShardConfigurationRefreshed += shardConfigurationRefreshedEventHandler;
            disposed = false;
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
        public async Task<List<String>> GetGroupToGroupReverseMappingsAsync(String group, Boolean includeIndirectMappings)
        {
            return await distributedOperationCoordinator.GetGroupToGroupReverseMappingsAsync(group, includeIndirectMappings);
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
        public async Task<Boolean> HasAccessToEntityAsync(String user, String entityType, String entity)
        {
            return await distributedOperationCoordinator.HasAccessToEntityAsync(user, entityType, entity);
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
        public async Task<List<String>> GetEntitiesAccessibleByGroupAsync(String group, String entityType)
        {
            return await distributedOperationCoordinator.GetEntitiesAccessibleByGroupAsync(group, entityType);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Refreshes the shard configuration with the configuration currently in persistent storage.
        /// </summary>
        /// <exception cref="ShardConfigurationRefreshException">An error occurred whilst refreshing the shard configuration.</exception>
        protected void RefreshShardConfiguration()
        {
            ShardConfigurationSet<TClientConfiguration> currentConfiguration = null;
            try
            {
                currentConfiguration = shardConfigurationSetPersister.Read();
            }
            catch (Exception e)
            {
                throw new ShardConfigurationRefreshException("Failed to read shard configuration from persistent storage.", e);
            }
            try
            {
                shardClientManager.RefreshConfiguration(currentConfiguration);
            }
            catch (Exception e)
            {
                throw new ShardConfigurationRefreshException("Failed to refresh shard configuration in shard client manager.", e);
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the DistributedOperationCoordinatorNode.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~DistributedOperationCoordinatorNode()
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
                    shardConfigurationRefreshStrategy.ShardConfigurationRefreshed -= shardConfigurationRefreshedEventHandler;
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
