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
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.DependencyFreeAccessManager class.
    /// </summary>
    public class DependencyFreeAccessManagerTests
    {
        // N.b. First test of each 'type' of event (primary add, primary remove, secondary add, secondary remove) contains comments explaining purpose of each test

        private IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel> mockEventProcessor;
        private DependencyFreeAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testDependencyFreeAccessManager;

        [SetUp]
        protected void SetUp()
        {
            mockEventProcessor = Substitute.For<IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel>>();
            testDependencyFreeAccessManager = new DependencyFreeAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(true);
            testDependencyFreeAccessManager.EventProcessor = mockEventProcessor;
        }

        [Test]
        public void Constructor_StoreBidirectionalMappingsParameterSetCorrectlyOnComposedFields()
        {
            DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> testDependencyFreeAccessManager;
            var fieldNamePath = new List<String>() { "storeBidirectionalMappings" };
            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentDirectedGraph<String, String>(false), true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentDirectedGraph<String, String>(true), false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentCollectionFactory(), true, false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentCollectionFactory(), false, true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);
        }

        [Test]
        public void Constructor_ThrowIdempotencyExceptionsParameterSetCorrectlyOnComposedFields()
        {
            DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> testDependencyFreeAccessManager;
            var fieldNamePath = new List<String>() { "throwIdempotencyExceptions" };
            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentDirectedGraph<String, String>(true), true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentDirectedGraph<String, String>(true), true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentCollectionFactory(), false, true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDependencyFreeAccessManager);


            testDependencyFreeAccessManager = new DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentCollectionFactory(), true, false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDependencyFreeAccessManager);
        }

        [Test]
        public void AddUser()
        {
            String testUser = "user1";
            String postProcessingActionUserParameter = "";
            Action<String> postProcessingAction = (String user) =>
            {
                postProcessingActionUserParameter = user;
            };

            testDependencyFreeAccessManager.AddUser(testUser, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Users.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testUser, postProcessingActionUserParameter);

            // Test idempotency
            testDependencyFreeAccessManager.AddUser(testUser, postProcessingAction);
        }

        [Test]
        public void RemoveUser()
        {
            String testUser = "user1";
            String postProcessingActionUserParameter = "";
            Action<String> postProcessingAction = (String user) =>
            {
                postProcessingActionUserParameter = user;
            };
            testDependencyFreeAccessManager.AddUser(testUser);

            testDependencyFreeAccessManager.RemoveUser(testUser, postProcessingAction);

            Assert.AreEqual(0, testDependencyFreeAccessManager.Users.Count());
            Assert.IsFalse(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testUser, postProcessingActionUserParameter);

            // Test idempotency
            testDependencyFreeAccessManager.RemoveUser(testUser, postProcessingAction);
        }

        [Test]
        public void RemoveUser_DependenciesAreRemoved()
        {
            // Tests that any elements dependent on the primary element being removed are also removed (in this case user to group mappings referencing the user removed)
            // Ensures that DependencyFreeAccessManager methods are calling into proper base methods, and not doing an incomplete override

            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testDependencyFreeAccessManager.AddUser(testUser);
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            testDependencyFreeAccessManager.RemoveUser(testUser, (String user) => { });

            Assert.AreEqual(0, testDependencyFreeAccessManager.Users.Count());
            Assert.IsFalse(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsFalse(testDependencyFreeAccessManager.UserToEntityReverseMap[testEntityType][testEntity].Contains(testUser));
        }

        [Test]
        public void AddGroup()
        {
            String testGroup = "group1";
            String postProcessingActionGroupParameter = "";
            Action<String> postProcessingAction = (String group) =>
            {
                postProcessingActionGroupParameter = group;
            };

            testDependencyFreeAccessManager.AddGroup(testGroup, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testGroup, postProcessingActionGroupParameter);


            testDependencyFreeAccessManager.AddGroup(testGroup, postProcessingAction);
        }

        [Test]
        public void RemoveGroup()
        {
            String testGroup = "group1";
            String postProcessingActionGroupParameter = "";
            Action<String> postProcessingAction = (String group) =>
            {
                postProcessingActionGroupParameter = group;
            };
            testDependencyFreeAccessManager.AddGroup(testGroup);

            testDependencyFreeAccessManager.RemoveGroup(testGroup, postProcessingAction);

            Assert.AreEqual(0, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsFalse(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testGroup, postProcessingActionGroupParameter);


            testDependencyFreeAccessManager.RemoveGroup(testGroup, postProcessingAction);
        }

        [Test]
        public void RemoveGroup_DependenciesAreRemoved()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);

            testDependencyFreeAccessManager.RemoveGroup(testGroup, (String user) => { });

            Assert.AreEqual(0, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsFalse(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsFalse(testDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String postProcessingActionUserParameter = "";
            String postProcessingActionGroupParameter = "";
            Action<String, String> postProcessingAction = (String user, String group) =>
            {
                postProcessingActionUserParameter = user;
                postProcessingActionGroupParameter = group;
            };

            testDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Users.Count());
            Assert.AreEqual(1, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsTrue(testDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).AddUser(testUser);
            mockEventProcessor.Received(1).AddGroup(testGroup);
            Assert.AreEqual(testUser, postProcessingActionUserParameter);
            Assert.AreEqual(testGroup, postProcessingActionGroupParameter);


            // Test that prepended events are not created when depended-on elements already exist
            testDependencyFreeAccessManager.Clear();
            testDependencyFreeAccessManager.AddUser(testUser);
            testDependencyFreeAccessManager.AddGroup(testGroup);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup);

            Assert.IsTrue(testDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());


            // Test idempotency (and that prepended events are not created when depended-on elements already exist)
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            String postProcessingActionUserParameter = "";
            String postProcessingActionGroupParameter = "";
            Action<String, String> postProcessingAction = (String user, String group) =>
            {
                postProcessingActionUserParameter = user;
                postProcessingActionGroupParameter = group;
            };
            testDependencyFreeAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.RemoveUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Users.Count());
            Assert.AreEqual(1, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsFalse(testDependencyFreeAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testUser, postProcessingActionUserParameter);
            Assert.AreEqual(testGroup, postProcessingActionGroupParameter);


            // Test idempotency
            testDependencyFreeAccessManager.RemoveUserToGroupMapping(testUser, testGroup, postProcessingAction);
        }

        [Test]
        public void AddGroupToGroupMapping_ToAndFromGroupsAreSame()
        {
            testDependencyFreeAccessManager.AddGroup("group1");
            testDependencyFreeAccessManager.AddGroup("group2");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDependencyFreeAccessManager.AddGroupToGroupMapping("group2", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Parameters 'fromGroup' and 'toGroup' cannot contain the same group."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void AddGroupToGroupMapping_AddingCreatesCircularReference()
        {
            testDependencyFreeAccessManager.AddGroup("group1");
            testDependencyFreeAccessManager.AddGroup("group2");
            testDependencyFreeAccessManager.AddGroup("group3");
            testDependencyFreeAccessManager.AddGroup("group4");
            testDependencyFreeAccessManager.AddGroupToGroupMapping("group1", "group2");
            testDependencyFreeAccessManager.AddGroupToGroupMapping("group2", "group3");
            testDependencyFreeAccessManager.AddGroupToGroupMapping("group3", "group4");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDependencyFreeAccessManager.AddGroupToGroupMapping("group3", "group1");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between groups 'group3' and 'group1' cannot be created as it would cause a circular reference."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String postProcessingActionFromGroupParameter = "";
            String postProcessingActionToGroupParameter = "";
            Action<String, String> postProcessingAction = (String fromGroup, String toGroup) =>
            {
                postProcessingActionFromGroupParameter = fromGroup;
                postProcessingActionToGroupParameter = toGroup;
            };

            testDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.AreEqual(2, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testFromGroup));
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testToGroup));
            Assert.IsTrue(testDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).AddGroup(testFromGroup);
            mockEventProcessor.Received(1).AddGroup(testToGroup);
            Assert.AreEqual(testFromGroup, postProcessingActionFromGroupParameter);
            Assert.AreEqual(testToGroup, postProcessingActionToGroupParameter);


            // Test that prepended events are not created when depended-on elements already exist
            testDependencyFreeAccessManager.Clear();
            testDependencyFreeAccessManager.AddGroup(testFromGroup);
            testDependencyFreeAccessManager.AddGroup(testToGroup);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            Assert.IsTrue(testDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());


            // Test idempotency (and that prepended events are not created when depended-on elements already exist)
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            String postProcessingActionFromGroupParameter = "";
            String postProcessingActionToGroupParameter = "";
            Action<String, String> postProcessingAction = (String fromGroup, String toGroup) =>
            {
                postProcessingActionFromGroupParameter = testFromGroup;
                postProcessingActionToGroupParameter = testToGroup;
            };
            testDependencyFreeAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.AreEqual(2, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testFromGroup));
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testToGroup));
            Assert.IsFalse(testDependencyFreeAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testFromGroup, postProcessingActionFromGroupParameter);
            Assert.AreEqual(testToGroup, postProcessingActionToGroupParameter);


            // Test idempotency
            testDependencyFreeAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            var testApplicationComponent = ApplicationScreen.Order;
            var testAccessLevel = AccessLevel.View;
            String postProcessingActionUserParameter = "";
            ApplicationScreen postProcessingActionApplicationComponentParameter = ApplicationScreen.Settings;
            AccessLevel postProcessingActionAccessLevelParameter = AccessLevel.Delete;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (String user, ApplicationScreen applicationComponent, AccessLevel accessLevel) =>
            {
                postProcessingActionUserParameter = user;
                postProcessingActionApplicationComponentParameter = applicationComponent;
                postProcessingActionAccessLevelParameter = accessLevel;
            };

            testDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Users.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsTrue(testDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).AddUser(testUser);
            Assert.AreEqual(testUser, postProcessingActionUserParameter);
            Assert.AreEqual(ApplicationScreen.Order, postProcessingActionApplicationComponentParameter);
            Assert.AreEqual(AccessLevel.View, postProcessingActionAccessLevelParameter);


            // Test that prepended events are not created when depended-on elements already exist
            testDependencyFreeAccessManager.Clear();
            testDependencyFreeAccessManager.AddUser(testUser);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(testDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());


            // Test idempotency (and that prepended events are not created when depended-on elements already exist)
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel, postProcessingAction);

            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            var testApplicationComponent = ApplicationScreen.Order;
            var testAccessLevel = AccessLevel.View;
            String postProcessingActionUserParameter = "";
            ApplicationScreen postProcessingActionApplicationComponentParameter = ApplicationScreen.Settings;
            AccessLevel postProcessingActionAccessLevelParameter = AccessLevel.Delete;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (String user, ApplicationScreen applicationComponent, AccessLevel accessLevel) =>
            {
                postProcessingActionUserParameter = user;
                postProcessingActionApplicationComponentParameter = applicationComponent;
                postProcessingActionAccessLevelParameter = accessLevel;
            };
            testDependencyFreeAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Users.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.IsFalse(testDependencyFreeAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testUser, postProcessingActionUserParameter);
            Assert.AreEqual(ApplicationScreen.Order, postProcessingActionApplicationComponentParameter);
            Assert.AreEqual(AccessLevel.View, postProcessingActionAccessLevelParameter);


            // Test idempotency
            testDependencyFreeAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel, postProcessingAction);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            var testApplicationComponent = ApplicationScreen.Order;
            var testAccessLevel = AccessLevel.Modify;
            String postProcessingActionGroupParameter = "";
            ApplicationScreen postProcessingActionApplicationComponentParameter = ApplicationScreen.Settings;
            AccessLevel postProcessingActionAccessLevelParameter = AccessLevel.Delete;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (String group, ApplicationScreen applicationComponent, AccessLevel accessLevel) =>
            {
                postProcessingActionGroupParameter = group;
                postProcessingActionApplicationComponentParameter = applicationComponent;
                postProcessingActionAccessLevelParameter = accessLevel;
            };

            testDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsTrue(testDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).AddGroup(testGroup);
            Assert.AreEqual(testGroup, postProcessingActionGroupParameter);
            Assert.AreEqual(ApplicationScreen.Order, postProcessingActionApplicationComponentParameter);
            Assert.AreEqual(AccessLevel.Modify, postProcessingActionAccessLevelParameter);


            // Test that prepended events are not created when depended-on elements already exist
            testDependencyFreeAccessManager.Clear();
            testDependencyFreeAccessManager.AddGroup(testGroup);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(testDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());


            // Test idempotency (and that prepended events are not created when depended-on elements already exist)
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel, postProcessingAction);

            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            var testApplicationComponent = ApplicationScreen.Order;
            var testAccessLevel = AccessLevel.View;
            String postProcessingActionUserParameter = "";
            ApplicationScreen postProcessingActionApplicationComponentParameter = ApplicationScreen.Settings;
            AccessLevel postProcessingActionAccessLevelParameter = AccessLevel.Delete;
            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (String group, ApplicationScreen applicationComponent, AccessLevel accessLevel) =>
            {
                postProcessingActionUserParameter = group;
                postProcessingActionApplicationComponentParameter = applicationComponent;
                postProcessingActionAccessLevelParameter = accessLevel;
            };
            testDependencyFreeAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.IsFalse(testDependencyFreeAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testGroup, postProcessingActionUserParameter);
            Assert.AreEqual(ApplicationScreen.Order, postProcessingActionApplicationComponentParameter);
            Assert.AreEqual(AccessLevel.View, postProcessingActionAccessLevelParameter);


            // Test idempotency
            testDependencyFreeAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel, postProcessingAction);
        }

        [Test]
        public void AddEntityType()
        {
            String testEntityType = "ClientAccount";
            String postProcessingActionEntityTypeParameter = "";
            Action<String> postProcessingAction = (String entityType) =>
            {
                postProcessingActionEntityTypeParameter = testEntityType;
            };

            testDependencyFreeAccessManager.AddEntityType(testEntityType, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);


            testDependencyFreeAccessManager.AddEntityType(testEntityType, postProcessingAction);
        }

        [Test]
        public void RemoveEntityType()
        {
            String testEntityType = "ClientAccount";
            String postProcessingActionEntityTypeParameter = "";
            Action<String> postProcessingAction = (String entityType) =>
            {
                postProcessingActionEntityTypeParameter = entityType;
            };
            testDependencyFreeAccessManager.AddEntityType(testEntityType);

            testDependencyFreeAccessManager.RemoveEntityType(testEntityType, postProcessingAction);

            Assert.AreEqual(0, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsFalse(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);

            // Test idempotency
            testDependencyFreeAccessManager.RemoveEntityType(testEntityType, postProcessingAction);
        }

        [Test]
        public void RemoveEntityTypePreRemovalActionsOverload()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            String testGroup = "group1";
            testDependencyFreeAccessManager.AddUser(testUser);
            testDependencyFreeAccessManager.AddGroup(testGroup);
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            testDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Int32 userToEntityTypeMappingPreRemovalActionCallCount = 0;
            Int32 groupToEntityTypeMappingPreRemovalActionCallCount = 0;
            Action<String, String, IEnumerable<String>, Int32> userToEntityTypeMappingPreRemovalAction = (String user, String entityType, IEnumerable<String> entities, Int32 entityCount) =>
            {
                userToEntityTypeMappingPreRemovalActionCallCount++;
            };
            Action<String, String, IEnumerable<String>, Int32> groupToEntityTypeMappingPreRemovalAction = (String group, String entityType, IEnumerable<String> entities, Int32 entityCount) =>
            {
                groupToEntityTypeMappingPreRemovalActionCallCount++;
            };

            // Test idempotency
            testDependencyFreeAccessManager.RemoveEntityType(testEntityType, userToEntityTypeMappingPreRemovalAction, groupToEntityTypeMappingPreRemovalAction);
            testDependencyFreeAccessManager.RemoveEntityType(testEntityType, userToEntityTypeMappingPreRemovalAction, groupToEntityTypeMappingPreRemovalAction);

            Assert.IsFalse(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(1, userToEntityTypeMappingPreRemovalActionCallCount);
            Assert.AreEqual(1, groupToEntityTypeMappingPreRemovalActionCallCount);
        }

        [Test]
        public void RemoveEntityType_DependenciesAreRemoved()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testDependencyFreeAccessManager.AddUser(testUser);
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            testDependencyFreeAccessManager.RemoveEntityType(testEntityType, (String entityType) => { });

            Assert.AreEqual(0, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsFalse(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            var typedUserToEntityMap = (IDictionary<String, IDictionary<String, ISet<String>>>)testDependencyFreeAccessManager.UserToEntityMap;
            Assert.IsFalse(typedUserToEntityMap[testUser].ContainsKey(testEntityType));
        }

        [Test]
        public void AddEntity()
        {
            // Need to treat as both primary and secondary... make sure all cases for both types are covered

            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String postProcessingActionEntityTypeParameter = "";
            String postProcessingActionEntityParameter = "";
            Action<String, String> postProcessingAction = (String entityType, String entity) =>
            {
                postProcessingActionEntityTypeParameter = testEntityType;
                postProcessingActionEntityParameter = testEntity;
            };

            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsTrue(testDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).AddEntityType(testEntityType);
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);
            Assert.AreEqual(testEntity, postProcessingActionEntityParameter);


            // Test that prepended events are not created when depended-on elements already exist
            testDependencyFreeAccessManager.Clear();
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsTrue(testDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());


            // Test idempotency (and that prepended events are not created when depended-on elements already exist)
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String postProcessingActionEntityTypeParameter = "";
            String postProcessingActionEntityParameter = "";
            Action<String, String> postProcessingAction = (String entityType, String entity) =>
            {
                postProcessingActionEntityTypeParameter = entityType;
                postProcessingActionEntityParameter = entity;
            };
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.RemoveEntity(testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.AreEqual(0, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsFalse(testDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);
            Assert.AreEqual(testEntity, postProcessingActionEntityParameter);

            // Test idempotency
            testDependencyFreeAccessManager.RemoveEntity(testEntityType, testEntity, postProcessingAction);
        }


        [Test]
        public void RemoveEntityPostRemovalActionsOverload()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String testUser = "user1";
            String testGroup = "group1";
            testDependencyFreeAccessManager.AddUser(testUser);
            testDependencyFreeAccessManager.AddGroup(testGroup);
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            testDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Int32 userToEntityMappingPostRemovalActionCallCount = 0;
            Int32 groupToEntityMappingPostRemovalActionCallCount = 0;
            Action<String, String, String> userToEntityMappingPostRemovalAction = (String user, String entityType, String entity) =>
            {
                userToEntityMappingPostRemovalActionCallCount++;
            };
            Action<String, String, String> groupToEntityMappingPostRemovalAction = (String group, String entityType, String entity) =>
            {
                groupToEntityMappingPostRemovalActionCallCount++;
            };

            // Test idempotency
            testDependencyFreeAccessManager.RemoveEntity(testEntityType, testEntity, userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);
            testDependencyFreeAccessManager.RemoveEntity(testEntityType, testEntity, userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);
            testDependencyFreeAccessManager.RemoveEntity("InvalidEntityType", "InvalidEntity", userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);

            Assert.IsFalse(testDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            Assert.AreEqual(1, userToEntityMappingPostRemovalActionCallCount);
            Assert.AreEqual(1, groupToEntityMappingPostRemovalActionCallCount);
        }

        [Test]
        public void RemoveEntity_DependenciesAreRemoved()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testDependencyFreeAccessManager.AddUser(testUser);
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            testDependencyFreeAccessManager.RemoveEntity(testEntityType, testEntity , (String entityType, String entity) => { });

            Assert.AreEqual(1, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(0, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsFalse(testDependencyFreeAccessManager.GetEntities(testEntityType).Contains(testEntity));
            var typedUserToEntityMap = (IDictionary<String, IDictionary<String, ISet<String>>>)testDependencyFreeAccessManager.UserToEntityMap;
            Assert.IsFalse(typedUserToEntityMap[testUser][testEntityType].Contains(testEntity));
        }
        [Test]
        public void AddUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String postProcessingActionUserParameter = "";
            String postProcessingActionEntityTypeParameter = "";
            String postProcessingActionEntityParameter = "";
            Action<String, String, String> postProcessingAction = (String user, String entityType, String entity) =>
            {
                postProcessingActionUserParameter = user;
                postProcessingActionEntityTypeParameter = entityType;
                postProcessingActionEntityParameter = entity;
            };

            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Users.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.AreEqual(1, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(1, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsTrue(testDependencyFreeAccessManager.GetUserToEntityMappings(testUser, testEntityType).Contains(testEntity));
            Assert.AreEqual(3, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).AddUser(testUser);
            mockEventProcessor.Received(1).AddEntityType(testEntityType);
            mockEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
            Assert.AreEqual(testUser, postProcessingActionUserParameter);
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);
            Assert.AreEqual(testEntity, postProcessingActionEntityParameter);


            // Test that prepended events are not created when depended-on elements already exist
            testDependencyFreeAccessManager.Clear();
            testDependencyFreeAccessManager.AddUser(testUser);
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            Assert.IsTrue(testDependencyFreeAccessManager.GetUserToEntityMappings(testUser, testEntityType).Contains(testEntity));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());


            // Test idempotency (and that prepended events are not created when depended-on elements already exist)
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String postProcessingActionUserParameter = "";
            String postProcessingActionEntityTypeParameter = "";
            String postProcessingActionEntityParameter = "";
            Action<String, String, String> postProcessingAction = (String user, String entityType, String entity) =>
            {
                postProcessingActionUserParameter = user;
                postProcessingActionEntityTypeParameter = entityType;
                postProcessingActionEntityParameter = entity;
            };
            testDependencyFreeAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Users.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Users.Contains(testUser));
            Assert.AreEqual(1, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(1, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsFalse(testDependencyFreeAccessManager.GetUserToEntityMappings(testUser, testEntityType).Contains(testEntity));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testUser, postProcessingActionUserParameter);
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);
            Assert.AreEqual(testEntity, postProcessingActionEntityParameter);

            // Test idempotency
            testDependencyFreeAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String postProcessingActionGroupParameter = "";
            String postProcessingActionEntityTypeParameter = "";
            String postProcessingActionEntityParameter = "";
            Action<String, String, String> postProcessingAction = (String group, String entityType, String entity) =>
            {
                postProcessingActionGroupParameter = group;
                postProcessingActionEntityTypeParameter = entityType;
                postProcessingActionEntityParameter = entity;
            };

            testDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.AreEqual(1, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(1, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsTrue(testDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup, testEntityType).Contains(testEntity));
            Assert.AreEqual(3, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).AddGroup(testGroup);
            mockEventProcessor.Received(1).AddEntityType(testEntityType);
            mockEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
            Assert.AreEqual(testGroup, postProcessingActionGroupParameter);
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);
            Assert.AreEqual(testEntity, postProcessingActionEntityParameter);


            // Test that prepended events are not created when depended-on elements already exist
            testDependencyFreeAccessManager.Clear();
            testDependencyFreeAccessManager.AddGroup(testGroup);
            testDependencyFreeAccessManager.AddEntityType(testEntityType);
            testDependencyFreeAccessManager.AddEntity(testEntityType, testEntity);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            Assert.IsTrue(testDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup, testEntityType).Contains(testEntity));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());


            // Test idempotency (and that prepended events are not created when depended-on elements already exist)
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            String postProcessingActionGroupParameter = "";
            String postProcessingActionEntityTypeParameter = "";
            String postProcessingActionEntityParameter = "";
            Action<String, String, String> postProcessingAction = (String group, String entityType, String entity) =>
            {
                postProcessingActionGroupParameter = group;
                postProcessingActionEntityTypeParameter = entityType;
                postProcessingActionEntityParameter = entity;
            };
            testDependencyFreeAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            mockEventProcessor.ClearReceivedCalls();

            testDependencyFreeAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.AreEqual(1, testDependencyFreeAccessManager.Groups.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.Groups.Contains(testGroup));
            Assert.AreEqual(1, testDependencyFreeAccessManager.EntityTypes.Count());
            Assert.IsTrue(testDependencyFreeAccessManager.EntityTypes.Contains(testEntityType));
            Assert.AreEqual(1, testDependencyFreeAccessManager.GetEntities(testEntityType).Count());
            Assert.IsFalse(testDependencyFreeAccessManager.GetGroupToEntityMappings(testGroup, testEntityType).Contains(testEntity));
            Assert.AreEqual(0, mockEventProcessor.ReceivedCalls().Count());
            Assert.AreEqual(testGroup, postProcessingActionGroupParameter);
            Assert.AreEqual(testEntityType, postProcessingActionEntityTypeParameter);
            Assert.AreEqual(testEntity, postProcessingActionEntityParameter);

            // Test idempotency
            testDependencyFreeAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);
        }

        #region Nested Classes

        /// <summary>
        /// Version of the DependencyFreeAccessManager class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        protected class DependencyFreeAccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : DependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>The reverse of the mappings in member 'userToEntityMap'.</summary>
            public IDictionary<String, IDictionary<String, ISet<TUser>>> UserToEntityReverseMap
            {
                get { return userToEntityReverseMap; }
            }

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

            public DependencyFreeAccessManagerWithProtectedMembers(Boolean storeBidirectionalMappings)
                : base(storeBidirectionalMappings)
            {
            }

            /// <summary>
            /// Removes an entity type.
            /// </summary>
            /// <param name="entityType">The entity type to remove.</param>
            /// <param name="userToEntityTypeMappingPreRemovalAction">An action which is invoked before removing the entity type mappings for a user.  Accepts 4 parameters: the user in the mappings, the type of the entity of the mappings being removed, the entities in the mappings, and the number of entities in the mappings.</param>
            /// <param name="groupToEntityTypeMappingPreRemovalAction">An action which is invoked before removing the entity type mappings for a group.  Accepts 4 parameters: the group in the mappings, the type of the entity of the mappings being removed, the entities in the mappings, and the number of entities in the mappings.</param>
            public new void RemoveEntityType(String entityType, Action<TUser, String, IEnumerable<String>, Int32> userToEntityTypeMappingPreRemovalAction, Action<TGroup, String, IEnumerable<String>, Int32> groupToEntityTypeMappingPreRemovalAction)
            {
                base.RemoveEntityType(entityType, userToEntityTypeMappingPreRemovalAction, groupToEntityTypeMappingPreRemovalAction);
            }

            /// <summary>
            /// Removes an entity.
            /// </summary>
            /// <param name="entityType">The type of the entity.</param>
            /// <param name="entity">The entity to remove.</param>
            /// <param name="userToEntityMappingPostRemovalAction">An action which is invoked after removing a user to entity mapping.  Accepts 3 parameters: the user in the mapping, the type of the entity in the mapping, and the entity in the mapping.</param>
            /// <param name="groupToEntityMappingPostRemovalAction">An action which is invoked after removing a group to entity mapping.  Accepts 3 parameters: the group in the mapping, the type of the entity in the mapping, and the entity in the mapping.</param>
            public new void RemoveEntity(String entityType, String entity, Action<TUser, String, String> userToEntityMappingPostRemovalAction, Action<TGroup, String, String> groupToEntityMappingPostRemovalAction)
            {
                base.RemoveEntity(entityType, entity, userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);
            }
        }

        #endregion
    }
}
