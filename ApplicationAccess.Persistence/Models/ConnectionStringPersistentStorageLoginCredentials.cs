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

namespace ApplicationAccess.Persistence.Models
{
    /// <summary>
    /// Base for model/container classes which store credentials for logging into a persistent storage component, in the form of a connection string.
    /// </summary>
    public abstract class ConnectionStringPersistentStorageLoginCredentials : IPersistentStorageLoginCredentials
    {
        /// <summary>The connection string for the persistent storage component.</summary>
        public String ConnectionString { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Models.ConnectionStringPersistentStorageLoginCredentials class.
        /// </summary>
        /// <param name="connectionString">The connection string for the persistent storage component.</param>
        public ConnectionStringPersistentStorageLoginCredentials(String connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{nameof(connectionString)}' must contain a value.", nameof(connectionString));

            this.ConnectionString = connectionString;
        }
    }
}
