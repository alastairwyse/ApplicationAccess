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
using System.Globalization;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationAccess.Redistribution.Models;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Redistribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupMerger class.
    /// </summary>
    public class DistributedAccessManagerShardGroupMergerTests
    {
        private Int32 testSourceWriterNodeOperationsCompleteCheckRetryAttempts;
        private Int32 testSourceWriterNodeOperationsCompleteCheckRetryInterval;
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup1EventReader;
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup2EventReader;
        private IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String> mockTargetShardGroupEventPersister;
        private IEventPersisterBuffer mockTargetShardGroupEventPersisterBuffer;
        private IDistributedAccessManagerOperationRouter mockOperationRouter;
        private IDistributedAccessManagerWriterAdministrator mockSourceShardGroup1WriterAdministrator;
        private IDistributedAccessManagerWriterAdministrator mockSourceShardGroup2WriterAdministrator;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerShardGroupMergerWithProtectedMembers testShardGroupMerger;


        [SetUp]
        protected void SetUp()
        {
            testSourceWriterNodeOperationsCompleteCheckRetryAttempts = 3;
            testSourceWriterNodeOperationsCompleteCheckRetryInterval = 50;
            mockSourceShardGroup1EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockSourceShardGroup2EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockTargetShardGroupEventPersister = Substitute.For<IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String>>();
            mockTargetShardGroupEventPersisterBuffer = Substitute.For<IEventPersisterBuffer>();
            mockOperationRouter = Substitute.For<IDistributedAccessManagerOperationRouter>();
            mockSourceShardGroup1WriterAdministrator = Substitute.For<IDistributedAccessManagerWriterAdministrator>();
            mockSourceShardGroup2WriterAdministrator = Substitute.For<IDistributedAccessManagerWriterAdministrator>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardGroupMerger = new DistributedAccessManagerShardGroupMergerWithProtectedMembers(mockApplicationLogger, mockMetricLogger);
        }

        [Test]
        public void MergeEventsToTargetShardGroup_SourceWriterNodeOperationsCompleteCheckRetryAttemptsParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testShardGroupMerger.MergeEventsToTargetShardGroup
                (
                    mockSourceShardGroup1EventReader,
                    mockSourceShardGroup2EventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroup1WriterAdministrator,
                    mockSourceShardGroup2WriterAdministrator,
                    3,
                    -1,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceWriterNodeOperationsCompleteCheckRetryAttempts' with value -1 must be greater than or equal to 0."));
            Assert.AreEqual("sourceWriterNodeOperationsCompleteCheckRetryAttempts", e.ParamName);
        }

        [Test]
        public void MergeEventsToTargetShardGroup_SourceWriterNodeOperationsCompleteCheckRetryIntervalParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testShardGroupMerger.MergeEventsToTargetShardGroup
                (
                    mockSourceShardGroup1EventReader,
                    mockSourceShardGroup2EventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroup1WriterAdministrator,
                    mockSourceShardGroup2WriterAdministrator,
                    3,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    -1
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceWriterNodeOperationsCompleteCheckRetryInterval' with value -1 must be greater than or equal to 0."));
            Assert.AreEqual("sourceWriterNodeOperationsCompleteCheckRetryInterval", e.ParamName);
        }

        [Test]
        public void MergeEventsToTargetShardGroup_EventBatchSizeParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testShardGroupMerger.MergeEventsToTargetShardGroup
                (
                    mockSourceShardGroup1EventReader,
                    mockSourceShardGroup2EventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroup1WriterAdministrator,
                    mockSourceShardGroup2WriterAdministrator,
                    0,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'eventBatchSize' with value 0 must be greater than 0."));
            Assert.AreEqual("eventBatchSize", e.ParamName);
        }

        [Test]
        public void MergeEventsToTargetShardGroup_NoEventsExistInSourceShardGroup1()
        {
            var mockException = new Exception("Mock exception");
            mockSourceShardGroup1EventReader.When(reader => reader.GetInitialEvent()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupMerger.MergeEventsToTargetShardGroup
                (
                    mockSourceShardGroup1EventReader,
                    mockSourceShardGroup2EventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroup1WriterAdministrator,
                    mockSourceShardGroup2WriterAdministrator,
                    1000,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve initial event id from the source shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeEventsToTargetShardGroup_NoEventsExistInSourceShardGroup2()
        {
            var mockException = new Exception("Mock exception");
            mockSourceShardGroup2EventReader.When(reader => reader.GetInitialEvent()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupMerger.MergeEventsToTargetShardGroup
                (
                    mockSourceShardGroup1EventReader,
                    mockSourceShardGroup2EventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroup1WriterAdministrator,
                    mockSourceShardGroup2WriterAdministrator,
                    1000,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve initial event id from the source shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeEventsToTargetShardGroup_SourceShardGroup1EventsPersistedFirst()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            // Mock initial event reads and initial merge (only 1 event from mockSourceShardGroup1EventReader buffered/persisted)
            mockSourceShardGroup1EventReader.GetInitialEvent().Returns<Guid>(event1Id);
            mockSourceShardGroup2EventReader.GetInitialEvent().Returns<Guid>(event3Id);
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event1 });
            mockSourceShardGroup2EventReader.GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event3 });
            // First return value is for the end of the initial merge (simulating that event2 is not yet in the source).  Second value is for the final merge, after event2 has been written to the source.
            mockSourceShardGroup1EventReader.GetNextEventAfter(event1Id).Returns((Nullable<Guid>)null, event2Id);
            // Mock waiting for event processing to complete
            mockSourceShardGroup1WriterAdministrator.GetEventProcessingCount().Returns(0);
            mockSourceShardGroup2WriterAdministrator.GetEventProcessingCount().Returns(0);
            // Mock final merge
            mockSourceShardGroup1EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event2 });
            // Don't need to redeclare initial call to mockSourceShardGroup2EventReader.GetEvents() as it's same as the one above
            mockSourceShardGroup1EventReader.GetNextEventAfter(event2Id).Returns((Nullable<Guid>)null);
            mockSourceShardGroup2EventReader.GetNextEventAfter(event3Id).Returns(event4Id);
            mockSourceShardGroup2EventReader.GetEvents(event4Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event4 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event4Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            testShardGroupMerger.MergeEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                mockSourceShardGroup2WriterAdministrator,
                1,
                testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                testSourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            mockSourceShardGroup1EventReader.Received(1).GetInitialEvent();
            mockSourceShardGroup2EventReader.Received(1).GetInitialEvent();
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup2EventReader.Received(2).GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup1EventReader.Received(2).GetNextEventAfter(event1Id);
            mockSourceShardGroup1WriterAdministrator.Received(1).GetEventProcessingCount();
            mockSourceShardGroup2WriterAdministrator.Received(1).GetEventProcessingCount();
            mockSourceShardGroup1WriterAdministrator.Received(1).FlushEventBuffers();
            mockSourceShardGroup2WriterAdministrator.Received(1).FlushEventBuffers();
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event2Id);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event4Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event4Id);
            mockTargetShardGroupEventPersister.Received(4).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            Assert.AreEqual(4, capturedPersistedEventLists.Count);
            Assert.AreEqual(1, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreEqual(1, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event2, capturedPersistedEventLists[1][0]);
            Assert.AreEqual(1, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[2][0]);
            Assert.AreEqual(1, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event4, capturedPersistedEventLists[3][0]);
            mockMetricLogger.Received(1).Add(Arg.Any<DuplicatePrimaryAddElementEventsFiltered>(), 0);
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Merging events from source shard groups to target shard group...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Starting initial event batch merge...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed initial event batch merge.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Pausing operations in the source and target shard groups.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Source writer nodes event processing to complete.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Flushing source writer nodes event buffers...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed flushing source writer nodes event buffers.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Starting final event batch merge...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed final event batch merge.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed merging events from source shard groups to target shard group.");
        }

        [Test]
        public void MergeEventsToTargetShardGroup_SourceShardGroup2EventsPersistedFirst()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            // Mock initial event reads and initial merge (only 1 event from mockSourceShardGroup2EventReader buffered/persisted)
            mockSourceShardGroup1EventReader.GetInitialEvent().Returns<Guid>(event3Id);
            mockSourceShardGroup2EventReader.GetInitialEvent().Returns<Guid>(event1Id);
            mockSourceShardGroup1EventReader.GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event3 });
            mockSourceShardGroup2EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event1 });
            // First return value is for the end of the initial merge (simulating that event2 is not yet in the source).  Second value is for the final merge, after event2 has been written to the source.
            mockSourceShardGroup2EventReader.GetNextEventAfter(event1Id).Returns((Nullable<Guid>)null, event2Id);
            // Mock waiting for event processing to complete
            mockSourceShardGroup1WriterAdministrator.GetEventProcessingCount().Returns(0);
            mockSourceShardGroup2WriterAdministrator.GetEventProcessingCount().Returns(0);
            // Mock final merge
            mockSourceShardGroup2EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event2 });
            // Don't need to redeclare initial call to mockSourceShardGroup1EventReader.GetEvents() as it's same as the one above
            mockSourceShardGroup2EventReader.GetNextEventAfter(event2Id).Returns((Nullable<Guid>)null);
            mockSourceShardGroup1EventReader.GetNextEventAfter(event3Id).Returns(event4Id);
            mockSourceShardGroup1EventReader.GetEvents(event4Id, Int32.MinValue, Int32.MaxValue, false, 1).Returns(new List<TemporalEventBufferItemBase> { event4 });
            mockSourceShardGroup1EventReader.GetNextEventAfter(event4Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            testShardGroupMerger.MergeEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                mockSourceShardGroup2WriterAdministrator,
                1,
                testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                testSourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            mockSourceShardGroup1EventReader.Received(1).GetInitialEvent();
            mockSourceShardGroup2EventReader.Received(1).GetInitialEvent();
            mockSourceShardGroup1EventReader.Received(2).GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup2EventReader.Received(2).GetNextEventAfter(event1Id);
            mockSourceShardGroup1WriterAdministrator.Received(1).GetEventProcessingCount();
            mockSourceShardGroup2WriterAdministrator.Received(1).GetEventProcessingCount();
            mockSourceShardGroup1WriterAdministrator.Received(1).FlushEventBuffers();
            mockSourceShardGroup2WriterAdministrator.Received(1).FlushEventBuffers();
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event2Id);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event4Id, Int32.MinValue, Int32.MaxValue, false, 1);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event4Id);
            mockTargetShardGroupEventPersister.Received(4).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            Assert.AreEqual(4, capturedPersistedEventLists.Count);
            Assert.AreEqual(1, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreEqual(1, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event2, capturedPersistedEventLists[1][0]);
            Assert.AreEqual(1, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[2][0]);
            Assert.AreEqual(1, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event4, capturedPersistedEventLists[3][0]);
            mockMetricLogger.Received(1).Add(Arg.Any<DuplicatePrimaryAddElementEventsFiltered>(), 0);
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Merging events from source shard groups to target shard group...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Starting initial event batch merge...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed initial event batch merge.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Pausing operations in the source and target shard groups.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Source writer nodes event processing to complete.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Flushing source writer nodes event buffers...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed flushing source writer nodes event buffers.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Starting final event batch merge...");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed final event batch merge.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Completed merging events from source shard groups to target shard group.");
        }

        [Test]
        public void MergeEventsToTargetShardGroup_DuplicatePrimaryAddElementEventsMetricLogged()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            mockSourceShardGroup1EventReader.GetInitialEvent().Returns<Guid>(event1Id);
            mockSourceShardGroup2EventReader.GetInitialEvent().Returns<Guid>(event1Id);
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 100).Returns(new List<TemporalEventBufferItemBase> { event1, event2, event3, event4 });
            mockSourceShardGroup2EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 100).Returns(new List<TemporalEventBufferItemBase> { event1, event2, event3, event4 });
            mockSourceShardGroup1EventReader.GetNextEventAfter(event4Id).Returns((Nullable<Guid>)null);
            mockSourceShardGroup2EventReader.GetNextEventAfter(event3Id).Returns(event4Id);
            mockSourceShardGroup2EventReader.GetEvents(event4Id, Int32.MinValue, Int32.MaxValue, false, 100).Returns(new List<TemporalEventBufferItemBase> { event4 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event4Id).Returns((Nullable<Guid>)null); 
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            testShardGroupMerger.MergeEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                mockSourceShardGroup2WriterAdministrator,
                100,
                testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                testSourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            Assert.AreEqual(1, capturedPersistedEventLists.Count);
            Assert.AreEqual(4, capturedPersistedEventLists[0].Count);
            mockMetricLogger.Received(1).Add(Arg.Any<DuplicatePrimaryAddElementEventsFiltered>(), 4);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_SourceShardGroupFirstEventIdParametersNull()
        {
            Int32 nextBatchNumber = 1;

            var e = Assert.Throws<ArgumentException>(delegate
            {
                Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
                (
                    mockSourceShardGroup1EventReader,
                    mockSourceShardGroup2EventReader,
                    mockTargetShardGroupEventPersister,
                    new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                    ref nextBatchNumber,
                    null,
                    null,
                    2,
                    NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
                );
            });

            Assert.That(e.Message, Does.StartWith($"One of parameters 'sourceShardGroup1FirstEventId' or 'sourceShardGroup2FirstEventId' must contain a value."));
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_SourceShardGroup1FirstEventIdParameterNull()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            Int32 nextBatchNumber = 1;
            // Mock initial event reads
            mockSourceShardGroup2EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event2  });
            // Source 2 has another additional event read which is made during the persisting of all remaing events
            mockSourceShardGroup2EventReader.GetNextEventAfter(event2Id).Returns(event3Id);
            mockSourceShardGroup2EventReader.GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event3 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event3Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                null,
                event1Id,
                2,
                NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
            );

            Assert.IsNull(result.Item1);
            Assert.AreEqual(event3Id, result.Item2);
            Assert.AreEqual(3, nextBatchNumber);
            mockSourceShardGroup1EventReader.DidNotReceive().GetEvents(Arg.Any<Guid>(), Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event2Id);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event3Id);
            mockTargetShardGroupEventPersister.Received(2).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(2).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(2).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 1 event(s) from second source shard group.");
            Assert.AreEqual(2, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreSame(event2, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(1, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[1][0]);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_SourceShardGroup2FirstEventIdParameterNull()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            Int32 nextBatchNumber = 2;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event2 });
            // Source 2 has another additional event read which is made during the persisting of all remaing events
            mockSourceShardGroup1EventReader.GetNextEventAfter(event2Id).Returns(event3Id);
            mockSourceShardGroup1EventReader.GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event3 });
            mockSourceShardGroup1EventReader.GetNextEventAfter(event3Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event1Id,
                null,
                2,
                NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
            );

            Assert.AreEqual(event3Id, result.Item1);
            Assert.IsNull(result.Item2);
            Assert.AreEqual(4, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.DidNotReceive().GetEvents(Arg.Any<Guid>(), Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event2Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event3Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event3Id);
            mockTargetShardGroupEventPersister.Received(2).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(2).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(2).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 1 event(s) from first source shard group.");
            Assert.AreEqual(2, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreSame(event2, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(1, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[1][0]);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_SourceShardGroup1EmptiesFirst_AllEventsPersistedFromSourceShardGroup2()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            var event5Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var event5 = GenerateAddUserEvent(event5Id, "user5", "2025-05-25 16:05");
            var event6Id = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var event6 = GenerateAddUserEvent(event6Id, "user6", "2025-05-25 16:06");
            var event7Id = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var event7 = GenerateAddUserEvent(event7Id, "user7", "2025-05-25 16:07");
            var event8Id = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var event8 = GenerateAddUserEvent(event8Id, "user8", "2025-05-25 16:08");
            var event10Id = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var event10 = GenerateAddUserEvent(event10Id, "user10", "2025-05-25 16:10");
            var event12Id = Guid.Parse("00000000-0000-0000-0000-000000000012");
            var event12 = GenerateAddUserEvent(event12Id, "user12", "2025-05-25 16:12");
            Int32 nextBatchNumber = 1;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event3 } );
            mockSourceShardGroup2EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event2, event4 } );
            // Mock 1 additional event read in each source
            mockSourceShardGroup1EventReader.GetNextEventAfter(event3Id).Returns(event5Id);
            mockSourceShardGroup1EventReader.GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event5, event7 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event4Id).Returns(event6Id);
            mockSourceShardGroup2EventReader.GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event6, event8 });
            // Source 1 only had a single additional event read (hence below call returns null)
            mockSourceShardGroup1EventReader.GetNextEventAfter(event7Id).Returns((Nullable<Guid>)null);
            // Source 2 has another additional event read which is made during the persisting of all remaing events
            mockSourceShardGroup2EventReader.GetNextEventAfter(event8Id).Returns(event10Id);
            mockSourceShardGroup2EventReader.GetEvents(event10Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event10, event12 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event12Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader, 
                mockSourceShardGroup2EventReader, 
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event1Id,
                event2Id, 
                2, 
                NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
            );

            Assert.AreEqual(event7Id, result.Item1);
            Assert.AreEqual(event12Id, result.Item2);
            Assert.AreEqual(6, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event4Id);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event7Id);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event8Id);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event10Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event12Id);
            mockTargetShardGroupEventPersister.Received(5).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(5).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(5).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(2).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(3).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            Assert.AreEqual(5, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreSame(event2, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[1][0]);
            Assert.AreSame(event4, capturedPersistedEventLists[1][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event5, capturedPersistedEventLists[2][0]);
            Assert.AreSame(event6, capturedPersistedEventLists[2][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[3].Count);
            Assert.AreSame(event7, capturedPersistedEventLists[3][0]);
            Assert.AreSame(event8, capturedPersistedEventLists[3][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[4].Count);
            Assert.AreSame(event10, capturedPersistedEventLists[4][0]);
            Assert.AreSame(event12, capturedPersistedEventLists[4][1]);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_SourceShardGroup2EmptiesFirst_AllEventsPersistedFromSourceShardGroup1()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            var event5Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var event5 = GenerateAddUserEvent(event5Id, "user5", "2025-05-25 16:05");
            var event6Id = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var event6 = GenerateAddUserEvent(event6Id, "user6", "2025-05-25 16:06");
            var event7Id = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var event7 = GenerateAddUserEvent(event7Id, "user7", "2025-05-25 16:07");
            var event8Id = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var event8 = GenerateAddUserEvent(event8Id, "user8", "2025-05-25 16:08");
            var event10Id = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var event10 = GenerateAddUserEvent(event10Id, "user10", "2025-05-25 16:10");
            var event12Id = Guid.Parse("00000000-0000-0000-0000-000000000012");
            var event12 = GenerateAddUserEvent(event12Id, "user12", "2025-05-25 16:12");
            Int32 nextBatchNumber = 10;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event2, event4 });
            mockSourceShardGroup2EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event3 });
            // Mock 1 additional event read in each source
            mockSourceShardGroup1EventReader.GetNextEventAfter(event4Id).Returns(event6Id);
            mockSourceShardGroup1EventReader.GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event6, event8 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event3Id).Returns(event5Id);
            mockSourceShardGroup2EventReader.GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event5, event7 });
            // Source 2 only had a single additional event read (hence below call returns null)
            mockSourceShardGroup2EventReader.GetNextEventAfter(event7Id).Returns((Nullable<Guid>)null);
            // Source 2 has another additional event read which is made during the persisting of all remaing events
            mockSourceShardGroup1EventReader.GetNextEventAfter(event8Id).Returns(event10Id);
            mockSourceShardGroup1EventReader.GetEvents(event10Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event10 });
            mockSourceShardGroup1EventReader.GetNextEventAfter(event10Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event2Id,
                event1Id,
                2,
                NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
            );

            Assert.AreEqual(event10Id, result.Item1);
            Assert.AreEqual(event7Id, result.Item2);
            Assert.AreEqual(15, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event4Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event7Id);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event8Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event10Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event10Id);
            mockTargetShardGroupEventPersister.Received(5).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(5).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(5).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(2).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 1 event(s) from first source shard group.");
            mockApplicationLogger.Received(2).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            Assert.AreEqual(5, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreSame(event2, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[1][0]);
            Assert.AreSame(event4, capturedPersistedEventLists[1][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event5, capturedPersistedEventLists[2][0]);
            Assert.AreSame(event6, capturedPersistedEventLists[2][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[3].Count);
            Assert.AreSame(event7, capturedPersistedEventLists[3][0]);
            Assert.AreSame(event8, capturedPersistedEventLists[3][1]);
            Assert.AreEqual(1, capturedPersistedEventLists[4].Count);
            Assert.AreSame(event10, capturedPersistedEventLists[4][0]);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_SourceShardGroup1EmptiesFirst_MergingStopped()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            var event5Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var event5 = GenerateAddUserEvent(event5Id, "user5", "2025-05-25 16:05");
            var event6Id = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var event6 = GenerateAddUserEvent(event6Id, "user6", "2025-05-25 16:06");
            var event7Id = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var event7 = GenerateAddUserEvent(event7Id, "user7", "2025-05-25 16:07");
            var event8Id = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var event8 = GenerateAddUserEvent(event8Id, "user8", "2025-05-25 16:08");
            Int32 nextBatchNumber = 1;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event3 });
            mockSourceShardGroup2EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event2, event4 });
            // Mock 1 additional event read in each source
            mockSourceShardGroup1EventReader.GetNextEventAfter(event3Id).Returns(event5Id);
            mockSourceShardGroup1EventReader.GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event5, event7 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event4Id).Returns(event6Id);
            mockSourceShardGroup2EventReader.GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event6, event8 });
            // Source 1 only had a single additional event read (hence below call returns null)
            mockSourceShardGroup1EventReader.GetNextEventAfter(event7Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event1Id,
                event2Id,
                2,
                NoEventsReadDuringMergeAction.StopMerging
            );

            Assert.AreEqual(event7Id, result.Item1);
            Assert.AreEqual(event6Id, result.Item2);
            Assert.AreEqual(5, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event4Id);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event7Id);
            mockTargetShardGroupEventPersister.Received(4).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(4).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(4).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(2).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(2).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            Assert.AreEqual(4, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreSame(event2, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[1][0]);
            Assert.AreSame(event4, capturedPersistedEventLists[1][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event5, capturedPersistedEventLists[2][0]);
            Assert.AreSame(event6, capturedPersistedEventLists[2][1]);
            Assert.AreEqual(1, capturedPersistedEventLists[3].Count);
            Assert.AreSame(event7, capturedPersistedEventLists[3][0]);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_SourceShardGroup2EmptiesFirst_MergingStopped()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            var event5Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var event5 = GenerateAddUserEvent(event5Id, "user5", "2025-05-25 16:05");
            var event6Id = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var event6 = GenerateAddUserEvent(event6Id, "user6", "2025-05-25 16:06");
            var event7Id = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var event7 = GenerateAddUserEvent(event7Id, "user7", "2025-05-25 16:07");
            var event8Id = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var event8 = GenerateAddUserEvent(event8Id, "user8", "2025-05-25 16:08");
            Int32 nextBatchNumber = 1;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event2, event4 });
            mockSourceShardGroup2EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event3 });
            // Mock 1 additional event read in each source
            mockSourceShardGroup1EventReader.GetNextEventAfter(event4Id).Returns(event6Id);
            mockSourceShardGroup1EventReader.GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event6, event8 });
            mockSourceShardGroup2EventReader.GetNextEventAfter(event3Id).Returns(event5Id);
            mockSourceShardGroup2EventReader.GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event5, event7 });
            // Source 2 only had a single additional event read (hence below call returns null)
            mockSourceShardGroup2EventReader.GetNextEventAfter(event7Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event2Id,
                event1Id,
                2,
                NoEventsReadDuringMergeAction.StopMerging
            );

            Assert.AreEqual(event6Id, result.Item1);
            Assert.AreEqual(event7Id, result.Item2);
            Assert.AreEqual(5, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event4Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event6Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event5Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event7Id);
            mockTargetShardGroupEventPersister.Received(4).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(4).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(4).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(2).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(2).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            Assert.AreEqual(4, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreSame(event2, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[1][0]);
            Assert.AreSame(event4, capturedPersistedEventLists[1][1]);
            Assert.AreEqual(2, capturedPersistedEventLists[2].Count);
            Assert.AreSame(event5, capturedPersistedEventLists[2][0]);
            Assert.AreSame(event6, capturedPersistedEventLists[2][1]);
            Assert.AreEqual(1, capturedPersistedEventLists[3].Count);
            Assert.AreSame(event7, capturedPersistedEventLists[3][0]);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_PrimaryElementEventsFiltered()
        {
            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            // Event 2 is adding the same user as event1 and hence should be filtered
            var event2 = GenerateAddUserEvent(event2Id, "user1", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            Int32 nextBatchNumber = 1;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event3 });
            mockSourceShardGroup2EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event2, event4 });
            mockSourceShardGroup1EventReader.GetNextEventAfter(event3Id).Returns((Nullable<Guid>)null);
            mockSourceShardGroup2EventReader.GetNextEventAfter(event4Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event1Id,
                event2Id,
                2,
                NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
            );

            Assert.AreEqual(event3Id, result.Item1);
            Assert.AreEqual(event4Id, result.Item2);
            Assert.AreEqual(3, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event4Id);
            mockTargetShardGroupEventPersister.Received(2).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(2).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(2).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            Assert.AreEqual(2, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            // Note event2 has not been persisted
            Assert.AreSame(event3, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(1, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event4, capturedPersistedEventLists[1][0]);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_CorrectEventIdsReturnedWhenFiltering_AllEventsPersistedFromSourceShardGroup2()
        {
            // If an event which was filtered was the last to be buffered from one source at the point the one of the sources becomes empty, need to make sure that the
            //   id of the filtered event is returned by MergeEventBatchesToTargetShardGroup(), not the id of the last persisted event.  If the id of the last persisted
            //   event is returned, the filtered event will be re-read and double processed on subsequent calls to MergeEventBatchesToTargetShardGroup().

            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var event2 = GenerateAddUserEvent(event2Id, "user2", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            // Event 4 is adding the same user as event1 and hence should be filtered
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user1", "2025-05-25 16:04");
            Int32 nextBatchNumber = 1;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event3 });
            mockSourceShardGroup2EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event2, event4 });
            mockSourceShardGroup1EventReader.GetNextEventAfter(event3Id).Returns((Nullable<Guid>)null);
            mockSourceShardGroup2EventReader.GetNextEventAfter(event4Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event1Id,
                event2Id,
                2,
                NoEventsReadDuringMergeAction.PersistAllEventsFromOtherSource
            );

            Assert.AreEqual(event3Id, result.Item1);
            Assert.AreEqual(event4Id, result.Item2);
            Assert.AreEqual(3, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup2EventReader.Received(1).GetNextEventAfter(event4Id);
            mockTargetShardGroupEventPersister.Received(2).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(2).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(2).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            Assert.AreEqual(2, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            Assert.AreSame(event2, capturedPersistedEventLists[0][1]);
            Assert.AreEqual(1, capturedPersistedEventLists[1].Count);
            Assert.AreSame(event3, capturedPersistedEventLists[1][0]);
            // Note event4 has not been persisted
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_CorrectEventIdsReturnedWhenFiltering_MergingStopped()
        {
            // Similar scenario to test MergeEventBatchesToTargetShardGroup_CorrectEventIdsReturnedWhenFiltering_AllEventsPersistedFromSourceShardGroup2() but
            //   'noEventsReadDuringMergeAction' parameter set to 'StopMerging'

            var event1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var event1 = GenerateAddUserEvent(event1Id, "user1", "2025-05-25 16:01");
            var event2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            // Event 2 is adding the same user as event1 and hence should be filtered
            var event2 = GenerateAddUserEvent(event2Id, "user1", "2025-05-25 16:02");
            var event3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var event3 = GenerateAddUserEvent(event3Id, "user3", "2025-05-25 16:03");
            var event4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var event4 = GenerateAddUserEvent(event4Id, "user4", "2025-05-25 16:04");
            Int32 nextBatchNumber = 1;
            // Mock initial event reads
            mockSourceShardGroup1EventReader.GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event1, event3 });
            mockSourceShardGroup2EventReader.GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2).Returns(new List<TemporalEventBufferItemBase> { event2, event4 });
            mockSourceShardGroup1EventReader.GetNextEventAfter(event3Id).Returns((Nullable<Guid>)null);
            var capturedPersistedEventLists = new List<IList<TemporalEventBufferItemBase>>();
            mockTargetShardGroupEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>
            (
                (argumentValue) =>
                {
                    capturedPersistedEventLists.Add(argumentValue);
                }
            ));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testShardGroupMerger.MergeEventBatchesToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                new PrimaryElementEventDuplicateFilter<String, String>(false, mockApplicationLogger, mockMetricLogger),
                ref nextBatchNumber,
                event1Id,
                event2Id,
                2,
                NoEventsReadDuringMergeAction.StopMerging
            );

            Assert.AreEqual(event3Id, result.Item1);
            Assert.AreEqual(event2Id, result.Item2);
            Assert.AreEqual(2, nextBatchNumber);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(event1Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup2EventReader.Received(1).GetEvents(event2Id, Int32.MinValue, Int32.MaxValue, false, 2);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(event3Id);
            mockSourceShardGroup2EventReader.DidNotReceive().GetNextEventAfter(Arg.Any<Guid>()); 
            mockTargetShardGroupEventPersister.Received(1).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(2).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(2).End(Arg.Any<Guid>(), Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from first source shard group.");
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, "Read 2 event(s) from second source shard group.");
            Assert.AreEqual(1, capturedPersistedEventLists.Count);
            Assert.AreEqual(2, capturedPersistedEventLists[0].Count);
            Assert.AreSame(event1, capturedPersistedEventLists[0][0]);
            // Note event2 has not been persisted
            Assert.AreSame(event3, capturedPersistedEventLists[0][1]);
        }

        [Test]
        public void ReadSourceShardGroupEventsIntoQueue_DestinationEventQueueParameterNotEmpty()
        {
            var initialEventId = Guid.NewGuid();
            var destinationEventQueue = new Queue<TemporalEventBufferItemBase>();
            destinationEventQueue.Enqueue
            (
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "testUser", DateTime.UtcNow, 0)
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    mockSourceShardGroup1EventReader,
                    destinationEventQueue,
                    100,
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith($"The queue in parameter 'destinationEventQueue' is not empty."));
            Assert.AreEqual("destinationEventQueue", e.ParamName);
        }

        [Test]
        public void ReadSourceShardGroupEventsIntoQueue_ExceptionReadingEvents()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var initialEventId = Guid.NewGuid();
            var destinationEventQueue = new Queue<TemporalEventBufferItemBase>();
            Int32 eventCount = 100;
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockSourceShardGroup1EventReader.When(reader => reader.GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    mockSourceShardGroup1EventReader,
                    destinationEventQueue,
                    eventCount,
                    true
                );
            });

            mockSourceShardGroup1EventReader.Received(1).GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EventBatchReadTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve event batch from first source shard group beginning with event with id '{initialEventId}'."));
            Assert.AreSame(mockException, e.InnerException);


            mockMetricLogger.ClearReceivedCalls();
            mockSourceShardGroup1EventReader.ClearReceivedCalls();

            e = Assert.Throws<Exception>(delegate
            {
                testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    mockSourceShardGroup1EventReader,
                    destinationEventQueue,
                    eventCount,
                    false
                );
            });

            mockSourceShardGroup1EventReader.Received(1).GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EventBatchReadTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve event batch from second source shard group beginning with event with id '{initialEventId}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void ReadSourceShardGroupEventsIntoQueue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var initialEventId = Guid.NewGuid();
            var event1Id = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            var event2Id = Guid.Parse("3c11c947-7fe7-4264-bfff-144272496d90");
            var event3Id = Guid.Parse("2fb989e6-54d5-48c3-a3bf-1635c14bade4");
            var returnEvents = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(event1Id, "user1"),
                GenerateAddUserEvent(event2Id, "user2"),
                GenerateAddUserEvent(event3Id, "user3")
            };
            var destinationEventQueue = new Queue<TemporalEventBufferItemBase>();
            Int32 eventCount = 100;
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockSourceShardGroup1EventReader.GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount).Returns<IList<TemporalEventBufferItemBase>>(returnEvents);

            Nullable<Guid> result = testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
            (
                initialEventId,
                mockSourceShardGroup1EventReader,
                destinationEventQueue,
                eventCount,
                true
            );

            mockSourceShardGroup1EventReader.Received(1).GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, $"Read 3 event(s) from first source shard group.");
            Assert.AreEqual(event3Id, result.Value);
            Assert.AreEqual(3, destinationEventQueue.Count);
            var resultList = new List<TemporalEventBufferItemBase>();
            while (destinationEventQueue.Count > 0)
            {
                resultList.Add(destinationEventQueue.Dequeue());
            }
            Assert.AreSame(returnEvents[0], resultList[0]);
            Assert.AreSame(returnEvents[1], resultList[1]);
            Assert.AreSame(returnEvents[2], resultList[2]);
        }

        [Test]
        public void ReadSourceShardGroupEventsIntoQueue_0EventsRead()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var initialEventId = Guid.NewGuid();
            var returnEvents = new List<TemporalEventBufferItemBase>();
            var destinationEventQueue = new Queue<TemporalEventBufferItemBase>();
            Int32 eventCount = 100;
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockSourceShardGroup1EventReader.GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount).Returns<IList<TemporalEventBufferItemBase>>(returnEvents);

            Nullable<Guid> result = testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
            (
                initialEventId,
                mockSourceShardGroup1EventReader,
                destinationEventQueue,
                eventCount,
                false
            );

            mockSourceShardGroup1EventReader.Received(1).GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventBatchReadTime>());
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, $"Read 0 event(s) from second source shard group.");
            Assert.IsFalse(result.HasValue);
            Assert.AreEqual(0, destinationEventQueue.Count);
            var resultList = new List<TemporalEventBufferItemBase>();
        }

        [Test]
        public void BufferAllRemainingEvents_0EventsQueued()
        {
            var sourceShardGroupEventQueue = new Queue<TemporalEventBufferItemBase>();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testShardGroupMerger.BufferAllRemainingEvents<String, String, String, String>
                (
                    mockSourceShardGroup1EventReader,
                    sourceShardGroupEventQueue,
                    mockTargetShardGroupEventPersisterBuffer,
                    3,
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith($"The queue in parameter 'sourceShardGroupEventQueue' cannot be empty."));
            Assert.AreEqual("sourceShardGroupEventQueue", e.ParamName);
        }

        [Test]
        public void BufferAllRemainingEvents()
        {
            var sourceShardGroupEventQueue = new Queue<TemporalEventBufferItemBase>();
            var userEvent1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var userEvent2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var userEvent3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var userEvent4Id = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var userEvent1 = GenerateAddUserEvent(userEvent1Id, "user1");
            var userEvent2 = GenerateAddUserEvent(userEvent2Id, "user2");
            var userEvent3 = GenerateAddUserEvent(userEvent3Id, "user3");
            var userEvent4 = GenerateAddUserEvent(userEvent4Id, "user4");
            sourceShardGroupEventQueue.Enqueue(userEvent1);
            sourceShardGroupEventQueue.Enqueue(userEvent2);
            mockSourceShardGroup1EventReader.GetNextEventAfter(userEvent2Id).Returns(userEvent3Id);
            mockSourceShardGroup1EventReader.GetNextEventAfter(userEvent4Id).Returns((Nullable<Guid>)null);
            mockSourceShardGroup1EventReader.GetEvents(userEvent3Id, Int32.MinValue, Int32.MaxValue, false, 3).Returns(new List<TemporalEventBufferItemBase>()
            { 
                userEvent3,
                userEvent4
            });

            Nullable<Guid> result = testShardGroupMerger.BufferAllRemainingEvents<String, String, String, String>
            (
                mockSourceShardGroup1EventReader,
                sourceShardGroupEventQueue,
                mockTargetShardGroupEventPersisterBuffer,
                3,
                true
            );

            Assert.AreEqual(userEvent4.EventId, result);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(userEvent2Id);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(userEvent4Id);
            mockSourceShardGroup1EventReader.Received(1).GetEvents(userEvent3Id, Int32.MinValue, Int32.MaxValue, false, 3);
            mockTargetShardGroupEventPersisterBuffer.Received(1).BufferEvent(userEvent1, true);
            mockTargetShardGroupEventPersisterBuffer.Received(1).BufferEvent(userEvent2, true);
            mockTargetShardGroupEventPersisterBuffer.Received(1).BufferEvent(userEvent3, true);
            mockTargetShardGroupEventPersisterBuffer.Received(1).BufferEvent(userEvent4, true);
        }

        [Test]
        public void BufferAllRemainingEvents_0EventsRead()
        {
            var sourceShardGroupEventQueue = new Queue<TemporalEventBufferItemBase>();
            var userEvent1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var userEvent2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var userEvent1 = GenerateAddUserEvent(userEvent1Id, "user1");
            var userEvent2 = GenerateAddUserEvent(userEvent2Id, "user2");
            sourceShardGroupEventQueue.Enqueue(userEvent1);
            sourceShardGroupEventQueue.Enqueue(userEvent2);
            mockSourceShardGroup1EventReader.GetNextEventAfter(userEvent2Id).Returns((Nullable<Guid>)null);

            Nullable<Guid> result = testShardGroupMerger.BufferAllRemainingEvents<String, String, String, String>
            (
                mockSourceShardGroup1EventReader,
                sourceShardGroupEventQueue,
                mockTargetShardGroupEventPersisterBuffer,
                3,
                true
            );

            Assert.AreEqual(userEvent2.EventId, result);
            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(userEvent2Id);
            mockTargetShardGroupEventPersisterBuffer.Received(1).BufferEvent(userEvent1, true);
            mockTargetShardGroupEventPersisterBuffer.Received(1).BufferEvent(userEvent2, true);
        }

        [Test]
        public void GetNextEventIdAfter_ExceptionReadingFromEventReader()
        {
            var mockException = new Exception("Mock exception");
            Guid inputEventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockSourceShardGroup1EventReader.When(reader => reader.GetNextEventAfter(inputEventId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupMerger.GetNextEventIdAfter(inputEventId, mockSourceShardGroup1EventReader);
            });

            mockSourceShardGroup1EventReader.Received(1).GetNextEventAfter(inputEventId);
            Assert.That(e.Message, Does.StartWith($"Failed to read event following event with id '5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32' from event reader."));
            Assert.AreSame(mockException, e.InnerException);

        }

        #region Private/Protected Methods

        /// <summary>
        /// Generates an 'add user' event.
        /// </summary>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="user">The user.</param>
        /// <returns>An 'add user' event.</returns>
        protected UserEventBufferItem<String> GenerateAddUserEvent(Guid eventId, String user)
        {
            return new UserEventBufferItem<String>(eventId, EventAction.Add, user, DateTime.UtcNow, user.GetHashCode());
        }

        /// <summary>
        /// Generates an 'add user' event.
        /// </summary>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="user">The user.</param>
        /// <param name="occurredTimeString">The time the event occured in 'yyyy-MM-dd HH:mm' format.</param>
        /// <returns>An 'add user' event.</returns>
        protected UserEventBufferItem<String> GenerateAddUserEvent(Guid eventId, String user, String occurredTimeString)
        {
            DateTime occurredTime = DateTime.ParseExact($"{occurredTimeString}:00.0000000", "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            occurredTime = DateTime.SpecifyKind(occurredTime, DateTimeKind.Utc);

            return new UserEventBufferItem<String>(eventId, EventAction.Add, user, occurredTime, user.GetHashCode());
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the DistributedAccessManagerShardGroupMerger class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        private class DistributedAccessManagerShardGroupMergerWithProtectedMembers : DistributedAccessManagerShardGroupMerger
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.UnitTests.DistributedAccessManagerShardGroupMergerTests+DistributedAccessManagerShardGroupMergerWithProtectedMembers class.
            /// </summary>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public DistributedAccessManagerShardGroupMergerWithProtectedMembers(IApplicationLogger logger, IMetricLogger metricLogger)
                : base(logger, metricLogger)
            {
            }

            public new Tuple<Nullable<Guid>, Nullable<Guid>> MergeEventBatchesToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
            (
                IAccessManagerTemporalEventBatchReader sourceShardGroup1EventReader,
                IAccessManagerTemporalEventBatchReader sourceShardGroup2EventReader,
                IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
                PrimaryElementEventDuplicateFilter<TUser, TGroup> eventFilter,
                ref Int32 currentBatchNumber,
                Nullable<Guid> sourceShardGroup1FirstEventId,
                Nullable<Guid> sourceShardGroup2FirstEventId,
                Int32 eventBatchSize,
                NoEventsReadDuringMergeAction noEventsReadDuringMergeAction
            )
            {
                return base.MergeEventBatchesToTargetShardGroup
                (
                    sourceShardGroup1EventReader,
                    sourceShardGroup2EventReader,
                    targetShardGroupEventPersister,
                    eventFilter,
                    ref currentBatchNumber,
                    sourceShardGroup1FirstEventId,
                    sourceShardGroup2FirstEventId,
                    eventBatchSize, 
                    noEventsReadDuringMergeAction
                );
            }

            public new Nullable<Guid> ReadSourceShardGroupEventsIntoQueue
            (
                Guid initialEventId,
                IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
                Queue<TemporalEventBufferItemBase> destinationEventQueue,
                Int32 eventCount,
                Boolean readerIsFirstEventReader
            )
            {
                return base.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    sourceShardGroupEventReader,
                    destinationEventQueue,
                    eventCount,
                    readerIsFirstEventReader
                );
            }

            public new Nullable<Guid> BufferAllRemainingEvents<TUser, TGroup, TComponent, TAccess>
            (
                IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
                Queue<TemporalEventBufferItemBase> sourceShardGroupEventQueue,
                IEventPersisterBuffer targetShardGroupEventPersisterBuffer,
                Int32 eventBatchSize,
                Boolean sourceShardGroupIsFirst
            )
            {
                return base.BufferAllRemainingEvents<TUser, TGroup, TComponent, TAccess>
                (
                    sourceShardGroupEventReader,
                    sourceShardGroupEventQueue,
                    targetShardGroupEventPersisterBuffer,
                    eventBatchSize,
                    sourceShardGroupIsFirst
                );
            }

            public new Nullable<Guid> GetNextEventIdAfter(Guid inputEventId, IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader)
            {
                return base.GetNextEventIdAfter(inputEventId, sourceShardGroupEventReader);
            }
        }

        #endregion
    }
}
