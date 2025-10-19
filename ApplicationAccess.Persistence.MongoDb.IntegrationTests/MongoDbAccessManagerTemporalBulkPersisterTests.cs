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
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Persistence.MongoDb.Models.Documents;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

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

        private IApplicationLogger logger;
        private IMetricLogger metricLogger;
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
            logger = Substitute.For<IApplicationLogger>();
            metricLogger = Substitute.For<IMetricLogger>();
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
                accessLevelStringifier, 
                false, 
                logger, 
                metricLogger
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
        public void PersistEvents()
        {
            List<TemporalEventBufferItemBase> testEvents = new()
            {
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000000"), EventAction.Add, "user1", CreateDataTimeFromString("2025-10-18 08:42:00.0000000"), 0),
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000001"), EventAction.Remove, "user1", CreateDataTimeFromString("2025-10-18 08:42:01.0000000"), 1),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000002"), EventAction.Add, "group1", CreateDataTimeFromString("2025-10-18 08:42:02.0000000"), 2),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000003"), EventAction.Remove, "group1", CreateDataTimeFromString("2025-10-18 08:42:03.0000000"), 3),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000004"), EventAction.Add, "user1", "group1", CreateDataTimeFromString("2025-10-18 08:42:04.0000000"), 4),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000005"), EventAction.Remove, "user1", "group1", CreateDataTimeFromString("2025-10-18 08:42:05.0000000"), 5),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000006"), EventAction.Add, "group1", "group2", CreateDataTimeFromString("2025-10-18 08:42:06.0000000"), 6),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000007"), EventAction.Remove, "group1", "group2", CreateDataTimeFromString("2025-10-18 08:42:07.0000000"), 7),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000008"), EventAction.Add, "user1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:08.0000000"), 8),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000009"), EventAction.Remove, "user1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:09.0000000"), 9),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000010"), EventAction.Add, "group1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:10.0000000"), 10),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000011"), EventAction.Remove, "group1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:11.0000000"), 11),
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000012"), EventAction.Add, "ClientAccount", CreateDataTimeFromString("2025-10-18 08:42:12.0000000"), 12),
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000013"), EventAction.Remove, "ClientAccount", CreateDataTimeFromString("2025-10-18 08:42:13.0000000"), 13),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000014"), EventAction.Add, "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:14.0000000"), 14),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000015"), EventAction.Remove, "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:15.0000000"), 15),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000016"), EventAction.Add, "user1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:16.0000000"), 16),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000017"), EventAction.Remove, "user1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:17.0000000"), 17),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000018"), EventAction.Add, "group1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:18.0000000"), 18),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000019"), EventAction.Remove, "group1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:19.0000000"), 19)
            };

            testMongoDbAccessManagerTemporalBulkPersister.PersistEvents(testEvents);

            List<UserDocument> allUserDocuments = usersCollection.Find(FilterDefinition<UserDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserDocuments.Count);
            Assert.AreEqual("user1", allUserDocuments[0].User);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:00.0000000"), allUserDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:00.9999999"), allUserDocuments[0].TransactionTo);
            List<GroupDocument> allGroupDocuments = groupsCollection.Find(FilterDefinition<GroupDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupDocuments.Count);
            Assert.AreEqual("group1", allGroupDocuments[0].Group);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:02.0000000"), allGroupDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:02.9999999"), allGroupDocuments[0].TransactionTo);
            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToGroupMappingDocuments[0].User);
            Assert.AreEqual("group1", allUserToGroupMappingDocuments[0].Group);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:04.0000000"), allUserToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:04.9999999"), allUserToGroupMappingDocuments[0].TransactionTo);
            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToGroupMappingDocuments[0].FromGroup);
            Assert.AreEqual("group2", allGroupToGroupMappingDocuments[0].ToGroup);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:06.0000000"), allGroupToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:06.9999999"), allGroupToGroupMappingDocuments[0].TransactionTo);
            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].User);
            Assert.AreEqual("OrderScreen", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual("Create", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:08.0000000"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:08.9999999"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].Group);
            Assert.AreEqual("OrderScreen", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual("Create", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:10.0000000"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:10.9999999"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<EntityTypeDocument> allEntityTypeDocuments = entityTypesCollection.Find(FilterDefinition<EntityTypeDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allEntityTypeDocuments.Count);
            Assert.AreEqual("ClientAccount", allEntityTypeDocuments[0].EntityType);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:12.0000000"), allEntityTypeDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:12.9999999"), allEntityTypeDocuments[0].TransactionTo);
            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allEntityDocuments.Count);
            Assert.AreEqual("ClientAccount", allEntityDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allEntityDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:14.0000000"), allEntityDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:14.9999999"), allEntityDocuments[0].TransactionTo);
            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToEntityMappingDocuments[0].User);
            Assert.AreEqual("ClientAccount", allUserToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allUserToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:16.0000000"), allUserToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:16.9999999"), allUserToEntityMappingDocuments[0].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToEntityMappingDocuments[0].Group);
            Assert.AreEqual("ClientAccount", allGroupToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allGroupToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:18.0000000"), allGroupToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:18.9999999"), allGroupToEntityMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventIdToTransactionTimeMappingDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .SortBy(document => document.TransactionSequence)
                .ToList();
            Assert.AreEqual(20, allEventIdToTransactionTimeMappingDocuments.Count);
        }

        [Test]
        public void PersistEvents_IgnorePreExistingEventsParameterTrue()
        {
            List<TemporalEventBufferItemBase> testEvents = new()
            {
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000000"), EventAction.Add, "user1", CreateDataTimeFromString("2025-10-18 08:42:00.0000000"), 0),
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000001"), EventAction.Remove, "user1", CreateDataTimeFromString("2025-10-18 08:42:01.0000000"), 1),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000002"), EventAction.Add, "group1", CreateDataTimeFromString("2025-10-18 08:42:02.0000000"), 2),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000003"), EventAction.Remove, "group1", CreateDataTimeFromString("2025-10-18 08:42:03.0000000"), 3),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000004"), EventAction.Add, "user1", "group1", CreateDataTimeFromString("2025-10-18 08:42:04.0000000"), 4),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000005"), EventAction.Remove, "user1", "group1", CreateDataTimeFromString("2025-10-18 08:42:05.0000000"), 5),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000006"), EventAction.Add, "group1", "group2", CreateDataTimeFromString("2025-10-18 08:42:06.0000000"), 6),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000007"), EventAction.Remove, "group1", "group2", CreateDataTimeFromString("2025-10-18 08:42:07.0000000"), 7),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000008"), EventAction.Add, "user1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:08.0000000"), 8),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000009"), EventAction.Remove, "user1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:09.0000000"), 9),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000010"), EventAction.Add, "group1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:10.0000000"), 10),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000011"), EventAction.Remove, "group1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:11.0000000"), 11),
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000012"), EventAction.Add, "ClientAccount", CreateDataTimeFromString("2025-10-18 08:42:12.0000000"), 12),
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000013"), EventAction.Remove, "ClientAccount", CreateDataTimeFromString("2025-10-18 08:42:13.0000000"), 13),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000014"), EventAction.Add, "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:14.0000000"), 14),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000015"), EventAction.Remove, "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:15.0000000"), 15),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000016"), EventAction.Add, "user1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:16.0000000"), 16),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000017"), EventAction.Remove, "user1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:17.0000000"), 17),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000018"), EventAction.Add, "group1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:18.0000000"), 18),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000019"), EventAction.Remove, "group1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:19.0000000"), 19)
            };
            testMongoDbAccessManagerTemporalBulkPersister.PersistEvents(testEvents);

            testMongoDbAccessManagerTemporalBulkPersister.PersistEvents(testEvents, true);

            List<UserDocument> allUserDocuments = usersCollection.Find(FilterDefinition<UserDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserDocuments.Count);
            Assert.AreEqual("user1", allUserDocuments[0].User);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:00.0000000"), allUserDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:00.9999999"), allUserDocuments[0].TransactionTo);
            List<GroupDocument> allGroupDocuments = groupsCollection.Find(FilterDefinition<GroupDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupDocuments.Count);
            Assert.AreEqual("group1", allGroupDocuments[0].Group);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:02.0000000"), allGroupDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:02.9999999"), allGroupDocuments[0].TransactionTo);
            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToGroupMappingDocuments[0].User);
            Assert.AreEqual("group1", allUserToGroupMappingDocuments[0].Group);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:04.0000000"), allUserToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:04.9999999"), allUserToGroupMappingDocuments[0].TransactionTo);
            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToGroupMappingDocuments[0].FromGroup);
            Assert.AreEqual("group2", allGroupToGroupMappingDocuments[0].ToGroup);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:06.0000000"), allGroupToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:06.9999999"), allGroupToGroupMappingDocuments[0].TransactionTo);
            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].User);
            Assert.AreEqual("OrderScreen", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual("Create", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:08.0000000"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:08.9999999"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].Group);
            Assert.AreEqual("OrderScreen", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual("Create", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:10.0000000"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:10.9999999"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<EntityTypeDocument> allEntityTypeDocuments = entityTypesCollection.Find(FilterDefinition<EntityTypeDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allEntityTypeDocuments.Count);
            Assert.AreEqual("ClientAccount", allEntityTypeDocuments[0].EntityType);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:12.0000000"), allEntityTypeDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:12.9999999"), allEntityTypeDocuments[0].TransactionTo);
            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allEntityDocuments.Count);
            Assert.AreEqual("ClientAccount", allEntityDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allEntityDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:14.0000000"), allEntityDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:14.9999999"), allEntityDocuments[0].TransactionTo);
            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToEntityMappingDocuments[0].User);
            Assert.AreEqual("ClientAccount", allUserToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allUserToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:16.0000000"), allUserToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:16.9999999"), allUserToEntityMappingDocuments[0].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToEntityMappingDocuments[0].Group);
            Assert.AreEqual("ClientAccount", allGroupToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allGroupToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:18.0000000"), allGroupToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:18.9999999"), allGroupToEntityMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventIdToTransactionTimeMappingDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .SortBy(document => document.TransactionSequence)
                .ToList();
            Assert.AreEqual(20, allEventIdToTransactionTimeMappingDocuments.Count);
        }

        [Test]
        public void PersistEvents_WithTransaction()
        {
            testMongoDbAccessManagerTemporalBulkPersister.Dispose();
            testMongoDbAccessManagerTemporalBulkPersister = new MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers<String, String, String, String>
            (
                mongoRunner.ConnectionString,
                "ApplicationAccess",
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier,
                true, 
                logger,
                metricLogger
            );
            List<TemporalEventBufferItemBase> testEvents = new()
            {
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000000"), EventAction.Add, "user1", CreateDataTimeFromString("2025-10-18 08:42:00.0000000"), 0),
                new UserEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000001"), EventAction.Remove, "user1", CreateDataTimeFromString("2025-10-18 08:42:01.0000000"), 1),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000002"), EventAction.Add, "group1", CreateDataTimeFromString("2025-10-18 08:42:02.0000000"), 2),
                new GroupEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000003"), EventAction.Remove, "group1", CreateDataTimeFromString("2025-10-18 08:42:03.0000000"), 3),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000004"), EventAction.Add, "user1", "group1", CreateDataTimeFromString("2025-10-18 08:42:04.0000000"), 4),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.Parse("00000000-0000-0000-0000-000000000005"), EventAction.Remove, "user1", "group1", CreateDataTimeFromString("2025-10-18 08:42:05.0000000"), 5),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000006"), EventAction.Add, "group1", "group2", CreateDataTimeFromString("2025-10-18 08:42:06.0000000"), 6),
                new GroupToGroupMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000007"), EventAction.Remove, "group1", "group2", CreateDataTimeFromString("2025-10-18 08:42:07.0000000"), 7),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000008"), EventAction.Add, "user1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:08.0000000"), 8),
                new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000009"), EventAction.Remove, "user1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:09.0000000"), 9),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000010"), EventAction.Add, "group1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:10.0000000"), 10),
                new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>(Guid.Parse("00000000-0000-0000-0000-000000000011"), EventAction.Remove, "group1", "OrderScreen", "Create", CreateDataTimeFromString("2025-10-18 08:42:11.0000000"), 11),
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000012"), EventAction.Add, "ClientAccount", CreateDataTimeFromString("2025-10-18 08:42:12.0000000"), 12),
                new EntityTypeEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000013"), EventAction.Remove, "ClientAccount", CreateDataTimeFromString("2025-10-18 08:42:13.0000000"), 13),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000014"), EventAction.Add, "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:14.0000000"), 14),
                new EntityEventBufferItem(Guid.Parse("00000000-0000-0000-0000-000000000015"), EventAction.Remove, "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:15.0000000"), 15),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000016"), EventAction.Add, "user1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:16.0000000"), 16),
                new UserToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000017"), EventAction.Remove, "user1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:17.0000000"), 17),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000018"), EventAction.Add, "group1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:18.0000000"), 18),
                new GroupToEntityMappingEventBufferItem<String>(Guid.Parse("00000000-0000-0000-0000-000000000019"), EventAction.Remove, "group1", "ClientAccount", "CompanyA", CreateDataTimeFromString("2025-10-18 08:42:19.0000000"), 19)
            };

            testMongoDbAccessManagerTemporalBulkPersister.PersistEvents(testEvents);

            List<UserDocument> allUserDocuments = usersCollection.Find(FilterDefinition<UserDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserDocuments.Count);
            Assert.AreEqual("user1", allUserDocuments[0].User);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:00.0000000"), allUserDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:00.9999999"), allUserDocuments[0].TransactionTo);
            List<GroupDocument> allGroupDocuments = groupsCollection.Find(FilterDefinition<GroupDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupDocuments.Count);
            Assert.AreEqual("group1", allGroupDocuments[0].Group);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:02.0000000"), allGroupDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:02.9999999"), allGroupDocuments[0].TransactionTo);
            List<UserToGroupMappingDocument> allUserToGroupMappingDocuments = userToGroupMappingsCollection.Find(FilterDefinition<UserToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToGroupMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToGroupMappingDocuments[0].User);
            Assert.AreEqual("group1", allUserToGroupMappingDocuments[0].Group);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:04.0000000"), allUserToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:04.9999999"), allUserToGroupMappingDocuments[0].TransactionTo);
            List<GroupToGroupMappingDocument> allGroupToGroupMappingDocuments = groupToGroupMappingsCollection.Find(FilterDefinition<GroupToGroupMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToGroupMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToGroupMappingDocuments[0].FromGroup);
            Assert.AreEqual("group2", allGroupToGroupMappingDocuments[0].ToGroup);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:06.0000000"), allGroupToGroupMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:06.9999999"), allGroupToGroupMappingDocuments[0].TransactionTo);
            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].User);
            Assert.AreEqual("OrderScreen", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual("Create", allUserToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:08.0000000"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:08.9999999"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].Group);
            Assert.AreEqual("OrderScreen", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual("Create", allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:10.0000000"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:10.9999999"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<EntityTypeDocument> allEntityTypeDocuments = entityTypesCollection.Find(FilterDefinition<EntityTypeDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allEntityTypeDocuments.Count);
            Assert.AreEqual("ClientAccount", allEntityTypeDocuments[0].EntityType);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:12.0000000"), allEntityTypeDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:12.9999999"), allEntityTypeDocuments[0].TransactionTo);
            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allEntityDocuments.Count);
            Assert.AreEqual("ClientAccount", allEntityDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allEntityDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:14.0000000"), allEntityDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:14.9999999"), allEntityDocuments[0].TransactionTo);
            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual("user1", allUserToEntityMappingDocuments[0].User);
            Assert.AreEqual("ClientAccount", allUserToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allUserToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:16.0000000"), allUserToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:16.9999999"), allUserToEntityMappingDocuments[0].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(1, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual("group1", allGroupToEntityMappingDocuments[0].Group);
            Assert.AreEqual("ClientAccount", allGroupToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual("CompanyA", allGroupToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:18.0000000"), allGroupToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-18 08:42:18.9999999"), allGroupToEntityMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventIdToTransactionTimeMappingDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .SortBy(document => document.TransactionSequence)
                .ToList();
            Assert.AreEqual(20, allEventIdToTransactionTimeMappingDocuments.Count);
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
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
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
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
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
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000004")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
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
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000004")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
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
        public void RemoveGroupToGroupMapping_GroupToGroupMappingDoesntExist()
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
            String user = "user1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed38");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-12 23:34:48.0000012");

            testMongoDbAccessManagerTemporalBulkPersister.AddUserToApplicationComponentAndAccessLevelMapping(null, user, applicationComponent, accessLevel, eventId, transactionTime);

            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(user, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].User);
            Assert.AreEqual(applicationComponent, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual(accessLevel, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(transactionTime, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMappingWithTransaction()
        {
            String user = "user1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed38");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-12 23:34:48.0000012");

            testMongoDbAccessManagerTemporalBulkPersister.AddUserToApplicationComponentAndAccessLevelMappingWithTransaction(user, applicationComponent, accessLevel, eventId, transactionTime);

            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(user, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].User);
            Assert.AreEqual(applicationComponent, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual(accessLevel, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(transactionTime, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_UserToApplicationComponentAndAccessLevelMappingDoesntExist()
        {
            String user = "user1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed39");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-12 23:51:56.0000013");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToApplicationComponentAndAccessLevelMapping(null, user, applicationComponent, accessLevel, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for user 'user1', application component 'OrderScreen', access level 'Create', and transaction time '2025-10-12 23:51:56.0000013'."));
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            List<UserToApplicationComponentAndAccessLevelMappingDocument> initialUserToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "StatusScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "View", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialUserToApplicationComponentAndAccessLevelMappingDocuments);
            String user = "user1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3a");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000014");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToApplicationComponentAndAccessLevelMapping(null, user, applicationComponent, accessLevel, eventId, transactionTime);

            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 09:26:31.0000013"), allUserToApplicationComponentAndAccessLevelMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMappingWithTransaction()
        {
            List<UserToApplicationComponentAndAccessLevelMappingDocument> initialUserToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "StatusScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "View", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToApplicationComponentAndAccessLevelMappingDocument() {  User = "user1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialUserToApplicationComponentAndAccessLevelMappingDocuments);
            String user = "user1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3a");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-11 09:26:31.0000014");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToApplicationComponentAndAccessLevelMappingWithTransaction(user, applicationComponent, accessLevel, eventId, transactionTime);

            List<UserToApplicationComponentAndAccessLevelMappingDocument> allUserToApplicationComponentAndAccessLevelMappingDocuments = userToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allUserToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToApplicationComponentAndAccessLevelMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-11 09:26:31.0000013"), allUserToApplicationComponentAndAccessLevelMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            String group = "group1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3b");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-12 23:34:48.0000015");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroupToApplicationComponentAndAccessLevelMapping(null, group, applicationComponent, accessLevel, eventId, transactionTime);

            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(group, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].Group);
            Assert.AreEqual(applicationComponent, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual(accessLevel, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(transactionTime, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMappingWithTransaction()
        {
            String group = "group1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3b");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-12 23:34:48.0000015");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroupToApplicationComponentAndAccessLevelMappingWithTransaction(group, applicationComponent, accessLevel, eventId, transactionTime);

            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(group, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].Group);
            Assert.AreEqual(applicationComponent, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].ApplicationComponent);
            Assert.AreEqual(accessLevel, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].AccessLevel);
            Assert.AreEqual(transactionTime, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_GroupToApplicationComponentAndAccessLevelMappingDoesntExist()
        {
            String group = "group1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3c");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-14 21:54:50.0000016");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping(null, group, applicationComponent, accessLevel, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for group 'group1', application component 'OrderScreen', access level 'Create', and transaction time '2025-10-14 21:54:50.0000016'."));
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> initialGroupToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "StatusScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "View", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialGroupToApplicationComponentAndAccessLevelMappingDocuments);
            String group = "group1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3c");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-14 21:54:50.0000016");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping(null, group, applicationComponent, accessLevel, eventId, transactionTime);

            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-14 21:54:50.0000015"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMappingWithTransaction()
        {
            List<GroupToApplicationComponentAndAccessLevelMappingDocument> initialGroupToApplicationComponentAndAccessLevelMappingDocuments = new()
            {
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group2", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "StatusScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "View", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToApplicationComponentAndAccessLevelMappingDocument() {  Group = "group1", ApplicationComponent = "OrderScreen", AccessLevel = "Create", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToApplicationComponentAndAccessLevelMappingsCollection.InsertMany(initialGroupToApplicationComponentAndAccessLevelMappingDocuments);
            String group = "group1";
            String applicationComponent = "OrderScreen";
            String accessLevel = "Create";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3c");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-14 21:54:50.0000016");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToApplicationComponentAndAccessLevelMappingWithTransaction(group, applicationComponent, accessLevel, eventId, transactionTime);

            List<GroupToApplicationComponentAndAccessLevelMappingDocument> allGroupToApplicationComponentAndAccessLevelMappingDocuments = groupToApplicationComponentAndAccessLevelMappingsCollection.Find(FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allGroupToApplicationComponentAndAccessLevelMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToApplicationComponentAndAccessLevelMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-14 21:54:50.0000015"), allGroupToApplicationComponentAndAccessLevelMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
            Assert.AreEqual(1, applicationComponentStringifier.ToStringCallCount);
            Assert.AreEqual(1, accessLevelStringifier.ToStringCallCount);
        }

        [Test]
        public void AddEntityType()
        {
            String entityType = "ClientAccount";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3d");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-14 22:08:57.0000017");

            testMongoDbAccessManagerTemporalBulkPersister.AddEntityType(null, entityType, eventId, transactionTime);

            List<EntityTypeDocument> allUserDocuments = entityTypesCollection.Find(FilterDefinition<EntityTypeDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserDocuments.Count);
            Assert.AreEqual(entityType, allUserDocuments[0].EntityType);
            Assert.AreEqual(transactionTime, allUserDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void AddEntityTypeWithTransaction()
        {
            String entityType = "ClientAccount";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3d");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-14 22:08:57.0000017");

            testMongoDbAccessManagerTemporalBulkPersister.AddEntityTypeWithTransaction(entityType, eventId, transactionTime);

            List<EntityTypeDocument> allUserDocuments = entityTypesCollection.Find(FilterDefinition<EntityTypeDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allUserDocuments.Count);
            Assert.AreEqual(entityType, allUserDocuments[0].EntityType);
            Assert.AreEqual(transactionTime, allUserDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allUserDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void RemoveEntityType_EntityTypeDoesntExist()
        {
            String entityType = "ClientAccount";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3e");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-14 22:27:56.0000018");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveEntityType(null, entityType, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for entity type 'ClientAccount', and transaction time '2025-10-14 22:27:56.0000018'."));
        }

        [Test]
        public void RemoveEntityType()
        {
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:54.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocuments);
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:56.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:57.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:58.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:59.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocuments);
            List<EntityDocument> initialEntityDocuments = new()
            {
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:00.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new EntityDocument() {  EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:01.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:02.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            entitiesCollection.InsertMany(initialEntityDocuments);
            List<EntityTypeDocument> initialEntityTypeDocuments = new()
            {
                new EntityTypeDocument() {  EntityType = "ClientAccount", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:03.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new EntityTypeDocument() {  EntityType = "BusinessUnit", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:04.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityTypeDocument() {  EntityType = "ClientAccount", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            entityTypesCollection.InsertMany(initialEntityTypeDocuments);
            String entityType = "ClientAccount";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3f");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-15 08:00:09.0000019");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveEntityType(null, entityType, eventId, transactionTime);

            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allUserToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allUserToEntityMappingDocuments[4].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                        .SortBy(document => document.TransactionFrom)
                        .ToList();
            Assert.AreEqual(5, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allGroupToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allGroupToEntityMappingDocuments[4].TransactionTo);
            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(4, allEntityDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000002"), allEntityDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allEntityDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allEntityDocuments[3].TransactionTo);
            List<EntityTypeDocument> allEntityTypDocuments = entityTypesCollection.Find(FilterDefinition<EntityTypeDocument>.Empty)
                            .SortBy(document => document.TransactionFrom)
                            .ToList();
            Assert.AreEqual(3, allEntityTypDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allEntityTypDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityTypDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allEntityTypDocuments[2].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void RemoveEntityTypeWithTransaction()
        {
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:54.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocuments);
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:56.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:57.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:58.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:59.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocuments);
            List<EntityDocument> initialEntityDocuments = new()
            {
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:00.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000002")},
                new EntityDocument() {  EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:01.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:02.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            entitiesCollection.InsertMany(initialEntityDocuments);
            List<EntityTypeDocument> initialEntityTypeDocuments = new()
            {
                new EntityTypeDocument() {  EntityType = "ClientAccount", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:03.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new EntityTypeDocument() {  EntityType = "BusinessUnit", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:04.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityTypeDocument() {  EntityType = "ClientAccount", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            entityTypesCollection.InsertMany(initialEntityTypeDocuments);
            String entityType = "ClientAccount";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed3f");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-15 08:00:09.0000019");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveEntityTypeWithTransaction(entityType, eventId, transactionTime);

            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allUserToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allUserToEntityMappingDocuments[4].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                        .SortBy(document => document.TransactionFrom)
                        .ToList();
            Assert.AreEqual(5, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allGroupToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allGroupToEntityMappingDocuments[4].TransactionTo);
            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(4, allEntityDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000002"), allEntityDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allEntityDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allEntityDocuments[3].TransactionTo);
            List<EntityTypeDocument> allEntityTypDocuments = entityTypesCollection.Find(FilterDefinition<EntityTypeDocument>.Empty)
                            .SortBy(document => document.TransactionFrom)
                            .ToList();
            Assert.AreEqual(3, allEntityTypDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allEntityTypDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityTypDocuments[1].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-15 08:00:09.0000018"), allEntityTypDocuments[2].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void AddEntity()
        {
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed40");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-16 22:59:44.0000020");

            testMongoDbAccessManagerTemporalBulkPersister.AddEntity(null, entityType, entity, eventId, transactionTime);

            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEntityDocuments.Count);
            Assert.AreEqual(entityType, allEntityDocuments[0].EntityType);
            Assert.AreEqual(entity, allEntityDocuments[0].Entity);
            Assert.AreEqual(transactionTime, allEntityDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allEntityDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void AddEntityWithTransaction()
        {
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed40");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-16 22:59:44.0000020");

            testMongoDbAccessManagerTemporalBulkPersister.AddEntityWithTransaction(entityType, entity, eventId, transactionTime);

            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEntityDocuments.Count);
            Assert.AreEqual(entityType, allEntityDocuments[0].EntityType);
            Assert.AreEqual(entity, allEntityDocuments[0].Entity);
            Assert.AreEqual(transactionTime, allEntityDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allEntityDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void RemoveEntity_EntityDoesntExist()
        {
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed41");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-16 23:31:20.0000021");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveEntity(null, entityType, entity, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for entity type 'ClientAccount', entity 'CompanyA', and transaction time '2025-10-16 23:31:20.0000021'."));
        }

        [Test]
        public void RemoveEntity()
        {
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:54.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:42.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocuments);
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:56.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:57.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:58.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:59.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:42.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocuments);
            List<EntityDocument> initialEntityDocuments = new()
            {
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:03.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new EntityDocument() {  EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:04.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:05.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            entitiesCollection.InsertMany(initialEntityDocuments);
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed41");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-16 23:31:20.0000021");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveEntity(null, entityType, entity, eventId, transactionTime);

            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(6, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allUserToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allUserToEntityMappingDocuments[5].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                        .SortBy(document => document.TransactionFrom)
                        .ToList();
            Assert.AreEqual(6, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allGroupToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allGroupToEntityMappingDocuments[5].TransactionTo);
            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(4, allEntityDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allEntityDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allEntityDocuments[3].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void RemoveEntityWithTransaction()
        {
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:54.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:55.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:42.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocuments);
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:56.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000001")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:57.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:58.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:59.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:42.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocuments);
            List<EntityDocument> initialEntityDocuments = new()
            {
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:03.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000003")},
                new EntityDocument() {  EntityType = "BusinessUnit", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:04.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:34:05.0000000"), TransactionTo = DateTime.MaxValue},
                new EntityDocument() {  EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            entitiesCollection.InsertMany(initialEntityDocuments);
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed41");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-16 23:31:20.0000021");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveEntityWithTransaction(entityType, entity, eventId, transactionTime);

            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(6, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allUserToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allUserToEntityMappingDocuments[5].TransactionTo);
            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                        .SortBy(document => document.TransactionFrom)
                        .ToList();
            Assert.AreEqual(6, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000001"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allGroupToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allGroupToEntityMappingDocuments[5].TransactionTo);
            List<EntityDocument> allEntityDocuments = entitiesCollection.Find(FilterDefinition<EntityDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(4, allEntityDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000003"), allEntityDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allEntityDocuments[2].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-16 23:31:20.0000020"), allEntityDocuments[3].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            String user = "user1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed42");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 10:02:28.0000022");

            testMongoDbAccessManagerTemporalBulkPersister.AddUserToEntityMapping(null, user, entityType, entity, eventId, transactionTime);

            List<UserToEntityMappingDocument> allAddUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allAddUserToEntityMappingDocuments.Count);
            Assert.AreEqual(user, allAddUserToEntityMappingDocuments[0].User);
            Assert.AreEqual(entityType, allAddUserToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual(entity, allAddUserToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(transactionTime, allAddUserToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allAddUserToEntityMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddUserToEntityMappingWithTransaction()
        {
            String user = "user1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed42");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 10:02:28.0000022");

            testMongoDbAccessManagerTemporalBulkPersister.AddUserToEntityMappingWithTransaction(user, entityType, entity, eventId, transactionTime);

            List<UserToEntityMappingDocument> allAddUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allAddUserToEntityMappingDocuments.Count);
            Assert.AreEqual(user, allAddUserToEntityMappingDocuments[0].User);
            Assert.AreEqual(entityType, allAddUserToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual(entity, allAddUserToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(transactionTime, allAddUserToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allAddUserToEntityMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToEntityMapping_UserToEntityMappingDoesntExist()
        {
            String user = "user1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed43");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 10:15:42.0000023");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToEntityMapping(null, user, entityType, entity, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for user 'user1', entity type 'ClientAccount', entity 'CompanyA', and transaction time '2025-10-17 10:15:42.0000023'."));
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocumentDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "Suppliers", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocumentDocuments);
            String user = "user1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed43");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 10:15:42.0000023");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToEntityMapping(null, user, entityType, entity, eventId, transactionTime);

            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-17 10:15:42.0000022"), allUserToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveUserToEntityMappingWithTransaction()
        {
            List<UserToEntityMappingDocument> initialUserToEntityMappingDocumentDocuments = new()
            {
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new UserToEntityMappingDocument() {  User = "user2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "Suppliers", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new UserToEntityMappingDocument() {  User = "user1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            userToEntityMappingsCollection.InsertMany(initialUserToEntityMappingDocumentDocuments);
            String user = "user1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed43");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 10:15:42.0000023");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveUserToEntityMappingWithTransaction(user, entityType, entity, eventId, transactionTime);

            List<UserToEntityMappingDocument> allUserToEntityMappingDocuments = userToEntityMappingsCollection.Find(FilterDefinition<UserToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allUserToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allUserToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allUserToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-17 10:15:42.0000022"), allUserToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, userStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            String group = "group1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed44");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 22:43:20.0000024");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroupToEntityMapping(null, group, entityType, entity, eventId, transactionTime);

            List<GroupToEntityMappingDocument> allAddGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allAddGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(group, allAddGroupToEntityMappingDocuments[0].Group);
            Assert.AreEqual(entityType, allAddGroupToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual(entity, allAddGroupToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(transactionTime, allAddGroupToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allAddGroupToEntityMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void AddGroupToEntityMappingWithTransaction()
        {
            String group = "group1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed44");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 22:43:20.0000024");

            testMongoDbAccessManagerTemporalBulkPersister.AddGroupToEntityMappingWithTransaction(group, entityType, entity, eventId, transactionTime);

            List<GroupToEntityMappingDocument> allAddGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allAddGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(group, allAddGroupToEntityMappingDocuments[0].Group);
            Assert.AreEqual(entityType, allAddGroupToEntityMappingDocuments[0].EntityType);
            Assert.AreEqual(entity, allAddGroupToEntityMappingDocuments[0].Entity);
            Assert.AreEqual(transactionTime, allAddGroupToEntityMappingDocuments[0].TransactionFrom);
            Assert.AreEqual(temporalMaxDate, allAddGroupToEntityMappingDocuments[0].TransactionTo);
            List<EventIdToTransactionTimeMappingDocument> allEventDocuments = eventIdToTransactionTimeMapCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                .ToList();
            Assert.AreEqual(1, allEventDocuments.Count);
            Assert.AreEqual(eventId, allEventDocuments[0].EventId);
            Assert.AreEqual(transactionTime, allEventDocuments[0].TransactionTime);
            Assert.AreEqual(0, allEventDocuments[0].TransactionSequence);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToEntityMapping_GroupToEntityMappingDoesntExist()
        {
            String group = "group1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed45");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 22:43:43.0000025");

            var e = Assert.Throws<Exception>(delegate
            {
                testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToEntityMapping(null, group, entityType, entity, eventId, transactionTime);
            });

            Assert.That(e.Message, Does.StartWith($"No document exists for group 'group1', entity type 'ClientAccount', entity 'CompanyA', and transaction time '2025-10-17 22:43:43.0000025'."));
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocumentDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "Suppliers", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocumentDocuments);
            String group = "group1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed45");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 22:43:43.0000025");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToEntityMapping(null, group, entityType, entity, eventId, transactionTime);

            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-17 22:43:43.0000024"), allGroupToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
        }

        [Test]
        public void RemoveGroupToEntityMappingWithTransaction()
        {
            List<GroupToEntityMappingDocument> initialGroupToEntityMappingDocumentDocuments = new()
            {
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:52.0000000"), TransactionTo = CreateDataTimeFromString("2025-10-09 07:53:28.0000000")},
                new GroupToEntityMappingDocument() {  Group = "group2", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "Suppliers", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyB", TransactionFrom = CreateDataTimeFromString("2025-10-04 17:33:53.0000000"), TransactionTo = DateTime.MaxValue},
                new GroupToEntityMappingDocument() {  Group = "group1", EntityType = "ClientAccount", Entity = "CompanyA", TransactionFrom = CreateDataTimeFromString("2025-10-09 08:26:41.0000000"), TransactionTo = DateTime.MaxValue}
            };
            groupToEntityMappingsCollection.InsertMany(initialGroupToEntityMappingDocumentDocuments);
            String group = "group1";
            String entityType = "ClientAccount";
            String entity = "CompanyA";
            var eventId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed45");
            DateTime transactionTime = CreateDataTimeFromString("2025-10-17 22:43:43.0000025");

            testMongoDbAccessManagerTemporalBulkPersister.RemoveGroupToEntityMappingWithTransaction(group, entityType, entity, eventId, transactionTime);

            List<GroupToEntityMappingDocument> allGroupToEntityMappingDocuments = groupToEntityMappingsCollection.Find(FilterDefinition<GroupToEntityMappingDocument>.Empty)
                .SortBy(document => document.TransactionFrom)
                .ToList();
            Assert.AreEqual(5, allGroupToEntityMappingDocuments.Count);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-09 07:53:28.0000000"), allGroupToEntityMappingDocuments[0].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[1].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[2].TransactionTo);
            Assert.AreEqual(DateTime.MaxValue, allGroupToEntityMappingDocuments[3].TransactionTo);
            Assert.AreEqual(CreateDataTimeFromString("2025-10-17 22:43:43.0000024"), allGroupToEntityMappingDocuments[4].TransactionTo);
            Assert.AreEqual(1, groupStringifier.ToStringCallCount);
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
            /// <param name="useTransactions">Whether to execute MongoDB changes within transactions.</param>
            public MongoDbAccessManagerTemporalBulkPersisterWithProtectedMembers
            (
                String connectionString,
                String databaseName,
                IUniqueStringifier<TUser> userStringifier,
                IUniqueStringifier<TGroup> groupStringifier,
                IUniqueStringifier<TComponent> applicationComponentStringifier,
                IUniqueStringifier<TAccess> accessLevelStringifier,
                Boolean useTransactions,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            ) : base(connectionString, databaseName, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, useTransactions, logger, metricLogger)
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

            public new void RemoveUserToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUserToApplicationComponentAndAccessLevelMapping(session, user, applicationComponent, accessLevel, eventId, transactionTime);
            }

            public new void RemoveUserToApplicationComponentAndAccessLevelMappingWithTransaction(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUserToApplicationComponentAndAccessLevelMappingWithTransaction(user, applicationComponent, accessLevel, eventId, transactionTime);
            }

            public new void AddGroupToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.AddGroupToApplicationComponentAndAccessLevelMapping(session, group, applicationComponent, accessLevel, eventId, transactionTime);
            }

            public new void AddGroupToApplicationComponentAndAccessLevelMappingWithTransaction(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.AddGroupToApplicationComponentAndAccessLevelMappingWithTransaction(group, applicationComponent, accessLevel, eventId, transactionTime);
            }

            public new void RemoveGroupToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroupToApplicationComponentAndAccessLevelMapping(session, group, applicationComponent, accessLevel, eventId, transactionTime);
            }

            public new void RemoveGroupToApplicationComponentAndAccessLevelMappingWithTransaction(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroupToApplicationComponentAndAccessLevelMappingWithTransaction(group, applicationComponent, accessLevel, eventId, transactionTime);
            }

            public new void AddEntityType(IClientSessionHandle session, String entityType, Guid eventId, DateTime transactionTime)
            {
                base.AddEntityType(session, entityType, eventId, transactionTime);
            }

            public new void AddEntityTypeWithTransaction(String entityType, Guid eventId, DateTime transactionTime)
            {
                base.AddEntityTypeWithTransaction(entityType, eventId, transactionTime);
            }

            public new void RemoveEntityType(IClientSessionHandle session, String entityType, Guid eventId, DateTime transactionTime)
            {
                base.RemoveEntityType(session, entityType, eventId, transactionTime);
            }

            public new void RemoveEntityTypeWithTransaction(String entityType, Guid eventId, DateTime transactionTime)
            {
                base.RemoveEntityTypeWithTransaction(entityType, eventId, transactionTime);
            }

            public new void AddEntity(IClientSessionHandle session, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.AddEntity(session, entityType, entity, eventId, transactionTime);
            }

            public new void AddEntityWithTransaction(String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.AddEntityWithTransaction(entityType, entity, eventId, transactionTime);
            }

            public new void RemoveEntity(IClientSessionHandle session, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.RemoveEntity(session, entityType, entity, eventId, transactionTime);
            }

            public new void RemoveEntityWithTransaction(String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.RemoveEntityWithTransaction(entityType, entity, eventId, transactionTime);
            }

            public new void AddUserToEntityMapping(IClientSessionHandle session, TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.AddUserToEntityMapping(session, user, entityType, entity, eventId, transactionTime);
            }

            public new void AddUserToEntityMappingWithTransaction(TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.AddUserToEntityMappingWithTransaction(user, entityType, entity, eventId, transactionTime);
            }

            public new void RemoveUserToEntityMapping(IClientSessionHandle session, TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUserToEntityMapping(session, user, entityType, entity, eventId, transactionTime);
            }

            public new void RemoveUserToEntityMappingWithTransaction(TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.RemoveUserToEntityMappingWithTransaction(user, entityType, entity, eventId, transactionTime);
            }

            public new void AddGroupToEntityMapping(IClientSessionHandle session, TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.AddGroupToEntityMapping(session, group, entityType, entity, eventId, transactionTime);
            }

            public new void AddGroupToEntityMappingWithTransaction(TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.AddGroupToEntityMappingWithTransaction(group, entityType, entity, eventId, transactionTime);
            }

            public new void RemoveGroupToEntityMapping(IClientSessionHandle session, TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroupToEntityMapping(session, group, entityType, entity, eventId, transactionTime);
            }

            public new void RemoveGroupToEntityMappingWithTransaction(TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
            {
                base.RemoveGroupToEntityMappingWithTransaction(group, entityType, entity, eventId, transactionTime);
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
