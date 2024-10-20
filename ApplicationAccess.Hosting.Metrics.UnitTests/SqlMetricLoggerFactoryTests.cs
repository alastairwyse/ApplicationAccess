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
using ApplicationAccess.Persistence;
using ApplicationAccess.UnitTests;
using ApplicationLogging;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;
using ApplicationMetrics.MetricLoggers.PostgreSql;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Persistence.Sql.SqlAccessManagerTemporalBulkPersisterFactory class.
    /// </summary>
    public class SqlMetricLoggerFactoryTests
    {
        protected const string testCategory = "TestMetricCategory";

        protected IBufferProcessingStrategy mockBufferProcessingStrategy;
        protected IApplicationLogger mockApplicationLogger;
        protected IntervalMetricBaseTimeUnit testIntervalMetricBaseTimeUnit;
        protected SqlMetricLoggerFactory testSqlMetricLoggerFactory;

        [SetUp]
        protected void SetUp()
        {
            mockBufferProcessingStrategy = Substitute.For<IBufferProcessingStrategy>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            testIntervalMetricBaseTimeUnit = IntervalMetricBaseTimeUnit.Nanosecond;
        }

        [Test]
        public void GetMetricLogger_SqlServerIndividualAonnectionParameters()
        {
            String testUserId = "sa";
            String testPassword = "password";
            String testDataSource = "127.0.0.1";
            String testInitialCatalogue = "ApplicationAccess";
            Int32 testRetryCount = 5;
            Int32 testRetryInterval = 10;
            Int32 testOperationTimeout = 0;
            var testConnectionParameters = new SqlServerConnectionParameters(testUserId, testPassword, testDataSource, testInitialCatalogue, testRetryCount, testRetryInterval, testOperationTimeout);
            testSqlMetricLoggerFactory = new SqlMetricLoggerFactory(testCategory, mockBufferProcessingStrategy, testIntervalMetricBaseTimeUnit, true, mockApplicationLogger);

            using (MetricLoggerBuffer testMetricLoggerBuffer = testSqlMetricLoggerFactory.GetMetricLogger(testConnectionParameters))
            {

                Assert.IsAssignableFrom<SqlServerMetricLogger>(testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "category" }, testCategory, testMetricLoggerBuffer);
                String expectedConnectionString = $"Data Source={testDataSource};Initial Catalog={testInitialCatalogue};User ID={testUserId};Password={testPassword};Encrypt=False;Authentication=SqlPassword";
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "connectionString" }, expectedConnectionString, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "retryCount" }, testRetryCount, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "retryInterval" }, testRetryInterval, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "operationTimeout" }, testOperationTimeout, testMetricLoggerBuffer);
            }
        }

        [Test]
        public void GetMetricLogger_SqlServerConnectionString()
        {
            String testConnectionString = "Data Source=127.0.0.1;Initial Catalog=ApplicationAccess;User ID=sa;Password=password;Encrypt=False;Authentication=SqlPassword";
            Int32 testRetryCount = 5;
            Int32 testRetryInterval = 10;
            Int32 testOperationTimeout = 0;
            var testConnectionParameters = new SqlServerConnectionParameters(testConnectionString, testRetryCount, testRetryInterval, testOperationTimeout);
            testSqlMetricLoggerFactory = new SqlMetricLoggerFactory(testCategory, mockBufferProcessingStrategy, testIntervalMetricBaseTimeUnit, true, mockApplicationLogger);

            using (MetricLoggerBuffer testMetricLoggerBuffer = testSqlMetricLoggerFactory.GetMetricLogger(testConnectionParameters))
            {

                Assert.IsAssignableFrom<SqlServerMetricLogger>(testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "category" }, testCategory, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "connectionString" }, testConnectionString, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "retryCount" }, testRetryCount, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "retryInterval" }, testRetryInterval, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "operationTimeout" }, testOperationTimeout, testMetricLoggerBuffer);
            }
        }

        [Test]
        public void GetMetricLogger_PostgreSqlIndividualAonnectionParameters()
        {
            String testUsername = "postgres";
            String testPassword = "password";
            String testHost = "127.0.0.1";
            String testDatabase= "ApplicationAccess";
            Int32 testCommandTimeout = 0;
            var testConnectionParameters = new PostgreSqlConnectionParameters(testUsername, testPassword, testHost, testDatabase, testCommandTimeout);
            testSqlMetricLoggerFactory = new SqlMetricLoggerFactory(testCategory, mockBufferProcessingStrategy, testIntervalMetricBaseTimeUnit, true, mockApplicationLogger);

            using (MetricLoggerBuffer testMetricLoggerBuffer = testSqlMetricLoggerFactory.GetMetricLogger(testConnectionParameters))
            {

                Assert.IsAssignableFrom<PostgreSqlMetricLogger>(testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "category" }, testCategory, testMetricLoggerBuffer);
                String expectedConnectionString = $"Host={testHost};Database={testDatabase};Username={testUsername};Password={testPassword}";
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "connectionString" }, expectedConnectionString, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "commandTimeout" }, testCommandTimeout, testMetricLoggerBuffer);
            }
        }

        [Test]
        public void GetMetricLogger_PostgreConnectionString()
        {
            String testConnectionString = "Host=127.0.0.1;Database=ApplicationAccess;Username=postgres;Password=password";
            Int32 testCommandTimeout = 0;
            var testConnectionParameters = new PostgreSqlConnectionParameters(testConnectionString, testCommandTimeout);
            testSqlMetricLoggerFactory = new SqlMetricLoggerFactory(testCategory, mockBufferProcessingStrategy, testIntervalMetricBaseTimeUnit, true, mockApplicationLogger);

            using (MetricLoggerBuffer testMetricLoggerBuffer = testSqlMetricLoggerFactory.GetMetricLogger(testConnectionParameters))
            {

                Assert.IsAssignableFrom<PostgreSqlMetricLogger>(testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "category" }, testCategory, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<String>(new List<String>() { "connectionString" }, testConnectionString, testMetricLoggerBuffer);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "commandTimeout" }, testCommandTimeout, testMetricLoggerBuffer);
            }
        }
    }
}
