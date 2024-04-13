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
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Utilities;
using NUnit.Framework;

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
                testSqlServerPersisterUtilities.Load(DateTime.Now, new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' must be expressed as UTC."));
            Assert.AreEqual("stateTime", e.ParamName);
        }

        [Test]
        public void LoadStateTimeOverload_ParameterStateDateInTheFuture()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerPersisterUtilities.Load(DateTime.MaxValue.ToUniversalTime(), new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'stateTime' will value '{DateTime.MaxValue.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff")}' is greater than the current time '"));
            Assert.AreEqual("stateTime", e.ParamName);
        }

        [Test]
        public void LoadStateTimeOverload_NoEventsReturned()
        {
            var testException = new Exception("Mock Exception");
            var testReturnEvents = new List<Tuple<Guid, DateTime, Int32>>();
            var sqlRetryLogicOption = new SqlRetryLogicOption();
            sqlRetryLogicOption.NumberOfTries = 1;
            sqlRetryLogicOption.MinTimeInterval = TimeSpan.FromSeconds(0);
            sqlRetryLogicOption.MaxTimeInterval = TimeSpan.FromSeconds(120);
            sqlRetryLogicOption.DeltaTime = TimeSpan.FromSeconds(10);
            var testSqlServerPersisterUtilities = new SqlServerPersisterUtilitiesWithOverriddenMembers<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                60,
                sqlRetryLogicOption,
                (Object sender, SqlRetryingEventArgs eventArgs) => { },
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testReturnEvents
            );

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerPersisterUtilities.Load(DateTime.UtcNow, new AccessManager<String, String, String, String>(), testException);
            });

            Assert.AreSame(testException, e);
        }

        [Test]
        public void LoadEventIdOverload_MultipleEventsWithSameTransactionTimeReturned()
        {
            var testGuid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var testGuid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var testReturnEvents = new List<Tuple<Guid, DateTime, Int32>>()
            {
                Tuple.Create
                (
                    testGuid1, 
                    DateTime.ParseExact("2024-04-11 14:06:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
                    0
                ),
                Tuple.Create
                (
                    testGuid2, 
                    DateTime.ParseExact("2024-04-11 14:06:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    1
                ),
            };
            var sqlRetryLogicOption = new SqlRetryLogicOption();
            sqlRetryLogicOption.NumberOfTries = 1;
            sqlRetryLogicOption.MinTimeInterval = TimeSpan.FromSeconds(0);
            sqlRetryLogicOption.MaxTimeInterval = TimeSpan.FromSeconds(120);
            sqlRetryLogicOption.DeltaTime = TimeSpan.FromSeconds(10);
            var testSqlServerPersisterUtilities = new SqlServerPersisterUtilitiesWithOverriddenMembers<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                60,
                sqlRetryLogicOption,
                (Object sender, SqlRetryingEventArgs eventArgs) => { },
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testReturnEvents
            );

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerPersisterUtilities.Load(testGuid1, new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Multiple EventIdToTransactionTimeMap rows were returned with EventId '00000000-0000-0000-0000-000000000001' and/or TransactionTime '2024-04-11T14:06:00.0000000'."));
        }

        [Test]
        public void LoadEventIdOverload_MultipleEventsWithSameEventIdReturned()
        {
            var testGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var testReturnEvents = new List<Tuple<Guid, DateTime, Int32>>()
            {
                Tuple.Create
                (
                    testGuid,
                    DateTime.ParseExact("2024-04-11 14:06:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    0
                ),
                Tuple.Create
                (
                    testGuid,
                    DateTime.ParseExact("2024-04-11 14:06:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    0
                ),
            };
            var sqlRetryLogicOption = new SqlRetryLogicOption();
            sqlRetryLogicOption.NumberOfTries = 1;
            sqlRetryLogicOption.MinTimeInterval = TimeSpan.FromSeconds(0);
            sqlRetryLogicOption.MaxTimeInterval = TimeSpan.FromSeconds(120);
            sqlRetryLogicOption.DeltaTime = TimeSpan.FromSeconds(10);
            var testSqlServerPersisterUtilities = new SqlServerPersisterUtilitiesWithOverriddenMembers<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                60,
                sqlRetryLogicOption,
                (Object sender, SqlRetryingEventArgs eventArgs) => { },
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testReturnEvents
            );

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerPersisterUtilities.Load(testGuid, new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"Multiple EventIdToTransactionTimeMap rows were returned with EventId '00000000-0000-0000-0000-000000000001' and/or TransactionTime '2024-04-11T14:06:00.0000000'."));
        }

        [Test]
        public void LoadEventIdOverload_NoEventsReturned()
        {
            var testGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var testReturnEvents = new List<Tuple<Guid, DateTime, Int32>>();
            var sqlRetryLogicOption = new SqlRetryLogicOption();
            sqlRetryLogicOption.NumberOfTries = 1;
            sqlRetryLogicOption.MinTimeInterval = TimeSpan.FromSeconds(0);
            sqlRetryLogicOption.MaxTimeInterval = TimeSpan.FromSeconds(120);
            sqlRetryLogicOption.DeltaTime = TimeSpan.FromSeconds(10);
            var testSqlServerPersisterUtilities = new SqlServerPersisterUtilitiesWithOverriddenMembers<String, String, String, String>
            (
                "Server=testServer; Database=testDB; User Id=userId; Password=password;",
                60,
                sqlRetryLogicOption,
                (Object sender, SqlRetryingEventArgs eventArgs) => { },
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testReturnEvents
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testSqlServerPersisterUtilities.Load(testGuid, new AccessManager<String, String, String, String>());
            });

            Assert.That(e.Message, Does.StartWith($"No EventIdToTransactionTimeMap rows were returned for EventId '00000000-0000-0000-0000-000000000001'."));
            Assert.AreEqual("eventId", e.ParamName);
        }

        #region Nested Classes

        /// <summary>
        /// Version of the <see cref="SqlServerPersisterUtilities{TUser, TGroup, TComponent, TAccess}"/> class where members are overridden to facilitate unit testing.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        /// <remarks>This class exists specifically to test the <see cref="SqlPersisterUtilitiesBase{TUser, TGroup, TComponent, TAccess}.Load(AccessManagerBase{TUser, TGroup, TComponent, TAccess})"/> method overloads.</remarks>
        private class SqlServerPersisterUtilitiesWithOverriddenMembers<TUser, TGroup, TComponent, TAccess> : SqlServerPersisterUtilities<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>Values to return from the ExecuteMultiResultQueryAndHandleException() method.</summary>
            protected IEnumerable<Tuple<Guid, DateTime, Int32>> executeMultiResultQueryMethodReturnValues;

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.UnitTests.SqlPersisterUtilitiesBaseTests+SqlServerPersisterUtilitiesWithOverriddenMembers class.
            /// </summary>
            /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
            /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
            /// <param name="sqlRetryLogicOption">The retry logic to use when connecting to and executing against the SQL Server database.</param>
            /// <param name="connectionRetryAction">The action to invoke if an action is retried due to a transient error.</param>
            /// <param name="userStringifier">A string converter for users.</param>
            /// <param name="groupStringifier">A string converter for groups.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.</param>
            /// <param name="executeMultiResultQueryMethodReturnValues">Values to return from the ExecuteMultiResultQueryAndHandleException() method.</param>
            public SqlServerPersisterUtilitiesWithOverriddenMembers
            (
                String connectionString,
                Int32 operationTimeout,
                SqlRetryLogicOption sqlRetryLogicOption,
                EventHandler<SqlRetryingEventArgs> connectionRetryAction,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                IEnumerable<Tuple<Guid, DateTime, Int32>> executeMultiResultQueryMethodReturnValues
            ) : base(connectionString, operationTimeout, sqlRetryLogicOption, connectionRetryAction, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
            {
                this.executeMultiResultQueryMethodReturnValues = executeMultiResultQueryMethodReturnValues;
            }

            public override IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteMultiResultQueryAndHandleException<TReturn1, TReturn2>
            (
                String query,
                String columnToConvert1,
                String columnToConvert2,
                Func<String, TReturn1> returnType1ConversionFromStringFunction,
                Func<String, TReturn2> returnType2ConversionFromStringFunction
            )
            {
                if (typeof(TReturn1) == typeof(DateTime) && typeof(TReturn2) == typeof(Int32))
                {
                    foreach (Tuple<Guid, DateTime, Int32> currentReturnTuple in executeMultiResultQueryMethodReturnValues)
                    {
                        yield return new Tuple<DateTime, Int32>(currentReturnTuple.Item2, currentReturnTuple.Item3) as Tuple<TReturn1, TReturn2>;
                    }
                }
                else
                {
                    throw new Exception("Call to method ExecuteMultiResultQueryAndHandleException() contained unexpected type parameters.");
                }
            }

            public override IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteMultiResultQueryAndHandleException<TReturn1, TReturn2, TReturn3>
            (
                String query,
                String columnToConvert1,
                String columnToConvert2,
                String columnToConvert3,
                Func<String, TReturn1> returnType1ConversionFromStringFunction,
                Func<String, TReturn2> returnType2ConversionFromStringFunction,
                Func<String, TReturn3> returnType3ConversionFromStringFunction
            )
            {
                if (typeof(TReturn1) == typeof(Guid) && typeof(TReturn2) == typeof(DateTime) && typeof(TReturn3) == typeof(Int32))
                {
                    return (IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>>)executeMultiResultQueryMethodReturnValues;
                }
                else
                {
                    throw new Exception("Call to method ExecuteMultiResultQueryAndHandleException() contained unexpected type parameters.");
                }
            }
        }

        #endregion
    }
}
