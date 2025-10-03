/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using MongoDB.Driver;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Persistence.MongoDb.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.MongoDb.MongoDbAccessManagerTemporalBulkPersister class.
    /// </summary>
    public class MongoDbAccessManagerTemporalBulkPersisterTests
    {
        private IMongoCollectionShim mockMongoCollectionShim;
        private MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String> testMongoDbAccessManagerTemporalBulkPersister;

        [SetUp]
        protected void SetUp()
        {
            mockMongoCollectionShim = Substitute.For<IMongoCollectionShim>();
            testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
            (
                "mongodb://testServer:27017", 
                "ApplicationAccess",
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                mockMongoCollectionShim
            );
        }

        [Test]
        public void Constructor_ConnectionStringNull()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
                (
                    null,
                    "ApplicationAccess",
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void Constructor_ConnectionStringWhitespace()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
                (
                    " ",
                    "ApplicationAccess",
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'connectionString' must contain a value."));
            Assert.AreEqual("connectionString", e.ParamName);
        }

        [Test]
        public void Constructor_DatabaseNameNull()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
                (
                    "mongodb://testServer:27017",
                    null,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'databaseName' must contain a value."));
            Assert.AreEqual("databaseName", e.ParamName);
        }

        [Test]
        public void Constructor_DatabaseNameWhitespace()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
                (
                    "mongodb://testServer:27017",
                    " ",
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'databaseName' must contain a value."));
            Assert.AreEqual("databaseName", e.ParamName);
        }

        [Test]
        public void CreateEvent_TransactionTimeParameterLessThanLastTransactionTime()
        {
            throw new NotImplementedException();
        }

        #region Nested Classes

        /// <summary>
        /// Version of the <see cref="MongoDbAccessManagerTemporalBulkPersister{TUser, TGroup, TComponent, TAccess}""> class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : MongoDbAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.MongoDb.UnitTests.MongoDbAccessManagerTemporalBulkPersisterTests+MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers class.
            /// </summary>
            /// <param name="connectionString">The string to use to connect to the MongoDB database.</param>
            /// <param name="databaseName">The name of the database.</param>
            /// <param name="userStringifier">A string converter for users.</param>
            /// <param name="groupStringifier">A string converter for groups.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.</param>
            public MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers
            (
                String connectionString,
                String databaseName,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier
            ) : base(connectionString, databaseName, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.MongoDb.UnitTests.MongoDbAccessManagerTemporalBulkPersisterTests+MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers class.
            /// </summary>
            /// <param name="connectionString">The string to use to connect to the MongoDB database.</param>
            /// <param name="databaseName">The name of the database.</param>
            /// <param name="userStringifier">A string converter for users.</param>
            /// <param name="groupStringifier">A string converter for groups.</param>
            /// <param name="applicationComponentStringifier">A string converter for application components.</param>
            /// <param name="accessLevelStringifier">A string converter for access levels.</param>
            /// <param name="mongoCollectionShim">Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to <see cref="IMongoCollection{TDocument}"/> instances.</param>
            /// <remarks>This constructor is included to facilitate unit testing.</remarks>
            public MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers
            (
                String connectionString,
                String databaseName,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                IMongoCollectionShim mongoCollectionShim
            ) : base(connectionString, databaseName, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, mongoCollectionShim)
            {
            }

            #pragma warning disable 1591

            public new void CreateEvent(Guid eventId, DateTime transactionTime, IMongoDatabase database)
            {
                base.CreateEvent(eventId, transactionTime, database);
            }

            #pragma warning restore 1591
        }

        #endregion
    }
}
