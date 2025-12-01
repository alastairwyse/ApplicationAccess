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
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Defines methods to add persistent storage credentials to AccessManager component's 'appsettings.json' configuration.
    /// </summary>
    public interface IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> where TPersistentStorageCredentials : IPersistentStorageLoginCredentials
    {
        /// <summary>
        /// Configures a <see cref="JObject"/> containing an AccessManager component's 'appsettings.json' configuration with persistent storage credentials.
        /// </summary>
        /// <param name="persistentStorageCredentials">The persistent storage credentials.</param>
        /// <param name="appsettingsJson">The 'appsettings.json' configuration.</param>
        void ConfigureAppsettingsJsonWithPersistentStorageCredentials(TPersistentStorageCredentials persistentStorageCredentials, JObject appsettingsJson);

        /// <summary>
        /// Configures a <see cref="JObject"/> containing an AccessManager component's 'appsettings.json' configuration (which includes partial persistent storage credentials) with a persistent storage instance name.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the persistent storage instance (e.g. database name).</param>
        /// <param name="appsettingsJson">The 'appsettings.json' configuration.</param>
        void ConfigureAppsettingsJsonWithPersistentStorageInstanceName(String persistentStorageInstanceName, JObject appsettingsJson);
    }
}
