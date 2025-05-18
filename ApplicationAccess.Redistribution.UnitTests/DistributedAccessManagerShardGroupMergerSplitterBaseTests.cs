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
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution;
using ApplicationAccess.Persistence;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Redistribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupMergerSplitterBase class.
    /// </summary>
    public class DistributedAccessManagerShardGroupMergerSplitterBaseTests
    {
        private Int32 testSourceWriterNodeOperationsCompleteCheckRetryAttempts;
        private Int32 testSourceWriterNodeOperationsCompleteCheckRetryInterval;
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroupEventReader;
        private IDistributedAccessManagerWriterAdministrator mockSourceShardGroupWriterAdministrator;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerShardGroupMergerSplitterBaseWithProtectedMembers testShardGroupSplitterBase;

        [SetUp]
        protected void SetUp()
        {
            testSourceWriterNodeOperationsCompleteCheckRetryAttempts = 3;
            testSourceWriterNodeOperationsCompleteCheckRetryInterval = 50;
            mockSourceShardGroupEventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockSourceShardGroupWriterAdministrator = Substitute.For<IDistributedAccessManagerWriterAdministrator>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardGroupSplitterBase = new DistributedAccessManagerShardGroupMergerSplitterBaseWithProtectedMembers(mockApplicationLogger, mockMetricLogger);
        }

        [Test]
        public void GetNextEventAfter_EventRetrievalFails()
        {
            var testEventId = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            var mockException = new Exception("Mock exception");
            mockSourceShardGroupEventReader.When(reader => reader.GetNextEventAfter(testEventId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitterBase.GetNextEventAfter(mockSourceShardGroupEventReader, testEventId);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve next event after event with id '{testEventId.ToString()}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void WaitForSourceWriterNodeEventProcessingCompletion_RetrievingEventProcessingCountFails()
        {
            var mockException = new Exception("Mock exception");
            mockSourceShardGroupWriterAdministrator.When(administrator => administrator.GetEventProcessingCount()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitterBase.WaitForSourceWriterNodeEventProcessingCompletion
                (
                    mockSourceShardGroupWriterAdministrator,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to check for active operations in source shard group event writer node."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void WaitForSourceWriterNodeEventProcessingCompletion_ProcessingNotCompletedAfterRetries()
        {
            mockSourceShardGroupWriterAdministrator.GetEventProcessingCount().Returns(3);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitterBase.WaitForSourceWriterNodeEventProcessingCompletion
                (
                    mockSourceShardGroupWriterAdministrator,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            mockSourceShardGroupWriterAdministrator.Received(4).GetEventProcessingCount();
            mockMetricLogger.Received(4).Set(Arg.Any<WriterNodeEventProcessingCount>(), 3);
            mockMetricLogger.Received(3).Increment(Arg.Any<EventProcessingCountCheckRetried>());
            Assert.That(e.Message, Does.StartWith($"Active operations in source shard group event writer node remains at 3 after 3 retries with 50ms interval."));
        }

        [Test]
        public void WaitForSourceWriterNodeEventProcessingCompletion_SuccessAfterRetries()
        {
            mockSourceShardGroupWriterAdministrator.GetEventProcessingCount().Returns(3, 2, 0);

            testShardGroupSplitterBase.WaitForSourceWriterNodeEventProcessingCompletion
            (
                mockSourceShardGroupWriterAdministrator,
                testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                testSourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            mockSourceShardGroupWriterAdministrator.Received(3).GetEventProcessingCount();
            mockMetricLogger.Received(1).Set(Arg.Any<WriterNodeEventProcessingCount>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<WriterNodeEventProcessingCount>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<WriterNodeEventProcessingCount>(), 0);
            mockMetricLogger.Received(2).Increment(Arg.Any<EventProcessingCountCheckRetried>());
        }

        [Test]
        public void FlushSourceWriterNodeEventBuffers_FlushFails()
        {
            var mockException = new Exception("Mock exception");
            mockSourceShardGroupWriterAdministrator.When(administrator => administrator.FlushEventBuffers()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitterBase.FlushSourceWriterNodeEventBuffers(mockSourceShardGroupWriterAdministrator);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to flush event buffer(s) in the source shard group event writer node."));
            Assert.AreSame(mockException, e.InnerException);
        }

        #region Nested Classes

        private class DistributedAccessManagerShardGroupMergerSplitterBaseWithProtectedMembers : DistributedAccessManagerShardGroupMergerSplitterBase
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.UnitTests.DistributedAccessManagerShardGroupMergerSplitterBaseTests+DistributedAccessManagerShardGroupMergerSplitterBaseWithProtectedMembers class.
            /// </summary>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public DistributedAccessManagerShardGroupMergerSplitterBaseWithProtectedMembers(IApplicationLogger logger, IMetricLogger metricLogger)
                : base(logger, metricLogger)
            {
            }

            /// <summary>
            /// Retrieves the id of the next event after the specified event. 
            /// </summary>
            /// <param name="reader">The <see cref="IAccessManagerTemporalEventBatchReader"/> to get the event from.</param>
            /// <param name="inputEventId">The id of the preceding event.</param>
            /// <returns>The next event, or null of the specified event is the latest.</returns>
            public new Nullable<Guid> GetNextEventAfter(IAccessManagerTemporalEventBatchReader reader, Guid inputEventId)
            {
                return base.GetNextEventAfter(reader, inputEventId);
            }

            /// <summary>
            /// Waits until any active event processing in the source shard group writer node is completed.
            /// </summary>
            /// <param name="sourceShardGroupWriterAdministrator">The source shard group writer node client.</param>
            /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking active operations.</param>
            /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
            public new void WaitForSourceWriterNodeEventProcessingCompletion
            (
                IDistributedAccessManagerWriterAdministrator sourceShardGroupWriterAdministrator,
                Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
            )
            {
                base.WaitForSourceWriterNodeEventProcessingCompletion(sourceShardGroupWriterAdministrator, sourceWriterNodeOperationsCompleteCheckRetryAttempts, sourceWriterNodeOperationsCompleteCheckRetryInterval);
            }

            /// <summary>
            /// Flushes the event buffer(s) on the source shard group's writer node.
            /// </summary>
            /// <param name="sourceShardGroupWriterAdministrator">The source shard group writer node client.</param>
            public new void FlushSourceWriterNodeEventBuffers(IDistributedAccessManagerWriterAdministrator sourceShardGroupWriterAdministrator)
            {
                base.FlushSourceWriterNodeEventBuffers(sourceShardGroupWriterAdministrator);
            }
        }

        #endregion
    }
}
