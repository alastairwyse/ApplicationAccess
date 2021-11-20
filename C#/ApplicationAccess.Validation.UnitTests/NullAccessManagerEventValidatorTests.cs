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
using ApplicationAccess.UnitTests;
using NUnit.Framework;

namespace ApplicationAccess.Validation.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Validation.NullAccessManagerEventValidator class.
    /// </summary>
    public class NullAccessManagerEventValidatorTests
    {
        private NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel> testNullAccessManagerEventValidator;

        [SetUp]
        protected void SetUp()
        {
            testNullAccessManagerEventValidator = new NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>();
        }

        [Test]
        public void ValidateAddUser()
        {
            string testUser = "user1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String>((user) => 
            {
                Assert.AreEqual(testUser, user);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddUser(testUser, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveUser()
        {
            string testUser = "user1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String>((user) =>
            {
                Assert.AreEqual(testUser, user);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveUser(testUser, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddGroup()
        {
            string testGroup = "group1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String>((group) =>
            {
                Assert.AreEqual(testGroup, group);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddGroup(testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveGroup()
        {
            string testGroup = "group1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String>((group) =>
            {
                Assert.AreEqual(testGroup, group);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveGroup(testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddUserToGroupMapping()
        {
            string testUser = "user1";
            string testGroup = "group1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String>((user, group) =>
            {
                Assert.AreEqual(testUser, user);
                Assert.AreEqual(testGroup, group);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddUserToGroupMapping(testUser, testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveUserToGroupMapping()
        {
            string testUser = "user1";
            string testGroup = "group1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String>((user, group) =>
            {
                Assert.AreEqual(testUser, user);
                Assert.AreEqual(testGroup, group);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveUserToGroupMapping(testUser, testGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddGroupToGroupMapping()
        {
            string testFromGroup = "group1";
            string testToGroup = "group2";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String>((fromGroup, toGroup) =>
            {
                Assert.AreEqual(testFromGroup, fromGroup);
                Assert.AreEqual(testToGroup, toGroup);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddGroupToGroupMapping(testFromGroup, testToGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveGroupToGroupMapping()
        {
            string testFromGroup = "group1";
            string testToGroup = "group2";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String>((fromGroup, toGroup) =>
            {
                Assert.AreEqual(testFromGroup, fromGroup);
                Assert.AreEqual(testToGroup, toGroup);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveGroupToGroupMapping(testFromGroup, testToGroup, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddUserToApplicationComponentAndAccessLevelMapping()
        {
            string testUser = "user1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, ApplicationScreen, AccessLevel>((user, applicationComponent, accessLevel) =>
            {
                Assert.AreEqual(testUser, user);
                Assert.AreEqual(ApplicationScreen.Order, applicationComponent);
                Assert.AreEqual(AccessLevel.Create, accessLevel);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            string testUser = "user1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, ApplicationScreen, AccessLevel>((user, applicationComponent, accessLevel) =>
            {
                Assert.AreEqual(testUser, user);
                Assert.AreEqual(ApplicationScreen.Order, applicationComponent);
                Assert.AreEqual(AccessLevel.Create, accessLevel);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddGroupToApplicationComponentAndAccessLevelMapping()
        {
            string testGroup = "group1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, ApplicationScreen, AccessLevel>((group, applicationComponent, accessLevel) =>
            {
                Assert.AreEqual(testGroup, group);
                Assert.AreEqual(ApplicationScreen.Order, applicationComponent);
                Assert.AreEqual(AccessLevel.Create, accessLevel);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            string testGroup = "group1";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, ApplicationScreen, AccessLevel>((group, applicationComponent, accessLevel) =>
            {
                Assert.AreEqual(testGroup, group);
                Assert.AreEqual(ApplicationScreen.Order, applicationComponent);
                Assert.AreEqual(AccessLevel.Create, accessLevel);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddEntityType()
        {
            string testEntityType = "ClientAccount";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String>((entityType) =>
            {
                Assert.AreEqual(testEntityType, entityType);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddEntityType(testEntityType, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveEntityType()
        {
            string testEntityType = "ClientAccount";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String>((entityType) =>
            {
                Assert.AreEqual(testEntityType, entityType);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveEntityType(testEntityType, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddEntity()
        {
            string testEntityType = "ClientAccount";
            string testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String>((entityType, entity) =>
            {
                Assert.AreEqual(testEntityType, entityType);
                Assert.AreEqual(testEntity, entity);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddEntity(testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveEntity()
        {
            string testEntityType = "ClientAccount";
            string testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String>((entityType, entity) =>
            {
                Assert.AreEqual(testEntityType, entityType);
                Assert.AreEqual(testEntity, entity);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveEntity(testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddUserToEntityMapping()
        {
            string testUser = "user1";
            string testEntityType = "ClientAccount";
            string testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String, String>((user, entityType, entity) =>
            {
                Assert.AreEqual(testUser, user);
                Assert.AreEqual(testEntityType, entityType);
                Assert.AreEqual(testEntity, entity);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddUserToEntityMapping(testUser, testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveUserToEntityMapping()
        {
            string testUser = "user1";
            string testEntityType = "ClientAccount";
            string testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String, String>((user, entityType, entity) =>
            {
                Assert.AreEqual(testUser, user);
                Assert.AreEqual(testEntityType, entityType);
                Assert.AreEqual(testEntity, entity);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveUserToEntityMapping(testUser, testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateAddGroupToEntityMapping()
        {
            string testGroup = "group1";
            string testEntityType = "ClientAccount";
            string testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String, String>((group, entityType, entity) =>
            {
                Assert.AreEqual(testGroup, group);
                Assert.AreEqual(testEntityType, entityType);
                Assert.AreEqual(testEntity, entity);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateAddGroupToEntityMapping(testGroup, testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void ValidateRemoveGroupToEntityMapping()
        {
            string testGroup = "group1";
            string testEntityType = "ClientAccount";
            string testEntity = "CompanyA";
            Boolean assertionsWereChecked = false;

            var postValidationAction = new Action<String, String, String>((group, entityType, entity) =>
            {
                Assert.AreEqual(testGroup, group);
                Assert.AreEqual(testEntityType, entityType);
                Assert.AreEqual(testEntity, entity);
                assertionsWereChecked = true;
            });

            ValidationResult result = testNullAccessManagerEventValidator.ValidateRemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postValidationAction);

            Assert.IsTrue(result.Successful);
            Assert.IsTrue(assertionsWereChecked);
        }
    }
}
