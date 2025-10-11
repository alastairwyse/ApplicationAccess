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
using System.Threading;
using EphemeralMongo;
using MongoDB.Driver;
using ApplicationAccess.Persistence.MongoDb.Models.Documents;
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

        protected readonly DateTime temporalMaxDate = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

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
        private MethodCallCountingStringUniqueStringifier userStringifier;
        private MethodCallCountingStringUniqueStringifier groupStringifier;
        private MethodCallCountingStringUniqueStringifier applicationComponentStringifier;
        private MethodCallCountingStringUniqueStringifier accessLevelStringifier;
        private MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String> testMongoDbAccessManagerTemporalBulkPersister;

        [OneTimeSetUp]
        protected void OneTimeSetUp()
        {
            var mongoRunnerOptions = new MongoRunnerOptions();
            mongoRunnerOptions.UseSingleNodeReplicaSet = true;
            mongoRunner = MongoRunner.Run(mongoRunnerOptions);
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
            userStringifier = new MethodCallCountingStringUniqueStringifier();
            groupStringifier = new MethodCallCountingStringUniqueStringifier();
            applicationComponentStringifier = new MethodCallCountingStringUniqueStringifier();
            accessLevelStringifier = new MethodCallCountingStringUniqueStringifier();
            testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
            (
                mongoRunner.ConnectionString,
                "ApplicationAccess",
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier
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
            DateTime transactionTime = CreateDataTimeFromString("2025-10-04 17:33:54.0000000");

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(null, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'transactionTime' with value '2025-10-04 17:33:54.0000000' must be greater than or equal to last transaction time '2025-10-04 17:33:55.0000000'."));
            Assert.AreEqual("transactionTime", e.ParamName);
        }

        [Test]
        public void CreateEvent_CollectionEmpty()
        {
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-04 17:33:54.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(null, eventId, transactionTime);

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
            DateTime transactionTime = CreateDataTimeFromString("2025-10-04 17:33:55.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(null, eventId, transactionTime);

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
            DateTime transactionTime = CreateDataTimeFromString("2025-10-04 17:33:56.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(null, eventId, transactionTime);

            List<EventIdToTransactionTimeMappingDocument> allDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .SortBy(document => document.TransactionTime)
                .ToList();
            Assert.AreEqual(5, allDocuments.Count);
            Assert.AreEqual(eventId, allDocuments[4].EventId);
            Assert.AreEqual(transactionTime, allDocuments[4].TransactionTime);
            Assert.AreEqual(0, allDocuments[4].TransactionSequence);
        }

        [Test]
        public void CreateEvent_WithTransaction()
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
            DateTime transactionTime = CreateDataTimeFromString("2025-10-04 17:33:56.0000000");

            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    testMongoDbAccessManagerTemporalBulkPersister.CreateEvent(s, eventId, transactionTime);
                    return new Object();
                });
            }

            List<EventIdToTransactionTimeMappingDocument> allDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .SortBy(document => document.TransactionTime)
                .ToList();
            Assert.AreEqual(5, allDocuments.Count);
            Assert.AreEqual(eventId, allDocuments[4].EventId);
            Assert.AreEqual(transactionTime, allDocuments[4].TransactionTime);
            Assert.AreEqual(0, allDocuments[4].TransactionSequence);
        }

        [Test]
        public void AddUser()
        {
            String user = "user1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-06 23:13:26.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.AddUser(null, user, eventId, transactionTime);

            List<UserDocument> allUserDocuments = usersCollection.Find(FilterDefinition<UserDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserDocuments.Count);
            Assert.AreEqual(user, allUserDocuments[0].User);
            Assert.AreEqual(transactionTime, allUserDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserWithTransaction()
        {
            String user = "user1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-06 23:13:26.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.AddUserWithTransaction(user, eventId, transactionTime);

            List<UserDocument> allUserDocuments = usersCollection.Find(FilterDefinition<UserDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserDocuments.Count);
            Assert.AreEqual(user, allUserDocuments[0].User);
            Assert.AreEqual(transactionTime, allUserDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUser_UserDoesntExist()
        {
            String user = "user1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-06 23:13:26.0000000");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveUser(null, user, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for user 'user1', and transaction time '2025-10-06 23:13:26.0000000'."));
        }

        [Test]
        public void RemoveUser()
        {
            List<UserToGroupMappingDocument> initialUserToGroupMappingDocuments = new()
            {
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToGroupMappingDocument() {  User = "user2", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToGroupMappingsCollection.InsertMany(initialUserToGroupMappingDocuments);
            List<UserToApplicationComponentAndAccessLevelMappingDocument> initialUserToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialUserToApplicationComponentAndAccessLevelMappingDocuments);
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocuments);
            List<UserDocument> initialUserDocuments = new()
            {
                new UserDocument() {  User = "user1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new UserDocument() {  User = "user2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserDocument() {  User = "user1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            usersCollection.InsertMany(initialUserDocuments);
            String user = "user1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-09 08:34:55.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUser(null, user, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserToGroupMappingDocuments[2].TransactionTo);
            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000002"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserToEntityMappingDocuments[2].TransactionTo);
            List<UserDocument> allUserDocuments = usersCollection.Find(FilterDefinition<UserDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allUserDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserDocuments[2].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserWithTransaction()
        {
            List<UserToGroupMappingDocument> initialUserToGroupMappingDocuments = new()
            {
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToGroupMappingDocument() {  User = "user2", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
            };
            userToGroupMappingsCollection.InsertMany(initialUserToGroupMappingDocuments);
            List<UserToApplicationComponentAndAccessLevelMappingDocument> initialUserToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
            };
            userToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialUserToApplicationComponentAndAccessLevelMappingDocuments);
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocuments);
            List<UserDocument> initialUserDocuments = new()
            {
                new UserDocument() {  User = "user1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new UserDocument() {  User = "user2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserDocument() {  User = "user1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
            };
            usersCollection.InsertMany(initialUserDocuments);
            String user = "user1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-09 08:34:55.0000000");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUserWithTransaction(user, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserToGroupMappingDocuments[2].TransactionTo);
            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000002"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserToEntityMappingDocuments[2].TransactionTo);
            List<UserDocument> allUserDocuments = usersCollection.Find(FilterDefinition<UserDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allUserDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 08:34:54.9999999"), allUserDocuments[2].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroup()
        {
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 22:32:33.0000001");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroup(null, group, eventId, transactionTime);

            List<GroupDocument> allGroupDocuments = groupsCollection.Find(FilterDefinition<GroupDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allGroupDocuments.Count);
            Assert.AreEqual(group, allGroupDocuments[0].Group);
            Assert.AreEqual(transactionTime, allGroupDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allGroupDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupWithTransaction()
        {
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 22:32:33.0000001");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroupWithTransaction(group, eventId, transactionTime);

            List<GroupDocument> allGroupDocuments = groupsCollection.Find(FilterDefinition<GroupDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allGroupDocuments.Count);
            Assert.AreEqual(group, allGroupDocuments[0].Group);
            Assert.AreEqual(transactionTime, allGroupDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allGroupDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroup_GroupDoesntExist()
        {
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed33");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 06:30:51.0000002");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveGroup(null, group, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for group 'group1', and transaction time '2025-10-11 06:30:51.0000002'."));
        }

        [Test]
        public void RemoveGroup()
        {
            List<UserToGroupMappingDocument> initialUserToGroupMappingDocuments = new()
            {
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToGroupMappingsCollection.InsertMany(initialUserToGroupMappingDocuments);
            List<GroupToGroupMappingDocument> initialGroupToGroupMappingDocuments = new()
            {
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new GroupToGroupMappingDocument() {  FromGroup = "group3", ToGroup = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.5000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new GroupToGroupMappingDocument() {  FromGroup = "group4", ToGroup = "group5", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToGroupMappingDocument() {  FromGroup = "group3", ToGroup = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.5000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToGroupMappingsCollection.InsertMany(initialGroupToGroupMappingDocuments);
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> initialGroupToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialGroupToApplicationComponentAndAccessLevelMappingDocuments);
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000004")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocuments);
            List<GroupDocument> initialGroupDocuments = new()
            {
                new GroupDocument() {  Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000005")},
                new GroupDocument() {  Group = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupDocument() {  Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupsCollection.InsertMany(initialGroupDocuments);
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed33");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 06:30:51.0000003");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroup(null, group, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allUserToGroupMappingDocuments[2].TransactionTo);
            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allGroupToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000002"), allGroupToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToGroupMappingDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToGroupMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToGroupMappingDocuments[4].TransactionTo);
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments =groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000004"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToEntityMappingDocuments[2].TransactionTo);
            List<GroupDocument> allGroupsDocuments = groupsCollection.Find(FilterDefinition<GroupDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupsDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000005"), allGroupsDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupsDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupsDocuments[2].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }
        
        [Test]
        public void RemoveGroupWithTransaction()
        {
            List<UserToGroupMappingDocument> initialUserToGroupMappingDocuments = new()
            {
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToGroupMappingsCollection.InsertMany(initialUserToGroupMappingDocuments);
            List<GroupToGroupMappingDocument> initialGroupToGroupMappingDocuments = new()
            {
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new GroupToGroupMappingDocument() {  FromGroup = "group3", ToGroup = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.5000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new GroupToGroupMappingDocument() {  FromGroup = "group4", ToGroup = "group5", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToGroupMappingDocument() {  FromGroup = "group3", ToGroup = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.5000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToGroupMappingsCollection.InsertMany(initialGroupToGroupMappingDocuments);
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> initialGroupToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialGroupToApplicationComponentAndAccessLevelMappingDocuments);
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000004")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "Clients", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocuments);
            List<GroupDocument> initialGroupDocuments = new()
            {
                new GroupDocument() {  Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000005")},
                new GroupDocument() {  Group = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupDocument() {  Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupsCollection.InsertMany(initialGroupDocuments);
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed33");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 06:30:51.0000003");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupWithTransaction(group, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allUserToGroupMappingDocuments[2].TransactionTo);
            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allGroupToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000002"), allGroupToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToGroupMappingDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToGroupMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToGroupMappingDocuments[4].TransactionTo);
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000004"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupToEntityMappingDocuments[2].TransactionTo);
            List<GroupDocument> allGroupsDocuments = groupsCollection.Find(FilterDefinition<GroupDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupsDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000005"), allGroupsDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupsDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 06:30:51.0000002"), allGroupsDocuments[2].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }
           
        [Test]
        public void AddUserToGroupMapping()
        {
            String user = "user1";
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed34");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:12:37.0000008");

            testMongoDbAccessManagerTemporalBulkPersister.AddUserToGroupMapping(null, user, group, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(user, allUserToGroupMappingDocuments[0].User);
            Assert.AreEqual(group, allUserToGroupMappingDocuments[0].Group);
            Assert.AreEqual(transactionTime, allUserToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserToGroupMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToGroupMappingWithTransaction()
        {
            String user = "user1";
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed34");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:12:37.0000008");

            testMongoDbAccessManagerTemporalBulkPersister.AddUserToGroupMappingWithTransaction(user, group, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(user, allUserToGroupMappingDocuments[0].User);
            Assert.AreEqual(group, allUserToGroupMappingDocuments[0].Group);
            Assert.AreEqual(transactionTime, allUserToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserToGroupMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToGroupMapping_UserToGroupMappingDoesntExist()
        {
            String user = "user1";
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed35");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000009");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToGroupMapping(null, user, group, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for user 'user1', group 'group1', and transaction time '2025-10-11 09:26:31.0000009'."));
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            List<UserToGroupMappingDocument> initialUserToGroupMappingDocuments = new()
            {
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToGroupMappingDocument() {  User = "user2", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToGroupMappingsCollection.InsertMany(initialUserToGroupMappingDocuments);
            String user = "user1";
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed35");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000009");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToGroupMapping(null, user, group, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 09:26:31.0000008"), allUserToGroupMappingDocuments[2].TransactionTo);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToGroupMappingWithTransaction()
        {
            List<UserToGroupMappingDocument> initialUserToGroupMappingDocuments = new()
            {
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToGroupMappingDocument() {  User = "user2", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToGroupMappingDocument() {  User = "user1", Group = "group1", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToGroupMappingsCollection.InsertMany(initialUserToGroupMappingDocuments);
            String user = "user1";
            String group = "group1";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed35");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000009");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToGroupMappingWithTransaction(user, group, eventId, transactionTime);

            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 09:26:31.0000008"), allUserToGroupMappingDocuments[2].TransactionTo);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            String fromGroup = "group1";
            String toGroup = "group2";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed36");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:12:37.0000010");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroupToGroupMapping(null, fromGroup, toGroup, eventId, transactionTime);

            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual(fromGroup, allGroupToGroupMappingDocuments[0].FromGroup);
            Assert.AreEqual(toGroup, allGroupToGroupMappingDocuments[0].ToGroup);
            Assert.AreEqual(transactionTime, allGroupToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allGroupToGroupMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToGroupMappingWithTransaction()
        {
            String fromGroup = "group1";
            String toGroup = "group2";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed36");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:12:37.0000010");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroupToGroupMappingWithTransaction(fromGroup, toGroup, eventId, transactionTime);

            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual(fromGroup, allGroupToGroupMappingDocuments[0].FromGroup);
            Assert.AreEqual(toGroup, allGroupToGroupMappingDocuments[0].ToGroup);
            Assert.AreEqual(transactionTime, allGroupToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allGroupToGroupMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToGroupMapping_UserToGroupMappingDoesntExist()
        {
            String fromGroup = "group1";
            String toGroup = "group2";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed37");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000011");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToGroupMapping(null, fromGroup, toGroup, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for from group 'group1', to group 'group2', and transaction time '2025-10-11 09:26:31.0000011'."));
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            List<GroupToGroupMappingDocument> initialGroupToGroupMappingDocuments = new()
            {
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new GroupToGroupMappingDocument() {  FromGroup = "group3", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToGroupMappingsCollection.InsertMany(initialGroupToGroupMappingDocuments);
            String fromGroup = "group1";
            String toGroup = "group2";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed37");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000011");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToGroupMapping(null, fromGroup, toGroup, eventId, transactionTime);

            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allGroupToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 09:26:31.0000010"), allGroupToGroupMappingDocuments[2].TransactionTo);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToGroupMappingWithTransaction()
        {
            List<GroupToGroupMappingDocument> initialGroupToGroupMappingDocuments = new()
            {
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new GroupToGroupMappingDocument() {  FromGroup = "group3", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToGroupMappingDocument() {  FromGroup = "group1", ToGroup = "group2", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToGroupMappingsCollection.InsertMany(initialGroupToGroupMappingDocuments);
            String fromGroup = "group1";
            String toGroup = "group2";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed37");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000011");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToGroupMappingWithTransaction(fromGroup, toGroup, eventId, transactionTime);

            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(3, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allGroupToGroupMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToGroupMappingDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 09:26:31.0000010"), allGroupToGroupMappingDocuments[2].TransactionTo);
            Assert.AreEqual(2, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMappingWithTransaction()
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// Subtracts the most granular supported time unit in the data's temporal model from the specified <see cref="DateTime" />.
        /// </summary>
        /// <param name="inputDateTime">The <see cref="DateTime" /> to subtract from.</param>
        /// <returns>The <see cref="DateTime" /> after the subtraction.</returns>
        protected DateTime SubtractTemporalMinimumTimeUnit(DateTime inputDateTime)
        {
            return inputDateTime.Subtract(TimeSpan.FromMicroseconds(0.1));
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

            public new void CreateEvent(IClientSessionHandle session, Guid eventId, DateTime transactionTime)
            {
                base.CreateEvent(session, eventId, transactionTime);
            }

            public new void AddUser(IClientSessionHandle session, TUser user, Guid eventId, DateTime transactionTime)
            {
                base.AddUser(session, user, eventId, transactionTime);
            }

            public new void AddUserWithTransaction(TUser user, Guid eventId, DateTime transactionTime)
            {
                base.AddUserWithTransaction(user, eventId, transactionTime);
            }

            public new void RemoveUser(IClientSessionHandle session, TUser user, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUser(session, user, eventId, transactionTime);
            }

            public new void RemoveUserWithTransaction(TUser user, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUserWithTransaction(user, eventId, transactionTime);
            }

            public new void AddGroup(IClientSessionHandle session, TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.AddGroup(session, group, eventId, transactionTime);
            }

            public new void AddGroupWithTransaction(TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.AddGroupWithTransaction(group, eventId, transactionTime);
            }

            public new void RemoveGroup(IClientSessionHandle session, TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroup(session, group, eventId, transactionTime);
            }

            public new void RemoveGroupWithTransaction(TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroupWithTransaction(group, eventId, transactionTime);
            }

            public new void AddUserToGroupMapping(IClientSessionHandle session, TUser user, TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.AddUserToGroupMapping(session, user, group, eventId, transactionTime);
            }

            public new void AddUserToGroupMappingWithTransaction(TUser user, TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.AddUserToGroupMappingWithTransaction(user, group, eventId, transactionTime);
            }

            public new void RemoveUserToGroupMapping(IClientSessionHandle session, TUser user, TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUserToGroupMapping(session, user, group, eventId, transactionTime);
            }

            public new void RemoveUserToGroupMappingWithTransaction(TUser user, TGroup group, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUserToGroupMappingWithTransaction(user, group, eventId, transactionTime);
            }

            public new void AddGroupToGroupMapping(IClientSessionHandle session, TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
            {
                base.AddGroupToGroupMapping(session, fromGroup, toGroup, eventId, transactionTime);
            }

            public new void AddGroupToGroupMappingWithTransaction(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
            {
                base.AddGroupToGroupMappingWithTransaction(fromGroup, toGroup, eventId, transactionTime);
            }

            public new void RemoveGroupToGroupMapping(IClientSessionHandle session, TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroupToGroupMapping(session, fromGroup, toGroup, eventId, transactionTime);
            }

            public new void RemoveGroupToGroupMappingWithTransaction(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroupToGroupMappingWithTransaction(fromGroup, toGroup, eventId, transactionTime);
            }

            public new void AddUserToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.AddUserToApplicationComponentAndAccessLevelMapping(session, user, applicationComponent, accessLevel, eventId, transactionTime);
            }

            public new void AddUserToApplicationComponentAndAccessLevelMappingWithTransaction(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.AddUserToApplicationComponentAndAccessLevelMappingWithTransaction(user, applicationComponent, accessLevel, eventId, transactionTime);
            }

            #pragma warning restore 1591
        }

        /// <summary>
        /// Implementation of <see cref="IUniqueStringifier{T}"/> which counts the number of calls to the FromString() and ToString() methods.
        /// </summary>
        private class MethodCallCountingStringUniqueStringifier : IUniqueStringifier<String>
        {
            public Int32 FromStringCallCount { get; protected set; }
            public Int32 ToStringCallCount { get; protected set; }

            public MethodCallCountingStringUniqueStringifier()
            {
                Reset();
            }

            /// <inheritdoc/>
            public String FromString(String stringifiedObject)
            {
                FromStringCallCount++;

                return stringifiedObject;
            }

            /// <inheritdoc/>
            public String ToString(String inputObject)
            {
                ToStringCallCount++;

                return inputObject;
            }

            /// <summary>
            /// Resets the method call counts to 0.
            /// </summary>
            public void Reset()
            {
                FromStringCallCount = 0;
                ToStringCallCount = 0;
            }
        }

        #endregion
    }
}
