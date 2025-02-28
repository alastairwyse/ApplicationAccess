﻿/*
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
    /// Unit tests for the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkCache class.
    /// </summary>
    public class AccessManagerTemporalEventBulkCacheTests
    {
        private IMetricLogger mockMetricLogger;
        private IGuidProvider mockGuidProvider;
        private IDateTimeProvider mockDateTimeProvider;
        private AccessManagerTemporalEventBulkCache<String, String, ApplicationScreen, AccessLevel> testAccessManagerTemporalEventBulkCache;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            testAccessManagerTemporalEventBulkCache = new AccessManagerTemporalEventBulkCache<String, String, ApplicationScreen, AccessLevel>(2, mockMetricLogger, mockGuidProvider, mockDateTimeProvider);
        }

        [TearDown]
        protected void TearDown()
        {
            testAccessManagerTemporalEventBulkCache.Dispose();
        }

        [Test]
        public void PersistEvents()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000029");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000002A");
            var testEvent1 = new UserEventBufferItem<String>(eventId1, EventAction.Add, "user1", DateTime.UtcNow, 1);
            var testEvent2 = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>(eventId2, EventAction.Remove, "user1", ApplicationScreen.ManageProducts, AccessLevel.Modify, DateTime.UtcNow, 1);
            var testEvent3 = new GroupToEntityMappingEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "group1", "Clients", "CompanyA", DateTime.UtcNow, 11);
            var testEvents = new List<TemporalEventBufferItemBase>() { testEvent1, testEvent2, testEvent3 };
            mockMetricLogger.Begin(Arg.Any<EventsCachingTime>()).Returns(testBeginId);

            testAccessManagerTemporalEventBulkCache.PersistEvents(testEvents);

            IList<TemporalEventBufferItemBase> result = testAccessManagerTemporalEventBulkCache.GetAllEventsSince(eventId2);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventsCachingTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventsCachingTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsCached>(), 3);
            Assert.AreEqual(1, result.Count);
            Assert.AreSame(testEvent3, result[0]);


            mockMetricLogger.ClearReceivedCalls();
            testAccessManagerTemporalEventBulkCache = new AccessManagerTemporalEventBulkCache<String, String, ApplicationScreen, AccessLevel>(3, mockMetricLogger, mockGuidProvider, mockDateTimeProvider);

            testAccessManagerTemporalEventBulkCache.PersistEvents(testEvents);

            result = testAccessManagerTemporalEventBulkCache.GetAllEventsSince(eventId1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventsCachingTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventsCachingTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsCached>(), 3);
            Assert.AreEqual(2, result.Count);
            Assert.AreSame(testEvent2, result[0]);
            Assert.AreSame(testEvent3, result[1]);
        }
    }
}
