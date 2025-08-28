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
using System.Linq;
using System.Linq.Expressions;
using System.Data;
using System.Globalization;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using Microsoft.Data.SqlClient;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Distribution.Persistence.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerAccessManagerTemporalEventBatchReader class.
    /// </summary>
    public class SqlServerAccessManagerTemporalEventBatchReaderTests
    {
        #pragma warning disable 1591

        protected const String deleteUserEventsStoredProcedureName = "DeleteUserEvents";
        protected const String deleteGroupEventsStoredProcedureName = "DeleteGroupEvents";
        protected const String deleteUserToGroupMappingEventsStoredProcedureName = "DeleteUserToGroupMappingEvents";
        protected const String deleteUserToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName = "DeleteUserToApplicationComponentAndAccessLevelMappingEvents";
        protected const String deleteGroupToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName = "DeleteGroupToApplicationComponentAndAccessLevelMappingEvents";
        protected const String deleteUserToEntityMappingEventsStoredProcedureName = "DeleteUserToEntityMappingEvents";
        protected const String deleteGroupToEntityMappingEventsStoredProcedureName = "DeleteGroupToEntityMappingEvents";
        protected const String hashRangeStartParameterName = "@HashRangeStart";
        protected const String hashRangeEndParameterName = "@HashRangeEnd";

        #pragma warning restore 1591

        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private IStoredProcedureExecutionWrapper mockStoredProcedureExecutionWrapper;
        private SqlServerAccessManagerTemporalEventBatchReaderWithProtectedMembers<String, String, String, String> testSqlServerAccessManagerTemporalEventBatchReader;

        [SetUp]
        protected void SetUp()
        {
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockStoredProcedureExecutionWrapper = Substitute.For<IStoredProcedureExecutionWrapper>();
            testSqlServerAccessManagerTemporalEventBatchReader = new SqlServerAccessManagerTemporalEventBatchReaderWithProtectedMembers<String, String, String, String>
            (
                "Data Source=127.0.0.1;Initial Catalog=ApplicationAccess;User ID=sa;Password=password;Encrypt=False;Authentication=SqlPassword", 
                5, 
                10, 
                0, 
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                mockApplicationLogger,
                mockMetricLogger,
                mockStoredProcedureExecutionWrapper
            );
        }

        [TearDown]
        public void TearDown()
        {
            testSqlServerAccessManagerTemporalEventBatchReader.Dispose();
        }

        [Test]
        public void GenerateGetEventsQuery_EventCountSetFilterGroupEventsByHashRangeTrue()
        {
            DateTime testTransactionTime = DateTime.ParseExact("2025-02-08 16:57:29.0000001", "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            testTransactionTime = DateTime.SpecifyKind(testTransactionTime, DateTimeKind.Utc);
            Int32 testTransactionSequence = 2;
            Int32 testHashRangeStart = -2345;
            Int32 testHashRangeEnd = 6789;

            String result = testSqlServerAccessManagerTemporalEventBatchReader.GenerateGetEventsQuery(testTransactionTime, testTransactionSequence, testHashRangeStart, testHashRangeEnd, true);

            String[] resultLines = result.Split(Environment.NewLine);
            Assert.AreEqual("            SELECT   ", resultLines[1]);
            Assert.AreEqual("                    EventType, ", resultLines[2]);
            Assert.AreEqual("                        WHERE   eu.HashCode BETWEEN -2345 AND 6789 ", resultLines[28]);
            Assert.AreEqual("                        WHERE   eg.HashCode BETWEEN -2345 AND 6789 ", resultLines[46]);
            Assert.AreEqual("                        UNION ALL ", resultLines[47]);
            Assert.AreEqual("                        WHERE   eug.HashCode BETWEEN -2345 AND 6789 ", resultLines[68]);
            Assert.AreEqual("                        WHERE   euaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[92]);
            Assert.AreEqual("                        WHERE   egaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[116]);
            Assert.AreEqual("                        WHERE   eue.HashCode BETWEEN -2345 AND 6789 ", resultLines[176]);
            Assert.AreEqual("                        WHERE   ege.HashCode BETWEEN -2345 AND 6789 ", resultLines[200]);
            Assert.AreEqual("            WHERE   TransactionTime >= CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) ", resultLines[202]);
            Assert.AreEqual("              AND   NOT (TransactionTime = CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) AND TransactionSequence < 2) ", resultLines[203]);
        }

        [Test]
        public void GenerateGetEventsQuery_EventCountNullFilterGroupEventsByHashRangeTrue()
        {
            DateTime testTransactionTime = DateTime.ParseExact("2025-02-08 16:57:29.0000001", "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            testTransactionTime = DateTime.SpecifyKind(testTransactionTime, DateTimeKind.Utc);
            Int32 testTransactionSequence = 2;
            Int32 testHashRangeStart = -2345;
            Int32 testHashRangeEnd = 6789;
            Int32 testEventCount = 10000;

            String result = testSqlServerAccessManagerTemporalEventBatchReader.GenerateGetEventsQuery(testTransactionTime, testTransactionSequence, testHashRangeStart, testHashRangeEnd, true, testEventCount);

            String[] resultLines = result.Split(Environment.NewLine);
            Assert.AreEqual("            SELECT  TOP(10000) ", resultLines[1]);
            Assert.AreEqual("                    EventType, ", resultLines[2]);
            Assert.AreEqual("                        WHERE   eu.HashCode BETWEEN -2345 AND 6789 ", resultLines[28]);
            Assert.AreEqual("                        WHERE   eg.HashCode BETWEEN -2345 AND 6789 ", resultLines[46]);
            Assert.AreEqual("                        UNION ALL ", resultLines[47]);
            Assert.AreEqual("                        WHERE   eug.HashCode BETWEEN -2345 AND 6789 ", resultLines[68]);
            Assert.AreEqual("                        WHERE   euaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[92]);
            Assert.AreEqual("                        WHERE   egaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[116]);
            Assert.AreEqual("                        WHERE   eue.HashCode BETWEEN -2345 AND 6789 ", resultLines[176]);
            Assert.AreEqual("                        WHERE   ege.HashCode BETWEEN -2345 AND 6789 ", resultLines[200]);
            Assert.AreEqual("            WHERE   TransactionTime >= CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) ", resultLines[202]);
            Assert.AreEqual("              AND   NOT (TransactionTime = CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) AND TransactionSequence < 2) ", resultLines[203]);
        }

        [Test]
        public void GenerateGetEventsQuery_EventCountSetFilterGroupEventsByHashRangeFalse()
        {
            DateTime testTransactionTime = DateTime.ParseExact("2025-02-08 16:57:29.0000001", "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            testTransactionTime = DateTime.SpecifyKind(testTransactionTime, DateTimeKind.Utc);
            Int32 testTransactionSequence = 2;
            Int32 testHashRangeStart = -2345;
            Int32 testHashRangeEnd = 6789;

            String result = testSqlServerAccessManagerTemporalEventBatchReader.GenerateGetEventsQuery(testTransactionTime, testTransactionSequence, testHashRangeStart, testHashRangeEnd, false);

            String[] resultLines = result.Split(Environment.NewLine);
            Assert.AreEqual("            SELECT   ", resultLines[1]);
            Assert.AreEqual("                    EventType, ", resultLines[2]);
            Assert.AreEqual("                        WHERE   eu.HashCode BETWEEN -2345 AND 6789 ", resultLines[28]);
            Assert.AreEqual("                        ", resultLines[46]);
            Assert.AreEqual("                        UNION ALL ", resultLines[47]);
            Assert.AreEqual("                        WHERE   eug.HashCode BETWEEN -2345 AND 6789 ", resultLines[68]);
            Assert.AreEqual("                        WHERE   euaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[92]);
            Assert.AreEqual("                        WHERE   egaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[116]);
            Assert.AreEqual("                        WHERE   eue.HashCode BETWEEN -2345 AND 6789 ", resultLines[176]);
            Assert.AreEqual("                        WHERE   ege.HashCode BETWEEN -2345 AND 6789 ", resultLines[200]);
            Assert.AreEqual("            WHERE   TransactionTime >= CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) ", resultLines[202]);
            Assert.AreEqual("              AND   NOT (TransactionTime = CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) AND TransactionSequence < 2) ", resultLines[203]);
        }

        [Test]
        public void GenerateGetEventsQuery_EventCountNullFilterGroupEventsByHashRangeFalse()
        {
            DateTime testTransactionTime = DateTime.ParseExact("2025-02-08 16:57:29.0000001", "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            testTransactionTime = DateTime.SpecifyKind(testTransactionTime, DateTimeKind.Utc);
            Int32 testTransactionSequence = 2;
            Int32 testHashRangeStart = -2345;
            Int32 testHashRangeEnd = 6789;
            Int32 testEventCount = 10000;

            String result = testSqlServerAccessManagerTemporalEventBatchReader.GenerateGetEventsQuery(testTransactionTime, testTransactionSequence, testHashRangeStart, testHashRangeEnd, false, testEventCount);

            String[] resultLines = result.Split(Environment.NewLine);
            Assert.AreEqual("            SELECT  TOP(10000) ", resultLines[1]);
            Assert.AreEqual("                    EventType, ", resultLines[2]);
            Assert.AreEqual("                        WHERE   eu.HashCode BETWEEN -2345 AND 6789 ", resultLines[28]);
            Assert.AreEqual("                        ", resultLines[46]);
            Assert.AreEqual("                        UNION ALL ", resultLines[47]);
            Assert.AreEqual("                        WHERE   eug.HashCode BETWEEN -2345 AND 6789 ", resultLines[68]);
            Assert.AreEqual("                        WHERE   euaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[92]);
            Assert.AreEqual("                        WHERE   egaa.HashCode BETWEEN -2345 AND 6789 ", resultLines[116]);
            Assert.AreEqual("                        WHERE   eue.HashCode BETWEEN -2345 AND 6789 ", resultLines[176]);
            Assert.AreEqual("                        WHERE   ege.HashCode BETWEEN -2345 AND 6789 ", resultLines[200]);
            Assert.AreEqual("            WHERE   TransactionTime >= CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) ", resultLines[202]);
            Assert.AreEqual("              AND   NOT (TransactionTime = CONVERT(datetime2, '2025-02-08T16:57:29.0000001', 126) AND TransactionSequence < 2) ", resultLines[203]);
        }

        [Test]
        public void DeleteEvents_IncludeGroupEventsParameterTrue()
        {
            var testHashRangeStart = -1234;
            var testHashRangeEnd = 5678;
            var expectedHashRangeStartParameter = new SqlParameter(hashRangeStartParameterName, SqlDbType.Int);
            expectedHashRangeStartParameter.Value = testHashRangeStart;
            var expectedHashRangeEndParameter = new SqlParameter(hashRangeEndParameterName, SqlDbType.Int);
            expectedHashRangeEndParameter.Value = testHashRangeEnd;
            var expectedParameters = new List<SqlParameter> { expectedHashRangeStartParameter, expectedHashRangeEndParameter };

            testSqlServerAccessManagerTemporalEventBatchReader.DeleteEvents(testHashRangeStart, testHashRangeEnd, true);

            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteGroupToEntityMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserToEntityMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteGroupToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserToGroupMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteGroupEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
        }

        [Test]
        public void DeleteEvents_IncludeGroupEventsParameterFalse()
        {
            var testHashRangeStart = -1234;
            var testHashRangeEnd = 5678;
            var expectedHashRangeStartParameter = new SqlParameter(hashRangeStartParameterName, SqlDbType.Int);
            expectedHashRangeStartParameter.Value = testHashRangeStart;
            var expectedHashRangeEndParameter = new SqlParameter(hashRangeEndParameterName, SqlDbType.Int);
            expectedHashRangeEndParameter.Value = testHashRangeEnd;
            var expectedParameters = new List<SqlParameter> { expectedHashRangeStartParameter, expectedHashRangeEndParameter };

            testSqlServerAccessManagerTemporalEventBatchReader.DeleteEvents(-1234, 5678, false);

            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteGroupToEntityMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserToEntityMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteGroupToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserToApplicationComponentAndAccessLevelMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserToGroupMappingEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
            mockStoredProcedureExecutionWrapper.DidNotReceive().ExecuteWithDeadlockRetry(deleteGroupEventsStoredProcedureName, Arg.Any<IList<SqlParameter>>());
            mockStoredProcedureExecutionWrapper.Received(1).ExecuteWithDeadlockRetry(deleteUserEventsStoredProcedureName, Arg.Is<IList<SqlParameter>>(ContainEqualParameters(expectedParameters)));
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns an <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/> which checks whether a list of <see cref="SqlParameter">SqlParameters</see> matches the list in parameter <paramref name="expected"/>.
        /// </summary>
        /// <param name="expected">The list of parameters the predicate compares to.</param>
        /// <returns>The <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/>.</returns>
        /// <remarks>Designed to be passed to the 'predicate' parameter of the NSubstitute Arg.Any{T} argument matcher.</remarks>
        protected Expression<Predicate<IList<SqlParameter>>> ContainEqualParameters(IList<SqlParameter> expected)
        {
            return (IList<SqlParameter> actual) => SqlParameterListsContainSameValues(expected, actual);
        }

        /// <summary>
        /// Checks whether two lists of <see cref="SqlParameter"/> contain equal elements.
        /// </summary>
        /// <param name="enumerable1">The first list.</param>
        /// <param name="enumerable2">The second list.</param>
        /// <returns>True if the lists contain the same parameters.  False otherwise.</returns>
        protected Boolean SqlParameterListsContainSameValues(IList<SqlParameter> list1, IList<SqlParameter> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (Int32 i = 0; i < list1.Count; i++)
            {
                if (list1[i].ParameterName != list2[i].ParameterName)
                    return false;
                if (list1[i].SqlDbType != list2[i].SqlDbType)
                    return false;
                if (!(list1[i].Value.Equals(list2[i].Value)))
                    return false;
            }

            return true;
        }

        #endregion

        #region Nested Classes

        private class SqlServerAccessManagerTemporalEventBatchReaderWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : SqlServerAccessManagerTemporalEventBatchReader<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.SqlServer.SqlServerAccessManagerTemporalEventBatchReader class.
            /// </summary>
            /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
            /// <param name="retryCount">The number of times an operation against the SQL Server database should be retried in the case of execution failure.</param>
            /// <param name="retryInterval">The time in seconds between operation retries.</param>
            /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
            /// <param name="userStringifier">A string converter for users.</param>
            /// <param name="groupStringifier">A string converter for groups.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <param name="storedProcedureExecutor">A test (mock) <see cref="IStoredProcedureExecutionWrapper"/> object.</param>
            /// <remarks>This constructor is included to facilitate unit testing.</remarks>
            public SqlServerAccessManagerTemporalEventBatchReaderWithProtectedMembers
            (
                String connectionString,
                Int32 retryCount,
                Int32 retryInterval,
                Int32 operationTimeout,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                IApplicationLogger logger,
                IMetricLogger metricLogger,
                IStoredProcedureExecutionWrapper storedProcedureExecutor
            )
                : base(connectionString, retryCount, retryInterval, operationTimeout, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger, metricLogger, storedProcedureExecutor)
            {
            }

            /// <summary>
            /// Generates a query which returns an ordered series of events.
            /// </summary>
            /// <param name="transactionTime">The transaction time of the first event to return.</param>
            /// <param name="transactionSequence">The transaction sequence number of the first event to return.</param>
            /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to return.</param>
            /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to return.</param>
            /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will return all group events if set to false.</param>
            /// <param name="eventCount">The (optional) maximum number of events to return.  If not specified, all events in the database are returned.</param>
            /// <returns>The query.</returns>
            public new String GenerateGetEventsQuery(DateTime transactionTime, Int32 transactionSequence, Int32 hashRangeStart, Int32 hashRangeEnd, Boolean filterGroupEventsByHashRange, Nullable<Int32> eventCount = null)
            {
                return base.GenerateGetEventsQuery(transactionTime, transactionSequence, hashRangeStart, hashRangeEnd, filterGroupEventsByHashRange, eventCount);
            }
        }

        #endregion
    }
}
