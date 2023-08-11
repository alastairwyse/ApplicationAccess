/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Validation;
using ApplicationMetrics;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Persistence.DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer class.
    /// </summary>
    /// <remarks>Tests a <see cref="DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer{TUser, TGroup, TComponent, TAccess}"/> where the validator injected into it wraps a <see cref="DependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance, whose event processor loops back to the <see cref="DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer{TUser, TGroup, TComponent, TAccess}"/> under test.  This simulates how these objects would be setup in a dependency-free writer node.</remarks>
    public class DependencyFreeAccessManagerTemporalEventBulkPersisterBufferIntegrationTests
    {
        protected IAccessManagerEventBufferFlushStrategy mockBufferFlushStrategy;
        protected IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        protected IMetricLogger mockMetricLogger;
        protected IGuidProvider mockGuidProvider;
        protected IDateTimeProvider mockDateTimeProvider;
        protected DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> testAccessManager;
        protected ConcurrentAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel> testEventValidator;
        protected DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testEventBulkPersisterBuffer;

        [SetUp]
        protected void SetUp()
        {
            mockBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            testAccessManager = new DependencyFreeAccessManager<string, string, ApplicationScreen, AccessLevel>(true);
            testEventValidator = new ConcurrentAccessManagerEventValidator<string, string, ApplicationScreen, AccessLevel>(testAccessManager);
            testEventBulkPersisterBuffer = new DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(testEventValidator, mockBufferFlushStrategy, mockEventPersister, mockMetricLogger, mockGuidProvider, mockDateTimeProvider);
            // Set the PersisterBuffer on the AccessManager to complete the 'loop back'
            testAccessManager.EventProcessor = testEventBulkPersisterBuffer;
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            var eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var eventId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var eventTime1 = CreateDataTimeFromString("2023-08-05 12:32:04");
            var eventTime2 = CreateDataTimeFromString("2023-08-05 12:32:05");
            var eventTime3 = CreateDataTimeFromString("2023-08-05 12:32:06");
            const String user = "user1";
            const String group = "group1";
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                eventId1,
                eventId2,
                eventId3
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                eventTime1,
                eventTime2,
                eventTime3
            );

            testEventBulkPersisterBuffer.AddUserToGroupMapping(user, group);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).UserToGroupMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testEventBulkPersisterBuffer.UserEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.GroupEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.UserToGroupMappingEventBuffer.Count);
            UserEventBufferItem<String> bufferedUserEvent = testEventBulkPersisterBuffer.UserEventBuffer.First.Value.Item1; 
            Assert.AreEqual(eventId1, bufferedUserEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedUserEvent.EventAction);
            Assert.AreEqual(user, bufferedUserEvent.User);
            Assert.AreEqual(eventTime1, bufferedUserEvent.OccurredTime);
            GroupEventBufferItem<String> bufferedGroupEvent = testEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId2, bufferedGroupEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedGroupEvent.EventAction);
            Assert.AreEqual(group, bufferedGroupEvent.Group);
            Assert.AreEqual(eventTime2, bufferedGroupEvent.OccurredTime);
            UserToGroupMappingEventBufferItem<String, String> bufferedUserToGroupEvent = testEventBulkPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId3, bufferedUserToGroupEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedUserToGroupEvent.EventAction);
            Assert.AreEqual(user, bufferedUserToGroupEvent.User);
            Assert.AreEqual(group, bufferedUserToGroupEvent.Group);
            Assert.AreEqual(eventTime3, bufferedUserToGroupEvent.OccurredTime);
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            var eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var eventId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var eventTime1 = CreateDataTimeFromString("2023-08-05 12:32:04");
            var eventTime2 = CreateDataTimeFromString("2023-08-05 12:32:05");
            var eventTime3 = CreateDataTimeFromString("2023-08-05 12:32:06");
            const String fromGroup = "group1";
            const String toGroup = "group2";
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                eventId1,
                eventId2,
                eventId3
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                eventTime1,
                eventTime2,
                eventTime3
            );

            testEventBulkPersisterBuffer.AddGroupToGroupMapping(fromGroup, toGroup);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 2;
            mockBufferFlushStrategy.Received(1).GroupToGroupMappingEventBufferItemCount = 1;
            Assert.AreEqual(2, testEventBulkPersisterBuffer.GroupEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.GroupToGroupMappingEventBuffer.Count);
            GroupEventBufferItem<String> bufferedFromGroupEvent = testEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId1, bufferedFromGroupEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedFromGroupEvent.EventAction);
            Assert.AreEqual(fromGroup, bufferedFromGroupEvent.Group);
            Assert.AreEqual(eventTime1, bufferedFromGroupEvent.OccurredTime);
            GroupEventBufferItem<String> bufferedToGroupEvent = testEventBulkPersisterBuffer.GroupEventBuffer.First.Next.Value.Item1;
            Assert.AreEqual(eventId2, bufferedToGroupEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedToGroupEvent.EventAction);
            Assert.AreEqual(toGroup, bufferedToGroupEvent.Group);
            Assert.AreEqual(eventTime2, bufferedToGroupEvent.OccurredTime);
            GroupToGroupMappingEventBufferItem<String> bufferedGroupToGroupEvent = testEventBulkPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId3, bufferedGroupToGroupEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedGroupToGroupEvent.EventAction);
            Assert.AreEqual(fromGroup, bufferedGroupToGroupEvent.FromGroup);
            Assert.AreEqual(toGroup, bufferedGroupToGroupEvent.ToGroup);
            Assert.AreEqual(eventTime3, bufferedGroupToGroupEvent.OccurredTime);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            var eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var eventTime1 = CreateDataTimeFromString("2023-08-05 12:32:04");
            var eventTime2 = CreateDataTimeFromString("2023-08-05 12:32:05");
            const String user = "user1";
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                eventId1,
                eventId2
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                eventTime1,
                eventTime2
            );

            testEventBulkPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.ManageProducts, AccessLevel.Delete);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testEventBulkPersisterBuffer.UserEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            UserEventBufferItem<String> bufferedUserEvent = testEventBulkPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId1, bufferedUserEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedUserEvent.EventAction);
            Assert.AreEqual(user, bufferedUserEvent.User);
            Assert.AreEqual(eventTime1, bufferedUserEvent.OccurredTime);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedUserToApplicationComponentAndAccessLevelEvent = testEventBulkPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId2, bufferedUserToApplicationComponentAndAccessLevelEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedUserToApplicationComponentAndAccessLevelEvent.EventAction);
            Assert.AreEqual(user, bufferedUserToApplicationComponentAndAccessLevelEvent.User);
            Assert.AreEqual(ApplicationScreen.ManageProducts, bufferedUserToApplicationComponentAndAccessLevelEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Delete, bufferedUserToApplicationComponentAndAccessLevelEvent.AccessLevel);
            Assert.AreEqual(eventTime2, bufferedUserToApplicationComponentAndAccessLevelEvent.OccurredTime);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            var eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var eventTime1 = CreateDataTimeFromString("2023-08-05 12:32:04");
            var eventTime2 = CreateDataTimeFromString("2023-08-05 12:32:05");
            const String group = "group1";
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                eventId1,
                eventId2
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                eventTime1,
                eventTime2
            );

            testEventBulkPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Summary, AccessLevel.View);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testEventBulkPersisterBuffer.GroupEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            GroupEventBufferItem<String> bufferedGroupEvent = testEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId1, bufferedGroupEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedGroupEvent.EventAction);
            Assert.AreEqual(group, bufferedGroupEvent.Group);
            Assert.AreEqual(eventTime1, bufferedGroupEvent.OccurredTime);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedGroupToApplicationComponentAndAccessLevelEvent = testEventBulkPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId2, bufferedGroupToApplicationComponentAndAccessLevelEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedGroupToApplicationComponentAndAccessLevelEvent.EventAction);
            Assert.AreEqual(group, bufferedGroupToApplicationComponentAndAccessLevelEvent.Group);
            Assert.AreEqual(ApplicationScreen.Summary, bufferedGroupToApplicationComponentAndAccessLevelEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.View, bufferedGroupToApplicationComponentAndAccessLevelEvent.AccessLevel);
            Assert.AreEqual(eventTime2, bufferedGroupToApplicationComponentAndAccessLevelEvent.OccurredTime);
        }

        [Test]
        public void AddEntity()
        {
            var eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var eventTime1 = CreateDataTimeFromString("2023-08-05 12:32:04");
            var eventTime2 = CreateDataTimeFromString("2023-08-05 12:32:05");
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                eventId1,
                eventId2
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                eventTime1,
                eventTime2
            );

            testEventBulkPersisterBuffer.AddEntity(entityType, entity);

            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            Assert.AreEqual(1, testEventBulkPersisterBuffer.EntityTypeEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.EntityEventBuffer.Count);
            EntityTypeEventBufferItem bufferedEntityTypeEvent = testEventBulkPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId1, bufferedEntityTypeEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEntityTypeEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEntityTypeEvent.EntityType);
            Assert.AreEqual(eventTime1, bufferedEntityTypeEvent.OccurredTime);
            EntityEventBufferItem bufferedEntityEvent = testEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId2, bufferedEntityEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEntityEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEntityEvent.EntityType);
            Assert.AreEqual(entity, bufferedEntityEvent.Entity);
            Assert.AreEqual(eventTime2, bufferedEntityEvent.OccurredTime);
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            var eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var eventId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var eventId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var eventTime1 = CreateDataTimeFromString("2023-08-05 12:32:04");
            var eventTime2 = CreateDataTimeFromString("2023-08-05 12:32:05");
            var eventTime3 = CreateDataTimeFromString("2023-08-05 12:32:06");
            var eventTime4 = CreateDataTimeFromString("2023-08-05 12:32:07");
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                eventId1,
                eventId2,
                eventId3,
                eventId4
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                eventTime1,
                eventTime2,
                eventTime3,
                eventTime4
            );

            testEventBulkPersisterBuffer.AddUserToEntityMapping(user, entityType, entity);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).UserToEntityMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testEventBulkPersisterBuffer.UserEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.EntityTypeEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.EntityEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.UserToEntityMappingEventBuffer.Count);
            UserEventBufferItem<String> bufferedUserEvent = testEventBulkPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId1, bufferedUserEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedUserEvent.EventAction);
            Assert.AreEqual(user, bufferedUserEvent.User);
            Assert.AreEqual(eventTime1, bufferedUserEvent.OccurredTime);
            EntityTypeEventBufferItem bufferedEntityTypeEvent = testEventBulkPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId2, bufferedEntityTypeEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEntityTypeEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEntityTypeEvent.EntityType);
            Assert.AreEqual(eventTime2, bufferedEntityTypeEvent.OccurredTime);
            EntityEventBufferItem bufferedEntityEvent = testEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId3, bufferedEntityEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEntityEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEntityEvent.EntityType);
            Assert.AreEqual(entity, bufferedEntityEvent.Entity);
            Assert.AreEqual(eventTime3, bufferedEntityEvent.OccurredTime);
            UserToEntityMappingEventBufferItem<String> bufferedUserToEntityEvent = testEventBulkPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId4, bufferedUserToEntityEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedUserToEntityEvent.EventAction);
            Assert.AreEqual(user, bufferedUserToEntityEvent.User);
            Assert.AreEqual(entityType, bufferedUserToEntityEvent.EntityType);
            Assert.AreEqual(entity, bufferedUserToEntityEvent.Entity);
            Assert.AreEqual(eventTime4, bufferedUserToEntityEvent.OccurredTime);
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            var eventId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var eventId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var eventId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var eventId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var eventTime1 = CreateDataTimeFromString("2023-08-05 12:32:04");
            var eventTime2 = CreateDataTimeFromString("2023-08-05 12:32:05");
            var eventTime3 = CreateDataTimeFromString("2023-08-05 12:32:06");
            var eventTime4 = CreateDataTimeFromString("2023-08-05 12:32:07");
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                eventId1,
                eventId2,
                eventId3,
                eventId4
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                eventTime1,
                eventTime2,
                eventTime3,
                eventTime4
            );

            testEventBulkPersisterBuffer.AddGroupToEntityMapping(group, entityType, entity);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            mockBufferFlushStrategy.Received(1).GroupToEntityMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testEventBulkPersisterBuffer.GroupEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.EntityTypeEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.EntityEventBuffer.Count);
            Assert.AreEqual(1, testEventBulkPersisterBuffer.GroupToEntityMappingEventBuffer.Count);
            GroupEventBufferItem<String> bufferedGroupEvent = testEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId1, bufferedGroupEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedGroupEvent.EventAction);
            Assert.AreEqual(group, bufferedGroupEvent.Group);
            Assert.AreEqual(eventTime1, bufferedGroupEvent.OccurredTime);
            EntityTypeEventBufferItem bufferedEntityTypeEvent = testEventBulkPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId2, bufferedEntityTypeEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEntityTypeEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEntityTypeEvent.EntityType);
            Assert.AreEqual(eventTime2, bufferedEntityTypeEvent.OccurredTime);
            EntityEventBufferItem bufferedEntityEvent = testEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId3, bufferedEntityEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEntityEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEntityEvent.EntityType);
            Assert.AreEqual(entity, bufferedEntityEvent.Entity);
            Assert.AreEqual(eventTime3, bufferedEntityEvent.OccurredTime);
            GroupToEntityMappingEventBufferItem<String> bufferedGroupToEntityEvent = testEventBulkPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId4, bufferedGroupToEntityEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedGroupToEntityEvent.EventAction);
            Assert.AreEqual(group, bufferedGroupToEntityEvent.Group);
            Assert.AreEqual(entityType, bufferedGroupToEntityEvent.EntityType);
            Assert.AreEqual(entity, bufferedGroupToEntityEvent.Entity);
            Assert.AreEqual(eventTime4, bufferedGroupToEntityEvent.OccurredTime);
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

        #region Nested Classes

        /// <summary>
        /// Version of the DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        protected class DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>The queue used to buffer user events.</summary>
            public LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>> UserEventBuffer
            {
                get { return userEventBuffer; }
            }

            /// <summary>The queue used to buffer group events.</summary>
            public LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>> GroupEventBuffer
            {
                get { return groupEventBuffer; }
            }

            /// <summary>The queue used to buffer user to group mapping events.</summary>
            public LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>> UserToGroupMappingEventBuffer
            {
                get { return userToGroupMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer group to group mapping events.</summary>
            public LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>> GroupToGroupMappingEventBuffer
            {
                get { return groupToGroupMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer user to application component and access level mapping events.</summary>
            public LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>> UserToApplicationComponentAndAccessLevelMappingEventBuffer
            {
                get { return userToApplicationComponentAndAccessLevelMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer group to application component and access level mapping events.</summary>
            public LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>> GroupToApplicationComponentAndAccessLevelMappingEventBuffer
            {
                get { return groupToApplicationComponentAndAccessLevelMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer entity type events.</summary>
            public LinkedList<Tuple<EntityTypeEventBufferItem, Int64>> EntityTypeEventBuffer
            {
                get { return entityTypeEventBuffer; }
            }

            /// <summary>The queue used to buffer entity events.</summary>
            public LinkedList<Tuple<EntityEventBufferItem, Int64>> EntityEventBuffer
            {
                get { return entityEventBuffer; }
            }

            /// <summary>The queue used to buffer user to entity mapping events.</summary>
            public LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>> UserToEntityMappingEventBuffer
            {
                get { return userToEntityMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer group to entity mapping events.</summary>
            public LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>> GroupToEntityMappingEventBuffer
            {
                get { return groupToEntityMappingEventBuffer; }
            }

            /// <summary>Lock objects for the user event queue.</summary>
            public Object UserEventBufferLock
            {
                get { return userEventBufferLock; }
            }

            /// <summary>Lock objects for the group event queue.</summary>
            public Object GroupEventBufferLock
            {
                get { return groupEventBufferLock; }
            }

            /// <summary>Lock objects for the user to group mapping event queue.</summary>
            public Object UserToGroupMappingEventBufferLock
            {
                get { return userToGroupMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the group to group mapping event queue.</summary>
            public Object GroupToGroupMappingEventBufferLock
            {
                get { return groupToGroupMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the user to application component and access level mapping event queue.</summary>
            public Object UserToApplicationComponentAndAccessLevelMappingEventBufferLock
            {
                get { return userToApplicationComponentAndAccessLevelMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the group to application component and access level mapping event queue.</summary>
            public Object GroupToApplicationComponentAndAccessLevelMappingEventBufferLock
            {
                get { return groupToApplicationComponentAndAccessLevelMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the entity type event queue.</summary>
            public Object EntityTypeEventBufferLock
            {
                get { return entityTypeEventBufferLock; }
            }

            /// <summary>Lock objects for the entity event queue.</summary>
            public Object EntityEventBufferLock
            {
                get { return entityEventBufferLock; }
            }

            /// <summary>Lock objects for the user to entity mapping event queue.</summary>
            public Object UserToEntityMappingEventBufferLock
            {
                get { return userToEntityMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the group to entity mapping event queue.</summary>
            public Object GroupToEntityMappingEventBufferLock
            {
                get { return groupToEntityMappingEventBufferLock; }
            }

            /// <summary>Lock object for the 'lastEventSequenceNumber' and 'dateTimeProvider' members, to ensure that their sequence orders are maintained between queuing of different events.</summary>
            public Object EventSequenceNumberLock
            {
                get { return eventSequenceNumberLock; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer+DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers class.
            /// </summary>
            /// <param name="eventValidator">The validator to use to validate events.</param>
            /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
            /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <param name="guidProvider">The provider to use for random Guids.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            public DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers
            (
                IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
                IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
                IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
                IMetricLogger metricLogger,
                IGuidProvider guidProvider,
                IDateTimeProvider dateTimeProvider
            )
                : base(eventValidator, bufferFlushStrategy, eventPersister, metricLogger, guidProvider, dateTimeProvider)
            {
            }
        }

        #endregion
    }
}
