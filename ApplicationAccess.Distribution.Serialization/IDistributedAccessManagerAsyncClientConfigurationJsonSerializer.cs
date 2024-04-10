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
using ApplicationAccess.Distribution;

namespace ApplicationAccess.Distribution.Serialization
{
    /// <summary>
    /// Defines methods which serialize and deserialize implementations of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> to and from JSON documents.
    /// </summary>
    /// <typeparam name="T">An implementation of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> to serialize and deserialize.</typeparam>
    public interface IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<T>
        where T : IDistributedAccessManagerAsyncClientConfiguration
    {
        /// <summary>
        /// Serializes the specified implementation of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> to a JSON document converted to a string.
        /// </summary>
        /// <param name="clientConfiguration">The <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> to serialize.</param>
        /// <returns>The <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> serialized as a JSON document converted to a string.</returns>
        String Serialize(T clientConfiguration);

        /// <summary>
        /// Deserializes a <see cref="IDistributedAccessManagerAsyncClientConfiguration"/> from a JSON document formatted as a string.
        /// </summary>
        /// <param name="serializedClientConfiguration">The string to deserialize.</param>
        /// <returns>The deserialized implementation of <see cref="IDistributedAccessManagerAsyncClientConfiguration"/>.</returns>
        T Deserialize(String serializedClientConfiguration);
    }
}
