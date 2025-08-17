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
using ApplicationAccess.Persistence.Sql.SqlServer;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Redistribution.Persistence.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Persistence.SqlServer.SqlServerCredentialsAppSettingsConfigurer class.
    /// </summary>
    public class SqlServerCredentialsAppSettingsConfigurerUnitTests
    {
        protected SqlServerLoginCredentials testSqlServerLoginCredentials;
        protected SqlServerCredentialsAppSettingsConfigurer testSqlServerCredentialsAppSettingsConfigurer;

        [SetUp]
        protected void SetUp()
        {
            testSqlServerLoginCredentials = new SqlServerLoginCredentials("Data Source=127.0.0.1;Initial Catalog=applicationaccess_user_n2147483648;User ID=sa;Password=password;Encrypt=False;Authentication=SqlPassword");
            testSqlServerCredentialsAppSettingsConfigurer = new SqlServerCredentialsAppSettingsConfigurer();
        }

        [Test]
        public void ConfigureAppsettingsJsonWithPersistentStorageCredentials_AppsettingsJsonDoesntContainConnectionParametersProperty()
        {
            JObject testAppsettingsJson = JObject.Parse
            (
                @"
                { 
                    ""DatabaseConnection"": {
                        ""SqlDatabaseConnection"": {
                            ""DatabaseType"": ""SqlServer""
                        }
                    }

                }"
            );

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerCredentialsAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(testSqlServerLoginCredentials, testAppsettingsJson);
            });

            Assert.That(e.Message, Does.StartWith($"JSON path 'DatabaseConnection.SqlDatabaseConnection.ConnectionParameters' was not found in the specified 'appsettings.json' configuration."));
        }

        [Test]
        public void ConfigureAppsettingsJsonWithPersistentStorageCredentials()
        {
            JObject testAppsettingsJson = JObject.Parse
            (
                @"
                { 
                    ""DatabaseConnection"": {
                        ""SqlDatabaseConnection"": {
                            ""DatabaseType"": ""SqlServer"", 
                            ""ConnectionParameters"": {
                                ""RetryCount"": 5,
                                ""RetryInterval"": 10,
                                ""OperationTimeout"": 0
                            }
                        }
                    }
                }"
            );
            JObject expectedAppsettingsJson = JObject.Parse
            (
                @"
                { 
                    ""DatabaseConnection"": {
                        ""SqlDatabaseConnection"": {
                            ""DatabaseType"": ""SqlServer"", 
                            ""ConnectionParameters"": {
                                ""RetryCount"": 5,
                                ""RetryInterval"": 10,
                                ""OperationTimeout"": 0, 
                                ""ConnectionString"": ""Data Source=127.0.0.1;Initial Catalog=applicationaccess_user_n2147483648;User ID=sa;Password=password;Encrypt=False;Authentication=SqlPassword""
                            }
                        }
                    }
                }"
            );

            testSqlServerCredentialsAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(testSqlServerLoginCredentials, testAppsettingsJson);

            Assert.AreEqual(expectedAppsettingsJson, testAppsettingsJson);
        }
    }
}
