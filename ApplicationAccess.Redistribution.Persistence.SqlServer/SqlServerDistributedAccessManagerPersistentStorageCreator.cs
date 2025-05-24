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
using System.IO;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution;

namespace ApplicationAccess.Redistribution.Persistence.SqlServer
{
    /// <summary>
    /// Creates SQL Server database instances for a distributed AccessManager implementation.
    /// </summary>
    public class SqlServerDistributedAccessManagerPersistentStorageCreator : IDistributedAccessManagerPersistentStorageCreator<SqlServerLoginCredentials>
    {
        #pragma warning disable 1591

        protected const String accessManagerDatabaseCreateScriptPath = "./Resources/CreateDatabase.sql";
        protected const String accessManagerDatabaseUpdateScriptPath = "./Resources/ApplicationAccess/UpdateDatabase.sql";
        protected const String accessManagerConfigurationDatabaseCreateScriptPath = "./Resources/ApplicationAccessConfiguration/CreateDatabase.sql";
        protected const String applicationAccessDatabaseNameSetvarStatement = ":Setvar DatabaseName ApplicationAccess";
        protected const String applicationAccessConfigurationDatabaseNameSetvarStatement = ":Setvar DatabaseName ApplicationAccessConfiguration";
        protected const String databaseNameWildcard = "$(DatabaseName)";

        #pragma warning restore 1591

        /// <summary>Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to the <see cref="File"/> class.</summary>
        protected IFileShim fileShim;
        /// <summary>Executes scripts against the SQL database.</summary>
        protected ISqlServerScriptExecutor scriptExecutor;
        /// <summary>The connection string to use to connect to SQL Server.</summary>
        protected String connectionString;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Persistence.SqlServer.SqlServerDistributedAccessManagerPersistentStorageCreator class.
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to SQL Server.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public SqlServerDistributedAccessManagerPersistentStorageCreator(String connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{nameof(connectionString)}' must contain a value.", nameof(connectionString));

            fileShim = new DefaultFileShim();
            scriptExecutor = new SqlServerScriptExecutor(connectionString);
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Persistence.SqlServer.SqlServerDistributedAccessManagerPersistentStorageCreator class.
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to SQL Server.</param>
        /// <param name="mockFileShim">A mock <see cref="IFileShim"/>.</param>
        /// <param name="mockScriptExecutor">A mock <see cref="ISqlServerScriptExecutor"/>.</param>
        public SqlServerDistributedAccessManagerPersistentStorageCreator(String connectionString, IFileShim mockFileShim, ISqlServerScriptExecutor mockScriptExecutor)
            : this(connectionString)
        {
            this.fileShim = mockFileShim;
            this.scriptExecutor = mockScriptExecutor;
        }

        /// <summary>
        /// Creates a new distributed AccessManager database instance.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the databse instance.</param>
        /// <returns>Credentials which can be used to connect to the database.</returns>
        public SqlServerLoginCredentials CreateAccessManagerPersistentStorage(string persistentStorageInstanceName)
        {
            // Read scripts from files
            String createDatabaseScript = "";
            try
            {
                createDatabaseScript = fileShim.ReadAllText(accessManagerDatabaseCreateScriptPath);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to read AccessManager create script from path '{accessManagerDatabaseCreateScriptPath}'.", e);
            }
            String updateDatabaseScript = "";
            try
            {
                updateDatabaseScript = fileShim.ReadAllText(accessManagerDatabaseUpdateScriptPath);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to read AccessManager update script from path '{accessManagerDatabaseUpdateScriptPath}'.", e);
            }

            // Check and populate variables in scripts
            if (createDatabaseScript.Contains(applicationAccessDatabaseNameSetvarStatement) == false)
                throw new Exception($"AccessManager create script at path '{accessManagerDatabaseCreateScriptPath}' did not contain 'Setvar' statement '{applicationAccessDatabaseNameSetvarStatement}'.");
            if (createDatabaseScript.Contains(databaseNameWildcard) == false)
                throw new Exception($"AccessManager create script at path '{accessManagerDatabaseCreateScriptPath}' did not contain any database name wildcards.");
            if (updateDatabaseScript.Contains(applicationAccessDatabaseNameSetvarStatement) == false)
                throw new Exception($"AccessManager update script at path '{accessManagerDatabaseUpdateScriptPath}' did not contain 'Setvar' statement '{applicationAccessDatabaseNameSetvarStatement}'.");
            if (updateDatabaseScript.Contains(databaseNameWildcard) == false)
                throw new Exception($"AccessManager update script at path '{accessManagerDatabaseUpdateScriptPath}' did not contain any database name wildcards.");
            createDatabaseScript = createDatabaseScript.Replace(applicationAccessDatabaseNameSetvarStatement, "");
            createDatabaseScript = createDatabaseScript.Replace(databaseNameWildcard, persistentStorageInstanceName);
            updateDatabaseScript = updateDatabaseScript.Replace(applicationAccessDatabaseNameSetvarStatement, "");
            updateDatabaseScript = updateDatabaseScript.Replace(databaseNameWildcard, persistentStorageInstanceName);

            // Create the database
            List<Tuple<String, String>> scriptsAndContents = new()
            {
                Tuple.Create(createDatabaseScript, "create the ApplicationAccess database"),
                Tuple.Create(updateDatabaseScript, "update the ApplicationAccess database"),
            };
            try
            {
                scriptExecutor.ExecuteScripts(scriptsAndContents);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create distributed AccessManager database instance '{persistentStorageInstanceName}' in SQL Server.", e);
            }

            // Create and return the connection string
            SqlConnectionStringBuilder builder = new(connectionString);
            builder.InitialCatalog = persistentStorageInstanceName;
            SqlServerLoginCredentials returnLoginCredentials = new(builder.ToString());

            return returnLoginCredentials;
        }

        /// <summary>
        /// Renames a database instance.
        /// </summary>
        /// <param name="currentPersistentStorageInstanceName">The current name of the database instance.</param>
        /// <param name="newPersistentStorageInstanceName">The new name of the database instance.</param>
        public void RenamePersistentStorage(String currentPersistentStorageInstanceName, String newPersistentStorageInstanceName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes a database instance.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the database instance.</param>
        public void DeletePersistentStorage(String persistentStorageInstanceName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new distributed AccessManager configuration database instance.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the databse instance.</param>
        /// <returns>Credentials which can be used to connect to the database.</returns>
        public SqlServerLoginCredentials CreateAccessManagerConfigurationPersistentStorage(string persistentStorageInstanceName)
        {
            // Read scripts from files
            String createDatabaseScript = "";
            try
            {
                createDatabaseScript = fileShim.ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to read AccessManager configuration create script from path '{accessManagerConfigurationDatabaseCreateScriptPath}'.", e);
            }

            // Check and populate variables in scripts
            if (createDatabaseScript.Contains(applicationAccessConfigurationDatabaseNameSetvarStatement) == false)
                throw new Exception($"AccessManager configuration create script at path '{accessManagerConfigurationDatabaseCreateScriptPath}' did not contain 'Setvar' statement '{applicationAccessConfigurationDatabaseNameSetvarStatement}'.");
            if (createDatabaseScript.Contains(databaseNameWildcard) == false)
                throw new Exception($"AccessManager configuration create script at path '{accessManagerConfigurationDatabaseCreateScriptPath}' did not contain any database name wildcards.");
            createDatabaseScript = createDatabaseScript.Replace(applicationAccessConfigurationDatabaseNameSetvarStatement, "");
            createDatabaseScript = createDatabaseScript.Replace(databaseNameWildcard, persistentStorageInstanceName);

            // Create the database
            List<Tuple<String, String>> scriptsAndContents = new()
            {
                Tuple.Create(createDatabaseScript, "create the ApplicationAccess configuration database"),
            };
            try
            {
                scriptExecutor.ExecuteScripts(scriptsAndContents);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create distributed AccessManager configuration database '{persistentStorageInstanceName}' in SQL Server.", e);
            }

            // Create and return the connection string
            SqlConnectionStringBuilder builder = new(connectionString);
            builder.InitialCatalog = persistentStorageInstanceName;
            SqlServerLoginCredentials returnLoginCredentials = new(builder.ToString());

            return returnLoginCredentials;
        }
    }
}
