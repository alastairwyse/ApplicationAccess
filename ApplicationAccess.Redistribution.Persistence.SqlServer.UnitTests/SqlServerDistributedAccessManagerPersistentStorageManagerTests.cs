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
using System.Text;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Utilities;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Redistribution.Persistence.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Persistence.SqlServer.SqlServerDistributedAccessManagerPersistentStorageManager class.
    /// </summary>
    public class SqlServerDistributedAccessManagerPersistentStorageManagerTests
    {
        protected const String accessManagerDatabaseCreateScriptPath = "./Resources/CreateDatabase.sql";
        protected const String accessManagerDatabaseUpdateScriptPath = "./Resources/ApplicationAccess/UpdateDatabase.sql";
        protected const String accessManagerConfigurationDatabaseCreateScriptPath = "./Resources/ApplicationAccessConfiguration/CreateDatabase.sql";

        protected TestUtilities testUtilities;
        protected String testConnectionString;
        protected IFileShim mockFileShim;
        protected ISqlServerScriptExecutor mockSqlServerScriptExecutor;
        protected SqlServerDistributedAccessManagerPersistentStorageManager testSqlServerDistributedAccessManagerPersistentStorageManager;

        [SetUp]
        protected void SetUp()
        {
            testConnectionString = "Server=127.0.0.1;User Id=sa;Password=password;Encrypt=false;Authentication=SqlPassword";
            mockFileShim = Substitute.For<IFileShim>();
            mockSqlServerScriptExecutor = Substitute.For<ISqlServerScriptExecutor>();
            testSqlServerDistributedAccessManagerPersistentStorageManager = new SqlServerDistributedAccessManagerPersistentStorageManager(testConnectionString, mockFileShim, mockSqlServerScriptExecutor);
        }

        [Test]
        public void Constructor_ConnectionStringParameterNull()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager = new SqlServerDistributedAccessManagerPersistentStorageManager(null, mockFileShim, mockSqlServerScriptExecutor);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void Constructor_ConnectionStringParameterWhiteSpace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager = new SqlServerDistributedAccessManagerPersistentStorageManager(" ", mockFileShim, mockSqlServerScriptExecutor);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void CreateAccessManagerPersistentStorage_ExceptionReadingCreateDatabaseScript()
        {
            var mockException = new Exception("Mock exception");
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            mockFileShim.When((fileShim) => fileShim.ReadAllText(accessManagerDatabaseCreateScriptPath)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseCreateScriptPath);
            Assert.That(e.Message, Does.StartWith($"Failed to read AccessManager create script from path '{accessManagerDatabaseCreateScriptPath}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CreateAccessManagerPersistentStorage_ExceptionReadingUpdateDatabaseScript()
        {
            var mockException = new Exception("Mock exception");
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            mockFileShim.When((fileShim) => fileShim.ReadAllText(accessManagerDatabaseUpdateScriptPath)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            Assert.That(e.Message, Does.StartWith($"Failed to read AccessManager update script from path '{accessManagerDatabaseUpdateScriptPath}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CreateAccessManagerPersistentStorage_CreateDatabaseScriptDoesNotContainSetvarStatement()
        {
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            String testCreateDatabaseScript = "No setvar";
            mockFileShim.ReadAllText(accessManagerDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            Assert.That(e.Message, Does.StartWith($"AccessManager create script at path '{accessManagerDatabaseCreateScriptPath}' did not contain 'Setvar' statement ':Setvar DatabaseName ApplicationAccess'."));
        }

        [Test]
        public void CreateAccessManagerPersistentStorage_CreateDatabaseScriptDoesNotContainDatabaseNameWildcard()
        {
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            CREATE DATABASE ApplicationAccess;
            GO";
            mockFileShim.ReadAllText(accessManagerDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            Assert.That(e.Message, Does.StartWith($"AccessManager create script at path '{accessManagerDatabaseCreateScriptPath}' did not contain any database name wildcards."));
        }

        [Test]
        public void CreateAccessManagerPersistentStorage_UpdateDatabaseScriptDoesNotContainSetvarStatement()
        {
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            CREATE DATABASE $(DatabaseName);
            GO";
            String testUpdateDatabaseScript = "No setvar";
            mockFileShim.ReadAllText(accessManagerDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);
            mockFileShim.ReadAllText(accessManagerDatabaseUpdateScriptPath).Returns(testUpdateDatabaseScript);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            Assert.That(e.Message, Does.StartWith($"AccessManager update script at path '{accessManagerDatabaseUpdateScriptPath}' did not contain 'Setvar' statement ':Setvar DatabaseName ApplicationAccess'."));
        }

        [Test]
        public void CreateAccessManagerPersistentStorage_UpdateDatabaseScriptDoesNotContainDatabaseNameWildcard()
        {
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            CREATE DATABASE $(DatabaseName);
            GO";
            String testUpdateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            USE ApplicationAccess;
            GO ";
            mockFileShim.ReadAllText(accessManagerDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);
            mockFileShim.ReadAllText(accessManagerDatabaseUpdateScriptPath).Returns(testUpdateDatabaseScript);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            Assert.That(e.Message, Does.StartWith($"AccessManager update script at path '{accessManagerDatabaseUpdateScriptPath}' did not contain any database name wildcards."));
        }

        [Test]
        public void CreateAccessManagerPersistentStorage_ExceptionExecutingCreateDatabaseScript()
        {
            var mockException = new Exception("Mock exception");
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            CREATE DATABASE $(DatabaseName);
            GO";
            String testUpdateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            USE $(DatabaseName);
            GO ";
            mockFileShim.ReadAllText(accessManagerDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);
            mockFileShim.ReadAllText(accessManagerDatabaseUpdateScriptPath).Returns(testUpdateDatabaseScript);
            mockSqlServerScriptExecutor.When((scriptExecutor) => scriptExecutor.ExecuteScripts(Arg.Any<List<Tuple<String, String>>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.That(e.Message, Does.StartWith($"Failed to create distributed AccessManager database instance 'applicationaccess_user_n2147483648' in SQL Server."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CreateAccessManagerPersistentStorage()
        {
            String testPersistentStorageInstanceName = "applicationaccess_user_n2147483648";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            CREATE DATABASE $(DatabaseName);
            GO";
            String testUpdateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccess

            USE $(DatabaseName);
            GO ";
            mockFileShim.ReadAllText(accessManagerDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);
            mockFileShim.ReadAllText(accessManagerDatabaseUpdateScriptPath).Returns(testUpdateDatabaseScript);

            SqlServerLoginCredentials result = testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerPersistentStorage(testPersistentStorageInstanceName);

            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            mockFileShim.Received(1).ReadAllText(accessManagerDatabaseUpdateScriptPath);
            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.AreEqual("Data Source=127.0.0.1;Initial Catalog=applicationaccess_user_n2147483648;User ID=sa;Password=password;Encrypt=False;Authentication=SqlPassword", result.ConnectionString);
        }

        [Test]
        public void RenamePersistentStorage_CurrentPersistentStorageInstanceNameParameterWhiteSpace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.RenamePersistentStorage(" ", "user_n2147483648");
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'currentPersistentStorageInstanceName' must contain a value."));
            Assert.AreEqual("currentPersistentStorageInstanceName", e.ParamName);
        }

        [Test]
        public void RenamePersistentStorage_NewPersistentStorageInstanceNameParameterWhiteSpace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.RenamePersistentStorage("merge_temp", " ");
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'newPersistentStorageInstanceName' must contain a value."));
            Assert.AreEqual("newPersistentStorageInstanceName", e.ParamName);
        }

        [Test]
        public void RenamePersistentStorage_ExceptionExecutingRenameDatabaseScript()
        {
            var mockException = new Exception("Mock exception");
            String testCurrentPersistentStorageInstanceName = "merge_temp";
            String testNewPersistentStorageInstanceName = "user_n2147483648";
            mockSqlServerScriptExecutor.When((scriptExecutor) => scriptExecutor.ExecuteScripts(Arg.Any<List<Tuple<String, String>>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.RenamePersistentStorage(testCurrentPersistentStorageInstanceName, testNewPersistentStorageInstanceName);
            });

            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.That(e.Message, Does.StartWith($"Failed to rename database 'merge_temp' to 'user_n2147483648' in SQL Server."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void RenamePersistentStorage()
        {
            String testCurrentPersistentStorageInstanceName = "merge_temp";
            String testNewPersistentStorageInstanceName = "user_n2147483648";
            StringBuilder testRenameScriptBuider = new();
            testRenameScriptBuider.AppendLine("ALTER DATABASE merge_temp SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
            testRenameScriptBuider.AppendLine("GO ");
            testRenameScriptBuider.AppendLine("ALTER DATABASE merge_temp MODIFY NAME = user_n2147483648;");
            testRenameScriptBuider.AppendLine("GO ");
            testRenameScriptBuider.AppendLine("ALTER DATABASE Test2 SET user_n2147483648;");
            testRenameScriptBuider.AppendLine("GO ");
            List<Tuple<String, String>> capturedScriptsAndContents = null;
            mockSqlServerScriptExecutor.ExecuteScripts(Arg.Do<List<Tuple<String, String>>>(argumentValue => capturedScriptsAndContents = argumentValue));

            testSqlServerDistributedAccessManagerPersistentStorageManager.RenamePersistentStorage(testCurrentPersistentStorageInstanceName, testNewPersistentStorageInstanceName);

            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.AreEqual(testRenameScriptBuider.ToString(), capturedScriptsAndContents[0].Item1);
        }

        [Test]
        public void DeletePersistentStorage_PersistentStorageInstanceNameParameterWhiteSpace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.DeletePersistentStorage(" ");
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'persistentStorageInstanceName' must contain a value."));
            Assert.AreEqual("persistentStorageInstanceName", e.ParamName);
        }

        [Test]
        public void DeletePersistentStorage_ExceptionExecutingDeleteDatabaseScript()
        {
            var mockException = new Exception("Mock exception");
            String testPersistentStorageInstanceName = "user_0";
            mockSqlServerScriptExecutor.When((scriptExecutor) => scriptExecutor.ExecuteScripts(Arg.Any<List<Tuple<String, String>>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.DeletePersistentStorage(testPersistentStorageInstanceName);
            });

            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.That(e.Message, Does.StartWith($"Failed to delete database 'user_0' in SQL Server."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void DeletePersistentStorage()
        {
            String testPersistentStorageInstanceName = "user_0";
            StringBuilder testRenameScriptBuider = new();
            testRenameScriptBuider.AppendLine("DROP DATABASE user_0;");
            testRenameScriptBuider.AppendLine("GO ");
            List<Tuple<String, String>> capturedScriptsAndContents = null;
            mockSqlServerScriptExecutor.ExecuteScripts(Arg.Do<List<Tuple<String, String>>>(argumentValue => capturedScriptsAndContents = argumentValue));

            testSqlServerDistributedAccessManagerPersistentStorageManager.DeletePersistentStorage(testPersistentStorageInstanceName);

            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.AreEqual(testRenameScriptBuider.ToString(), capturedScriptsAndContents[0].Item1);
        }

        [Test]
        public void CreateAccessManagerConfigurationPersistentStorage_ExceptionReadingCreateDatabaseScript()
        {
            var mockException = new Exception("Mock exception");
            String testPersistentStorageInstanceName = "ApplicationAccessConfiguration";
            mockFileShim.When((fileShim) => fileShim.ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerConfigurationPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath);
            Assert.That(e.Message, Does.StartWith($"Failed to read AccessManager configuration create script from path '{accessManagerConfigurationDatabaseCreateScriptPath}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CreateAccessManagerConfigurationPersistentStorage_CreateDatabaseScriptDoesNotContainSetvarStatement()
        {
            String testPersistentStorageInstanceName = "ApplicationAccessConfiguration";
            String testCreateDatabaseScript = "No setvar";
            mockFileShim.ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerConfigurationPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath);
            Assert.That(e.Message, Does.StartWith($"AccessManager configuration create script at path '{accessManagerConfigurationDatabaseCreateScriptPath}' did not contain 'Setvar' statement ':Setvar DatabaseName ApplicationAccessConfiguration'."));
        }

        [Test]
        public void CreateAccessManagerConfigurationPersistentStorage_CreateDatabaseScriptDoesNotContainDatabaseNameWildcard()
        {
            String testPersistentStorageInstanceName = "ApplicationAccessConfiguration";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccessConfiguration

            CREATE DATABASE ApplicationAccessConfiguration;
            GO";
            mockFileShim.ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerConfigurationPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath);
            Assert.That(e.Message, Does.StartWith($"AccessManager configuration create script at path '{accessManagerConfigurationDatabaseCreateScriptPath}' did not contain any database name wildcards."));
        }

        [Test]
        public void CreateAccessManagerConfigurationPersistentStorage_ExceptionExecutingCreateDatabaseScript()
        {
            var mockException = new Exception("Mock exception");
            String testPersistentStorageInstanceName = "ApplicationAccessConfiguration";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccessConfiguration

            CREATE DATABASE $(DatabaseName);
            GO";
            mockFileShim.ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);
            mockSqlServerScriptExecutor.When((scriptExecutor) => scriptExecutor.ExecuteScripts(Arg.Any<List<Tuple<String, String>>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerConfigurationPersistentStorage(testPersistentStorageInstanceName);
            });

            mockFileShim.Received(1).ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath);
            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.That(e.Message, Does.StartWith($"Failed to create distributed AccessManager configuration database 'ApplicationAccessConfiguration' in SQL Server."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CreateAccessManagerConfigurationPersistentStorage()
        {
            String testPersistentStorageInstanceName = "ApplicationAccessConfiguration";
            String testCreateDatabaseScript = @"
            :Setvar DatabaseName ApplicationAccessConfiguration

            CREATE DATABASE $(DatabaseName);
            GO";
            mockFileShim.ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath).Returns(testCreateDatabaseScript);

            SqlServerLoginCredentials result = testSqlServerDistributedAccessManagerPersistentStorageManager.CreateAccessManagerConfigurationPersistentStorage(testPersistentStorageInstanceName);

            mockFileShim.Received(1).ReadAllText(accessManagerConfigurationDatabaseCreateScriptPath);
            mockSqlServerScriptExecutor.Received(1).ExecuteScripts(Arg.Any<List<Tuple<String, String>>>());
            Assert.AreEqual("Data Source=127.0.0.1;Initial Catalog=ApplicationAccessConfiguration;User ID=sa;Password=password;Encrypt=False;Authentication=SqlPassword", result.ConnectionString);
        }
    }
}
