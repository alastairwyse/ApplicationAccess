﻿/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Text;
using System.Globalization;
using System.Text.Json;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Utilities;
using ApplicationMetrics;
using Npgsql;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Persistence.Sql.PostgreSql.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.Sql.PostgreSql.PostgreSqlAccessManagerTemporalBulkPersister class.
    /// </summary>
    public class PostgreSqlAccessManagerTemporalBulkPersisterTests
    {
        protected const String processEventsStoredProcedureName = "ProcessEvents";
        // These values are used in the 'Type' property in the 'ProcessEvents' stored procedure JSON parameter
        protected const String userEventTypeValue = "user";
        protected const String groupEventTypeValue = "group";
        protected const String userToGroupMappingEventTypeValue = "userToGroupMapping";
        protected const String groupToGroupMappingEventTypeValue = "groupToGroupMapping";
        protected const String userToApplicationComponentAndAccessLevelMappingEventTypeValue = "userToApplicationComponentAndAccessLevelMapping";
        protected const String groupToApplicationComponentAndAccessLevelMappingEventTypeValue = "groupToApplicationComponentAndAccessLevelMapping";
        protected const String entityTypeEventTypeValue = "entityType";
        protected const String entityEventTypeValue = "entity";
        protected const String userToEntityMappingEventTypeValue = "userToEntityMapping";
        protected const String groupToEntityMappingEventTypeValue = "groupToEntityMapping";
        // These values are used in the 'Action' property in the 'ProcessEvents' stored procedure JSON parameter
        protected const String addEventActionValue = "add";
        protected const String removeEventActionValue = "remove";
        protected const String typePropertyName = "Type";
        protected const String idPropertyName = "Id";
        protected const String actionPropertyName = "Action";
        protected const String occurredTimePropertyName = "OccurredTime";
        protected const String data1PropertyName = "Data1";
        protected const String data2PropertyName = "Data2";
        protected const String data3PropertyName = "Data3";

        /// <summary>DateTime format string which can be interpreted by the <see href="https://www.postgresql.org/docs/8.1/functions-formatting.html">PostgreSQL to_timestamp() function</see>.</summary>
        protected const String postgreSQLTimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";
        protected const Int32 varCharColumnSizeLimit = 450;
        protected Guid testEventId;
        protected DateTime testOccurredTime;
        protected Int32 testHashCode;
        protected IMetricLogger mockMetricLogger;
        private IStoredProcedureExecutionWrapper mockStoredProcedureExecutionWrapper;
        private PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String> testPostgreSqlAccessManagerTemporalBulkPersister;

        [SetUp]
        protected void SetUp()
        {
            testEventId = Guid.Parse("e191a845-0f09-406c-b8e3-6c39663ef58b");
            testOccurredTime = DateTime.ParseExact("2022-07-18 12:15:33", "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);
            testOccurredTime = DateTime.SpecifyKind(testOccurredTime, DateTimeKind.Utc);
            testHashCode = 123;
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockStoredProcedureExecutionWrapper = Substitute.For<IStoredProcedureExecutionWrapper>();
            testPostgreSqlAccessManagerTemporalBulkPersister = new PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>
            (
                "Server=testServer; Port=5432; Database=ApplicationAccess; User Id=userId; Password=password;",
                60,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new NullLogger(),
                mockMetricLogger,
                mockStoredProcedureExecutionWrapper
            );
        }

        [TearDown]
        protected void TearDown()
        {
            testPostgreSqlAccessManagerTemporalBulkPersister.Dispose();
        }

        [Test]
        public void Constructor_ConnectionStringParameterWhitespace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister = new PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    "  ",
                    60,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void Constructor_CommandTimeoutParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister = new PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    "Server=testServer; Port=5432; Database=ApplicationAccess; User Id=userId; Password=password;",
                    -1,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'commandTimeout' with value -1 cannot be less than 0."));
            Assert.AreEqual("commandTimeout", e.ParamName);
        }

        [Test]
        public void Constructor_DatasourceBuildFails()
        {
            var e = Assert.Throws<Exception>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister = new PostgreSqlAccessManagerTemporalBulkPersister<String, String, String, String>
                (
                    "InvalidParam=ABC; Server=testServer; Port=5432; Database=ApplicationAccess; User Id=userId; Password=password;",
                    60,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to create NpgsqlDataSource from connection string 'InvalidParam=ABC; Server=testServer; Port=5432; Database=ApplicationAccess; User Id=userId; Password=password;'."));
        }

        [Test]
        public void PersistEvents_UserEventUserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new UserEventBufferItem<String>(testEventId, EventAction.Add, testUser, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'User' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("User", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupEventGroupLongerThanVarCharLimit()
        {
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new GroupEventBufferItem<String>(testEventId, EventAction.Add, testGroup, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'Group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("Group", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToGroupMappingEventUserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);
            String testGroup = "group1";
            var testBufferItem = new UserToGroupMappingEventBufferItem<String, String>(testEventId, EventAction.Add, testUser, testGroup, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'User' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("User", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToGroupMappingEventGroupLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new UserToGroupMappingEventBufferItem<String, String>(testEventId, EventAction.Add, testUser, testGroup, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'Group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("Group", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToGroupMappingEventFromGroupLongerThanVarCharLimit()
        {
            String testFromGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            String testToGroup = "group2";
            var testBufferItem = new GroupToGroupMappingEventBufferItem<String>(testEventId, EventAction.Add, testFromGroup, testToGroup, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'FromGroup' with stringified value '{testFromGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("FromGroup", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToGroupMappingEventToGroupLongerThanVarCharLimit()
        {
            String testFromGroup = "group1";
            String testToGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new GroupToGroupMappingEventBufferItem<String>(testEventId, EventAction.Add, testFromGroup, testToGroup, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'ToGroup' with stringified value '{testToGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("ToGroup", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToApplicationComponentAndAccessLevelMappingEventUserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);
            String testApplicationComponent = "SummaryScreen";
            String testAccessLevel = "View";
            var testBufferItem = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(testEventId, EventAction.Add, testUser, testApplicationComponent, testAccessLevel, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'User' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("User", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToApplicationComponentAndAccessLevelMappingEventApplicationComponentLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testApplicationComponent = GenerateLongString(varCharColumnSizeLimit + 1);
            String testAccessLevel = "View";
            var testBufferItem = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(testEventId, EventAction.Add, testUser, testApplicationComponent, testAccessLevel, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'ApplicationComponent' with stringified value '{testApplicationComponent}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("ApplicationComponent", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToApplicationComponentAndAccessLevelMappingEventAccessLevelLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testApplicationComponent = "SummaryScreen";
            String testAccessLevel = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(testEventId, EventAction.Add, testUser, testApplicationComponent, testAccessLevel, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'AccessLevel' with stringified value '{testAccessLevel}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("AccessLevel", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToApplicationComponentAndAccessLevelMappingEventGroupLongerThanVarCharLimit()
        {
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            String testApplicationComponent = "SummaryScreen";
            String testAccessLevel = "View";
            var testBufferItem = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(testEventId, EventAction.Add, testGroup, testApplicationComponent, testAccessLevel, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'Group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("Group", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToApplicationComponentAndAccessLevelMappingEventApplicationComponentLongerThanVarCharLimit()
        {
            String testGroup = "group1";
            String testApplicationComponent = GenerateLongString(varCharColumnSizeLimit + 1);
            String testAccessLevel = "View";
            var testBufferItem = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(testEventId, EventAction.Add, testGroup, testApplicationComponent, testAccessLevel, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'ApplicationComponent' with stringified value '{testApplicationComponent}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("ApplicationComponent", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToApplicationComponentAndAccessLevelMappingEventAccessLevelLongerThanVarCharLimit()
        {
            String testGroup = "group1";
            String testApplicationComponent = "SummaryScreen";
            String testAccessLevel = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(testEventId, EventAction.Add, testGroup, testApplicationComponent, testAccessLevel, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'AccessLevel' with stringified value '{testAccessLevel}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("AccessLevel", e.ParamName);
        }

        [Test]
        public void PersistEvents_EntityTypeEventEntityTypeLongerThanVarCharLimit()
        {
            String testEntityType = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new EntityTypeEventBufferItem(testEventId, EventAction.Add, testEntityType, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'EntityType' with stringified value '{testEntityType}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("EntityType", e.ParamName);
        }

        [Test]
        public void PersistEvents_EntityEventEntityTypeLongerThanVarCharLimit()
        {
            String testEntityType = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntity = "CompanyA";
            var testBufferItem = new EntityEventBufferItem(testEventId, EventAction.Add, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'EntityType' with stringified value '{testEntityType}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("EntityType", e.ParamName);
        }

        [Test]
        public void PersistEvents_EntityEventEntityLongerThanVarCharLimit()
        {
            String testEntityType = "Clients";
            String testEntity = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new EntityEventBufferItem(testEventId, EventAction.Add, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'Entity' with stringified value '{testEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("Entity", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToEntityMappingEventUserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var testBufferItem = new UserToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testUser, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'User' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("User", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToEntityMappingEventEntityTypeLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testEntityType = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntity = "CompanyA";
            var testBufferItem = new UserToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testUser, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'EntityType' with stringified value '{testEntityType}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("EntityType", e.ParamName);
        }

        [Test]
        public void PersistEvents_UserToEntityMappingEventEntityLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testEntityType = "Clients";
            String testEntity = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new UserToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testUser, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'Entity' with stringified value '{testEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("Entity", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToEntityMappingEventGroupLongerThanVarCharLimit()
        {
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var testBufferItem = new GroupToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testGroup, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'Group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("Group", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToEntityMappingEventEntityTypeLongerThanVarCharLimit()
        {
            String testGroup = "group1";
            String testEntityType = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntity = "CompanyA";
            var testBufferItem = new GroupToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testGroup, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'EntityType' with stringified value '{testEntityType}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("EntityType", e.ParamName);
        }

        [Test]
        public void PersistEvents_GroupToEntityMappingEventEntityLongerThanVarCharLimit()
        {
            String testGroup = "group1";
            String testEntityType = "Clients";
            String testEntity = GenerateLongString(varCharColumnSizeLimit + 1);
            var testBufferItem = new GroupToEntityMappingEventBufferItem<String>(testEventId, EventAction.Add, testGroup, testEntityType, testEntity, testOccurredTime, testHashCode);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith($"Event property 'Entity' with stringified value '{testEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("Entity", e.ParamName);
        }

        [Test]
        public void PersistEvents_ExceptionExecutingStoredProcedure()
        {
            String testUser = "user1";
            var testBufferItem = new UserEventBufferItem<String>(testEventId, EventAction.Add, testUser, testOccurredTime, testHashCode);
            string mockExceptionMessage = "Mock PostgreSql exception";
            mockStoredProcedureExecutionWrapper.When(wrapper => wrapper.Execute(processEventsStoredProcedureName, Arg.Any<IList<NpgsqlParameter>>())).Do(callInfo => { throw new Exception(mockExceptionMessage); });

            var e = Assert.Throws<Exception>(delegate
            {
                testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(new List<TemporalEventBufferItemBase>() { testBufferItem });
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void PersistEvents()
        {
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid5 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var guid6 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var guid7 = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var guid8 = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var guid9 = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var guid10 = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var guid11 = Guid.Parse("00000000-0000-0000-0000-00000000000a");
            var guid12 = Guid.Parse("00000000-0000-0000-0000-00000000000b");
            var guid13 = Guid.Parse("00000000-0000-0000-0000-00000000000c");
            var guid14 = Guid.Parse("00000000-0000-0000-0000-00000000000d");
            var guid15 = Guid.Parse("00000000-0000-0000-0000-00000000000e");
            var guid16 = Guid.Parse("00000000-0000-0000-0000-00000000000f");
            var guid17 = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var guid18 = Guid.Parse("00000000-0000-0000-0000-000000000011");
            var guid19 = Guid.Parse("00000000-0000-0000-0000-000000000012");
            var guid20 = Guid.Parse("00000000-0000-0000-0000-000000000013");
            var occurredTime1 = CreateDataTimeFromString("2021-06-12 13:43:00");
            var occurredTime2 = CreateDataTimeFromString("2021-06-12 13:43:01");
            var occurredTime3 = CreateDataTimeFromString("2021-06-12 13:43:02");
            var occurredTime4 = CreateDataTimeFromString("2021-06-12 13:43:03");
            var occurredTime5 = CreateDataTimeFromString("2021-06-12 13:43:04");
            var occurredTime6 = CreateDataTimeFromString("2021-06-12 13:43:05");
            var occurredTime7 = CreateDataTimeFromString("2021-06-12 13:43:06");
            var occurredTime8 = CreateDataTimeFromString("2021-06-12 13:43:07");
            var occurredTime9 = CreateDataTimeFromString("2021-06-12 13:43:08");
            var occurredTime10 = CreateDataTimeFromString("2021-06-12 13:43:09");
            var occurredTime11 = CreateDataTimeFromString("2021-06-12 13:43:10");
            var occurredTime12 = CreateDataTimeFromString("2021-06-12 13:43:11");
            var occurredTime13 = CreateDataTimeFromString("2021-06-12 13:43:12");
            var occurredTime14 = CreateDataTimeFromString("2021-06-12 13:43:13");
            var occurredTime15 = CreateDataTimeFromString("2021-06-12 13:43:14");
            var occurredTime16 = CreateDataTimeFromString("2021-06-12 13:43:15");
            var occurredTime17 = CreateDataTimeFromString("2021-06-12 13:43:16");
            var occurredTime18 = CreateDataTimeFromString("2021-06-12 13:43:17");
            var occurredTime19 = CreateDataTimeFromString("2021-06-12 13:43:18");
            var occurredTime20 = CreateDataTimeFromString("2021-06-12 13:43:19");

            var testEvents = new List<TemporalEventBufferItemBase>()
            {
                new UserEventBufferItem<String>(guid1, EventAction.Add, "user1", occurredTime1, testHashCode),
                new UserEventBufferItem<String>(guid2, EventAction.Remove, "user2", occurredTime2, testHashCode),
                new GroupEventBufferItem<String>(guid3, EventAction.Add, "group1", occurredTime3, testHashCode),
                new GroupEventBufferItem<String>(guid4, EventAction.Remove, "group2", occurredTime4, testHashCode),
                new UserToGroupMappingEventBufferItem<String, String>(guid5, EventAction.Add, "user3", "group3", occurredTime5, testHashCode),
                new UserToGroupMappingEventBufferItem<String, String>(guid6, EventAction.Remove, "user4", "group4", occurredTime6, testHashCode),
                new GroupToGroupMappingEventBufferItem<String>(guid7, EventAction.Add, "group5", "group6", occurredTime7, testHashCode),
                new GroupToGroupMappingEventBufferItem<String>(guid8, EventAction.Remove, "group7", "group8", occurredTime8, testHashCode),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(guid9, EventAction.Add, "user5", "SummaryScreen", "View", occurredTime9, testHashCode),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(guid10, EventAction.Remove, "user6", "OrderScreen", "Modify", occurredTime10, testHashCode),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(guid11, EventAction.Add, "group9", "SettingsScreen", "Save", occurredTime11, testHashCode),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(guid12, EventAction.Remove, "group10", "AdminScreen", "Remove", occurredTime12, testHashCode),
                new EntityTypeEventBufferItem(guid13, EventAction.Add, "Clients", occurredTime13, testHashCode),
                new EntityTypeEventBufferItem(guid14, EventAction.Remove, "Products", occurredTime14, testHashCode),
                new EntityEventBufferItem(guid15, EventAction.Add, "Accounts", "ABC123", occurredTime15, testHashCode),
                new EntityEventBufferItem(guid16, EventAction.Remove, "Staff", "Jane.Smith@company.com",  occurredTime16, testHashCode),
                new UserToEntityMappingEventBufferItem<String>(guid17, EventAction.Add, "user7", "EndPoints", "api/Staff/", occurredTime17, testHashCode),
                new UserToEntityMappingEventBufferItem<String>(guid18, EventAction.Remove, "user8", "Software", "ServiceNow",  occurredTime18, testHashCode),
                new GroupToEntityMappingEventBufferItem<String>(guid19, EventAction.Add, "group11", "Versions", "22H2 (2022 Update)", occurredTime19, testHashCode),
                new GroupToEntityMappingEventBufferItem<String>(guid20, EventAction.Remove, "group12", "Packages", "NumPy",  occurredTime20, testHashCode),
            };
            var capturedJsonParameter = new JArray();
            mockStoredProcedureExecutionWrapper.Execute(processEventsStoredProcedureName, Arg.Do<IList<NpgsqlParameter>>((parameters) =>
            {
                using (JsonDocument eventsJson = (JsonDocument)parameters[0].Value)
                {
                    capturedJsonParameter = JArray.Parse(eventsJson.RootElement.ToString());
                }
            }));

            testPostgreSqlAccessManagerTemporalBulkPersister.PersistEvents(testEvents);

            mockStoredProcedureExecutionWrapper.Received(1).Execute(processEventsStoredProcedureName, Arg.Any<IList<NpgsqlParameter>>());
            Assert.AreEqual(20, capturedJsonParameter.Count);
            AssertJsonParameterElement((JObject)capturedJsonParameter[0], userEventTypeValue, guid1, addEventActionValue, occurredTime1, "user1");
            AssertJsonParameterElement((JObject)capturedJsonParameter[1], userEventTypeValue, guid2, removeEventActionValue, occurredTime2, "user2");
            AssertJsonParameterElement((JObject)capturedJsonParameter[2], groupEventTypeValue, guid3, addEventActionValue, occurredTime3, "group1");
            AssertJsonParameterElement((JObject)capturedJsonParameter[3], groupEventTypeValue, guid4, removeEventActionValue, occurredTime4, "group2");
            AssertJsonParameterElement((JObject)capturedJsonParameter[4], userToGroupMappingEventTypeValue, guid5, addEventActionValue, occurredTime5, "user3", "group3");
            AssertJsonParameterElement((JObject)capturedJsonParameter[5], userToGroupMappingEventTypeValue, guid6, removeEventActionValue, occurredTime6, "user4", "group4");
            AssertJsonParameterElement((JObject)capturedJsonParameter[6], groupToGroupMappingEventTypeValue, guid7, addEventActionValue, occurredTime7, "group5", "group6");
            AssertJsonParameterElement((JObject)capturedJsonParameter[7], groupToGroupMappingEventTypeValue, guid8, removeEventActionValue, occurredTime8, "group7", "group8");
            AssertJsonParameterElement((JObject)capturedJsonParameter[8], userToApplicationComponentAndAccessLevelMappingEventTypeValue, guid9, addEventActionValue, occurredTime9, "user5", "SummaryScreen", "View");
            AssertJsonParameterElement((JObject)capturedJsonParameter[9], userToApplicationComponentAndAccessLevelMappingEventTypeValue, guid10, removeEventActionValue, occurredTime10, "user6", "OrderScreen", "Modify");
            AssertJsonParameterElement((JObject)capturedJsonParameter[10], groupToApplicationComponentAndAccessLevelMappingEventTypeValue, guid11, addEventActionValue, occurredTime11, "group9", "SettingsScreen", "Save");
            AssertJsonParameterElement((JObject)capturedJsonParameter[11], groupToApplicationComponentAndAccessLevelMappingEventTypeValue, guid12, removeEventActionValue, occurredTime12, "group10", "AdminScreen", "Remove");
            AssertJsonParameterElement((JObject)capturedJsonParameter[12], entityTypeEventTypeValue, guid13, addEventActionValue, occurredTime13, "Clients");
            AssertJsonParameterElement((JObject)capturedJsonParameter[13], entityTypeEventTypeValue, guid14, removeEventActionValue, occurredTime14, "Products");
            AssertJsonParameterElement((JObject)capturedJsonParameter[14], entityEventTypeValue, guid15, addEventActionValue, occurredTime15, "Accounts", "ABC123");
            AssertJsonParameterElement((JObject)capturedJsonParameter[15], entityEventTypeValue, guid16, removeEventActionValue, occurredTime16, "Staff", "Jane.Smith@company.com");
            AssertJsonParameterElement((JObject)capturedJsonParameter[16], userToEntityMappingEventTypeValue, guid17, addEventActionValue, occurredTime17, "user7", "EndPoints", "api/Staff/");
            AssertJsonParameterElement((JObject)capturedJsonParameter[17], userToEntityMappingEventTypeValue, guid18, removeEventActionValue, occurredTime18, "user8", "Software", "ServiceNow");
            AssertJsonParameterElement((JObject)capturedJsonParameter[18], groupToEntityMappingEventTypeValue, guid19, addEventActionValue, occurredTime19, "group11", "Versions", "22H2 (2022 Update)");
            AssertJsonParameterElement((JObject)capturedJsonParameter[19], groupToEntityMappingEventTypeValue, guid20, removeEventActionValue, occurredTime20, "group12", "Packages", "NumPy");
        }

        #region Private/Protected Methods

        /// <summary>
        /// Asserts that the specified element of the 'ProcessEvents' stored procedure parameter contains the specified values.
        /// </summary>
        protected void AssertJsonParameterElement(JObject arrayElement, String typeValue, Guid idValue, String actionValue, DateTime occurredTimeValue, String data1Value)
        {
            Assert.IsNotNull(arrayElement[typePropertyName]);
            Assert.IsNotNull(arrayElement[idPropertyName]);
            Assert.IsNotNull(arrayElement[actionPropertyName]);
            Assert.IsNotNull(arrayElement[occurredTimePropertyName]);
            Assert.IsNotNull(arrayElement[data1PropertyName]);
            Assert.AreEqual(typeValue, arrayElement[typePropertyName].ToString());
            Assert.AreEqual(idValue.ToString(), arrayElement[idPropertyName].ToString());
            Assert.AreEqual(actionValue, arrayElement[actionPropertyName].ToString());
            Assert.AreEqual(occurredTimeValue.ToString(postgreSQLTimestampFormat), arrayElement[occurredTimePropertyName].ToString());
            Assert.AreEqual(data1Value, arrayElement[data1PropertyName].ToString());
        }

        /// <summary>
        /// Asserts that the specified element of the 'ProcessEvents' stored procedure parameter contains the specified values.
        /// </summary>
        protected void AssertJsonParameterElement(JObject arrayElement, String typeValue, Guid idValue, String actionValue, DateTime occurredTimeValue, String data1Value, String data2Value)
        {
            AssertJsonParameterElement(arrayElement, typeValue, idValue, actionValue, occurredTimeValue, data1Value);
            Assert.IsNotNull(arrayElement[data2PropertyName]);
            Assert.AreEqual(data2Value, arrayElement[data2PropertyName].ToString());
        }

        /// <summary>
        /// Asserts that the specified element of the 'ProcessEvents' stored procedure parameter contains the specified values.
        /// </summary>
        protected void AssertJsonParameterElement(JObject arrayElement, String typeValue, Guid idValue, String actionValue, DateTime occurredTimeValue, String data1Value, String data2Value, String data3Value)
        {
            AssertJsonParameterElement(arrayElement, typeValue, idValue, actionValue, occurredTimeValue, data1Value, data2Value);
            Assert.IsNotNull(arrayElement[data3PropertyName]);
            Assert.AreEqual(data3Value, arrayElement[data3PropertyName].ToString());
        }

        /// <summary>
        /// Generates a string of the specified length.
        /// </summary>
        /// <param name="stringLength">The length of the string to generate.</param>
        /// <returns>The generated string.</returns>
        protected String GenerateLongString(Int32 stringLength)
        {
            if (stringLength < 1)
                throw new ArgumentOutOfRangeException(nameof(stringLength), $"Parameter '{nameof(stringLength)}' with value {stringLength} must be greater than 0.");

            Int32 currentAsciiIndex = 65;
            var stringBuilder = new StringBuilder();
            Int32 localStringLength = stringLength;
            while (localStringLength > 0)
            {
                stringBuilder.Append((Char)currentAsciiIndex);
                if (currentAsciiIndex == 90)
                {
                    currentAsciiIndex = 65;
                }
                else
                {
                    currentAsciiIndex++;
                }

                localStringLength--;
            }

            return stringBuilder.ToString();
        }

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
