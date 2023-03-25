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
using System.Net.Http;
using System.Runtime.ExceptionServices;
using ApplicationAccess.Persistence;
using ApplicationAccess.Hosting.Rest.EventCache.IntegrationTests;
using ApplicationAccess.Hosting.Rest.AsyncClient;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;
using Polly;

namespace ApplicationAccess.Hosting.Rest.Client.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Hosting.Rest.Client.EventCacheClient class.
    /// </summary>
    public class EventCacheClientTests : IntegrationTestsBase
    {
        private Uri testBaseUrl;
        private StringUniqueStringifier userStringifier;
        private StringUniqueStringifier groupStringifier;
        private StringUniqueStringifier applicationComponentStringifier;
        private StringUniqueStringifier accessLevelStringifier;
        private IApplicationLogger mockLogger;
        private IMetricLogger mockMetricLogger;
        private TestEventCacheClient<String, String, String, String> testEventCacheClient;

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
        }

        [OneTimeTearDown]
        protected override void OneTimeTearDown()
        {
            base.OneTimeTearDown();
        }

        [SetUp]
        protected void SetUp()
        {
            testBaseUrl = new Uri("http://localhost/");
            userStringifier = new StringUniqueStringifier();
            groupStringifier = new StringUniqueStringifier();
            applicationComponentStringifier = new StringUniqueStringifier();
            accessLevelStringifier = new StringUniqueStringifier();
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testEventCacheClient = new TestEventCacheClient<String, String, String, String>
            (
                testBaseUrl,
                client,
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier,
                5,
                1,
                mockLogger,
                mockMetricLogger
            );
            mockTemporalEventBulkPersister.ClearReceivedCalls();
            mockTemporalEventQueryProcessor.ClearReceivedCalls();
        }

        [TearDown]
        protected void TearDown()
        {
            testEventCacheClient.Dispose();
        }

        [Test]
        public void GetAllEventsSince()
        {
            var testEventId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            List<TemporalEventBufferItemBase> returnEvents = CreateTestEvents();
            mockTemporalEventQueryProcessor.GetAllEventsSince(testEventId).Returns(returnEvents);

            IList<TemporalEventBufferItemBase> result = testEventCacheClient.GetAllEventsSince(testEventId);

            mockTemporalEventQueryProcessor.Received(1).GetAllEventsSince(testEventId);
            Assert.AreEqual(10, result.Count);
            AssertEntityTypeEventBufferItemEqual
            (
                (EntityTypeEventBufferItem)returnEvents[0],
                (EntityTypeEventBufferItem)result[0]
            );
            AssertEntityEventBufferItemEqual
            (
                (EntityEventBufferItem)returnEvents[1],
                (EntityEventBufferItem)result[1]
            );
            AssertUserToEntityMappingEventBufferItemEqual
            (
                (UserToEntityMappingEventBufferItem<String>)returnEvents[2],
                (UserToEntityMappingEventBufferItem<String>)result[2]
            );
            AssertGroupToEntityMappingEventBufferItemEqual
            (
                (GroupToEntityMappingEventBufferItem<String>)returnEvents[3],
                (GroupToEntityMappingEventBufferItem<String>)result[3]
            );
            AssertUserEventBufferItemEqual
            (
                (UserEventBufferItem<String>)returnEvents[4],
                (UserEventBufferItem<String>)result[4]
            );
            AssertUserToApplicationComponentAndAccessLevelMappingEventBufferItemEqual
            (
                (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)returnEvents[5],
                (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)result[5]
            );
            AssertUserToGroupMappingEventBufferItemEqual
            (
                (UserToGroupMappingEventBufferItem<String, String>)returnEvents[6],
                (UserToGroupMappingEventBufferItem<String, String>)result[6]
            );
            AssertGroupEventBufferItemEqual
            (
                (GroupEventBufferItem<String>)returnEvents[7],
                (GroupEventBufferItem<String>)result[7]
            );
            AssertGroupToApplicationComponentAndAccessLevelMappingEventBufferItemEqual
            (
                (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)returnEvents[8],
                (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)result[8]
            );
            AssertGroupToGroupMappingEventBufferItemEqual
            (
                (GroupToGroupMappingEventBufferItem<String>)returnEvents[9],
                (GroupToGroupMappingEventBufferItem<String>)result[9]
            );
        }

        [Test]
        public void GetAllEventsSince_SpecifiedEventNotCached()
        {
            mockTemporalEventBulkPersister.ClearReceivedCalls();
            var testEventId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            String exceptionMessage = $"No event with eventId '{testEventId}' was found in the cache.";
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(testEventId)).Do((callInfo) => throw new EventNotCachedException(exceptionMessage));

            var e = Assert.Throws<EventNotCachedException>(delegate
            {
                testEventCacheClient.GetAllEventsSince(testEventId);
            });

            mockTemporalEventQueryProcessor.Received(1).GetAllEventsSince(testEventId);
            Assert.That(e.Message, Does.StartWith(exceptionMessage));
        }

        [Test]
        public void PersistEvents()
        {
            List<TemporalEventBufferItemBase> testEvents = CreateTestEvents();
            List<TemporalEventBufferItemBase> capturedEvents = null;
            mockTemporalEventBulkPersister.PersistEvents(Arg.Do<List<TemporalEventBufferItemBase>>(argumentValue => capturedEvents = argumentValue));

            testEventCacheClient.PersistEvents(testEvents);

            Assert.AreEqual(10, capturedEvents.Count);
            AssertEntityTypeEventBufferItemEqual
            (
                (EntityTypeEventBufferItem)testEvents[0],
                (EntityTypeEventBufferItem)capturedEvents[0]
            );
            AssertEntityEventBufferItemEqual
            (
                (EntityEventBufferItem)testEvents[1],
                (EntityEventBufferItem)capturedEvents[1]
            );
            AssertUserToEntityMappingEventBufferItemEqual
            (
                (UserToEntityMappingEventBufferItem<String>)testEvents[2],
                (UserToEntityMappingEventBufferItem<String>)capturedEvents[2]
            );
            AssertGroupToEntityMappingEventBufferItemEqual
            (
                (GroupToEntityMappingEventBufferItem<String>)testEvents[3],
                (GroupToEntityMappingEventBufferItem<String>)capturedEvents[3]
            );
            AssertUserEventBufferItemEqual
            (
                (UserEventBufferItem<String>)testEvents[4],
                (UserEventBufferItem<String>)capturedEvents[4]
            );
            AssertUserToApplicationComponentAndAccessLevelMappingEventBufferItemEqual
            (
                (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)testEvents[5],
                (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)capturedEvents[5]
            );
            AssertUserToGroupMappingEventBufferItemEqual
            (
                (UserToGroupMappingEventBufferItem<String, String>)testEvents[6],
                (UserToGroupMappingEventBufferItem<String, String>)capturedEvents[6]
            );
            AssertGroupEventBufferItemEqual
            (
                (GroupEventBufferItem<String>)testEvents[7],
                (GroupEventBufferItem<String>)capturedEvents[7]
            );
            AssertGroupToApplicationComponentAndAccessLevelMappingEventBufferItemEqual
            (
                (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)testEvents[8],
                (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>)capturedEvents[8]
            );
            AssertGroupToGroupMappingEventBufferItemEqual
            (
                (GroupToGroupMappingEventBufferItem<String>)testEvents[9],
                (GroupToGroupMappingEventBufferItem<String>)capturedEvents[9]
            );
        }

        [Test]
        public void RetryOnHttpRequestException()
        {
            using (var testClient = new HttpClient())
            {
                testBaseUrl = new Uri("http://www.acd8aac2-cb88-4296-b604-285f6132e449.com/");
                testEventCacheClient = new TestEventCacheClient<String, String, String, String>
                (
                    testBaseUrl,
                    testClient,
                    userStringifier,
                    groupStringifier,
                    applicationComponentStringifier,
                    accessLevelStringifier,
                    5,
                    1,
                    mockLogger,
                    mockMetricLogger
                );
                var testEventId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");

                var e = Assert.Throws<HttpRequestException>(delegate
                {
                    testEventCacheClient.GetAllEventsSince(testEventId);
                });

                mockLogger.Received(1).Log(testEventCacheClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 1 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testEventCacheClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 2 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testEventCacheClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 3 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testEventCacheClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 4 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testEventCacheClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 5 of 5).", Arg.Any<HttpRequestException>());
                mockMetricLogger.Received(5).Increment(Arg.Any<HttpRequestRetried>());
            }
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
        /// Asserts that the specified <see cref="UserToApplicationComponentAndAccessLevelMappingEventBufferItem{TUser, TComponent, TAccess}"/> instances are equal.
        /// </summary>
        protected void AssertUserToApplicationComponentAndAccessLevelMappingEventBufferItemEqual<TUser, TComponent, TAccess>
        (
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> expected, 
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> actual
        )
        {
            AssertTemporalEventBufferItemBaseEqual(expected, actual);
            Assert.AreEqual(expected.User, actual.User);
            Assert.AreEqual(expected.ApplicationComponent, actual.ApplicationComponent);
            Assert.AreEqual(expected.AccessLevel, actual.AccessLevel);
        }

        /// <summary>
        /// Asserts that the specified <see cref="GroupToApplicationComponentAndAccessLevelMappingEventBufferItem{TGroup, TComponent, TAccess}"/> instances are equal.
        /// </summary>
        protected void AssertGroupToApplicationComponentAndAccessLevelMappingEventBufferItemEqual<TGroup, TComponent, TAccess>
        (
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> expected,
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> actual
        )
        {
            AssertTemporalEventBufferItemBaseEqual(expected, actual);
            Assert.AreEqual(expected.Group, actual.Group);
            Assert.AreEqual(expected.ApplicationComponent, actual.ApplicationComponent);
            Assert.AreEqual(expected.AccessLevel, actual.AccessLevel);
        }

        /// <summary>
        /// Asserts that the specified <see cref="UserToGroupMappingEventBufferItem{TUser, TGroup}"/> instances are equal.
        /// </summary>
        protected void AssertUserToGroupMappingEventBufferItemEqual<TUser, TGroup>(UserToGroupMappingEventBufferItem<TUser, TGroup> expected, UserToGroupMappingEventBufferItem<TUser, TGroup> actual)
        {
            AssertTemporalEventBufferItemBaseEqual(expected, actual);
            Assert.AreEqual(expected.User, actual.User);
            Assert.AreEqual(expected.Group, actual.Group);
        }

        /// <summary>
        /// Asserts that the specified <see cref="GroupToGroupMappingEventBufferItem{TGroup}"/> instances are equal.
        /// </summary>
        protected void AssertGroupToGroupMappingEventBufferItemEqual<TGroup>(GroupToGroupMappingEventBufferItem<TGroup> expected, GroupToGroupMappingEventBufferItem<TGroup> actual)
        {
            AssertTemporalEventBufferItemBaseEqual(expected, actual);
            Assert.AreEqual(expected.ToGroup, actual.ToGroup);
            Assert.AreEqual(expected.FromGroup, actual.FromGroup);
        }

        /// <summary>
        /// Asserts that the specified <see cref="UserEventBufferItem{TUser}"/> instances are equal.
        /// </summary>
        protected void AssertUserEventBufferItemEqual<TUser>(UserEventBufferItem<TUser> expected, UserEventBufferItem<TUser> actual)
        {
            AssertTemporalEventBufferItemBaseEqual(expected, actual);
            Assert.AreEqual(expected.User, actual.User);
        }

        /// <summary>
        /// Asserts that the specified <see cref="GroupEventBufferItem{TGroup}"/> instances are equal.
        /// </summary>
        protected void AssertGroupEventBufferItemEqual<TGroup>(GroupEventBufferItem<TGroup> expected, GroupEventBufferItem<TGroup> actual)
        {
            AssertTemporalEventBufferItemBaseEqual(expected, actual);
            Assert.AreEqual(expected.Group, actual.Group);
        }

        /// <summary>
        /// Asserts that the specified <see cref="UserToEntityMappingEventBufferItem{TUser}"/> instances are equal.
        /// </summary>
        protected void AssertUserToEntityMappingEventBufferItemEqual<TUser>(UserToEntityMappingEventBufferItem<TUser> expected, UserToEntityMappingEventBufferItem<TUser> actual)
        {
            AssertEntityTypeEventBufferItemEqual(expected, actual);
            Assert.AreEqual(expected.User, actual.User);
        }

        /// <summary>
        /// Asserts that the specified <see cref="GroupToEntityMappingEventBufferItem{TGroup}"/> instances are equal.
        /// </summary>
        protected void AssertGroupToEntityMappingEventBufferItemEqual<TGroup>(GroupToEntityMappingEventBufferItem<TGroup> expected, GroupToEntityMappingEventBufferItem<TGroup> actual)
        {
            AssertEntityTypeEventBufferItemEqual(expected, actual);
            Assert.AreEqual(expected.Group, actual.Group);
        }

        /// <summary>
        /// Asserts that the specified <see cref="EntityEventBufferItem"/> instances are equal.
        /// </summary>
        protected void AssertEntityEventBufferItemEqual(EntityEventBufferItem expected, EntityEventBufferItem actual)
        {
            AssertEntityTypeEventBufferItemEqual(expected, actual);
            Assert.AreEqual(expected.Entity, actual.Entity);
        }

        /// <summary>
        /// Asserts that the specified <see cref="EntityTypeEventBufferItem"/> instances are equal.
        /// </summary>
        protected void AssertEntityTypeEventBufferItemEqual(EntityTypeEventBufferItem expected, EntityTypeEventBufferItem actual)
        {
            AssertTemporalEventBufferItemBaseEqual(expected, actual);
            Assert.AreEqual(expected.EntityType, actual.EntityType);
        }

        /// <summary>
        /// Asserts that the specified <see cref="TemporalEventBufferItemBase"/> instances are equal.
        /// </summary>
        protected void AssertTemporalEventBufferItemBaseEqual(TemporalEventBufferItemBase expected, TemporalEventBufferItemBase actual)
        {
            Assert.AreEqual(expected.EventId, actual.EventId);
            Assert.AreEqual(expected.EventAction, actual.EventAction);
            Assert.AreEqual(expected.OccurredTime, actual.OccurredTime);
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
        /// Test version of the <see cref="EventCacheClient{TUser, TGroup, TComponent, TAccess}"/> class which overrides the SendRequestMessage() method so the class can be tested synchronously using <see cref="WebApplicationFactory{TEntryPoint}"/>.
        /// </summary>
        /// <remarks>Testing the <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> class directly using <see cref="WebApplicationFactory{TEntryPoint}"/> resulted in error "The synchronous method is not supported by 'Microsoft.AspNetCore.TestHost.ClientHandler'".  Judging by the <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.clienthandler?view=aspnetcore-6.0"> documentation for the clienthandler class</see> (which I assume wraps HttpClient calls), it only supports a SendAsync() method.  Given support for the syncronous <see cref="HttpClient.Send(HttpRequestMessage)">HttpClient.Send()</see> was only ontroduced in .NET 5, I'm assuming this is yet to be supported by clients generated via the <see cref="WebApplicationFactory{TEntryPoint}.CreateClient">WebApplicationFactory.CreateClient()</see> method.  Hence, in order to test the class, this class overrides the SendRequest() method to call the HttpClient using the SendAsync() method and 'Result' property.  Although you wouldn't do this in released code (due to risk of deadlocks in certain run contexts outlined <see href="https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d">here</see>, better to test the other functionality in the class (exception handling, response parsing, etc...) than not to test at all.</remarks>
        private class TestEventCacheClient<TUser, TGroup, TComponent, TAccess> : EventCacheClient<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.EventCacheClientTests+TestEventCacheClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            public TestEventCacheClient
            (
                Uri baseUrl,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval
            )
                : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.EventCacheClientTests+TestEventCacheClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public TestEventCacheClient
            (
                Uri baseUrl,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.EventCacheClientTests+TestEventCacheClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            public TestEventCacheClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.EventCacheClientTests+TestEventCacheClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public TestEventCacheClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.EventCacheClientTests+TestEventCacheClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="exceptionHandingPolicy">Exception handling policy for HttpClient calls.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <remarks>When setting parameter 'exceptionHandingPolicy', note that the web API only returns non-success HTTP status errors in the case of persistent, and non-transient errors (e.g. 400 in the case of bad/malformed requests, and 500 in the case of critical server-side errors).  Retrying the same request after receiving these error statuses will result in an identical response, and hence these statuses are not passed to Polly and will be ignored if included as part of a transient exception handling policy.  Exposing of this parameter is designed to allow overriding of the retry policy and actions when encountering <see cref="HttpRequestException">HttpRequestExceptions</see> caused by network errors, etc.</remarks>
            public TestEventCacheClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Policy exceptionHandingPolicy,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, exceptionHandingPolicy, logger, metricLogger)
            {
            }

            /// <inheritdoc/>
            protected override HttpResponseMessage SendRequestMessage(HttpRequestMessage request)
            {
                // See class remarks for explanation of the need to override this.
                try
                {
                    return httpClient.SendAsync(request).Result;
                }
                catch (AggregateException ae)
                {
                    // Since the SendAsync() method is used above, it will throw an AggregateException on failure which needs to be rethrown as its base exception to be able to properly test retries with the syncronous version of the Polly.Policy used by EventCacheClient
                    ExceptionDispatchInfo.Capture(ae.GetBaseException()).Throw();
                    throw new Exception($"Unexpected failure to rethrow {typeof(AggregateException).Name}.");
                }
            }
        }

        #endregion
    }
}
