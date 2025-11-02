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
using ApplicationAccess.Hosting.Grpc;
using ApplicationAccess.Persistence.Models;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Grpc.EventCache.IntegrationTests
{
    /// <summary>
    /// Integration tests for RPC methods in the ApplicationAccess.Hosting.Grpc.EventCache.EventCacheService class.
    /// </summary>
    /// <remarks>This class additionally implicitly tests the ApplicationAccess.Hosting.Grpc.EventBufferItemToGrpcMessageConverter, ApplicationAccess.Hosting.Grpc.Client.EventCacheClient, and ApplicationAccess.Hosting.Grpc.Client.AccessManagerClientBase classes.</remarks>
    public class RpcTests : IntegrationTestsBase
    {
        [Test]
        public void GetAllEventsSince()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            List<TemporalEventBufferItemBase> returnEvents = CreateTestEvents();
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.GetAllEventsSince(priorEventdId).Returns(returnEvents);

            IList<TemporalEventBufferItemBase> results = grpcClient.GetAllEventsSince(priorEventdId);

            Assert.AreEqual(10, results.Count);

            Assert.IsInstanceOf<EntityTypeEventBufferItem>(results[0]);
            var entityTypeEvent = (EntityTypeEventBufferItem)results[0];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000000"), entityTypeEvent.EventId);
            Assert.AreEqual(EventAction.Remove, entityTypeEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000000"), entityTypeEvent.OccurredTime);
            Assert.AreEqual(-5, entityTypeEvent.HashCode);
            Assert.AreEqual("ClientAccount", entityTypeEvent.EntityType);

            Assert.IsInstanceOf<EntityEventBufferItem>(results[1]);
            var entityEvent = (EntityEventBufferItem)results[1];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000001"), entityEvent.EventId);
            Assert.AreEqual(EventAction.Add, entityEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000001"), entityEvent.OccurredTime);
            Assert.AreEqual(-4, entityEvent.HashCode);
            Assert.AreEqual("BusinessUnit", entityEvent.EntityType);
            Assert.AreEqual("Sales", entityEvent.Entity);

            Assert.IsInstanceOf<UserToEntityMappingEventBufferItem<String>>(results[2]);
            var userToEntityMappingEvent = (UserToEntityMappingEventBufferItem<String>)results[2];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000002"), userToEntityMappingEvent.EventId);
            Assert.AreEqual(EventAction.Remove, userToEntityMappingEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000002"), userToEntityMappingEvent.OccurredTime);
            Assert.AreEqual(-3, userToEntityMappingEvent.HashCode);
            Assert.AreEqual("user1", userToEntityMappingEvent.User);
            Assert.AreEqual("ClientAccount", userToEntityMappingEvent.EntityType);
            Assert.AreEqual("ClientA", userToEntityMappingEvent.Entity);

            Assert.IsInstanceOf<GroupToEntityMappingEventBufferItem<String>>(results[3]);
            var groupToEntityMappingEvent = (GroupToEntityMappingEventBufferItem<String>)results[3];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000003"), groupToEntityMappingEvent.EventId);
            Assert.AreEqual(EventAction.Add, groupToEntityMappingEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000003"), groupToEntityMappingEvent.OccurredTime);
            Assert.AreEqual(-2, groupToEntityMappingEvent.HashCode);
            Assert.AreEqual("group1", groupToEntityMappingEvent.Group);
            Assert.AreEqual("BusinessUnit", groupToEntityMappingEvent.EntityType);
            Assert.AreEqual("Marketing", groupToEntityMappingEvent.Entity);

            Assert.IsInstanceOf<UserEventBufferItem<String>>(results[4]);
            var userEvent = (UserEventBufferItem<String>)results[4];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000004"), userEvent.EventId);
            Assert.AreEqual(EventAction.Add, userEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000004"), userEvent.OccurredTime);
            Assert.AreEqual(-1, userEvent.HashCode);
            Assert.AreEqual("user2", userEvent.User);

            Assert.IsInstanceOf<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>>(results[5]);
            var userToApplicationComponentAndAccessLevelMappingEvent = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)results[5];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000005"), userToApplicationComponentAndAccessLevelMappingEvent.EventId);
            Assert.AreEqual(EventAction.Remove, userToApplicationComponentAndAccessLevelMappingEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000005"), userToApplicationComponentAndAccessLevelMappingEvent.OccurredTime);
            Assert.AreEqual(0, userToApplicationComponentAndAccessLevelMappingEvent.HashCode);
            Assert.AreEqual("user3", userToApplicationComponentAndAccessLevelMappingEvent.User);
            Assert.AreEqual("Order", userToApplicationComponentAndAccessLevelMappingEvent.ApplicationComponent);
            Assert.AreEqual("Create", userToApplicationComponentAndAccessLevelMappingEvent.AccessLevel);

            Assert.IsInstanceOf<UserToGroupMappingEventBufferItem<String, String>>(results[6]);
            var userToGroupMappingEvent = (UserToGroupMappingEventBufferItem<String, String>)results[6];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000006"), userToGroupMappingEvent.EventId);
            Assert.AreEqual(EventAction.Add, userToGroupMappingEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000006"), userToGroupMappingEvent.OccurredTime);
            Assert.AreEqual(1, userToGroupMappingEvent.HashCode);
            Assert.AreEqual("user4", userToGroupMappingEvent.User);
            Assert.AreEqual("group2", userToGroupMappingEvent.Group);

            Assert.IsInstanceOf<GroupEventBufferItem<String>>(results[7]);
            var groupEvent = (GroupEventBufferItem<String>)results[7];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000007"), groupEvent.EventId);
            Assert.AreEqual(EventAction.Remove, groupEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000007"), groupEvent.OccurredTime);
            Assert.AreEqual(2, groupEvent.HashCode);
            Assert.AreEqual("group3", groupEvent.Group);

            Assert.IsInstanceOf<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>>(results[8]);
            var groupToApplicationComponentAndAccessLevelMappingEvent = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)results[8];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000008"), groupToApplicationComponentAndAccessLevelMappingEvent.EventId);
            Assert.AreEqual(EventAction.Add, groupToApplicationComponentAndAccessLevelMappingEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000008"), groupToApplicationComponentAndAccessLevelMappingEvent.OccurredTime);
            Assert.AreEqual(3, groupToApplicationComponentAndAccessLevelMappingEvent.HashCode);
            Assert.AreEqual("group4", groupToApplicationComponentAndAccessLevelMappingEvent.Group);
            Assert.AreEqual("Summary", groupToApplicationComponentAndAccessLevelMappingEvent.ApplicationComponent);
            Assert.AreEqual("View", groupToApplicationComponentAndAccessLevelMappingEvent.AccessLevel);

            Assert.IsInstanceOf<GroupToGroupMappingEventBufferItem<String>>(results[9]);
            var groupToGroupMappingEvent = (GroupToGroupMappingEventBufferItem<String>)results[9];
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000009"), groupToGroupMappingEvent.EventId);
            Assert.AreEqual(EventAction.Remove, groupToGroupMappingEvent.EventAction);
            Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000009"), groupToGroupMappingEvent.OccurredTime);
            Assert.AreEqual(4, groupToGroupMappingEvent.HashCode);
            Assert.AreEqual("group5", groupToGroupMappingEvent.FromGroup);
            Assert.AreEqual("group6", groupToGroupMappingEvent.ToGroup);

            // Check the number of times ToString() was called on the *Stringifier classes
            //   Note that ToString() counts can't be checked as the IUniqueStringifier implementations used for serialization are fixed in the EventCacheService class.
            Assert.AreEqual(4, userStringifier.FromStringCallCount);
            Assert.AreEqual(6, groupStringifier.FromStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a set of test events which derive from <see cref="TemporalEventBufferItemBase"/>.
        /// </summary>
        /// <returns>The test events.</returns>
        protected List<TemporalEventBufferItemBase> CreateTestEvents()
        {
            var testEvents = new List<TemporalEventBufferItemBase>()
            {
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000000"), EventAction.Remove, "ClientAccount", CreateDataTimeFromString("2023-03-18 23:49:35.0000000"), -5),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000001"), EventAction.Add, "BusinessUnit", "Sales", CreateDataTimeFromString("2023-03-18 23:49:35.0000001"), -4),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000002"), EventAction.Remove, "user1", "ClientAccount", "ClientA", CreateDataTimeFromString("2023-03-18 23:49:35.0000002"), -3),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000003"), EventAction.Add, "group1", "BusinessUnit", "Marketing", CreateDataTimeFromString("2023-03-18 23:49:35.0000003"), -2),
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000004"), EventAction.Add, "user2", CreateDataTimeFromString("2023-03-18 23:49:35.0000004"), -1),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000005"), EventAction.Remove, "user3", "Order", "Create", CreateDataTimeFromString("2023-03-18 23:49:35.0000005"), 0),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000006"), EventAction.Add, "user4", "group2", CreateDataTimeFromString("2023-03-18 23:49:35.0000006"), 1),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000007"), EventAction.Remove, "group3", CreateDataTimeFromString("2023-03-18 23:49:35.0000007"), 2),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000008"), EventAction.Add, "group4", "Summary", "View", CreateDataTimeFromString("2023-03-18 23:49:35.0000008"), 3),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000009"), EventAction.Remove, "group5", "group6", CreateDataTimeFromString("2023-03-18 23:49:35.0000009"), 4)
            };

            return testEvents;
        }

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss.fffffff format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion
    }
}
