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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Defines methods for managing persistent storage instances in a distributed AccessManager implementation.
    /// </summary>
    /// <typeparam name="TPersistentStorageCredentials">An implementation of <see cref="IPersistentStorageLoginCredentials"/> containing login credentials for created persistent storage instances.</typeparam>
    public interface IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> where TPersistentStorageCredentials : IPersistentStorageLoginCredentials
    {
        /// <summary>
        /// Creates a new distributed AccessManager persistent storage instance.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the persistent storage instance.</param>
        /// <returns>Credentials which can be used to connect to the persistent storage instance.</returns>
        TPersistentStorageCredentials CreateAccessManagerPersistentStorage(String persistentStorageInstanceName);

        /// <summary>
        /// Creates a new distributed AccessManager configuration persistent storage instance.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the persistent storage instance.</param>
        /// <returns>Credentials which can be used to connect to the persistent storage instance.</returns>
        TPersistentStorageCredentials CreateAccessManagerConfigurationPersistentStorage(String persistentStorageInstanceName);

        /// <summary>
        /// Renames a persistent storage instance.
        /// </summary>
        /// <param name="currentPersistentStorageInstanceName">The current name of the persistent storage instance.</param>
        /// <param name="newPersistentStorageInstanceName">The new name of the persistent storage instance.</param>
        void RenamePersistentStorage(String currentPersistentStorageInstanceName, String newPersistentStorageInstanceName);

        /// <summary>
        /// Deletes a persistent storage instance.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the persistent storage instance.</param>
        void DeletePersistentStorage(String persistentStorageInstanceName);
    }
}
