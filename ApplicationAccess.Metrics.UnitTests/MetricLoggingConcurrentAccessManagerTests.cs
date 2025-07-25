﻿/*
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
using ApplicationMetrics;
using MoreComplexDataStructures;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
    /// </summary>
    /// <remarks>Most of the functionality tested in this class is in the <see cref="ConcurrentAccessManagerMetricLogger{TUser, TGroup, TComponent, TAccess}"/> class.  Better to test it through the <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> class than in isolation, as this represents the proper real-world use case.</remarks>
    public class MetricLoggingConcurrentAccessManagerTests
    {
        private const String beginMetricsLoggedText = "beginMetricsLogged";
        private const String postProcessingActionInvokedText = "postProcessingActionInvoked";
        private const String endMetricsLoggedText = "endMetricsLogged";

        private IMetricLogger mockMetricLogger;
        private ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingWrapper;
        private MetricLoggingConcurrentAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingConcurrentAccessManager;
       
        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);
            testMetricLoggingWrapper = new ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);
            // Test version of class exposes a setter, so that the test version of the wrapper can be set after construction
            testMetricLoggingConcurrentAccessManager.MetricLoggingWrapper = testMetricLoggingWrapper;
        }

        [Test]
        public void Constructor_MappingMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingConcurrentAccessManager;
            var fieldNamePath = new List<String>() { "mappingMetricLogger" };
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testMetricLoggingConcurrentAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapAcquireLocksParameterSetCorrectlyOnComposedFields()
        {
            MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingConcurrentAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "acquireLocks" };
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testMetricLoggingConcurrentAccessManager);


            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testMetricLoggingConcurrentAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingConcurrentAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "metricLogger" };
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testMetricLoggingConcurrentAccessManager);
        }

        [Test]
        public void Clear()
        {
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group2");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group2", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            Assert.AreNotEqual(0, testMetricLoggingConcurrentAccessManager.Users.Count());
            Assert.AreNotEqual(0, testMetricLoggingConcurrentAccessManager.Groups.Count());

            testMetricLoggingConcurrentAccessManager.Clear();

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
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.Users.Count());
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.Groups.Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.FrequencyCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.FrequencyCount);
        }

        [Test]
        public void Clear_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group2");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group2", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            Assert.AreNotEqual(0, testMetricLoggingConcurrentAccessManager.Users.Count());
            Assert.AreNotEqual(0, testMetricLoggingConcurrentAccessManager.Groups.Count());

            testMetricLoggingConcurrentAccessManager.Clear();

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.Users.Count());
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.Groups.Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.FrequencyCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.FrequencyCount);
        }

        [Test]
        public void AddUser_ExceptionWhenAdding()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UsersStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddUser()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void AddUserPostProcessingActionOverload()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String> postProcessingAction = (user) => { postProcessingActionInvoked = true; };
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUser(testUser, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Users.Contains(testUser));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddUser_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";

            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void AddUser_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (user) =>
            {
                // Check that the user has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddUser(testUser, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void ContainsUser()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsUser(testUser);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsUserQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void ContainsUser_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsUser(testUser);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUser_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUser("user1");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UsersStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveUser()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUser("user1");

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 5);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 5);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Users.Contains("user1"));


            // Additional scenario to test bug found with 'userToEntityMappingCountPerUser' member
            //   FrequencyTable.DecrementBy() method will not accept 0 parameter.
            testMetricLoggingConcurrentAccessManager.Clear();
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUser("user4");

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Users.Contains("user4"));
        }

        [Test]
        public void RemoveUserPostProcessingActionOverload()
        {
            Boolean postProcessingActionInvoked = false;
            Action<String> postProcessingAction = (user) => { postProcessingActionInvoked = true; };
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUser("user1", postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 5);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 5);
            Assert.AreEqual(7, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Users.Contains("user1"));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveUser_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group3");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group4");
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user3", "BusinessUnit", "Sales");

            testMetricLoggingConcurrentAccessManager.RemoveUser("user1");

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Users.Contains("user1"));
        }

        [Test]
        public void RemoveUser_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (user) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Users.Contains(testUser));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveUser(testUser, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddGroup_ExceptionWhenAdding()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddGroup()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Groups.Contains(testGroup));
        }

        [Test]
        public void AddGroupPostProcessingActionOverload()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String> postProcessingAction = (group) => { postProcessingActionInvoked = true; };
            mockMetricLogger.Begin(Arg.Any<GroupAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Groups.Contains(testGroup));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";

            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Groups.Contains(testGroup));
        }

        [Test]
        public void AddGroup_WrappingActionMethodOverrideOrdering()
        {
            String testGroup = "group1";
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains(testGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (user) =>
            {
                // Check that the user has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Groups.Contains(testGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void ContainsGroup()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsGroup(testGroup);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsGroupQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void ContainsGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsGroup(testGroup);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroup_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group5");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group1");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group5");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroup("group2");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToGroupMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveGroup()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group5");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group2");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group2", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group5");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroup("group2");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 5);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group2"));


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroup("group3");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 3);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group3"));


            // Additional scenario to test bug found with 'groupToEntityMappingCountPerGroup' member
            //   FrequencyTable.DecrementBy() method will not accept 0 parameter.
            testMetricLoggingConcurrentAccessManager.Clear();
            testMetricLoggingConcurrentAccessManager.AddGroup("group6");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroup("group6");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group6"));
        }

        [Test]
        public void RemoveGroupPostProcessingActionOverload()
        {
            Boolean postProcessingActionInvoked = false;
            Action<String> postProcessingAction = (group) => { postProcessingActionInvoked = true; };
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group5");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group2");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group2", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group5");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Sales");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroup("group2", postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 5);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group2"));
            Assert.IsTrue(postProcessingActionInvoked);


            mockMetricLogger.ClearReceivedCalls();
            postProcessingActionInvoked = false;
            mockMetricLogger.Begin(Arg.Any<GroupRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroup("group3", postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 3);
            Assert.AreEqual(8, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group3"));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddGroup("group3");
            testMetricLoggingConcurrentAccessManager.AddGroup("group4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group5");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user1", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user2", "group2");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group2");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group2", "group3");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group3", "group5");
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping("group1", "group4");
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Modify);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Order, AccessLevel.View);
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Sales");

            testMetricLoggingConcurrentAccessManager.RemoveGroup("group2");

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group2"));
        }

        [Test]
        public void RemoveGroup_WrappingActionMethodOverrideOrdering()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.Groups.Contains(testGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (group) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains(testGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveGroup(testGroup, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddUserToGroupMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToGroupMappingAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToGroupMappingAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMappingPostProcessingActionOverload()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String> postProcessingAction = (user, group) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddUserToGroupMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToGroupMapLock));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (fromGroup, toGroup) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToGroupMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToGroupMapLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void GetUserToGroupMappings_ExceptionWhenQuerying()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToGroupMappingsQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetUserToGroupMappingsQuery>());


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);

            e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, true);
            });
            ;
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQuery>());
        }

        [Test]
        public void GetUserToGroupMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetUserToGroupMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false);

            Assert.IsTrue(result.Contains(testGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToUserMappingsForGroup_ExceptionWhenQuerying()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetGroupToUserMappings(testGroup, false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetGroupToUserMappingsForGroupQuery>());


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);

            e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetGroupToUserMappings(testGroup, true);
            });
            ;
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQuery>());
        }

        [Test]
        public void GetGroupToUserMappingsForGroup()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToUserMappings(testGroup, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetGroupToUserMappings(testIndirectGroup, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToUserMappingsForGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            String testIndirectGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToUserMappings(testGroup, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup, testIndirectGroup);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetGroupToUserMappings(testIndirectGroup, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToGroupMapping_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToGroupMappingRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToGroupMappingRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void RemoveUserToGroupMappingPostProcessingActionOverload()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String> postProcessingAction = (user, group) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveUserToGroupMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            testMetricLoggingConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void RemoveUserToGroupMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToGroupMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToGroupMapLock));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (user, group) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToGroupMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToGroupMapLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddGroupToGroupMapping_ExceptionWhenAdding()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToGroupMappingAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToGroupMappingAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void AddGroupToGroupMappingPostProcessingActionOverload()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String> postProcessingAction = (fromGroup, toGroup) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddGroupToGroupMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);

            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void AddGroupToGroupMapping_WrappingActionMethodOverrideOrdering()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2"; 
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToGroupMapLock));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (fromGroup, toGroup) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToGroupMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToGroupMapLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void GetGroupToGroupMappings_ExceptionWhenQuerying()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testGroup, false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetGroupToGroupMappingsForGroupQuery>());


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);

            e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testGroup, true);
            });
            ;
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery>());
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testIndirectGroup = "group3";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false);

            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testToGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testIndirectGroup = "group3";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false);

            Assert.IsTrue(result.Contains(testToGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testToGroup, testIndirectGroup);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupReverseMappingsForGroup_ExceptionWhenQuerying()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetGroupToGroupReverseMappings(testGroup, false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupQuery>());


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);

            e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetGroupToGroupReverseMappings(testGroup, true);
            });
            ;
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery>());
        }

        [Test]
        public void GetGroupToGroupReverseMappingsForGroup()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testIndirectGroup = "group3";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupReverseMappings(testToGroup, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testToGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupReverseMappings(testIndirectGroup, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupReverseMappingsForGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testIndirectGroup = "group3";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupReverseMappings(testToGroup, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingConcurrentAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testToGroup, testIndirectGroup);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupReverseMappings(testIndirectGroup, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.IsTrue(result.Contains(testToGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToGroupMapping_ExceptionWhenRemoving()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToGroupMappingRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToGroupMappingRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void RemoveGroupToGroupMappingPostProcessingActionOverload()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String> postProcessingAction = (fromGroup, toGroup) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveGroupToGroupMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void RemoveGroupToGroupMapping_WrappingActionMethodOverrideOrdering()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToGroupMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToGroupMapLock));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (fromGroup, toGroup) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToGroupMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToGroupMapLock));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMappingPostProcessingActionOverload()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (user, applicationComponent, accessLevel) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1"; 
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToComponentMap));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (user, applicationComponent, accessLevel) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToComponentMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Create);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser);

            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Create);

            IEnumerable<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser);

            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.ManageProducts, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();
            
            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
            

            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Delete, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.ManageProducts, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Delete, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMappingPostProcessingActionOverload()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (user, applicationComponent, accessLevel) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            testMetricLoggingConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Modify);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToComponentMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (user, applicationComponent, accessLevel) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToComponentMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Modify, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_ExceptionWhenAdding()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMappingPostProcessingActionOverload()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (group, applicationComponent, accessLevel) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_WrappingActionMethodOverrideOrdering()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToComponentMap));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (group, applicationComponent, accessLevel) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToComponentMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.ManageProducts, AccessLevel.Create);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup);

            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.ManageProducts, AccessLevel.Create);

            IEnumerable<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup);

            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testFromGroup, ApplicationScreen.ManageProducts, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testToGroup, ApplicationScreen.ManageProducts, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Delete, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testFromGroup, ApplicationScreen.ManageProducts, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testToGroup, ApplicationScreen.ManageProducts, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Delete, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_ExceptionWhenRemoving()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMappingPostProcessingActionOverload()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (group, applicationComponent, accessLevel) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_WrappingActionMethodOverrideOrdering()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Modify);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToComponentMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (group, applicationComponent, accessLevel) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToComponentMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Modify, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddEntityType_ExceptionWhenAdding()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityTypeAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddEntityType()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
        }

        [Test]
        public void AddEntityTypePostProcessingActionOverload()
        {
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String> postProcessingAction = (entityType) => { postProcessingActionInvoked = true; };
            mockMetricLogger.Begin(Arg.Any<EntityTypeAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddEntityType_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";

            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
        }

        [Test]
        public void AddEntityType_WrappingActionMethodOverrideOrdering()
        {
            String testEntityType = "ClientAccount";
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<EntityTypeAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (user) =>
            {
                // Check that the user has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<EntityTypeAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void ContainsEntityType()
        {
            String testEntityType = "ClientAccount";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsEntityType(testEntityType);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityTypeQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void ContainsEntityType_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsEntityType(testEntityType);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveEntityType_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveEntityType("ProductType");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityTypeRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntityTypeRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntitiesStored>(), 2);
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), 2);
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), 2);
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(5, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(3, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user2"));
        }

        [Test]
        public void RemoveEntityType()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ProductType");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ClientAccount");

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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ClientAccount"));


            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ProductType");

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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ProductType"));


            // Additional scenario to test bug found with 'userToEntityMappingCountPerUser' and 'groupToEntityMappingCountPerGroup' member
            //   FrequencyTable.DecrementBy() method will not accept 0 parameter.
            testMetricLoggingConcurrentAccessManager.Clear();
            testMetricLoggingConcurrentAccessManager.AddUser("user5");
            testMetricLoggingConcurrentAccessManager.AddGroup("group2");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user5", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ClientAccount");

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
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency("group2"));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ClientAccount"));
        }

        [Test]
        public void RemoveEntityTypePostProcessingActionOverload()
        {
            Boolean postProcessingActionInvoked = false;
            Action<String> postProcessingAction = (entityType) => { postProcessingActionInvoked = true; };
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ProductType");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ClientAccount", postProcessingAction);

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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ClientAccount"));
            Assert.IsTrue(postProcessingActionInvoked);


            mockMetricLogger.ClearReceivedCalls();
            postProcessingActionInvoked = false;
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ProductType", postProcessingAction);

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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ProductType"));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveEntityType_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ProductType");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ClientAccount");

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(2, testMetricLoggingWrapper.EntityCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ClientAccount"));
        }

        [Test]
        public void RemoveEntityType_WrappingActionMethodOverrideOrdering()
        {
            String testEntityType = "ClientAccount";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<EntityTypeRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (entityType) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<EntityTypeRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveEntityType(testEntityType, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddEntity_ExceptionWhenAdding()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntitiesStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingWrapper.EntityCount);
        }

        [Test]
        public void AddEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.EntityCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
        }

        [Test]
        public void AddEntityPostProcessingActionOverload()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String> postProcessingAction = (entityType, entity) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.EntityCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddEntity_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);

            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.EntityCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
        }

        [Test]
        public void AddEntity_WrappingActionMethodOverrideOrdering()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<EntityAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (entityType, entity) =>
            {
                // Check that the entity has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<EntityAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void GetEntities()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntities_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void ContainsEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsEntity(testEntityType, testEntity);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void ContainsEntity_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsEntity(testEntityType, testEntity);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveEntity_ExceptionWhenRemoving()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ProductType");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveEntity("BusinessUnit", "Marketing");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EntityRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntitiesStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), Arg.Any<Int64>());
            Assert.AreEqual(3, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(4, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user2"));
        }

        [Test]
        public void RemoveEntity()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ProductType");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveEntity("BusinessUnit", "Marketing");

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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetEntities("BusinessUnit").Contains("Marketing"));
        }

        [Test]
        public void RemoveEntityPostProcessingActionOverload()
        {
            Boolean postProcessingActionInvoked = false;
            Action<String, String> postProcessingAction = (entityType, entity) => { postProcessingActionInvoked = true; };
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ProductType");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveEntity("BusinessUnit", "Marketing", postProcessingAction);

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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetEntities("BusinessUnit").Contains("Marketing"));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveEntity_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            testMetricLoggingConcurrentAccessManager.AddEntityType("ClientAccount");
            testMetricLoggingConcurrentAccessManager.AddEntityType("BusinessUnit");
            testMetricLoggingConcurrentAccessManager.AddEntityType("ProductType");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddEntity("ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddEntity("BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUser("user1");
            testMetricLoggingConcurrentAccessManager.AddUser("user2");
            testMetricLoggingConcurrentAccessManager.AddUser("user3");
            testMetricLoggingConcurrentAccessManager.AddUser("user4");
            testMetricLoggingConcurrentAccessManager.AddGroup("group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user3", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping("user4", "group1");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "BusinessUnit", "Sales");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Marketing");

            testMetricLoggingConcurrentAccessManager.RemoveEntity("BusinessUnit", "Marketing");

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(4, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetEntities("BusinessUnit").Contains("Marketing"));
        }

        [Test]
        public void RemoveEntity_WrappingActionMethodOverrideOrdering()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<EntityRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String> postProcessingAction = (testEntityType, testEntity) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<EntityRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveEntity(testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddUserToEntityMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToEntityMappingAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToEntityMappingAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddUserToEntityMappingPostProcessingActionOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String, String> postProcessingAction = (user, entityType, entity) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddUserToEntityMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddUserToEntityMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String, String> postProcessingAction = (user, entityType, entity) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToEntityMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void GetUserToEntityMappingsForUser()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser);

            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetUserToEntityMappingsForUser_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            IEnumerable<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser);

            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetUserToEntityMappingsForUserAndEntityType()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetUserToEntityMappingsForUserAndEntityType_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToUserMappings_ExceptionWhenQuerying()
        {
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntityToUserMappings(testEntityType, testEntity1, false);
            });

            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntityToUserMappingsQuery>());


            mockMetricLogger.ClearReceivedCalls();

            e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntityToUserMappings(testEntityType, testEntity1, true);
            });

            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
        }

        [Test]
        public void GetEntityToUserMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "CompanyB";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetEntityToUserMappings(testEntityType, testEntity1, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetEntityToUserMappings(testEntityType, testEntity2, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToUserMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToUserMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "CompanyB";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetEntityToUserMappings(testEntityType, testEntity1, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetEntityToUserMappings(testEntityType, testEntity2, true);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testUser));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToEntityMapping_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<UserToEntityMappingRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToEntityMappingRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void RemoveUserToEntityMappingPostProcessingActionOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String, String> postProcessingAction = (user, entityType, entity) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveUserToEntityMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            testMetricLoggingConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void RemoveUserToEntityMapping_WrappingActionMethodOverrideOrdering()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<UserToEntityMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser, testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String, String> postProcessingAction = (user, testEntityType, testEntity) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser, testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<UserToEntityMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.UserToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void AddGroupToEntityMapping_ExceptionWhenAdding()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToEntityMappingAddTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToEntityMappingAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddGroupToEntityMappingPostProcessingActionOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String, String> postProcessingAction = (group, entityType, entity) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void AddGroupToEntityMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddGroupToEntityMapping_WrappingActionMethodOverrideOrdering()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String, String> postProcessingAction = (group, entityType, entity) =>
            {
                // Check that the mapping has been added
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToEntityMappingAddTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void GetGroupToEntityMappingsForGroup()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup);

            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity)));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToEntityMappingsForGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            IEnumerable<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup);

            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToEntityMappingsForGroupAndEntityType()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToEntityMappingsForGroupAndEntityType_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToGroupMappings_ExceptionWhenQuerying()
        {
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntityToGroupMappings(testEntityType, testEntity1, false);
            });

            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntityToGroupMappingsQuery>());


            mockMetricLogger.ClearReceivedCalls();

            e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntityToGroupMappings(testEntityType, testEntity1, true);
            });

            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQuery>());
        }

        [Test]
        public void GetEntityToGroupMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "CompanyB";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testFromGroup, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testToGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetEntityToGroupMappings(testEntityType, testEntity1, false);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetEntityToGroupMappings(testEntityType, testEntity2, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testFromGroup));
            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntityToGroupMappingsWithIndirectMappingsQuery>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToGroupMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "CompanyB";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testFromGroup, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testToGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetEntityToGroupMappings(testEntityType, testEntity1, false);

            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingConcurrentAccessManager.GetEntityToGroupMappings(testEntityType, testEntity2, true);

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToEntityMapping_ExceptionWhenRemoving()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GroupToEntityMappingRemoveTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToEntityMappingRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void RemoveGroupToEntityMappingPostProcessingActionOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String, String, String> postProcessingAction = (group, entityType, entity) => { postProcessingActionInvoked = true; };
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>()).Returns(testBeginId);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
            Assert.IsTrue(postProcessingActionInvoked);
        }

        [Test]
        public void RemoveGroupToEntityMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            testMetricLoggingConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void RemoveGroupToEntityMapping_WrappingActionMethodOverrideOrdering()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<GroupToEntityMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup, testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String, String, String> postProcessingAction = (group, testEntityType, testEntity) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup, testEntityType).Contains(testEntity));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<GroupToEntityMappingRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testMetricLoggingConcurrentAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testMetricLoggingConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(3, wrappingActionOrder.Count);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[1]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[2]);
        }

        [Test]
        public void HasAccessToApplicationComponent_ExceptionWhenQuerying()
        {
            // TODO: Would like to add this test, but haven't been able to find a way to force the method to throw an exception (invalid users are ignored and return false)
        }

        [Test]
        public void HasAccessToApplicationComponent()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>()).Returns(testBeginId);

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToApplicationComponent(testUser, ApplicationScreen.Order, AccessLevel.Delete);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToApplicationComponent_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Delete);

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToApplicationComponent(testUser, ApplicationScreen.Order, AccessLevel.Delete);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToEntity_ExceptionWhenQuerying()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToEntity(testUser, testEntityType, "invalid entity");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<HasAccessToEntityForUserQuery>());
        }

        [Test]
        public void HasAccessToEntity()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForUserQueryTime>()).Returns(testBeginId);

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToEntity(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToEntity_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToEntity(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser_ExceptionWhenQuerying()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetApplicationComponentsAccessibleByUser(testUser);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetApplicationComponentsAccessibleByUserQuery>());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Summary, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentsAccessibleByUser(testUser);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Summary, AccessLevel.View);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentsAccessibleByUser(testUser);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup_ExceptionWhenQuerying()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetApplicationComponentsAccessibleByGroup(testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetApplicationComponentsAccessibleByGroupQuery>());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup()
        {
            String testGroup1 = "group1";
            String testGroup2 = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup1);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup1, ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup2, ApplicationScreen.Summary, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentsAccessibleByGroup(testGroup1);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup1 = "group1";
            String testGroup2 = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup1);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup1, ApplicationScreen.Order, AccessLevel.Create);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup2, ApplicationScreen.Summary, AccessLevel.View);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testMetricLoggingConcurrentAccessManager.GetApplicationComponentsAccessibleByGroup(testGroup1);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserOverload_ExceptionWhenQuerying()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByUser(testUser);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
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
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByUser(testUser);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity1)));
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity2)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserOverload_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);

            HashSet<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByUser(testUser);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity1)));
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity2)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload_ExceptionWhenQuerying()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByUser(testUser, testEntityType);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
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
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>()).Returns(testBeginId);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByUser(testUser, testEntityType);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(testEntity1));
            Assert.IsTrue(result.Contains(testEntity2));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByUserQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByUserQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity2);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByUser(testUser, testEntityType);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(testEntity1));
            Assert.IsTrue(result.Contains(testEntity2));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupOverload_ExceptionWhenQuerying()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByGroup(testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
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
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup1);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup1, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup2, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByGroup(testGroup1);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity1)));
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity2)));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupOverload_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup1 = "group1";
            String testGroup2 = "group2";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup1);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup1, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup2, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);

            HashSet<Tuple<String, String>> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByGroup(testGroup1);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity1)));
            Assert.IsTrue(result.Contains(new Tuple<String, String>(testEntityType, testEntity2)));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload_ExceptionWhenQuerying()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByGroup(testGroup, testEntityType);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
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
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup1);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup1, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup2, testEntityType, testEntity2);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>()).Returns(testBeginId);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByGroup(testGroup1, testEntityType);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(testEntity1));
            Assert.IsTrue(result.Contains(testEntity2));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup1 = "group1";
            String testGroup2 = "group2";
            String testEntityType = "ClientAccount";
            String testEntity1 = "CompanyA";
            String testEntity2 = "Companyb";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup1);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup2);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testGroup1, testGroup2);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity2);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup1, testEntityType, testEntity1);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup2, testEntityType, testEntity2);

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetEntitiesAccessibleByGroup(testGroup1, testEntityType);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(testEntity1));
            Assert.IsTrue(result.Contains(testEntity2));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

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
            /// The number of group to entity mappings stored for each user.
            /// </summary>
            public FrequencyTable<TGroup> GroupToEntityMappingCountPerUser
            {
                get { return groupToEntityMappingCountPerGroup; }
            }
        }

        /// <summary>
        /// Version of the MetricLoggingConcurrentAccessManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class MetricLoggingConcurrentAccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>
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
            public IDictionary<TUser, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> UserToComponentMap
            {
                get { return userToComponentMap; }
            }

            /// <summary>The group to application component and access level map as an object (to check for locking).</summary>
            public IDictionary<TGroup, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> GroupToComponentMap
            {
                get { return groupToComponentMap; }
            }

            /// <summary>The entities colection as an object (to check for locking).</summary>
            public IDictionary<String, ISet<String>> Entities
            {
                get { return entities; }
            }

            /// <summary>The user to entity map as an object (to check for locking).</summary>
            public IDictionary<TUser, IDictionary<String, ISet<String>>> UserToEntityMap
            {
                get { return userToEntityMap; }
            }

            /// <summary>The group to entity map as an object (to check for locking).</summary>
            public IDictionary<TGroup, IDictionary<String, ISet<String>>> GroupToEntityMap
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
            /// Initialises a new instance of the ApplicationAccess.Metrics.UnitTests.MetricLoggingConcurrentAccessManagerTests+MetricLoggingConcurrentAccessManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="metricLogger">The logger for metrics.</param>
            public MetricLoggingConcurrentAccessManagerWithProtectedMembers(IMetricLogger metricLogger)
                : base(metricLogger)
            {
            }
        }

        #endregion
    }
}
