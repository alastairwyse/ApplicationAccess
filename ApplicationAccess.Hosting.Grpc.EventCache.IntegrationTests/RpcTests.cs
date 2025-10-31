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
using ApplicationAccess.Persistence.Models;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Grpc.EventCache.IntegrationTests
{
    /// <summary>
    /// Integration tests for RPC methods in the ApplicationAccess.Hosting.Grpc.EventCache.EventCacheService class.
    /// </summary>
    /// <remarks>This class additionally implicitly tests the <see cref="EventBufferItemToGrpcMessageConverter"/> class.</remarks>
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

            Assert.AreNotEqual(0, results.Count);
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
