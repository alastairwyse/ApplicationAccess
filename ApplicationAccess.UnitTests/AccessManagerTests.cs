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

        [SetUp]
        protected void SetUp()
        {
            testAccessManager = new AccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>();
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
            Assert.AreEqual(0, testAccessManager.UserToComponentReverseMap.Count());
            Assert.AreEqual(0, testAccessManager.GroupToComponentReverseMap.Count());
            Assert.AreEqual(0, testAccessManager.Entities.Count());
            Assert.AreEqual(0, testAccessManager.UserToEntityMap.Count());
            Assert.AreEqual(0, testAccessManager.GroupToEntityMap.Count());
            Assert.AreEqual(0, testAccessManager.UserToEntityReverseMap.Count());
            Assert.AreEqual(0, testAccessManager.GroupToEntityReverseMap.Count());
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
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveUser("user1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap.ContainsKey(ApplicationScreen.Order));
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap[ApplicationScreen.Order].ContainsKey(AccessLevel.Create));
            Assert.IsFalse(testAccessManager.UserToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("user1"));
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("user2"));
            Assert.IsFalse(testAccessManager.UserToEntityMap.ContainsKey("user1"));
            Assert.IsTrue(testAccessManager.UserToEntityMap.ContainsKey("user2"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("user2"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
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
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveGroup("group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap.ContainsKey(ApplicationScreen.Order));
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap[ApplicationScreen.Order].ContainsKey(AccessLevel.Create));
            Assert.IsFalse(testAccessManager.GroupToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("group1"));
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("group2"));
            Assert.IsFalse(testAccessManager.GroupToEntityMap.ContainsKey("group1"));
            Assert.IsTrue(testAccessManager.GroupToEntityMap.ContainsKey("group2"));
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("group2"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
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

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.AddUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
        }

        [Test]
        public void AddUserToGroupMapping_GroupDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.AddUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
        }

        [Test]
        public void GetUserToGroupMappings_UserDoesntExist()
        {
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.GetUserToGroupMappings("user1", false).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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
        public void GetGroupToUserMappings_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToUserMappings("group1", false).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);


            e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToUserMappings("group1", true).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
        }

        [Test]
        public void GetGroupToUserMappings()
        {
            CreateUserGroupGraph(testAccessManager);

            HashSet<String> result = testAccessManager.GetGroupToUserMappings("Grp10", false);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = testAccessManager.GetGroupToUserMappings("Grp10", true);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));


            result = testAccessManager.GetGroupToUserMappings("Grp8", false);

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetGroupToUserMappings("Grp8", true);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));


            result = testAccessManager.GetGroupToUserMappings("Grp9", false);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = testAccessManager.GetGroupToUserMappings("Grp9", true);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = testAccessManager.GetGroupToUserMappings("Grp5", false);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = testAccessManager.GetGroupToUserMappings("Grp5", true);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = testAccessManager.GetGroupToUserMappings("Grp6", false);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = testAccessManager.GetGroupToUserMappings("Grp6", true);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = testAccessManager.GetGroupToUserMappings("Grp7", false);

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetGroupToUserMappings("Grp7", true);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void RemoveUserToGroupMapping_UserDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
        }

        [Test]
        public void RemoveUserToGroupMapping_GroupDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveUserToGroupMapping("user1", "group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.AddGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group2' does not exist."));
            Assert.AreEqual("toGroup", e.ParamName);
            Assert.AreEqual("group2", e.Group);
        }

        [Test]
        public void AddGroupToGroupMapping_FromGroupDoesntExist()
        {
            testAccessManager.AddGroup("group2");

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.AddGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("fromGroup", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToGroupMappings("group1", false).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
        public void GetGroupToGroupReverseMappings_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToGroupReverseMappings("group1", false).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);


            e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToGroupReverseMappings("group1", true).FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
        }

        [Test]
        public void GetGroupToGroupReverseMappings()
        {
            CreateUserGroupGraph(testAccessManager);

            HashSet<String> result = testAccessManager.GetGroupToGroupReverseMappings("Grp10", false);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp10", true);

            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


           result = testAccessManager.GetGroupToGroupReverseMappings("Grp8", false);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp8", true);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp9", false);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp9", true);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp5", false);

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp5", true);

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp6", false);

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp6", true);

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp7", false);

            Assert.AreEqual(0, result.Count);


            result = testAccessManager.GetGroupToGroupReverseMappings("Grp7", true);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void RemoveGroupToGroupMapping_ToGroupDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group2' does not exist."));
            Assert.AreEqual("toGroup", e.ParamName);
            Assert.AreEqual("group2", e.Group);
        }

        [Test]
        public void RemoveGroupToGroupMapping_FromGroupDoesntExist()
        {
            testAccessManager.AddGroup("group2");

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveGroupToGroupMapping("group1", "group2");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("fromGroup", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            testAccessManager.AddUser("user1");

            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);

            Assert.IsTrue(testAccessManager.UserToComponentMapContainsKey("user1"));
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap.ContainsKey(ApplicationScreen.Order));
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap[ApplicationScreen.Order].ContainsKey(AccessLevel.Create));
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("user1"));
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings_UserDoesntExist()
        {
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("user1").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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
        public void GetApplicationComponentAndAccessLevelToUserMappings_NoMappingsOrApplicationComponentAndOrAccessLevelDontExist()
        {
            IEnumerable<String> result = testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.View, false);

            Assert.AreEqual(0, result.Count());


            result = testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.View, true);

            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappings()
        {
            CreateUserGroupGraph(testAccessManager);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp10", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp8", ApplicationScreen.Order, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp9", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp5", ApplicationScreen.Order, AccessLevel.Delete);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp5", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp6", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp6", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp7", ApplicationScreen.Summary, AccessLevel.Create);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("Per8", ApplicationScreen.Summary, AccessLevel.Modify);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("Per8", ApplicationScreen.Summary, AccessLevel.Delete);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("Per9", ApplicationScreen.Summary, AccessLevel.Delete);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("Per9", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("Per10", ApplicationScreen.ManageProducts, AccessLevel.Create);

            var result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.View, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.View, true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.Create, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.Create, true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.Modify, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.Modify, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.Delete, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Order, AccessLevel.Delete, true));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.View, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.View, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.Create, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.Create, true));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.Modify, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per8"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.Modify, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per8"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.Delete, false));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.Summary, AccessLevel.Delete, true));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.View, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.View, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Modify, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Modify, true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_UserDoesntExist()
        {
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);

            testAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping("user1", ApplicationScreen.Order, AccessLevel.Create);

            Assert.IsFalse(testAccessManager.UserToComponentMapContainsKey("user1"));
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap.ContainsKey(ApplicationScreen.Order));
            Assert.IsTrue(testAccessManager.UserToComponentReverseMap[ApplicationScreen.Order].ContainsKey(AccessLevel.Create));
            Assert.IsFalse(testAccessManager.UserToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("user1"));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            testAccessManager.AddGroup("group1");

            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);

            Assert.IsTrue(testAccessManager.GroupToComponentMapContainsKey("group1"));
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap.ContainsKey(ApplicationScreen.Order));
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap[ApplicationScreen.Order].ContainsKey(AccessLevel.Create));
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("group1"));
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("group1").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
        public void GetApplicationComponentAndAccessLevelToGroupMappings_NoMappingsOrApplicationComponentAndOrAccessLevelDontExist()
        {
            IEnumerable<String> result = testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.View, false);

            Assert.AreEqual(0, result.Count());


            result = testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.View, true);

            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappings()
        {
            CreateUserGroupGraph(testAccessManager);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp10", ApplicationScreen.Order, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp8", ApplicationScreen.Order, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp8", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp9", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp9", ApplicationScreen.ManageProducts, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp5", ApplicationScreen.Order, AccessLevel.Delete);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp5", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp6", ApplicationScreen.Summary, AccessLevel.View);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp6", ApplicationScreen.Summary, AccessLevel.Create);
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Grp7", ApplicationScreen.Summary, AccessLevel.Modify);
            testAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("Per10", ApplicationScreen.Summary, AccessLevel.Delete);

            var result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.View, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp10"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.View, true));

            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.Create, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.Create, true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.View, false));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.View, true));

            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.Modify, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp9"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.Modify, true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp9"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.Delete, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Order, AccessLevel.Delete, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.View, false));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.View, true));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.Create, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.Create, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.Modify, false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.Modify, true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.Delete, false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.Summary, AccessLevel.Delete, true));

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);

            testAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.Create);

            Assert.IsFalse(testAccessManager.GroupToComponentMapContainsKey("group1"));
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap.ContainsKey(ApplicationScreen.Order));
            Assert.IsTrue(testAccessManager.GroupToComponentReverseMap[ApplicationScreen.Order].ContainsKey(AccessLevel.Create));
            Assert.IsFalse(testAccessManager.GroupToComponentReverseMap[ApplicationScreen.Order][AccessLevel.Create].Contains("group1"));
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
            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.RemoveEntityType("ClientAccount");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
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
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(2, testAccessManager.UserToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Sales"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("User1"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Contains("User2"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("Group1"));
        }

        [Test]
        public void AddEntity_EntityTypeDoesntExist()
        {
            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.AddEntity("ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
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
            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetEntities("ClientAccount").FirstOrDefault();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
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
            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
        }

        [Test]
        public void RemoveEntity_EntityDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.RemoveEntity("ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);
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
            Assert.AreEqual(2, testAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.AreEqual(2, testAccessManager.UserToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"].ContainsKey("Sales"));
            Assert.AreEqual(2, testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("User1"));
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("User2"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("User1"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["BusinessUnit"]["Sales"].Contains("User2"));
            Assert.AreEqual(2, testAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap.ContainsKey("BusinessUnit"));
            Assert.AreEqual(0, testAccessManager.GroupToEntityReverseMap["ClientAccount"].Count);
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["BusinessUnit"].Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["BusinessUnit"].ContainsKey("Marketing"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["BusinessUnit"]["Marketing"].Contains("Group1"));
        }

        [Test]
        public void AddUserToEntityMapping_UserDoesntExist()
        {
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
        }

        [Test]
        public void AddUserToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
        }

        [Test]
        public void AddUserToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);
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
        public void AddUserToEntityMapping()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");

            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyA");

            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("user1"));
        }

        [Test]
        public void GetUserToEntityMappingsUserOverload_UserDoesntExist()
        {
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.GetUserToEntityMappings("user1").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.GetUserToEntityMappings("user1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
        }

        [Test]
        public void GetUserToEntityMappingsUserAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetUserToEntityMappings("user1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("BusinessUnit", e.EntityType);
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
        public void GetEntityToUserMappings_EntityTypeDoesntExist()
        {
            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyA", false).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);


            e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyA", true).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
        }

        [Test]
        public void GetEntityToUserMappings_EntityDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyA", false).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);


            e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyA", true).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);
        }

        [Test]
        public void GetEntityToUserMappings()
        {
            CreateUserGroupGraph(testAccessManager);
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("ClientAccount", "CompanyE");
            testAccessManager.AddEntity("ClientAccount", "CompanyF");
            testAccessManager.AddEntity("ClientAccount", "CompanyG");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddEntity("BusinessUnit", "IT");
            testAccessManager.AddEntity("BusinessUnit", "HR");
            testAccessManager.AddGroupToEntityMapping("Grp10", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("Grp8", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Grp9", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("Grp5", "ClientAccount", "CompanyD");
            testAccessManager.AddGroupToEntityMapping("Grp5", "ClientAccount", "CompanyE");
            testAccessManager.AddGroupToEntityMapping("Grp6", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("Grp6", "ClientAccount", "CompanyE");
            testAccessManager.AddGroupToEntityMapping("Grp7", "BusinessUnit", "Sales");
            testAccessManager.AddUserToEntityMapping("Per8", "BusinessUnit", "IT");
            testAccessManager.AddUserToEntityMapping("Per8", "BusinessUnit", "HR");
            testAccessManager.AddUserToEntityMapping("Per9", "BusinessUnit", "HR");
            testAccessManager.AddUserToEntityMapping("Per9", "ClientAccount", "CompanyF");
            testAccessManager.AddUserToEntityMapping("Per10", "ClientAccount", "CompanyG");

            var result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyA", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyA", true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyB", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyB", true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyC", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyC", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyD", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyD", true));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "Marketing", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "Marketing", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "Sales", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "Sales", true));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "IT", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per8"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "IT", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per8"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "HR", false));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("BusinessUnit", "HR", true));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyF", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyF", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per9"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyG", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyG", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Per10"));


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyE", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToUserMappings("ClientAccount", "CompanyE", true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Per8"));
            Assert.IsTrue(result.Contains("Per9"));
            Assert.IsTrue(result.Contains("Per10"));
        }

        [Test]
        public void RemoveUserToEntityMapping_UserDoesntExist()
        {
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
        }

        [Test]
        public void RemoveUserToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
        }

        [Test]
        public void RemoveUserToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);
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
        public void RemoveUserToEntityMapping()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddUser("user2");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddUserToEntityMapping("user1", "ClientAccount", "CompanyB");

            testAccessManager.RemoveUserToEntityMapping("user1", "ClientAccount", "CompanyB");

            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testAccessManager.UserToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testAccessManager.UserToEntityReverseMap["ClientAccount"].ContainsKey("CompanyB"));
            Assert.AreEqual(0, testAccessManager.UserToEntityReverseMap["ClientAccount"]["CompanyB"].Count);
        }

        [Test]
        public void AddGroupToEntityMapping_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
        }

        [Test]
        public void AddGroupToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
        }

        [Test]
        public void AddGroupToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);
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
        public void AddGroupToEntityMapping()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");

            testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");

            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["ClientAccount"].ContainsKey("CompanyA"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyA"].Contains("group1"));
        }

        [Test]
        public void GetGroupToEntityMappingsGroupOverload_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToEntityMappings("group1").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetGroupToEntityMappings("group1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetGroupToEntityMappings("group1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("BusinessUnit", e.EntityType);
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
        public void GetEntityToGroupMappings_EntityTypeDoesntExist()
        {
            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyA", false).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);


            e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyA", true).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
        }

        [Test]
        public void GetEntityToGroupMappings_EntityDoesntExist()
        {
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyA", false).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);


            e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyA", true).Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);
        }

        [Test]
        public void GetEntityToGroupMappings()
        {
            CreateUserGroupGraph(testAccessManager);
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("ClientAccount", "CompanyC");
            testAccessManager.AddEntity("ClientAccount", "CompanyD");
            testAccessManager.AddEntity("ClientAccount", "CompanyF");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddEntity("BusinessUnit", "Sales");
            testAccessManager.AddEntity("BusinessUnit", "IT");
            testAccessManager.AddEntity("BusinessUnit", "HR");
            testAccessManager.AddGroupToEntityMapping("Grp10", "ClientAccount", "CompanyA");
            testAccessManager.AddGroupToEntityMapping("Grp8", "ClientAccount", "CompanyB");
            testAccessManager.AddGroupToEntityMapping("Grp8", "ClientAccount", "CompanyF");
            testAccessManager.AddGroupToEntityMapping("Grp9", "ClientAccount", "CompanyC");
            testAccessManager.AddGroupToEntityMapping("Grp9", "ClientAccount", "CompanyF");
            testAccessManager.AddGroupToEntityMapping("Grp5", "ClientAccount", "CompanyD");
            testAccessManager.AddGroupToEntityMapping("Grp5", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("Grp6", "BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("Grp6", "BusinessUnit", "Sales");
            testAccessManager.AddGroupToEntityMapping("Grp7", "BusinessUnit", "IT");
            testAccessManager.AddUserToEntityMapping("Per10", "BusinessUnit", "HR");

            var result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyA", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp10"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyA", true));

            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyB", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyB", true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyF", false));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyF", true));

            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp9"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyC", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp9"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyC", true));

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp9"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyD", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("ClientAccount", "CompanyD", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "Marketing", false));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "Marketing", true));

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "Sales", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "Sales", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp6"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "IT", false));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "IT", true));

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Grp7"));


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "HR", false));

            Assert.AreEqual(0, result.Count);


            result = new HashSet<String>(testAccessManager.GetEntityToGroupMappings("BusinessUnit", "HR", true));
        }

        [Test]
        public void RemoveGroupToEntityMapping_GroupDoesntExist()
        {
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
        }

        [Test]
        public void RemoveGroupToEntityMapping_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'ClientAccount' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
        }

        [Test]
        public void RemoveGroupToEntityMapping_EntityDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'CompanyA' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("ClientAccount", e.EntityType);
            Assert.AreEqual("CompanyA", e.Entity);
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
        public void RemoveGroupToEntityMapping()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddGroup("group2");
            testAccessManager.AddEntityType("ClientAccount");
            testAccessManager.AddEntityType("BusinessUnit");
            testAccessManager.AddEntity("ClientAccount", "CompanyA");
            testAccessManager.AddEntity("ClientAccount", "CompanyB");
            testAccessManager.AddEntity("BusinessUnit", "Marketing");
            testAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");

            testAccessManager.RemoveGroupToEntityMapping("group1", "ClientAccount", "CompanyB");

            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap.Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap.ContainsKey("ClientAccount"));
            Assert.AreEqual(1, testAccessManager.GroupToEntityReverseMap["ClientAccount"].Count);
            Assert.IsTrue(testAccessManager.GroupToEntityReverseMap["ClientAccount"].ContainsKey("CompanyB"));
            Assert.AreEqual(0, testAccessManager.GroupToEntityReverseMap["ClientAccount"]["CompanyB"].Count);
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

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.HasAccessToEntity("user1", "BusinessUnit", "Marketing");
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("BusinessUnit", e.EntityType);
        }

        [Test]
        public void HasAccessToEntity_EntityDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("BusinessUnit");

            var e = Assert.Throws<EntityNotFoundException>(delegate
            {
                testAccessManager.HasAccessToEntity("user1", "BusinessUnit", "Marketing");
            });

            Assert.That(e.Message, Does.StartWith("Entity 'Marketing' does not exist."));
            Assert.AreEqual("entity", e.ParamName);
            Assert.AreEqual("BusinessUnit", e.EntityType);
            Assert.AreEqual("Marketing", e.Entity);
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
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.GetApplicationComponentsAccessibleByUser("user1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetApplicationComponentsAccessibleByGroup("group1");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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
            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByUser("user1").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
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

            var e = Assert.Throws<UserNotFoundException<String>>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByUser("user1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
            Assert.AreEqual("user1", e.User);
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddUser("user1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByUser("user1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("BusinessUnit", e.EntityType);
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
            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByGroup("group1").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
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

            var e = Assert.Throws<GroupNotFoundException<String>>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByGroup("group1", "ClientAccount").Count();
            });

            Assert.That(e.Message, Does.StartWith("Group 'group1' does not exist."));
            Assert.AreEqual("group", e.ParamName);
            Assert.AreEqual("group1", e.Group);
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload_EntityTypeDoesntExist()
        {
            testAccessManager.AddGroup("group1");
            testAccessManager.AddEntityType("ClientAccount");

            var e = Assert.Throws<EntityTypeNotFoundException>(delegate
            {
                testAccessManager.GetEntitiesAccessibleByGroup("group1", "BusinessUnit").Count();
            });

            Assert.That(e.Message, Does.StartWith("Entity type 'BusinessUnit' does not exist."));
            Assert.AreEqual("entityType", e.ParamName);
            Assert.AreEqual("BusinessUnit", e.EntityType);
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
            foreach (ApplicationScreen currentScreen in testAccessManager.UserToComponentReverseMap.Keys)
            {
                foreach (AccessLevel currentAccessLevel in testAccessManager.UserToComponentReverseMap[currentScreen].Keys)
                {
                    Assert.AreEqual(0, testAccessManager.UserToComponentReverseMap[currentScreen][currentAccessLevel].Count);
                }
            }
            foreach (ApplicationScreen currentScreen in testAccessManager.GroupToComponentReverseMap.Keys)
            {
                foreach (AccessLevel currentAccessLevel in testAccessManager.GroupToComponentReverseMap[currentScreen].Keys)
                {
                    Assert.AreEqual(0, testAccessManager.GroupToComponentReverseMap[currentScreen][currentAccessLevel].Count);
                }
            }
            Assert.AreEqual(0, testAccessManager.Entities.Count());
            Assert.AreEqual(0, testAccessManager.UserToEntityMap.Count());
            Assert.AreEqual(0, testAccessManager.GroupToEntityMap.Count);
            Assert.AreEqual(0, testAccessManager.UserToEntityReverseMap.Count);
            Assert.AreEqual(0, testAccessManager.GroupToEntityReverseMap.Count);


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

        #region Private/Protected Methods

        // Creates the following graph of users and groups
        //
        //                             ----------------Grp10
        //                            /        --------/  |
        //                           /        /           |
        //                        Grp8       Grp9         |
        //                       /    \     / |  \        |
        //                      /      \   /  |   \       |
        // Groups           Grp5        Grp6  |    Grp7   |
        //                /      \          \ |           |
        // Users      Per8        Per9       Per10--------|
        //
        /// <summary>
        /// Creates a sample user and group model within the specified <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/>.
        /// </summary>
        /// <param name="inputAccessManager">The AccessManager to create the sample model in.</param>
        /// <remarks>Designed for testing of reverse mappings.</remarks>
        protected void CreateUserGroupGraph(AccessManager<String, String, ApplicationScreen, AccessLevel> inputAccessManager)
        {
            inputAccessManager.AddUser("Per8");
            inputAccessManager.AddUser("Per9");
            inputAccessManager.AddUser("Per10");
            inputAccessManager.AddGroup("Grp5");
            inputAccessManager.AddGroup("Grp6");
            inputAccessManager.AddGroup("Grp7");
            inputAccessManager.AddGroup("Grp8");
            inputAccessManager.AddGroup("Grp9");
            inputAccessManager.AddGroup("Grp10");
            inputAccessManager.AddUserToGroupMapping("Per8", "Grp5");
            inputAccessManager.AddUserToGroupMapping("Per9", "Grp5");
            inputAccessManager.AddUserToGroupMapping("Per10", "Grp6");
            inputAccessManager.AddUserToGroupMapping("Per10", "Grp9");
            inputAccessManager.AddUserToGroupMapping("Per10", "Grp10");
            inputAccessManager.AddGroupToGroupMapping("Grp5", "Grp8");
            inputAccessManager.AddGroupToGroupMapping("Grp6", "Grp8");
            inputAccessManager.AddGroupToGroupMapping("Grp6", "Grp9");
            inputAccessManager.AddGroupToGroupMapping("Grp7", "Grp9");
            inputAccessManager.AddGroupToGroupMapping("Grp8", "Grp10");
            inputAccessManager.AddGroupToGroupMapping("Grp9", "Grp10");
        }

        #endregion

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

            /// <summary>The reverse of the mappings in member 'userToComponentMap'.</summary>
            public IDictionary<TComponent, IDictionary<TAccess, ISet<TUser>>> UserToComponentReverseMap
            {
                get { return userToComponentReverseMap; }
            }

            /// <summary>The reverse of the mappings in member 'groupToComponentMap'.</summary>
            public IDictionary<TComponent, IDictionary<TAccess, ISet<TGroup>>> GroupToComponentReverseMap
            {
                get { return groupToComponentReverseMap; }
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
            public AccessManagerWithProtectedMembers()
                : base()
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
