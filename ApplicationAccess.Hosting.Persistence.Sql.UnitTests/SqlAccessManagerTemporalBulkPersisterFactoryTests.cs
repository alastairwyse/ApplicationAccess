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
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Persistence.Sql;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Sql.PostgreSql;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NSubstitute;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using System.IO;
using ApplicationAccess.Persistence.File;

namespace ApplicationAccess.Hosting.Persistence.Sql.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Persistence.Sql.SqlAccessManagerTemporalBulkPersisterFactory class.
    /// </summary>
    public class SqlAccessManagerTemporalBulkPersisterFactoryTests
    {
        protected IApplicationLogger testLogger;
        protected IMetricLogger testMetricLogger;
        protected SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String> testSqlAccessManagerTemporalBulkPersisterFactory;

        [SetUp]
        protected void SetUp()
        {
            testLogger = new NullLogger();
            testMetricLogger = Substitute.For<IMetricLogger>();
        }

        [Test]
        public void GetPersister_SqlServerWithoutMetricsWithoutBackup()
        {
            String testUserId = "sa";
            String testPassword = "password";
            String testDataSource = "127.0.0.1";
            String testInitialCatalogue = "ApplicationAccess";
            Int32 testRetryCount = 5;
            Int32 testRetryInterval = 10;
            Int32 testOperationTimeout = 0;
            var connectionParameters = new SqlServerConnectionParameters(testUserId, testPassword, testDataSource, testInitialCatalogue, testRetryCount, testRetryInterval, testOperationTimeout);
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, null))
            {

                Assert.IsAssignableFrom<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, testPersister);
            }
        }

        [Test]
        public void GetPersister_SqlServerWithMetricsWithoutBackup()
        {
            String testUserId = "sa";
            String testPassword = "password";
            String testDataSource = "127.0.0.1";
            String testInitialCatalogue = "ApplicationAccess";
            Int32 testRetryCount = 5;
            Int32 testRetryInterval = 10;
            Int32 testOperationTimeout = 0;
            var connectionParameters = new SqlServerConnectionParameters(testUserId, testPassword, testDataSource, testInitialCatalogue, testRetryCount, testRetryInterval, testOperationTimeout);
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger,
                testMetricLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, null))
            {

                Assert.IsAssignableFrom<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, testPersister, true);
            }
        }

        [Test]
        public void GetPersister_PostgreSqlWithoutMetricsWithoutBackup()
        {
            String testUserName = "user";
            String testPassword = "password";
            String testHost = "127.0.0.1";
            String testDatabase = "ApplicationAccess";
            Int32 testCommandTimeout = 0;
            var connectionParameters = new PostgreSqlConnectionParameters(testUserName, testPassword, testHost, testDatabase, testCommandTimeout);
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, null))
            {

                Assert.IsAssignableFrom<PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, testPersister);
            }
        }

        [Test]
        public void GetPersister_PostgreSqlWithMetricsWithoutBackup()
        {
            String testUserName = "user";
            String testPassword = "password";
            String testHost = "127.0.0.1";
            String testDatabase = "ApplicationAccess";
            Int32 testCommandTimeout = 0;
            var connectionParameters = new PostgreSqlConnectionParameters(testUserName, testPassword, testHost, testDatabase, testCommandTimeout);
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger,
                testMetricLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, null))
            {

                Assert.IsAssignableFrom<PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, testPersister, true);
            }
        }

        [Test]
        public void GetPersister_SqlServerWithoutMetricsWithBackup()
        {
            String testUserId = "sa";
            String testPassword = "password";
            String testDataSource = "127.0.0.1";
            String testInitialCatalogue = "ApplicationAccess";
            Int32 testRetryCount = 5;
            Int32 testRetryInterval = 10;
            Int32 testOperationTimeout = 0;
            var connectionParameters = new SqlServerConnectionParameters(testUserId, testPassword, testDataSource, testInitialCatalogue, testRetryCount, testRetryInterval, testOperationTimeout);
            var testPersisterBackupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestBackupEvents.json");
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, testPersisterBackupFilePath))
            {

                Assert.IsAssignableFrom<AccessManagerRedundantTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, testPersister);
                NonPublicFieldAssert.IsOfType<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryReader" }, testPersister);
                NonPublicFieldAssert.IsOfType<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryPersister" }, testPersister);
                NonPublicFieldAssert.IsOfType<FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>>(new List<String>() { "backupPersister" }, testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "primaryPersister", "metricLogger" }, testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "backupPersister", "metricLogger" }, testPersister);
            }
        }

        [Test]
        public void GetPersister_SqlServerWithMetricsWithBackup()
        {
            String testUserId = "sa";
            String testPassword = "password";
            String testDataSource = "127.0.0.1";
            String testInitialCatalogue = "ApplicationAccess";
            Int32 testRetryCount = 5;
            Int32 testRetryInterval = 10;
            Int32 testOperationTimeout = 0;
            var connectionParameters = new SqlServerConnectionParameters(testUserId, testPassword, testDataSource, testInitialCatalogue, testRetryCount, testRetryInterval, testOperationTimeout);
            var testPersisterBackupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestBackupEvents.json");
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger,
                testMetricLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, testPersisterBackupFilePath))
            {

                Assert.IsAssignableFrom<AccessManagerRedundantTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, testPersister, true);
                NonPublicFieldAssert.IsOfType<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryReader" }, testPersister);
                NonPublicFieldAssert.IsOfType<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryPersister" }, testPersister);
                NonPublicFieldAssert.IsOfType<FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>>(new List<String>() { "backupPersister" }, testPersister);
                NonPublicFieldAssert.HasValue(new List<String>() { "primaryPersister", "metricLogger" }, testMetricLogger, testPersister, true);
                NonPublicFieldAssert.HasValue(new List<String>() { "backupPersister", "metricLogger" }, testMetricLogger, testPersister, true);
            }
        }

        [Test]
        public void GetPersister_PostgreSqlWithoutMetricsWithBackup()
        {
            String testUserName = "user";
            String testPassword = "password";
            String testHost = "127.0.0.1";
            String testDatabase = "ApplicationAccess";
            Int32 testCommandTimeout = 0;
            var connectionParameters = new PostgreSqlConnectionParameters(testUserName, testPassword, testHost, testDatabase, testCommandTimeout);
            var testPersisterBackupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestBackupEvents.json");
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, testPersisterBackupFilePath))
            {

                Assert.IsAssignableFrom<AccessManagerRedundantTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, testPersister);
                NonPublicFieldAssert.IsOfType<PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryReader" }, testPersister);
                NonPublicFieldAssert.IsOfType<PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryPersister" }, testPersister);
                NonPublicFieldAssert.IsOfType<FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>>(new List<String>() { "backupPersister" }, testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "primaryPersister", "metricLogger" }, testPersister);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "backupPersister", "metricLogger" }, testPersister);
            }
        }

        [Test]
        public void GetPersister_PostgreSqlWithMetricsWithBackup()
        {
            String testUserName = "user";
            String testPassword = "password";
            String testHost = "127.0.0.1";
            String testDatabase = "ApplicationAccess";
            Int32 testCommandTimeout = 0;
            var connectionParameters = new PostgreSqlConnectionParameters(testUserName, testPassword, testHost, testDatabase, testCommandTimeout);
            var testPersisterBackupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestBackupEvents.json");
            testSqlAccessManagerTemporalBulkPersisterFactory = new SqlAccessManagerTemporalBulkPersisterFactory<String, String, String, String>
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger,
                testMetricLogger
            );

            using (IAccessManagerTemporalBulkPersister<String, String, String, String> testPersister = testSqlAccessManagerTemporalBulkPersisterFactory.GetPersister(connectionParameters, testPersisterBackupFilePath))
            {

                Assert.IsAssignableFrom<AccessManagerRedundantTemporalBulkPersister<String, String, String, String>>(testPersister);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, testPersister, true);
                NonPublicFieldAssert.IsOfType<PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryReader" }, testPersister);
                NonPublicFieldAssert.IsOfType<PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>>(new List<String>() { "primaryPersister" }, testPersister);
                NonPublicFieldAssert.IsOfType<FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>>(new List<String>() { "backupPersister" }, testPersister);
                NonPublicFieldAssert.HasValue(new List<String>() { "primaryPersister", "metricLogger" }, testMetricLogger, testPersister, true);
                NonPublicFieldAssert.HasValue(new List<String>() { "backupPersister", "metricLogger" }, testMetricLogger, testPersister, true);
            }
        }
    }
}
