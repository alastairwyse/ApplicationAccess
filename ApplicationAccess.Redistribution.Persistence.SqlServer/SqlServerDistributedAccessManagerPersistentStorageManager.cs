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
using System.Text;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution;

namespace ApplicationAccess.Redistribution.Persistence.SqlServer
{
    /// <summary>
    /// Manages SQL Server database instances for a distributed AccessManager implementation.
    /// </summary>
    public class SqlServerDistributedAccessManagerPersistentStorageManager : IDistributedAccessManagerPersistentStorageManager<SqlServerLoginCredentials>
    {
        #pragma warning disable 1591

        protected const String accessManagerDatabaseCreateScriptPath = "Resources/CreateDatabase.sql";
        protected const String accessManagerDatabaseUpdateScriptPath = "Resources/ApplicationAccess/UpdateDatabase.sql";
        protected const String accessManagerConfigurationDatabaseCreateScriptPath = "Resources/ApplicationAccessConfiguration/CreateDatabase.sql";
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
        /// <summary>Whether to rename the database's logical files to match the new name when the <see cref="SqlServerDistributedAccessManagerPersistentStorageManager.RenamePersistentStorage(String, String)">RenamePersistentStorage()</see> method is called.</summary>
        protected Boolean renameLogicalFilesOnRename;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Persistence.SqlServer.SqlServerDistributedAccessManagerPersistentStorageManager class.
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to SQL Server.</param>
        /// <param name="renameLogicalFilesOnRename">Whether to rename the database's logical files to match the new name when the <see cref="SqlServerDistributedAccessManagerPersistentStorageManager.RenamePersistentStorage(String, String)">RenamePersistentStorage()</see> method is called.</param>
        /// <remarks>When <paramref name="renameLogicalFilesOnRename"/> is set true, it's assumed a only a single logical data file (named [old database name].mdf), and a single logical log file (named [old database name]_log.ldf) exist.  The feature does not support databases with multiple logical data or log files.</remarks>
        public SqlServerDistributedAccessManagerPersistentStorageManager(String connectionString, Boolean renameLogicalFilesOnRename)
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{nameof(connectionString)}' must contain a value.", nameof(connectionString));

            fileShim = new DefaultFileShim();
            scriptExecutor = new SqlServerScriptExecutor(connectionString);
            this.connectionString = connectionString;
            this.renameLogicalFilesOnRename = renameLogicalFilesOnRename;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Persistence.SqlServer.SqlServerDistributedAccessManagerPersistentStorageManager class.
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to SQL Server.</param>
        /// <param name="renameLogicalFilesOnRename">Whether to rename the database's logical files to match the new name when the <see cref="SqlServerDistributedAccessManagerPersistentStorageManager.RenamePersistentStorage(String, String)">RenamePersistentStorage()</see> method is called.</param>
        /// <param name="mockFileShim">A mock <see cref="IFileShim"/>.</param>
        /// <param name="mockScriptExecutor">A mock <see cref="ISqlServerScriptExecutor"/>.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public SqlServerDistributedAccessManagerPersistentStorageManager(String connectionString, Boolean renameLogicalFilesOnRename, IFileShim mockFileShim, ISqlServerScriptExecutor mockScriptExecutor)
            : this(connectionString, renameLogicalFilesOnRename)
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
            ThrowExceptionIfStringParameterNullOrWhiteSpace(currentPersistentStorageInstanceName, nameof(currentPersistentStorageInstanceName));
            ThrowExceptionIfStringParameterNullOrWhiteSpace(newPersistentStorageInstanceName, nameof(newPersistentStorageInstanceName));

            // As per below link need to put the database in single-user mode before renaming
            //   https://learn.microsoft.com/en-us/sql/relational-databases/databases/rename-a-database?view=sql-server-ver17#rename-a-sql-server-database-by-placing-it-in-single-user-mode
            StringBuilder renameScriptBuider = new();
            renameScriptBuider.AppendLine($"ALTER DATABASE {currentPersistentStorageInstanceName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
            renameScriptBuider.AppendLine("GO ");
            renameScriptBuider.AppendLine($"ALTER DATABASE {currentPersistentStorageInstanceName} MODIFY NAME = {newPersistentStorageInstanceName};");
            renameScriptBuider.AppendLine("GO ");
            if (renameLogicalFilesOnRename == true)
            {
                renameScriptBuider.AppendLine($"ALTER DATABASE {newPersistentStorageInstanceName} MODIFY FILE (NAME = {currentPersistentStorageInstanceName}_Log, NEWNAME = {newPersistentStorageInstanceName}_Log);");
                renameScriptBuider.AppendLine("GO ");
                renameScriptBuider.AppendLine($"ALTER DATABASE {newPersistentStorageInstanceName} MODIFY FILE (NAME = {currentPersistentStorageInstanceName}, NEWNAME = {newPersistentStorageInstanceName});");
                renameScriptBuider.AppendLine("GO ");
            }
            renameScriptBuider.AppendLine($"ALTER DATABASE {newPersistentStorageInstanceName} SET MULTI_USER;");
            renameScriptBuider.AppendLine("GO ");

            // Rename the database
            List<Tuple<String, String>> scriptsAndContents = new()
            {
                Tuple.Create(renameScriptBuider.ToString(), $"rename database '{currentPersistentStorageInstanceName}' to '{newPersistentStorageInstanceName}'")
            };
            try
            {
                scriptExecutor.ExecuteScripts(scriptsAndContents);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to rename database '{currentPersistentStorageInstanceName}' to '{newPersistentStorageInstanceName}' in SQL Server.", e);
            }
        }

        /// <summary>
        /// Deletes a database instance.
        /// </summary>
        /// <param name="persistentStorageInstanceName">The name of the database instance.</param>
        public void DeletePersistentStorage(String persistentStorageInstanceName)
        {
            ThrowExceptionIfStringParameterNullOrWhiteSpace(persistentStorageInstanceName, nameof(persistentStorageInstanceName));

            StringBuilder deleteScriptBuider = new();
            deleteScriptBuider.AppendLine($"ALTER DATABASE {persistentStorageInstanceName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
            deleteScriptBuider.AppendLine("GO ");
            deleteScriptBuider.AppendLine($"DROP DATABASE {persistentStorageInstanceName};");
            deleteScriptBuider.AppendLine("GO ");

            // Delete the database
            List<Tuple<String, String>> scriptsAndContents = new()
            {
                Tuple.Create(deleteScriptBuider.ToString(), $"delete database '{persistentStorageInstanceName}'")
            };
            try
            {
                scriptExecutor.ExecuteScripts(scriptsAndContents);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to delete database '{persistentStorageInstanceName}' in SQL Server.", e);
            }
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

        #region Private/Protected Methods

        #pragma warning disable 1591

        protected void ThrowExceptionIfStringParameterNullOrWhiteSpace(String parameterValue, String parameterName)
        {
            if (String.IsNullOrWhiteSpace(parameterValue) == true)
                throw new ArgumentException($"Parameter '{parameterName}' must contain a value.", parameterName);
        }

        #pragma warning restore 1591

        #endregion
    }
}
