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
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;
using ApplicationAccess.UnitTests;
using MoreComplexDataStructures;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggingDependencyFreeAccessManager class.
    /// </summary>
    /// <remarks>The metric logging functionality in this class is implemented in <see cref="ConcurrentAccessManagerMetricLoggingInternalDecorator{TUser, TGroup, TComponent, TAccess}"/> and tested in <see cref="MetricLoggingConcurrentAccessManagerTests"/>, hence only cursory tests are included in this class.</remarks>
    public class MetricLoggingDependencyFreeAccessManagerTests
    {
        private IMetricLogger mockMetricLogger;
        private ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingWrapper;
        private MetricLoggingDependencyFreeAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingDependencyFreeAccessManager;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testMetricLoggingDependencyFreeAccessManager = new MetricLoggingDependencyFreeAccessManagerWithProtectedMembers<string, string, ApplicationScreen, AccessLevel>(false, mockMetricLogger);
            testMetricLoggingWrapper = new ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);
            // Test version of class exposes a setter, so that the test version of the decorator can be set after construction
            testMetricLoggingDependencyFreeAccessManager.MetricLoggingWrapper = testMetricLoggingWrapper;
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

            Assert.AreEqual(0, testMetricLoggingDependencyFreeAccessManager.Users.Count());
            Assert.AreEqual(0, testMetricLoggingDependencyFreeAccessManager.Groups.Count());
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
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
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

            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
        }

        [Test]
        public void AddUserPostProcessingActionOverload()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean postProcessingActionInvoked = false;
            Action<String> postProcessingAction = (user) => { postProcessingActionInvoked = true; };
            mockMetricLogger.Begin(Arg.Any<UserAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser, postProcessingAction);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsTrue(postProcessingActionInvoked);
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
        public void AddUserToGroupMapping()
        {
            // TODO: Now because of prepending metrics are logged for usersstored and groupsStored, so total ReceivedCalls() is 6 not 4
            //   Might run into probs here, because when entities or types are prepended I think their counts won't update... and counts will be wrong
            //   Also, it should really be logging UserAdded and GroupAdded and it's not
            //   Bloody hell, just gets more complex

            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            //testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            //testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToGroupMappingAddTime>()).Returns(testBeginId);

            //testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup, (actFromUser, actToUser) => { Console.WriteLine("POST PROCESSING"); });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 1);

            Assert.AreEqual(6, mockMetricLogger.ReceivedCalls().Count());
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
        public void AddGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToGroupMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.IsTrue(testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String testIndirectGroup = "group3";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testFromGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testToGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false);

            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());


            testMetricLoggingDependencyFreeAccessManager.AddGroup(testIndirectGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToGroupMapping(testToGroup, testIndirectGroup);
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsWithIndirectMappingsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            result = testMetricLoggingDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, true);

            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Contains(testIndirectGroup));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsWithIndirectMappingsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsWithIndirectMappingsQuery>());
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
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToApplicationComponentAndAccessLevelMappingCount);
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
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToApplicationComponentAndAccessLevelMappingCount);
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
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency("group2"));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.EntityTypes.Contains("ClientAccount"));
        }

        [Test]
        public void AddEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.EntityCount);
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
        public void AddUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<UserToEntityMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency(testUser));
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
        public void AddGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddEntityType(testEntityType);
            testMetricLoggingDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<GroupToEntityMappingAddTime>()).Returns(testBeginId);

            testMetricLoggingDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
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
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void HasAccessToApplicationComponent()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testMetricLoggingDependencyFreeAccessManager.AddUser(testUser);
            testMetricLoggingDependencyFreeAccessManager.AddGroup(testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentQueryTime>()).Returns(testBeginId);

            Boolean result = testMetricLoggingDependencyFreeAccessManager.HasAccessToApplicationComponent(testUser, ApplicationScreen.Order, AccessLevel.Delete);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToEntity()
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
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityQueryTime>()).Returns(testBeginId);

            Boolean result = testMetricLoggingDependencyFreeAccessManager.HasAccessToEntity(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityQuery>());
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
        /// Version of the MetricLoggingDependencyFreeAccessManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class MetricLoggingDependencyFreeAccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>
        {
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
            /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public MetricLoggingDependencyFreeAccessManagerWithProtectedMembers(Boolean storeBidirectionalMappings, IMetricLogger metricLogger)
                : base(storeBidirectionalMappings, metricLogger)
            {
            }
        }

        #endregion
    }
}
