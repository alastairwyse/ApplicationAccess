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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Mvc.Testing;
using ApplicationAccess.Hosting.Rest.AsyncClient;
using ApplicationAccess.Hosting.Rest.Reader.IntegrationTests;
using ApplicationAccess.UnitTests;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;
using Polly;

namespace ApplicationAccess.Hosting.Rest.Client.IntegrationTests
{
    /// <summary>
    /// Integration tests for query methods in the ApplicationAccess.Hosting.Rest.Client.AccessManagerClient class.
    /// </summary>
    public class AccessManagerClientQueryTests : ReaderIntegrationTestsBase
    {
        private const String urlReservedCharcters = "! * ' ( ) ; : @ & = + $ , / ? % # [ ]";

        private Uri testBaseUrl;
        private MethodCallCountingStringUniqueStringifier userStringifier;
        private MethodCallCountingStringUniqueStringifier groupStringifier;
        private MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        private MethodCallCountingStringUniqueStringifier accessLevelStringifier;
        private IApplicationLogger mockLogger;
        private IMetricLogger mockMetricLogger;
        private IAccessManagerQueryProcessor<String, String, String, String> testAccessManagerClient;

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
            testBaseUrl = client.BaseAddress;
            userStringifier = new MethodCallCountingStringUniqueStringifier();
            groupStringifier = new MethodCallCountingStringUniqueStringifier();
            applicationComponentStringifier = new MethodCallCountingStringUniqueStringifier();
            accessLevelStringifier = new MethodCallCountingStringUniqueStringifier();
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerClient = new TestAccessManagerClient<String, String, String, String>
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
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.ClearReceivedCalls();
        }

        [TearDown]
        protected void TearDown()
        {
            ((IDisposable)testAccessManagerClient).Dispose();
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
        public void ContainsUser_UrlEncoding()
        {
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.ContainsUser(urlReservedCharcters).Returns(true);

            Boolean result = testAccessManagerClient.ContainsUser(urlReservedCharcters);

            mockUserQueryProcessor.Received(1).ContainsUser(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.IsTrue(result);
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
        public void ContainsGroup_UrlEncoding()
        {
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.ContainsGroup(urlReservedCharcters).Returns(true);

            Boolean result = testAccessManagerClient.ContainsGroup(urlReservedCharcters);

            mockGroupQueryProcessor.Received(1).ContainsGroup(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.IsTrue(result);
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
        public void GetUserToGroupMappings_UrlEncoding()
        {
            var testGroups = new HashSet<String>() { "group1", "group2", "group3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToGroupMappings(urlReservedCharcters, false).Returns(testGroups);

            HashSet<String> result = testAccessManagerClient.GetUserToGroupMappings(urlReservedCharcters, false);

            mockUserQueryProcessor.Received(1).GetUserToGroupMappings(urlReservedCharcters, false);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void GetGroupToUserMappings()
        {
            const String testGroup = "group1";
            var testUsers = new HashSet<String>() { "user1", "user2", "user3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetGroupToUserMappings(testGroup, false).Returns(testUsers);

            HashSet<String> result = testAccessManagerClient.GetGroupToUserMappings(testGroup, false);

            mockUserQueryProcessor.Received(1).GetGroupToUserMappings(testGroup, false);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));


            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetGroupToUserMappings(testGroup, true).Returns(testUsers);
            groupStringifier.Reset();
            userStringifier.Reset();

            result = testAccessManagerClient.GetGroupToUserMappings(testGroup, true);

            mockUserQueryProcessor.Received(1).GetGroupToUserMappings(testGroup, true);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public void GetGroupToUserMappings_UrlEncoding()
        {
            var testUsers = new HashSet<String>() { "user1", "user2", "user3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetGroupToUserMappings(urlReservedCharcters, false).Returns(testUsers);

            HashSet<String> result = testAccessManagerClient.GetGroupToUserMappings(urlReservedCharcters, false);

            mockUserQueryProcessor.Received(1).GetGroupToUserMappings(urlReservedCharcters, false);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetGroupToGroupMappings_UrlEncoding()
        {
            var testToGroups = new HashSet<String>() { "group2", "group3", "group4" };
            mockGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockGroupToGroupQueryProcessor.GetGroupToGroupMappings(urlReservedCharcters, false).Returns(testToGroups);

            HashSet<String> result = testAccessManagerClient.GetGroupToGroupMappings(urlReservedCharcters, false);

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupMappings(urlReservedCharcters, false);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void GetGroupToGroupReverseMappings()
        {
            const String testGroup = "group1";
            var testReturnGroups = new HashSet<String>() { "group2", "group3", "group4" };
            mockGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockGroupToGroupQueryProcessor.GetGroupToGroupReverseMappings(testGroup, false).Returns(testReturnGroups);

            HashSet<String> result = testAccessManagerClient.GetGroupToGroupReverseMappings(testGroup, false);

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupReverseMappings(testGroup, false);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));


            mockGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockGroupToGroupQueryProcessor.GetGroupToGroupReverseMappings(testGroup, true).Returns(testReturnGroups);
            groupStringifier.Reset();
            userStringifier.Reset();

            result = testAccessManagerClient.GetGroupToGroupReverseMappings(testGroup, true);

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupReverseMappings(testGroup, true);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            Assert.IsTrue(result.Contains("group4"));
        }

        [Test]
        public void GetGroupToGroupReverseMappings_UrlEncoding()
        {
            var testReturnGroups = new HashSet<String>() { "group2", "group3", "group4" };
            mockGroupToGroupQueryProcessor.ClearReceivedCalls();
            mockGroupToGroupQueryProcessor.GetGroupToGroupReverseMappings(urlReservedCharcters, false).Returns(testReturnGroups);

            HashSet<String> result = testAccessManagerClient.GetGroupToGroupReverseMappings(urlReservedCharcters, false);

            mockGroupToGroupQueryProcessor.Received(1).GetGroupToGroupReverseMappings(urlReservedCharcters, false);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetUserToApplicationComponentAndAccessLevelMappings_UrlEncoding()
        {
            var testApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ManageProductsScreen", "Modify"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(urlReservedCharcters).Returns(testApplicationComponentsAndAccessLevels);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetUserToApplicationComponentAndAccessLevelMappings(urlReservedCharcters));

            mockUserQueryProcessor.Received(1).GetUserToApplicationComponentAndAccessLevelMappings(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappings()
        {
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            var testUsers = new HashSet<String>() { "user1", "user2", "user3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetApplicationComponentAndAccessLevelToUserMappings(testApplicationComponent, testAccessLevel, false).Returns(testUsers);

            var result = new List<String>(testAccessManagerClient.GetApplicationComponentAndAccessLevelToUserMappings(testApplicationComponent, testAccessLevel, false));

            mockUserQueryProcessor.Received(1).GetApplicationComponentAndAccessLevelToUserMappings(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));


            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetApplicationComponentAndAccessLevelToUserMappings(testApplicationComponent, testAccessLevel, true).Returns(testUsers);
            applicationComponentStringifier.Reset();
            accessLevelStringifier.Reset();
            userStringifier.Reset();

            result = new List<String>(testAccessManagerClient.GetApplicationComponentAndAccessLevelToUserMappings(testApplicationComponent, testAccessLevel, true));

            mockUserQueryProcessor.Received(1).GetApplicationComponentAndAccessLevelToUserMappings(testApplicationComponent, testAccessLevel, true);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappings_UrlEncoding()
        {
            var testUsers = new HashSet<String>() { "user1", "user2", "user3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetApplicationComponentAndAccessLevelToUserMappings(urlReservedCharcters, urlReservedCharcters, false).Returns(testUsers);

            var result = new List<String>(testAccessManagerClient.GetApplicationComponentAndAccessLevelToUserMappings(urlReservedCharcters, urlReservedCharcters, false));

            mockUserQueryProcessor.Received(1).GetApplicationComponentAndAccessLevelToUserMappings(urlReservedCharcters, urlReservedCharcters, false);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetGroupToApplicationComponentAndAccessLevelMappings_UrlEncoding()
        {
            var testApplicationComponentsAndAccessLevels = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("ManageProductsScreen", "Create"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(urlReservedCharcters).Returns(testApplicationComponentsAndAccessLevels);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetGroupToApplicationComponentAndAccessLevelMappings(urlReservedCharcters));

            mockGroupQueryProcessor.Received(1).GetGroupToApplicationComponentAndAccessLevelMappings(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappings()
        {
            const String testApplicationComponent = "ManageProductsScreen";
            const String testAccessLevel = "View";
            var testGroups = new HashSet<String>() { "group1", "group2", "group3" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetApplicationComponentAndAccessLevelToGroupMappings(testApplicationComponent, testAccessLevel, false).Returns(testGroups);

            var result = new List<String>(testAccessManagerClient.GetApplicationComponentAndAccessLevelToGroupMappings(testApplicationComponent, testAccessLevel, false));

            mockGroupQueryProcessor.Received(1).GetApplicationComponentAndAccessLevelToGroupMappings(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));


            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetApplicationComponentAndAccessLevelToGroupMappings(testApplicationComponent, testAccessLevel, true).Returns(testGroups);
            applicationComponentStringifier.Reset();
            accessLevelStringifier.Reset();
            groupStringifier.Reset();

            result = new List<String>(testAccessManagerClient.GetApplicationComponentAndAccessLevelToGroupMappings(testApplicationComponent, testAccessLevel, true));

            mockGroupQueryProcessor.Received(1).GetApplicationComponentAndAccessLevelToGroupMappings(testApplicationComponent, testAccessLevel, true);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappings_UrlEncoding()
        {
            var testGroups = new HashSet<String>() { "group1", "group2", "group3" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetApplicationComponentAndAccessLevelToGroupMappings(urlReservedCharcters, urlReservedCharcters, false).Returns(testGroups);

            var result = new List<String>(testAccessManagerClient.GetApplicationComponentAndAccessLevelToGroupMappings(urlReservedCharcters, urlReservedCharcters, false));

            mockGroupQueryProcessor.Received(1).GetApplicationComponentAndAccessLevelToGroupMappings(urlReservedCharcters, urlReservedCharcters, false);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void ContainsEntityType_UrlEncoding()
        {
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.ContainsEntityType(urlReservedCharcters).Returns(true);

            Boolean result = testAccessManagerClient.ContainsEntityType(urlReservedCharcters);

            mockEntityQueryProcessor.Received(1).ContainsEntityType(urlReservedCharcters);
            Assert.IsTrue(result);
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
        public void GetEntities_UrlEncoding()
        {
            var testEntitiess = new List<String>() { "ClientA", "ClientB", "ClientC" };
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.GetEntities(urlReservedCharcters).Returns(testEntitiess);

            var result = new List<String>(testAccessManagerClient.GetEntities(urlReservedCharcters));

            mockEntityQueryProcessor.Received(1).GetEntities(urlReservedCharcters);
            Assert.AreEqual(3, result.Count);
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
        public void ContainsEntity_UrlEncoding()
        {
            mockEntityQueryProcessor.ClearReceivedCalls();
            mockEntityQueryProcessor.ContainsEntity(urlReservedCharcters, urlReservedCharcters).Returns(true);

            Boolean result = testAccessManagerClient.ContainsEntity(urlReservedCharcters, urlReservedCharcters);

            mockEntityQueryProcessor.Received(1).ContainsEntity(urlReservedCharcters, urlReservedCharcters);
            Assert.IsTrue(result);
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
        public void GetUserToEntityMappings_UrlEncoding()
        {
            var testEntittTypesAndEntities = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientB"),
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToEntityMappings(urlReservedCharcters).Returns(testEntittTypesAndEntities);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetUserToEntityMappings(urlReservedCharcters));

            mockUserQueryProcessor.Received(1).GetUserToEntityMappings(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetUserToEntityMappingsUserAndEntityTypeOverload_UrlEncoding()
        {
            var testEntities = new List<String>() { "ClientA", "ClientB" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetUserToEntityMappings(urlReservedCharcters, urlReservedCharcters).Returns(testEntities);

            var result = new List<String>(testAccessManagerClient.GetUserToEntityMappings(urlReservedCharcters, urlReservedCharcters));

            mockUserQueryProcessor.Received(1).GetUserToEntityMappings(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetEntityToUserMappings()
        {
            const String testEntityType = "ClientAccount";
            const String testEntity = "ClientA";
            var testUsers = new HashSet<String>() { "user1", "user2", "user3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetEntityToUserMappings(testEntityType, testEntity, false).Returns(testUsers);

            var result = new List<String>(testAccessManagerClient.GetEntityToUserMappings(testEntityType, testEntity, false));

            mockUserQueryProcessor.Received(1).GetEntityToUserMappings(testEntityType, testEntity, false);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));


            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetEntityToUserMappings(testEntityType, testEntity, true).Returns(testUsers);
            userStringifier.Reset();

            result = new List<String>(testAccessManagerClient.GetEntityToUserMappings(testEntityType, testEntity, true));

            mockUserQueryProcessor.Received(1).GetEntityToUserMappings(testEntityType, testEntity, true);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
        }

        [Test]
        public void GetEntityToUserMappings_UrlEncoding()
        {
            var testUsers = new HashSet<String>() { "user1", "user2", "user3" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetEntityToUserMappings(urlReservedCharcters, urlReservedCharcters, false).Returns(testUsers);

            var result = new List<String>(testAccessManagerClient.GetEntityToUserMappings(urlReservedCharcters, urlReservedCharcters, false));

            mockUserQueryProcessor.Received(1).GetEntityToUserMappings(urlReservedCharcters, urlReservedCharcters, false);
            Assert.AreEqual(3, userStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetGroupToEntityMappings_UrlEncoding()
        {
            var testEntittTypesAndEntities = new List<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientB"),
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetGroupToEntityMappings(urlReservedCharcters).Returns(testEntittTypesAndEntities);

            var result = new List<Tuple<String, String>>(testAccessManagerClient.GetGroupToEntityMappings(urlReservedCharcters));

            mockGroupQueryProcessor.Received(1).GetGroupToEntityMappings(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetGroupToEntityMappingsUserAndEntityTypeOverload_UrlEncoding()
        {
            var testEntities = new List<String>() { "ClientA", "ClientB" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetGroupToEntityMappings(urlReservedCharcters, urlReservedCharcters).Returns(testEntities);

            var result = new List<String>(testAccessManagerClient.GetGroupToEntityMappings(urlReservedCharcters, urlReservedCharcters));

            mockGroupQueryProcessor.Received(1).GetGroupToEntityMappings(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetEntityToGroupMappings()
        {
            const String testEntityType = "ClientAccount";
            const String testEntity = "ClientA";
            var testGroups = new HashSet<String>() { "group1", "group2", "group3" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetEntityToGroupMappings(testEntityType, testEntity, false).Returns(testGroups);

            var result = new List<String>(testAccessManagerClient.GetEntityToGroupMappings(testEntityType, testEntity, false));

            mockGroupQueryProcessor.Received(1).GetEntityToGroupMappings(testEntityType, testEntity, false);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));


            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetEntityToGroupMappings(testEntityType, testEntity, true).Returns(testGroups);
            groupStringifier.Reset();

            result = new List<String>(testAccessManagerClient.GetEntityToGroupMappings(testEntityType, testEntity, true));

            mockGroupQueryProcessor.Received(1).GetEntityToGroupMappings(testEntityType, testEntity, true);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
        }

        [Test]
        public void GetEntityToGroupMappings_UrlEncoding()
        {
            var testGroups = new HashSet<String>() { "group1", "group2", "group3" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetEntityToGroupMappings(urlReservedCharcters, urlReservedCharcters, false).Returns(testGroups);

            var result = new List<String>(testAccessManagerClient.GetEntityToGroupMappings(urlReservedCharcters, urlReservedCharcters, false));

            mockGroupQueryProcessor.Received(1).GetEntityToGroupMappings(urlReservedCharcters, urlReservedCharcters, false);
            Assert.AreEqual(3, groupStringifier.FromStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void HasAccessToApplicationComponent_UrlEncoding()
        {
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.HasAccessToApplicationComponent(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters).Returns(true);

            Boolean result = testAccessManagerClient.HasAccessToApplicationComponent(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserQueryProcessor.Received(1).HasAccessToApplicationComponent(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
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
        public void HasAccessToEntity_UrlEncoding()
        {
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.HasAccessToEntity(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters).Returns(false);

            Boolean result = testAccessManagerClient.HasAccessToEntity(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);

            mockUserQueryProcessor.Received(1).HasAccessToEntity(urlReservedCharcters, urlReservedCharcters, urlReservedCharcters);
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
        public void GetApplicationComponentsAccessibleByUser_UrlEncoding()
        {
            var testApplicationComponentsAndAccessLevels = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("ManageProductsScreen", "Modify"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetApplicationComponentsAccessibleByUser(urlReservedCharcters).Returns(testApplicationComponentsAndAccessLevels);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetApplicationComponentsAccessibleByUser(urlReservedCharcters));

            mockUserQueryProcessor.Received(1).GetApplicationComponentsAccessibleByUser(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
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
        public void GetApplicationComponentsAccessibleByGroup_UrlEncoding()
        {
            var testApplicationComponentsAndAccessLevels = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("ManageProductsScreen", "Create"),
                new Tuple<String, String>("SummaryScreen", "View")
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetApplicationComponentsAccessibleByGroup(urlReservedCharcters).Returns(testApplicationComponentsAndAccessLevels);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetApplicationComponentsAccessibleByGroup(urlReservedCharcters));

            mockGroupQueryProcessor.Received(1).GetApplicationComponentsAccessibleByGroup(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, applicationComponentStringifier.FromStringCallCount);
            Assert.AreEqual(2, accessLevelStringifier.FromStringCallCount);
            Assert.AreEqual(2, result.Count);
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
        public void GetEntitiesAccessibleByUser_UrlEncoding()
        {
            var testEntittTypesAndEntities = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientB"),
            };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetEntitiesAccessibleByUser(urlReservedCharcters).Returns(testEntittTypesAndEntities);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetEntitiesAccessibleByUser(urlReservedCharcters));

            mockUserQueryProcessor.Received(1).GetEntitiesAccessibleByUser(urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload_UrlEncoding()
        {
            var testEntities = new HashSet<String>() { "ClientA", "ClientB" };
            mockUserQueryProcessor.ClearReceivedCalls();
            mockUserQueryProcessor.GetEntitiesAccessibleByUser(urlReservedCharcters, urlReservedCharcters).Returns(testEntities);

            var result = new HashSet<String>(testAccessManagerClient.GetEntitiesAccessibleByUser(urlReservedCharcters, urlReservedCharcters));

            mockUserQueryProcessor.Received(1).GetEntitiesAccessibleByUser(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
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
        public void GetEntitiesAccessibleByGroup_UrlEncoding()
        {
            var testEntittTypesAndEntities = new HashSet<Tuple<String, String>>()
            {
                new Tuple<String, String>("BusinessUnit", "Sales"),
                new Tuple<String, String>("ClientAccount", "ClientA"),
                new Tuple<String, String>("ClientAccount", "ClientC"),
            };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetEntitiesAccessibleByGroup(urlReservedCharcters).Returns(testEntittTypesAndEntities);

            var result = new HashSet<Tuple<String, String>>(testAccessManagerClient.GetEntitiesAccessibleByGroup(urlReservedCharcters));

            mockGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroup(urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(3, result.Count);
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
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload_UrlEncoding()
        {
            var testEntities = new HashSet<String>() { "ClientA", "ClientD" };
            mockGroupQueryProcessor.ClearReceivedCalls();
            mockGroupQueryProcessor.GetEntitiesAccessibleByGroup(urlReservedCharcters, urlReservedCharcters).Returns(testEntities);

            var result = new HashSet<String>(testAccessManagerClient.GetEntitiesAccessibleByGroup(urlReservedCharcters, urlReservedCharcters));

            mockGroupQueryProcessor.Received(1).GetEntitiesAccessibleByGroup(urlReservedCharcters, urlReservedCharcters);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(2, result.Count);
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
        
        #region Nested Classes

        /// <summary>
        /// Test version of the <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> class which overrides the SendRequest() method so the class can be tested synchronously using <see cref="WebApplicationFactory{TEntryPoint}"/>.
        /// </summary>
        /// <remarks>Testing the <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> class directly using <see cref="WebApplicationFactory{TEntryPoint}"/> resulted in error "The synchronous method is not supported by 'Microsoft.AspNetCore.TestHost.ClientHandler'".  Judging by the <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.clienthandler?view=aspnetcore-6.0"> documentation for the clienthandler class</see> (which I assume wraps HttpClient calls), it only supports a SendAsync() method.  Given support for the syncronous <see cref="HttpClient.Send(HttpRequestMessage)">HttpClient.Send()</see> was only ontroduced in .NET 5, I'm assuming this is yet to be supported by clients generated via the <see cref="WebApplicationFactory{TEntryPoint}.CreateClient">WebApplicationFactory.CreateClient()</see> method.  Hence, in order to test the class, this class overrides the SendRequest() method to call the HttpClient using the SendAsync() method and 'Result' property.  Although you wouldn't do this in released code (due to risk of deadlocks in certain run contexts outlined <see href="https://medium.com/rubrikkgroup/understanding-async-avoiding-deadlocks-e41f8f2c6f5d">here</see>, better to test the other functionality in the class (exception handling, response parsing, etc...) than not to test at all.</remarks>
        private class TestAccessManagerClient<TUser, TGroup, TComponent, TAccess> : AccessManagerClient<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval
            )
                : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
            /// <param name="retryInterval">The time in seconds between retries.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Int32 retryCount,
                Int32 retryInterval,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Client.IntegrationTests.AccessManagerClientTests+TestAccessManagerClient class.
            /// </summary>
            /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
            /// <param name="httpClient">The client to use to connect.</param>
            /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to <see cref="TUser"/> instances.</param>
            /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to <see cref="TGroup"/> instances.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to <see cref="TComponent"/> instances.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to <see cref="TAccess"/> instances.</param>
            /// <param name="exceptionHandingPolicy">Exception handling policy for HttpClient calls.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <remarks>When setting parameter 'exceptionHandingPolicy', note that the web API only returns non-success HTTP status errors in the case of persistent, and non-transient errors (e.g. 400 in the case of bad/malformed requests, and 500 in the case of critical server-side errors).  Retrying the same request after receiving these error statuses will result in an identical response, and hence these statuses are not passed to Polly and will be ignored if included as part of a transient exception handling policy.  Exposing of this parameter is designed to allow overriding of the retry policy and actions when encountering <see cref="HttpRequestException">HttpRequestExceptions</see> caused by network errors, etc.</remarks>
            public TestAccessManagerClient
            (
                Uri baseUrl,
                HttpClient httpClient,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Policy exceptionHandingPolicy,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base (baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, exceptionHandingPolicy, logger, metricLogger)
            {
            }

            /// <inheritdoc/>
            protected override void SendRequest(HttpMethod method, Uri requestUrl, Action<HttpMethod, Uri, HttpStatusCode, Stream> responseAction)
            {
                Action httpClientAction = () =>
                {
                    using (var request = new HttpRequestMessage(method, requestUrl))
                    try
                    {
                        using (var response = httpClient.SendAsync(request).Result)
                        {
                            responseAction.Invoke(method, requestUrl, response.StatusCode, response.Content.ReadAsStream());
                        }
                    }
                    catch (AggregateException ae)
                    {
                        // Since the SendAsync() method is used above, it will throw an AggregateException on failure which needs to be rethrown as its base exception to be able to properly test retries with the syncronous version of the Polly.Policy used by AccessManagerClient
                        ExceptionDispatchInfo.Capture(ae.GetBaseException()).Throw();
                    }
                };

                exceptionHandingPolicy.Execute(httpClientAction);
            }
        }

        #endregion
    }
}
