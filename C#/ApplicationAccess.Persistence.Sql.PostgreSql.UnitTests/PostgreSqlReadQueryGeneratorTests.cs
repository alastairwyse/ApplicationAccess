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

namespace ApplicationAccess.Persistence.Sql.PostgreSql.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.Sql.SqlServer.PostgreSqlReadQueryGenerator class.
    /// </summary>
    public class PostgreSqlReadQueryGeneratorTests
    {
        private DateTime testOccurredTime;
        private PostgreSqlReadQueryGenerator testPostgreSqlReadQueryGenerator;

        [SetUp]
        protected void SetUp()
        {
            testOccurredTime = DateTime.ParseExact("2024-03-10 11:45:33.1234567", "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);
            testOccurredTime = DateTime.SpecifyKind(testOccurredTime, DateTimeKind.Utc);
            testPostgreSqlReadQueryGenerator = new PostgreSqlReadQueryGenerator();
        }

        [Test]
        public void GenerateGetTransactionTimeOfEventQuery()
        {
            String eventIdAsString = "2b4a64f4-c50f-495b-a880-2a17d025cb20";
            Guid eventId = Guid.Parse(eventIdAsString);
            String expectedQuery =
            @$" 
            SELECT  TO_CHAR(TransactionTime, 'YYYY-MM-DD HH24:MI:ss.US') AS TransactionTime 
            FROM    EventIdToTransactionTimeMap
            WHERE   EventId = '{eventId.ToString()}';";

            String result = testPostgreSqlReadQueryGenerator.GenerateGetTransactionTimeOfEventQuery(eventId);

            Assert.AreEqual(expectedQuery, result);
        }

        [Test]
        public void GenerateGetEventCorrespondingToStateTimeQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  EventId::varchar AS EventId,
		            TO_CHAR(TransactionTime, 'YYYY-MM-DD HH24:MI:ss.US') AS TransactionTime
            FROM    EventIdToTransactionTimeMap
            WHERE   TransactionTime <= TO_TIMESTAMP('2024-03-10 11:45:33.123456', 'YYYY-MM-DD HH24:MI:ss.US')::timestamp
            ORDER   BY TransactionTime DESC
            LIMIT   1;";

            String result = testPostgreSqlReadQueryGenerator.GenerateGetEventCorrespondingToStateTimeQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }

        // Testing just GenerateGetUserToGroupMappingsQuery() should cover everything that's different between this class and SqlServerReadQueryGenerator
        //   in terms of queries of main AccessManager elements (i.e. reserved word delimiters and conversion from DateTime to string)
        //   Everything else (aside from abstract methods test above) is common to base class ReadQueryGeneratorBase.

        [Test]
        public void GenerateGetUserToGroupMappingsQuery()
        {
            String expectedQuery =
            @$" 
            SELECT  u.""User"", 
                    g.""Group""
            FROM    UserToGroupMappings ug
                    INNER JOIN Users u
                      ON ug.UserId = u.Id
                    INNER JOIN Groups g
                      ON ug.GroupId = g.Id
            WHERE   TO_TIMESTAMP('2024-03-10 11:45:33.123456', 'YYYY-MM-DD HH24:MI:ss.US')::timestamp BETWEEN ug.TransactionFrom AND ug.TransactionTo;";

            String result = testPostgreSqlReadQueryGenerator.GenerateGetUserToGroupMappingsQuery(testOccurredTime);

            Assert.AreEqual(expectedQuery, result);
        }
    }
}
