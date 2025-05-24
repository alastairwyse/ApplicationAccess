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
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Redistribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.PrimaryElementEventDuplicateFilter class.
    /// </summary>
    public class PrimaryElementEventDuplicateFilterTests
    {
        private IEventPersisterBuffer testEventPersisterBuffer;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private PrimaryElementEventDuplicateFilter<String, String> testPrimaryElementEventDuplicateFilter;

        [SetUp]
        protected void SetUp()
        {
            testEventPersisterBuffer = Substitute.For<IEventPersisterBuffer>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, false, mockApplicationLogger, mockMetricLogger);
        }

        [Test]
        public void BufferEvent()
        {
            String testUser = "User1";
            String testGroup = "Group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var addUserEvent = new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000001"), EventAction.Add, testUser, DateTime.Now.ToUniversalTime(), testUser.GetHashCode());
            var removeUserEvent = new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000002"), EventAction.Remove, testUser, DateTime.Now.ToUniversalTime(), testUser.GetHashCode());
            var addGroupEvent = new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000003"), EventAction.Add, testGroup, DateTime.Now.ToUniversalTime(), testGroup.GetHashCode());
            var removeGroupEvent = new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000004"), EventAction.Remove, testGroup, DateTime.Now.ToUniversalTime(), testGroup.GetHashCode());
            var addEntityTypeEvent = new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000005"), EventAction.Add, testEntityType, DateTime.Now.ToUniversalTime(), testEntityType.GetHashCode());
            var removeEntityTypeEvent = new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000006"), EventAction.Remove, testEntityType, DateTime.Now.ToUniversalTime(), testEntityType.GetHashCode());
            var addEntityEvent = new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000007"), EventAction.Add, testEntityType, testEntity, DateTime.Now.ToUniversalTime(), testEntityType.GetHashCode());
            var removeEntityEvent = new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000008"), EventAction.Remove, testEntityType, testEntity, DateTime.Now.ToUniversalTime(), testEntityType.GetHashCode());
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("10000000-0000-0000-0000-000000000000"), Guid.Parse("20000000-0000-0000-0000-000000000000"))
            );
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeUserEvent, false).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("30000000-0000-0000-0000-000000000000"), Guid.Parse("40000000-0000-0000-0000-000000000000"))
            );
            testEventPersisterBuffer.BufferEvent(addGroupEvent, false).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("50000000-0000-0000-0000-000000000000"), Guid.Parse("60000000-0000-0000-0000-000000000000"))
            );
            testEventPersisterBuffer.BufferEvent(addGroupEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeGroupEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeGroupEvent, true).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("70000000-0000-0000-0000-000000000000"), Guid.Parse("80000000-0000-0000-0000-000000000000"))
            );
            testEventPersisterBuffer.BufferEvent(addEntityTypeEvent, true).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("90000000-0000-0000-0000-000000000000"), Guid.Parse("10000000-0000-0000-0000-000000000000"))
            );
            testEventPersisterBuffer.BufferEvent(addEntityTypeEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeEntityTypeEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeEntityTypeEvent, true).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("11000000-0000-0000-0000-000000000000"), Guid.Parse("12000000-0000-0000-0000-000000000000"))
            );
            testEventPersisterBuffer.BufferEvent(addEntityEvent, false).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("13000000-0000-0000-0000-000000000000"), Guid.Parse("14000000-0000-0000-0000-000000000000"))
            );
            testEventPersisterBuffer.BufferEvent(addEntityEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeEntityEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, null));
            testEventPersisterBuffer.BufferEvent(removeEntityEvent, false).Returns
            (
                new Tuple<Nullable<Guid>, Nullable<Guid>>(Guid.Parse("15000000-0000-0000-0000-000000000000"), Guid.Parse("16000000-0000-0000-0000-000000000000"))
            );

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);

            Assert.AreEqual("10000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("20000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);

            Assert.AreEqual("10000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("20000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);

            Assert.AreEqual("10000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("20000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);

            Assert.AreEqual("30000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("40000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addGroupEvent, false);

            Assert.AreEqual("50000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("60000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addGroupEvent, true);

            Assert.AreEqual("50000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("60000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeGroupEvent, false);

            Assert.AreEqual("50000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("60000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeGroupEvent, true);

            Assert.AreEqual("70000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("80000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addEntityTypeEvent, true);

            Assert.AreEqual("90000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("10000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addEntityTypeEvent, false);

            Assert.AreEqual("90000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("10000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeEntityTypeEvent, false);

            Assert.AreEqual("90000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("10000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeEntityTypeEvent, true);

            Assert.AreEqual("11000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("12000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addEntityEvent, false);

            Assert.AreEqual("13000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("14000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addEntityEvent, true);

            Assert.AreEqual("13000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("14000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeEntityEvent, true);

            Assert.AreEqual("13000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("14000000-0000-0000-0000-000000000000", result.Item2.ToString());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeEntityEvent, false);

            Assert.AreEqual("15000000-0000-0000-0000-000000000000", result.Item1.ToString());
            Assert.AreEqual("16000000-0000-0000-0000-000000000000", result.Item2.ToString());

            testEventPersisterBuffer.Received(1).BufferEvent(addUserEvent, true);
            testEventPersisterBuffer.Received(1).BufferEvent(removeUserEvent, false);
            testEventPersisterBuffer.Received(1).BufferEvent(addGroupEvent, false);
            testEventPersisterBuffer.Received(1).BufferEvent(removeGroupEvent, true);
            testEventPersisterBuffer.Received(1).BufferEvent(addEntityTypeEvent, true);
            testEventPersisterBuffer.Received(1).BufferEvent(removeEntityTypeEvent, true);
            testEventPersisterBuffer.Received(1).BufferEvent(addEntityEvent, false);
            testEventPersisterBuffer.Received(1).BufferEvent(removeEntityEvent, false);
        }

        // The following tests cover all paths in protected method FilterPrimaryElementEvent()
        //   The table below explains the test scenario inputs and expected results...

        // EventType    SourceShard     ExistsShard1    ExistsShard2    Result
        // ----------------------------------------------------------------------
        // Add          1               No              No              Send event
        // Add          1               No              Yes             Don't send event
        // Add          1               Yes             No              Exception
        // Add          1               Yes             Yes             Exception
        // Add          2               No              No              Send event
        // Add          2               No              Yes             Exception
        // Add          2               Yes             No              Don't send event
        // Add          2               Yes             Yes             Exception
        // Remove       1               No              No              Exception
        // Remove       1               No              Yes             Exception
        // Remove       1               Yes             No              Send event
        // Remove       1               Yes             Yes             Don't send event
        // Remove       2               No              No              Exception
        // Remove       2               No              Yes             Send event
        // Remove       2               Yes             No              Exception
        // Remove       2               Yes             Yes             Don't send event

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard1_NoEventsExist()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);

            testEventPersisterBuffer.Received(1).BufferEvent(addUserEvent, true);
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard1_EventExistsInSource2()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard1_EventExistsInSource1()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testEventPersisterBuffer.ClearReceivedCalls();

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A duplicate 'add' user event was received from the first source shard group with value 'user1'."));
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard1_EventExistsInBoth()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A duplicate 'add' user event was received from the first source shard group with value 'user1'."));
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard2_NoEventsExist()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);

            testEventPersisterBuffer.Received(1).BufferEvent(addUserEvent, false);
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard2_EventExistsInSource2()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A duplicate 'add' user event was received from the second source shard group with value 'user1'."));
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard2_EventExistsInSource1()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard2_EventExistsInBoth()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A duplicate 'add' user event was received from the second source shard group with value 'user1'."));
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard1_NoEventsExist()
        {
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A 'remove' user event was received from the first source shard group with value 'user1', where that element did not already exist."));
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard1_EventExistsInSource2()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A 'remove' user event was received from the first source shard group with value 'user1', where that element did not already exist."));
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard1_EventExistsInSource1()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(removeUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(removeUserEvent.EventId, null));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);

            testEventPersisterBuffer.Received(1).BufferEvent(removeUserEvent, true);
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard1_EventExistsInBoth()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard2_NoEventsExist()
        {
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A 'remove' user event was received from the second source shard group with value 'user1', where that element did not already exist."));
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard2_EventExistsInSource2()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testEventPersisterBuffer.BufferEvent(removeUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, removeUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);

            testEventPersisterBuffer.Received(1).BufferEvent(removeUserEvent, false);
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard2_EventExistsInSource1()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testEventPersisterBuffer.ClearReceivedCalls();

            var e = Assert.Throws<Exception>(delegate
            {
                testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);
            });

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
            Assert.That(e.Message, Does.StartWith($"A 'remove' user event was received from the second source shard group with value 'user1', where that element did not already exist."));
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard2_EventExistsInBoth()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);

            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_AddRemoveAdd()
        {
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testEventPersisterBuffer.BufferEvent(removeUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(removeUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(removeUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, removeUserEvent.EventId));

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);

            Assert.AreEqual(1, testEventPersisterBuffer.ReceivedCalls().Count());
            testEventPersisterBuffer.Received(1).BufferEvent(addUserEvent, false);


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);

            Assert.AreEqual(1, testEventPersisterBuffer.ReceivedCalls().Count());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);

            Assert.AreEqual(1, testEventPersisterBuffer.ReceivedCalls().Count());


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);

            Assert.AreEqual(2, testEventPersisterBuffer.ReceivedCalls().Count());
            testEventPersisterBuffer.Received(1).BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.Received(1).BufferEvent(removeUserEvent, true);


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);

            Assert.AreEqual(3, testEventPersisterBuffer.ReceivedCalls().Count());
            testEventPersisterBuffer.Received(2).BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.Received(1).BufferEvent(removeUserEvent, true);


            result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);

            Assert.AreEqual(3, testEventPersisterBuffer.ReceivedCalls().Count());
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard1_EventExistsInSource1_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A duplicate 'add' user event was received from the first source shard group with value 'user1'.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidAddPrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard1_EventExistsInBoth_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A duplicate 'add' user event was received from the first source shard group with value 'user1'.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidAddPrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard2_EventExistsInSource2_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A duplicate 'add' user event was received from the second source shard group with value 'user1'.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidAddPrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_AddEvent_SourceShard2_EventExistsInBoth_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A duplicate 'add' user event was received from the second source shard group with value 'user1'.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidAddPrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard1_NoEventsExist_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A 'remove' user event was received from the first source shard group with value 'user1', where that element did not already exist.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidRemovePrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard1_EventExistsInSource2_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, false).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(null, addUserEvent.EventId));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, false);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, true);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A 'remove' user event was received from the first source shard group with value 'user1', where that element did not already exist.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidRemovePrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard2_NoEventsExist_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A 'remove' user event was received from the second source shard group with value 'user1', where that element did not already exist.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidRemovePrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
        }

        [Test]
        public void FilterPrimaryElementEvent_RemoveEvent_SourceShard2_EventExistsInSource1_IgnoreInvalidEventsTrue()
        {
            testPrimaryElementEventDuplicateFilter = new PrimaryElementEventDuplicateFilter<String, String>(testEventPersisterBuffer, true, mockApplicationLogger, mockMetricLogger);
            UserEventBufferItem<String> addUserEvent = GenerateAddUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            UserEventBufferItem<String> removeUserEvent = GenerateRemoveUserEvent(Guid.Parse("00000000-0000-0000-0000-000000000001"), "user1");
            testEventPersisterBuffer.BufferEvent(addUserEvent, true).Returns(new Tuple<Nullable<Guid>, Nullable<Guid>>(addUserEvent.EventId, null));
            testPrimaryElementEventDuplicateFilter.BufferEvent(addUserEvent, true);
            testEventPersisterBuffer.ClearReceivedCalls();

            Tuple<Nullable<Guid>, Nullable<Guid>> result = testPrimaryElementEventDuplicateFilter.BufferEvent(removeUserEvent, false);

            mockApplicationLogger.Received(1).Log(testPrimaryElementEventDuplicateFilter, LogLevel.Error, "A 'remove' user event was received from the second source shard group with value 'user1', where that element did not already exist.");
            mockMetricLogger.Received(1).Increment(Arg.Any<InvalidRemovePrimaryElementEventReceived>());
            testEventPersisterBuffer.DidNotReceive().BufferEvent(Arg.Any<UserEventBufferItem<String>>(), Arg.Any<Boolean>());
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
        /// Generates a 'remove user' event.
        /// </summary>
        /// <param name="eventId">The id of the event.</param>
        /// <param name="user">The user.</param>
        /// <returns>An 'remove user' event.</returns>
        protected UserEventBufferItem<String> GenerateRemoveUserEvent(Guid eventId, String user)
        {
            return new UserEventBufferItem<String>(eventId, EventAction.Remove, user, DateTime.UtcNow, user.GetHashCode());
        }

        #endregion
    }
}
