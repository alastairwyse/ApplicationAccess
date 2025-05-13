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
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Models;
using MoreComplexDataStructures;

namespace ApplicationAccess.Redistribution.Kubernetes.Models
{
    /// <summary>
    /// Model/container class holding a set of <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> objects, and maintaining uniqueness of items in the set.
    /// </summary>
    /// <typeparam name="TPersistentStorageCredentials">The type of AccessManager client configuration stored in the shard configuration.</typeparam>
    public class KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials>
        where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
    {
        /// <summary>Underlying storage for the items in the set.</summary>
        protected WeightBalancedTree<HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials>> shardGroupConfigurationTree;

        /// <summary>
        /// The number of shard group configuration items in the set.
        /// </summary>
        public Int32 Count
        {
            get { return shardGroupConfigurationTree.Count; }
        }

        /// <summary>
        /// The <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> items in the set.
        /// </summary>
        public IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> Items
        {
            get
            {
                List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> returnList = new();
                foreach(HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials> currentItem in shardGroupConfigurationTree)
                {
                    returnList.Add(currentItem.ShardGroupConfiguration);
                }

                return returnList;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.Models.KubernetesShardGroupConfigurationSet class.
        /// </summary>
        public KubernetesShardGroupConfigurationSet()
            : this(Enumerable.Empty<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>())
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.Models.KubernetesShardGroupConfigurationSet class.
        /// </summary>
        /// <param name="items">The items to initially populate the set with.</param>
        public KubernetesShardGroupConfigurationSet(IEnumerable<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> items)
        {
            shardGroupConfigurationTree = new WeightBalancedTree<HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials>>();
            foreach (KubernetesShardGroupConfiguration<TPersistentStorageCredentials> currentItem in items)
            {
                try
                {
                    Add(currentItem);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Failed to add all items in parameter '{nameof(items)}' to the set.", nameof(items), e);
                }
            }
        }

        /// <summary>
        /// Adds a new items to the set.
        /// </summary>
        /// <param name="newItem">The item to add.</param>
        public void Add(KubernetesShardGroupConfiguration<TPersistentStorageCredentials> newItem)
        {
            HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials> treeItem = new(newItem);
            if (shardGroupConfigurationTree.Contains(treeItem) == true)
                throw new ArgumentException($"An item with hash range start value {newItem.HashRangeStart} specified in parameter '{nameof(newItem)}' already exists in the set.", nameof(newItem));

            shardGroupConfigurationTree.Add(treeItem);
        }

        /// <summary>
        /// Adds a collection of new items to the set.
        /// </summary>
        /// <param name="newItems">The items to add.</param>
        public void AddRange(IEnumerable<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> newItems)
        {
            foreach (KubernetesShardGroupConfiguration<TPersistentStorageCredentials> currentItem in newItems)
            {
                HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials> treeItem = new(currentItem);
                if (shardGroupConfigurationTree.Contains(treeItem) == true)
                    throw new ArgumentException($"An item with hash range start value {currentItem.HashRangeStart} specified in an item of parameter '{nameof(newItems)}' already exists in the set.", nameof(newItems));

                shardGroupConfigurationTree.Add(treeItem);
            }
        }

        /// <summary>
        /// Removes an item from the set.
        /// </summary>
        /// <param name="itemHashRangeStart">The hash range start value of the item to remove.</param>
        public void Remove(Int32 itemHashRangeStart)
        {
            ThrowExceptionIfSetDoesntContainItemWithHashRangeStartValue(itemHashRangeStart, nameof(itemHashRangeStart));

            shardGroupConfigurationTree.Remove(new HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials>(CreateTreeSearchItem(itemHashRangeStart)));
        }

        /// <summary>
        /// Checks whether an item with the specified hash range start value exists in the set.
        /// </summary>
        /// <param name="hashRangeStart">The hash range start value to check for.</param>
        /// <returns>True if an item with the specified hash range start value exists.  False otherwise.</returns>
        public Boolean ContainsHashRangeStart(Int32 hashRangeStart)
        {
            return shardGroupConfigurationTree.Contains(new HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials>(CreateTreeSearchItem(hashRangeStart)));
        }

        /// <summary>
        /// Updates the node client configuration properties of an items in the set.
        /// </summary>
        /// <param name="itemHashRangeStart">The hash range start value of the item to update.</param>
        /// <param name="readerNodeClientConfiguration">The updated reader node client configuration.</param>
        /// <param name="writerNodeClientConfiguration">The updated writer node client configuration.</param>
        public void UpdateRestClientConfiguration(Int32 itemHashRangeStart, AccessManagerRestClientConfiguration readerNodeClientConfiguration, AccessManagerRestClientConfiguration writerNodeClientConfiguration)
        {
            ThrowExceptionIfSetDoesntContainItemWithHashRangeStartValue(itemHashRangeStart, nameof(itemHashRangeStart));

            HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials> updateItem = shardGroupConfigurationTree.Get(new HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials>(CreateTreeSearchItem(itemHashRangeStart)));
            updateItem.ShardGroupConfiguration.ReaderNodeClientConfiguration = readerNodeClientConfiguration;
            updateItem.ShardGroupConfiguration.WriterNodeClientConfiguration = writerNodeClientConfiguration;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a fake <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> instance with only the hash range start value set to use to search within the tree field.
        /// </summary>
        /// <param name="hashRangeStart">The hash range start value.</param>
        /// <returns></returns>
        protected KubernetesShardGroupConfiguration<TPersistentStorageCredentials> CreateTreeSearchItem(Int32 hashRangeStart)
        {
            return new(hashRangeStart, null, null, null);
        }

        #pragma warning disable 1591

        protected void ThrowExceptionIfSetDoesntContainItemWithHashRangeStartValue(Int32 itemHashRangeStart, String itemHashRangeStartParameterName)
        {
            HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials> treeItem = new(CreateTreeSearchItem(itemHashRangeStart));
            if (shardGroupConfigurationTree.Contains(treeItem) == false)
                throw new ArgumentException($"No item with hash range start value {itemHashRangeStart} specified in parameter '{itemHashRangeStartParameterName}' exists in the set.", itemHashRangeStartParameterName);
        }

        #pragma warning restore 1591

        #endregion

        #region Nested Classes

        /// <summary>
        /// Model/container class holding a hash range start value and a <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> instance.
        /// </summary>
        /// <typeparam name="TPersistentStorageCredentials">The type of AccessManager client configuration stored in the shard configuration.</typeparam>
        /// <remarks>Used to store the <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> instance property in a binary search tree, and sort by the hash range start.</remarks>
        protected class HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials> : IComparable<HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials>>
            where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
        {
            /// <summary>The hash range start value of the <see cref="HashRangeStartAndKubernetesShardGroupConfiguration{TPersistentStorageCredentials}.ShardGroupConfiguration">ShardGroupConfiguration</see> property.</summary>
            public Int32 HashRangeStart { get; protected set; }

            /// <summary>The configuration of a shard group in a Kubernetes distributed AccessManager instance.</summary>
            public KubernetesShardGroupConfiguration<TPersistentStorageCredentials> ShardGroupConfiguration { get; protected set; }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.Models.KubernetesShardGroupConfigurationSet+HashRangeStartAndKubernetesShardGroupConfiguration class.
            /// </summary>
            /// <param name="shardGroupConfiguration">The configuration of a shard group in a Kubernetes distributed AccessManager instance.</param>
            public HashRangeStartAndKubernetesShardGroupConfiguration(KubernetesShardGroupConfiguration<TPersistentStorageCredentials> shardGroupConfiguration)
            {
                HashRangeStart = shardGroupConfiguration.HashRangeStart;
                ShardGroupConfiguration = shardGroupConfiguration;
            }

            /// <inheritdoc/>
            public Int32 CompareTo(HashRangeStartAndKubernetesShardGroupConfiguration<TPersistentStorageCredentials> other)
            {
                return HashRangeStart.CompareTo(other.HashRangeStart);
            }
        }

        #endregion
    }
}
