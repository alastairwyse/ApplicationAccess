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
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Redistribution;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Redistribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupSplitter class.
    /// </summary>
    public class DistributedAccessManagerShardGroupSplitterTests
    {
        private Int32 testHashRangeStart;
        private Int32 testHashRangeEnd;
        private Boolean testFilterGroupEventsByHashRange;
        private Int32 testEventBatchSize;
        private Int32 testSourceWriterNodeOperationsCompleteCheckRetryAttempts;
        private Int32 testSourceWriterNodeOperationsCompleteCheckRetryInterval;
        private IHashCodeGenerator<String> hashCodeGenerator;
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroupEventReader;
        private IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String> mockTargetShardGroupEventPersister;
        private IAccessManagerTemporalEventDeleter mockSourceShardGroupEventDeleter;
        private IDistributedAccessManagerOperationRouter mockOperationRouter;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private IDistributedAccessManagerWriterAdministrator mockSourceShardGroupWriterAdministrator;
        private DistributedAccessManagerShardGroupSplitterWithProtectedMembers testShardGroupSplitter;

        [SetUp]
        protected void SetUp()
        {
            testHashRangeStart = 0;
            testHashRangeEnd = 1_073_741_824;
            testFilterGroupEventsByHashRange = false;
            testEventBatchSize = 2;
            testSourceWriterNodeOperationsCompleteCheckRetryAttempts = 3;
            testSourceWriterNodeOperationsCompleteCheckRetryInterval = 50;
            hashCodeGenerator = new DefaultStringHashCodeGenerator();
            mockSourceShardGroupEventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockTargetShardGroupEventPersister = Substitute.For<IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String>>();
            mockSourceShardGroupEventDeleter = Substitute.For<IAccessManagerTemporalEventDeleter>();
            mockOperationRouter = Substitute.For<IDistributedAccessManagerOperationRouter>();
            mockSourceShardGroupWriterAdministrator = Substitute.For<IDistributedAccessManagerWriterAdministrator>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardGroupSplitter = new DistributedAccessManagerShardGroupSplitterWithProtectedMembers(mockApplicationLogger, mockMetricLogger);
        }

        [Test]
        public void CopyEventsToTargetShardGroup_SourceWriterNodeOperationsCompleteCheckRetryAttemptsParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testShardGroupSplitter.CopyEventsToTargetShardGroup
                (
                    mockSourceShardGroupEventReader, 
                    mockTargetShardGroupEventPersister, 
                    mockOperationRouter, 
                    mockSourceShardGroupWriterAdministrator,
                    testHashRangeStart,
                    testHashRangeEnd,
                    testFilterGroupEventsByHashRange,
                    testEventBatchSize, 
                    -1,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceWriterNodeOperationsCompleteCheckRetryAttempts' with value -1 must be greater than or equal to 0."));
            Assert.AreEqual("sourceWriterNodeOperationsCompleteCheckRetryAttempts", e.ParamName);
        }

        [Test]
        public void CopyEventsToTargetShardGroup_SourceWriterNodeOperationsCompleteCheckRetryIntervalParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testShardGroupSplitter.CopyEventsToTargetShardGroup
                (
                    mockSourceShardGroupEventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroupWriterAdministrator,
                    testHashRangeStart,
                    testHashRangeEnd,
                    testFilterGroupEventsByHashRange,
                    testEventBatchSize,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    -1
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceWriterNodeOperationsCompleteCheckRetryInterval' with value -1 must be greater than or equal to 0."));
            Assert.AreEqual("sourceWriterNodeOperationsCompleteCheckRetryInterval", e.ParamName);
        }

        [Test]
        public void CopyEventsToTargetShardGroup_EventBatchSizeParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testShardGroupSplitter.CopyEventsToTargetShardGroup
                (
                    mockSourceShardGroupEventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroupWriterAdministrator,
                    testHashRangeStart,
                    testHashRangeEnd,
                    testFilterGroupEventsByHashRange,
                    0,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'eventBatchSize' with value 0 must be greater than 0."));
            Assert.AreEqual("eventBatchSize", e.ParamName);
        }

        [Test]
        public void CopyEventsToTargetShardGroup_NoEventsExistInSourceShardGroup()
        {
            var mockException = new Exception("Mock exception");
            mockSourceShardGroupEventReader.When(reader => reader.GetInitialEvent()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.CopyEventsToTargetShardGroup
                (
                    mockSourceShardGroupEventReader,
                    mockTargetShardGroupEventPersister,
                    mockOperationRouter,
                    mockSourceShardGroupWriterAdministrator,
                    testHashRangeStart,
                    testHashRangeEnd,
                    testFilterGroupEventsByHashRange,
                    testEventBatchSize,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve initial event id from the source shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CopyEventsToTargetShardGroup()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var batch1FirstEventId = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            var batch2FirstEventId = Guid.Parse("3c11c947-7fe7-4264-bfff-144272496d90");
            var batch3FirstEventId = Guid.Parse("2fb989e6-54d5-48c3-a3bf-1635c14bade4");
            var batch4FirstEventId = Guid.Parse("72b06d87-076c-430e-b17a-1706bea6c33c");
            var eventBatch1 = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(batch1FirstEventId, "user1"),
                GenerateAddUserEvent(Guid.NewGuid(), "user2")
            };
            var eventBatch2 = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(batch2FirstEventId, "user3"),
                GenerateAddUserEvent(Guid.NewGuid(), "user4")
            };
            var eventBatch3 = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(batch3FirstEventId, "user5"),
                GenerateAddUserEvent(Guid.NewGuid(), "user6")
            };
            var eventBatch4 = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(batch4FirstEventId, "user7"),
                GenerateAddUserEvent(Guid.NewGuid(), "user8")
            };
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(testBeginId);
            mockSourceShardGroupEventReader.GetInitialEvent().Returns<Guid>(batch1FirstEventId);
            mockSourceShardGroupEventReader.GetEvents(batch1FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch1);
            mockSourceShardGroupEventReader.GetNextEventAfter(eventBatch1[1].EventId).Returns(batch2FirstEventId);
            mockSourceShardGroupEventReader.GetEvents(batch2FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch2);
            mockSourceShardGroupEventReader.GetNextEventAfter(eventBatch2[1].EventId).Returns(batch3FirstEventId);
            mockSourceShardGroupEventReader.GetEvents(batch3FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch3);
            mockSourceShardGroupEventReader.GetNextEventAfter(eventBatch3[1].EventId).Returns((Nullable<Guid>)null, batch4FirstEventId);
            mockSourceShardGroupEventReader.GetEvents(batch4FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch4);
            mockSourceShardGroupEventReader.GetNextEventAfter(eventBatch4[1].EventId).Returns((Nullable<Guid>)null);
            mockSourceShardGroupWriterAdministrator.GetEventProcessingCount().Returns(0);

            testShardGroupSplitter.CopyEventsToTargetShardGroup
            (
                mockSourceShardGroupEventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroupWriterAdministrator,
                testHashRangeStart,
                testHashRangeEnd,
                testFilterGroupEventsByHashRange,
                testEventBatchSize,
                testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                testSourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            mockSourceShardGroupEventReader.Received(1).GetInitialEvent();
            mockSourceShardGroupEventReader.Received(1).GetEvents(batch1FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.PersistEvents(eventBatch1);
            mockSourceShardGroupEventReader.Received(1).GetNextEventAfter(eventBatch1[1].EventId);
            mockSourceShardGroupEventReader.Received(1).GetEvents(batch2FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.PersistEvents(eventBatch2);
            mockSourceShardGroupEventReader.Received(1).GetNextEventAfter(eventBatch2[1].EventId);
            mockSourceShardGroupEventReader.Received(1).GetEvents(batch3FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.PersistEvents(eventBatch3);
            mockSourceShardGroupEventReader.Received(2).GetNextEventAfter(eventBatch3[1].EventId);
            mockSourceShardGroupEventReader.Received(1).GetEvents(batch4FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.PersistEvents(eventBatch4);
            mockSourceShardGroupEventReader.Received(1).GetNextEventAfter(eventBatch4[1].EventId);
            mockOperationRouter.Received(1).PauseOperations();
            mockSourceShardGroupWriterAdministrator.Received(1).GetEventProcessingCount();
            mockSourceShardGroupWriterAdministrator.Received(1).FlushEventBuffers();
            mockMetricLogger.Received(4).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(4).End(testBeginId, Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(4).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(4).End(testBeginId, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<WriterNodeEventProcessingCount>(), 0);
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EventProcessingCountCheckRetried>());
        }

        [Test]
        public void CopyEventsToTargetShardGroup_FinalBatchNotRequired()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var batch1FirstEventId = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            var batch2FirstEventId = Guid.Parse("3c11c947-7fe7-4264-bfff-144272496d90");
            var batch3FirstEventId = Guid.Parse("2fb989e6-54d5-48c3-a3bf-1635c14bade4");
            var eventBatch1 = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(batch1FirstEventId, "user1"),
                GenerateAddUserEvent(Guid.NewGuid(), "user2")
            };
            var eventBatch2 = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(batch2FirstEventId, "user3"),
                GenerateAddUserEvent(Guid.NewGuid(), "user4")
            };
            var eventBatch3 = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(batch3FirstEventId, "user5"),
                GenerateAddUserEvent(Guid.NewGuid(), "user6")
            };
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(testBeginId);
            mockSourceShardGroupEventReader.GetInitialEvent().Returns<Guid>(batch1FirstEventId);
            mockSourceShardGroupEventReader.GetEvents(batch1FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch1);
            mockSourceShardGroupEventReader.GetNextEventAfter(eventBatch1[1].EventId).Returns(batch2FirstEventId);
            mockSourceShardGroupEventReader.GetEvents(batch2FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch2);
            mockSourceShardGroupEventReader.GetNextEventAfter(eventBatch2[1].EventId).Returns(batch3FirstEventId);
            mockSourceShardGroupEventReader.GetEvents(batch3FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch3);
            mockSourceShardGroupEventReader.GetNextEventAfter(eventBatch3[1].EventId).Returns((Nullable<Guid>)null);
            mockSourceShardGroupWriterAdministrator.GetEventProcessingCount().Returns(0);

            testShardGroupSplitter.CopyEventsToTargetShardGroup
            (
                mockSourceShardGroupEventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroupWriterAdministrator,
                testHashRangeStart,
                testHashRangeEnd,
                testFilterGroupEventsByHashRange,
                testEventBatchSize,
                testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                testSourceWriterNodeOperationsCompleteCheckRetryInterval
            );

            mockSourceShardGroupEventReader.Received(1).GetInitialEvent();
            mockSourceShardGroupEventReader.Received(1).GetEvents(batch1FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.PersistEvents(eventBatch1);
            mockSourceShardGroupEventReader.Received(1).GetNextEventAfter(eventBatch1[1].EventId);
            mockSourceShardGroupEventReader.Received(1).GetEvents(batch2FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.PersistEvents(eventBatch2);
            mockSourceShardGroupEventReader.Received(1).GetNextEventAfter(eventBatch2[1].EventId);
            mockSourceShardGroupEventReader.Received(1).GetEvents(batch3FirstEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.PersistEvents(eventBatch3);
            mockSourceShardGroupEventReader.Received(2).GetNextEventAfter(eventBatch3[1].EventId);
            mockOperationRouter.Received(1).PauseOperations();
            mockSourceShardGroupWriterAdministrator.Received(1).GetEventProcessingCount();
            mockSourceShardGroupWriterAdministrator.Received(1).FlushEventBuffers();
            mockMetricLogger.Received(3).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(3).End(testBeginId, Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(3).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(3).End(testBeginId, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<WriterNodeEventProcessingCount>(), 0);
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EventProcessingCountCheckRetried>());
        }

        [Test]
        public void DeleteEventsFromSourceShardGroup_DeleteFais()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean includeGroupEvents = true;
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<EventDeleteTime>()).Returns(testBeginId);
            mockSourceShardGroupEventDeleter.When(deleter => deleter.DeleteEvents(testHashRangeStart, testHashRangeEnd, includeGroupEvents)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.DeleteEventsFromSourceShardGroup(mockSourceShardGroupEventDeleter, testHashRangeStart, testHashRangeEnd, includeGroupEvents);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EventDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EventDeleteTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to delete events from the source shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void DeleteEventsFromSourceShardGroup()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean includeGroupEvents = true;
            mockMetricLogger.Begin(Arg.Any<EventDeleteTime>()).Returns(testBeginId);

            testShardGroupSplitter.DeleteEventsFromSourceShardGroup(mockSourceShardGroupEventDeleter, testHashRangeStart, testHashRangeEnd, includeGroupEvents);

            mockSourceShardGroupEventDeleter.Received(1).DeleteEvents(testHashRangeStart, testHashRangeEnd, includeGroupEvents);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventDeleteTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventDeleteTime>());
        }

        [Test]
        public void PauseOperations_CallToRouterFails()
        {
            var mockException = new Exception("Mock exception");
            mockOperationRouter.When(router => router.PauseOperations()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.PauseOperations(mockOperationRouter);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to hold/pause incoming operations to the source and target shard groups."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CopyEventBatchesToTargetShardGroup_EventRetrievalFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var initialEventId = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            Int32 currentBatchNumber = 1;
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockSourceShardGroupEventReader.When(reader => reader.GetEvents(initialEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.CopyEventBatchesToTargetShardGroup
                (
                    mockSourceShardGroupEventReader,
                    mockTargetShardGroupEventPersister,
                    ref currentBatchNumber,
                    initialEventId,
                    testHashRangeStart,
                    testHashRangeEnd,
                    testFilterGroupEventsByHashRange,
                    testEventBatchSize
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EventBatchReadTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve event batch from the source shard group beginning with event with id '{initialEventId}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CopyEventBatchesToTargetShardGroup_EventPersistenceFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var initialEventId = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            var eventBatch = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(initialEventId, "user1"),
                GenerateAddUserEvent(Guid.NewGuid(), "user2")
            };
            Int32 currentBatchNumber = 1;
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(testBeginId);
            mockSourceShardGroupEventReader.GetEvents(initialEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch);
            mockTargetShardGroupEventPersister.When(persister => persister.PersistEvents(eventBatch)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.CopyEventBatchesToTargetShardGroup
                (
                    mockSourceShardGroupEventReader,
                    mockTargetShardGroupEventPersister,
                    ref currentBatchNumber,
                    initialEventId,
                    testHashRangeStart,
                    testHashRangeEnd,
                    testFilterGroupEventsByHashRange,
                    testEventBatchSize
                );
            });

            mockSourceShardGroupEventReader.Received(1).GetEvents(initialEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EventBatchWriteTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to write events to the target shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CopyEventBatchesToTargetShardGroup_GetNextEventFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var initialEventId = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            var eventBatch = new List<TemporalEventBufferItemBase>
            {
                GenerateAddUserEvent(initialEventId, "user1"),
                GenerateAddUserEvent(Guid.NewGuid(), "user2")
            };
            Int32 currentBatchNumber = 1;
            var mockException = new Exception("Mock exception");
            mockMetricLogger.Begin(Arg.Any<EventBatchReadTime>()).Returns(testBeginId);
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(testBeginId);
            mockSourceShardGroupEventReader.GetEvents(initialEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize).Returns(eventBatch);
            mockSourceShardGroupEventReader.When(reader => reader.GetNextEventAfter(eventBatch[1].EventId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.CopyEventBatchesToTargetShardGroup
                (
                    mockSourceShardGroupEventReader,
                    mockTargetShardGroupEventPersister,
                    ref currentBatchNumber,
                    initialEventId,
                    testHashRangeStart,
                    testHashRangeEnd,
                    testFilterGroupEventsByHashRange,
                    testEventBatchSize
                );
            });

            mockSourceShardGroupEventReader.Received(1).GetEvents(initialEventId, testHashRangeStart, testHashRangeEnd, testFilterGroupEventsByHashRange, testEventBatchSize);
            mockTargetShardGroupEventPersister.Received(1).PersistEvents(eventBatch);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventBatchReadTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventBatchWriteTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve next event after event with id '{eventBatch[1].EventId}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void GetNextEventAfter_EventRetrievalFails()
        {
            var testEventId = Guid.Parse("5ce76236-0d94-481e-a14b-d9ff5c7ab250");
            var mockException = new Exception("Mock exception");
            mockSourceShardGroupEventReader.When(reader => reader.GetNextEventAfter(testEventId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.GetNextEventAfter(mockSourceShardGroupEventReader, testEventId);
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
                testShardGroupSplitter.WaitForSourceWriterNodeEventProcessingCompletion
                (
                    mockSourceShardGroupWriterAdministrator,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to check for active operations in the source shard group event writer node."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void WaitForSourceWriterNodeEventProcessingCompletion_ProcessingNotCompletedAfterRetries()
        {
            mockSourceShardGroupWriterAdministrator.GetEventProcessingCount().Returns(3);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupSplitter.WaitForSourceWriterNodeEventProcessingCompletion
                (
                    mockSourceShardGroupWriterAdministrator,
                    testSourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    testSourceWriterNodeOperationsCompleteCheckRetryInterval
                );
            });

            mockSourceShardGroupWriterAdministrator.Received(4).GetEventProcessingCount();
            mockMetricLogger.Received(4).Set(Arg.Any<WriterNodeEventProcessingCount>(), 3);
            mockMetricLogger.Received(3).Increment(Arg.Any<EventProcessingCountCheckRetried>());
            Assert.That(e.Message, Does.StartWith($"Active operations in the source shard group event writer node remains at 3 after 3 retries with 50ms interval."));
        }

        [Test]
        public void WaitForSourceWriterNodeEventProcessingCompletion_SuccessAfterRetries()
        {
            mockSourceShardGroupWriterAdministrator.GetEventProcessingCount().Returns(3, 2, 0);

            testShardGroupSplitter.WaitForSourceWriterNodeEventProcessingCompletion
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
                testShardGroupSplitter.FlushSourceWriterNodeEventBuffers(mockSourceShardGroupWriterAdministrator);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to flush event buffer(s) in the source shard group event writer node."));
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
            return new UserEventBufferItem<String>(eventId, EventAction.Add, user, DateTime.UtcNow, hashCodeGenerator.GetHashCode(user));
        }

        #endregion

        #region Nested Classes

        private class DistributedAccessManagerShardGroupSplitterWithProtectedMembers : DistributedAccessManagerShardGroupSplitter
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.UnitTests.DistributedAccessManagerShardGroupSplitterTests+DistributedAccessManagerShardGroupSplitterWithProtectedMembers class.
            /// </summary>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public DistributedAccessManagerShardGroupSplitterWithProtectedMembers(IApplicationLogger logger, IMetricLogger metricLogger)
                : base(logger, metricLogger)
            {
            }

            /// <summary>
            /// Pauses/holds any incoming operation requests to the specified <see cref="IDistributedAccessManagerOperationRouter"/>.
            /// </summary>
            /// <param name="operationRouter">The <see cref="IDistributedAccessManagerOperationRouter"/> to pause operations on.</param>
            public new void PauseOperations(IDistributedAccessManagerOperationRouter operationRouter)
            {
                base.PauseOperations(operationRouter);
            }

            /// <summary>
            /// Copies a portion of events from a source to a target shard group in batches
            /// </summary>
            /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
            /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
            /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
            /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
            /// <param name="sourceShardGroupEventReader">The event reader for the source shard group.</param>
            /// <param name="targetShardGroupEventPersister">The event persister for the target shard group.</param>
            /// <param name="currentBatchNumber">The sequential number of the first batch of events (may be set to greater than 1 if this method is called multiple times).</param>
            /// <param name="firstEventId">The id of the first event in the sequence of events to copy.</param>
            /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of events to copy.</param>
            /// <param name="hashRangeEnd">The last (inclusive) in the range of hash codes of events to copy.</param>
            /// <param name="filterGroupEventsByHashRange">Whether to filter <see cref="GroupEventBufferItem{TGroup}">group events</see> by the hash range.  Will move all group events if set to false.</param>
            /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard in each batch.</param>
            /// <returns>The id of the last event that was copied.</returns>
            public new Guid CopyEventBatchesToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
            (
                IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
                IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
                ref Int32 currentBatchNumber,
                Nullable<Guid> firstEventId,
                Int32 hashRangeStart,
                Int32 hashRangeEnd,
                Boolean filterGroupEventsByHashRange,
                Int32 eventBatchSize
            )
            {
                return base.CopyEventBatchesToTargetShardGroup
                (
                sourceShardGroupEventReader,
                targetShardGroupEventPersister,
                ref currentBatchNumber,
                firstEventId,
                hashRangeStart,
                hashRangeEnd,
                filterGroupEventsByHashRange,
                eventBatchSize
                );
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
