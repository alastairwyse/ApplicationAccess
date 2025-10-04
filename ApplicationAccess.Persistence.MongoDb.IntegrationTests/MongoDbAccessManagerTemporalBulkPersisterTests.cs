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
using System.Globalization;
using ApplicationAccess.Persistence.MongoDb.Models.Documents;
using EphemeralMongo;
using MongoDB.Driver;
using NUnit.Framework;

namespace ApplicationAccess.Persistence.MongoDb.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Persistence.MongoDb.MongoDbAccessManagerTemporalBulkPersister class.
    /// </summary>
    public class MongoDbAccessManagerTemporalBulkPersisterTests
    {
        #pragma warning disable 1591

        protected const String eventIdToTransactionTimeMapCollectionName = "EventIdToTransactionTimeMap";
        protected const String usersCollectionName = "Users";
        protected const String groupsCollectionName = "Groups";
        protected const String userToGroupMappingsCollectionName = "UserToGroupMappings";
        protected const String groupToGroupMappingsCollectionName = "GroupToGroupMappings";
        protected const String userToApplicationComponentAndAccessLevelMappingsCollectionName = "UserToApplicationComponentAndAccessLevelMappings";
        protected const String groupToApplicationComponentAndAccessLevelMappingsCollectionName = "GroupToApplicationComponentAndAccessLevelMappings";
        protected const String entityTypesCollectionName = "EntityTypes";
        protected const String entitiesCollectionName = "Entities";
        protected const String userToEntityMappingsCollectionName = "UserToEntityMappings";
        protected const String groupToEntityMappingsCollectionName = "GroupToEntityMappings";

        #pragma warning restore 1591

        private IMongoRunner mongoRunner;
        private IMongoClient mongoClient;
        private IMongoDatabase mongoDatabase;
        private IMongoCollection<EventIdToTransactionTimeMappingDocument> eventIdToTransactionTimeMapCollection;
        private IMongoCollection<UserDocument> usersCollection;
        private IMongoCollection<GroupDocument> groupsCollection;
        private IMongoCollection<UserToGroupMappingDocument> userToGroupMappingsCollection;
        private IMongoCollection<GroupToGroupMappingDocument> groupToGroupMappingsCollection;
        private IMongoCollection<UserToApplicationComponentAndAccessLevelMappingDocument> userToApplicationComponentAndAccessLevelMappingsCollection;
        private IMongoCollection<GroupToApplicationComponentAndAccessLevelMappingDocument> groupToApplicationComponentAndAccessLevelMappingsCollection;
        private IMongoCollection<EntityTypeDocument> entityTypesCollection;
        private IMongoCollection<EntityDocument> entitiesCollection;
        private IMongoCollection<UserToEntityMappingDocument> userToEntityMappingsCollection;
        private IMongoCollection<GroupToEntityMappingDocument> groupToEntityMappingsCollection;
        private MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String> testMongoDbAccessManagerTemporalBulkPersister;

        [OneTimeSetUp]
        protected void OneTimeSetUp()
        {
            mongoRunner = MongoRunner.Run();
            mongoClient = new MongoClient(mongoRunner.ConnectionString);
            mongoDatabase = mongoClient.GetDatabase("ApplicationAccess");
        }

        [SetUp]
        protected void SetUp()
        {
            mongoDatabase.DropCollection(eventIdToTransactionTimeMapCollectionName);
            mongoDatabase.DropCollection(usersCollectionName);
            mongoDatabase.DropCollection(groupsCollectionName);
            mongoDatabase.DropCollection(userToGroupMappingsCollectionName);
            mongoDatabase.DropCollection(groupToGroupMappingsCollectionName);
            mongoDatabase.DropCollection(userToApplicationComponentAndAccessLevelMappingsCollectionName);
            mongoDatabase.DropCollection(groupToApplicationComponentAndAccessLevelMappingsCollectionName);
            mongoDatabase.DropCollection(entityTypesCollectionName);
            mongoDatabase.DropCollection(entitiesCollectionName);
            mongoDatabase.DropCollection(userToEntityMappingsCollectionName);
            mongoDatabase.DropCollection(groupToEntityMappingsCollectionName);
            eventIdToTransactionTimeMapCollection = mongoDatabase.GetCollection<EventIdToTransactionTimeMappingDocument>(eventIdToTransactionTimeMapCollectionName);
            usersCollection = mongoDatabase.GetCollection<UserDocument>(usersCollectionName);
            groupsCollection = mongoDatabase.GetCollection<GroupDocument>(groupsCollectionName);
            userToGroupMappingsCollection = mongoDatabase.GetCollection<UserToGroupMappingDocument>(userToGroupMappingsCollectionName);
            groupToGroupMappingsCollection = mongoDatabase.GetCollection<GroupToGroupMappingDocument>(groupToGroupMappingsCollectionName);
            userToApplicationComponentAndAccessLevelMappingsCollection = mongoDatabase.GetCollection<UserToApplicationComponentAndAccessLevelMappingDocument>(userToApplicationComponentAndAccessLevelMappingsCollectionName);
            groupToApplicationComponentAndAccessLevelMappingsCollection = mongoDatabase.GetCollection<GroupToApplicationComponentAndAccessLevelMappingDocument>(groupToApplicationComponentAndAccessLevelMappingsCollectionName);
            entityTypesCollection = mongoDatabase.GetCollection<EntityTypeDocument>(entityTypesCollectionName);
            entitiesCollection = mongoDatabase.GetCollection<EntityDocument>(entitiesCollectionName);
            userToEntityMappingsCollection = mongoDatabase.GetCollection<UserToEntityMappingDocument>(userToEntityMappingsCollectionName);
            groupToEntityMappingsCollection = mongoDatabase.GetCollection<GroupToEntityMappingDocument>(groupToEntityMappingsCollectionName);
            testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
            (
                mongoRunner.ConnectionString,
                "ApplicationAccess",
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier()
            );
        }

        [TearDown]
        public void TearDown()
        {
            testMongoDbAccessManagerTemporalBulkPersister.Dispose();
        }

        [OneTimeTearDown]
        protected void OneTimeTearDown()
        {
            mongoClient.Dispose();
            mongoRunner.Dispose();
        }

        [Test]
        public void CreateEvent_TransactionTimeParameterLessThanLastTransactionTime()
        {
            List<EventIdToTransactionTimeMappingDocument> initialDocuments = new()
            {
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionSequence = 0 },
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionSequence = 1 },
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionSequence = 0 },
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionSequence = 0 }
            };
            eventIdToTransactionTimeMapCollection.InsertMany(initialDocuments);
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var transactionTime = CreateDataTimeFromString("2025-10-04 17:33:54.0000000");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'transactionTime' with value '2025-10-04 17:33:54.0000000' must be greater than or equal to last transaction time '2025-10-04 17:33:55.0000000'."));
            Assert.AreEqual("transactionTime", e.ParamName);
        }

        [Test]
        public void CreateEvent_CollectionEmpty()
        {
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var transactionTime = CreateDataTimeFromString("2025-10-04 17:33:54.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(eventId, transactionTime);

            List<EventIdToTransactionTimeMappingDocument> allDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty).ToList();
            Assert.AreEqual(1, allDocuments.Count);
            Assert.AreEqual(eventId, allDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allDocuments[0].TransactionTime);
            Assert.AreEqual(0, allDocuments[0].TransactionSequence);
        }

        [Test]
        public void CreateEvent_EventsExistWithSameTransactionTime()
        {
            List<EventIdToTransactionTimeMappingDocument> initialDocuments = new()
            {
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionSequence = 0 },
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionSequence = 1 }
            };
            eventIdToTransactionTimeMapCollection.InsertMany(initialDocuments);
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var transactionTime = CreateDataTimeFromString("2025-10-04 17:33:55.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(eventId, transactionTime);

            List<EventIdToTransactionTimeMappingDocument> allDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .SortBy(document => document.TransactionSequence)
                .ToList();
            Assert.AreEqual(3, allDocuments.Count);
            Assert.AreEqual(eventId, allDocuments[2].EventId);
            Assert.AreEqual(transactionTime, allDocuments[2].TransactionTime);
            Assert.AreEqual(2, allDocuments[2].TransactionSequence);
        }

        [Test]
        public void CreateEvent()
        {
            List<EventIdToTransactionTimeMappingDocument> initialDocuments = new()
            {
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionSequence = 0 },
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionSequence = 1 },
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionSequence = 0 },
                new EventIdToTransactionTimeMappingDocument() { EventId = Guid.NewGuid(), TransactionTime = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionSequence = 0 }
            };
            eventIdToTransactionTimeMapCollection.InsertMany(initialDocuments);
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var transactionTime = CreateDataTimeFromString("2025-10-04 17:33:56.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(eventId, transactionTime);

            List<EventIdToTransactionTimeMappingDocument> allDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .SortBy(document => document.TransactionTime)
                .ToList();
            Assert.AreEqual(5, allDocuments.Count);
            Assert.AreEqual(eventId, allDocuments[4].EventId);
            Assert.AreEqual(transactionTime, allDocuments[4].TransactionTime);
            Assert.AreEqual(0, allDocuments[4].TransactionSequence);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss.fffffff", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion

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

            #pragma warning disable 1591

            public new void CreateEvent(Guid eventId, DateTime transactionTime)
            {
                base.CreateEvent(eventId, transactionTime);
            }

            #pragma warning restore 1591
        }

        #endregion
    }
}
