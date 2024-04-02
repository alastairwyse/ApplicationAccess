/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Hosting.Models.Options.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.SqlDatabaseConnectionParametersParser class.
    /// </summary>
    public class SqlDatabaseConnectionParametersParserTests
    {
        protected const String connectionStringConfigurationKey = "ConnectionString";
        protected const String userIdConfigurationKey = "UserId";
        protected const String usernameConfigurationKey = "Username";
        protected const String passwordConfigurationKey = "Password";
        protected const String dataSourceConfigurationKey = "DataSource";
        protected const String hostConfigurationKey = "Host";
        protected const String initialCatalogConfigurationKey = "InitialCatalog";
        protected const String databaseConfigurationKey = "Database";
        protected const String retryCountConfigurationKey = "RetryCount";
        protected const String retryIntervalConfigurationKey = "RetryInterval";
        protected const String operationTimeoutConfigurationKey = "OperationTimeout";
        protected const String commandTimeoutConfigurationKey = "CommandTimeout";

        protected const String testSqlServerConnectionString = "Server=127.0.0.1;Database=ApplicationAccess;User Id=user;Password=pass123;";
        protected const String testPostgreSqlConnectionString = "User ID=user;Password=pass123;Host=127.0.0.1;Database=ApplicationAccess;";
        protected const String testParentConfigurationName = "ConnectionParameters";
        protected SqlDatabaseConnectionParametersParser testSqlDatabaseConnectionParametersParser;

        [SetUp]
        protected void SetUp()
        {
            testSqlDatabaseConnectionParametersParser = new SqlDatabaseConnectionParametersParser();
        }

        [Test]
        public void Parse_SqlServerRetryCountDoesntExist()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(connectionStringConfigurationKey, testSqlServerConnectionString)
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.SqlServer, config, testParentConfigurationName);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to read configuration item with key '{retryCountConfigurationKey}' from the '{testParentConfigurationName}' configuration."));
        }

        [Test]
        public void Parse_SqlServerRetryCountNotNumber()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(connectionStringConfigurationKey, testSqlServerConnectionString),
                new(retryCountConfigurationKey, "abc"),
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.SqlServer, config, testParentConfigurationName);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to read configuration item with key '{retryCountConfigurationKey}' and value 'abc' from the '{testParentConfigurationName}' configuration as an integer."));
        }

        [Test]
        public void Parse_SqlServerUserIdDoesntExist()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(retryCountConfigurationKey, "5"),
                new(retryIntervalConfigurationKey, "10"),
                new(operationTimeoutConfigurationKey, "30"),
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.SqlServer, config, testParentConfigurationName);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to read configuration item with key '{userIdConfigurationKey}' from the '{testParentConfigurationName}' configuration."));
        }

        [Test]
        public void Parse_SqlServer()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(connectionStringConfigurationKey, testSqlServerConnectionString), 
                new(retryCountConfigurationKey, "5"),
                new(retryIntervalConfigurationKey, "10"),
                new(operationTimeoutConfigurationKey, "30"),
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            SqlDatabaseConnectionParametersBase result = testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.SqlServer, config, testParentConfigurationName);

            Assert.IsAssignableFrom(typeof(SqlServerConnectionParameters), result);
            var typedResult = (SqlServerConnectionParameters)result;
            Assert.AreEqual(testSqlServerConnectionString, typedResult.ConnectionString);
            Assert.AreEqual(5, typedResult.RetryCount);
            Assert.AreEqual(10, typedResult.RetryInterval);
            Assert.AreEqual(30, typedResult.OperationTimeout);


            configValues = new List<KeyValuePair<String, String>>()
            {
                new(userIdConfigurationKey, "User1"),
                new(passwordConfigurationKey, "Password123"),
                new(dataSourceConfigurationKey, "127.0.0.1"),
                new(initialCatalogConfigurationKey, "ApplicationAccess"),
                new(retryCountConfigurationKey, "5"),
                new(retryIntervalConfigurationKey, "10"),
                new(operationTimeoutConfigurationKey, "30"),
            };
            config = CreateConfigurationFromKeyValuePairs(configValues);

            result = testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.SqlServer, config, testParentConfigurationName);

            Assert.IsAssignableFrom(typeof(SqlServerConnectionParameters), result);
            typedResult = (SqlServerConnectionParameters)result;
            Assert.IsNull(typedResult.ConnectionString);
            Assert.AreEqual("User1", typedResult.UserId);
            Assert.AreEqual("Password123", typedResult.Password);
            Assert.AreEqual("127.0.0.1", typedResult.DataSource);
            Assert.AreEqual("ApplicationAccess", typedResult.InitialCatalog);
            Assert.AreEqual(5, typedResult.RetryCount);
            Assert.AreEqual(10, typedResult.RetryInterval);
            Assert.AreEqual(30, typedResult.OperationTimeout);
        }

        [Test]
        public void Parse_PostgreSqlCommandTimeoutDoesntExist()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(connectionStringConfigurationKey, testPostgreSqlConnectionString)
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.PostgreSQL, config, testParentConfigurationName);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to read configuration item with key '{commandTimeoutConfigurationKey}' from the '{testParentConfigurationName}' configuration."));
        }

        [Test]
        public void Parse_PostgreSqlCommandTimeoutNotNumber()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(connectionStringConfigurationKey, testPostgreSqlConnectionString),
                new(commandTimeoutConfigurationKey, "abc"),
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.PostgreSQL, config, testParentConfigurationName);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to read configuration item with key '{commandTimeoutConfigurationKey}' and value 'abc' from the '{testParentConfigurationName}' configuration as an integer."));
        }

        [Test]
        public void Parse_PostgreSqlUserNameDoesntExist()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(commandTimeoutConfigurationKey, "60")
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.PostgreSQL, config, testParentConfigurationName);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to read configuration item with key '{usernameConfigurationKey}' from the '{testParentConfigurationName}' configuration."));
        }

        [Test]
        public void Parse_PostgreSql()
        {
            var configValues = new List<KeyValuePair<String, String>>()
            {
                new(connectionStringConfigurationKey, testPostgreSqlConnectionString),
                new(commandTimeoutConfigurationKey, "60"),
            };
            IConfiguration config = CreateConfigurationFromKeyValuePairs(configValues);

            SqlDatabaseConnectionParametersBase result = testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.PostgreSQL, config, testParentConfigurationName);

            Assert.IsAssignableFrom(typeof(PostgreSqlConnectionParameters), result);
            var typedResult = (PostgreSqlConnectionParameters)result;
            Assert.AreEqual(testPostgreSqlConnectionString, typedResult.ConnectionString);
            Assert.AreEqual(60, typedResult.CommandTimeout);


            configValues = new List<KeyValuePair<String, String>>()
            {
                new(usernameConfigurationKey, "User1"),
                new(passwordConfigurationKey, "Password123"),
                new(hostConfigurationKey, "127.0.0.1"),
                new(databaseConfigurationKey, "ApplicationAccess"),
                new(commandTimeoutConfigurationKey, "60"),
            };
            config = CreateConfigurationFromKeyValuePairs(configValues);

            result = testSqlDatabaseConnectionParametersParser.Parse(DatabaseType.PostgreSQL, config, testParentConfigurationName);

            Assert.IsAssignableFrom(typeof(PostgreSqlConnectionParameters), result);
            typedResult = (PostgreSqlConnectionParameters)result;
            Assert.IsNull(typedResult.ConnectionString);
            Assert.AreEqual("User1", typedResult.UserName);
            Assert.AreEqual("Password123", typedResult.Password);
            Assert.AreEqual("127.0.0.1", typedResult.Host);
            Assert.AreEqual("ApplicationAccess", typedResult.Database);
            Assert.AreEqual(60, typedResult.CommandTimeout);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates an <see cref="IConfiguration"/> instance from a collection of name/value <see cref="KeyValuePair">KeyValuePairs</see>.
        /// </summary>
        /// <param name="configurationData">The <see cref="KeyValuePair">KeyValuePairs</see> to create the configuration from.</param>
        /// <returns>The configuration.</returns>
        protected IConfiguration CreateConfigurationFromKeyValuePairs(IEnumerable<KeyValuePair<String, String>> configurationData)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(configurationData);
            
            return configBuilder.Build();
        }

        #endregion
    }
}