/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence. AccessManagerTemporalEventCacheBase class.
    /// </summary>
    /// <remarks>Since  AccessManagerTemporalEventCacheBase is abstract, tests are performed through derived class AccessManagerTemporalEventCache.</remarks>
    public class AccessManagerTemporalEventCacheBaseTests
    {
        private IHashCodeGenerator<String> mockUserHashCodeGenerator;
        private IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        private IHashCodeGenerator<String> mockEntityTypeHashCodeGenerator;
        private IMetricLogger mockMetricLogger;
        private IGuidProvider mockGuidProvider;
        private IDateTimeProvider mockDateTimeProvider;
        private AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel> testAccessManagerTemporalEventCache;

        [SetUp]
        protected void SetUp()
        {
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEntityTypeHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            testAccessManagerTemporalEventCache = new AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator, 
                mockGroupHashCodeGenerator, 
                mockEntityTypeHashCodeGenerator, 
                2, 
                mockMetricLogger, 
                mockGuidProvider, 
                mockDateTimeProvider
            );
        }

        [TearDown]
        protected void TearDown()
        {
            testAccessManagerTemporalEventCache.Dispose();
        }

        [Test]
        public void GetAllEventsSince_EventWithIdDoesntExist()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testEventId1 = Guid.Parse("00000000-0000-0000-0000-000000000029");
            mockMetricLogger.Begin(Arg.Any<CachedEventsReadTime>()).Returns(testBeginId);

            mockMetricLogger.ClearReceivedCalls();
            testAccessManagerTemporalEventCache.AddUser("user1", Guid.Parse("00000000-0000-0000-0000-00000000002A"), DateTime.UtcNow, 1);
            testAccessManagerTemporalEventCache.AddUserToGroupMapping("user1", "group1", Guid.Parse("00000000-0000-0000-0000-00000000002B"), DateTime.UtcNow, 1);
            testAccessManagerTemporalEventCache.AddEntity("ClientAccount", "CompanyA", Guid.Parse("00000000-0000-0000-0000-00000000002C"), DateTime.UtcNow, 21);

            var e = Assert.Throws<EventNotCachedException>(delegate
            {
                testAccessManagerTemporalEventCache.GetAllEventsSince(testEventId1);
            });

            Assert.That(e.Message, Does.StartWith("No event with eventId '00000000-0000-0000-0000-000000000029' was found in the cache."));
            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.DidNotReceive().Add(Arg.Any<CachedEventsRead>(), Arg.Any<Int64>());
        }

        [Test]
        public void GetAllEventsSince_EventCacheIsEmpty()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testEventId1 = Guid.Parse("00000000-0000-0000-0000-000000000029");
            mockMetricLogger.Begin(Arg.Any<CachedEventsReadTime>()).Returns(testBeginId);

            var e = Assert.Throws<EventCacheEmptyException>(delegate
            {
                testAccessManagerTemporalEventCache.GetAllEventsSince(testEventId1);
            });

            Assert.That(e.Message, Does.StartWith("The event cache is empty."));
            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.DidNotReceive().Add(Arg.Any<CachedEventsRead>(), Arg.Any<Int64>());
        }

        [Test]
        public void GetAllEventsSince()
        {
            const String user = "user1";
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000002D");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000002E");
            Guid eventId3 = Guid.Parse("00000000-0000-0000-0000-00000000002F");
            Guid eventId4 = Guid.Parse("00000000-0000-0000-0000-000000000030");
            DateTime occurredTime1 = CreateDataTimeFromString("2022-09-25 13:51:36");
            DateTime occurredTime2 = CreateDataTimeFromString("2022-09-25 13:51:37");
            DateTime occurredTime3 = CreateDataTimeFromString("2022-09-25 13:51:38");
            DateTime occurredTime4 = CreateDataTimeFromString("2022-09-25 13:51:39");
            Int32 user1HashCode = 1;
            Int32 group1HashCode = 11;
            Int32 clientAccountHashCode = 21;
            mockMetricLogger.Begin(Arg.Any<CachedEventsReadTime>()).Returns(testBeginId);
            testAccessManagerTemporalEventCache = new AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                5,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
            testAccessManagerTemporalEventCache.AddUser(user, eventId1, occurredTime1, user1HashCode);
            testAccessManagerTemporalEventCache.AddGroup(group, eventId2, occurredTime2, group1HashCode);

            IList<TemporalEventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsRead>(), 1);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            GroupEventBufferItem<String> groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group1HashCode, groupEventItem.HashCode);
            Assert.AreEqual(group, groupEventItem.Group);


            mockMetricLogger.ClearReceivedCalls();
            testAccessManagerTemporalEventCache.AddEntityType(entityType, eventId3, occurredTime3, clientAccountHashCode);

            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsRead>(), 2);
            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group1HashCode, groupEventItem.HashCode);
            Assert.AreEqual(group, groupEventItem.Group);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            EntityTypeEventBufferItem entityTypeEventItem = (EntityTypeEventBufferItem)result[1];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(clientAccountHashCode, entityTypeEventItem.HashCode);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);


            mockMetricLogger.ClearReceivedCalls();
            testAccessManagerTemporalEventCache.AddEntity(entityType, entity, eventId4, occurredTime4, clientAccountHashCode);

            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsRead>(), 3);
            Assert.AreEqual(3, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group1HashCode, groupEventItem.HashCode);
            Assert.AreEqual(group, groupEventItem.Group);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            entityTypeEventItem = (EntityTypeEventBufferItem)result[1];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(clientAccountHashCode, entityTypeEventItem.HashCode);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[2]);
            EntityEventBufferItem entityEventItem = (EntityEventBufferItem)result[2];
            Assert.AreEqual(eventId4, entityEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityEventItem.EventAction);
            Assert.AreEqual(occurredTime4, entityEventItem.OccurredTime);
            Assert.AreEqual(clientAccountHashCode, entityEventItem.HashCode);
            Assert.AreEqual(entityType, entityEventItem.EntityType);
            Assert.AreEqual(entity, entityEventItem.Entity);


            mockMetricLogger.ClearReceivedCalls();

            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId3);

            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsRead>(), 1);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[0]);
            entityEventItem = (EntityEventBufferItem)result[0];
            Assert.AreEqual(eventId4, entityEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityEventItem.EventAction);
            Assert.AreEqual(occurredTime4, entityEventItem.OccurredTime);
            Assert.AreEqual(clientAccountHashCode, entityEventItem.HashCode);
            Assert.AreEqual(entityType, entityEventItem.EntityType);
            Assert.AreEqual(entity, entityEventItem.Entity);


            mockMetricLogger.ClearReceivedCalls();

            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId4);

            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsRead>(), 0);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAllEventsSince_CacheIsTrimmedCorrectly()
        {
            const String user = "user1";
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000031");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000032");
            Guid eventId3 = Guid.Parse("00000000-0000-0000-0000-000000000033");
            Guid eventId4 = Guid.Parse("00000000-0000-0000-0000-000000000034");
            DateTime occurredTime1 = CreateDataTimeFromString("2022-09-25 14:04:10");
            DateTime occurredTime2 = CreateDataTimeFromString("2022-09-25 14:04:11");
            DateTime occurredTime3 = CreateDataTimeFromString("2022-09-25 14:04:12");
            DateTime occurredTime4 = CreateDataTimeFromString("2022-09-25 14:04:13");
            Int32 user1HashCode = 1;
            Int32 group1HashCode = 11;
            Int32 clientAccountHashCode = 21;
            mockMetricLogger.Begin(Arg.Any<CachedEventsReadTime>()).Returns(testBeginId);
            testAccessManagerTemporalEventCache = new AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                3,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
            testAccessManagerTemporalEventCache.AddUser(user, eventId1, occurredTime1, user1HashCode);
            testAccessManagerTemporalEventCache.AddGroup(group, eventId2, occurredTime2, group1HashCode);
            testAccessManagerTemporalEventCache.AddEntityType(entityType, eventId3, occurredTime3, clientAccountHashCode);

            IList<TemporalEventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsRead>(), 2);
            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            GroupEventBufferItem<String> groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group1HashCode, groupEventItem.HashCode);
            Assert.AreEqual(group, groupEventItem.Group);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            EntityTypeEventBufferItem entityTypeEventItem = (EntityTypeEventBufferItem)result[1];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(clientAccountHashCode, entityTypeEventItem.HashCode);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);


            mockMetricLogger.ClearReceivedCalls();
            testAccessManagerTemporalEventCache.AddEntity(entityType, entity, eventId4, occurredTime4, clientAccountHashCode);

            EventNotCachedException e = Assert.Throws<EventNotCachedException>(delegate
            {
                testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            });

            Assert.That(e.Message, Does.StartWith($"No event with eventId '{eventId1}' was found in the cache."));
            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.DidNotReceive().Add(Arg.Any<CachedEventsRead>(), Arg.Any<Int64>());


            mockMetricLogger.ClearReceivedCalls();

            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId2);

            mockMetricLogger.Received(1).Begin(Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<CachedEventsReadTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsRead>(), 2);
            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[0]);
            entityTypeEventItem = (EntityTypeEventBufferItem)result[0];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(clientAccountHashCode, entityTypeEventItem.HashCode);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            EntityEventBufferItem entityEventItem = (EntityEventBufferItem)result[1];
            Assert.AreEqual(eventId4, entityEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityEventItem.EventAction);
            Assert.AreEqual(occurredTime4, entityEventItem.OccurredTime);
            Assert.AreEqual(clientAccountHashCode, entityEventItem.HashCode);
            Assert.AreEqual(entityType, entityEventItem.EntityType);
            Assert.AreEqual(entity, entityEventItem.Entity);
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
