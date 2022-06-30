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
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;
using ApplicationAccess.UnitTests;
using MoreComplexDataStructures;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
    /// </summary>
    public class MetricLoggingConcurrentAccessManagerTests
    {
        private MetricLoggingConcurrentAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingConcurrentAccessManager;
        private IMetricLogger mockMetricLogger;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManagerWithProtectedMembers<string, string, ApplicationScreen, AccessLevel>(true, mockMetricLogger);
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

            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.Users.Count());
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.Groups.Count());
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.FrequencyCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCountPerUser.FrequencyCount);
        }

        [Test]
        public void AddUser_ExceptionWhenAdding()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UsersAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UsersStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddUser()
        {
            String testUser = "user1";

            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UsersAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 1);
            testMetricLoggingConcurrentAccessManager.Users.Contains(testUser);
        }

        [Test]
        public void AddUser_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";

            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            testMetricLoggingConcurrentAccessManager.Users.Contains(testUser);
        }

        [Test]
        public void ContainsUser()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsUser(testUser);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsUserQueries>());
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

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUser("user1");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UsersRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UsersStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveUser()
        {
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

            testMetricLoggingConcurrentAccessManager.RemoveUser("user1");

            mockMetricLogger.Received(1).Begin(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UsersRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UsersStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 5);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 5);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Users.Contains("user1"));
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
        public void AddGroup_ExceptionWhenAdding()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupsAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddGroup()
        {
            String testGroup = "group1";

            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupsAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 1);
            testMetricLoggingConcurrentAccessManager.Users.Contains(testGroup);
        }

        [Test]
        public void AddGroup_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";

            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            testMetricLoggingConcurrentAccessManager.Users.Contains(testGroup);
        }

        [Test]
        public void ContainsGroup()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsGroup(testGroup);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsGroupQueries>());
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

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroup("group2");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupsRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToGroupMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveGroup()
        {
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

            testMetricLoggingConcurrentAccessManager.RemoveGroup("group2");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 5);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group2"));


            mockMetricLogger.ClearReceivedCalls();
            testMetricLoggingConcurrentAccessManager.RemoveGroup("group3");

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 3);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.Groups.Contains("group3"));
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
        public void AddUserToGroupMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToGroupMappingsAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingsAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 1);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser).Contains(testGroup));
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
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser).Contains(testGroup));
        }

        [Test]
        public void GetUserToGroupMappings()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser);

            Assert.IsTrue(result.Contains(testGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToGroupMappingsQueries>());
        }

        [Test]
        public void GetUserToGroupMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser);

            Assert.IsTrue(result.Contains(testGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToGroupMapping_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToGroupMappingsRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToGroupMappingsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToGroupMappingsStored>(), 0);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser).Contains(testGroup));
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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToGroupMappings(testUser).Contains(testGroup));
        }

        [Test]
        public void AddGroupToGroupMapping_ExceptionWhenAdding()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToGroupMappingsAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupToGroupMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingsAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 1);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup).Contains(testToGroup));
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
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup).Contains(testToGroup));
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup);

            Assert.IsTrue(result.Contains(testToGroup));
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsQueries>());
        }

        [Test]
        public void GetGroupToGroupMappings_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            IEnumerable<String> result = testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup);

            Assert.IsTrue(result.Contains(testToGroup));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToGroupMapping_ExceptionWhenRemoving()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToGroupMappingsRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToGroupMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testMetricLoggingConcurrentAccessManager.AddGroup(testFromGroup);
            testMetricLoggingConcurrentAccessManager.AddGroup(testToGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupToGroupMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToGroupMappingsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToGroupMappingsStored>(), 0);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup).Contains(testToGroup));
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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup).Contains(testToGroup));
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToApplicationComponentAndAccessLevelMappingCount);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToApplicationComponentAndAccessLevelMappingsQueries>());
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
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
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
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_ExceptionWhenAdding()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToApplicationComponentAndAccessLevelMappingCount);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 1);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_MetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager.MetricLoggingEnabled = false;
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);

            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToApplicationComponentAndAccessLevelMappingsQueries>());
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
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_ExceptionWhenRemoving()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.View);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToApplicationComponentAndAccessLevelMappingsStored>(), 0);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
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
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToApplicationComponentAndAccessLevelMappingCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

        [Test]
        public void AddEntityType_ExceptionWhenAdding()
        {
            String testEntityType = "ClientAccount";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntityTypesAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), Arg.Any<Int64>());
        }

        [Test]
        public void AddEntityType()
        {
            String testEntityType = "ClientAccount";

            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<EntityTypeAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypesAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains(testEntityType));
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
        public void ContainsEntityType()
        {
            String testEntityType = "ClientAccount";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.ContainsEntityType(testEntityType);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityTypeQueries>());
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

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveEntityType("ProductType");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntityTypesRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntitiesStored>(), 2);
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), 2);
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), 2);
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(4, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.AreEqual(5, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(3, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user2"));
        }

        [Test]
        public void RemoveEntityType()
        {
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

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ClientAccount");

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypesRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ClientAccount"));


            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.RemoveEntityType("ProductType");

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypesRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 2);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ProductType"));
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
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.EntityTypes.Contains("ClientAccount"));
        }

        [Test]
        public void AddEntity_ExceptionWhenAdding()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<EntityAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntitiesAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntitiesStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.EntityCount);
        }

        [Test]
        public void AddEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<EntityAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntitiesAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 1);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
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
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesQueries>());
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
            mockMetricLogger.Received(1).Increment(Arg.Any<ContainsEntityQueries>());
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

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveEntity("BusinessUnit", "Marketing");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntitiesRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntitiesStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), Arg.Any<Int64>());
            Assert.AreEqual(3, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.AreEqual(4, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user2"));
        }

        [Test]
        public void RemoveEntity()
        {
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

            testMetricLoggingConcurrentAccessManager.RemoveEntity("BusinessUnit", "Marketing");

            mockMetricLogger.Received(1).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntitiesRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), Arg.Any<Int64>());
            Assert.AreEqual(3, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.AreEqual(4, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetEntities("BusinessUnit").Contains("Marketing"));
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
            Assert.AreEqual(3, testMetricLoggingConcurrentAccessManager.EntityCount);
            Assert.AreEqual(4, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user1"));
            Assert.AreEqual(2, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency("user2"));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetEntities("BusinessUnit").Contains("Marketing"));
        }

        [Test]
        public void AddUserToEntityMapping_ExceptionWhenAdding()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToEntityMappingsAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency(testUser));
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingsAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 1);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
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
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserQueries>());
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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetUserToEntityMappingsForUserAndEntityTypeQueries>());
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
        public void RemoveUserToEntityMapping_ExceptionWhenRemoving()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<UserToEntityMappingsRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<UserToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency(testUser));
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UserToEntityMappingsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 0);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
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
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.UserToEntityMappingCountPerUser.GetFrequency(testUser));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetUserToEntityMappings(testUser).Contains(new Tuple<String, String>(testEntityType, testEntity)));
        }

        [Test]
        public void AddGroupToEntityMapping_ExceptionWhenAdding()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToEntityMappingsAdded>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupToEntityMappingAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingsAdded>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 1);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
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
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupQueries>());
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
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToEntityMappingsForGroupAndEntityTypeQueries>());
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
        public void RemoveGroupToEntityMapping_ExceptionWhenRemoving()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GroupToEntityMappingsRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<GroupToEntityMappingsStored>(), Arg.Any<Int64>());
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddEntityType(testEntityType);
            testMetricLoggingConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testMetricLoggingConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockMetricLogger.ClearReceivedCalls();

            testMetricLoggingConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockMetricLogger.Received(1).Begin(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GroupToEntityMappingRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GroupToEntityMappingsRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 0);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
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
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingConcurrentAccessManager.GroupToEntityMappingCountPerUser.GetFrequency(testGroup));
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToEntityMappings(testGroup).Contains(new Tuple<String, String>(testEntityType, testEntity)));
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
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToApplicationComponent(testUser, ApplicationScreen.Order, AccessLevel.Delete);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentQueryTime>());
            mockMetricLogger.Received(1).End(Arg.Any<HasAccessToApplicationComponentQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentQueries>());
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
        public void HasAccessToApplicationComponent_IntervalMetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManagerWithProtectedMembers<string, string, ApplicationScreen, AccessLevel>(false, mockMetricLogger);
            String testUser = "user1";
            String testGroup = "group1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            testMetricLoggingConcurrentAccessManager.AddGroup(testGroup);
            testMetricLoggingConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            testMetricLoggingConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Delete);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToApplicationComponent(testUser, ApplicationScreen.Order, AccessLevel.Delete);

            Assert.IsTrue(result);
            mockMetricLogger.DidNotReceive().Begin(Arg.Any<HasAccessToApplicationComponentQueryTime>());
            mockMetricLogger.DidNotReceive().End(Arg.Any<HasAccessToApplicationComponentQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentQueries>());
        }

        [Test]
        public void HasAccessToEntity_ExceptionWhenQuerying()
        {
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
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToEntity(testUser, testEntityType, "invalid entity");
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<HasAccessToEntityQueries>());
        }

        [Test]
        public void HasAccessToEntity()
        {
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
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToEntity(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.Received(1).End(Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityQueries>());
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
        public void HasAccessToEntity_IntervalMetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManagerWithProtectedMembers<string, string, ApplicationScreen, AccessLevel>(false, mockMetricLogger);
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
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testMetricLoggingConcurrentAccessManager.HasAccessToEntity(testUser, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockMetricLogger.DidNotReceive().Begin(Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.DidNotReceive().End(Arg.Any<HasAccessToEntityQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityQueries>());
        }

        [Test]
        public void GetAccessibleEntities_ExceptionWhenQuerying()
        {
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
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetAccessibleEntities("invalid user", testEntityType);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<GetAccessibleEntitiesQueryTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<GetAccessibleEntitiesQueryTime>());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<GetAccessibleEntitiesQueries>());
        }

        [Test]
        public void GetAccessibleEntities()
        {
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
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetAccessibleEntities(testUser, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetAccessibleEntitiesQueryTime>());
            mockMetricLogger.Received(1).End(Arg.Any<GetAccessibleEntitiesQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetAccessibleEntitiesQueries>());
        }

        [Test]
        public void GetAccessibleEntities_MetricLoggingDisabled()
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
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetAccessibleEntities(testUser, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetAccessibleEntities_IntervalMetricLoggingDisabled()
        {
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManagerWithProtectedMembers<string, string, ApplicationScreen, AccessLevel>(false, mockMetricLogger);
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
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testMetricLoggingConcurrentAccessManager.GetAccessibleEntities(testUser, testEntityType);

            Assert.IsTrue(result.Contains(testEntity));
            mockMetricLogger.DidNotReceive().Begin(Arg.Any<GetAccessibleEntitiesQueryTime>());
            mockMetricLogger.DidNotReceive().End(Arg.Any<GetAccessibleEntitiesQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetAccessibleEntitiesQueries>());
        }

        #region Nested Classes

        /// <summary>
        /// Version of the MetricLoggingConcurrentAccessManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class MetricLoggingConcurrentAccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>
        {
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

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Metrics.UnitTests.MetricLoggingConcurrentAccessManagerTests+MetricLoggingConcurrentAccessManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="logQueryProcessorIntervalMetrics">Whether interval metrics should be logged for methods belonging to the IAccessManagerQueryProcessor interface.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public MetricLoggingConcurrentAccessManagerWithProtectedMembers(Boolean logQueryProcessorIntervalMetrics, IMetricLogger metricLogger)
                : base(logQueryProcessorIntervalMetrics, metricLogger)
            {
            }
        }

        #endregion
    }
}
