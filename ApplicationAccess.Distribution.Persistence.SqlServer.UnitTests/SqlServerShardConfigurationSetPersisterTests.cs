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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence.SqlServer;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationLogging;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Distribution.Persistence.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerShardConfigurationSetPersister class.
    /// </summary>
    public class SqlServerShardConfigurationSetPersisterTests
    {
        protected const String updateShardConfigurationsStoredProcedureName = "UpdateShardConfiguration";
        protected const String dataElementTypeColumnName = "DataElementType";
        protected const String operationTypeColumnName = "OperationType";
        protected const String hashRangeStartColumnName = "HashRangeStart";
        protected const String clientConfigurationColumnName = "ClientConfiguration";

        private IStoredProcedureExecutionWrapper mockStoredProcedureExecutionWrapper;
        private IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<AccessManagerRestClientConfiguration> mockJsonSerializer;
        private IApplicationLogger mockLogger;
        private SqlServerShardConfigurationSetPersister<AccessManagerRestClientConfiguration, IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<AccessManagerRestClientConfiguration>> testSqlServerShardConfigurationSetPersister;

        [SetUp]
        protected void SetUp()
        {
            mockStoredProcedureExecutionWrapper = Substitute.For<IStoredProcedureExecutionWrapper>();
            mockJsonSerializer = Substitute.For<IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<AccessManagerRestClientConfiguration>>();
            mockLogger = Substitute.For<IApplicationLogger>();
            testSqlServerShardConfigurationSetPersister = new SqlServerShardConfigurationSetPersister<AccessManagerRestClientConfiguration, IDistributedAccessManagerAsyncClientConfigurationJsonSerializer<AccessManagerRestClientConfiguration>>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                5,
                10,
                60,
                mockJsonSerializer,
                mockLogger,
                mockStoredProcedureExecutionWrapper
            );
        }

        [TearDown]
        public void TearDown()
        {
            testSqlServerShardConfigurationSetPersister.Dispose();
        }

        [Test]
        public void Write_FailureToSerializeClientConfiguration()
        {
            var testClientConfiguration1 = new AccessManagerRestClientConfiguration(new Uri("https://127.0.0.1:5001/"));
            var testClientConfiguration2 = new AccessManagerRestClientConfiguration(new Uri("https://127.0.0.1:5002/"));
            var testConfigurationItems = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, Int32.MinValue, testClientConfiguration1),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, 0, testClientConfiguration2),
            };
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems);
            var mockException = new Exception("Mock exception");
            mockJsonSerializer.When(serializer => serializer.Serialize(testClientConfiguration2)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerShardConfigurationSetPersister.Write(testShardConfigurationSet);
            });

            mockJsonSerializer.Received(1).Serialize(testClientConfiguration1);
            mockJsonSerializer.Received(1).Serialize(testClientConfiguration2);
            Assert.That(e.Message, Does.StartWith($"Failed to serialize shard configuration item for data element 'User', operation 'Event', and hash range start 0."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void Write()
        {
            var testClientConfiguration1 = new AccessManagerRestClientConfiguration(new Uri("https://127.0.0.1:5001/"));
            var testClientConfiguration2 = new AccessManagerRestClientConfiguration(new Uri("https://127.0.0.1:5002/"));
            var testClientConfiguration3 = new AccessManagerRestClientConfiguration(new Uri("https://127.0.0.1:5003/"));
            var testConfigurationItems = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, Int32.MinValue, testClientConfiguration1),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, testClientConfiguration2),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.GroupToGroupMapping, Operation.Query, Int32.MaxValue, testClientConfiguration3),
            };
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems);
            String serializedClientConfiguration1 = $"{{ \"BaseUrl\": \"{testClientConfiguration1.ToString()}\" }}";
            String serializedClientConfiguration2 = $"{{ \"BaseUrl\": \"{testClientConfiguration2.ToString()}\" }}";
            String serializedClientConfiguration3 = $"{{ \"BaseUrl\": \"{testClientConfiguration3.ToString()}\" }}";
            mockJsonSerializer.Serialize(testClientConfiguration1).Returns(serializedClientConfiguration1);
            mockJsonSerializer.Serialize(testClientConfiguration2).Returns(serializedClientConfiguration2);
            mockJsonSerializer.Serialize(testClientConfiguration3).Returns(serializedClientConfiguration3);
            DataTable capturedStagingTable = null;
            mockStoredProcedureExecutionWrapper.Execute(updateShardConfigurationsStoredProcedureName, Arg.Do<IEnumerable<SqlParameter>>(parameters => capturedStagingTable = (DataTable)parameters.First<SqlParameter>().Value));

            testSqlServerShardConfigurationSetPersister.Write(testShardConfigurationSet);

            Assert.AreEqual(3, capturedStagingTable.Rows.Count);
            AssertStagingTableRow(capturedStagingTable.Rows[0], "User", "Event", Int32.MinValue, serializedClientConfiguration1);
            AssertStagingTableRow(capturedStagingTable.Rows[1], "User", "Query", 0, serializedClientConfiguration2);
            AssertStagingTableRow(capturedStagingTable.Rows[2], "GroupToGroupMapping", "Query", Int32.MaxValue, serializedClientConfiguration3);
            mockJsonSerializer.Received(1).Serialize(testClientConfiguration1);
            mockJsonSerializer.Received(1).Serialize(testClientConfiguration2);
            mockJsonSerializer.Received(1).Serialize(testClientConfiguration3);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Asserts that the specified row of a shard configuration item staging table contains the specified values.
        /// </summary>
        protected void AssertStagingTableRow
        (
            DataRow row,
            String dataElementTypeValue,
            String operationTypeValue,
            Int32 hashRangeStartValue,
            String clientConfigurationValue
        )
        {
            Assert.IsInstanceOf<String>(dataElementTypeValue);
            Assert.AreEqual(dataElementTypeValue, row[dataElementTypeColumnName]);
            Assert.IsInstanceOf<String>(operationTypeValue);
            Assert.AreEqual(operationTypeValue, row[operationTypeColumnName]);
            Assert.IsInstanceOf<Int32>(hashRangeStartValue);
            Assert.AreEqual(hashRangeStartValue, row[hashRangeStartColumnName]);
            Assert.IsInstanceOf<String>(clientConfigurationValue);
            Assert.AreEqual(clientConfigurationValue, row[clientConfigurationColumnName]);
        }

        #endregion
    }
}
