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
using Microsoft.Data.SqlClient;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using System.Globalization;

namespace ApplicationAccess.Persistence.Sql.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.Sql.SqlPersisterUtilitiesBase class.
    /// </summary>
    /// <remarks>Since <see cref="SqlPersisterUtilitiesBase{TUser, TGroup, TComponent, TAccess}"/> is abstract, tests are performed via the <see cref="SqlServerPersisterUtilities{TUser, TGroup, TComponent, TAccess}"/> class.</remarks>
    public class SqlPersisterUtilitiesBaseTests
    {
        private SqlServerPersisterUtilities<String, String, String, String> testSqlServerPersisterUtilities;

        [SetUp]
        protected void SetUp()
        {
            testSqlServerPersisterUtilities = new SqlServerPersisterUtilities<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                60,
                new SqlRetryLogicOption(),
                (Object sender, SqlRetryingEventArgs eventArgs) => { }, 
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier()
            );
        }

        [Test]
        public void LoadStateTimeOverload_ParameterStateDateNotUtc()
        {
            DateTime testStateTime = DateTime.ParseExact("2022-08-20 19:48:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerPersisterUtilities.Load(DateTime.Now, new AccessManager<String, String, String, String>(false));
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' must be expressed as UTC."));
            Assert.AreEqual("stateTime", e.ParamName);
        }

        [Test]
        public void LoadStateTimeOverload_ParameterStateDateInTheFuture()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerPersisterUtilities.Load(DateTime.MaxValue.ToUniversalTime(), new AccessManager<String, String, String, String>(false));
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' will value '{DateTime.MaxValue.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff")}' is greater than the current time '"));
            Assert.AreEqual("stateTime", e.ParamName);
        }
    }
}
