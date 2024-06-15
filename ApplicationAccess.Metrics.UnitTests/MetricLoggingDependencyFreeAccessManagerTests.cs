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
using System.Linq;
using System.Threading;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using NSubstitute.Core;
using ApplicationMetrics;
using MoreComplexDataStructures;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggingDependencyFreeAccessManager class.
    /// </summary>
    /// <remarks>The metric logging functionality in this class is implemented in <see cref="ConcurrentAccessManagerMetricLogger{TUser, TGroup, TComponent, TAccess}"/> and tested in <see cref="MetricLoggingConcurrentAccessManagerTests"/>, hence only cursory tests are included in this class.</remarks>
    public class MetricLoggingDependencyFreeAccessManagerTests
    {
        private const String prependedEventsProcessedText = "prependedEventsProcessed";
        private const String beginMetricsLoggedText = "beginMetricsLogged";
        private const String postProcessingActionInvokedText = "postProcessingActionInvoked";
        private const String endMetricsLoggedText = "endMetricsLogged";

        private IMetricLogger mockMetricLogger;
        private IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel> mockEventProcessor;
        private ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingWrapper;
        private MetricLoggingDependencyFreeAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingDependencyFreeAccessManager;

        // N.b. for one of each of the types/classes of event methods (specifically AddUser(), RemoveUser(), AddUserToGroupMapping(), and RemoveUserToGroupMapping()) additional tests
        //   are included which are replicated from MetricLoggingConcurrentAccessManagerTests.  This is just to try to ensure that MetricLoggingDependencyFreeAccessManager implementation
        //   doesn't deviate from MetricLoggingConcurrentAccessManager, but without going to the extent of duplicating every test from MetricLoggingConcurrentAccessManagerTests.

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockEventProcessor = Substitute.For<IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel>>();
            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);
            testMetricLoggingDependencyFreeAccessManager.EventProcessor = mockEventProcessor;
            testMetricLoggingWrapper = new ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);
            // Test version of class exposes a setter, so that the test version of the decorator can be set after construction
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingWrapper = testMetricLoggingWrapper;
        }

        [Test]
        public void Constructor_MappingMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingDependencyFreeAccessManager;
            var fieldNamePath = new List<String>() { "mappingMetricLogger" };
            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testMetricLoggingDependencyFreeAccessManager);
        }

        [Test]
        public void Constructor_ThrowIdempotencyExceptionsParameterSetCorrectlyOnComposedFields()
        {
            MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingDependencyFreeAccessManager;
            var fieldNamePath = new List<String>() { "throwIdempotencyExceptions" };
            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testMetricLoggingDependencyFreeAccessManager);


            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testMetricLoggingDependencyFreeAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapAcquireLocksParameterSetCorrectlyOnComposedFields()
        {
            MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingDependencyFreeAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "acquireLocks" };
            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testMetricLoggingDependencyFreeAccessManager);


            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testMetricLoggingDependencyFreeAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingDependencyFreeAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "metricLogger" };
            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testMetricLoggingDependencyFreeAccessManager);
        }

        [Test]
        public void Clear()
        {
            testMetricLoggingDependencyFreeAccessManager.AddUser("user1");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user2");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group1");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group2");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group3");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group4");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group1", "group2");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group2", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            Assert.AreNotEqual(0, testMetricLoggingDependencyFreeAccessManager.Users.Count());
            Assert.AreNotEqual(0, testMetricLoggingDependencyFreeAccessManager.Groups.Count());

            testMetricLoggingDependencyFreeAccessManager.Clear();

            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            Assert.AreEqual(0, testMetricLoggingDependencyFreeAccessManager.Users.Count());
            Assert.AreEqual(0, testMetricLoggingDependencyFreeAccessManager.Groups.Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.FrequencyCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.FrequencyCount);
        }

        [Test]
        public void AddUser()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void AddUser_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);

            mockMetricLogger.Received(2).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<UserAddTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void AddUser_ExceptionWhenAdding()
        {
            String testUser = "user1";
            String mockExceptionMessage = "Postprocessing Exception";
            Action<String> postProcessingAction = (String user) =>
            {
                throw new Exception(mockExceptionMessage);
            };
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<Exception>(delegate
            {
                testMetricLoggingDependencyFreeAccessManager.AddUser(testUser, postProcessingAction);
            });
            
            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserAddTime>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
            // User actually gets added as exception happens using post-processing, and there's no transaction rollback functionality
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void AddUser_MetricLoggingDisabled()
        {
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";

            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void AddUser_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (user) =>
            {
                // Check that the user has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void ContainsUser()
        {
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingDependencyFreeAccessManager.ContainsUser(testUser);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsUserQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void ContainsUser_MetricLoggingDisabled()
        {
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);

            Boolean result = testMetricLoggingDependencyFreeAccessManager.ContainsUser(testUser);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUser()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user1");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user2");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user3");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group1");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group2");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group3");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group2");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user3", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user3", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user3", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveUser("user1");

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 5);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 5);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Users.Contains("user1"));


            // Additional scenario to test bug found with 'userToEntityMappingCountPerUser' member
            //   FrequencyTable.DecrementBy() method will not accept 0 parameter.
            testMetricLoggingDependencyFreeAccessManager.Clear();
            testMetricLoggingDependencyFreeAccessManager.AddUser("user4");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveUser("user4");

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Users.Contains("user4"));
        }

        [Test]
        public void RemoveUser_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.RemoveUser(testUser);

            mockMetricLogger.Received(2).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<UserRemoveTime>());


            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.ItemCount);
            Assert.AreEqual(9, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void RemoveUser_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            String mockExceptionMessage = "Postprocessing Exception";
            Action<String> postProcessingAction = (String user) =>
            {
                throw new Exception(mockExceptionMessage);
            };
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<Exception>(delegate
            {
                testMetricLoggingDependencyFreeAccessManager.RemoveUser(testUser, postProcessingAction);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserRemoveTime>());
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
            // User actually gets removed as exception happens using post-processing, and there's no transaction rollback functionality
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void RemoveUser_MetricLoggingDisabled()
        {
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingEnabled = false;
            testMetricLoggingDependencyFreeAccessManager.AddUser("user1");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user2");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user3");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group1");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group2");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group3");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group2");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user3", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user3", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user3", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Sales");

            testMetricLoggingDependencyFreeAccessManager.RemoveUser("user1");

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Users.Contains("user1"));
        }

        [Test]
        public void RemoveUser_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToEntityMap));
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (user) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.RemoveUser(testUser, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddGroup()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testGroup));
        }

        [Test]
        public void AddGroup_IdempotentCallDoesntLogMetrics()
        {
            String testGroup = "group1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);

            mockMetricLogger.Received(2).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<GroupAddTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testGroup));
        }

        [Test]
        public void ContainsGroup()
        {
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingDependencyFreeAccessManager.ContainsGroup(testGroup);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsGroupQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroup()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user1");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user2");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user3");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group1");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group2");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group3");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group4");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group5");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user3", "group2");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group1", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group2", "group3");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group3", "group5");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping("group1", "group4");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group4", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroup("group2");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 5);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Groups.Contains("group2"));


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroup("group3");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 3);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Groups.Contains("group3"));


            // Additional scenario to test bug found with 'groupToEntityMappingCountPerGroup' member
            //   FrequencyTable.DecrementBy() method will not accept 0 parameter.
            testMetricLoggingDependencyFreeAccessManager.Clear();
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group6");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroup("group6");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Groups.Contains("group6"));
        }

        [Test]
        public void RemoveGroup_IdempotentCallDoesntLogMetrics()
        {
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.RemoveGroup(testGroup);

            mockMetricLogger.Received(2).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<GroupRemoveTime>());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.ItemCount);
            Assert.AreEqual(10, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId3);

            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 1);
            Assert.AreEqual(12, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMapping_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            Guid testBeginId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId3, testBeginId4);

            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(2).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId4, Arg.Any<UserToGroupMappingAddTime>());
            Assert.AreEqual(14, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            String testGroup = "group1";
            var wrappingActionOrder = new List<String>();
            mockEventProcessor.When((processor) => processor.AddUser(testUser)).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToGroupMapLock));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (fromGroup, toGroup) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToGroupMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToGroupMapLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.AreEqual(4, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[3]);
        }

        [Test]
        public void AddUserToGroupMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String mockExceptionMessage = "Postprocessing Exception";
            Action<String, String> postProcessingAction = (String user, String group) =>
            {
                throw new Exception(mockExceptionMessage);
            };
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId3);

            var e = Assert.Throws<Exception>(delegate
            {
                testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId3, Arg.Any<UserToGroupMappingAddTime>());
            Assert.AreEqual(11, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
            // Mapping actually gets added as exception happens using post-processing, and there's no transaction rollback functionality
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMapping_MetricLoggingDisabled()
        {
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";

            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMapping_DependentElementsPartiallyCreated()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);

            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);

            mockEventProcessor.Received(1).AddGroup(testGroup);
            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void GetUserToGroupMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingDependencyFreeAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToUserMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetGroupToUserMappings(testGroup, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingDependencyFreeAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetGroupToUserMappings(testIndirectGroup, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void RemoveUserToGroupMapping_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.RemoveUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(2).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<UserToGroupMappingRemoveTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void RemoveUserToGroupMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String mockExceptionMessage = "Postprocessing Exception";
            Action<String, String> postProcessingAction = (String user, String group) =>
            {
                throw new Exception(mockExceptionMessage);
            };
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<Exception>(delegate
            {
                testMetricLoggingDependencyFreeAccessManager.RemoveUserToGroupMapping(testUser, testGroup, postProcessingAction);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToGroupMappingRemoveTime>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
            // Mapping actually gets removed as exception happens using post-processing, and there's no transaction rollback functionality
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void RemoveUserToGroupMapping_MetricLoggingDisabled()
        {
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false);

            Assert.IsTrue(result.Contains(testGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingDependencyFreeAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToGroupMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingDependencyFreeAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId1, testBeginId2);
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId3);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(2).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 2);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            Assert.AreEqual(12, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void AddGroupToGroupMapping_WrappingActionMethodOverrideOrdering()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            var wrappingActionOrder = new List<String>();
            mockEventProcessor.When((processor) => processor.AddGroup(testFromGroup)).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupToGroupMapLock));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (fromGroup, toGroup) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToGroupMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupToGroupMapLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.AreEqual(4, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[3]);
        }

        [Test]
        public void AddGroupToGroupMapping_IdempotentCallDoesntLogMetrics()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            Guid testBeginId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId1, testBeginId2);
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId3, testBeginId4);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(2).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 2);
            mockMetricLogger.Received(2).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId4, Arg.Any<GroupToGroupMappingAddTime>());
            Assert.AreEqual(14, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void AddGroupToGroupMapping_DependentElementsPartiallyCreated()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockEventProcessor.Received(1).AddGroup(testToGroup);
            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testFromGroup));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testToGroup));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void GetGroupToGroupMappingsGroupOverload()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testIndirectGroup = "group3";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false);

            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingDependencyFreeAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testToGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupReverseMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testIndirectGroup = "group3";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupReverseMappings(testToGroup, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingDependencyFreeAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testToGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupReverseMappings(testIndirectGroup, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void RemoveGroupToGroupMapping_IdempotentCallDoesntLogMetrics()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(2).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<GroupToGroupMappingRemoveTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            var wrappingActionOrder = new List<String>();
            mockEventProcessor.When((processor) => processor.AddUser(testUser)).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToComponentMap));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (user, applicationComponent, accessLevel) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToComponentMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            Assert.AreEqual(4, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[3]);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId2, testBeginId3);

            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(2).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId3, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            Assert.AreEqual(10, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings()
        {
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Create);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser);

            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Create);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.ManageProducts, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingDependencyFreeAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Delete, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(2).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_WrappingActionMethodOverrideOrdering()
        {
            String testGroup = "group1";
            var wrappingActionOrder = new List<String>();
            mockEventProcessor.When((processor) => processor.AddGroup(testGroup)).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupToComponentMap));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (group, applicationComponent, accessLevel) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupToComponentMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            Assert.AreEqual(4, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[3]);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_IdempotentCallDoesntLogMetrics()
        {
            String testGroup = "group1";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId2, testBeginId3);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(2).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId3, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            Assert.AreEqual(10, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings()
        {
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.ManageProducts, AccessLevel.Create);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup);

            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testFromGroup, ApplicationScreen.ManageProducts, AccessLevel.Create);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testToGroup, ApplicationScreen.ManageProducts, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingDependencyFreeAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Delete, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_IdempotentCallDoesntLogMetrics()
        {
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(2).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddEntityType()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
        }

        [Test]
        public void AddEntityType_IdempotentCallDoesntLogMetrics()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);

            mockMetricLogger.Received(2).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<EntityTypeAddTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));

        }

        [Test]
        public void ContainsEntityType()
        {
            String testEntityType = "ClientAccount";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingDependencyFreeAccessManager.ContainsEntityType(testEntityType);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityTypeQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveEntityType()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ProductType");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user1");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user2");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user3");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user4");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveEntityType("ClientAccount");

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains("ClientAccount"));


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveEntityType("ProductType");

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains("ProductType"));


            // Additional scenario to test bug found with 'userToEntityMappingCountPerUser' and 'groupToEntityMappingCountPerGroup' member
            //   FrequencyTable.DecrementBy() method will not accept 0 parameter.
            testMetricLoggingDependencyFreeAccessManager.Clear();
            testMetricLoggingDependencyFreeAccessManager.AddUser("user5");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group2");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user5", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveEntityType("ClientAccount");

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user5"));
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("group2"));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains("ClientAccount"));
        }

        [Test]
        public void RemoveEntityType_IdempotentCallDoesntLogMetrics()
        {
            String testEntityType = "ClientAccount";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.RemoveEntityType(testEntityType);

            mockMetricLogger.Received(2).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<EntityTypeRemoveTime>());
            Assert.AreEqual(0, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.ItemCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.ItemCount);
            Assert.AreEqual(9, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
        }

        [Test]
        public void AddEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.EntityCount);
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
        }

        [Test]
        public void AddEntity_WrappingActionMethodOverrideOrdering()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var wrappingActionOrder = new List<String>();
            mockEventProcessor.When((processor) => processor.AddEntityType(testEntityType)).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.Entities));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<EntityAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (entityType, entity) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<EntityAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.Entities));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(4, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[3]);
        }

        [Test]
        public void AddEntity_IdempotentCallDoesntLogMetrics()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId2, testBeginId3);

            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(2).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId3, Arg.Any<EntityAddTime>());
            Assert.AreEqual(10, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
        }

        [Test]
        public void GetEntities()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void ContainsEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingDependencyFreeAccessManager.ContainsEntity(testEntityType, testEntity);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveEntity()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType("ProductType");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user1");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user2");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user3");
            testMetricLoggingDependencyFreeAccessManager.AddUser("user4");
            testMetricLoggingDependencyFreeAccessManager.AddGroup("group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveEntity("BusinessUnit", "Marketing");

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), Arg.Any<Int64>());
            Assert.AreEqual(3, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(4, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetEntities("BusinessUnit").Contains("Marketing"));
        }

        [Test]
        public void RemoveEntity_IdempotentCallDoesntLogMetrics()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.RemoveEntity(testEntityType, testEntity);

            mockMetricLogger.Received(2).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<EntityRemoveTime>());
            Assert.AreEqual(0, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.ItemCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.ItemCount);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            Guid testBeginId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId4);

            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId4, Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 1);
            Assert.AreEqual(16, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddUserToEntityMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var wrappingActionOrder = new List<String>();
            mockEventProcessor.When((processor) => processor.AddUser(testUser)).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToEntityMap));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String, String> postProcessingAction = (user, entityType, entity) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToEntityMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(4, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[3]);
        }

        [Test]
        public void AddUserToEntityMapping_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            Guid testBeginId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            Guid testBeginId5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId4, testBeginId5);

            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            mockMetricLogger.Received(2).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId4, Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId5, Arg.Any<UserToEntityMappingAddTime>());
            Assert.AreEqual(18, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddUserToEntityMapping_DependentElementsPartiallyCreated()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);

            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockEventProcessor.Received(1).AddEntityType(testEntityType);
            mockEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));


            testMetricLoggingDependencyFreeAccessManager.Clear();
            mockEventProcessor.ClearReceivedCalls();
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);

            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void GetUserToEntityMappingsForUser()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<String, String>> result = testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser);

            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetUserToEntityMappingsForUserAndEntityType()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToUserMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "CompanyB";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingDependencyFreeAccessManager.GetEntityToUserMappings(testEntityType, testEntity1, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetEntityToUserMappings(testEntityType, testEntity2, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void RemoveUserToEntityMapping_IdempotentCallDoesntLogMetrics()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(2).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<UserToEntityMappingRemoveTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            Guid testBeginId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId4);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId4, Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(16, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency(testGroup));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddGroupToEntityMapping_WrappingActionMethodOverrideOrdering()
        {   
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var wrappingActionOrder = new List<String>();
            mockEventProcessor.When((processor) => processor.AddGroup(testGroup)).Do((callInfo) => 
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String, String> postProcessingAction = (group, entityType, entity) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToEntityMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(4, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[3]);
        }

        [Test]
        public void AddGroupToEntityMapping_IdempotentCallDoesntLogMetrics()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid testBeginId3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            Guid testBeginId4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            Guid testBeginId5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId4, testBeginId5);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            mockMetricLogger.Received(2).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId4, Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            mockMetricLogger.Received(1).CancelBegin(testBeginId5, Arg.Any<GroupToEntityMappingAddTime>());
            Assert.AreEqual(18, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddGroupToEntityMapping_DependentElementsPartiallyCreated()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockEventProcessor.Received(1).AddEntityType(testEntityType);
            mockEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));


            testMetricLoggingDependencyFreeAccessManager.Clear();
            mockEventProcessor.ClearReceivedCalls();
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void GetGroupToEntityMappingsForGroup()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<String, String>> result = testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup);

            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToEntityMappingsForGroupAndEntityType()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testGroup = "group1";
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToGroupMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "CompanyB";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testFromGroup, testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testToGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingDependencyFreeAccessManager.GetEntityToGroupMappings(testEntityType, testEntity1, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetEntityToGroupMappings(testEntityType, testEntity2, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void RemoveGroupToEntityMapping_IdempotentCallDoesntLogMetrics()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            Guid testBeginId1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid testBeginId2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>()).Returns(testBeginId1, testBeginId2);

            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(2).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<GroupToEntityMappingRemoveTime>());
            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void HasAccessToApplicationComponentUserOverload()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);

            Boolean result = testMetricLoggingDependencyFreeAccessManager.HasAccessToApplicationComponent(testUser, ApplicationScreen.Order, AccessLevel.Delete);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToEntityUserOverload()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);

            Boolean result = testMetricLoggingDependencyFreeAccessManager.HasAccessToEntity(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Summary, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingDependencyFreeAccessManager.GetApplicationComponentsAccessibleByUser(testUser);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup()
        {
            String testGroup1 = "group1";
            String testGroup2 = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup1);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup2);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup1, ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup2, ApplicationScreen.Summary, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingDependencyFreeAccessManager.GetApplicationComponentsAccessibleByGroup(testGroup1);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserOverload()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<String, String>> result = testMetricLoggingDependencyFreeAccessManager.GetEntitiesAccessibleByUser(testUser);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity1)));
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity2)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetEntitiesAccessibleByUser(testUser, testEntityType);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(testEntity1));
            Assert.IsTrue(result.Contains(testEntity2));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupOverload()
        {
            String testGroup1 = "group1";
            String testGroup2 = "group2";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup1);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup2);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup1, testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup2, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<String, String>> result = testMetricLoggingDependencyFreeAccessManager.GetEntitiesAccessibleByGroup(testGroup1);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity1)));
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity2)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload()
        {
            String testGroup1 = "group1";
            String testGroup2 = "group2";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup1);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup2);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup1, testEntityType, testEntity1);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup2, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetEntitiesAccessibleByGroup(testGroup1, testEntityType);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(testEntity1));
            Assert.IsTrue(result.Contains(testEntity2));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        #region Override Ordering In Subclass Test

        // This test tests that a class derived from MetricLoggingDependencyFreeAccessManager which overrides an event method overload with the 'wrappingAction' parameter (in this case 
        //   AddUserToGroupMapping()) preserves correct ordering of operations within the hierarchy of the overridden methods.

        [Test]
        public void AddUserToGroupMapping_WrappingActionMethodOverrideOrderingInDerivedclass()
        {
            var wrappingActionOrder = new List<String>();
            String preEventProcessingActionInvokedText = "preEventProcessingActionInvoked";
            String postEventProcessingActionInvokedText = "postEventProcessingActionInvoked";
            Action preEventProcessingAction = () =>
            {
                wrappingActionOrder.Add(preEventProcessingActionInvokedText);
            };
            Action postEventProcessingAction = () =>
            {
                wrappingActionOrder.Add(postEventProcessingActionInvokedText);
            };
            var testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManagerWithOverriddenEventMethod<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger, preEventProcessingAction, postEventProcessingAction);
            testMetricLoggingDependencyFreeAccessManager.EventProcessor = mockEventProcessor;
            testMetricLoggingWrapper = new ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);
            // Test version of class exposes a setter, so that the test version of the decorator can be set after construction
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingWrapper = testMetricLoggingWrapper;

            String testUser = "user1";
            String testGroup = "group1";
            mockEventProcessor.When((processor) => processor.AddUser(testUser)).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToGroupMapLock));
                wrappingActionOrder.Add(prependedEventsProcessedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>())).Do((callInfo) =>
            {
                Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (fromGroup, toGroup) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToGroupMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingDependencyFreeAccessManager.UserToGroupMapLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.AreEqual(6, wrappingActionOrder.Count);
            Assert.AreEqual(prependedEventsProcessedText, wrappingActionOrder[0]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(preEventProcessingActionInvokedText, wrappingActionOrder[2]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[3]);
            Assert.AreEqual(postEventProcessingActionInvokedText, wrappingActionOrder[4]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[5]);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the ConcurrentAccessManagerMetricLogger class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class ConcurrentAccessManagerMetricLoggerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Metrics.UnitTests.MetricLoggingConcurrentAccessManagerTests+ConcurrentAccessManagerMetricLoggerWithProtectedMembers class.
            /// </summary>
            /// <param name="metricLogger">The logger for metrics.</param>
            public ConcurrentAccessManagerMetricLoggerWithProtectedMembers(IMetricLogger metricLogger)
                : base(metricLogger)
            {
            }

            /// <summary>
            /// The number of user to application component and access level mappings in the access manager.
            /// </summary>
            public Int32 UserToApplicationComponentAndAccessLevelMappingCount
            {
                get { return userToApplicationComponentAndAccessLevelMappingCount; }
            }

            /// <summary>
            /// The number of group to application component and access level mappings in the access manager.
            /// </summary>
            public Int32 GroupToApplicationComponentAndAccessLevelMappingCount
            {
                get { return groupToApplicationComponentAndAccessLevelMappingCount; }
            }

            /// <summary>
            /// The number of entities in the access manager.
            /// </summary>
            public Int32 EntityCount
            {
                get { return entityCount; }
            }

            /// <summary>
            /// The number of user to entity mappings stored.
            /// </summary>
            public Int32 UserToEntityMappingCount
            {
                get { return userToEntityMappingCount; }
            }

            /// <summary>
            /// The number of user to entity mappings stored for each user.
            /// </summary>
            public FrequencyTable<TUser> UserToEntityMappingCountPerUser
            {
                get { return userToEntityMappingCountPerUser; }
            }

            /// <summary>
            /// The number of group tp entity mappings stored.
            /// </summary>
            public Int32 GroupToEntityMappingCount
            {
                get { return groupToEntityMappingCount; }
            }

            /// <summary>
            /// The number of group to entity mappings stored for each group.
            /// </summary>
            public FrequencyTable<TGroup> GroupToEntityMappingCountPerGroup
            {
                get { return groupToEntityMappingCountPerGroup; }
            }
        }

        /// <summary>
        /// Version of the MetricLoggingDependencyFreeAccessManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class MetricLoggingDependencyFreeAccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>Lock object for the users collection.</summary>
            public Object UsersLock
            {
                get { return usersLock; }
            }

            /// <summary>Lock object for the groups collection.</summary>
            public Object GroupsLock
            {
                get { return groupsLock; }
            }

            /// <summary>Lock object for the user to group map.</summary>
            public Object UserToGroupMapLock
            {
                get { return userToGroupMapLock; }
            }

            /// <summary>Lock object for the group to group map.</summary>
            public Object GroupToGroupMapLock
            {
                get { return groupToGroupMapLock; }
            }

            /// <summary>The user to application component and access level map as an object (to check for locking).</summary>
            public Object UserToComponentMap
            {
                get { return userToComponentMap; }
            }

            /// <summary>The group to application component and access level map as an object (to check for locking).</summary>
            public Object GroupToComponentMap
            {
                get { return groupToComponentMap; }
            }

            /// <summary>The entities colection as an object (to check for locking).</summary>
            public Object Entities
            {
                get { return entities; }
            }

            /// <summary>The user to entity map as an object (to check for locking).</summary>
            public Object UserToEntityMap
            {
                get { return userToEntityMap; }
            }

            /// <summary>The group to entity map as an object (to check for locking).</summary>
            public Object GroupToEntityMap
            {
                get { return groupToEntityMap; }
            }

            /// <summary>
            /// The logger for metrics.
            /// </summary>
            public ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess> MetricLoggingWrapper
            {
                set { metricLoggingWrapper = value; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Metrics.UnitTests.MetricLoggingDependencyFreeAccessManagerTests+MetricLoggingDependencyFreeAccessManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="metricLogger">The logger for metrics.</param>
            public MetricLoggingDependencyFreeAccessManagerWithProtectedMembers(IMetricLogger metricLogger)
                : base(metricLogger)
            {
            }
        }

        /// <summary>
        /// Version of the MetricLoggingDependencyFreeAccessManager class where the AddUserToGroupMapping() method overload with the 'wrappingAction' parameter is overridden to ensure the ordering of operations within the hierarchy of these methods is correct.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class MetricLoggingDependencyFreeAccessManagerWithOverriddenEventMethod<TUser, TGroup, TComponent, TAccess> : MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>Action to invoke before the 'baseAction' in overridden event methods.</summary>
            protected Action preEventProcessingAction;
            /// <summary>Action to invoke after the 'baseAction' in overridden event methods.</summary>
            protected Action postEventProcessingAction;

            /// <summary>Lock object for the users collection.</summary>
            public Object UsersLock
            {
                get { return usersLock; }
            }

            /// <summary>Lock object for the groups collection.</summary>
            public Object GroupsLock
            {
                get { return groupsLock; }
            }

            /// <summary>Lock object for the user to group map.</summary>
            public Object UserToGroupMapLock
            {
                get { return userToGroupMapLock; }
            }

            /// <summary>
            /// The logger for metrics.
            /// </summary>
            public ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess> MetricLoggingWrapper
            {
                set { metricLoggingWrapper = value; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Metrics.UnitTests.MetricLoggingDependencyFreeAccessManagerTests+MetricLoggingDependencyFreeAccessManagerWithOverriddenEventMethod class.
            /// </summary>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <param name="preEventProcessingAction">Action to invoke before the 'baseAction' in overridden event methods.</param>
            /// <param name="postEventProcessingAction">Action to invoke after the 'baseAction' in overridden event methods.</param>
            public MetricLoggingDependencyFreeAccessManagerWithOverriddenEventMethod
            (
                IMetricLogger metricLogger,
                Action preEventProcessingAction,
                Action postEventProcessingAction
            )
                : base(metricLogger)
            {
                this.preEventProcessingAction = preEventProcessingAction;
                this.postEventProcessingAction = postEventProcessingAction;
            }

            /// <inheritdoc/>
            protected override void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
            {
                Action<TUser, TGroup, Action> testAction = (actionUser, actionGroup, baseAction) =>
                {
                    preEventProcessingAction.Invoke();

                    wrappingAction.Invoke(actionUser, actionGroup, () =>
                    {
                        baseAction.Invoke();
                    });

                    postEventProcessingAction.Invoke();
                };
                base.AddUserToGroupMapping(user, group, testAction);
            }
        }

        #endregion
    }
}
