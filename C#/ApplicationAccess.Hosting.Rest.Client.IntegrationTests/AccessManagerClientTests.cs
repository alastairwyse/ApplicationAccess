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
using ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests;
using NUnit.Framework;
using NSubstitute;
using ApplicationLogging;
using ApplicationMetrics;
using System.Collections.Generic;
using System.Net.Http;

namespace ApplicationAccess.Hosting.Rest.Client.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
    /// </summary>
    public class AccessManagerClientTests : IntegrationTestsBase
    {
        private Uri testBaseUrl;
        private MethodCallCountingStringUniqueStringifier userStringifier;
        private MethodCallCountingStringUniqueStringifier groupStringifier;
        private MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        private MethodCallCountingStringUniqueStringifier accessLevelStringifier;
        private IApplicationLogger mockLogger;
        private IMetricLogger mockMetricLogger;
        private AccessManagerClient<String, String, String, String> testAccessManagerClient;

        [OneTimeSetUp]
        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
        }

        [OneTimeTearDown]
        protected override void OneTimeTearDown()
        {
            base.OneTimeTearDown();
        }

        [SetUp]
        protected void SetUp()
        {
            testBaseUrl = new Uri("http://localhost/");
            userStringifier = new MethodCallCountingStringUniqueStringifier();
            groupStringifier = new MethodCallCountingStringUniqueStringifier();
            applicationComponentStringifier = new MethodCallCountingStringUniqueStringifier();
            accessLevelStringifier = new MethodCallCountingStringUniqueStringifier();
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerClient = new AccessManagerClient<String, String, String, String>
            (
                testBaseUrl, 
                client, 
                userStringifier, 
                groupStringifier, 
                applicationComponentStringifier, 
                accessLevelStringifier, 
                5, 
                1, 
                mockLogger,
                mockMetricLogger
            );
        }

        [TearDown]
        protected void TearDown()
        {
            testAccessManagerClient.Dispose();
        }

        [Test]
        public void UsersProperty()
        {
            var users = new List<String>() { "user1", "user2", "user3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.Users.Returns(users);

            var result = new List<String>(testAccessManagerClient.Users);

            var throwAway = mockUserQueryProcessor.Received(1).Users;
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("user1", result[0]);
            Assert.AreEqual("user2", result[1]);
            Assert.AreEqual("user3", result[2]);
        }

        [Test]
        public void GroupsProperty()
        {
            var groups = new List<String>() { "group1", "group2", "group3" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.Groups.Returns(groups);

            var result = new List<String>(testAccessManagerClient.Groups);

            var throwAway = mockGroupQueryProcessor.Received(1).Groups;
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("group1", result[0]);
            Assert.AreEqual("group2", result[1]);
            Assert.AreEqual("group3", result[2]);
        }

        [Test]
        public void EntityTypesProperty()
        {
            var entityTypes = new List<String>() { "BusinessUnit", "ClientAccount" };
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.EntityTypes.Returns(entityTypes);

            var result = new List<String>(testAccessManagerClient.EntityTypes);

            var throwAway = mockEntityQueryProcessor.Received(1).EntityTypes;
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("BusinessUnit", result[0]);
            Assert.AreEqual("ClientAccount", result[1]);
        }

        [Test]
        public void AddUser()
        {
            const String testUser = "user1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUser(testUser);

            mockUserEventProcessor.Received(1).AddUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void ContainsUser()
        {
            const String testUser = "user1";
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.ContainsUser(testUser).Returns(true);

            Boolean result = testAccessManagerClient.ContainsUser(testUser);

            mockUserQueryProcessor.Received(1).ContainsUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.IsTrue(result);


            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.ContainsUser(testUser).Returns(false);

            result = testAccessManagerClient.ContainsUser(testUser);

            mockUserQueryProcessor.Received(1).ContainsUser(testUser);
            Assert.AreEqual(2, userStringifier.ToStringCallCount);
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveUser()
        {
            const String testUser = "user1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUser(testUser);

            mockUserEventProcessor.Received(1).RemoveUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroup()
        {
            const String testGroup = "group1";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroup(testGroup);

            mockGroupEventProcessor.Received(1).AddGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void ContainsGroup()
        {
            const String testGroup = "group1";
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.ContainsGroup(testGroup).Returns(true);

            Boolean result = testAccessManagerClient.ContainsGroup(testGroup);

            mockGroupQueryProcessor.Received(1).ContainsGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.IsTrue(result);


            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.ContainsGroup(testGroup).Returns(false);

            result = testAccessManagerClient.ContainsGroup(testGroup);

            mockGroupQueryProcessor.Received(1).ContainsGroup(testGroup);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveGroup()
        {
            const String testGroup = "group1";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroup(testGroup);

            mockGroupEventProcessor.Received(1).RemoveGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            const String testUser = "user1";
            const String testGroup = "group1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToGroupMapping(testUser, testGroup);

            mockUserEventProcessor.Received(1).AddUserToGroupMapping(testUser, testGroup);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void GetUserToGroupMappings()
        {
            const String testUser = "user1";
            var testGroups = new HashSet<String>() { "group1", "group2", "group3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToGroupMappings(testUser, false).Returns(testGroups);

            HashSet<String> result = testAccessManagerClient.GetUserToGroupMappings(testUser, false);

            mockUserQueryProcessor.Received(1).GetUserToGroupMappings(testUser, false);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            const String testUser = "user1";
            const String testGroup = "group1";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToGroupMapping(testUser, testGroup);

            mockUserEventProcessor.Received(1).RemoveUserToGroupMapping(testUser, testGroup);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            const String testFromGroup = "group1";
            const String testToGroup = "group2";
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToGroupMapping(testFromGroup, testToGroup);

            mockGroupToGroupEventProcessor.Received(1).AddGroupToGroupMapping(testFromGroup, testToGroup);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            const String testFromGroup = "group1";
            var testToGroups = new HashSet<String>() { "group2", "group3", "group4" };
            mockGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockGroupToGroupQueryProcessor.GetGroupToGroupMappings(testFromGroup, false).Returns(testToGroups);

            HashSet<String> result = testAccessManagerClient.GetGroupToGroupMappings(testFromGroup, false);

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(testFromGroup, false);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            const String testFromGroup = "group1";
            const String testToGroup = "group2";
            mockGroupToGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToGroupMapping(testFromGroup, testToGroup);

            mockGroupToGroupEventProcessor.Received(1).RemoveGroupToGroupMapping(testFromGroup, testToGroup);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            const String testUser = "user1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);

            mockUserEventProcessor.Received(1).AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings()
        {
            const String testUser = "user1";
            var testApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            { 
                new Tuple<String, String>("ManageProductsScreen", "Modify"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(testUser).Returns(testApplicationComponentsAndAccessLevels);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetUserToApplicationComponentAndAccessLevelMappings(testUser));

            mockUserQueryProcessor.Received(1).GetUserToApplicationComponentAndAccessLevelMappings(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("ManageProductsScreen", result[0].Item1);
            Assert.AreEqual("Modify", result[0].Item2);
            Assert.AreEqual("SummaryScreen", result[1].Item1);
            Assert.AreEqual("View", result[1].Item2);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            const String testUser = "user1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);

            mockUserEventProcessor.Received(1).RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            const String testGroup = "group1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);

            mockGroupEventProcessor.Received(1).AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings()
        {
            const String testGroup = "group1";
            var testApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ManageProductsScreen", "Create"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Returns(testApplicationComponentsAndAccessLevels);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup));

            mockGroupQueryProcessor.Received(1).GetGroupToApplicationComponentAndAccessLevelMappings(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("ManageProductsScreen", result[0].Item1);
            Assert.AreEqual("Create", result[0].Item2);
            Assert.AreEqual("SummaryScreen", result[1].Item1);
            Assert.AreEqual("View", result[1].Item2);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            const String testGroup = "group1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);

            mockGroupEventProcessor.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddEntityType()
        {
            const String testEntityType = "BusinessUnit";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddEntityType(testEntityType);

            mockEntityEventProcessor.Received(1).AddEntityType(testEntityType);
        }

        [Test]
        public void ContainsEntityType()
        {
            const String testEntityType = "BusinessUnit";
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.ContainsEntityType(testEntityType).Returns(true);

            Boolean result = testAccessManagerClient.ContainsEntityType(testEntityType);

            mockEntityQueryProcessor.Received(1).ContainsEntityType(testEntityType);
            Assert.IsTrue(result);


            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.ContainsEntityType(testEntityType).Returns(false);

            result = testAccessManagerClient.ContainsEntityType(testEntityType);

            mockEntityQueryProcessor.Received(1).ContainsEntityType(testEntityType);
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveEntityType()
        {
            const String testEntityType = "BusinessUnit";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveEntityType(testEntityType);

            mockEntityEventProcessor.Received(1).RemoveEntityType(testEntityType);
        }

        [Test]
        public void AddEntity()
        {
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddEntity(testEntityType, testEntity);

            mockEntityEventProcessor.Received(1).AddEntity(testEntityType, testEntity);
        }

        [Test]
        public void GetEntities()
        {
            const String testEntityType = "ClientAccount";
            var testEntitiess = new List<String>() { "ClientA", "ClientB", "ClientC" };
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.GetEntities(testEntityType).Returns(testEntitiess);

            var result = new List<String>(testAccessManagerClient.GetEntities(testEntityType));

            mockEntityQueryProcessor.Received(1).GetEntities(testEntityType);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("ClientA", result[0]);
            Assert.AreEqual("ClientB", result[1]);
            Assert.AreEqual("ClientC", result[2]);
        }

        [Test]
        public void ContainsEntity()
        {
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.ContainsEntity(testEntityType, testEntity).Returns(true);

            Boolean result = testAccessManagerClient.ContainsEntity(testEntityType, testEntity);

            mockEntityQueryProcessor.Received(1).ContainsEntity(testEntityType, testEntity);
            Assert.IsTrue(result);


            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.ContainsEntity(testEntityType, testEntity).Returns(false);

            result = testAccessManagerClient.ContainsEntity(testEntityType, testEntity);

            mockEntityQueryProcessor.Received(1).ContainsEntity(testEntityType, testEntity);
            Assert.IsFalse(result);
        }

        [Test]
        public void RemoveEntity()
        {
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockEntityEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveEntity(testEntityType, testEntity);

            mockEntityEventProcessor.Received(1).RemoveEntity(testEntityType, testEntity);
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            const String testUser = "user1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            mockUserEventProcessor.Received(1).AddUserToEntityMapping(testUser, testEntityType, testEntity);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void GetUserToEntityMappings()
        {
            const String testUser = "user1";
            var testEntittTypesAndEntities = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientB"),
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToEntityMappings(testUser).Returns(testEntittTypesAndEntities);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetUserToEntityMappings(testUser));

            mockUserQueryProcessor.Received(1).GetUserToEntityMappings(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("BusinessUnit", result[0].Item1);
            Assert.AreEqual("Sales", result[0].Item2);
            Assert.AreEqual("ClientAccount", result[1].Item1);
            Assert.AreEqual("ClientA", result[1].Item2);
            Assert.AreEqual("ClientAccount", result[2].Item1);
            Assert.AreEqual("ClientB", result[2].Item2);
        }

        [Test]
        public void GetUserToEntityMappingsUserAndEntityTypeOverload()
        {
            const String testUser = "user1";
            const String testEntityType = "ClientAccount";
            var testEntities = new List<String>() { "ClientA", "ClientB" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToEntityMappings(testUser, testEntityType).Returns(testEntities);

            var result = new List<String>(testAccessManagerClient.GetUserToEntityMappings(testUser, testEntityType));

            mockUserQueryProcessor.Received(1).GetUserToEntityMappings(testUser, testEntityType);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("ClientA", result[0]);
            Assert.AreEqual("ClientB", result[1]);
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            const String testUser = "user1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockUserEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveUserToEntityMapping(testUser, testEntityType, testEntity);

            mockUserEventProcessor.Received(1).RemoveUserToEntityMapping(testUser, testEntityType, testEntity);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            const String testGroup = "group1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockGroupEventProcessor.Received(1).AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void GetGroupToEntityMappings()
        {
            const String testGroup = "group1";
            var testEntittTypesAndEntities = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientB"),
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetGroupToEntityMappings(testGroup).Returns(testEntittTypesAndEntities);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetGroupToEntityMappings(testGroup));

            mockGroupQueryProcessor.Received(1).GetGroupToEntityMappings(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("BusinessUnit", result[0].Item1);
            Assert.AreEqual("Sales", result[0].Item2);
            Assert.AreEqual("ClientAccount", result[1].Item1);
            Assert.AreEqual("ClientA", result[1].Item2);
            Assert.AreEqual("ClientAccount", result[2].Item1);
            Assert.AreEqual("ClientB", result[2].Item2);
        }

        [Test]
        public void GetGroupToEntityMappingsUserAndEntityTypeOverload()
        {
            const String testGroup = "group1";
            const String testEntityType = "ClientAccount";
            var testEntities = new List<String>() { "ClientA", "ClientB" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetGroupToEntityMappings(testGroup, testEntityType).Returns(testEntities);

            var result = new List<String>(testAccessManagerClient.GetGroupToEntityMappings(testGroup, testEntityType));

            mockGroupQueryProcessor.Received(1).GetGroupToEntityMappings(testGroup, testEntityType);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("ClientA", result[0]);
            Assert.AreEqual("ClientB", result[1]);
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            const String testGroup = "group1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockGroupEventProcessor.ClearReceivedCalls();

            testAccessManagerClient.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);

            mockGroupEventProcessor.Received(1).RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void HasAccessToApplicationComponent()
        {
            const String testUser = "user1";
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.HasAccessToApplicationComponent(testUser, testApplicationComponent, testAccessLevel).Returns(true);

            Boolean result = testAccessManagerClient.HasAccessToApplicationComponent(testUser, testApplicationComponent, testAccessLevel);

            mockUserQueryProcessor.Received(1).HasAccessToApplicationComponent(testUser, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToEntity()
        {
            const String testUser = "user1";
            const String testEntityType = "BusinessUnit";
            const String testEntity = "Sales";
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.HasAccessToEntity(testUser, testEntityType, testEntity).Returns(false);

            Boolean result = testAccessManagerClient.HasAccessToEntity(testUser, testEntityType, testEntity);

            mockUserQueryProcessor.Received(1).HasAccessToEntity(testUser, testEntityType, testEntity);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.IsFalse(result);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser()
        {
            const String testUser = "user1";
            var testApplicationComponentsAndAccessLevels = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("ManageProductsScreen", "Modify"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetApplicationComponentsAccessibleByUser(testUser).Returns(testApplicationComponentsAndAccessLevels);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetApplicationComponentsAccessibleByUser(testUser));

            mockUserQueryProcessor.Received(1).GetApplicationComponentsAccessibleByUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ManageProductsScreen", "Modify")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("SummaryScreen", "View")));
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup()
        {
            const String testGroup = "group1";
            var testApplicationComponentsAndAccessLevels = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("ManageProductsScreen", "Create"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetApplicationComponentsAccessibleByGroup(testGroup).Returns(testApplicationComponentsAndAccessLevels);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetApplicationComponentsAccessibleByGroup(testGroup));

            mockGroupQueryProcessor.Received(1).GetApplicationComponentsAccessibleByGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ManageProductsScreen", "Create")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("SummaryScreen", "View")));
        }

        [Test]
        public void GetEntitiesAccessibleByUser()
        {
            const String testUser = "user1";
            var testEntittTypesAndEntities = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientB"),
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetEntitiesAccessibleByUser(testUser).Returns(testEntittTypesAndEntities);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetEntitiesAccessibleByUser(testUser));

            mockUserQueryProcessor.Received(1).GetEntitiesAccessibleByUser(testUser);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "ClientA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "ClientB")));
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload()
        {
            const String testUser = "user1";
            const String testEntityType = "ClientAccount";
            var testEntities = new HashSet<String>() { "ClientA", "ClientB" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetEntitiesAccessibleByUser(testUser, testEntityType).Returns(testEntities);

            var result = new HashSet<String>(testAccessManagerClient.GetEntitiesAccessibleByUser(testUser, testEntityType));

            mockUserQueryProcessor.Received(1).GetEntitiesAccessibleByUser(testUser, testEntityType);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("ClientA"));
            Assert.IsTrue(result.Contains("ClientB"));
        }

        [Test]
        public void GetEntitiesAccessibleByGroup()
        {
            const String testGroup = "group1";
            var testEntittTypesAndEntities = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientC"),
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetEntitiesAccessibleByGroup(testGroup).Returns(testEntittTypesAndEntities);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetEntitiesAccessibleByGroup(testGroup));

            mockGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroup(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "ClientA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "ClientC")));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload()
        {
            const String testGroup = "group1";
            const String testEntityType = "ClientAccount";
            var testEntities = new HashSet<String>() { "ClientA", "ClientD" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetEntitiesAccessibleByGroup(testGroup, testEntityType).Returns(testEntities);

            var result = new HashSet<String>(testAccessManagerClient.GetEntitiesAccessibleByGroup(testGroup, testEntityType));

            mockGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroup(testGroup, testEntityType);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("ClientA"));
            Assert.IsTrue(result.Contains("ClientD"));
        }

        [Test]
        public void GetMethodReturningEmptyEnumerable()
        {
            const String testGroup = "group1";
            var testApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>();
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup).Returns(testApplicationComponentsAndAccessLevels);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetGroupToApplicationComponentAndAccessLevelMappings(testGroup));

            mockGroupQueryProcessor.Received(1).GetGroupToApplicationComponentAndAccessLevelMappings(testGroup);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(0, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(0, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetMethodReturningEmptyList()
        {
            var entityTypes = new List<String>();
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.EntityTypes.Returns(entityTypes);

            var result = new List<String>(testAccessManagerClient.EntityTypes);

            var throwAway = mockEntityQueryProcessor.Received(1).EntityTypes;
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetMethodReturningEmptyHashSet()
        {
            const String testUser = "user1";
            var testGroups = new HashSet<String>();
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToGroupMappings(testUser, false).Returns(testGroups);

            HashSet<String> result = testAccessManagerClient.GetUserToGroupMappings(testUser, false);

            mockUserQueryProcessor.Received(1).GetUserToGroupMappings(testUser, false);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(0, groupStringifier.FromStringCallCount);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ExceptionOnServerSideIsRethrown()
        {
            // Have to make the group name here something that's unique amongst all the tests... since mock*Processor classes remain between tests, and NSubstitute doesn't seem to offer a way to clear When() declarations on mocks 
            const String testFromGroup = "group99";
            const String exceptionMessage = "Group 'group99' does not exist.";
            const String parameterName = "group";
            var mockException = new ArgumentException(exceptionMessage, parameterName);
            mockGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockGroupToGroupQueryProcessor.When((processor) => processor.GetGroupToGroupMappings(testFromGroup, false)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerClient.GetGroupToGroupMappings(testFromGroup, false);
            });

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(testFromGroup, false);
            Assert.That(e.Message, Does.StartWith(exceptionMessage));
        }

        [Test]
        public void RetryOnHttpRequestException()
        {
            using (var testClient = new HttpClient())
            {
                testBaseUrl = new Uri("http://www.acd8aac2-cb88-4296-b604-285f6132e449.com/");
                testAccessManagerClient = new AccessManagerClient<String, String, String, String>
                (
                    testBaseUrl,
                    testClient,
                    userStringifier,
                    groupStringifier,
                    applicationComponentStringifier,
                    accessLevelStringifier,
                    5,
                    1,
                    mockLogger,
                    mockMetricLogger
                );
                const String testEntityType = "BusinessUnit";
                mockEntityEventProcessor.ClearReceivedCalls();

                var e = Assert.Throws<HttpRequestException>(delegate
                {
                    testAccessManagerClient.RemoveEntityType(testEntityType);
                });

                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 1 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 2 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 3 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 4 of 5).", Arg.Any<HttpRequestException>());
                mockLogger.Received(1).Log(testAccessManagerClient, LogLevel.Warning, "Exception occurred when sending HTTP request.  Retrying in 1 seconds (retry 5 of 5).", Arg.Any<HttpRequestException>());
                mockMetricLogger.Received(5).Increment(Arg.Any<HttpRequestRetried>());
            }
        }

        #region Nested Classes

        /// <summary>
        /// Implementation of <see cref="IUniqueStringifier{T}"/> which counts the number of calls to the FromString() and ToString() methods.
        /// </summary>
        private class MethodCallCountingStringUniqueStringifier : IUniqueStringifier<String>
        {
            public Int32 FromStringCallCount { get; protected set; }
            public Int32 ToStringCallCount { get; protected set; }

            public MethodCallCountingStringUniqueStringifier()
            {
                FromStringCallCount = 0;
                ToStringCallCount = 0;
            }

            /// <inheritdoc/>
            public String FromString(String stringifiedObject)
            {
                FromStringCallCount++;

                return stringifiedObject;
            }

            /// <inheritdoc/>
            public String ToString(String inputObject)
            {
                ToStringCallCount++;

                return inputObject;
            }
        }

        #endregion
    }
}
