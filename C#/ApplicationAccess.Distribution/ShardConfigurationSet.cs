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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Model/container class holding a set of <see cref="ShardConfiguration{TClientConfiguration}"/> objects.
    /// </summary>
    /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration stored in the shard configuration.</typeparam>
    public class ShardConfigurationSet<TClientConfiguration> : IEquatable<ShardConfigurationSet<TClientConfiguration>>
        where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
    {
        /// <summary>Stores the set of <see cref="ShardConfiguration{TClientConfiguration}"/>.</summary>
        protected HashSet<ShardConfiguration<TClientConfiguration>> configurationItems;

        /// <summary>
        /// The <see cref="ShardConfiguration{TClientConfiguration}"/> objects in the set.
        /// </summary>
        public IEnumerable<ShardConfiguration<TClientConfiguration>> Items 
        {
            get
            {
                return configurationItems;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.ShardConfigurationSet class.
        /// </summary>
        /// <param name="configurationItems">The <see cref="ShardConfiguration{TClientConfiguration}"/> items which make up the set.</param>
        public ShardConfigurationSet(IEnumerable<ShardConfiguration<TClientConfiguration>> configurationItems)
        {
            this.configurationItems = new HashSet<ShardConfiguration<TClientConfiguration>>();
            HashSet<ShardConfiguration<TClientConfiguration>> configurationItemKeys = new HashSet<ShardConfiguration<TClientConfiguration>>(new ShardConfigurationKeyEqualityComparer());

            foreach (ShardConfiguration<TClientConfiguration> currentConfigurationItem in configurationItems)
            {
                if (configurationItemKeys.Contains(currentConfigurationItem) == true)
                    throw new ArgumentException($"Parameter '{nameof(configurationItems)}' contains duplicate items with {nameof(ShardConfiguration<TClientConfiguration>.DataElementType)} '{currentConfigurationItem.DataElementType}', {nameof(ShardConfiguration<TClientConfiguration>.OperationType)} '{currentConfigurationItem.OperationType}', and {nameof(ShardConfiguration<TClientConfiguration>.HashRangeStart)} {currentConfigurationItem.HashRangeStart}.", nameof(configurationItems));

                configurationItemKeys.Add(currentConfigurationItem);
                this.configurationItems.Add(currentConfigurationItem);
            }
        }

        /// <inheritdoc/>
        public Boolean Equals(ShardConfigurationSet<TClientConfiguration> other)
        {
            if (configurationItems.Count != other.configurationItems.Count)
            {
                return false;
            }
            else
            {
                foreach (ShardConfiguration<TClientConfiguration> currentOtherConfigurationItem in other.configurationItems)
                {
                    if (configurationItems.Contains(currentOtherConfigurationItem) == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            // TODO: Iterating the whole set of items is not efficient, but need to implement this (strictly speaking) as part of IEquatable<T> implementation, even though
            //   I don't expect this method to be called (although Equals() will be called regularly as part of refresh checking).
            //   See if I can find a better way to do this.

            unchecked
            {
                Int32 hashCode = 1;
                foreach (ShardConfiguration<TClientConfiguration> currentConfigurationItem in configurationItems)
                {
                    hashCode *= currentConfigurationItem.GetHashCode();
                }

                return hashCode;
            }
        }

        #region Nested Classes

        /// <summary>
        /// Implementation of <see cref="IEqualityComparer{T}"/> for the key properties of <see cref="ShardConfiguration{TClientConfiguration}"/> instances (i.e. excluding the <see cref="ShardConfiguration{TClientConfiguration}.ClientConfiguration"/> property).
        /// </summary>
        protected class ShardConfigurationKeyEqualityComparer : IEqualityComparer<ShardConfiguration<TClientConfiguration>>
        {
            /// <summary>Prime number used in calculating hash code.</summary>
            protected const Int32 prime1 = 7;
            /// <summary>Prime number used in calculating hash code.</summary>
            protected const Int32 prime2 = 11;

            /// <inheritdoc/>
            public Boolean Equals(ShardConfiguration<TClientConfiguration> x, ShardConfiguration<TClientConfiguration> y)
            {
                return
                (
                    x.DataElementType == y.DataElementType &&
                    x.OperationType == y.OperationType &&
                    x.HashRangeStart == y.HashRangeStart
                );
            }

            /// <inheritdoc/>
            public Int32 GetHashCode(ShardConfiguration<TClientConfiguration> obj)
            {
                return
                (
                    prime1 * obj.DataElementType.GetHashCode() +
                    prime2 * obj.OperationType.GetHashCode() +
                    obj.HashRangeStart.GetHashCode()
                );
            }
        }

        #endregion
    }
}
