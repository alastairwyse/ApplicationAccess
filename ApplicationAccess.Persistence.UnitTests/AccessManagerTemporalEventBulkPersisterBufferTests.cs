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
using System.Linq;
using System.Globalization;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Validation;
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.AccessManagerTemporalEventBulkPersisterBuffer class.
    /// </summary>
    public class AccessManagerTemporalEventBulkPersisterBufferTests
    {
        protected IAccessManagerEventBufferFlushStrategy mockBufferFlushStrategy;
        protected IHashCodeGenerator<String> mockUserHashCodeGenerator;
        protected IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        protected IHashCodeGenerator<String> mockEntityTypeHashCodeGenerator;
        protected IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        protected IMetricLogger mockMetricLogger;
        protected IGuidProvider mockGuidProvider;
        protected IDateTimeProvider mockDateTimeProvider;
        protected AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel> testAccessManagerTemporalEventBulkPersisterBuffer;

        [SetUp]
        protected void SetUp()
        {
            mockBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEntityTypeHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            testAccessManagerTemporalEventBulkPersisterBuffer = new AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>
            (
                new NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>(), 
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator, 
                mockEventPersister,
                mockMetricLogger, 
                mockGuidProvider,
                mockDateTimeProvider
            );
        }

        [Test]
        public void Flush_CallToPersisterFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            Int32 hashCode = -20;
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Failed to persist events.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockMetricLogger.Begin(Arg.Any<FlushTime>()).Returns(testBeginId);
            mockEventPersister.When((eventPersister) => eventPersister.PersistEvents(Arg.Any<IList<TemporalEventBufferItemBase>>())).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventBulkPersisterBuffer.AddUser(user);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventBulkPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to process buffers and persist flushed events."));
            Assert.AreEqual(mockException, e.InnerException);
            mockMetricLogger.Received(1).Begin(Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<FlushTime>());
            mockMetricLogger.DidNotReceive().Add(Arg.Any<BufferedEventsFlushed>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<BufferFlushOperationCompleted>());
        }

        [Test]
        public void Flush()
        {
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var guid5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var guid6 = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var guid7 = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var guid8 = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var guid9 = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var guid10 = Guid.Parse("00000000-0000-0000-0000-00000000000a");
            var guid11 = Guid.Parse("00000000-0000-0000-0000-00000000000b");
            var guid12 = Guid.Parse("00000000-0000-0000-0000-00000000000c");
            var guid13 = Guid.Parse("00000000-0000-0000-0000-00000000000d");
            var guid14 = Guid.Parse("00000000-0000-0000-0000-00000000000e");
            var guid15 = Guid.Parse("00000000-0000-0000-0000-00000000000f");
            var guid16 = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var guid17 = Guid.Parse("00000000-0000-0000-0000-000000000011");
            var guid18 = Guid.Parse("00000000-0000-0000-0000-000000000012");
            var guid19 = Guid.Parse("00000000-0000-0000-0000-000000000013");
            var guid20 = Guid.Parse("00000000-0000-0000-0000-000000000014");
            var guid21 = Guid.Parse("00000000-0000-0000-0000-000000000015");
            var guid22 = Guid.Parse("00000000-0000-0000-0000-000000000016");
            mockBufferFlushStrategy.ClearReceivedCalls();

            Int32 user1HashCode = 1;
            Int32 group1HashCode = 11;
            Int32 group2HashCode = 12;
            Int32 clientsHashCode = 21;
            mockUserHashCodeGenerator.GetHashCode("user1").Returns(user1HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group1").Returns(group1HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group2").Returns(group2HashCode);
            mockEntityTypeHashCodeGenerator.GetHashCode("Clients").Returns(clientsHashCode);

            mockGuidProvider.NewGuid().Returns<Guid>
            (
                guid1,
                guid2,
                guid3,
                guid4,
                guid5,
                guid6,
                guid7,
                guid8,
                guid9,
                guid10,
                guid11,
                guid12,
                guid13,
                guid14,
                guid15,
                guid16,
                guid17,
                guid18,
                guid19,
                guid20,
                guid21,
                guid22
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                CreateDataTimeFromString("2021-06-12 13:43:01"),
                CreateDataTimeFromString("2021-06-12 13:43:02"),
                CreateDataTimeFromString("2021-06-12 13:43:03"),
                CreateDataTimeFromString("2021-06-12 13:43:04"),
                CreateDataTimeFromString("2021-06-12 13:43:05"),
                CreateDataTimeFromString("2021-06-12 13:43:06"),
                CreateDataTimeFromString("2021-06-12 13:43:07"),
                CreateDataTimeFromString("2021-06-12 13:43:08"),
                CreateDataTimeFromString("2021-06-12 13:43:09"),
                CreateDataTimeFromString("2021-06-12 13:43:10"),
                CreateDataTimeFromString("2021-06-12 13:43:11"),
                CreateDataTimeFromString("2021-06-12 13:43:12"),
                CreateDataTimeFromString("2021-06-12 13:43:13"),
                CreateDataTimeFromString("2021-06-12 13:43:14"),
                CreateDataTimeFromString("2021-06-12 13:43:15"),
                CreateDataTimeFromString("2021-06-12 13:43:16"),
                CreateDataTimeFromString("2021-06-12 13:43:17"),
                CreateDataTimeFromString("2021-06-12 13:43:18"),
                CreateDataTimeFromString("2021-06-12 13:43:19"),
                CreateDataTimeFromString("2021-06-12 13:43:20"),
                CreateDataTimeFromString("2021-06-12 13:43:21"),
                CreateDataTimeFromString("2021-06-12 13:43:22")
            );
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<FlushTime>()).Returns(testBeginId);
            IList<TemporalEventBufferItemBase> capturedPersistedEvents = null;
            mockEventPersister.PersistEvents(Arg.Do<IList<TemporalEventBufferItemBase>>(events => capturedPersistedEvents = events));

            testAccessManagerTemporalEventBulkPersisterBuffer.AddUser("user1");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddGroup("group1");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddGroup("group2");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddUserToGroupMapping("user1", "group1");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddGroupToGroupMapping("group1", "group2");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManagerTemporalEventBulkPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManagerTemporalEventBulkPersisterBuffer.AddEntityType("Clients");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddEntity("Clients", "CompanyA");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddUserToEntityMapping("user1", "Clients", "CompanyA");
            testAccessManagerTemporalEventBulkPersisterBuffer.AddGroupToEntityMapping("group2", "Clients", "CompanyA");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroupToEntityMapping("group2", "Clients", "CompanyA");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveUserToEntityMapping("user1", "Clients", "CompanyA");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveEntity("Clients", "CompanyA");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveEntityType("Clients");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroupToGroupMapping("group1", "group2");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveUserToGroupMapping("user1", "group1");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroup("group2");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroup("group1");
            testAccessManagerTemporalEventBulkPersisterBuffer.RemoveUser("user1");

            testAccessManagerTemporalEventBulkPersisterBuffer.Flush();

            mockMetricLogger.Received(1).Begin(Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 22);
            mockMetricLogger.Received(1).Increment(Arg.Any<BufferFlushOperationCompleted>());
            Assert.AreEqual(1, mockEventPersister.ReceivedCalls().Count());
            Assert.AreEqual(22, capturedPersistedEvents.Count);
            UserEventBufferItem<String> addUserEventBufferItem = AssertEventIsOfTypeAndConvert<UserEventBufferItem<String>>(capturedPersistedEvents[0]);
            AssertTemporalEventBufferItemBaseProperties(addUserEventBufferItem, EventAction.Add, guid1, CreateDataTimeFromString("2021-06-12 13:43:01"), user1HashCode);
            Assert.AreEqual("user1", addUserEventBufferItem.User);
            GroupEventBufferItem<String> addGroupEventBufferItem1 = AssertEventIsOfTypeAndConvert<GroupEventBufferItem<String>>(capturedPersistedEvents[1]);
            AssertTemporalEventBufferItemBaseProperties(addGroupEventBufferItem1, EventAction.Add, guid2, CreateDataTimeFromString("2021-06-12 13:43:02"), group1HashCode);
            Assert.AreEqual("group1", addGroupEventBufferItem1.Group);
            GroupEventBufferItem<String> addGroupEventBufferItem2 = AssertEventIsOfTypeAndConvert<GroupEventBufferItem<String>>(capturedPersistedEvents[2]);
            AssertTemporalEventBufferItemBaseProperties(addGroupEventBufferItem2, EventAction.Add, guid3, CreateDataTimeFromString("2021-06-12 13:43:03"), group2HashCode);
            Assert.AreEqual("group2", addGroupEventBufferItem2.Group);
            UserToGroupMappingEventBufferItem<String, String> addUserToGroupMappingEventBufferItem = AssertEventIsOfTypeAndConvert<UserToGroupMappingEventBufferItem<String, String>>(capturedPersistedEvents[3]);
            AssertTemporalEventBufferItemBaseProperties(addUserToGroupMappingEventBufferItem, EventAction.Add, guid4, CreateDataTimeFromString("2021-06-12 13:43:04"), user1HashCode);
            Assert.AreEqual("user1", addUserToGroupMappingEventBufferItem.User);
            Assert.AreEqual("group1", addUserToGroupMappingEventBufferItem.Group);
            GroupToGroupMappingEventBufferItem<String> addGroupToGroupMappingEventBufferItem = AssertEventIsOfTypeAndConvert<GroupToGroupMappingEventBufferItem<String>>(capturedPersistedEvents[4]);
            AssertTemporalEventBufferItemBaseProperties(addGroupToGroupMappingEventBufferItem, EventAction.Add, guid5, CreateDataTimeFromString("2021-06-12 13:43:05"), group1HashCode);
            Assert.AreEqual("group1", addGroupToGroupMappingEventBufferItem.FromGroup);
            Assert.AreEqual("group2", addGroupToGroupMappingEventBufferItem.ToGroup);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> addUserToApplicationComponentAndAccessLevelMappingEventBufferItem = AssertEventIsOfTypeAndConvert<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(capturedPersistedEvents[5]);
            AssertTemporalEventBufferItemBaseProperties(addUserToApplicationComponentAndAccessLevelMappingEventBufferItem, EventAction.Add, guid6, CreateDataTimeFromString("2021-06-12 13:43:06"), user1HashCode);
            Assert.AreEqual("user1", addUserToApplicationComponentAndAccessLevelMappingEventBufferItem.User);
            Assert.AreEqual(ApplicationScreen.Order, addUserToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Modify, addUserToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> addGroupToApplicationComponentAndAccessLevelMappingEventBufferItem = AssertEventIsOfTypeAndConvert<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(capturedPersistedEvents[6]);
            AssertTemporalEventBufferItemBaseProperties(addGroupToApplicationComponentAndAccessLevelMappingEventBufferItem, EventAction.Add, guid7, CreateDataTimeFromString("2021-06-12 13:43:07"), group1HashCode);
            Assert.AreEqual("group1", addGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.Group);
            Assert.AreEqual(ApplicationScreen.Order, addGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.View, addGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel);
            EntityTypeEventBufferItem addEntityTypeEventBufferItem = AssertEventIsOfTypeAndConvert<EntityTypeEventBufferItem>(capturedPersistedEvents[7]);
            AssertTemporalEventBufferItemBaseProperties(addEntityTypeEventBufferItem, EventAction.Add, guid8, CreateDataTimeFromString("2021-06-12 13:43:08"), clientsHashCode);
            Assert.AreEqual("Clients", addEntityTypeEventBufferItem.EntityType);
            EntityEventBufferItem addEntityEventBufferItem = AssertEventIsOfTypeAndConvert<EntityEventBufferItem>(capturedPersistedEvents[8]);
            AssertTemporalEventBufferItemBaseProperties(addEntityEventBufferItem, EventAction.Add, guid9, CreateDataTimeFromString("2021-06-12 13:43:09"), clientsHashCode);
            Assert.AreEqual("Clients", addEntityEventBufferItem.EntityType);
            Assert.AreEqual("CompanyA", addEntityEventBufferItem.Entity);
            UserToEntityMappingEventBufferItem<String> addUserToEntityMappingEventBufferItem = AssertEventIsOfTypeAndConvert<UserToEntityMappingEventBufferItem<String>>(capturedPersistedEvents[9]);
            AssertTemporalEventBufferItemBaseProperties(addUserToEntityMappingEventBufferItem, EventAction.Add, guid10, CreateDataTimeFromString("2021-06-12 13:43:10"), user1HashCode);
            Assert.AreEqual("user1", addUserToEntityMappingEventBufferItem.User);
            Assert.AreEqual("Clients", addUserToEntityMappingEventBufferItem.EntityType);
            Assert.AreEqual("CompanyA", addUserToEntityMappingEventBufferItem.Entity);
            GroupToEntityMappingEventBufferItem<String> addGroupToEntityMappingEventBufferItem = AssertEventIsOfTypeAndConvert<GroupToEntityMappingEventBufferItem<String>>(capturedPersistedEvents[10]);
            AssertTemporalEventBufferItemBaseProperties(addGroupToEntityMappingEventBufferItem, EventAction.Add, guid11, CreateDataTimeFromString("2021-06-12 13:43:11"), group2HashCode);
            Assert.AreEqual("group2", addGroupToEntityMappingEventBufferItem.Group);
            Assert.AreEqual("Clients", addGroupToEntityMappingEventBufferItem.EntityType);
            Assert.AreEqual("CompanyA", addGroupToEntityMappingEventBufferItem.Entity);
            GroupToEntityMappingEventBufferItem<String> removeGroupToEntityMappingEventBufferItem = AssertEventIsOfTypeAndConvert<GroupToEntityMappingEventBufferItem<String>>(capturedPersistedEvents[11]);
            AssertTemporalEventBufferItemBaseProperties(removeGroupToEntityMappingEventBufferItem, EventAction.Remove, guid12, CreateDataTimeFromString("2021-06-12 13:43:12"), group2HashCode);
            Assert.AreEqual("group2", removeGroupToEntityMappingEventBufferItem.Group);
            Assert.AreEqual("Clients", removeGroupToEntityMappingEventBufferItem.EntityType);
            Assert.AreEqual("CompanyA", removeGroupToEntityMappingEventBufferItem.Entity);
            UserToEntityMappingEventBufferItem<String> removeUserToEntityMappingEventBufferItem = AssertEventIsOfTypeAndConvert<UserToEntityMappingEventBufferItem<String>>(capturedPersistedEvents[12]);
            AssertTemporalEventBufferItemBaseProperties(removeUserToEntityMappingEventBufferItem, EventAction.Remove, guid13, CreateDataTimeFromString("2021-06-12 13:43:13"), user1HashCode);
            Assert.AreEqual("user1", removeUserToEntityMappingEventBufferItem.User);
            Assert.AreEqual("Clients", removeUserToEntityMappingEventBufferItem.EntityType);
            Assert.AreEqual("CompanyA", removeUserToEntityMappingEventBufferItem.Entity);
            EntityEventBufferItem removeEntityEventBufferItem = AssertEventIsOfTypeAndConvert<EntityEventBufferItem>(capturedPersistedEvents[13]);
            AssertTemporalEventBufferItemBaseProperties(removeEntityEventBufferItem, EventAction.Remove, guid14, CreateDataTimeFromString("2021-06-12 13:43:14"), clientsHashCode);
            Assert.AreEqual("Clients", removeEntityEventBufferItem.EntityType);
            Assert.AreEqual("CompanyA", removeEntityEventBufferItem.Entity);
            EntityTypeEventBufferItem removeEntityTypeEventBufferItem = AssertEventIsOfTypeAndConvert<EntityTypeEventBufferItem>(capturedPersistedEvents[14]);
            AssertTemporalEventBufferItemBaseProperties(removeEntityTypeEventBufferItem, EventAction.Remove, guid15, CreateDataTimeFromString("2021-06-12 13:43:15"), clientsHashCode);
            Assert.AreEqual("Clients", removeEntityTypeEventBufferItem.EntityType);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> removeGroupToApplicationComponentAndAccessLevelMappingEventBufferItem = AssertEventIsOfTypeAndConvert<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(capturedPersistedEvents[15]);
            AssertTemporalEventBufferItemBaseProperties(removeGroupToApplicationComponentAndAccessLevelMappingEventBufferItem, EventAction.Remove, guid16, CreateDataTimeFromString("2021-06-12 13:43:16"), group1HashCode);
            Assert.AreEqual("group1", removeGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.Group);
            Assert.AreEqual(ApplicationScreen.Order, removeGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.View, removeGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> removeUserToApplicationComponentAndAccessLevelMappingEventBufferItem = AssertEventIsOfTypeAndConvert<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>>(capturedPersistedEvents[16]);
            AssertTemporalEventBufferItemBaseProperties(removeUserToApplicationComponentAndAccessLevelMappingEventBufferItem, EventAction.Remove, guid17, CreateDataTimeFromString("2021-06-12 13:43:17"), user1HashCode);
            Assert.AreEqual("user1", removeUserToApplicationComponentAndAccessLevelMappingEventBufferItem.User);
            Assert.AreEqual(ApplicationScreen.Order, removeUserToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Modify, removeUserToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel);
            GroupToGroupMappingEventBufferItem<String> removeGroupToGroupMappingEventBufferItem = AssertEventIsOfTypeAndConvert<GroupToGroupMappingEventBufferItem<String>>(capturedPersistedEvents[17]);
            AssertTemporalEventBufferItemBaseProperties(removeGroupToGroupMappingEventBufferItem, EventAction.Remove, guid18, CreateDataTimeFromString("2021-06-12 13:43:18"), group1HashCode);
            Assert.AreEqual("group1", removeGroupToGroupMappingEventBufferItem.FromGroup);
            Assert.AreEqual("group2", removeGroupToGroupMappingEventBufferItem.ToGroup);
            UserToGroupMappingEventBufferItem<String, String> removeUserToGroupMappingEventBufferItem = AssertEventIsOfTypeAndConvert<UserToGroupMappingEventBufferItem<String, String>>(capturedPersistedEvents[18]);
            AssertTemporalEventBufferItemBaseProperties(removeUserToGroupMappingEventBufferItem, EventAction.Remove, guid19, CreateDataTimeFromString("2021-06-12 13:43:19"), user1HashCode);
            Assert.AreEqual("user1", removeUserToGroupMappingEventBufferItem.User);
            Assert.AreEqual("group1", removeUserToGroupMappingEventBufferItem.Group);
            GroupEventBufferItem<String> removeGroupEventBufferItem2 = AssertEventIsOfTypeAndConvert<GroupEventBufferItem<String>>(capturedPersistedEvents[19]);
            AssertTemporalEventBufferItemBaseProperties(removeGroupEventBufferItem2, EventAction.Remove, guid20, CreateDataTimeFromString("2021-06-12 13:43:20"), group2HashCode);
            Assert.AreEqual("group2", removeGroupEventBufferItem2.Group);
            GroupEventBufferItem<String> removeGroupEventBufferItem1 = AssertEventIsOfTypeAndConvert<GroupEventBufferItem<String>>(capturedPersistedEvents[20]);
            AssertTemporalEventBufferItemBaseProperties(removeGroupEventBufferItem1, EventAction.Remove, guid21, CreateDataTimeFromString("2021-06-12 13:43:21"), group1HashCode);
            Assert.AreEqual("group1", removeGroupEventBufferItem1.Group);
            UserEventBufferItem<String> removeUserEventBufferItem = AssertEventIsOfTypeAndConvert<UserEventBufferItem<String>>(capturedPersistedEvents[21]);
            AssertTemporalEventBufferItemBaseProperties(removeUserEventBufferItem, EventAction.Remove, guid22, CreateDataTimeFromString("2021-06-12 13:43:22"), user1HashCode);
            Assert.AreEqual("user1", removeUserEventBufferItem.User);


            // Test that calling Flush() again resets variables holding the number of events processed for metric logging
            mockMetricLogger.ClearReceivedCalls();

            testAccessManagerTemporalEventBulkPersisterBuffer.Flush();

            mockMetricLogger.Received(1).Begin(Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 0);
            mockMetricLogger.Received(1).Increment(Arg.Any<BufferFlushOperationCompleted>());
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

        /// <summary>
        /// Asserts that the specified <see cref="TemporalEventBufferItemBase"/> instance of the specified type, and returns the instance cast to that type.
        /// </summary>
        /// <typeparam name="TBufferItem">The type to check for.</typeparam>
        /// <param name="eventBufferItem">The event buffer item to check the type of.</param>
        /// <returns>The event buffer item cast to the specified type.</returns>
        protected TBufferItem AssertEventIsOfTypeAndConvert<TBufferItem>(TemporalEventBufferItemBase eventBufferItem) where TBufferItem : TemporalEventBufferItemBase
        {
            Assert.IsInstanceOf<TBufferItem>(eventBufferItem);

            return (TBufferItem)eventBufferItem;
        }

        /// <summary>
        /// Asserts that the specified <see cref="TemporalEventBufferItemBase"/> instance has the specified properties.
        /// </summary>
        /// <param name="eventBufferItem">The event buffer item to check.</param>
        /// <param name="eventAction">The event action for check for.</param>
        /// <param name="eventId">The event id to check for.</param>
        /// <param name="occurredTime">The event occurred time to check for.</param>
        protected void AssertTemporalEventBufferItemBaseProperties(TemporalEventBufferItemBase eventBufferItem, EventAction eventAction, Guid eventId, DateTime occurredTime, Int32 hashCode)
        {
            Assert.AreEqual(eventAction, eventBufferItem.EventAction);
            Assert.AreEqual(eventId, eventBufferItem.EventId);
            Assert.AreEqual(occurredTime, eventBufferItem.OccurredTime);
            Assert.AreEqual(hashCode, eventBufferItem.HashCode);
        }

        #endregion
    }
}
