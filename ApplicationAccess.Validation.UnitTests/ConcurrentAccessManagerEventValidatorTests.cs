/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.UnitTests;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Validation.UnitTests
{
    /// <summary>
    /// Unit tests for the ConcurrentAccessManagerEventValidator class.
    /// </summary>
    public class ConcurrentAccessManagerEventValidatorTests
    {
        private ConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel> testConcurrentAccessManager;
        private ConcurrentAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel> testConcurrentAccessManagerEventValidator;

        [SetUp]
        protected void SetUp()
        {
            testConcurrentAccessManager = new ConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>();
            testConcurrentAccessManagerEventValidator = new ConcurrentAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>(testConcurrentAccessManager);
        }

        [Test]
        public void ValidateAddUser()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;

            Action<String> postValidationAction = (actionUser) =>
            {
                Assert.AreEqual(testUser, actionUser);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUser(testUser, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.ContainsUser(testUser));
        }

        [Test]
        public void ValidateAddUser_ValidationNotSuccessful()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);

            Action<String> postValidationAction = (actionUser) =>
            {
                Assert.AreEqual(testUser, actionUser);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUser(testUser, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"User '{testUser}' already exists."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"User '{testUser}' already exists."));
            Assert.IsFalse(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.ContainsUser(testUser));
        }

        [Test]
        public void ValidateRemoveUser()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);

            Action<String> postValidationAction = (actionUser) =>
            {
                Assert.AreEqual(testUser, actionUser);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUser(testUser, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.ContainsUser(testUser));
        }

        [Test]
        public void ValidateRemoveUser_ValidationNotSuccessful()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;

            Action<String> postValidationAction = (actionUser) =>
            {
                Assert.AreEqual(testUser, actionUser);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUser(testUser, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.ContainsUser(testUser));
        }

        [Test]
        public void ValidateAddGroup()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;

            Action<String> postValidationAction = (actionGroup) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroup(testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.ContainsGroup(testGroup));
        }

        [Test]
        public void ValidateAddGroup_ValidationNotSuccessful()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testGroup);

            Action<String> postValidationAction = (actionGroup) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroup(testGroup, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Group '{testGroup}' already exists."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Group '{testGroup}' already exists."));
            Assert.IsFalse(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.ContainsGroup(testGroup));
        }

        [Test]
        public void ValidateRemoveGroup()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testGroup);

            Action<String> postValidationAction = (actionGroup) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroup(testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.ContainsGroup(testGroup));
        }

        [Test]
        public void ValidateRemoveGroup_ValidationNotSuccessful()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;

            Action<String> postValidationAction = (actionGroup) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroup(testGroup, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.ContainsGroup(testGroup));
        }

        [Test]
        public void ValidateAddUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddGroup(testGroup);

            Action<String, String> postValidationAction = (actionUser, actionGroup) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUserToGroupMapping(testUser, testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void ValidateAddUserToGroupMapping_ValidationNotSuccessful()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;

            Action<String, String> postValidationAction = (actionUser, actionGroup) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUserToGroupMapping(testUser, testGroup, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveUserToGroupMapping()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);

            Action<String, String> postValidationAction = (actionUser, actionGroup) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUserToGroupMapping(testUser, testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.GetUserToGroupMappings(testUser, false).Contains(testGroup));
        }

        [Test]
        public void ValidateRemoveUserToGroupMapping_ValidationNotSuccessful()
        {
            String testUser = "user1";
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;

            Action<String, String> postValidationAction = (actionUser, actionGroup) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testGroup, actionGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUserToGroupMapping(testUser, testGroup, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testFromGroup);
            testConcurrentAccessManager.AddGroup(testToGroup);

            Action<String, String> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                Assert.AreEqual(testFromGroup, actionFromGroup);
                Assert.AreEqual(testToGroup, actionToGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroupToGroupMapping(testFromGroup, testToGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void ValidateAddGroupToGroupMapping_ValidationNotSuccessful()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Boolean assertionsWereChecked = false;

            Action<String, String> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                Assert.AreEqual(testFromGroup, actionFromGroup);
                Assert.AreEqual(testToGroup, actionToGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroupToGroupMapping(testFromGroup, testToGroup, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Group '{testFromGroup}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Group '{testFromGroup}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveGroupToGroupMapping()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testFromGroup);
            testConcurrentAccessManager.AddGroup(testToGroup);
            testConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);

            Action<String, String> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                Assert.AreEqual(testFromGroup, actionFromGroup);
                Assert.AreEqual(testToGroup, actionToGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroupToGroupMapping(testFromGroup, testToGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.GetGroupToGroupMappings(testFromGroup, false).Contains(testToGroup));
        }

        [Test]
        public void ValidateRemoveGroupToGroupMapping_ValidationNotSuccessful()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            Boolean assertionsWereChecked = false;

            Action<String, String> postValidationAction = (actionFromGroup, actionToGroup) =>
            {
                Assert.AreEqual(testFromGroup, actionFromGroup);
                Assert.AreEqual(testToGroup, actionToGroup);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroupToGroupMapping(testFromGroup, testToGroup, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Group '{testFromGroup}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Group '{testFromGroup}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);

            Action<String, ApplicationScreen, AccessLevel> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.ManageProducts);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Modify);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Modify, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
        }

        [Test]
        public void ValidateAddUserToApplicationComponentAndAccessLevelMapping_ValidationNotSuccessful()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.ManageProducts);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Modify);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Modify, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Modify);

            Action <String, ApplicationScreen, AccessLevel> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.ManageProducts);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Modify);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Modify, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
        }

        [Test]
        public void ValidateRemoveUserToApplicationComponentAndAccessLevelMapping_ValidationNotSuccessful()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postValidationAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.ManageProducts);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Modify);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.ManageProducts, AccessLevel.Modify, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testGroup);

            Action<String, ApplicationScreen, AccessLevel> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.Order);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Create);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
        }

        [Test]
        public void ValidateAddGroupToApplicationComponentAndAccessLevelMapping_ValidationNotSuccessful()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.Order);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Create);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create);

            Action<String, ApplicationScreen, AccessLevel> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.Order);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Create);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
        }

        [Test]
        public void ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping_ValidationNotSuccessful()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postValidationAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(actionApplicationComponent, ApplicationScreen.Order);
                Assert.AreEqual(actionAccessLevel, AccessLevel.Create);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddEntityType()
        {
            String testEntityType = "ClientAccount";
            Boolean assertionsWereChecked = false;

            Action<String> postValidationAction = (actionEntityType) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddEntityType(testEntityType, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.ContainsEntityType(testEntityType));
        }

        [Test]
        public void ValidateAddEntityType_ValidationNotSuccessful()
        {
            String testEntityType = "ClientAccount";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddEntityType(testEntityType);

            Action<String> postValidationAction = (actionEntityType) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddEntityType(testEntityType, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Entity type '{testEntityType}' already exists."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Entity type '{testEntityType}' already exists."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveEntityType()
        {
            String testEntityType = "ClientAccount";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddEntityType(testEntityType);

            Action<String> postValidationAction = (actionEntityType) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveEntityType(testEntityType, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.ContainsEntityType(testEntityType));
        }

        [Test]
        public void ValidateRemoveEntityType_ValidationNotSuccessful()
        {
            String testEntityType = "ClientAccount";
            Boolean assertionsWereChecked = false;

            Action<String> postValidationAction = (actionEntityType) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveEntityType(testEntityType, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Entity type '{testEntityType}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Entity type '{testEntityType}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddEntityType(testEntityType);

            Action<String, String> postValidationAction = (actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddEntity(testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
        }

        [Test]
        public void ValidateAddEntity_ValidationNotSuccessful()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            Action<String, String> postValidationAction = (actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddEntity(testEntityType, testEntity, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Entity type '{testEntityType}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Entity type '{testEntityType}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveEntity()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            Action<String, String> postValidationAction = (actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveEntity(testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.GetEntities(testEntityType).Contains(testEntity));
        }

        [Test]
        public void ValidateRemoveEntity_ValidationNotSuccessful()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, "CompanyB");

            Action<String, String> postValidationAction = (actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveEntity(testEntityType, testEntity, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Entity '{testEntity}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Entity '{testEntity}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            Action<String, String, String> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUserToEntityMapping(testUser, testEntityType, testEntity, postValidationAction);
            
            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.GetUserToEntityMappings(testUser, testEntityType).Contains(testEntity));
        }

        [Test]
        public void ValidateAddUserToEntityMapping_ValidationNotSuccessful()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            Action<String, String, String> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddUserToEntityMapping(testUser, testEntityType, testEntity, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"User '{testUser}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveUserToEntityMapping()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            Action<String, String, String> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUserToEntityMapping(testUser, testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.GetUserToEntityMappings(testUser, testEntityType).Contains(testEntity));
        }

        [Test]
        public void ValidateRemoveUserToEntityMapping_ValidationNotSuccessful()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddEntityType(testEntityType);

            Action<String, String, String> postValidationAction = (actionUser, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveUserToEntityMapping(testUser, testEntityType, testEntity, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Entity '{testEntity}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Entity '{testEntity}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            Action<String, String, String> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroupToEntityMapping(testGroup, testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsTrue(testConcurrentAccessManager.GetGroupToEntityMappings(testGroup, testEntityType).Contains(testEntity));
        }

        [Test]
        public void ValidateAddGroupToEntityMapping_ValidationNotSuccessful()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            Action<String, String, String> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateAddGroupToEntityMapping(testGroup, testEntityType, testEntity, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Group '{testGroup}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveGroupToEntityMapping()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            Action<String, String, String> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
            Assert.IsFalse(testConcurrentAccessManager.GetGroupToEntityMappings(testGroup, testEntityType).Contains(testEntity));
        }

        [Test]
        public void ValidateRemoveGroupToEntityMapping_ValidationNotSuccessful()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddEntityType(testEntityType);

            Action<String, String, String> postValidationAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                assertionsWereChecked = true;
            };

            ValidationResult result = testConcurrentAccessManagerEventValidator.ValidateRemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postValidationAction);

            Assert.IsFalse(result.Successful);
            Assert.That(result.Message, Does.StartWith($"Entity '{testEntity}' does not exist."));
            Assert.That(result.ValidationExceptionDispatchInfo.SourceException.Message, Does.StartWith($"Entity '{testEntity}' does not exist."));
            Assert.IsFalse(assertionsWereChecked);
        }
    }
}
