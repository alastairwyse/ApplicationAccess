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
using System.Text.Json;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ApplicationAccess.Persistence;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Rest.EventCache.IntegrationTests
{
    /// <summary>
    /// Integration tests for methods in the ApplicationAccess.Hosting.Rest.EventCache.Controllers.EventCacheController class.
    /// </summary>
    /// <remarks>This class additionally implicitly tests the <see cref="TemporalEventBufferItemBaseConverter{TUser, TGroup, TComponent, TAccess}"/> class.</remarks>
    public class ControllerTests : IntegrationTestsBase
    {
        const String requestUrl = "api/v1/eventBufferItems";

        private MethodCallCountingStringUniqueStringifier userStringifier;
        private MethodCallCountingStringUniqueStringifier groupStringifier;
        private MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        private MethodCallCountingStringUniqueStringifier accessLevelStringifier;

        [SetUp]
        protected void SetUp()
        {
            userStringifier = new MethodCallCountingStringUniqueStringifier();
            groupStringifier = new MethodCallCountingStringUniqueStringifier();
            applicationComponentStringifier = new MethodCallCountingStringUniqueStringifier();
            accessLevelStringifier = new MethodCallCountingStringUniqueStringifier();
        }

        [Test]
        public void CacheEvents()
        {
            List<TemporalEventBufferItemBase> testEvents = CreateTestEvents();
            var serialIzerOptions = new JsonSerializerOptions
            {
                Converters =
                {
                    new TemporalEventBufferItemBaseConverter<String, String, String, String>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
                }
            };
            List<TemporalEventBufferItemBase> capturedEvents = null;
            mockTemporalEventBulkPersister.PersistEvents(Arg.Do<List<TemporalEventBufferItemBase>>(argumentValue => capturedEvents = argumentValue));
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl))
            {
                requestMessage.Content = JsonContent.Create(testEvents, testEvents.GetType(), null, serialIzerOptions);

                using (HttpResponseMessage response = client.SendAsync(requestMessage).Result)
                {

                    Assert.AreEqual(response.StatusCode, HttpStatusCode.Created);
                    Assert.AreEqual(10, capturedEvents.Count);

                    Assert.IsInstanceOf<EntityTypeEventBufferItem>(capturedEvents[0]);
                    var entityTypeEvent = (EntityTypeEventBufferItem)capturedEvents[0];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000000"), entityTypeEvent.EventId);
                    Assert.AreEqual(EventAction.Remove, entityTypeEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000000"), entityTypeEvent.OccurredTime);
                    Assert.AreEqual("ClientAccount", entityTypeEvent.EntityType);

                    Assert.IsInstanceOf<EntityEventBufferItem>(capturedEvents[1]);
                    var entityEvent = (EntityEventBufferItem)capturedEvents[1];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000001"), entityEvent.EventId);
                    Assert.AreEqual(EventAction.Add, entityEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000001"), entityEvent.OccurredTime);
                    Assert.AreEqual("BusinessUnit", entityEvent.EntityType);
                    Assert.AreEqual("Sales", entityEvent.Entity);

                    Assert.IsInstanceOf<UserToEntityMappingEventBufferItem<String>>(capturedEvents[2]);
                    var userToEntityMappingEvent = (UserToEntityMappingEventBufferItem<String>)capturedEvents[2];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000002"), userToEntityMappingEvent.EventId);
                    Assert.AreEqual(EventAction.Remove, userToEntityMappingEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000002"), userToEntityMappingEvent.OccurredTime);
                    Assert.AreEqual("user1", userToEntityMappingEvent.User);
                    Assert.AreEqual("ClientAccount", userToEntityMappingEvent.EntityType);
                    Assert.AreEqual("ClientA", userToEntityMappingEvent.Entity);

                    Assert.IsInstanceOf<GroupToEntityMappingEventBufferItem<String>>(capturedEvents[3]);
                    var groupToEntityMappingEvent = (GroupToEntityMappingEventBufferItem<String>)capturedEvents[3];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000003"), groupToEntityMappingEvent.EventId);
                    Assert.AreEqual(EventAction.Add, groupToEntityMappingEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000003"), groupToEntityMappingEvent.OccurredTime);
                    Assert.AreEqual("group1", groupToEntityMappingEvent.Group);
                    Assert.AreEqual("BusinessUnit", groupToEntityMappingEvent.EntityType);
                    Assert.AreEqual("Marketing", groupToEntityMappingEvent.Entity);

                    Assert.IsInstanceOf<UserEventBufferItem<String>>(capturedEvents[4]);
                    var userEvent = (UserEventBufferItem<String>)capturedEvents[4];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000004"), userEvent.EventId);
                    Assert.AreEqual(EventAction.Add, userEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000004"), userEvent.OccurredTime);
                    Assert.AreEqual("user2", userEvent.User);

                    Assert.IsInstanceOf<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>>(capturedEvents[5]);
                    var userToApplicationComponentAndAccessLevelMappingEvent = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)capturedEvents[5];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000005"), userToApplicationComponentAndAccessLevelMappingEvent.EventId);
                    Assert.AreEqual(EventAction.Remove, userToApplicationComponentAndAccessLevelMappingEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000005"), userToApplicationComponentAndAccessLevelMappingEvent.OccurredTime);
                    Assert.AreEqual("user3", userToApplicationComponentAndAccessLevelMappingEvent.User);
                    Assert.AreEqual("Order", userToApplicationComponentAndAccessLevelMappingEvent.ApplicationComponent);
                    Assert.AreEqual("Create", userToApplicationComponentAndAccessLevelMappingEvent.AccessLevel);

                    Assert.IsInstanceOf<UserToGroupMappingEventBufferItem<String, String>>(capturedEvents[6]);
                    var userToGroupMappingEvent = (UserToGroupMappingEventBufferItem<String, String>)capturedEvents[6];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000006"), userToGroupMappingEvent.EventId);
                    Assert.AreEqual(EventAction.Add, userToGroupMappingEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000006"), userToGroupMappingEvent.OccurredTime);
                    Assert.AreEqual("user4", userToGroupMappingEvent.User);
                    Assert.AreEqual("group2", userToGroupMappingEvent.Group);

                    Assert.IsInstanceOf<GroupEventBufferItem<String>>(capturedEvents[7]);
                    var groupEvent = (GroupEventBufferItem<String>)capturedEvents[7];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000007"), groupEvent.EventId);
                    Assert.AreEqual(EventAction.Remove, groupEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000007"), groupEvent.OccurredTime);
                    Assert.AreEqual("group3", groupEvent.Group);

                    Assert.IsInstanceOf<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>>(capturedEvents[8]);
                    var groupToApplicationComponentAndAccessLevelMappingEvent = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)capturedEvents[8];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000008"), groupToApplicationComponentAndAccessLevelMappingEvent.EventId);
                    Assert.AreEqual(EventAction.Add, groupToApplicationComponentAndAccessLevelMappingEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000008"), groupToApplicationComponentAndAccessLevelMappingEvent.OccurredTime);
                    Assert.AreEqual("group4", groupToApplicationComponentAndAccessLevelMappingEvent.Group);
                    Assert.AreEqual("Summary", groupToApplicationComponentAndAccessLevelMappingEvent.ApplicationComponent);
                    Assert.AreEqual("View", groupToApplicationComponentAndAccessLevelMappingEvent.AccessLevel);

                    Assert.IsInstanceOf<GroupToGroupMappingEventBufferItem<String>>(capturedEvents[9]);
                    var groupToGroupMappingEvent = (GroupToGroupMappingEventBufferItem<String>)capturedEvents[9];
                    Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000009"), groupToGroupMappingEvent.EventId);
                    Assert.AreEqual(EventAction.Remove, groupToGroupMappingEvent.EventAction);
                    Assert.AreEqual(CreateDataTimeFromString("2023-03-18 23:49:35.0000009"), groupToGroupMappingEvent.OccurredTime);
                    Assert.AreEqual("group5", groupToGroupMappingEvent.FromGroup);
                    Assert.AreEqual("group6", groupToGroupMappingEvent.ToGroup);

                    // Check the number of times ToString() was called on the *Stringifier classes
                    //   Note that FromString() counts can't be checked as the IUniqueStringifier implementations used for deserialization are fixed in the EventCache.Program class.
                    Assert.AreEqual(4, userStringifier.ToStringCallCount);
                    Assert.AreEqual(6, groupStringifier.ToStringCallCount);
                    Assert.AreEqual(2, applicationComponentStringifier.ToStringCallCount);
                    Assert.AreEqual(2, accessLevelStringifier.ToStringCallCount);
                }
            }
        }

        [Test]
        public void GetAllEventsSince()
        {        
            const String EventIdPropertyName = "eventId";
            const String EventActionPropertyName = "eventAction";
            const String OccurredTimePropertyName = "occurredTime";
            const String EntityTypePropertyName = "entityType";
            const String EntityPropertyName = "entity";
            const String UserPropertyName = "user";
            const String GroupPropertyName = "group";
            const String ApplicationComponentPropertyName = "applicationComponent";
            const String AccessLevelPropertyName = "accessLevel";
            const String FromGroupPropertyName = "fromGroup";
            const String ToGroupPropertyName = "toGroup";
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            List<TemporalEventBufferItemBase> returnEvents = CreateTestEvents();
            mockTemporalEventQueryProcessor.GetAllEventsSince(priorEventdId).Returns(returnEvents);
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{requestUrl}?priorEventdId={priorEventdId}"))
            {

                using (HttpResponseMessage response = client.SendAsync(requestMessage).Result)
                {

                    mockTemporalEventQueryProcessor.Received(1).GetAllEventsSince(priorEventdId);
                    Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
                    String responseAsString = response.Content.ReadAsStringAsync().Result;
                    JArray responseJson = JArray.Parse(responseAsString);
                    Assert.AreEqual(10, responseJson.Count);

                    var entityTypeEventJson = (JObject)responseJson[0];
                    AssertJObjectContainsStringProperty(entityTypeEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000000");
                    AssertJObjectContainsStringProperty(entityTypeEventJson, EventActionPropertyName, "Remove");
                    AssertJObjectContainsStringProperty(entityTypeEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000000");
                    AssertJObjectContainsStringProperty(entityTypeEventJson, EntityTypePropertyName, "ClientAccount");

                    var entityEventJson = (JObject)responseJson[1];
                    AssertJObjectContainsStringProperty(entityEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000001");
                    AssertJObjectContainsStringProperty(entityEventJson, EventActionPropertyName, "Add");
                    AssertJObjectContainsStringProperty(entityEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000001");
                    AssertJObjectContainsStringProperty(entityEventJson, EntityTypePropertyName, "BusinessUnit");
                    AssertJObjectContainsStringProperty(entityEventJson, EntityPropertyName, "Sales");

                    var userToEntityMappingEventJson = (JObject)responseJson[2];
                    AssertJObjectContainsStringProperty(userToEntityMappingEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000002");
                    AssertJObjectContainsStringProperty(userToEntityMappingEventJson, EventActionPropertyName, "Remove");
                    AssertJObjectContainsStringProperty(userToEntityMappingEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000002");
                    AssertJObjectContainsStringProperty(userToEntityMappingEventJson, UserPropertyName, "user1");
                    AssertJObjectContainsStringProperty(userToEntityMappingEventJson, EntityTypePropertyName, "ClientAccount");
                    AssertJObjectContainsStringProperty(userToEntityMappingEventJson, EntityPropertyName, "ClientA");

                    var groupToEntityMappingEventJson = (JObject)responseJson[3];
                    AssertJObjectContainsStringProperty(groupToEntityMappingEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000003");
                    AssertJObjectContainsStringProperty(groupToEntityMappingEventJson, EventActionPropertyName, "Add");
                    AssertJObjectContainsStringProperty(groupToEntityMappingEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000003");
                    AssertJObjectContainsStringProperty(groupToEntityMappingEventJson, GroupPropertyName, "group1");
                    AssertJObjectContainsStringProperty(groupToEntityMappingEventJson, EntityTypePropertyName, "BusinessUnit");
                    AssertJObjectContainsStringProperty(groupToEntityMappingEventJson, EntityPropertyName, "Marketing");

                    var userEventJson = (JObject)responseJson[4];
                    AssertJObjectContainsStringProperty(userEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000004");
                    AssertJObjectContainsStringProperty(userEventJson, EventActionPropertyName, "Add");
                    AssertJObjectContainsStringProperty(userEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000004");
                    AssertJObjectContainsStringProperty(userEventJson, UserPropertyName, "user2");

                    var userToApplicationComponentAndAccessLevelMappingEventJson = (JObject)responseJson[5];
                    AssertJObjectContainsStringProperty(userToApplicationComponentAndAccessLevelMappingEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000005");
                    AssertJObjectContainsStringProperty(userToApplicationComponentAndAccessLevelMappingEventJson, EventActionPropertyName, "Remove");
                    AssertJObjectContainsStringProperty(userToApplicationComponentAndAccessLevelMappingEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000005");
                    AssertJObjectContainsStringProperty(userToApplicationComponentAndAccessLevelMappingEventJson, UserPropertyName, "user3");
                    AssertJObjectContainsStringProperty(userToApplicationComponentAndAccessLevelMappingEventJson, ApplicationComponentPropertyName, "Order");
                    AssertJObjectContainsStringProperty(userToApplicationComponentAndAccessLevelMappingEventJson, AccessLevelPropertyName, "Create");

                    var userToGroupMappingEventJson = (JObject)responseJson[6];
                    AssertJObjectContainsStringProperty(userToGroupMappingEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000006");
                    AssertJObjectContainsStringProperty(userToGroupMappingEventJson, EventActionPropertyName, "Add");
                    AssertJObjectContainsStringProperty(userToGroupMappingEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000006");
                    AssertJObjectContainsStringProperty(userToGroupMappingEventJson, UserPropertyName, "user4");
                    AssertJObjectContainsStringProperty(userToGroupMappingEventJson, GroupPropertyName, "group2");

                    var groupEventJson = (JObject)responseJson[7];
                    AssertJObjectContainsStringProperty(groupEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000007");
                    AssertJObjectContainsStringProperty(groupEventJson, EventActionPropertyName, "Remove");
                    AssertJObjectContainsStringProperty(groupEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000007");
                    AssertJObjectContainsStringProperty(groupEventJson, GroupPropertyName, "group3");

                    var groupToApplicationComponentAndAccessLevelMappingEventJson = (JObject)responseJson[8];
                    AssertJObjectContainsStringProperty(groupToApplicationComponentAndAccessLevelMappingEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000008");
                    AssertJObjectContainsStringProperty(groupToApplicationComponentAndAccessLevelMappingEventJson, EventActionPropertyName, "Add");
                    AssertJObjectContainsStringProperty(groupToApplicationComponentAndAccessLevelMappingEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000008");
                    AssertJObjectContainsStringProperty(groupToApplicationComponentAndAccessLevelMappingEventJson, GroupPropertyName, "group4");
                    AssertJObjectContainsStringProperty(groupToApplicationComponentAndAccessLevelMappingEventJson, ApplicationComponentPropertyName, "Summary");
                    AssertJObjectContainsStringProperty(groupToApplicationComponentAndAccessLevelMappingEventJson, AccessLevelPropertyName, "View");

                    var groupToGroupMappingEventJson = (JObject)responseJson[9];
                    AssertJObjectContainsStringProperty(groupToGroupMappingEventJson, EventIdPropertyName, "00000000-0000-0000-0000-000000000009");
                    AssertJObjectContainsStringProperty(groupToGroupMappingEventJson, EventActionPropertyName, "Remove");
                    AssertJObjectContainsStringProperty(groupToGroupMappingEventJson, OccurredTimePropertyName, "2023-03-18 23:49:35.0000009");
                    AssertJObjectContainsStringProperty(groupToGroupMappingEventJson, FromGroupPropertyName, "group5");
                    AssertJObjectContainsStringProperty(groupToGroupMappingEventJson, ToGroupPropertyName, "group6");
                }
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Asserts that the specified <see cref="JObject"/> containa a string property with the specified value.
        /// </summary>
        /// <param name="jObject">Yje JObject to assert on.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The value of the property.</param>
        protected void AssertJObjectContainsStringProperty(JObject jObject, String propertyName, String propertyValue)
        {
            Assert.IsNotNull(jObject[propertyName]);
            Assert.AreEqual(propertyValue, jObject[propertyName].ToString());
        }

        /// <summary>
        /// Creates a set of test events which derive from <see cref="TemporalEventBufferItemBase"/>.
        /// </summary>
        /// <returns>The test events.</returns>
        protected List<TemporalEventBufferItemBase> CreateTestEvents()
        {
            var testEvents = new List<TemporalEventBufferItemBase>()
            {
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000000"), EventAction.Remove, "ClientAccount", CreateDataTimeFromString("2023-03-18 23:49:35.0000000")),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000001"), EventAction.Add, "BusinessUnit", "Sales", CreateDataTimeFromString("2023-03-18 23:49:35.0000001")),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000002"), EventAction.Remove, "user1", "ClientAccount", "ClientA", CreateDataTimeFromString("2023-03-18 23:49:35.0000002")),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000003"), EventAction.Add, "group1", "BusinessUnit", "Marketing", CreateDataTimeFromString("2023-03-18 23:49:35.0000003")),
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000004"), EventAction.Add, "user2", CreateDataTimeFromString("2023-03-18 23:49:35.0000004")),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000005"), EventAction.Remove, "user3", "Order", "Create", CreateDataTimeFromString("2023-03-18 23:49:35.0000005")),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000006"), EventAction.Add, "user4", "group2", CreateDataTimeFromString("2023-03-18 23:49:35.0000006")),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000007"), EventAction.Remove, "group3", CreateDataTimeFromString("2023-03-18 23:49:35.0000007")),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000008"), EventAction.Add, "group4", "Summary", "View", CreateDataTimeFromString("2023-03-18 23:49:35.0000008")),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000009"), EventAction.Remove, "group5", "group6", CreateDataTimeFromString("2023-03-18 23:49:35.0000009"))
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

        #region Nested Classes

        /// <summary>
        /// Implementation of <see cref="IUniqueStringifier{T}"/> which counts the number of calls to the FromString() and ToString() methods.
        /// </summary>
        private class MethodCallCountingStringUniqueStringifier : IUniqueStringifier<String>
        {
            public Int32 FromStringCallCount { get; protected set; }
            public Int32 ToStringCallCount { get; protected set; }

            public MethodCallCountingStringUniqueStringifier()
            {
                FromStringCallCount = 0;
                ToStringCallCount = 0;
            }

            /// <inheritdoc/>
            public String FromString(String stringifiedObject)
            {
                FromStringCallCount++;

                return stringifiedObject;
            }

            /// <inheritdoc/>
            public String ToString(String inputObject)
            {
                ToStringCallCount++;

                return inputObject;
            }
        }

        #endregion
    }
}
