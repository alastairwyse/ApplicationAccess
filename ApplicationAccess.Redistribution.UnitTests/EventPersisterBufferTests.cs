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
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationLogging;
using ApplicationMetrics;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Redistribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.EventPersisterBuffer class.
    /// </summary>
    public class EventPersisterBufferTests
    {
        private IAccessManagerTemporalEventBulkPersister<String, String, String, String> mockEventBulkPersister;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private EventPersisterBuffer<String, String, String, String> testEventPersisterBuffer;

        [SetUp]
        protected void SetUp()
        {
            mockEventBulkPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, String, String>>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testEventPersisterBuffer = new EventPersisterBuffer<String, String, String, String>(mockEventBulkPersister, 3, 1, mockApplicationLogger, mockMetricLogger);
        }

        [Test]
        public void Constructor_BufferSizeParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testEventPersisterBuffer = new EventPersisterBuffer<String, String, String, String>(mockEventBulkPersister, 0, 1, mockApplicationLogger, mockMetricLogger);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'bufferSize' with value 0 must be greater than 0."));
            Assert.AreEqual("bufferSize", e.ParamName);
        }

        [Test]
        public void BufferEvent()
        {
            Guid beginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid beginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(beginId1);
            var testEvent1 = GenerateUserEventBufferItem(1);
            var testEvent2 = GenerateUserEventBufferItem(2);
            var testEvent3 = GenerateUserEventBufferItem(3);
            var testEvent4 = GenerateUserEventBufferItem(4);
            var testEvent5 = GenerateUserEventBufferItem(5);
            var testEvent6 = GenerateUserEventBufferItem(6);
            var testEvent7 = GenerateUserEventBufferItem(7);
            List<TemporalEventBufferItemBase> capturedeventList = null;
            mockEventBulkPersister.PersistEvents(Arg.Do<List<TemporalEventBufferItemBase>>(argumentValue => capturedeventList = argumentValue));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testEventPersisterBuffer.BufferEvent(testEvent1, true);

            Assert.IsNull(result.Item1);
            Assert.IsNull(result.Item2);


            result = testEventPersisterBuffer.BufferEvent(testEvent2, true);

            Assert.IsNull(result.Item1);
            Assert.IsNull(result.Item2);


            result = testEventPersisterBuffer.BufferEvent(testEvent3, true);

            Assert.AreEqual("00000000-0000-0000-0000-000000000003", result.Item1.ToString());
            Assert.IsNull(result.Item2);
            Assert.AreEqual(3, capturedeventList.Count);
            Assert.AreSame(testEvent1, capturedeventList[0]);
            Assert.AreSame(testEvent2, capturedeventList[1]);
            Assert.AreSame(testEvent3, capturedeventList[2]);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).End(beginId1, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsCopiedFromSourceToTargetShardGroup>(), 3);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventBatchCopyCompleted>());
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, "Writing batch 1 of events from source shard groups to target shard group.");
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, "Wrote 3 event(s) to target shard group.");


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(beginId1, beginId2);
            mockApplicationLogger.ClearReceivedCalls();
            testEventPersisterBuffer = new EventPersisterBuffer<String, String, String, String>(mockEventBulkPersister, 3, 1, mockApplicationLogger, mockMetricLogger);
            result = testEventPersisterBuffer.BufferEvent(testEvent4, false);

            Assert.IsNull(result.Item1);
            Assert.IsNull(result.Item2);


            result = testEventPersisterBuffer.BufferEvent(testEvent5, false);

            Assert.IsNull(result.Item1);
            Assert.IsNull(result.Item2);


            result = testEventPersisterBuffer.BufferEvent(testEvent6, false);

            Assert.IsNull(result.Item1);
            Assert.AreEqual("00000000-0000-0000-0000-000000000006", result.Item2.ToString());
            Assert.AreEqual(3, capturedeventList.Count);
            Assert.AreSame(testEvent4, capturedeventList[0]);
            Assert.AreSame(testEvent5, capturedeventList[1]);
            Assert.AreSame(testEvent6, capturedeventList[2]);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).End(beginId1, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsCopiedFromSourceToTargetShardGroup>(), 3);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventBatchCopyCompleted>());
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, "Writing batch 1 of events from source shard groups to target shard group.");
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, "Wrote 3 event(s) to target shard group.");


            result = testEventPersisterBuffer.BufferEvent(testEvent7, false);

            Assert.IsNull(result.Item1);
            Assert.AreEqual("00000000-0000-0000-0000-000000000006", result.Item2.ToString());


            result = testEventPersisterBuffer.BufferEvent(testEvent1, true);

            Assert.IsNull(result.Item1);
            Assert.AreEqual("00000000-0000-0000-0000-000000000006", result.Item2.ToString());


            result = testEventPersisterBuffer.BufferEvent(testEvent2, false);

            Assert.AreEqual("00000000-0000-0000-0000-000000000001", result.Item1.ToString());
            Assert.AreEqual("00000000-0000-0000-0000-000000000002", result.Item2.ToString());
            Assert.AreEqual(3, capturedeventList.Count);
            Assert.AreSame(testEvent7, capturedeventList[0]);
            Assert.AreSame(testEvent1, capturedeventList[1]);
            Assert.AreSame(testEvent2, capturedeventList[2]);
            mockMetricLogger.Received(2).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).End(beginId1, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).End(beginId2, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(2).Add(Arg.Any<EventsCopiedFromSourceToTargetShardGroup>(), 3);
            mockMetricLogger.Received(2).Increment(Arg.Any<EventBatchCopyCompleted>());
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, "Writing batch 1 of events from source shard groups to target shard group.");
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, "Writing batch 2 of events from source shard groups to target shard group.");
            mockApplicationLogger.Received(2).Log(testEventPersisterBuffer, LogLevel.Information, "Wrote 3 event(s) to target shard group.");


            result = testEventPersisterBuffer.BufferEvent(testEvent7, true);

            Assert.AreEqual("00000000-0000-0000-0000-000000000001", result.Item1.ToString());
            Assert.AreEqual("00000000-0000-0000-0000-000000000002", result.Item2.ToString());
        }

        [Test]
        public void Flush_ExceptionPersistingEvents()
        {
            var mockException = new Exception("Mock exception");
            var testEvent = GenerateUserEventBufferItem(1);
            Guid beginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(beginId);
            mockEventBulkPersister.When((persister) => persister.PersistEvents(Arg.Any<List<TemporalEventBufferItemBase>>())).Do((callInfo) => throw mockException);
            testEventPersisterBuffer.BufferEvent(testEvent, true);

            var e = Assert.Throws<Exception>(delegate
            {
                testEventPersisterBuffer.Flush();
            });

            mockEventBulkPersister.Received(1).PersistEvents(Arg.Any<List<TemporalEventBufferItemBase>>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).CancelBegin(beginId, Arg.Any<EventBatchWriteTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to write events to the target shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void Flush()
        {
            var testEvent1 = GenerateUserEventBufferItem(1);
            var testEvent2 = GenerateUserEventBufferItem(2);
            var testEvent3 = GenerateUserEventBufferItem(3);
            Guid beginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<EventBatchWriteTime>()).Returns(beginId);
            List<TemporalEventBufferItemBase> capturedeventList = null;
            mockEventBulkPersister.PersistEvents(Arg.Do<List<TemporalEventBufferItemBase>>(argumentValue => capturedeventList = argumentValue));
            testEventPersisterBuffer.BufferEvent(testEvent1, true);

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testEventPersisterBuffer.Flush();

            Assert.AreEqual("00000000-0000-0000-0000-000000000001", result.Item1.ToString());
            Assert.IsNull(result.Item2);
            Assert.AreEqual(1, capturedeventList.Count);
            Assert.AreSame(testEvent1, capturedeventList[0]);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).End(beginId, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsCopiedFromSourceToTargetShardGroup>(), 1);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventBatchCopyCompleted>());
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, $"Writing batch 1 of events from source shard groups to target shard group.");
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, $"Wrote 1 event(s) to target shard group.");


            mockMetricLogger.ClearReceivedCalls();
            mockApplicationLogger.ClearReceivedCalls();
            capturedeventList = null;
            testEventPersisterBuffer.BufferEvent(testEvent2, false);
            testEventPersisterBuffer.BufferEvent(testEvent3, false);

            result = testEventPersisterBuffer.Flush();

            Assert.AreEqual("00000000-0000-0000-0000-000000000001", result.Item1.ToString());
            Assert.AreEqual("00000000-0000-0000-0000-000000000003", result.Item2.ToString());
            Assert.AreEqual(2, capturedeventList.Count);
            Assert.AreSame(testEvent2, capturedeventList[0]);
            Assert.AreSame(testEvent3, capturedeventList[1]);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).End(beginId, Arg.Any<EventBatchWriteTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsCopiedFromSourceToTargetShardGroup>(), 2);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventBatchCopyCompleted>());
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, $"Writing batch 2 of events from source shard groups to target shard group.");
            mockApplicationLogger.Received(1).Log(testEventPersisterBuffer, LogLevel.Information, $"Wrote 2 event(s) to target shard group.");
        }

        [Test]
        public void Flush_BufferEmpty()
        {
            var testEvent1 = GenerateUserEventBufferItem(1);
            var testEvent2 = GenerateUserEventBufferItem(2);
            var testEvent3 = GenerateUserEventBufferItem(3);
            testEventPersisterBuffer.BufferEvent(testEvent1, true);
            testEventPersisterBuffer.BufferEvent(testEvent2, true);
            testEventPersisterBuffer.BufferEvent(testEvent3, false);
            mockEventBulkPersister.Received(1).PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
            mockEventBulkPersister.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testEventPersisterBuffer.Flush();

            Assert.AreEqual("00000000-0000-0000-0000-000000000002", result.Item1.ToString());
            Assert.AreEqual("00000000-0000-0000-0000-000000000003", result.Item2.ToString());
            mockEventBulkPersister.DidNotReceive().PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>());
        }

        #region Private/Protected Methods

        private UserEventBufferItem<String> GenerateUserEventBufferItem(Int32 eventIndex)
        {
            String testUser = $"User{eventIndex}";
            return new UserEventBufferItem<String>(Guid.Parse($"00000000-0000-0000-0000-00000000000{eventIndex}"), EventAction.Add, testUser, DateTime.Now.ToUniversalTime(), testUser.GetHashCode());
        }

        #endregion
    }
}
