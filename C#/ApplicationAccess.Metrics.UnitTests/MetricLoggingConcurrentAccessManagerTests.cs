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
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;
using ApplicationAccess.UnitTests;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
    /// </summary>
    public class MetricLoggingConcurrentAccessManagerTests
    {
        private MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingConcurrentAccessManager;
        private IMetricLogger mockMetricLogger;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManager<string, string, ApplicationScreen, AccessLevel>(true, mockMetricLogger);
        }

        // TODO: Tests in MetricLoggingConcurrentDirectedGraphTests should be replicated here, but checking the relevant mapped metrics

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
        public void RemoveUser_ExceptionWhenRemoving()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void RemoveUser()
        {
            throw new NotImplementedException();
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
        public void RemoveGroup_ExceptionWhenRemoving()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void RemoveGroup()
        {
            throw new NotImplementedException();
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
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
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
            Assert.IsTrue(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
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
            Assert.IsFalse(testMetricLoggingConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
        }

    }
}
