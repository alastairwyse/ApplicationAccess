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
using NUnit.Framework;
using NUnit.Framework.Internal;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Persistence.Sql.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerAccessManagerTemporalPersisterBase class.
    /// </summary>
    /// <remarks>Since <see cref="SqlServerAccessManagerTemporalPersisterBase{TUser, TGroup, TComponent, TAccess}"/> is abstract, tests are performed via the <see cref="SqlServerAccessManagerTemporalPersister{TUser, TGroup, TComponent, TAccess}"/> class.</remarks>
    public class SqlServerAccessManagerTemporalPersisterBaseTests
    {
        private SqlServerAccessManagerTemporalPersister<String, String, String, String> testSqlServerAccessManagerTemporalPersister;

        [SetUp]
        protected void SetUp()
        {
            var hashCodeGenerator = new DefaultStringHashCodeGenerator();
            testSqlServerAccessManagerTemporalPersister = new SqlServerAccessManagerTemporalPersister<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                5,
                10,
                60, 
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                hashCodeGenerator,
                hashCodeGenerator,
                hashCodeGenerator,
                new NullLogger()
            );
        }
    }
}
