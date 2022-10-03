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
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.AccessManagerEventProcessor class.
    /// </summary>
    public class AccessManagerEventProcessorTests
    {
        private Guid testEventId;
        private DateTime testOccurredTime;
        private IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel> mockEventProcessor;
        private AccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel> testAccessManagerEventProcessor;

        [SetUp]
        protected void SetUp()
        {
            testEventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testOccurredTime = DateTime.ParseExact("2022-10-03T18:29:53.0000001", "yyyy-MM-ddTHH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            testOccurredTime = DateTime.SpecifyKind(testOccurredTime, DateTimeKind.Utc);
            mockEventProcessor = Substitute.For<IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel>>();
            testAccessManagerEventProcessor = new AccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel>(mockEventProcessor);
        }

        [Test]
        public void Process_UserEventBufferItem()
        {
            String testUser = "user1";
            var testEvent = new UserEventBufferItem<String>(testEventId, EventAction.Add, testUser, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddUser(testUser);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new UserEventBufferItem<String>(testEventId, EventAction.Remove, testUser, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveUser(testUser);
        }

        [Test]
        public void Process_GroupEventBufferItem()
        {
            String testGroup = "group1";
            var testEvent = new GroupEventBufferItem<String>(testEventId, EventAction.Add, testGroup, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddGroup(testGroup);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new GroupEventBufferItem<String>(testEventId, EventAction.Remove, testGroup, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveGroup(testGroup);
        }

        [Test]
        public void Process_UserToGroupMappingEventBufferItem()
        {
            String testUser = "user1";
            String testGroup = "group1";
            var testEvent = new UserToGroupMappingEventBufferItem<String, String>(testEventId, EventAction.Add, testUser, testGroup, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddUserToGroupMapping(testUser, testGroup);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new UserToGroupMappingEventBufferItem<String, String>(testEventId, EventAction.Remove, testUser, testGroup, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveUserToGroupMapping(testUser, testGroup);
        }

        [Test]
        public void Process_GroupToGroupMappingEventBufferItem()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            var testEvent = new GroupToGroupMappingEventBufferItem<String>(testEventId, EventAction.Add, testFromGroup, testToGroup, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddGroupToGroupMapping(testFromGroup, testToGroup);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new GroupToGroupMappingEventBufferItem<String>(testEventId, EventAction.Remove, testFromGroup, testToGroup, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveGroupToGroupMapping(testFromGroup, testToGroup);
        }

        [Test]
        public void Process_UserToApplicationComponentAndAccessLevelMappingEventBufferItem()
        {
            String testUser = "user1";
            var testEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>(testEventId, EventAction.Add, testUser, ApplicationScreen.ManageProducts, AccessLevel.Modify, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Modify);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>(testEventId, EventAction.Remove, testUser, ApplicationScreen.Order, AccessLevel.View, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
        }

        [Test]
        public void Process_GroupToApplicationComponentAndAccessLevelMappingEventBufferItem()
        {
            String testGroup = "group1";
            var testEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>(testEventId, EventAction.Add, testGroup, ApplicationScreen.Settings, AccessLevel.Delete, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Settings, AccessLevel.Delete);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel>(testEventId, EventAction.Remove, testGroup, ApplicationScreen.Summary, AccessLevel.Create, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Summary, AccessLevel.Create);
        }

        [Test]
        public void Process_EntityTypeEventBufferItem()
        {
            String testEntityType = "ClientAccount";
            var testEvent = new EntityTypeEventBufferItem(testEventId, EventAction.Add, testEntityType, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddEntityType(testEntityType);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new EntityTypeEventBufferItem(testEventId, EventAction.Remove, testEntityType, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveEntityType(testEntityType);
        }

        [Test]
        public void Process_EntityEventBufferItem()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var testEvent = new EntityEventBufferItem(testEventId, EventAction.Add, testEntityType, testEntity, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddEntity(testEntityType, testEntity);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new EntityEventBufferItem(testEventId, EventAction.Remove, testEntityType, testEntity, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveEntity(testEntityType, testEntity);
        }

        [Test]
        public void Process_UserToEntityMappingEventBufferItem()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var testEvent = new UserToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testUser, testEntityType, testEntity, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddUserToEntityMapping(testUser, testEntityType, testEntity);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new UserToEntityMappingEventBufferItem<String>(testEventId, EventAction.Remove, testUser, testEntityType, testEntity, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveUserToEntityMapping(testUser, testEntityType, testEntity);
        }

        [Test]
        public void Process_GroupToEntityMappingEventBufferItem()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var testEvent = new GroupToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testGroup, testEntityType, testEntity, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).AddGroupToEntityMapping(testGroup, testEntityType, testEntity);


            mockEventProcessor.ClearReceivedCalls();
            testEvent = new GroupToEntityMappingEventBufferItem<String>(testEventId, EventAction.Remove, testGroup, testEntityType, testEntity, testOccurredTime);

            testAccessManagerEventProcessor.Process(new List<EventBufferItemBase>() { testEvent });

            mockEventProcessor.Received(1).RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);
        }
    }
}
