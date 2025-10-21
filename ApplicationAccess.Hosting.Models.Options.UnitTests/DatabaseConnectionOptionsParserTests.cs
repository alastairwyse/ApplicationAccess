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
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Models.Options.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.DatabaseConnectionOptionsParser class.
    /// </summary>
    public class DatabaseConnectionOptionsParserTests
    {
        private DatabaseConnectionOptionsParser testDatabaseConnectionOptionsParser;

        [SetUp]
        protected void SetUp()
        {
            testDatabaseConnectionOptionsParser = new DatabaseConnectionOptionsParser();
        }

        // No tests are included for DatabaseConnectionOptions.SqlDatabaseConnection property, as it contains a field of type IConfigurationSection which can't be simply instantiated

        [Test]
        public void Parse_DatabaseConnectionOptionsParameterContainsMongoDbDatabaseConnection()
        {
            var databaseConnectionOptions = new DatabaseConnectionOptions()
            {
                MongoDbDatabaseConnection = new MongoDbDatabaseConnectionOptions()
                {
                    ConnectionString = "mongodb://127.0.0.1:27017", 
                    DatabaseName = "ApplicationAccess", 
                    UseTransactions = true
                }
            };

            DatabaseConnectionParameters result = testDatabaseConnectionOptionsParser.Parse(databaseConnectionOptions);

            Assert.AreEqual(databaseConnectionOptions.MongoDbDatabaseConnection.ConnectionString, result.MongoDbDatabaseConnectionParameters.ConnectionString);
            Assert.AreEqual(databaseConnectionOptions.MongoDbDatabaseConnection.DatabaseName, result.MongoDbDatabaseConnectionParameters.DatabaseName);
            Assert.AreEqual(databaseConnectionOptions.MongoDbDatabaseConnection.UseTransactions, result.MongoDbDatabaseConnectionParameters.UseTransactions);
            Assert.IsNull(databaseConnectionOptions.SqlDatabaseConnection);
        }

        [Test]
        public void Parse_DatabaseConnectionOptionsParameterContainsNoDatabaseConnection()
        {
            var databaseConnectionOptions = new DatabaseConnectionOptions();

            DatabaseConnectionParameters result = testDatabaseConnectionOptionsParser.Parse(databaseConnectionOptions);

            Assert.IsNull(databaseConnectionOptions.SqlDatabaseConnection);
            Assert.IsNull(databaseConnectionOptions.MongoDbDatabaseConnection);
        }
    }
}
