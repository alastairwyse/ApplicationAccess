/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Utilities;
using NUnit.Framework;

namespace ApplicationAccess.Persistence.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.SqlServer.SqlServerPersisterBase class.
    /// </summary>
    /// <remarks>Since <see cref="SqlServerPersisterBase"/> is abstract tests are performed through derived class <see cref="SqlServerAccessManagerTemporalPersister{TUser, TGroup, TComponent, TAccess}"/>.</remarks>
    public class SqlServerPersisterBaseTests
    {
        private SqlServerAccessManagerTemporalPersister<String, String, String, String> testSqlServerAccessManagerTemporalPersister;

        [SetUp]
        protected void SetUp()
        {
            testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                5,
                10,
                60,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new NullLogger()
            );
        }

        [Test]
        public void Constructor_ConnectionStringParameterWhitespace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "  ",
                    5,
                    10,
                60,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void Constructor_RetryCountParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    -1,
                    10,
                    60,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryCount' with value -1 cannot be less than 0."));
            Assert.AreEqual("retryCount", e.ParamName);
        }

        [Test]
        public void Constructor_RetryCountParameterGreaterThan59()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    60,
                    10,
                    60,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryCount' with value 60 cannot be greater than 59."));
            Assert.AreEqual("retryCount", e.ParamName);
        }

        [Test]
        public void Constructor_RetryIntervalParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    5,
                    -1,
                    60,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryInterval' with value -1 cannot be less than 0."));
            Assert.AreEqual("retryInterval", e.ParamName);
        }

        [Test]
        public void Constructor_RetryIntervalParameterGreaterThan120()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    5,
                    121,
                    60,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryInterval' with value 121 cannot be greater than 120."));
            Assert.AreEqual("retryInterval", e.ParamName);
        }

        [Test]
        public void Constructor_OperationTimeoutParameterGreaterThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    5,
                    10,
                    -1,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'operationTimeout' with value -1 cannot be less than 0."));
            Assert.AreEqual("operationTimeout", e.ParamName);
        }
    }
}
