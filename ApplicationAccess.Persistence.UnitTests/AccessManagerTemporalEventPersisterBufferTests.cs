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
using System.Linq;
using ApplicationAccess.UnitTests;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
    /// </summary>
    public class AccessManagerTemporalEventPersisterBufferTests : AccessManagerTemporalEventPersisterBufferBaseTests
    {
        [Test]
        public void Flush_CallToPersisterAddUserFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            Int32 hashCode = -10;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddUser(user, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUser(user);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 11:39:55");
            Int32 hashCode = -9;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUser(user, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUser(user);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-10 22:50:15");
            Int32 hashCode = -8;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroup(group, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroup(group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000004");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 22:51:55");
            Int32 hashCode = -7;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroup(group, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup(group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddUserToGroupMappingFails()
        {
            const String user = "user1";
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000005");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 22:56:15");
            Int32 hashCode = -6;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddUserToGroupMapping(user, group, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping(user, group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserToGroupMappingFails()
        {
            const String user = "user1";
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000006");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 22:59:07");
            Int32 hashCode = -5;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUserToGroupMapping(user, group, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping(user, group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupToGroupMappingFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000007");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 23:02:15");
            Int32 hashCode = -4;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(fromGroup).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroupToGroupMapping(fromGroup, toGroup, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping(fromGroup, toGroup);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupToGroupMappingFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000008");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 23:02:16");
            Int32 hashCode = -3;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(fromGroup).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddUserToApplicationComponentAndAccessLevelMappingFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000009");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 23:05:16");
            Int32 hashCode = -2;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Order, AccessLevel.Delete, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Order, AccessLevel.Delete);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserToApplicationComponentAndAccessLevelMappingFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 23:07:17");
            Int32 hashCode = -1;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Settings, AccessLevel.Modify, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Settings, AccessLevel.Modify);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupToApplicationComponentAndAccessLevelMappingFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000b");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 23:10:18");
            Int32 hashCode = 0;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Delete, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Delete);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupToApplicationComponentAndAccessLevelMappingFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000c");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 23:10:19");
            Int32 hashCode = 1;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Settings, AccessLevel.Modify, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Settings, AccessLevel.Modify);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddEntityTypeFails()
        {
            const String entityType = "Products";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000d");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:07:20");
            Int32 hashCode = 2;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddEntityType(entityType, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddEntityType(entityType);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add entity type' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveEntityTypeFails()
        {
            const String entityType = "Products";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000e");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:08:21");
            Int32 hashCode = 3;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveEntityType(entityType, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntityType(entityType);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove entity type' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddEntityFails()
        {
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000f");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:09:22");
            Int32 hashCode = 4;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddEntity(entityType, entity, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddEntity(entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add entity' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveEntityFails()
        {
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000010");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:10:23");
            Int32 hashCode = 5;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveEntity(entityType, entity, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity(entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove entity' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddUserToEntityMappingFails()
        {
            const String user = "user1";
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000011");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:18:24");
            Int32 hashCode = 6;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddUserToEntityMapping(user, entityType, entity, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUserToEntityMapping(user, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserToEntityMappingFails()
        {
            const String user = "user1";
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000012");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:19:24");
            Int32 hashCode = 7;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUserToEntityMapping(user, entityType, entity, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToEntityMapping(user, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupToEntityMappingFails()
        {
            const String group = "group1";
            const String entityType = "Clients";
            const String entity = "CompanyB";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000013");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 13:16:27");
            Int32 hashCode = 8;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroupToEntityMapping(group, entityType, entity, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping(group, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupToEntityMappingFails()
        {
            const String group = "group1";
            const String entityType = "Clients";
            const String entity = "CompanyB";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000014");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 13:17:28");
            Int32 hashCode = 9;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroupToEntityMapping(group, entityType, entity, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToEntityMapping(group, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_MetricsLoggedCorrectlyWhenCallToPersisterFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Int32 hashCode = 10;
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            mockMetricLogger.Begin(Arg.Any<FlushTime>()).Returns(testBeginId);
            mockEventPersister.When((eventPersister) => eventPersister.AddUser(user, eventId, eventOccurredTime, hashCode)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUser(user);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user' event."));
            Assert.AreEqual(mockException, e.InnerException);
            mockMetricLogger.Received(1).Begin(Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<FlushTime>());
            mockMetricLogger.DidNotReceive().Add(Arg.Any<BufferedEventsFlushed>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<BufferFlushOperationCompleted>());
        }

        [Test]
        public override void Flush()
        {
            var guid0 = Guid.Parse("00000000-0000-0000-0000-000000000000");
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
            var guid23 = Guid.Parse("00000000-0000-0000-0000-000000000017");
            var guid24 = Guid.Parse("00000000-0000-0000-0000-000000000018");
            var guid25 = Guid.Parse("00000000-0000-0000-0000-000000000019");
            var guid26 = Guid.Parse("00000000-0000-0000-0000-00000000001a");
            var guid27 = Guid.Parse("00000000-0000-0000-0000-00000000001b");
            mockBufferFlushStrategy.ClearReceivedCalls();

            Int32 user1HashCode = 1;
            Int32 user2HashCode = 2;
            Int32 group1HashCode = 11;
            Int32 group2HashCode = 12;
            Int32 clientsHashCode = 21;
            mockUserHashCodeGenerator.GetHashCode("user1").Returns(user1HashCode);
            mockUserHashCodeGenerator.GetHashCode("user2").Returns(user2HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group1").Returns(group1HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group2").Returns(group2HashCode);
            mockEntityTypeHashCodeGenerator.GetHashCode("Clients").Returns(clientsHashCode);

            mockGuidProvider.NewGuid().Returns<Guid>
            (
                guid0,
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
                guid22,
                guid23,
                guid24,
                guid25,
                guid26,
                guid27
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                CreateDataTimeFromString("2021-06-12 13:43:00"),
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
                CreateDataTimeFromString("2021-06-12 13:43:22"),
                CreateDataTimeFromString("2021-06-12 13:43:23"),
                CreateDataTimeFromString("2021-06-12 13:43:24"),
                CreateDataTimeFromString("2021-06-12 13:43:25"),
                CreateDataTimeFromString("2021-06-12 13:43:26"),
                CreateDataTimeFromString("2021-06-12 13:43:27")
            );
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<FlushTime>()).Returns(testBeginId);

            testAccessManagerTemporalEventPersisterBuffer.AddEntityType("Clients");
            testAccessManagerTemporalEventPersisterBuffer.AddEntity("Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.AddGroup("group1");
            testAccessManagerTemporalEventPersisterBuffer.AddUser("user1");
            testAccessManagerTemporalEventPersisterBuffer.AddUser("user2");
            testAccessManagerTemporalEventPersisterBuffer.AddGroup("group2");
            testAccessManagerTemporalEventPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManagerTemporalEventPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping("group1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping("user1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping("group2", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping("user2", "group1");
            testAccessManagerTemporalEventPersisterBuffer.AddUserToEntityMapping("user1", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.AddEntity("Clients", "CompanyB");
            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity("Clients", "CompanyB");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToEntityMapping("user1", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping("user2", "group1");
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToEntityMapping("group2", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping("user1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToGroupMapping("group1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup("group2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUser("user2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUser("user1");
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup("group1");
            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity("Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.RemoveEntityType("Clients");

            testAccessManagerTemporalEventPersisterBuffer.Flush();

            Received.InOrder(() => {
                // These get called as the initial events are buffered
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 2;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 3;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 3;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 4;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 3;
                mockBufferFlushStrategy.UserEventBufferItemCount = 3;
                mockBufferFlushStrategy.UserEventBufferItemCount = 4;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 4;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 4;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 2;
                // These are called as the buffers are cleared
                mockBufferFlushStrategy.UserEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
                // These are called as the buffered items are processed
                mockEventPersister.AddEntityType("Clients", guid0, CreateDataTimeFromString("2021-06-12 13:43:00"), clientsHashCode);
                mockEventPersister.AddEntity("Clients", "CompanyA", guid1, CreateDataTimeFromString("2021-06-12 13:43:01"), clientsHashCode);
                mockEventPersister.AddGroup("group1", guid2, CreateDataTimeFromString("2021-06-12 13:43:02"), group1HashCode);
                mockEventPersister.AddUser("user1", guid3, CreateDataTimeFromString("2021-06-12 13:43:03"), user1HashCode);
                mockEventPersister.AddUser("user2", guid4, CreateDataTimeFromString("2021-06-12 13:43:04"), user2HashCode);
                mockEventPersister.AddGroup("group2", guid5, CreateDataTimeFromString("2021-06-12 13:43:05"), group2HashCode);
                mockEventPersister.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View, guid6, CreateDataTimeFromString("2021-06-12 13:43:06"), group1HashCode);
                mockEventPersister.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify, guid7, CreateDataTimeFromString("2021-06-12 13:43:07"), user2HashCode);
                mockEventPersister.AddGroupToGroupMapping("group1", "group2", guid8, CreateDataTimeFromString("2021-06-12 13:43:08"), group1HashCode);
                mockEventPersister.AddUserToGroupMapping("user1", "group2", guid9, CreateDataTimeFromString("2021-06-12 13:43:09"), user1HashCode);
                mockEventPersister.AddGroupToEntityMapping("group2", "Clients", "CompanyA", guid10, CreateDataTimeFromString("2021-06-12 13:43:10"), group2HashCode);
                mockEventPersister.AddUserToGroupMapping("user2", "group1", guid11, CreateDataTimeFromString("2021-06-12 13:43:11"), user2HashCode);
                mockEventPersister.AddUserToEntityMapping("user1", "Clients", "CompanyA", guid12, CreateDataTimeFromString("2021-06-12 13:43:12"), user1HashCode);
                mockEventPersister.AddEntity("Clients", "CompanyB", guid13, CreateDataTimeFromString("2021-06-12 13:43:13"), clientsHashCode);
                mockEventPersister.RemoveEntity("Clients", "CompanyB", guid14, CreateDataTimeFromString("2021-06-12 13:43:14"), clientsHashCode);
                mockEventPersister.RemoveUserToEntityMapping("user1", "Clients", "CompanyA", guid15, CreateDataTimeFromString("2021-06-12 13:43:15"), user1HashCode);
                mockEventPersister.RemoveUserToGroupMapping("user2", "group1", guid16, CreateDataTimeFromString("2021-06-12 13:43:16"), user2HashCode);
                mockEventPersister.RemoveGroupToEntityMapping("group2", "Clients", "CompanyA", guid17, CreateDataTimeFromString("2021-06-12 13:43:17"), group2HashCode);
                mockEventPersister.RemoveUserToGroupMapping("user1", "group2", guid18, CreateDataTimeFromString("2021-06-12 13:43:18"), user1HashCode);
                mockEventPersister.RemoveGroupToGroupMapping("group1", "group2", guid19, CreateDataTimeFromString("2021-06-12 13:43:19"), group1HashCode);
                mockEventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify, guid20, CreateDataTimeFromString("2021-06-12 13:43:20"), user2HashCode);
                mockEventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View, guid21, CreateDataTimeFromString("2021-06-12 13:43:21"), group1HashCode);
                mockEventPersister.RemoveGroup("group2", guid22, CreateDataTimeFromString("2021-06-12 13:43:22"), group2HashCode);
                mockEventPersister.RemoveUser("user2", guid23, CreateDataTimeFromString("2021-06-12 13:43:23"), user2HashCode);
                mockEventPersister.RemoveUser("user1", guid24, CreateDataTimeFromString("2021-06-12 13:43:24"), user1HashCode);
                mockEventPersister.RemoveGroup("group1", guid25, CreateDataTimeFromString("2021-06-12 13:43:25"), group1HashCode);
                mockEventPersister.RemoveEntity("Clients", "CompanyA", guid26, CreateDataTimeFromString("2021-06-12 13:43:26"), clientsHashCode);
                mockEventPersister.RemoveEntityType("Clients", guid27, CreateDataTimeFromString("2021-06-12 13:43:27"), clientsHashCode);
            });
            mockMetricLogger.Received(1).Begin(Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 28);
            mockMetricLogger.Received(1).Increment(Arg.Any<BufferFlushOperationCompleted>());
            Assert.AreEqual(38, mockBufferFlushStrategy.ReceivedCalls().Count());
            Assert.AreEqual(28, mockEventPersister.ReceivedCalls().Count());


            // Test that calling Flush() again resets variables holding the number of events processed for metric logging
            mockMetricLogger.ClearReceivedCalls();

            testAccessManagerTemporalEventPersisterBuffer.Flush();

            mockMetricLogger.Received(1).Begin(Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<FlushTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<BufferedEventsFlushed>(), 0);
            mockMetricLogger.Received(1).Increment(Arg.Any<BufferFlushOperationCompleted>());
        }
    }
}
