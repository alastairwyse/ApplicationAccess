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
using System.ComponentModel.DataAnnotations;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationMetrics.MetricLoggers;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.DatabaseConnectionOptionsValidator class.
    /// </summary>
    public class DatabaseConnectionOptionsValidatorTests
    {
        private DatabaseConnectionOptionsValidator testDatabaseConnectionOptionsValidator;

        [SetUp]
        protected void SetUp()
        {
            testDatabaseConnectionOptionsValidator = new DatabaseConnectionOptionsValidator();
        }

        [Test]
        public void Validate_BothSqlDatabaseConnectionAndMongoDbDatabaseConnectionDefined()
        {
            var testDatabaseConnectionOptions = new DatabaseConnectionOptions();
            testDatabaseConnectionOptions.SqlDatabaseConnection = new SqlDatabaseConnectionOptions();
            testDatabaseConnectionOptions.MongoDbDatabaseConnection = new MongoDbDatabaseConnectionOptions();

            var e = Assert.Throws<ValidationException>(delegate
            {
                testDatabaseConnectionOptionsValidator.Validate(testDatabaseConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating DatabaseConnection options.  Configuration for either section 'SqlDatabaseConnection' or section 'MongoDbDatabaseConnection' must be provided, but not both."));
        }

        [Test]
        public void Validate_SqlDatabaseConnectionDatabaseTypeNull()
        {
            var testDatabaseConnectionOptions = new DatabaseConnectionOptions();
            testDatabaseConnectionOptions.SqlDatabaseConnection = new SqlDatabaseConnectionOptions();

            var e = Assert.Throws<ValidationException>(delegate
            {
                testDatabaseConnectionOptionsValidator.Validate(testDatabaseConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating DatabaseConnection options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating SqlDatabaseConnection options.  Configuration for 'DatabaseType' is required."));
        }
        [Test]
        public void Validate_MongoDbDatabaseConnectionUseTransactionsNull()
        {
            var testDatabaseConnectionOptions = new DatabaseConnectionOptions();
            testDatabaseConnectionOptions.MongoDbDatabaseConnection = new MongoDbDatabaseConnectionOptions();
            testDatabaseConnectionOptions.MongoDbDatabaseConnection.ConnectionString = "mongodb://127.0.0.1:27017";
            testDatabaseConnectionOptions.MongoDbDatabaseConnection.DatabaseName = "ApplicationAccess";

            var e = Assert.Throws<ValidationException>(delegate
            {
                testDatabaseConnectionOptionsValidator.Validate(testDatabaseConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating DatabaseConnection options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating MongoDbDatabaseConnection options.  Configuration for 'UseTransactions' is required."));
        }
    }
}
