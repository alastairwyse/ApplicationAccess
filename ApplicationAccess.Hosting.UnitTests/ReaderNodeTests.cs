﻿/*
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
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.ReaderNode class.
    /// </summary>
    public class ReaderNodeTests
    {
        private AccessManagerState returnedLoadState;
        private IReaderNodeRefreshStrategy mockRefreshStrategy;
        private IAccessManagerTemporalEventQueryProcessor<String, String, ApplicationScreen, AccessLevel> mockEventCache;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private IMetricLogger mockMetricLogger;
        private IDateTimeProvider mockDateTimeProvider;
        private ReaderNode<String, String, ApplicationScreen, AccessLevel> testReaderNode;

        [SetUp]
        protected void SetUp()
        {
            returnedLoadState = new AccessManagerState(Guid.Parse("5555795a-6408-4084-aa86-a70f8731376a"), CreateDataTimeFromString("2022-10-06 19:27:01"), 0);
            mockRefreshStrategy = Substitute.For<IReaderNodeRefreshStrategy>();
            mockEventCache = Substitute.For<IAccessManagerTemporalEventQueryProcessor<String, String, ApplicationScreen, AccessLevel>>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);
        }

        [TearDown]
        public void TearDown()
        {
            testReaderNode.Dispose();
        }

        [Test]
        public void Constructor_MetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            ReaderNode<String, String, ApplicationScreen, AccessLevel> testReaderNode;
            var fieldNamePath = new List<String>() { "metricLogger" };
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader);

            NonPublicFieldAssert.IsOfType<ApplicationMetrics.MetricLoggers.NullMetricLogger>(fieldNamePath, testReaderNode);


            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MetricLoggerBaseTypeInclusionFilter>(fieldNamePath, testReaderNode);


            fieldNamePath = new List<String>() { "metricLogger", "filteredMetricLogger" };
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderNode, true);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetFalseAndCallToPersistentReaderFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Failed to load.");
            mockMetricLogger.Begin(Arg.Any<ReaderNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testReaderNode.Load(false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ReaderNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetTrueAndCallToPersistentReaderFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Failed to load.");
            mockMetricLogger.Begin(Arg.Any<ReaderNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testReaderNode.Load(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ReaderNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetTrueAndPersistentStorageEmpty()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new PersistentStorageEmptyException("Persistent storage is empty.");
            mockMetricLogger.Begin(Arg.Any<ReaderNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testReaderNode.Load(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ReaderNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetFalseAndPersistentStorageEmpty()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new PersistentStorageEmptyException("Persistent storage is empty.");
            mockMetricLogger.Begin(Arg.Any<ReaderNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            testReaderNode.Load(false);

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderNodeLoadTime>());
            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ReaderNodeLoadTime>());
        }

        [Test]
        public void Users()
        {
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.Users);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user, result[0]);
        }

        [Test]
        public void Groups()
        {
            const String group = "group1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.Groups);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(group, result[0]);
        }

        [Test]
        public void EntityTypes()
        {
            const String entityType = "Clients";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddEntityType(entityType);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.EntityTypes);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entityType, result[0]);
        }

        [Test]
        public void ContainsUser()
        {
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = testReaderNode.ContainsUser(user);

            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsGroup()
        {
            const String group = "group1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = testReaderNode.ContainsGroup(group);

            Assert.IsTrue(result);
        }

        [Test]
        public void GetUserToGroupMappings()
        {
            const String user = "user1";
            const String group = "group1";
            const String indirectGroup = "group2";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddGroup(group);
                accessManager.AddGroup(indirectGroup);
                accessManager.AddUserToGroupMapping(user, group);
                accessManager.AddGroupToGroupMapping(group, indirectGroup);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetUserToGroupMappings(user, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(group, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetUserToGroupMappings(user, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(group, result);
            Assert.Contains(indirectGroup, result);
        }

        [Test]
        public void GetGroupToUserMappings()
        {
            const String user1 = "user1";
            const String user2 = "user2";
            const String group = "group1";
            const String indirectGroup = "group2";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user1);
                accessManager.AddUser(user2);
                accessManager.AddGroup(group);
                accessManager.AddGroup(indirectGroup);
                accessManager.AddUserToGroupMapping(user1, group);
                accessManager.AddUserToGroupMapping(user2, indirectGroup);
                accessManager.AddGroupToGroupMapping(group, indirectGroup);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetGroupToUserMappings(indirectGroup, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user2, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetGroupToUserMappings(indirectGroup, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(user1, result);
            Assert.Contains(user2, result);
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            const String indirectGroup = "group3";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(fromGroup);
                accessManager.AddGroup(toGroup);
                accessManager.AddGroup(indirectGroup);
                accessManager.AddGroupToGroupMapping(fromGroup, toGroup);
                accessManager.AddGroupToGroupMapping(toGroup, indirectGroup);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetGroupToGroupMappings(fromGroup, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(toGroup, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetGroupToGroupMappings(fromGroup, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(toGroup, result);
            Assert.Contains(indirectGroup, result);
        }

        [Test]
        public void GetGroupToGroupReverseMappings()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            const String indirectGroup = "group3";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(fromGroup);
                accessManager.AddGroup(toGroup);
                accessManager.AddGroup(indirectGroup);
                accessManager.AddGroupToGroupMapping(fromGroup, toGroup);
                accessManager.AddGroupToGroupMapping(toGroup, indirectGroup);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetGroupToGroupReverseMappings(indirectGroup, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(toGroup, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetGroupToGroupReverseMappings(indirectGroup, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(fromGroup, result);
            Assert.Contains(toGroup, result);
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings()
        {
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.ManageProducts, AccessLevel.Delete);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetUserToApplicationComponentAndAccessLevelMappings(user));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.ManageProducts, result[0].Item1);
            Assert.AreEqual(AccessLevel.Delete, result[0].Item2);
        }


        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappings()
        {
            const String user1 = "user1";
            const String user2 = "user2";
            const String group = "group1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user1);
                accessManager.AddUser(user2);
                accessManager.AddGroup(group);
                accessManager.AddUserToGroupMapping(user2, group);
                accessManager.AddUserToApplicationComponentAndAccessLevelMapping(user1, ApplicationScreen.ManageProducts, AccessLevel.Create);
                accessManager.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.ManageProducts, AccessLevel.Create);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user1, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetApplicationComponentAndAccessLevelToUserMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(user1, result);
            Assert.Contains(user2, result);
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings()
        {
            const String group = "group1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
                accessManager.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.ManageProducts, AccessLevel.Modify);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetGroupToApplicationComponentAndAccessLevelMappings(group));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.ManageProducts, result[0].Item1);
            Assert.AreEqual(AccessLevel.Modify, result[0].Item2);
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappings()
        {
            const String group1 = "group1";
            const String group2 = "group2";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group1);
                accessManager.AddGroup(group2);
                accessManager.AddGroupToGroupMapping(group1, group2);
                accessManager.AddGroupToApplicationComponentAndAccessLevelMapping(group2, ApplicationScreen.ManageProducts, AccessLevel.Create);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(group2, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetApplicationComponentAndAccessLevelToGroupMappings(ApplicationScreen.ManageProducts, AccessLevel.Create, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(group1, result);
            Assert.Contains(group2, result);
        }

        [Test]
        public void ContainsEntityType()
        {
            const String entityType = "ClientAccount";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddEntityType(entityType);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = testReaderNode.ContainsEntityType(entityType);

            Assert.IsTrue(result);
        }

        [Test]
        public void GetEntities()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetEntities(entityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void ContainsEntity()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = testReaderNode.ContainsEntity(entityType, entity);

            Assert.IsTrue(result);
        }

        [Test]
        public void GetUserToEntityMappingsUserOverload()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddUserToEntityMapping(user, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<Tuple<String, String>>(testReaderNode.GetUserToEntityMappings(user));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entityType, result[0].Item1);
            Assert.AreEqual(entity, result[0].Item2);
        }

        [Test]
        public void GetUserToEntityMappingsUserAndEntityTypeOverload()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddUserToEntityMapping(user, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetUserToEntityMappings(user, entityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetEntityToUserMappings()
        {
            const String user1 = "user1";
            const String user2 = "user2";
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user1);
                accessManager.AddUser(user2);
                accessManager.AddGroup(group);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddUserToGroupMapping(user2, group);
                accessManager.AddUserToEntityMapping(user1, entityType, entity);
                accessManager.AddGroupToEntityMapping(group, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetEntityToUserMappings(entityType, entity, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user1, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetEntityToUserMappings(entityType, entity, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(user1, result);
            Assert.Contains(user2, result);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupOverload()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddGroupToEntityMapping(group, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<Tuple<String, String>>(testReaderNode.GetGroupToEntityMappings(group));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entityType, result[0].Item1);
            Assert.AreEqual(entity, result[0].Item2);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupAndEntityTypeOverload()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddGroupToEntityMapping(group, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetGroupToEntityMappings(group, entityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetEntityToGroupMappings()
        {
            const String group1 = "group1";
            const String group2 = "group2";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group1);
                accessManager.AddGroup(group2);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddGroupToGroupMapping(group1, group2);
                accessManager.AddGroupToEntityMapping(group2, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetEntityToGroupMappings(entityType, entity, false));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(group2, result[0]);


            mockRefreshStrategy.ClearReceivedCalls();

            result = new List<String>(testReaderNode.GetEntityToGroupMappings(entityType, entity, true));

            Assert.AreEqual(2, result.Count);
            Assert.Contains(group1, result);
            Assert.Contains(group2, result);
        }

        [Test]
        public void HasAccessToApplicationComponent()
        {
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Settings, AccessLevel.Modify);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = testReaderNode.HasAccessToApplicationComponent(user, ApplicationScreen.Settings, AccessLevel.Modify);

            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToEntity()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddUserToEntityMapping(user, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = testReaderNode.HasAccessToEntity(user, entityType, entity);

            Assert.IsTrue(result);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser()
        {
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetApplicationComponentsAccessibleByUser(user));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.Summary, result[0].Item1);
            Assert.AreEqual(AccessLevel.View, result[0].Item2);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup()
        {
            const String group = "group1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
                accessManager.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Summary, AccessLevel.Modify);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetApplicationComponentsAccessibleByGroup(group));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.Summary, result[0].Item1);
            Assert.AreEqual(AccessLevel.Modify, result[0].Item2);
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserOverload()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddUserToEntityMapping(user, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            HashSet<Tuple<String, String>> result = testReaderNode.GetEntitiesAccessibleByUser(user);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Tuple<String, String>(entityType, entity), result.ElementAt(0));
        }

        [Test]
        public void GetEntitiesAccessibleByUserUserAndEntityTypeOverload()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddUserToEntityMapping(user, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetEntitiesAccessibleByUser(user, entityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupOverload()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddGroupToEntityMapping(group, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            HashSet<Tuple<String, String>> result = testReaderNode.GetEntitiesAccessibleByGroup(group);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Tuple<String, String>(entityType, entity), result.ElementAt(0));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupGroupAndEntityTypeOverload()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(group);
                accessManager.AddEntityType(entityType);
                accessManager.AddEntity(entityType, entity);
                accessManager.AddGroupToEntityMapping(group, entityType, entity);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load(true);

            var result = new List<String>(testReaderNode.GetEntitiesAccessibleByGroup(group, entityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void Refresh_LoadFails()
        {
            var mockException = new Exception("Failed to load.");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockMetricLogger.Begin(Arg.Any<RefreshTime>()).Returns(testBeginId);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns
            (
                // Return successfully from the initial Load() call
                callnfo => { return returnedLoadState; }, 
                // Fail the call which occurs as part of the Refresh() call
                callnfo => { throw mockException; }
            );
            mockEventCache.When((eventCache) => eventCache.GetAllEventsSince(returnedLoadState.EventId)).Do((callInfo) => throw new EventNotCachedException("The specified event was not cached."));
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger); 
            testReaderNode.Load(true);

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<RefreshTime>());
            mockPersistentReader.Received(2).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.EventId);
            mockMetricLogger.Received(1).Increment(Arg.Any<CacheMiss>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<RefreshTime>());
            Assert.That(e.Message, Does.StartWith("Failed to refresh the entire contents of the reader node."));
            Assert.IsInstanceOf<Exception>(e.InnerException);
            Assert.That(e.InnerException.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void Refresh_CacheDoesntContainLatestEventId()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockMetricLogger.Begin(Arg.Any<RefreshTime>()).Returns(testBeginId);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns(returnedLoadState);
            mockEventCache.When((eventCache) => eventCache.GetAllEventsSince(returnedLoadState.EventId)).Do((callInfo) => throw new EventNotCachedException("The specified event was not cached."));
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);
            testReaderNode.Load(true);
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
            });
            var refreshEventId = Guid.Parse("cc902edc-d46a-4ce4-8e4d-02db408cd2dc");
            DateTime refreshEventOccurredTime = CreateDataTimeFromString("2022-10-08 10:53:02");
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(new AccessManagerState(refreshEventId, refreshEventOccurredTime, 0));

            capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);

            mockMetricLogger.Received(1).Begin(Arg.Any<RefreshTime>());
            mockPersistentReader.Received(2).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.EventId);
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<RefreshTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<CacheMiss>());
            var result = new List<String>(testReaderNode.Users);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user, result[0]);
        }

        [Test]
        public void Refresh_CallToEventCacheFails()
        {
            var mockException = new Exception("Failure to retrieve event from cache.");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockMetricLogger.Begin(Arg.Any<RefreshTime>()).Returns(testBeginId);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns(returnedLoadState);
            mockEventCache.When((eventCache) => eventCache.GetAllEventsSince(returnedLoadState.EventId)).Do((callInfo) => throw mockException);
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);
            testReaderNode.Load(true);

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<RefreshTime>());
            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.EventId);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<RefreshTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve latest access manager events following event '{returnedLoadState.EventId}' from cache."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Refresh_EventCacheIsEmpty()
        {
            var mockException = new EventCacheEmptyException("The event cache is empty.");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var testUpdateEvents = new List<TemporalEventBufferItemBase>();
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockMetricLogger.Begin(Arg.Any<RefreshTime>()).Returns(testBeginId);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns(returnedLoadState);
            mockEventCache.When((eventCache) => eventCache.GetAllEventsSince(returnedLoadState.EventId)).Do((callInfo) => throw mockException);
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger);
            testReaderNode.Load(true);

            capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);

            mockMetricLogger.Received(1).Begin(Arg.Any<RefreshTime>());
            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.EventId);
            mockMetricLogger.Received(1).Increment(Arg.Any<EventCacheEmpty>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<RefreshTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<RefreshOperationCompleted>());
        }

        [Test]
        public void Refresh()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var userEventId = Guid.Parse("625e7ff0-429a-40c8-a5d6-5c4457981522");
            DateTime userEventOccurredTime = CreateDataTimeFromString("2022-10-08 11:17:35");
            Int32 userEventHashCode = 1;
            var entityTypeEventId = Guid.Parse("aca8565a-2d95-471e-8fe1-51d27024f793");
            DateTime entityTypeEventOccurredTime = CreateDataTimeFromString("2022-10-08 11:17:59");
            Int32 entityTypeEventHashCode = 2;
            var testUpdateEvents = new List<TemporalEventBufferItemBase>()
            { 
                new UserEventBufferItem<String>(userEventId, EventAction.Add, user, userEventOccurredTime, userEventHashCode), 
                new EntityTypeEventBufferItem(entityTypeEventId, EventAction.Add, entityType, entityTypeEventOccurredTime, entityTypeEventHashCode)
            };
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockMetricLogger.Begin(Arg.Any<RefreshTime>()).Returns(testBeginId);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns(returnedLoadState);
            mockEventCache.GetAllEventsSince(returnedLoadState.EventId).Returns(testUpdateEvents);
            mockDateTimeProvider.UtcNow().Returns
            (
                CreateDataTimeFromString("2022-10-08 11:17:36"),
                CreateDataTimeFromString("2022-10-08 11:18:01")
            );
            testReaderNode = new ReaderNode<String, String, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader, mockMetricLogger, mockDateTimeProvider);
            testReaderNode.Load(true);

            capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);

            mockMetricLogger.Received(1).Begin(Arg.Any<RefreshTime>());
            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.EventId);
            mockMetricLogger.Received(1).Add(Arg.Any<CachedEventsReceived>(), 2);
            mockMetricLogger.Received(1).Add(Arg.Any<EventProcessingDelay>(), 1000);
            mockMetricLogger.Received(1).Add(Arg.Any<EventProcessingDelay>(), 2000);
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<RefreshTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<RefreshOperationCompleted>());
            var result = new List<String>(testReaderNode.Users);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user, result[0]);
            result = new List<String>(testReaderNode.EntityTypes);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entityType, result[0]);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion
    }
}
