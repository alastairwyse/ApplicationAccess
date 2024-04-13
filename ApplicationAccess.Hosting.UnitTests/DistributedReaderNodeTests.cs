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
using System.Globalization;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NSubstitute;
using ApplicationMetrics;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.DistributedReaderNode class.
    /// </summary>
    public class DistributedReaderNodeTests
    {
        private Tuple<Guid, DateTime> returnedLoadState;
        private IReaderNodeRefreshStrategy mockRefreshStrategy;
        private IAccessManagerTemporalEventQueryProcessor<String, String, ApplicationScreen, AccessLevel> mockEventCache;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private IMetricLogger mockMetricLogger;
        private IDateTimeProvider mockDateTimeProvider;
        private DistributedReaderNode<String, String, ApplicationScreen, AccessLevel> testDistributedReaderNode;

        [SetUp]
        protected void SetUp()
        {
            returnedLoadState = new Tuple<Guid, DateTime>(Guid.Parse("5555795a-6408-4084-aa86-a70f8731376a"), CreateDataTimeFromString("2022-10-06 19:27:01"));
            mockRefreshStrategy = Substitute.For<IReaderNodeRefreshStrategy>();
            mockEventCache = Substitute.For<IAccessManagerTemporalEventQueryProcessor<String, String, ApplicationScreen, AccessLevel>>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            testDistributedReaderNode = new DistributedReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);
        }

        [TearDown]
        public void TearDown()
        {
            testDistributedReaderNode.Dispose();
        }

        [Test]
        public void Constructor_MetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            DistributedReaderNode<String, String, ApplicationScreen, AccessLevel> testDistributedDistributedReaderNode;
            var fieldNamePath = new List<String>() { "metricLogger" };
            testDistributedDistributedReaderNode = new DistributedReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader);

            NonPublicFieldAssert.IsOfType<ApplicationMetrics.MetricLoggers.NullMetricLogger>(fieldNamePath, testDistributedDistributedReaderNode);


            testDistributedDistributedReaderNode = new DistributedReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MetricLoggerBaseTypeInclusionFilter>(fieldNamePath, testDistributedDistributedReaderNode);


            fieldNamePath = new List<String>() { "metricLogger", "filteredMetricLogger" };
            testDistributedDistributedReaderNode = new DistributedReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDistributedDistributedReaderNode, true);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion
    }
}
