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
using System.Globalization;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.AccessManagerTemporalEventCache class.
    /// </summary>
    public class AccessManagerTemporalEventCacheTests
    {
        private IGuidProvider mockGuidProvider;
        private IDateTimeProvider mockDateTimeProvider;
        private AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel> testAccessManagerTemporalEventCache;

        [SetUp]
        protected void SetUp()
        {
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            testAccessManagerTemporalEventCache = new AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>(2, mockGuidProvider, mockDateTimeProvider);
        }

        [Test]
        public void Constructor_CachedEventCountLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testAccessManagerTemporalEventCache = new AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>(0, mockGuidProvider, mockDateTimeProvider);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'cachedEventCount' must be greater than or equal to 1."));
            Assert.AreEqual("cachedEventCount", e.ParamName);
        }

        [Test]
        public void AddUser()
        {
            const String user = "user1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:11:58");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddUser("throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddUser(user);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<UserEventBufferItem<String>>(result[0]);
            UserEventBufferItem<String> eventItem = (UserEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
        }

        [Test]
        public void RemoveUser()
        {
            const String user = "user1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:27:59");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveUser("throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveUser(user);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<UserEventBufferItem<String>>(result[0]);
            UserEventBufferItem<String> eventItem = (UserEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
        }

        [Test]
        public void AddGroup()
        {
            const String group = "group1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000006");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:28:20");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddGroup("throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddGroup(group);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            GroupEventBufferItem<String> eventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(group, eventItem.Group);
        }

        [Test]
        public void RemoveGroup()
        {
            const String group = "group1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000007");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000008");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:28:25");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveGroup("throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveGroup(group);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            GroupEventBufferItem<String> eventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(group, eventItem.Group);
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            const String user = "user1";
            const String group = "group1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000009");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000000A");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:35:44");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddUserToGroupMapping("throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddUserToGroupMapping(user, group);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<UserToGroupMappingEventBufferItem<String, String>>(result[0]);
            UserToGroupMappingEventBufferItem<String, String> eventItem = (UserToGroupMappingEventBufferItem<String, String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
            Assert.AreEqual(group, eventItem.Group);
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            const String user = "user1";
            const String group = "group1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000000B");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000000C");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:35:49");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveUserToGroupMapping("throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveUserToGroupMapping(user, group);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<UserToGroupMappingEventBufferItem<String, String>>(result[0]);
            UserToGroupMappingEventBufferItem<String, String> eventItem = (UserToGroupMappingEventBufferItem<String, String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
            Assert.AreEqual(group, eventItem.Group);
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000000D");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000000E");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:35:58");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddGroupToGroupMapping("throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddGroupToGroupMapping(fromGroup, toGroup);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupToGroupMappingEventBufferItem<String>>(result[0]);
            GroupToGroupMappingEventBufferItem<String> eventItem = (GroupToGroupMappingEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(fromGroup, eventItem.FromGroup);
            Assert.AreEqual(toGroup, eventItem.ToGroup);
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000000F");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000010");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:36:02");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveGroupToGroupMapping("throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveGroupToGroupMapping(fromGroup, toGroup);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupToGroupMappingEventBufferItem<String>>(result[0]);
            GroupToGroupMappingEventBufferItem<String> eventItem = (GroupToGroupMappingEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(fromGroup, eventItem.FromGroup);
            Assert.AreEqual(toGroup, eventItem.ToGroup);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            const String user = "user1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000011");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000012");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:39:10");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddUserToApplicationComponentAndAccessLevelMapping("throwaway", ApplicationScreen.Summary, AccessLevel.View, eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Order, AccessLevel.Create);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(result[0]);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> eventItem = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
            Assert.AreEqual(ApplicationScreen.Order, eventItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Create, eventItem.AccessLevel);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            const String user = "user1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000013");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000014");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 10:39:16");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveUserToApplicationComponentAndAccessLevelMapping("throwaway", ApplicationScreen.Summary, AccessLevel.View, eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Order, AccessLevel.Create);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);

            Assert.IsInstanceOf<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(result[0]);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> eventItem = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
            Assert.AreEqual(ApplicationScreen.Order, eventItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Create, eventItem.AccessLevel);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            const String group = "group1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000015");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000016");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:16:58");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddGroupToApplicationComponentAndAccessLevelMapping("throwaway", ApplicationScreen.Summary, AccessLevel.View, eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Settings, AccessLevel.Modify);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(result[0]);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> eventItem = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(group, eventItem.Group);
            Assert.AreEqual(ApplicationScreen.Settings, eventItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Modify, eventItem.AccessLevel);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            const String group = "group1";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000017");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000018");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:17:04");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveGroupToApplicationComponentAndAccessLevelMapping("throwaway", ApplicationScreen.Summary, AccessLevel.View, eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Settings, AccessLevel.Modify);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(result[0]);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> eventItem = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(group, eventItem.Group);
            Assert.AreEqual(ApplicationScreen.Settings, eventItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Modify, eventItem.AccessLevel);
        }

        [Test]
        public void AddEntityType()
        {
            const String entityType = "ClientAccount";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000019");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000001A");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:21:47");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddEntityType("throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddEntityType(entityType);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[0]);
            EntityTypeEventBufferItem eventItem = (EntityTypeEventBufferItem)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(entityType, eventItem.EntityType);
        }

        [Test]
        public void RemoveEntityType()
        {
            const String entityType = "ClientAccount";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000001B");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000001C");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:21:50");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveEntityType("throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveEntityType(entityType);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[0]);
            EntityTypeEventBufferItem eventItem = (EntityTypeEventBufferItem)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(entityType, eventItem.EntityType);
        }

        [Test]
        public void AddEntity()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000001D");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000001E");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:25:59");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddEntity("throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddEntity(entityType, entity);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<EntityEventBufferItem>(result[0]);
            EntityEventBufferItem eventItem = (EntityEventBufferItem)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(entityType, eventItem.EntityType);
            Assert.AreEqual(entity, eventItem.Entity);
        }

        [Test]
        public void RemoveEntity()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000001F");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000020");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:26:01");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveEntity("throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveEntity(entityType, entity);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<EntityEventBufferItem>(result[0]);
            EntityEventBufferItem eventItem = (EntityEventBufferItem)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(entityType, eventItem.EntityType);
            Assert.AreEqual(entity, eventItem.Entity);
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000021");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000022");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:28:58");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddUserToEntityMapping(user, "throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddUserToEntityMapping(user, entityType, entity);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<UserToEntityMappingEventBufferItem<String>>(result[0]);
            UserToEntityMappingEventBufferItem<String> eventItem = (UserToEntityMappingEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
            Assert.AreEqual(entityType, eventItem.EntityType);
            Assert.AreEqual(entity, eventItem.Entity);
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000023");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000024");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:29:02");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveUserToEntityMapping(user, "throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveUserToEntityMapping(user, entityType, entity);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<UserToEntityMappingEventBufferItem<String>>(result[0]);
            UserToEntityMappingEventBufferItem<String> eventItem = (UserToEntityMappingEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(user, eventItem.User);
            Assert.AreEqual(entityType, eventItem.EntityType);
            Assert.AreEqual(entity, eventItem.Entity);
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000025");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000026");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:32:43");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.AddGroupToEntityMapping(group, "throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.AddGroupToEntityMapping(group, entityType, entity);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupToEntityMappingEventBufferItem<String>>(result[0]);
            GroupToEntityMappingEventBufferItem<String> eventItem = (GroupToEntityMappingEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Add, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(group, eventItem.Group);
            Assert.AreEqual(entityType, eventItem.EntityType);
            Assert.AreEqual(entity, eventItem.Entity);
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000027");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000028");
            DateTime occurredTime = CreateDataTimeFromString("2022-09-25 13:32:47");
            mockGuidProvider.NewGuid().Returns(eventId2);
            mockDateTimeProvider.UtcNow().Returns(occurredTime);
            testAccessManagerTemporalEventCache.RemoveGroupToEntityMapping(group, "throwaway", "throwaway", eventId1, DateTime.UtcNow);

            testAccessManagerTemporalEventCache.RemoveGroupToEntityMapping(group, entityType, entity);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            mockGuidProvider.Received(1).NewGuid();
            mockDateTimeProvider.Received(1).UtcNow();
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupToEntityMappingEventBufferItem<String>>(result[0]);
            GroupToEntityMappingEventBufferItem<String> eventItem = (GroupToEntityMappingEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, eventItem.EventId);
            Assert.AreEqual(EventAction.Remove, eventItem.EventAction);
            Assert.AreEqual(occurredTime, eventItem.OccurredTime);
            Assert.AreEqual(group, eventItem.Group);
            Assert.AreEqual(entityType, eventItem.EntityType);
            Assert.AreEqual(entity, eventItem.Entity);
        }

        [Test]
        public void GetAllEventsSince_EventWithIdDoesntExist()
        {
            Guid testEventId1 = Guid.Parse("00000000-0000-0000-0000-000000000029");

            var e = Assert.Throws<EventNotCachedException>(delegate
            {
                testAccessManagerTemporalEventCache.GetAllEventsSince(testEventId1);
            });

            Assert.That(e.Message, Does.StartWith("No event with eventId '00000000-0000-0000-0000-000000000029' was found in the cache."));


            testAccessManagerTemporalEventCache.AddUser("user1", Guid.Parse("00000000-0000-0000-0000-00000000002A"), DateTime.UtcNow);
            testAccessManagerTemporalEventCache.AddUserToGroupMapping("user1", "group1", Guid.Parse("00000000-0000-0000-0000-00000000002B"), DateTime.UtcNow);
            testAccessManagerTemporalEventCache.AddEntity("ClientAccount", "CompanyA", Guid.Parse("00000000-0000-0000-0000-00000000002C"), DateTime.UtcNow);

            e = Assert.Throws<EventNotCachedException>(delegate
            {
                testAccessManagerTemporalEventCache.GetAllEventsSince(testEventId1);
            });

            Assert.That(e.Message, Does.StartWith("No event with eventId '00000000-0000-0000-0000-000000000029' was found in the cache."));
        }

        [Test]
        public void GetAllEventsSince()
        {
            const String user = "user1";
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-00000000002D");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-00000000002E");
            Guid eventId3 = Guid.Parse("00000000-0000-0000-0000-00000000002F");
            Guid eventId4 = Guid.Parse("00000000-0000-0000-0000-000000000030");
            DateTime occurredTime1 = CreateDataTimeFromString("2022-09-25 13:51:36");
            DateTime occurredTime2 = CreateDataTimeFromString("2022-09-25 13:51:37");
            DateTime occurredTime3 = CreateDataTimeFromString("2022-09-25 13:51:38");
            DateTime occurredTime4 = CreateDataTimeFromString("2022-09-25 13:51:39");
            testAccessManagerTemporalEventCache = new AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>(5, mockGuidProvider, mockDateTimeProvider);
            testAccessManagerTemporalEventCache.AddUser(user, eventId1, occurredTime1);
            testAccessManagerTemporalEventCache.AddGroup(group, eventId2, occurredTime2);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            GroupEventBufferItem<String> groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group, groupEventItem.Group);


            testAccessManagerTemporalEventCache.AddEntityType(entityType, eventId3, occurredTime3);

            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group, groupEventItem.Group);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            EntityTypeEventBufferItem entityTypeEventItem = (EntityTypeEventBufferItem)result[1];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);


            testAccessManagerTemporalEventCache.AddEntity(entityType, entity, eventId4, occurredTime4);

            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            Assert.AreEqual(3, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group, groupEventItem.Group);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            entityTypeEventItem = (EntityTypeEventBufferItem)result[1];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[2]);
            EntityEventBufferItem entityEventItem = (EntityEventBufferItem)result[2];
            Assert.AreEqual(eventId4, entityEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityEventItem.EventAction);
            Assert.AreEqual(occurredTime4, entityEventItem.OccurredTime);
            Assert.AreEqual(entityType, entityEventItem.EntityType);
            Assert.AreEqual(entity, entityEventItem.Entity);


            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId3);
            
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[0]);
            entityEventItem = (EntityEventBufferItem)result[0];
            Assert.AreEqual(eventId4, entityEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityEventItem.EventAction);
            Assert.AreEqual(occurredTime4, entityEventItem.OccurredTime);
            Assert.AreEqual(entityType, entityEventItem.EntityType);
            Assert.AreEqual(entity, entityEventItem.Entity);


            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId4);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetAllEventsSince_CacheIsTrimmedCorrectly()
        {
            const String user = "user1";
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            Guid eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000031");
            Guid eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000032");
            Guid eventId3 = Guid.Parse("00000000-0000-0000-0000-000000000033");
            Guid eventId4 = Guid.Parse("00000000-0000-0000-0000-000000000034");
            DateTime occurredTime1 = CreateDataTimeFromString("2022-09-25 14:04:10");
            DateTime occurredTime2 = CreateDataTimeFromString("2022-09-25 14:04:11");
            DateTime occurredTime3 = CreateDataTimeFromString("2022-09-25 14:04:12");
            DateTime occurredTime4 = CreateDataTimeFromString("2022-09-25 14:04:13");
            testAccessManagerTemporalEventCache = new AccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>(3, mockGuidProvider, mockDateTimeProvider);
            testAccessManagerTemporalEventCache.AddUser(user, eventId1, occurredTime1);
            testAccessManagerTemporalEventCache.AddGroup(group, eventId2, occurredTime2);
            testAccessManagerTemporalEventCache.AddEntityType(entityType, eventId3, occurredTime3);

            IList<EventBufferItemBase> result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);

            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<GroupEventBufferItem<String>>(result[0]);
            GroupEventBufferItem<String> groupEventItem = (GroupEventBufferItem<String>)result[0];
            Assert.AreEqual(eventId2, groupEventItem.EventId);
            Assert.AreEqual(EventAction.Add, groupEventItem.EventAction);
            Assert.AreEqual(occurredTime2, groupEventItem.OccurredTime);
            Assert.AreEqual(group, groupEventItem.Group);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            EntityTypeEventBufferItem entityTypeEventItem = (EntityTypeEventBufferItem)result[1];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);


            testAccessManagerTemporalEventCache.AddEntity(entityType, entity, eventId4, occurredTime4);

            EventNotCachedException e = Assert.Throws<EventNotCachedException>(delegate
            {
                testAccessManagerTemporalEventCache.GetAllEventsSince(eventId1);
            });

            Assert.That(e.Message, Does.StartWith($"No event with eventId '{eventId1}' was found in the cache."));


            result = testAccessManagerTemporalEventCache.GetAllEventsSince(eventId2);

            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[0]);
            entityTypeEventItem = (EntityTypeEventBufferItem)result[0];
            Assert.AreEqual(eventId3, entityTypeEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityTypeEventItem.EventAction);
            Assert.AreEqual(occurredTime3, entityTypeEventItem.OccurredTime);
            Assert.AreEqual(entityType, entityTypeEventItem.EntityType);
            Assert.IsInstanceOf<EntityTypeEventBufferItem>(result[1]);
            EntityEventBufferItem entityEventItem = (EntityEventBufferItem)result[1];
            Assert.AreEqual(eventId4, entityEventItem.EventId);
            Assert.AreEqual(EventAction.Add, entityEventItem.EventAction);
            Assert.AreEqual(occurredTime4, entityEventItem.OccurredTime);
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
