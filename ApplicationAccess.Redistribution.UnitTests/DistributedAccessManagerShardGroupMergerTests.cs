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
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution;
using ApplicationAccess.Persistence;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;
using ApplicationAccess.Persistence.Models;
using NUnit.Framework.Internal.Execution;
using System.Diagnostics.Tracing;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationAccess.Redistribution.Models;

namespace ApplicationAccess.Redistribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupMerger class.
    /// </summary>
    public class DistributedAccessManagerShardGroupMergerTests
    {
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup1EventReader;
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup2EventReader;
        private IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String> mockTargetShardGroupEventPersister;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerShardGroupMergerWithProtectedMembers testShardGroupMerger;


        [SetUp]
        protected void SetUp()
        {
            mockSourceShardGroup1EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockSourceShardGroup2EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockTargetShardGroupEventPersister = Substitute.For<IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String>>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardGroupMerger = new DistributedAccessManagerShardGroupMergerWithProtectedMembers(mockApplicationLogger, mockMetricLogger);
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
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, $"Read 3 events from first source shard group.");
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
            mockApplicationLogger.Received(1).Log(testShardGroupMerger, LogLevel.Information, $"Read 0 events from second source shard group.");
            Assert.IsFalse(result.HasValue);
            Assert.AreEqual(0, destinationEventQueue.Count);
            var resultList = new List<TemporalEventBufferItemBase>();
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
                ref Int32 currentBatchNumber,
                Guid sourceShardGroup1FirstEventId,
                Guid sourceShardGroup2FirstEventId,
                Int32 eventBatchSize,
                NoEventsReadDuringMergeAction noEventsReadDuringMergeAction
            )
            {
                return base.MergeEventBatchesToTargetShardGroup
                (
                    sourceShardGroup1EventReader,
                    sourceShardGroup2EventReader,
                    targetShardGroupEventPersister,
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
        }

        #endregion
    }
}
