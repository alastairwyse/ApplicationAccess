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
using System.Text.Json;
using ApplicationAccess.Persistence.Sql.SqlServer;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Redistribution.Persistence.SqlServer
{
    /// <summary>
    /// Implementation of <see cref="IPersistentStorageCredentialsAppSettingsConfigurer{TPersistentStorageCredentials}"/> for adding <see cref="SqlServerLoginCredentials"/> to 'appsettings,json' configuration.
    /// </summary>
    public class SqlServerCredentialsAppSettingsConfigurer : IPersistentStorageCredentialsAppSettingsConfigurer<SqlServerLoginCredentials>
    {
        #pragma warning disable 1591

        protected const String appsettingsAccessManagerSqlDatabaseConnectionPropertyName = "AccessManagerSqlDatabaseConnection";
        protected const String appsettingsConnectionParametersPropertyName = "ConnectionParameters";
        protected const String appsettingsConnectionStringPropertyName = "ConnectionString";

        #pragma warning restore 1591

        /// <summary>
        /// Configures a <see cref="JObject"/> containing an AccessManager component's 'appsettings.json' configuration with Microsoft SQL Server credentials.
        /// </summary>
        /// <param name="persistentStorageCredentials">The SQL Server credentials.</param>
        /// <param name="appsettingsJson">The 'appsettings.json' configuration.</param>
        public void ConfigureAppsettingsJsonWithPersistentStorageCredentials(SqlServerLoginCredentials persistentStorageCredentials, JObject appsettingsJson)
        {
            String connectionParametersPath = $"{appsettingsAccessManagerSqlDatabaseConnectionPropertyName}.{appsettingsConnectionParametersPropertyName}";
            try
            {
                appsettingsJson.SelectToken(connectionParametersPath, true);
            }
            catch (Exception e)
            {
                throw new Exception($"JSON path '{connectionParametersPath}' was not found in the specified 'appsettings.json' configuration.", e);
            }

            appsettingsJson[appsettingsAccessManagerSqlDatabaseConnectionPropertyName][appsettingsConnectionParametersPropertyName][appsettingsConnectionStringPropertyName] = persistentStorageCredentials.ConnectionString;
        }
    }
}
