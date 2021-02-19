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
using System.Threading;
using ApplicationAccess;
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
                testAccessManager.GetUserToGroupMappings("user1");
            });

            Assert.That(e.Message, Does.StartWith("User 'user1' does not exist."));
            Assert.AreEqual("user", e.ParamName);
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
                testAccessManager.GetGroupToGroupMappings("group11");
            });

            Assert.That(e.Message, Does.StartWith("Group 'group11' does not exist."));
            Assert.AreEqual("group", e.ParamName);
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

            foreach (KeyValuePair<String, Dictionary<String, HashSet<String>>> currentKvp in testAccessManager.UserToEntityMap)
            {
                Assert.False(currentKvp.Value.ContainsKey("ClientAccount"));
            }
            foreach (KeyValuePair<String, Dictionary<String, HashSet<String>>> currentKvp in testAccessManager.GroupToEntityMap)
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
                testAccessManager.GetEntities("ClientAccount");
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

            foreach (KeyValuePair<String, Dictionary<String, HashSet<String>>> currentKvp in testAccessManager.UserToEntityMap)
            {
                if (currentKvp.Value.ContainsKey("ClientAccount"))
                {
                    Assert.False(currentKvp.Value["ClientAccount"].Contains("CompanyB"));
                }
            }
            foreach (KeyValuePair<String, Dictionary<String, HashSet<String>>> currentKvp in testAccessManager.GroupToEntityMap)
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
        public void GetAccessibleEntities_UserDoesntExist()
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
        public void GetAccessibleEntities_EntityTypeDoesntExist()
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
        public void GetAccessibleEntities()
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

            HashSet<String> result = testAccessManager.GetAccessibleEntities("user2", "ClientAccount");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        #region Nested Classes

        protected enum ApplicationScreen
        {
            Order, 
            Summary, 
            ManageProducts, 
            Settings
        }

        protected enum AccessLevel
        {
            View, 
            Create, 
            Modify, 
            Delete
        }

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
            public DirectedGraph<TUser, TGroup> UserToGroupMap
            {
                get { return userToGroupMap; }
            }

            /// <summary>Holds all valid entity types and values within the manager.  The Dictionary key holds the types of all entities, and each respective value holds the valid entity values within that type (e.g. the entity type could be 'ClientAccount', and values could be the names of all client accounts).</summary>
            public Dictionary<String, HashSet<String>> Entities
            {
                get { return entities; }
            }

            /// <summary>A dictionary which stores user to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the user.</summary>
            public Dictionary<TUser, Dictionary<String, HashSet<String>>> UserToEntityMap
            {
                get { return userToEntityMap; }
            }

            /// <summary>A dictionary which stores group to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the group.</summary>
            public Dictionary<TGroup, Dictionary<String, HashSet<String>>> GroupToEntityMap
            {
                get { return groupToEntityMap; }
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
