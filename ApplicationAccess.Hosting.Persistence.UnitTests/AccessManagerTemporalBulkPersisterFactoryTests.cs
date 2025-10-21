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
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Persistence;
using ApplicationAccess.Hosting.Persistence.Sql;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.File;
using ApplicationAccess.Persistence.MongoDb;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Utilities;
using ApplicationAccess.UnitTests;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Persistence.AccessManagerTemporalBulkPersisterFactory class.
    /// </summary>
    public class AccessManagerTemporalBulkPersisterFactoryTests
    {
        protected IApplicationLogger testLogger;
        protected IMetricLogger testMetricLogger;
        private AccessManagerTemporalBulkPersisterFactory<String, String, String, String> testAccessManagerTemporalBulkPersisterFactory;

        [SetUp]
        protected void SetUp()
        {
            testLogger = new NullLogger();
            testMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerTemporalBulkPersisterFactory = new
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger,
                testMetricLogger
            );
        }

        [Test]
        public void GetPersister_NullAccessManagerTemporalBulkPersisterReturned()
        {
            DatabaseConnectionParameters databaseConnectionParameters = new();

            IAccessManagerTemporalEventBulkPersister<String, String, String, String> result = testAccessManagerTemporalBulkPersisterFactory.GetPersister(databaseConnectionParameters, null);

            Assert.IsAssignableFrom<NullAccessManagerTemporalBulkPersister<String, String, String, String>>(result);
        }

        [Test]
        public void GetPersister_SqlServerAccessManagerTemporalBulkPersisterReturned()
        {
            String testUserId = "sa";
            String testPassword = "password";
            String testDataSource = "127.0.0.1";
            String testInitialCatalogue = "ApplicationAccess";
            Int32 testRetryCount = 5;
            Int32 testRetryInterval = 10;
            Int32 testOperationTimeout = 0;
            var sqlConnectionParameters = new SqlServerConnectionParameters(testUserId, testPassword, testDataSource, testInitialCatalogue, testRetryCount, testRetryInterval, testOperationTimeout);
            var testPersisterBackupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestBackupEvents.json");
            DatabaseConnectionParameters databaseConnectionParameters = new(sqlConnectionParameters);

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> result = testAccessManagerTemporalBulkPersisterFactory.GetPersister(databaseConnectionParameters, testPersisterBackupFilePath))
            {

                Assert.IsAssignableFrom<AccessManagerRedundantTemporalBulkPersister<String, String, String, String>>(result);
                NonPublicFieldAssert.IsOfType<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryReader" }, result);
                NonPublicFieldAssert.IsOfType<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryPersister" }, result);
            }
        }

        [Test]
        public void GetPersister_MongoDbAccessManagerTemporalBulkPersisterWithoutMetricsWithoutBackupReturned()
        {
            String connectionString = "mongodb://127.0.0.1:27017";
            String databaseName = "ApplicationAccess";
            Boolean userTransactions = true;
            var mongoDbConnectionParameters = new MongoDbDatabaseConnectionParameters(connectionString, databaseName, userTransactions);
            DatabaseConnectionParameters databaseConnectionParameters = new(mongoDbConnectionParameters);
            testAccessManagerTemporalBulkPersisterFactory = new
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> result = testAccessManagerTemporalBulkPersisterFactory.GetPersister(databaseConnectionParameters, null))
            {

                Assert.IsAssignableFrom<MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>>(result);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, result);
            }
        }

        [Test]
        public void GetPersister_MongoDbAccessManagerTemporalBulkPersisterWithMetricsWithoutBackupReturned()
        {
            String connectionString = "mongodb://127.0.0.1:27017";
            String databaseName = "ApplicationAccess";
            Boolean userTransactions = true;
            var mongoDbConnectionParameters = new MongoDbDatabaseConnectionParameters(connectionString, databaseName, userTransactions);
            DatabaseConnectionParameters databaseConnectionParameters = new(mongoDbConnectionParameters);

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> result = testAccessManagerTemporalBulkPersisterFactory.GetPersister(databaseConnectionParameters, null))
            {

                Assert.IsAssignableFrom<MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>>(result);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, result, true);
            }
        }

        [Test]
        public void GetPersister_MongoDbAccessManagerTemporalBulkPersisterWithoutMetricsWithBackupReturned()
        {
            String connectionString = "mongodb://127.0.0.1:27017";
            String databaseName = "ApplicationAccess";
            Boolean userTransactions = true;
            var mongoDbConnectionParameters = new MongoDbDatabaseConnectionParameters(connectionString, databaseName, userTransactions);
            var testPersisterBackupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestBackupEvents.json");
            DatabaseConnectionParameters databaseConnectionParameters = new(mongoDbConnectionParameters);
            testAccessManagerTemporalBulkPersisterFactory = new
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> result = testAccessManagerTemporalBulkPersisterFactory.GetPersister(databaseConnectionParameters, testPersisterBackupFilePath))
            {

                Assert.IsAssignableFrom<AccessManagerRedundantTemporalBulkPersister<String, String, String, String>>(result);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, result);
                NonPublicFieldAssert.IsOfType<MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryReader" }, result);
                NonPublicFieldAssert.IsOfType<MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryPersister" }, result);
                NonPublicFieldAssert.IsOfType<FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>>(new List<String>() { "backupPersister" }, result);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "primaryPersister", "metricLogger" }, result);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "backupPersister", "metricLogger" }, result);
            }
        }

        [Test]
        public void GetPersister_MongoDbAccessManagerTemporalBulkPersisterWithMetricsWithBackupReturned()
        {
            String connectionString = "mongodb://127.0.0.1:27017";
            String databaseName = "ApplicationAccess";
            Boolean userTransactions = true;
            var mongoDbConnectionParameters = new MongoDbDatabaseConnectionParameters(connectionString, databaseName, userTransactions);
            var testPersisterBackupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestBackupEvents.json");
            DatabaseConnectionParameters databaseConnectionParameters = new(mongoDbConnectionParameters);

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> result = testAccessManagerTemporalBulkPersisterFactory.GetPersister(databaseConnectionParameters, testPersisterBackupFilePath))
            {

                Assert.IsAssignableFrom<AccessManagerRedundantTemporalBulkPersister<String, String, String, String>>(result);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, result, true);
                NonPublicFieldAssert.IsOfType<MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryReader" }, result);
                NonPublicFieldAssert.IsOfType<MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryPersister" }, result);
                NonPublicFieldAssert.IsOfType<FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>>(new List<String>() { "backupPersister" }, result);
                NonPublicFieldAssert.HasValue(new List<String>() { "primaryPersister", "metricLogger" }, testMetricLogger, result, true);
                NonPublicFieldAssert.HasValue(new List<String>() { "backupPersister", "metricLogger" }, testMetricLogger, result, true);
            }
        }
    }
}
