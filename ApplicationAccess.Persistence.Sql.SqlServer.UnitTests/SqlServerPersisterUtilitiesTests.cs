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
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Utilities;
using NUnit.Framework;

namespace ApplicationAccess.Persistence.Sql.SqlServer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerPersisterUtilities class.
    /// </summary>
    public class SqlServerPersisterUtilitiesTests
    {
        private List<SqlRetryingEventArgs> connectionRetryActionInvocationParameters;
        private EventHandler<SqlRetryingEventArgs> connectionRetryAction;
        private List<Tuple<SqlConnection, SqlCommand, SessionDeadlockPriority>> prepareConnectionAndCommandMethodCallParameters;
        private List<Tuple<SqlConnection, SqlCommand>> teardownConnectionAndCommandMethodCallParameters;
        private SqlServerPersisterUtilitiesWithProtectedMethods testSqlServerPersisterUtilities;

        [SetUp]
        protected void SetUp()
        {
            connectionRetryActionInvocationParameters = new List<SqlRetryingEventArgs>();
            connectionRetryAction = (Object sender, SqlRetryingEventArgs eventArgs) =>
            {
                connectionRetryActionInvocationParameters.Add(eventArgs);
            };
            prepareConnectionAndCommandMethodCallParameters = new List<Tuple<SqlConnection, SqlCommand, SessionDeadlockPriority>>();
            teardownConnectionAndCommandMethodCallParameters = new List<Tuple<SqlConnection, SqlCommand>>();
            String testConnectionString = "Server=127.0.0.1;Database=ApplicationAccess;User Id=user;Password=pwd;Encrypt=false;Authentication=SqlPassword";
            var sqlRetryLogicOption = new SqlRetryLogicOption();
            sqlRetryLogicOption.NumberOfTries = 3;  
            sqlRetryLogicOption.MinTimeInterval = TimeSpan.FromSeconds(0);
            sqlRetryLogicOption.MaxTimeInterval = TimeSpan.FromSeconds(120);
            sqlRetryLogicOption.DeltaTime = TimeSpan.FromSeconds(5);
            Action<SqlConnection, SqlCommand, SessionDeadlockPriority> prepareConnectionAndCommandMethodAction = (sqlConnection, sqlCommand, sessionDeadlockPriority) =>
            {
                prepareConnectionAndCommandMethodCallParameters.Add(Tuple.Create(sqlConnection, sqlCommand, sessionDeadlockPriority));
            };
            Action< SqlConnection, SqlCommand > teardownConnectionAndCommandMethodAction = (sqlConnection, sqlCommand) =>
            {
                teardownConnectionAndCommandMethodCallParameters.Add(Tuple.Create(sqlConnection, sqlCommand));
            };
            testSqlServerPersisterUtilities = new SqlServerPersisterUtilitiesWithProtectedMethods
            (
                testConnectionString,
                0,
                sqlRetryLogicOption,
                connectionRetryAction,
                prepareConnectionAndCommandMethodAction,
                teardownConnectionAndCommandMethodAction
            );
        }

        [Test]
        public void ExecuteQueryAndConvertColumnWithDeadlockRetry()
        {
            var testQueryResults = new List<String>()
            {
                "User1",
                "User2",
                "User3"
            };
            Func<SqlCommand, List<String>> readAndConvertResultsFunction = (sqlCommand) =>
            {
                return testQueryResults;
            };

            List<String> result = testSqlServerPersisterUtilities.ExecuteQueryAndConvertColumnWithDeadlockRetry("SELECT 1;", readAndConvertResultsFunction);

            Assert.AreSame(testQueryResults, result);
            Assert.AreEqual(1, prepareConnectionAndCommandMethodCallParameters.Count);
            Assert.AreEqual(1, teardownConnectionAndCommandMethodCallParameters.Count);
        }

        [Test]
        public void ExecuteQueryAndConvertColumnWithDeadlockRetry_NonSqlExceptionWhileReading()
        {
            var mockException = new Exception("Unhandled exception");
            Func<SqlCommand, List<String>> readAndConvertResultsFunction = (sqlCommand) =>
            {
                throw mockException;
            };

            var e = Assert.Throws<Exception>(delegate
            {
                testSqlServerPersisterUtilities.ExecuteQueryAndConvertColumnWithDeadlockRetry("SELECT 1;", readAndConvertResultsFunction);
            });

            Assert.AreSame(mockException, e);
            Assert.AreEqual(0, connectionRetryActionInvocationParameters.Count);
            Assert.AreEqual(1, prepareConnectionAndCommandMethodCallParameters.Count);
            Assert.AreEqual(1, teardownConnectionAndCommandMethodCallParameters.Count);
        }

        [Test]
        public void ExecuteQueryAndConvertColumnWithDeadlockRetry_NonDeadlockSqlExceptionWhileReading()
        {
            SqlException sqlException = GetSqlException(53, "Host not found", 2);
            Func<SqlCommand, List<String>> readAndConvertResultsFunction = (sqlCommand) =>
            {
                throw sqlException;
            };

            var e = Assert.Throws<SqlException>(delegate
            {
                testSqlServerPersisterUtilities.ExecuteQueryAndConvertColumnWithDeadlockRetry("SELECT 1;", readAndConvertResultsFunction);
            });

            Assert.AreSame(sqlException, e);
            Assert.AreEqual(0, connectionRetryActionInvocationParameters.Count);
            Assert.AreEqual(1, prepareConnectionAndCommandMethodCallParameters.Count);
            Assert.AreEqual(1, teardownConnectionAndCommandMethodCallParameters.Count);
        }

        [Test]
        public void ExecuteQueryAndConvertColumnWithDeadlockRetry_DeadlockExceptionAndFailureOnAllRetries()
        {
            SqlException deadLockException = GetSqlException(1205, "Transaction (Process ID 123) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.", 2);
            Func<SqlCommand, List<String>> readAndConvertResultsFunction = (sqlCommand) =>
            {
                throw deadLockException;
            };

            var e = Assert.Throws<AggregateException>(delegate
            {
                testSqlServerPersisterUtilities.ExecuteQueryAndConvertColumnWithDeadlockRetry("SELECT 1;", readAndConvertResultsFunction);
            });

            Assert.That(e.Message, Does.StartWith($"The number of deadlock retries has exceeded the maximum of 3 attempt(s)."));
            Assert.AreEqual(3, e.InnerExceptions.Count);
            Assert.AreSame(deadLockException, e.InnerExceptions[0]);
            Assert.AreSame(deadLockException, e.InnerExceptions[1]);
            Assert.AreSame(deadLockException, e.InnerExceptions[2]);
            Assert.AreEqual(2, connectionRetryActionInvocationParameters.Count);
            Assert.AreEqual(0, connectionRetryActionInvocationParameters[0].Delay.Seconds);
            Assert.AreEqual(0, connectionRetryActionInvocationParameters[1].Delay.Seconds);
            Assert.AreEqual(1, connectionRetryActionInvocationParameters[0].RetryCount);
            Assert.AreEqual(2, connectionRetryActionInvocationParameters[1].RetryCount);
            // The 'Exceptions' property is a reference type, so we can only see the 'final' version of it (i.e. from the last invocation of 'connectionRetryAction')
            Assert.AreEqual(3, connectionRetryActionInvocationParameters[0].Exceptions.Count);
            Assert.AreEqual(3, connectionRetryActionInvocationParameters[1].Exceptions.Count);
            Assert.AreEqual(3, prepareConnectionAndCommandMethodCallParameters.Count);
            Assert.AreEqual(3, teardownConnectionAndCommandMethodCallParameters.Count);
        }

        [Test]
        public void ExecuteQueryAndConvertColumnWithDeadlockRetry_DeadlockExceptionAndSuccessOnFirstRetry()
        {
            SqlException deadLockException = GetSqlException(1205, "Transaction (Process ID 123) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction.", 2);
            var testQueryResults = new List<String>()
            {
                "User1",
                "User2",
                "User3"
            };
            Boolean firstCall = true;
            Func<SqlCommand, List<String>> readAndConvertResultsFunction = (sqlCommand) =>
            {
                if (firstCall == true)
                {
                    firstCall = false;
                    throw deadLockException;
                }
                else
                {
                    return testQueryResults;
                }
            };

            List<String> result = testSqlServerPersisterUtilities.ExecuteQueryAndConvertColumnWithDeadlockRetry("SELECT 1;", readAndConvertResultsFunction);

            Assert.AreSame(testQueryResults, result);
            Assert.AreEqual(1, connectionRetryActionInvocationParameters.Count);
            Assert.AreEqual(0, connectionRetryActionInvocationParameters[0].Delay.Seconds);
            Assert.AreEqual(1, connectionRetryActionInvocationParameters[0].RetryCount);
            Assert.AreEqual(1, connectionRetryActionInvocationParameters[0].Exceptions.Count);
            Assert.AreSame(deadLockException, connectionRetryActionInvocationParameters[0].Exceptions[0]);
            Assert.AreEqual(2, prepareConnectionAndCommandMethodCallParameters.Count);
            Assert.AreEqual(2, teardownConnectionAndCommandMethodCallParameters.Count);
        }

        #region Private/Protected Methods

        // Base of Below courtesy of https://blog.jonathanchannon.com/2014-01-02-unit-testing-with-sqlexception/ (required a few tweaks to get to the pass the right params to SqlError constructor)
        private SqlException GetSqlException(Int32 errorNumber, String errorMessage, Int32 constructorIndex)
        {
            SqlErrorCollection collection = ConstructObject<SqlErrorCollection>();
            var underlyingException = new Exception("Mock underlying deadlock exception");
            SqlError error = ConstructObject<SqlError>(errorNumber, (byte)56, (byte)13, "server name", errorMessage, "proc", 442, (uint)1, underlyingException);

            typeof(SqlErrorCollection)
                .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(collection, new object[] { error });

            var e = typeof(SqlException)
                .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.ExplicitThis, new[] { typeof(SqlErrorCollection), typeof(string) }, new ParameterModifier[] { })
                .Invoke(null, new object[] { collection, "11.0.0" }) as SqlException;

            return e;
        }

        private T ConstructObject<T>(params object[] parameters)
        {
            ConstructorInfo constructor = typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

            return (T)constructor.Invoke(parameters);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        ///  Version of the <see cref="SqlServerPersisterUtilities{TUser, TGroup, TComponent, TAccess}"/> class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        private class SqlServerPersisterUtilitiesWithProtectedMethods : SqlServerPersisterUtilities<String, String, String, String>
        {
            private Action<SqlConnection, SqlCommand, SessionDeadlockPriority> prepareConnectionAndCommandMethodAction;
            private Action<SqlConnection, SqlCommand> teardownConnectionAndCommandMethodAction;

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.UnitTests.SqlServerPersisterUtilitiesTests+SqlServerPersisterUtilitiesWithProtectedMethods class.
            /// </summary>
            /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
            /// <param name="operationTimeout">The timeout in seconds before terminating an operation against the SQL Server database.  A value of 0 indicates no limit.</param>
            /// <param name="sqlRetryLogicOption">The retry logic to use when connecting to and executing against the SQL Server database.</param>
            /// <param name="connectionRetryAction">The action to invoke if an action is retried due to a transient error.</param>
            /// <param name="prepareConnectionAndCommandMethodAction"></param>
            /// <param name="teardownConnectionAndCommandMethodAction"></param>
            public SqlServerPersisterUtilitiesWithProtectedMethods
            (
                String connectionString,
                Int32 operationTimeout,
                SqlRetryLogicOption sqlRetryLogicOption,
                EventHandler<SqlRetryingEventArgs> connectionRetryAction,
                Action<SqlConnection, SqlCommand, SessionDeadlockPriority> prepareConnectionAndCommandMethodAction,
                Action<SqlConnection, SqlCommand> teardownConnectionAndCommandMethodAction
            ) : base
            (
                connectionString, 
                operationTimeout, 
                sqlRetryLogicOption, 
                connectionRetryAction, 
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier()
            )
            {
                this.prepareConnectionAndCommandMethodAction = prepareConnectionAndCommandMethodAction;
                this.teardownConnectionAndCommandMethodAction = teardownConnectionAndCommandMethodAction;
            }

            /// <inheritdoc/>
            protected override void PrepareConnectionAndCommand(SqlConnection connection, SqlCommand command, SessionDeadlockPriority deadlockPriority)
            {
                prepareConnectionAndCommandMethodAction.Invoke(connection, command, deadlockPriority);
            }

            /// <inheritdoc/>
            protected override void TeardownConnectionAndCommand(SqlConnection connection, SqlCommand command)
            {
                teardownConnectionAndCommandMethodAction.Invoke(connection, command);
            }

            /// <summary>
            /// Executes a function which queries from a SQL Server database, catching any deadlock (<see href="https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/mssqlserver-1205-database-engine-error?view=sql-server-ver16">1205</see>) exceptions and retrying according to the retry count specified in the 'sqlRetryLogicOption' member.
            /// </summary>
            /// <typeparam name="T">The type of row data returned from the query function.</typeparam>
            /// <param name="query">The query to execute.</param>
            /// <param name="readAndConvertResultsFunction">The function which reads the results of the query and returns a list of results.  Accepts a single parameter which is the <see cref="SqlCommand"/> to use to execute the query and read the results, and returns a list of <typeparamref name="T"/>.</param>   
            /// <returns>An list of <typeparamref name="T"/> containing the results of the query.</returns>
            public new List<T> ExecuteQueryAndConvertColumnWithDeadlockRetry<T>(String query, Func<SqlCommand, List<T>> readAndConvertResultsFunction)
            {
                return base.ExecuteQueryAndConvertColumnWithDeadlockRetry(query, readAndConvertResultsFunction);
            }
        }

        #endregion
    }
}
