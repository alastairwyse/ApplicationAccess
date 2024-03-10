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

        // Testing just GenerateGetUserToGroupMappingsQuery() should cover everything that's different between this class and SqlServerReadQueryGenerator
        //   (i.e. reserved word delimiters and conversion from DateTime to string)
        //   Everything else is common to base class ReadQueryGeneratorBase.

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
