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
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using NSubstitute;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Persistence.AccessManagerRedundantTemporalBulkPersister class.
    /// </summary>
    public class AccessManagerRedundantTemporalBulkPersisterTests
    {
        protected IAccessManagerTemporalPersistentReader<String, String, String, String> mockPrimaryReader;
        protected IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String> mockPrimaryPersister;
        protected IAccessManagerTemporalEventBulkPersisterReader<String, String, String, String> mockBackupPersister;
        protected IApplicationLogger mockLogger;
        protected IMetricLogger mockMetricLogger;
        protected AccessManagerRedundantTemporalBulkPersister<String, String, String, String> testAccessManagerRedundantTemporalBulkPersister;

        [SetUp]
        protected void SetUp()
        {
            mockPrimaryReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, String, String>>();
            mockPrimaryPersister = Substitute.For<IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String>>();
            mockBackupPersister = Substitute.For<IAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>>();
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerRedundantTemporalBulkPersister = new AccessManagerRedundantTemporalBulkPersister<String, String, String, String>
            (
                mockPrimaryReader, 
                mockPrimaryPersister, 
                mockBackupPersister, 
                mockLogger, 
                mockMetricLogger
            );
        }

        [Test]
        public void Load()
        {
            var testAccessManager = new AccessManager<String, String, String, String>();

            testAccessManagerRedundantTemporalBulkPersister.Load(testAccessManager);

            mockPrimaryReader.Received(1).Load(testAccessManager);
        }

        [Test]
        public void Load_EventIdOverload()
        {
            var testEventId = Guid.NewGuid();
            var testAccessManager = new AccessManager<String, String, String, String>();

            testAccessManagerRedundantTemporalBulkPersister.Load(testEventId, testAccessManager);

            mockPrimaryReader.Received(1).Load(testEventId, testAccessManager);
        }

        [Test]
        public void Load_StateTimeOverload()
        {
            DateTime testStateTime = CreateDataTimeFromString("2024-08-25 22:19:26");
            var testAccessManager = new AccessManager<String, String, String, String>();

            testAccessManagerRedundantTemporalBulkPersister.Load(testStateTime, testAccessManager);

            mockPrimaryReader.Received(1).Load(testStateTime, testAccessManager);
        }

        [Test]
        public void PersistEvents_ExceptionRetrievingFromBackupPersister()
        {
            var mockException = new Exception("Mock GetAllEvents() Exception.");
            mockBackupPersister.When((persister) => persister.GetAllEvents()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>(), false);
            });

            Assert.AreEqual(1, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(0, mockPrimaryPersister.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, mockLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve backed-up events from backup persister."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void PersistEvents_ExceptionWhenWritingBackedUpEventsToPrimaryAfterRetrievingFromBackupPersister()
        {
            var mockException = new Exception("Mock PersistEvents() Exception.");
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);
            mockPrimaryPersister.When((persister) => persister.PersistEvents(testBackedUpEvents, true)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);
            });

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockBackupPersister.Received(1).PersistEvents(testBackedUpEvents);
            mockBackupPersister.Received(1).PersistEvents(testEvents);
            Assert.AreEqual(3, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(1, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToBackupPersister>(), 5);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventWriteToPrimaryPersisterFailed>());
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Error, "Wrote 5 events to backup event persister due to exception encountered during persist operation on primary persister.", mockException);
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, mockLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to persist previously backed-up events to primary persister."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void PersistEvents_ExceptionWhenWritingBackedUpEventsToBackupAfterRetrievingFromBackupPersister()
        {
            var mockPrimaryException = new Exception("Mock primary PersistEvents() Exception.");
            var mockBackupException = new Exception("Mock backup PersistEvents() Exception.");
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);
            mockPrimaryPersister.When((persister) => persister.PersistEvents(testBackedUpEvents, true)).Do((callInfo) => throw mockPrimaryException);
            mockBackupPersister.When((persister) => persister.PersistEvents(testBackedUpEvents)).Do((callInfo) => throw mockBackupException);

            var e = Assert.Throws<AggregateException>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);
            });

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockBackupPersister.Received(1).PersistEvents(testBackedUpEvents);
            mockBackupPersister.DidNotReceive().PersistEvents(testEvents);
            Assert.AreEqual(2, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(1, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, mockLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to persist events to backup persister whilst handling exception generated in primary persister from attempting to persist previously backed-up events."));
            Assert.IsTrue(e.InnerExceptions.Contains(mockPrimaryException));
            Assert.IsTrue(e.InnerExceptions.Contains(mockBackupException));
        }

        [Test]
        public void PersistEvents_ExceptionWhenWritingMainEventsToPrimaryAfterRetrievingFromBackupPersister()
        {
            var mockException = new Exception("Mock PersistEvents() Exception.");
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);
            mockPrimaryPersister.When((persister) => persister.PersistEvents(testEvents, false)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);
            });

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockPrimaryPersister.Received(1).PersistEvents(testEvents, false);
            mockBackupPersister.Received(1).PersistEvents(testEvents);
            Assert.AreEqual(2, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(2, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 3);
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToBackupPersister>(), 2);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventWriteToPrimaryPersisterFailed>());
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Error, "Wrote 2 events to backup event persister due to exception encountered during persist operation on primary persister.", mockException);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, mockLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to persist events to primary persister."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void PersistEvents_ExceptionWhenWritingMainEventsToBackupAfterRetrievingFromBackupPersister()
        {
            var mockPrimaryException = new Exception("Mock primary PersistEvents() Exception.");
            var mockBackupException = new Exception("Mock backup PersistEvents() Exception.");
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);
            mockPrimaryPersister.When((persister) => persister.PersistEvents(testEvents, false)).Do((callInfo) => throw mockPrimaryException);
            mockBackupPersister.When((persister) => persister.PersistEvents(testEvents)).Do((callInfo) => throw mockBackupException);

            var e = Assert.Throws<AggregateException>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);
            });

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockPrimaryPersister.Received(1).PersistEvents(testEvents, false);
            mockBackupPersister.Received(1).PersistEvents(testEvents);
            Assert.AreEqual(2, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(2, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 3);
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, mockLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to persist events to backup persister whilst handling exception generated in primary persister."));
            Assert.IsTrue(e.InnerExceptions.Contains(mockPrimaryException));
            Assert.IsTrue(e.InnerExceptions.Contains(mockBackupException));
        }

        [Test]
        public void PersistEvents_NoEventsRetrievedFromBackupPersister()
        {
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(new List<TemporalEventBufferItemBase>());

            testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testEvents, false);
            Assert.AreEqual(1, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(1, mockPrimaryPersister.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, mockLogger.ReceivedCalls().Count());
        }

        [Test]
        public void PersistEvents()
        {
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);

            testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockPrimaryPersister.Received(1).PersistEvents(testEvents, false);
            Assert.AreEqual(1, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(2, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 3);
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, mockLogger.ReceivedCalls().Count());


            // Test that a subsequent call to PersistEvents() doesn't again attempt to read from the backup persister
            testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockPrimaryPersister.Received(2).PersistEvents(testEvents, false);
            Assert.AreEqual(1, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(3, mockPrimaryPersister.ReceivedCalls().Count());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, mockLogger.ReceivedCalls().Count());
        }

        [Test]
        public void PersistEvents_IgnorePreExistingEventsParameterTrue()
        {
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);

            testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, true);

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockPrimaryPersister.Received(1).PersistEvents(testEvents, true);
            Assert.AreEqual(1, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(2, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 3);
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, mockLogger.ReceivedCalls().Count());
        }

        [Test]
        public void PersistEvents_ExceptionWhenWritingToBackupAfterPreviousExceptionWhenWritingToPrimaryPersister()
        {
            var mockPrimaryException = new Exception("Mock primary PersistEvents() Exception.");
            var mockBackupException = new Exception("Mock backup PersistEvents() Exception.");
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            var testEvents2 = new List<TemporalEventBufferItemBase>()
            {
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Remove, "User2", CreateDataTimeFromString("2024-08-25 15:58:01"), 2),
            };
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);
            mockPrimaryPersister.When((persister) => persister.PersistEvents(testEvents, false)).Do((callInfo) => throw mockPrimaryException);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);
            });

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockPrimaryPersister.Received(1).PersistEvents(testEvents, false);
            mockBackupPersister.Received(1).PersistEvents(testEvents);
            Assert.AreEqual(2, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(2, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 3);
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToBackupPersister>(), 2);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventWriteToPrimaryPersisterFailed>());
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Error, "Wrote 2 events to backup event persister due to exception encountered during persist operation on primary persister.", mockPrimaryException);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, mockLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to persist events to primary persister."));
            Assert.AreSame(mockPrimaryException, e.InnerException);


            mockBackupPersister.When((persister) => persister.PersistEvents(testEvents2)).Do((callInfo) => throw mockBackupException);

            e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents2, false);
            });

            mockBackupPersister.Received(1).PersistEvents(testEvents2);
            Assert.AreEqual(3, mockBackupPersister.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to persist events to backup persister after previous exception on primary persister."));
            Assert.AreSame(mockBackupException, e.InnerException);
        }

        [Test]
        public void PersistEvents_PreviousExceptionWhenWritingToPrimaryPersister()
        {
            var mockPrimaryException = new Exception("Mock primary PersistEvents() Exception.");
            var mockBackupException = new Exception("Mock backup PersistEvents() Exception.");
            IList<TemporalEventBufferItemBase> testBackedUpEvents = GenerateTestBackedUpEvents();
            IList<TemporalEventBufferItemBase> testEvents = GenerateTestEvents();
            var testEvents2 = new List<TemporalEventBufferItemBase>()
            {
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Remove, "User2", CreateDataTimeFromString("2024-08-25 15:58:01"), 2),
            };
            mockBackupPersister.GetAllEvents().Returns<IList<TemporalEventBufferItemBase>>(testBackedUpEvents);
            mockPrimaryPersister.When((persister) => persister.PersistEvents(testEvents, false)).Do((callInfo) => throw mockPrimaryException);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents, false);
            });

            mockBackupPersister.Received(1).GetAllEvents();
            mockPrimaryPersister.Received(1).PersistEvents(testBackedUpEvents, true);
            mockPrimaryPersister.Received(1).PersistEvents(testEvents, false);
            mockBackupPersister.Received(1).PersistEvents(testEvents);
            Assert.AreEqual(2, mockBackupPersister.ReceivedCalls().Count());
            Assert.AreEqual(2, mockPrimaryPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromBackupPersister>(), 3);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Information, "Read 3 events from backup event persister.");
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 3);
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToBackupPersister>(), 2);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventWriteToPrimaryPersisterFailed>());
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Error, "Wrote 2 events to backup event persister due to exception encountered during persist operation on primary persister.", mockPrimaryException);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, mockLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to persist events to primary persister."));
            Assert.AreSame(mockPrimaryException, e.InnerException);


            testAccessManagerRedundantTemporalBulkPersister.PersistEvents(testEvents2, false);

            mockBackupPersister.Received(1).PersistEvents(testEvents2);
            Assert.AreEqual(3, mockBackupPersister.ReceivedCalls().Count());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToBackupPersister>(), 1);
            mockLogger.Received(1).Log(testAccessManagerRedundantTemporalBulkPersister, LogLevel.Error, "Wrote 1 events to backup event persister after previous exception on primary persister.");
            Assert.AreEqual(5, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, mockLogger.ReceivedCalls().Count());
        }

        #region Private/Protected Methods

        /// <summary>
        /// Generates a set of test events returned from the backup persister.
        /// </summary>
        /// <returns>The test events.</returns>
        protected IList<TemporalEventBufferItemBase> GenerateTestBackedUpEvents()
        {
            var returnEvents = new List<TemporalEventBufferItemBase>()
            {
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "User1", CreateDataTimeFromString("2024-08-25 15:57:00"), 1),
                new GroupEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "Group1", CreateDataTimeFromString("2024-08-25 15:57:10"), 11),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.NewGuid(), EventAction.Add, "User1", "Group1", CreateDataTimeFromString("2024-08-25 15:57:20"), 1)
            };

            return returnEvents;
        }

        /// <summary>
        /// Generates a set of test events returned from the primary persister.
        /// </summary>
        /// <returns>The test events.</returns>
        protected IList<TemporalEventBufferItemBase> GenerateTestEvents()
        {
            var returnEvents = new List<TemporalEventBufferItemBase>()
            {
                new EntityTypeEventBufferItem(Guid.NewGuid(), EventAction.Add, "Clients", CreateDataTimeFromString("2024-08-25 15:57:30"), 21),
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Remove, "User1", CreateDataTimeFromString("2024-08-25 15:57:40"), 1),
            };

            return returnEvents;
        }

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
