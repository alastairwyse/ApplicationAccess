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
using System.Globalization;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Persistence.MongoDb.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.MongoDb.MongoDbAccessManagerTemporalBulkPersister class.
    /// </summary>
    public class MongoDbAccessManagerTemporalBulkPersisterTests
    {
        private IApplicationLogger logger;
        private IMetricLogger metricLogger;
        private MongoDbAccessManagerTemporalBulkPersister<String, String, String, String> testMongoDbAccessManagerTemporalBulkPersister;

        [SetUp]
        protected void SetUp()
        {
            logger = Substitute.For<IApplicationLogger>();
            metricLogger = Substitute.For<IMetricLogger>();
            testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>
            (
                "mongodb://testServer:27017", 
                "ApplicationAccess",
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(), 
                true,
                logger,
                metricLogger
            );
        }

        [TearDown]
        public void TearDown()
        {
            testMongoDbAccessManagerTemporalBulkPersister.Dispose();
        }

        [Test]
        public void Constructor_ConnectionStringNull()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    null,
                    "ApplicationAccess",
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    true,
                    logger,
                    metricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void Constructor_ConnectionStringWhitespace()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    " ",
                    "ApplicationAccess",
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    true,
                    logger,
                    metricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void Constructor_DatabaseNameNull()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    "mongodb://testServer:27017",
                    null,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    true,
                    logger,
                    metricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'databaseName' must contain a value."));
            Assert.AreEqual("databaseName", e.ParamName);
        }

        [Test]
        public void Constructor_DatabaseNameWhitespace()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    "mongodb://testServer:27017",
                    " ",
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    true,
                    logger,
                    metricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'databaseName' must contain a value."));
            Assert.AreEqual("databaseName", e.ParamName);
        }

        [Test]
        public void LoadStateTimeOverload_ParameterStateDateNotUtc()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.Load(DateTime.Now, new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' must be expressed as UTC."));
            Assert.AreEqual("stateTime", e.ParamName);
        }

        [Test]
        public void LoadStateTimeOverload_ParameterStateDateInTheFuture()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.Load(DateTime.MaxValue.ToUniversalTime(), new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' with value '{DateTime.MaxValue.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")}' is greater than the current time '"));
            Assert.AreEqual("stateTime", e.ParamName);
        }
    }
}
