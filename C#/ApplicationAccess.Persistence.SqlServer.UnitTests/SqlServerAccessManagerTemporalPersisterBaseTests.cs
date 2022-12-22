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
using NUnit.Framework;
using NUnit.Framework.Internal;
using ApplicationLogging;

namespace ApplicationAccess.Persistence.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.SqlServer.SqlServerAccessManagerTemporalPersisterBase class.
    /// </summary>
    /// <remarks>Since <see cref="SqlServerAccessManagerTemporalPersisterBase{TUser, TGroup, TComponent, TAccess}"/> is abstract, tests are performed via the <see cref="SqlServerAccessManagerTemporalPersister{TUser, TGroup, TComponent, TAccess}"/> class.</remarks>
    public class SqlServerAccessManagerTemporalPersisterBaseTests
    {
        private SqlServerAccessManagerTemporalPersister<String, String, String, String> testSqlServerAccessManagerTemporalPersister;

        [SetUp]
        protected void SetUp()
        {
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
        
        [Test]
        public void LoadStateTimeOverload_ParameterStateDateNotUtc()
        {
            DateTime testStateTime = DateTime.ParseExact("2022-08-20 19:48:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.Load(DateTime.Now, new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' must be expressed as UTC."));
            Assert.AreEqual("stateTime", e.ParamName);
        }

        [Test]
        public void LoadStateTimeOverload_ParameterStateDateInTheFuture()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerAccessManagerTemporalPersister.Load(DateTime.MaxValue.ToUniversalTime(), new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' will value '{DateTime.MaxValue.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff")}' is greater than the current time '"));
            Assert.AreEqual("stateTime", e.ParamName);
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
