/*
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
using System.Globalization;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Persistence.Sql.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerReadQueryGenerator class.
    /// </summary>
    public class SqlServerReadQueryGeneratorTests
    {
        private DateTime testOccurredTime;
        private SqlServerReadQueryGenerator testSqlServerReadQueryGenerator;

        [SetUp]
        protected void SetUp()
        {
            testOccurredTime = DateTime.ParseExact("2024-03-10 11:45:33.1234567", "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            testOccurredTime = DateTime.SpecifyKind(testOccurredTime, DateTimeKind.Utc);
            testSqlServerReadQueryGenerator = new SqlServerReadQueryGenerator();
        }

        [Test]
        public void GenerateGetTransactionTimeOfEventQuery()
        {
            String eventIdAsString = "2b4a64f4-c50f-495b-a880-2a17d025cb20";
            Guid eventId = Guid.Parse(eventIdAsString);
            String expectedQuery =
            @$" 
            SELECT  CONVERT(nvarchar(40), EventId) AS 'EventId',
                    CONVERT(nvarchar(30), TransactionTime , 126) AS 'TransactionTime', 
                    CONVERT(nvarchar(30), TransactionSequence) AS 'TransactionSequence' 
            FROM    EventIdToTransactionTimeMap 
            WHERE   TransactionTime = 
                    (
                        SELECT  TransactionTime 
                        FROM    EventIdToTransactionTimeMap 
                        WHERE   EventId = '{eventIdAsString}'
                    );";

            String result = testSqlServerReadQueryGenerator.GenerateGetTransactionTimeOfEventQuery(eventId);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetEventCorrespondingToStateTimeQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  TOP(1)
                    CONVERT(nvarchar(40), EventId) AS 'EventId',
                    CONVERT(nvarchar(30), TransactionTime , 126) AS 'TransactionTime', 
                    CONVERT(nvarchar(30), TransactionSequence) AS 'TransactionSequence' 
            FROM    EventIdToTransactionTimeMap
            WHERE   TransactionTime <= CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126)
            ORDER   BY TransactionTime DESC, 
                       TransactionSequence DESC;";

            String result = testSqlServerReadQueryGenerator.GenerateGetEventCorrespondingToStateTimeQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetUsersQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  [User] 
            FROM    Users 
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN TransactionFrom AND TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetUsersQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetGroupsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  [Group] 
            FROM    Groups 
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN TransactionFrom AND TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetGroupsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetUserToGroupMappingsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  u.[User], 
                    g.[Group]
            FROM    UserToGroupMappings ug
                    INNER JOIN Users u
                      ON ug.UserId = u.Id
                    INNER JOIN Groups g
                      ON ug.GroupId = g.Id
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN ug.TransactionFrom AND ug.TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetUserToGroupMappingsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetGroupToGroupMappingsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  gg.Id, 
                    fg.[Group] AS 'FromGroup', 
                    tg.[Group] AS 'ToGroup'
            FROM    GroupToGroupMappings gg
                    INNER JOIN Groups fg
                      ON gg.FromGroupId = fg.Id
                    INNER JOIN Groups tg
                      ON gg.ToGroupId = tg.Id
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN gg.TransactionFrom AND gg.TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetGroupToGroupMappingsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetUserToApplicationComponentAndAccessLevelMappingsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  u.[User], 
                    ac.ApplicationComponent, 
                    al.AccessLevel 
            FROM    UserToApplicationComponentAndAccessLevelMappings uaa
                    INNER JOIN Users u
                      ON uaa.UserId = u.Id
                    INNER JOIN ApplicationComponents ac
                      ON uaa.ApplicationComponentId = ac.Id
                    INNER JOIN AccessLevels al
                      ON uaa.AccessLevelId = al.Id
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN uaa.TransactionFrom AND uaa.TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetUserToApplicationComponentAndAccessLevelMappingsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetGroupToApplicationComponentAndAccessLevelMappingsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  g.[Group], 
                    ac.ApplicationComponent, 
                    al.AccessLevel 
            FROM    GroupToApplicationComponentAndAccessLevelMappings gaa
                    INNER JOIN Groups g
                      ON gaa.GroupId = g.Id
                    INNER JOIN ApplicationComponents ac
                      ON gaa.ApplicationComponentId = ac.Id
                    INNER JOIN AccessLevels al
                      ON gaa.AccessLevelId = al.Id
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN gaa.TransactionFrom AND gaa.TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetGroupToApplicationComponentAndAccessLevelMappingsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetEntityTypesQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  EntityType
            FROM    EntityTypes 
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN TransactionFrom AND TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetEntityTypesQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetEntitiesQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  et.EntityType, 
                    e.Entity 
            FROM    Entities e
                    INNER JOIN EntityTypes et
                      ON e.EntityTypeId = et.Id
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN e.TransactionFrom AND e.TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetEntitiesQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetUserToEntityMappingsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  u.[User], 
                    et.EntityType, 
                    e.Entity
            FROM    UserToEntityMappings ue
                    INNER JOIN Users u
                      ON ue.UserId = u.Id
                    INNER JOIN EntityTypes et
                      ON ue.EntityTypeId = et.Id
                    INNER JOIN Entities e
                      ON ue.EntityId = e.Id
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN ue.TransactionFrom AND ue.TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetUserToEntityMappingsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetGroupToEntityMappingsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  g.[Group], 
                    et.EntityType, 
                    e.Entity
            FROM    GroupToEntityMappings ge
                    INNER JOIN Groups g
                        ON ge.GroupId = g.Id
                    INNER JOIN EntityTypes et
                        ON ge.EntityTypeId = et.Id
                    INNER JOIN Entities e
                        ON ge.EntityId = e.Id
            WHERE   CONVERT(datetime2, '2024-03-10T11:45:33.1234567', 126) BETWEEN ge.TransactionFrom AND ge.TransactionTo;";

            String result = testSqlServerReadQueryGenerator.GenerateGetGroupToEntityMappingsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }
    }
}
