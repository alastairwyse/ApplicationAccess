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
using System.Globalization;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Persistence;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.ReaderNode class.
    /// </summary>
    public class ReaderNodeTests
    {
        private Tuple<Guid, DateTime> returnedLoadState;
        private IReaderNodeRefreshStrategy mockRefreshStrategy;
        private IAccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel> mockEventCache;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private ReaderNode<String, String, ApplicationScreen, AccessLevel> testReaderNode;

        [SetUp]
        protected void SetUp()
        {
            returnedLoadState = new Tuple<Guid, DateTime>(Guid.Parse("5555795a-6408-4084-aa86-a70f8731376a"), CreateDataTimeFromString("2022-10-06 19:27:01"));
            mockRefreshStrategy = Substitute.For<IReaderNodeRefreshStrategy>();
            mockEventCache = Substitute.For<IAccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            testReaderNode = new ReaderNode<string, string, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader);
        }

        [Test]
        public void Load_CallToPersisterFails()
        {
            var mockException = new Exception("Failed to load.");
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testReaderNode.Load();
            });

            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.Users);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user, result[0]);
        }

        [Test]
        public void Users_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.Users;
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.Groups);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(group, result[0]);
        }

        [Test]
        public void Groups_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.Groups;
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.EntityTypes);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entityType, result[0]);
        }

        [Test]
        public void EntityTypes_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.EntityTypes;
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = testReaderNode.ContainsUser(user);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsUser_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.ContainsUser("user1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = testReaderNode.ContainsGroup(group);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsGroup_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.ContainsGroup("group1");
            });

            Assert.AreEqual(mockException, e);
        }

        [Test]
        public void GetUserToGroupMappings()
        {
            const String user = "user1";
            const String group = "group1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
                accessManager.AddGroup(group);
                accessManager.AddUserToGroupMapping(user, group);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.GetUserToGroupMappings(user));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(group, result[0]);
        }

        [Test]
        public void GetUserToGroupMappings_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetUserToGroupMappings("user1");
            });

            Assert.AreEqual(mockException, e);
        }

        [Test]
        public void GetGroupToGroupMappings()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddGroup(fromGroup);
                accessManager.AddGroup(toGroup);
                accessManager.AddGroupToGroupMapping(fromGroup, toGroup);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.GetGroupToGroupMappings(fromGroup));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(toGroup, result[0]);
        }

        [Test]
        public void GetGroupToGroupMappings_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetGroupToGroupMappings("user1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetUserToApplicationComponentAndAccessLevelMappings(user));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.ManageProducts, result[0].Item1);
            Assert.AreEqual(AccessLevel.Delete, result[0].Item2);
        }

        [Test]
        public void GetUserToApplicationComponentAndAccessLevelMappings_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetUserToApplicationComponentAndAccessLevelMappings("user1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetGroupToApplicationComponentAndAccessLevelMappings(group));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.ManageProducts, result[0].Item1);
            Assert.AreEqual(AccessLevel.Modify, result[0].Item2);
        }

        [Test]
        public void GetGroupToApplicationComponentAndAccessLevelMappings_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetGroupToApplicationComponentAndAccessLevelMappings("user1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = testReaderNode.ContainsEntityType(entityType);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsEntityType_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.ContainsEntityType("ClientAccount");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.GetEntities(entityType));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetEntities_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetEntities("user1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = testReaderNode.ContainsEntity(entityType, entity);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsEntity_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.ContainsEntity("ClientAccount", "CompanyA");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<Tuple<String, String>>(testReaderNode.GetUserToEntityMappings(user));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entityType, result[0].Item1);
            Assert.AreEqual(entity, result[0].Item2);
        }

        [Test]
        public void GetUserToEntityMappingsUserOverload_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetUserToEntityMappings("user1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.GetUserToEntityMappings(user, entityType));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetUserToEntityMappingsUserAndEntityTypeOverload_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetUserToEntityMappings("user1", "ClientAccount");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<Tuple<String, String>>(testReaderNode.GetGroupToEntityMappings(group));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entityType, result[0].Item1);
            Assert.AreEqual(entity, result[0].Item2);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupOverload_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetGroupToEntityMappings("group1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.GetGroupToEntityMappings(group, entityType));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetGroupToEntityMappingsGroupAndEntityTypeOverload_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetGroupToEntityMappings("group1", "ClientAccount");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = testReaderNode.HasAccessToApplicationComponent(user, ApplicationScreen.Settings, AccessLevel.Modify);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToApplicationComponent_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.HasAccessToApplicationComponent("user1", ApplicationScreen.Settings, AccessLevel.Modify);
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = testReaderNode.HasAccessToEntity(user, entityType, entity);

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToEntity_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.HasAccessToEntity("user1", "ClientAccount", "CompanyA");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetApplicationComponentsAccessibleByUser(user));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.Summary, result[0].Item1);
            Assert.AreEqual(AccessLevel.View, result[0].Item2);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByUser_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetApplicationComponentsAccessibleByUser("user1");
            });

            Assert.AreEqual(mockException, e);
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
            testReaderNode.Load();

            var result = new List<Tuple<ApplicationScreen, AccessLevel>>(testReaderNode.GetApplicationComponentsAccessibleByGroup(group));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ApplicationScreen.Summary, result[0].Item1);
            Assert.AreEqual(AccessLevel.Modify, result[0].Item2);
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroup_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetApplicationComponentsAccessibleByGroup("group1");
            });

            Assert.AreEqual(mockException, e);
        }

        [Test]
        public void GetEntitiesAccessibleByUser()
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.GetEntitiesAccessibleByUser(user, entityType));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetEntitiesAccessibleByUser_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetEntitiesAccessibleByUser("user1", "ClientAccount");
            });

            Assert.AreEqual(mockException, e);
        }

        [Test]
        public void GetEntitiesAccessibleByGroup()
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
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.GetEntitiesAccessibleByGroup(group, entityType));

            mockRefreshStrategy.Received(1).NotifyQueryMethodCalled();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(entity, result[0]);
        }

        [Test]
        public void GetEntitiesAccessibleByGroup_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.GetEntitiesAccessibleByGroup("group1", "ClientAccount");
            });

            Assert.AreEqual(mockException, e);
        }

        [Test]
        public void Refresh_LoadFails()
        {
            var mockException = new Exception("Failed to load.");
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns
            (
                // Return successfully from the initial Load() call
                callnfo => { return returnedLoadState; }, 
                // Fail the call which occurs as part of the Refresh() call
                callnfo => { throw mockException; }
            );
            mockEventCache.When((eventCache) => eventCache.GetAllEventsSince(returnedLoadState.Item1)).Do((callInfo) => throw new EventNotCachedException("The specified event was not cached."));
            testReaderNode = new ReaderNode<string, string, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader); 
            testReaderNode.Load();

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);
            });

            mockPersistentReader.Received(2).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.Item1);
            Assert.That(e.Message, Does.StartWith("Failed to refresh the entire contents of the reader node."));
            Assert.IsInstanceOf<Exception>(e.InnerException);
            Assert.That(e.InnerException.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void Refresh_CacheDoesntContainLatestEventId()
        {
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns(returnedLoadState);
            mockEventCache.When((eventCache) => eventCache.GetAllEventsSince(returnedLoadState.Item1)).Do((callInfo) => throw new EventNotCachedException("The specified event was not cached."));
            testReaderNode = new ReaderNode<string, string, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader);
            testReaderNode.Load();
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
            });
            var refreshEventId = Guid.Parse("cc902edc-d46a-4ce4-8e4d-02db408cd2dc");
            DateTime refreshEventOccurredTime = CreateDataTimeFromString("2022-10-08 10:53:02");
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(new Tuple<Guid, DateTime>(refreshEventId, refreshEventOccurredTime));

            capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);

            mockPersistentReader.Received(2).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.Item1);
            var result = new List<String>(testReaderNode.Users);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user, result[0]);
        }

        [Test]
        public void Refresh_CallToEventCacheFails()
        {
            var mockException = new Exception("Failure to retrieve event from cache.");
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns(returnedLoadState);
            mockEventCache.When((eventCache) => eventCache.GetAllEventsSince(returnedLoadState.Item1)).Do((callInfo) => throw mockException);
            testReaderNode = new ReaderNode<string, string, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader);
            testReaderNode.Load();

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);
            });

            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.Item1);
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve latest access manager events following event '{returnedLoadState.Item1}' from cache."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Refresh()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            var userEventId = Guid.Parse("625e7ff0-429a-40c8-a5d6-5c4457981522");
            DateTime userEventOccurredTime = CreateDataTimeFromString("2022-10-08 11:17:35");
            var entityTypeEventId = Guid.Parse("aca8565a-2d95-471e-8fe1-51d27024f793");
            DateTime entityTypeEventOccurredTime = CreateDataTimeFromString("2022-10-08 11:17:59");
            var testUpdateEvents = new List<TemporalEventBufferItemBase>()
            { 
                new UserEventBufferItem<String>(userEventId, EventAction.Add, user, userEventOccurredTime), 
                new EntityTypeEventBufferItem(entityTypeEventId, EventAction.Add, entityType, entityTypeEventOccurredTime)
            };
            EventHandler capturedSubscriberMethod = null;
            mockRefreshStrategy.ReaderNodeRefreshed += Arg.Do<EventHandler>(eventHandler => capturedSubscriberMethod = eventHandler);
            mockPersistentReader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>()).Returns(returnedLoadState);
            mockEventCache.GetAllEventsSince(returnedLoadState.Item1).Returns(testUpdateEvents);
            testReaderNode = new ReaderNode<string, string, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader);
            testReaderNode.Load();

            capturedSubscriberMethod.Invoke(mockRefreshStrategy, EventArgs.Empty);

            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockEventCache.Received(1).GetAllEventsSince(returnedLoadState.Item1);
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