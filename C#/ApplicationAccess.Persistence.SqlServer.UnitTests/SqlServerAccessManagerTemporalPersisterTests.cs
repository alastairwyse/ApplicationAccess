/*
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
using System.Text;
using System.Globalization;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Persistence.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.SqlServer.SqlServerAccessManagerTemporalPersister class.
    /// </summary>
    public class SqlServerAccessManagerTemporalPersisterTests
    {
        private const Int32 varCharColumnSizeLimit = 450;
        private Guid testEventId;
        private DateTime testOccurredTime; 
        private SqlServerAccessManagerTemporalPersister<String, String, String, String> testSqlServerAccessManagerTemporalPersister;

        [SetUp]
        protected void SetUp()
        {
            testEventId = Guid.Parse("e191a845-0f09-406c-b8e3-6c39663ef58b");
            testOccurredTime = DateTime.ParseExact("2022-07-18 12:15:33", "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);
            testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                5, 
                10, 
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(), 
                new NullLogger()
            );
        }

        [TearDown]
        protected void TearDown()
        {
        }

        [Test]
        public void Constructor_ConnectionStringParameterWhitespace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "  ",
                    5,
                    10,
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
        public void Constructor_RetryCountParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    -1,
                    10,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryCount' with value -1 cannot be less than 0."));
            Assert.AreEqual("retryCount", e.ParamName);
        }

        [Test]
        public void Constructor_RetryCountParameterGreaterThan59()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    60,
                    10,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryCount' with value 60 cannot be greater than 59."));
            Assert.AreEqual("retryCount", e.ParamName);
        }

        [Test]
        public void Constructor_RetryIntervalParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    5,
                    -1,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryInterval' with value -1 cannot be less than 0."));
            Assert.AreEqual("retryInterval", e.ParamName);
        }

        [Test]
        public void Constructor_RetryIntervalParameterGreaterThan120()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
                (
                    "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                    5,
                    121,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new NullLogger()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'retryInterval' with value 121 cannot be greater than 120."));
            Assert.AreEqual("retryInterval", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserStoredProcedure_UserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUser(testUser, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'user' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteGroupStoredProcedure_GroupLongerThanVarCharLimit()
        {
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddGroup(testGroup, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToGroupMappingProcedure_UserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);
            String testGroup = "group1";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToGroupMapping(testUser, testGroup, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'user' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToGroupMappingProcedure_GroupLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1); 

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToGroupMapping(testUser, testGroup, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure_UserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);
            String testApplicationComponent = "OrderScreen";
            String testAccessLevel = "View";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'user' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure_ApplicationComponentLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testApplicationComponent = GenerateLongString(varCharColumnSizeLimit + 1);
            String testAccessLevel = "View";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'applicationComponent' with stringified value '{testApplicationComponent}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("applicationComponent", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToApplicationComponentAndAccessLevelMappingStoredProcedure_AccessLevelLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testApplicationComponent = "OrderScreen";
            String testAccessLevel = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToApplicationComponentAndAccessLevelMapping(testUser, testApplicationComponent, testAccessLevel, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'accessLevel' with stringified value '{testAccessLevel}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("accessLevel", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure_GroupLongerThanVarCharLimit()
        {
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            String testApplicationComponent = "OrderScreen";
            String testAccessLevel = "View";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure_ApplicationComponentLongerThanVarCharLimit()
        {
            String testGroup = "user1";
            String testApplicationComponent = GenerateLongString(varCharColumnSizeLimit + 1);
            String testAccessLevel = "View";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'applicationComponent' with stringified value '{testApplicationComponent}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("applicationComponent", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteGroupToApplicationComponentAndAccessLevelMappingStoredProcedure_AccessLevelLongerThanVarCharLimit()
        {
            String testGroup = "user1";
            String testApplicationComponent = "OrderScreen";
            String testAccessLevel = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, testApplicationComponent, testAccessLevel, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'accessLevel' with stringified value '{testAccessLevel}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("accessLevel", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteEntityTypeStoredProcedure_EntityTypeLongerThanVarCharLimit()
        {
            String testTypeEntity = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddEntityType(testTypeEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'entityType' with stringified value '{testTypeEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteEntityStoredProcedure_EntityTypeLongerThanVarCharLimit()
        {
            String testTypeEntity = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntity = "ClientA";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddEntity(testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'entityType' with stringified value '{testTypeEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteEntityStoredProcedure_EntityLongerThanVarCharLimit()
        {
            String testTypeEntity = "ClientAccount";
            String testEntity = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddEntity(testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'entity' with stringified value '{testEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToEntityMappingStoredProcedure_UserLongerThanVarCharLimit()
        {
            String testUser = GenerateLongString(varCharColumnSizeLimit + 1);
            String testTypeEntity = "ClientAccount";
            String testEntity = "ClientA";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToEntityMapping(testUser, testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'user' with stringified value '{testUser}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("user", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToEntityMappingStoredProcedure_EntityTypeLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testTypeEntity = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntity = "ClientA";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToEntityMapping(testUser, testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'entityType' with stringified value '{testTypeEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteUserToEntityMappingStoredProcedure_EntityLongerThanVarCharLimit()
        {
            String testUser = "user1";
            String testTypeEntity = "ClientAccount";
            String testEntity = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddUserToEntityMapping(testUser, testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'entity' with stringified value '{testEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("entity", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteGroupToEntityMappingStoredProcedure_GroupLongerThanVarCharLimit()
        {
            String testGroup = GenerateLongString(varCharColumnSizeLimit + 1);
            String testTypeEntity = "ClientAccount";
            String testEntity = "ClientA";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddGroupToEntityMapping(testGroup, testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'group' with stringified value '{testGroup}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("group", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteGroupToEntityMappingStoredProcedure_EntityTypeLongerThanVarCharLimit()
        {
            String testGroup = "group1";
            String testTypeEntity = GenerateLongString(varCharColumnSizeLimit + 1);
            String testEntity = "ClientA";

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddGroupToEntityMapping(testGroup, testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'entityType' with stringified value '{testTypeEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("entityType", e.ParamName);
        }

        [Test]
        public void SetupAndExecuteGroupToEntityMappingStoredProcedure_EntityLongerThanVarCharLimit()
        {
            String testGroup = "group1";
            String testTypeEntity = "ClientAccount";
            String testEntity = GenerateLongString(varCharColumnSizeLimit + 1);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.AddGroupToEntityMapping(testGroup, testTypeEntity, testEntity, testEventId, testOccurredTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'entity' with stringified value '{testEntity}' is longer than the maximum allowable column size of {varCharColumnSizeLimit}."));
            Assert.AreEqual("entity", e.ParamName);
        }

        /// <summary>
        /// Generates a string of the specified length.
        /// </summary>
        /// <param name="stringLength">The length of the string to generate.</param>
        /// <returns>The generated string.</returns>
        private String GenerateLongString(Int32 stringLength)
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
    }
}
