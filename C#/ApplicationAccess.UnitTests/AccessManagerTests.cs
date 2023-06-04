/*
 * Copyright 2020 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.AccessManager class.
    /// </summary>
    public class AccessManagerTests
    {
        private AccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testAccessManager;
        private AccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testBidirectionalAccessManager;

        [SetUp]
        protected void SetUp()
        {
            testAccessManager = new AccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(false);
            testBidirectionalAccessManager = new AccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(true);
        }

        [Test]
        public void Constructor()
        {
            Assert.IsNull(testAccessManager.UserToEntityReverseMap);
            Assert.IsNull(testAccessManager.GroupToEntityReverseMap);
        }

        [Test]
        public void Clear()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddGroupToGroupMapping("group1", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group3", "group4");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            Assert.AreNotEqual(0, testAccessManager.Users.Count());
            Assert.AreNotEqual(0, testAccessManager.Groups.Count());

            testAccessManager.Clear();

            Assert.AreEqual(0, testAccessManager.Users.Count());
            Assert.AreEqual(0, testAccessManager.Groups.Count());
            Assert.IsFalse(testAccessManager.UserToComponentMapContainsKey("user2"));
            Assert.IsFalse(testAccessManager.GroupToComponentMapContainsKey("group1"));
            Assert.AreEqual(0, testAccessManager.Entities.Count());
            Assert.AreEqual(0, testAccessManager.UserToEntityMap.Count());
            Assert.AreEqual(0, testAccessManager.GroupToEntityMap.Count());
        }

        [Test]
        public void Clear_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddUser("user1");
            testBidirectionalAccessManager.AddUser("user2");
            testBidirectionalAccessManager.AddGroup("group1");
            testBidirectionalAccessManager.AddGroup("group2");
            testBidirectionalAccessManager.AddGroup("group3");
            testBidirectionalAccessManager.AddGroup("group4");
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntityType("BusinessUnit");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddUserToGroupMapping("user1", "group1");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group1", "group2");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group2", "group3");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group3", "group4");
            testBidirectionalAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testBidirectionalAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testBidirectionalAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            Assert.AreNotEqual(0, testBidirectionalAccessManager.Users.Count());
            Assert.AreNotEqual(0, testBidirectionalAccessManager.Groups.Count());

            testBidirectionalAccessManager.Clear();

            Assert.AreEqual(0, testBidirectionalAccessManager.Entities.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.UserToEntityMap.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.GroupToEntityMap.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.UserToEntityReverseMap.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.GroupToEntityReverseMap.Count());
        }

        [Test]
        public void AddUser_UserAlreadyExists()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUser("user1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' already exists."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void RemoveUser_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUser("user1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void RemoveUser()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group1");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Create);

            testAccessManager.RemoveUser("user1");

            Assert.IsFalse(testAccessManager.Users.Contains("user1"));
            Assert.IsTrue(testAccessManager.Users.Contains("user2"));
            Assert.IsFalse(testAccessManager.UserToComponentMapContainsKey("user1"));
            Assert.IsTrue(testAccessManager.UserToComponentMapContainsKey("user2"));
            Assert.IsFalse(testAccessManager.UserToEntityMap.ContainsKey("user1"));
            Assert.IsTrue(testAccessManager.UserToEntityMap.ContainsKey("user2"));
        }

        [Test]
        public void RemoveUser_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddUser("user1");
            testBidirectionalAccessManager.AddUser("user2");
            testBidirectionalAccessManager.AddGroup("group1");
            testBidirectionalAccessManager.AddUserToGroupMapping("user1", "group1");
            testBidirectionalAccessManager.AddUserToGroupMapping("user2", "group1");
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");

            testBidirectionalAccessManager.RemoveUser("user1");

            Assert.IsFalse(testBidirectionalAccessManager.Users.Contains("user1"));
            Assert.IsTrue(testBidirectionalAccessManager.Users.Contains("user2"));
            Assert.IsFalse(testBidirectionalAccessManager.UserToEntityMap.ContainsKey("user1"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityMap.ContainsKey("user2"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("user2"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
        }

        [Test]
        public void AddGroup_GroupAlreadyExists()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroup("group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' already exists."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void RemoveGroup_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroup("group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void RemoveGroup()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Create);

            testAccessManager.RemoveGroup("group1");

            Assert.IsFalse(testAccessManager.Groups.Contains("group1"));
            Assert.IsTrue(testAccessManager.Groups.Contains("group2"));
            Assert.IsFalse(testAccessManager.GroupToComponentMapContainsKey("group1"));
            Assert.IsTrue(testAccessManager.GroupToComponentMapContainsKey("group2"));
            Assert.IsFalse(testAccessManager.GroupToEntityMap.ContainsKey("group1"));
            Assert.IsTrue(testAccessManager.GroupToEntityMap.ContainsKey("group2"));
        }

        [Test]
        public void RemoveGroup_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddGroup("group1");
            testBidirectionalAccessManager.AddGroup("group2");
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyA");

            testBidirectionalAccessManager.RemoveGroup("group1");

            Assert.IsFalse(testBidirectionalAccessManager.Groups.Contains("group1"));
            Assert.IsTrue(testBidirectionalAccessManager.Groups.Contains("group2"));
            Assert.IsFalse(testBidirectionalAccessManager.GroupToEntityMap.ContainsKey("group1"));
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityMap.ContainsKey("group2"));
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("group2"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
        }

        [Test]
        public void AddUserToGroupMapping_MappingAlreadyExists()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddUserToGroupMapping("user1", "group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user1' and group 'group1' already exists."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void AddUserToGroupMapping_userDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void AddUserToGroupMapping_GroupDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetUserToGroupMappings_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetUserToGroupMappings("user1", false).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void GetUserToGroupMappings()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroup("group5");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group3");
            testAccessManager.AddGroupToGroupMapping("group1", "group2");
            testAccessManager.AddGroupToGroupMapping("group1", "group3");
            testAccessManager.AddGroupToGroupMapping("group1", "group5");
            testAccessManager.AddGroupToGroupMapping("group3", "group4");

            HashSet<String> mappings = testAccessManager.GetUserToGroupMappings("user1", false);
            Assert.AreEqual(0, mappings.Count);


            mappings = testAccessManager.GetUserToGroupMappings("user2", false);
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains("group2"));


            mappings = testAccessManager.GetUserToGroupMappings("user3", false);
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains("group3"));


            mappings = testAccessManager.GetUserToGroupMappings("user1", true);
            Assert.AreEqual(0, mappings.Count);


            mappings = testAccessManager.GetUserToGroupMappings("user2", true);
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains("group2"));


            mappings = testAccessManager.GetUserToGroupMappings("user3", true);
            Assert.AreEqual(2, mappings.Count);
            Assert.IsTrue(mappings.Contains("group3"));
            Assert.IsTrue(mappings.Contains("group4"));
        }

        [Test]
        public void RemoveUserToGroupMapping_UserDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void RemoveUserToGroupMapping_GroupDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void RemoveUserToGroupMapping_MappingDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user1' and group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void AddGroupToGroupMapping_ToGroupDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group2' does not exist."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void AddGroupToGroupMapping_FromGroupDoesntExist()
        {
            testAccessManager.AddGroup("group2");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("fromGroup", e.ParamName);
        }

        [Test]
        public void AddGroupToGroupMapping_MappingAlreadyExists()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroupToGroupMapping("group1", "group2");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between groups 'group1' and 'group2' already exists."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void AddGroupToGroupMapping_ToAndFromGroupsAreSame()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToGroupMapping("group2", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Parameters 'fromGroup' and 'toGroup' cannot contain the same group."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void AddGroupToGroupMapping_AddingCreatesCircularReference()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroupToGroupMapping("group1", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group3", "group4");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToGroupMapping("group3", "group1");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between groups 'group3' and 'group1' cannot be created as it would cause a circular reference."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void GetGroupToGroupMappings_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetGroupToGroupMappings("group11", false).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group11' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroup("group5");
            testAccessManager.AddGroup("group6");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group3", "group4");
            testAccessManager.AddGroupToGroupMapping("group3", "group5");
            testAccessManager.AddGroupToGroupMapping("group4", "group5");
            testAccessManager.AddGroupToGroupMapping("group6", "group1");
            testAccessManager.AddGroupToGroupMapping("group6", "group3");

            HashSet<String> mappings = testAccessManager.GetGroupToGroupMappings("group1", false);
            Assert.AreEqual(0, mappings.Count);


            mappings = testAccessManager.GetGroupToGroupMappings("group2", false);
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains("group3"));


            mappings = testAccessManager.GetGroupToGroupMappings("group1", true);
            Assert.AreEqual(0, mappings.Count);


            mappings = testAccessManager.GetGroupToGroupMappings("group2", true);
            Assert.AreEqual(3, mappings.Count);
            Assert.IsTrue(mappings.Contains("group3"));
            Assert.IsTrue(mappings.Contains("group4"));
            Assert.IsTrue(mappings.Contains("group5"));
        }

        [Test]
        public void RemoveGroupToGroupMapping_ToGroupDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group2' does not exist."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void RemoveGroupToGroupMapping_FromGroupDoesntExist()
        {
            testAccessManager.AddGroup("group2");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("fromGroup", e.ParamName);
        }

        [Test]
        public void RemoveGroupToGroupMapping_MappingDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between groups 'group1' and 'group2' does not exist."));
            Assert.AreEqual("toGroup", e.ParamName);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_MappingAlreadyExists()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user1' application component 'Order' and access level 'Create' already exists."));
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("user1").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddGroup("group1");

            var result = new HashSet<Tuple<ApplicationScreen, AccessLevel>>(testAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("user1"));

            Assert.AreEqual(0, result.Count);


            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.View);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Create);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);

            result = new HashSet<Tuple<ApplicationScreen, AccessLevel>>(testAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("user1"));

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_MappingDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user1' application component 'Order' and access level 'Create' doesn't exist."));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_MappingAlreadyExists()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("A mapping between group 'group1' application component 'Order' and access level 'Create' already exists."));
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("group1").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");

            var result = new HashSet<Tuple<ApplicationScreen, AccessLevel>>(testAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("group1"));

            Assert.AreEqual(0, result.Count);


            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Settings, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Settings, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Settings, AccessLevel.Modify);

            result = new HashSet<Tuple<ApplicationScreen, AccessLevel>>(testAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("group1"));

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.View)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_MappingDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("A mapping between group 'group1' application component 'Order' and access level 'Create' doesn't exist."));
        }

        [Test]
        public void AddEntityType_EntityTypeAlreadyExists()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddEntityType("ClientAccount");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' already exists."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void AddEntityType_BlankName()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddEntityType(" ");
            });

            Assert.That(e.Message, Does.StartWith("Entity type ' ' must contain a valid character."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void ContainsEntityType()
        {
            testAccessManager.AddEntityType("ClientAccount");

            Assert.IsTrue(testAccessManager.ContainsEntityType("ClientAccount"));
            Assert.IsFalse(testAccessManager.ContainsEntityType("BusinessUnit"));
        }

        [Test]
        public void RemoveEntityType_EntityTypeDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveEntityType("ClientAccount");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void RemoveEntityType()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddUser("User1");
            testAccessManager.AddUser("User2");
            testAccessManager.AddGroup("Group1");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User2", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");

            testAccessManager.RemoveEntityType("ClientAccount");

            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testAccessManager.UserToEntityMap)
            {
                Assert.False(currentKvp.Value.ContainsKey("ClientAccount"));
            }
            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testAccessManager.GroupToEntityMap)
            {
                Assert.False(currentKvp.Value.ContainsKey("ClientAccount"));
            }
            var mappings = new HashSet<Tuple<String, String>>(testAccessManager.GetUserToEntityMappings("User1"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            mappings = new HashSet<Tuple<String, String>>(testAccessManager.GetUserToEntityMappings("User2"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            mappings = new HashSet<Tuple<String, String>>(testAccessManager.GetGroupToEntityMappings("Group1"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
        }

        [Test]
        public void RemoveEntityType_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntityType("BusinessUnit");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddUser("User1");
            testBidirectionalAccessManager.AddUser("User2");
            testBidirectionalAccessManager.AddGroup("Group1");
            testBidirectionalAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddUserToEntityMapping("User2", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");

            testBidirectionalAccessManager.RemoveEntityType("ClientAccount");

            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testBidirectionalAccessManager.UserToEntityMap)
            {
                Assert.False(currentKvp.Value.ContainsKey("ClientAccount"));
            }
            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testBidirectionalAccessManager.GroupToEntityMap)
            {
                Assert.False(currentKvp.Value.ContainsKey("ClientAccount"));
            }
            var mappings = new HashSet<Tuple<String, String>>(testBidirectionalAccessManager.GetUserToEntityMappings("User1"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            mappings = new HashSet<Tuple<String, String>>(testBidirectionalAccessManager.GetUserToEntityMappings("User2"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            mappings = new HashSet<Tuple<String, String>>(testBidirectionalAccessManager.GetGroupToEntityMappings("Group1"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(2, testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Sales"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("User1"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Contains("User2"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("Group1"));
        }

        [Test]
        public void AddEntity_EntityTypeDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddEntity("ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void AddEntity_EntityAlreadytExists()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddEntity("ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' already exists."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void AddEntity_BlankName()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddEntity("ClientAccount", "  ");
            });

            Assert.That(e.Message, Does.StartWith("Entity '  ' must contain a valid character."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void GetEntities_EntityTypeDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetEntities("ClientAccount").FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void ContainsEntity()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");

            Assert.IsTrue(testAccessManager.ContainsEntity("ClientAccount", "CompanyA"));
            Assert.IsFalse(testAccessManager.ContainsEntity("ClientAccount", "CompanyB"));
            Assert.IsFalse(testAccessManager.ContainsEntity("BusinessUnit", "Marketing"));
        }

        [Test]
        public void RemoveEntity_EntityTypeDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void RemoveEntity_EntityDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void RemoveEntity()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddUser("User1");
            testAccessManager.AddUser("User2");
            testAccessManager.AddGroup("Group1");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User2", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");

            testAccessManager.RemoveEntity("ClientAccount", "CompanyB");

            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testAccessManager.UserToEntityMap)
            {
                if (currentKvp.Value.ContainsKey("ClientAccount"))
                {
                    Assert.False(currentKvp.Value["ClientAccount"].Contains("CompanyB"));
                }
            }
            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testAccessManager.GroupToEntityMap)
            {
                if (currentKvp.Value.ContainsKey("ClientAccount"))
                {
                    Assert.False(currentKvp.Value["ClientAccount"].Contains("CompanyB"));
                }
            }
            var mappings = new HashSet<Tuple<String, String>>(testAccessManager.GetUserToEntityMappings("User1"));
            Assert.AreEqual(2, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            mappings = new HashSet<Tuple<String, String>>(testAccessManager.GetUserToEntityMappings("User2"));
            Assert.AreEqual(2, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            mappings = new HashSet<Tuple<String, String>>(testAccessManager.GetGroupToEntityMappings("Group1"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
        }

        [Test]
        public void RemoveEntity_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntityType("BusinessUnit");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddUser("User1");
            testBidirectionalAccessManager.AddUser("User2");
            testBidirectionalAccessManager.AddGroup("Group1");
            testBidirectionalAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddUserToEntityMapping("User2", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");

            testBidirectionalAccessManager.RemoveEntity("ClientAccount", "CompanyB");

            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testBidirectionalAccessManager.UserToEntityMap)
            {
                if (currentKvp.Value.ContainsKey("ClientAccount"))
                {
                    Assert.False(currentKvp.Value["ClientAccount"].Contains("CompanyB"));
                }
            }
            foreach (KeyValuePair<String, IDictionary<String, ISet<String>>> currentKvp in testBidirectionalAccessManager.GroupToEntityMap)
            {
                if (currentKvp.Value.ContainsKey("ClientAccount"))
                {
                    Assert.False(currentKvp.Value["ClientAccount"].Contains("CompanyB"));
                }
            }
            var mappings = new HashSet<Tuple<String, String>>(testBidirectionalAccessManager.GetUserToEntityMappings("User1"));
            Assert.AreEqual(2, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            mappings = new HashSet<Tuple<String, String>>(testBidirectionalAccessManager.GetUserToEntityMappings("User2"));
            Assert.AreEqual(2, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            mappings = new HashSet<Tuple<String, String>>(testBidirectionalAccessManager.GetGroupToEntityMappings("Group1"));
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            Assert.AreEqual(2, testBidirectionalAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.AreEqual(2, testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Sales"));
            Assert.AreEqual(2, testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("User1"));
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("User2"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("User1"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Contains("User2"));
            Assert.AreEqual(2, testBidirectionalAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(0, testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"].Count);
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("Group1"));
        }

        [Test]
        public void AddUserToEntityMapping_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void AddUserToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void AddUserToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void AddUserToEntityMapping_MappingAlreadyExists()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user1' and entity 'CompanyA' with type 'ClientAccount' already exists."));
        }
        [Test]
        public void AddUserToEntityMapping_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddUser("user1");
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");

            testBidirectionalAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");

            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("user1"));
        }

        [Test]
        public void GetUserToEntityMappingsUserOverload_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetUserToEntityMappings("user1").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void GetUserToEntityMappingsUserOverload()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddUser("User1");
            testAccessManager.AddUser("User2");
            testAccessManager.AddUser("User3");
            testAccessManager.AddGroup("Group1");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User2", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");

            var result = new HashSet<Tuple<String, String>>(testAccessManager.GetUserToEntityMappings("User3"));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<Tuple<String, String>>(testAccessManager.GetUserToEntityMappings("User1"));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
        }

        [Test]
        public void GetUserToEntityMappingsUserAndEntityTypeOverload_UserDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetUserToEntityMappings("user1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void GetUserToEntityMappingsUserAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetUserToEntityMappings("user1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void GetUserToEntityMappingsUserAndEntityTypeOverload()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntityType("ProductType");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddUser("User1");
            testAccessManager.AddUser("User2");
            testAccessManager.AddUser("User3");
            testAccessManager.AddGroup("Group1");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User2", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");

            var result = new HashSet<String>(testAccessManager.GetUserToEntityMappings("User3", "ClientAccount"));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetUserToEntityMappings("User1", "ProductType"));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetUserToEntityMappings("User1", "ClientAccount"));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
        }

        [Test]
        public void RemoveUserToEntityMapping_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void RemoveUserToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void RemoveUserToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void RemoveUserToEntityMapping_MappingDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyB");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user2' and entity 'CompanyA' with type 'ClientAccount' doesn't exist."));


            e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "BusinessUnit", "Marketing");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user1' and entity 'Marketing' with type 'BusinessUnit' doesn't exist."));


            e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between user 'user1' and entity 'CompanyA' with type 'ClientAccount' doesn't exist."));
        }

        [Test]
        public void RemoveUserToEntityMapping_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddUser("user1");
            testBidirectionalAccessManager.AddUser("user2");
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntityType("BusinessUnit");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyB");

            testBidirectionalAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyB");

            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyB"));
            Assert.AreEqual(0, testBidirectionalAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyB"].Count);
        }

        [Test]
        public void AddGroupToEntityMapping_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void AddGroupToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void AddGroupToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void AddGroupToEntityMapping_MappingAlreadyExists()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between group 'group1' and entity 'CompanyA' with type 'ClientAccount' already exists."));
        }

        [Test]
        public void AddGroupToEntityMapping_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddGroup("group1");
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");

            testBidirectionalAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");

            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("group1"));
        }

        [Test]
        public void GetGroupToEntityMappingsGroupOverload_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetGroupToEntityMappings("group1").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupOverload()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddUser("User1");
            testAccessManager.AddGroup("Group1");
            testAccessManager.AddGroup("Group2");
            testAccessManager.AddGroup("Group3");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("Group2", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("Group2", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Group2", "BusinessUnit", "Sales");

            var result = new HashSet<Tuple<String, String>>(testAccessManager.GetGroupToEntityMappings("Group3"));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<Tuple<String, String>>(testAccessManager.GetGroupToEntityMappings("Group2"));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
        }

        [Test]
        public void GetGroupToEntityMappingsGroupAndEntityTypeOverload_GroupDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetGroupToEntityMappings("group1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetGroupToEntityMappings("group1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupAndEntityTypeOverload()
        {
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntityType("ProductType");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddUser("User1");
            testAccessManager.AddGroup("Group1");
            testAccessManager.AddGroup("Group2");
            testAccessManager.AddGroup("Group3");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("User1", "ClientAccount", "CompanyB");
            testAccessManager.AddUserToEntityMapping("User1", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("Group1", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Group1", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("Group2", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("Group2", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Group2", "BusinessUnit", "Sales");

            var result = new HashSet<String>(testAccessManager.GetGroupToEntityMappings("Group3", "ClientAccount"));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetGroupToEntityMappings("Group1", "ProductType"));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetGroupToEntityMappings("Group2", "ClientAccount"));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
        }

        [Test]
        public void RemoveGroupToEntityMapping_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void RemoveGroupToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void RemoveGroupToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void RemoveGroupToEntityMapping_MappingDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group2", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between group 'group2' and entity 'CompanyA' with type 'ClientAccount' doesn't exist."));


            e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "BusinessUnit", "Marketing");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between group 'group1' and entity 'Marketing' with type 'BusinessUnit' doesn't exist."));


            e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("A mapping between group 'group1' and entity 'CompanyA' with type 'ClientAccount' doesn't exist."));
        }

        [Test]
        public void RemoveGroupToEntityMapping_BidirectionalMappingsTrue()
        {
            testBidirectionalAccessManager.AddGroup("group1");
            testBidirectionalAccessManager.AddGroup("group2");
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntityType("BusinessUnit");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");

            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyB");

            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"].ContainsKey("CompanyB"));
            Assert.AreEqual(0, testBidirectionalAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyB"].Count);
        }

        [Test]
        public void HasAccessToApplicationComponent_UserDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group1");
            testAccessManager.AddUserToGroupMapping("user3", "group2");

            Boolean result = testAccessManager.HasAccessToApplicationComponent("user4", ApplicationScreen.Order, AccessLevel.View);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToApplicationComponent_UserHasAccess()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.View);

            Boolean result = testAccessManager.HasAccessToApplicationComponent("user1", ApplicationScreen.Order, AccessLevel.View);

            Assert.IsTrue(result);


            result = testAccessManager.HasAccessToApplicationComponent("user2", ApplicationScreen.Order, AccessLevel.View);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToApplicationComponent_GroupHasAccess()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group1");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Settings, AccessLevel.Modify);

            Boolean result = testAccessManager.HasAccessToApplicationComponent("user3", ApplicationScreen.Order, AccessLevel.View);

            Assert.IsTrue(result);


            result = testAccessManager.HasAccessToApplicationComponent("user3", ApplicationScreen.Settings, AccessLevel.Modify);

            Assert.IsTrue(result);


            result = testAccessManager.HasAccessToApplicationComponent("user1", ApplicationScreen.Settings, AccessLevel.Modify);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToEntity_UserDoesntExist()
        {
            Boolean result = testAccessManager.HasAccessToEntity("user1", "BusinessUnit", "Marketing");

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToEntity_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.HasAccessToEntity("user1", "BusinessUnit", "Marketing");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void HasAccessToEntity_EntityDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("BusinessUnit");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.HasAccessToEntity("user1", "BusinessUnit", "Marketing");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'Marketing' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void HasAccessToEntity_UserHasAccess()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyB");

            Boolean result = testAccessManager.HasAccessToEntity("user1", "ClientAccount", "CompanyB");

            Assert.IsTrue(result);


            result = testAccessManager.HasAccessToEntity("user2", "ClientAccount", "CompanyB");

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToEntity_GroupHasAccess()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group1");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");

            Boolean result = testAccessManager.HasAccessToEntity("user3", "ClientAccount", "CompanyB");

            Assert.IsTrue(result);


            result = testAccessManager.HasAccessToEntity("user3", "BusinessUnit", "Marketing");

            Assert.IsTrue(result);


            result = testAccessManager.HasAccessToEntity("user1", "BusinessUnit", "Marketing");

            Assert.IsFalse(result);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetApplicationComponentsAccessibleByUser("user1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser_MappingsDontExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddUserToGroupMapping("user1", "group1");

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testAccessManager.GetApplicationComponentsAccessibleByUser("user1");

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Settings, AccessLevel.Modify);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testAccessManager.GetApplicationComponentsAccessibleByUser("user2");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Settings, AccessLevel.Modify)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
        }


        [Test]
        public void GetApplicationComponentsAccessibleByGroup_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetApplicationComponentsAccessibleByGroup("group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup_MappingsDontExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroupToGroupMapping("group1", "group2");

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testAccessManager.GetApplicationComponentsAccessibleByGroup("group1");

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroup("group5");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group2", "group4");
            testAccessManager.AddGroupToGroupMapping("group3", "group5");
            testAccessManager.AddGroupToGroupMapping("group4", "group5");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user3", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Settings, AccessLevel.Delete);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group5", ApplicationScreen.Settings, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group5", ApplicationScreen.ManageProducts, AccessLevel.Modify);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testAccessManager.GetApplicationComponentsAccessibleByGroup("group2");

            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Settings, AccessLevel.Modify)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Settings, AccessLevel.Delete)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Settings, AccessLevel.Create)));
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserOverload_UserDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByUser("user1").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserOverload()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddUser("user4");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddUserToGroupMapping("user4", "group4");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Sales");

            HashSet<Tuple<String, String>> result = testAccessManager.GetEntitiesAccessibleByUser("user4");

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetEntitiesAccessibleByUser("user1");

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyD")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));


            result = testAccessManager.GetEntitiesAccessibleByUser("user2");

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyC")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload_UserDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByUser("user1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByUser("user1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");

            HashSet<String> result = testAccessManager.GetEntitiesAccessibleByUser("user2", "ClientAccount");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupOverload_GroupDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByGroup("group1").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupOverload()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddUser("user4");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroup("group5");
            testAccessManager.AddGroup("group6");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddUserToGroupMapping("user4", "group4");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group3", "group5");
            testAccessManager.AddGroupToGroupMapping("group5", "group6");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("ClientAccount", "CompanyE");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddEntity("BusinessUnit", "Accounting");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group5", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group5", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Accounting");

            HashSet<Tuple<String, String>> result = testAccessManager.GetEntitiesAccessibleByGroup("group4");

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetEntitiesAccessibleByGroup("group1");

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));


            result = testAccessManager.GetEntitiesAccessibleByGroup("group2");

            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyC")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Accounting")));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload_GroupDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByGroup("group1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByGroup("group1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroup("group5");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group2", "group4");
            testAccessManager.AddGroupToGroupMapping("group3", "group5");
            testAccessManager.AddGroupToGroupMapping("group4", "group5");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("ClientAccount", "CompanyE");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddEntity("BusinessUnit", "Manufacturing");
            testAccessManager.AddEntity("BusinessUnit", "CustomerService");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user2", "BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("user3", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyD");
            testAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Manufacturing");
            testAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "CustomerService");
            testAccessManager.AddGroupToEntityMapping("group5", "ClientAccount", "CompanyE");
            testAccessManager.AddGroupToEntityMapping("group5", "ClientAccount", "CompanyD");

            HashSet<String> result = testAccessManager.GetEntitiesAccessibleByGroup("group2", "ClientAccount");

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("CompanyD"));
            Assert.IsTrue(result.Contains("CompanyE"));


            result = testAccessManager.GetEntitiesAccessibleByGroup("group2", "BusinessUnit");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Sales"));
            Assert.IsTrue(result.Contains("Manufacturing"));
            Assert.IsTrue(result.Contains("CustomerService"));
        }

        [Test]
        public void AddRemoveAdd()
        {
            // Tests Add*() > Remove*() > Add*() add operations in sequence, to ensure that no residual mappings are left in the underying structures after Remove*() operations
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddUser("user4");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroup("group5");
            testAccessManager.AddGroup("group6");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddUserToGroupMapping("user4", "group4");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group3", "group5");
            testAccessManager.AddGroupToGroupMapping("group5", "group6");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Create);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("ClientAccount", "CompanyE");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddEntity("BusinessUnit", "Accounting");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group5", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group5", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Accounting");

            testAccessManager.RemoveGroupToEntityMapping("group6", "BusinessUnit", "Accounting");
            testAccessManager.RemoveGroupToEntityMapping("group6", "BusinessUnit", "Sales");
            testAccessManager.RemoveGroupToEntityMapping("group5", "BusinessUnit", "Sales");
            testAccessManager.RemoveGroupToEntityMapping("group5", "ClientAccount", "CompanyB");
            testAccessManager.RemoveGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testAccessManager.RemoveGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testAccessManager.RemoveGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testAccessManager.RemoveGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testAccessManager.RemoveGroupToEntityMapping("group1", "BusinessUnit", "Sales");
            testAccessManager.RemoveUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testAccessManager.RemoveUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testAccessManager.RemoveEntity("BusinessUnit", "Accounting");
            testAccessManager.RemoveEntity("BusinessUnit", "Sales");
            testAccessManager.RemoveEntity("BusinessUnit", "Marketing");
            testAccessManager.RemoveEntity("ClientAccount", "CompanyE");
            testAccessManager.RemoveEntity("ClientAccount", "CompanyD");
            testAccessManager.RemoveEntity("ClientAccount", "CompanyC");
            testAccessManager.RemoveEntity("ClientAccount", "CompanyB");
            testAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            testAccessManager.RemoveEntityType("BusinessUnit");
            testAccessManager.RemoveEntityType("ClientAccount");
            testAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Create);
            testAccessManager.RemoveGroupToGroupMapping("group5", "group6");
            testAccessManager.RemoveGroupToGroupMapping("group3", "group5");
            testAccessManager.RemoveGroupToGroupMapping("group2", "group3");
            testAccessManager.RemoveUserToGroupMapping("user4", "group4");
            testAccessManager.RemoveUserToGroupMapping("user3", "group2");
            testAccessManager.RemoveUserToGroupMapping("user2", "group2");
            testAccessManager.RemoveUserToGroupMapping("user1", "group1");
            testAccessManager.RemoveGroup("group6");
            testAccessManager.RemoveGroup("group5");
            testAccessManager.RemoveGroup("group4");
            testAccessManager.RemoveGroup("group3");
            testAccessManager.RemoveGroup("group2");
            testAccessManager.RemoveGroup("group1");
            testAccessManager.RemoveUser("user4");
            testAccessManager.RemoveUser("user3");
            testAccessManager.RemoveUser("user2");
            testAccessManager.RemoveUser("user1");

            Assert.AreEqual(0, testAccessManager.UserToGroupMap.LeafVertices.Count());
            Assert.AreEqual(0, testAccessManager.UserToGroupMap.NonLeafVertices.Count());
            Assert.AreEqual(0, testAccessManager.Entities.Count());
            Assert.AreEqual(0, testAccessManager.UserToEntityMap.Count());
            Assert.AreEqual(0, testAccessManager.GroupToEntityMap.Count);


            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddUser("user3");
            testAccessManager.AddUser("user4");
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddGroup("group3");
            testAccessManager.AddGroup("group4");
            testAccessManager.AddGroup("group5");
            testAccessManager.AddGroup("group6");
            testAccessManager.AddUserToGroupMapping("user1", "group1");
            testAccessManager.AddUserToGroupMapping("user2", "group2");
            testAccessManager.AddUserToGroupMapping("user3", "group2");
            testAccessManager.AddUserToGroupMapping("user4", "group4");
            testAccessManager.AddGroupToGroupMapping("group2", "group3");
            testAccessManager.AddGroupToGroupMapping("group3", "group5");
            testAccessManager.AddGroupToGroupMapping("group5", "group6");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Create);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("ClientAccount", "CompanyE");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddEntity("BusinessUnit", "Accounting");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("group5", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("group5", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Accounting");
        }

        [Test]
        public void AddRemoveAdd_BidirectionalMappingsTrue()
        {
            // Tests Add*() > Remove*() > Add*() add operations in sequence, to ensure that no residual mappings are left in the underying structures after Remove*() operations, when storing bidirectional mappings
            testBidirectionalAccessManager.AddUser("user1");
            testBidirectionalAccessManager.AddUser("user2");
            testBidirectionalAccessManager.AddUser("user3");
            testBidirectionalAccessManager.AddUser("user4");
            testBidirectionalAccessManager.AddGroup("group1");
            testBidirectionalAccessManager.AddGroup("group2");
            testBidirectionalAccessManager.AddGroup("group3");
            testBidirectionalAccessManager.AddGroup("group4");
            testBidirectionalAccessManager.AddGroup("group5");
            testBidirectionalAccessManager.AddGroup("group6");
            testBidirectionalAccessManager.AddUserToGroupMapping("user1", "group1");
            testBidirectionalAccessManager.AddUserToGroupMapping("user2", "group2");
            testBidirectionalAccessManager.AddUserToGroupMapping("user3", "group2");
            testBidirectionalAccessManager.AddUserToGroupMapping("user4", "group4");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group2", "group3");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group3", "group5");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group5", "group6");
            testBidirectionalAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Create);
            testBidirectionalAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testBidirectionalAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testBidirectionalAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testBidirectionalAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntityType("BusinessUnit");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyC");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyD");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyE");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Accounting");
            testBidirectionalAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testBidirectionalAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group5", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group5", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Accounting");

            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group6", "BusinessUnit", "Accounting");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group6", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group5", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group5", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testBidirectionalAccessManager.RemoveGroupToEntityMapping("group1", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.RemoveUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.RemoveUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testBidirectionalAccessManager.RemoveEntity("BusinessUnit", "Accounting");
            testBidirectionalAccessManager.RemoveEntity("BusinessUnit", "Sales");
            testBidirectionalAccessManager.RemoveEntity("BusinessUnit", "Marketing");
            testBidirectionalAccessManager.RemoveEntity("ClientAccount", "CompanyE");
            testBidirectionalAccessManager.RemoveEntity("ClientAccount", "CompanyD");
            testBidirectionalAccessManager.RemoveEntity("ClientAccount", "CompanyC");
            testBidirectionalAccessManager.RemoveEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.RemoveEntityType("BusinessUnit");
            testBidirectionalAccessManager.RemoveEntityType("ClientAccount");
            testBidirectionalAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testBidirectionalAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testBidirectionalAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testBidirectionalAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testBidirectionalAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Create);
            testBidirectionalAccessManager.RemoveGroupToGroupMapping("group5", "group6");
            testBidirectionalAccessManager.RemoveGroupToGroupMapping("group3", "group5");
            testBidirectionalAccessManager.RemoveGroupToGroupMapping("group2", "group3");
            testBidirectionalAccessManager.RemoveUserToGroupMapping("user4", "group4");
            testBidirectionalAccessManager.RemoveUserToGroupMapping("user3", "group2");
            testBidirectionalAccessManager.RemoveUserToGroupMapping("user2", "group2");
            testBidirectionalAccessManager.RemoveUserToGroupMapping("user1", "group1");
            testBidirectionalAccessManager.RemoveGroup("group6");
            testBidirectionalAccessManager.RemoveGroup("group5");
            testBidirectionalAccessManager.RemoveGroup("group4");
            testBidirectionalAccessManager.RemoveGroup("group3");
            testBidirectionalAccessManager.RemoveGroup("group2");
            testBidirectionalAccessManager.RemoveGroup("group1");
            testBidirectionalAccessManager.RemoveUser("user4");
            testBidirectionalAccessManager.RemoveUser("user3");
            testBidirectionalAccessManager.RemoveUser("user2");
            testBidirectionalAccessManager.RemoveUser("user1");

            Assert.AreEqual(0, testBidirectionalAccessManager.UserToGroupMap.LeafVertices.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.UserToGroupMap.NonLeafVertices.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.Entities.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.UserToEntityMap.Count());
            Assert.AreEqual(0, testBidirectionalAccessManager.GroupToEntityMap.Count);
            Assert.AreEqual(0, testBidirectionalAccessManager.UserToEntityReverseMap.Count);
            Assert.AreEqual(0, testBidirectionalAccessManager.GroupToEntityReverseMap.Count);


            testBidirectionalAccessManager.AddUser("user1");
            testBidirectionalAccessManager.AddUser("user2");
            testBidirectionalAccessManager.AddUser("user3");
            testBidirectionalAccessManager.AddUser("user4");
            testBidirectionalAccessManager.AddGroup("group1");
            testBidirectionalAccessManager.AddGroup("group2");
            testBidirectionalAccessManager.AddGroup("group3");
            testBidirectionalAccessManager.AddGroup("group4");
            testBidirectionalAccessManager.AddGroup("group5");
            testBidirectionalAccessManager.AddGroup("group6");
            testBidirectionalAccessManager.AddUserToGroupMapping("user1", "group1");
            testBidirectionalAccessManager.AddUserToGroupMapping("user2", "group2");
            testBidirectionalAccessManager.AddUserToGroupMapping("user3", "group2");
            testBidirectionalAccessManager.AddUserToGroupMapping("user4", "group4");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group2", "group3");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group3", "group5");
            testBidirectionalAccessManager.AddGroupToGroupMapping("group5", "group6");
            testBidirectionalAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Create);
            testBidirectionalAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Settings, AccessLevel.Modify);
            testBidirectionalAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.View);
            testBidirectionalAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testBidirectionalAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testBidirectionalAccessManager.AddEntityType("ClientAccount");
            testBidirectionalAccessManager.AddEntityType("BusinessUnit");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyC");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyD");
            testBidirectionalAccessManager.AddEntity("ClientAccount", "CompanyE");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddEntity("BusinessUnit", "Accounting");
            testBidirectionalAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyD");
            testBidirectionalAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyA");
            testBidirectionalAccessManager.AddUserToEntityMapping("user2", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group1", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Marketing");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group3", "ClientAccount", "CompanyC");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group5", "ClientAccount", "CompanyB");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group5", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Sales");
            testBidirectionalAccessManager.AddGroupToEntityMapping("group6", "BusinessUnit", "Accounting");
        }

        #region Nested Classes

        /// <summary>
        /// Version of the AccessManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class AccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : AccessManager<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>The DirectedGraph which stores the user to group mappings.</summary>
            public DirectedGraphBase<TUser, TGroup> UserToGroupMap
            {
                get { return userToGroupMap; }
            }

            /// <summary>Holds all valid entity types and values within the manager.  The Dictionary key holds the types of all entities, and each respective value holds the valid entity values within that type (e.g. the entity type could be 'ClientAccount', and values could be the names of all client accounts).</summary>
            public IDictionary<String, ISet<String>> Entities
            {
                get { return entities; }
            }

            /// <summary>A dictionary which stores user to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the user.</summary>
            public IDictionary<TUser, IDictionary<String, ISet<String>>> UserToEntityMap
            {
                get { return userToEntityMap; }
            }

            /// <summary>A dictionary which stores group to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the group.</summary>
            public IDictionary<TGroup, IDictionary<String, ISet<String>>> GroupToEntityMap
            {
                get { return groupToEntityMap; }
            }

            /// <summary>The reverse of the mappings in member 'userToEntityMap'.</summary>
            public IDictionary<String, IDictionary<String, ISet<TUser>>> UserToEntityReverseMap
            {
                get { return userToEntityReverseMap; }
            }

            /// <summary>The reverse of the mappings in member 'groupToEntityMap'.</summary>
            public IDictionary<String, IDictionary<String, ISet<TGroup>>> GroupToEntityReverseMap
            {
                get { return groupToEntityReverseMap; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.UnitTests+AccessManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
            public AccessManagerWithProtectedMembers(Boolean storeBidirectionalMappings)
                : base(storeBidirectionalMappings)
            {
            }

            /// <summary>
            /// Returns true if the 'userToComponentMap' field contains the specified user as a key.
            /// </summary>
            /// <param name="user">The user to check for.</param>
            /// <returns>True if the user exists as a key.  False otherwise.</returns>
            /// <remarks>Have to create a custom method to expose this, as the 'userToComponentMap' field itself cannot be made public as the dictionaries' value is a protected nested class.</remarks>
            public Boolean UserToComponentMapContainsKey(TUser user)
            {
                return userToComponentMap.ContainsKey(user);
            }

            /// <summary>
            /// Returns true if the 'groupToComponentMap' field contains the specified group as a key.
            /// </summary>
            /// <param name="group">The group to check for.</param>
            /// <returns>True if the group exists as a key.  False otherwise.</returns>
            /// <remarks>Have to create a custom method to expose this, as the 'groupToComponentMap' field itself cannot be made public as the dictionaries' value is a protected nested class.</remarks>
            public Boolean GroupToComponentMapContainsKey(TGroup group)
            {
                return groupToComponentMap.ContainsKey(group);
            }
        }

        #endregion
    }
}
