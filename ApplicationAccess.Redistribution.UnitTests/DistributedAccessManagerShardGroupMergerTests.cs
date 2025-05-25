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
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup1EventReader;
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup2EventReader;
        private IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String> mockTargetShardGroupEventPersister;
        private IEventPersisterBuffer mockTargetShardGroupEventPersisterBuffer;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerShardGroupMergerWithProtectedMembers testShardGroupMerger;


        [SetUp]
        protected void SetUp()
        {
            mockSourceShardGroup1EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockSourceShardGroup2EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockTargetShardGroupEventPersister = Substitute.For<IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String>>();
            mockTargetShardGroupEventPersisterBuffer = Substitute.For<IEventPersisterBuffer>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardGroupMerger = new DistributedAccessManagerShardGroupMergerWithProtectedMembers(mockApplicationLogger, mockMetricLogger);
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

            testShardGroupMerger.BufferAllRemainingEvents<String, String, String, String>
            (
                mockSourceShardGroup1EventReader,
                sourceShardGroupEventQueue,
                mockTargetShardGroupEventPersisterBuffer,
                3,
                true
            );

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

            testShardGroupMerger.BufferAllRemainingEvents<String, String, String, String>
            (
                mockSourceShardGroup1EventReader,
                sourceShardGroupEventQueue,
                mockTargetShardGroupEventPersisterBuffer,
                3,
                true
            );

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

            public new void BufferAllRemainingEvents<TUser, TGroup, TComponent, TAccess>
            (
                IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
                Queue<TemporalEventBufferItemBase> sourceShardGroupEventQueue,
                IEventPersisterBuffer targetShardGroupEventPersisterBuffer,
                Int32 eventBatchSize,
                Boolean sourceShardGroupIsFirst
            )
            {
                base.BufferAllRemainingEvents<TUser, TGroup, TComponent, TAccess>
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
