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
using System.Collections;
using System.Linq.Expressions;
using System.Text;
using MongoDB.Driver;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Persistence.MongoDb.Models.Documents;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence.MongoDb
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> which persists access manager events in bulk to, and allows reading of <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> objects from a MongoDB database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class MongoDbAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
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
        protected const String dateTimeExceptionMessageFormatString = "yyyy-MM-dd HH:mm:ss.fffffff";

        #pragma warning restore 1591

        /// <summary>The maximum date allowed in the temporal model.</summary>
        protected readonly DateTime temporalMaxDate = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

        /// <summary>The string to use to connect to MongoDB.</summary>
        protected String connectionString;
        /// <summary>The name of the database.</summary>
        protected String databaseName;
        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary>Whether to execute MongoDB changes within transactions.</summary>
        protected Boolean useTransactions;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>The client to connect to MongoDB.</summary>
        protected IMongoClient mongoClient;
        /// <summary>The MongoDB database.</summary>
        protected IMongoDatabase database;
        /// <summary>Maps types (deriving from <see cref="TemporalEventBufferItemBase"/>) to actions which persists an event of that type.</summary>
        protected Dictionary<Type, Action<TemporalEventBufferItemBase>> eventTypeToPersistenceActionMap;
        /// <summary>Whether the PersistEvents() method has already been called.</summary>
        protected Boolean persistEventsCalled;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.MongoDb.MongoDbAccessManagerTemporalBulkPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the MongoDB database.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="useTransactions">Whether to execute MongoDB changes within transactions.</param>
        /// <param name="logger">The logger for general logging.</param>
        public MongoDbAccessManagerTemporalBulkPersister
        (
            String connectionString,
            String databaseName,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Boolean useTransactions,
            IApplicationLogger logger
        ) : this (connectionString, databaseName, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, useTransactions, logger, new NullMetricLogger())
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.MongoDb.MongoDbAccessManagerTemporalBulkPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the MongoDB database.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="useTransactions">Whether to execute MongoDB changes within transactions.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public MongoDbAccessManagerTemporalBulkPersister
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
        )
        {
            ThrowExceptionIfStringParameterNullOrWhitespace(nameof(connectionString), connectionString);
            ThrowExceptionIfStringParameterNullOrWhitespace(nameof(databaseName), databaseName);

            this.connectionString = connectionString;
            this.databaseName = databaseName;
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            this.useTransactions = useTransactions;
            this.logger = logger;
            this.metricLogger = metricLogger;
            mongoClient = new MongoClient(this.connectionString);
            database = mongoClient.GetDatabase(databaseName);
            eventTypeToPersistenceActionMap = new Dictionary<Type, Action<TemporalEventBufferItemBase>>();
            PopulateEventTypeToPersistenceActionMap();
            persistEventsCalled = false;
            disposed = false;
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            PersistEvents(events, false);
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events, Boolean ignorePreExistingEvents)
        {
            if (persistEventsCalled == false)
            {
                CreateCollections();
                CreateIndexes();
                persistEventsCalled = true;
            }

            foreach (TemporalEventBufferItemBase currentEventBufferItem in events)
            {
                if (eventTypeToPersistenceActionMap.ContainsKey(currentEventBufferItem.GetType()) == false)
                    throw new Exception($"Encountered unhandled event buffer item type '{currentEventBufferItem.GetType().Name}'.");

                if (ignorePreExistingEvents == true)
                {
                    IMongoCollection<EventIdToTransactionTimeMappingDocument> eventIdToTransactionTimeMappingCollection = database.GetCollection<EventIdToTransactionTimeMappingDocument>(eventIdToTransactionTimeMapCollectionName);
                    FilterDefinition<EventIdToTransactionTimeMappingDocument> existingEventFilterDefinition = Builders<EventIdToTransactionTimeMappingDocument>.Filter.Eq(document => document.EventId, currentEventBufferItem.EventId);
                    EventIdToTransactionTimeMappingDocument existingDocument;
                    try
                    {
                        existingDocument = Find(null, eventIdToTransactionTimeMappingCollection, existingEventFilterDefinition).FirstOrDefault();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to retrieve existing document from collecion '{eventIdToTransactionTimeMapCollectionName}' when persisting events in MongoDB.", e);
                    }
                    if (existingDocument != null)
                    {
                        continue;
                    }
                }

                eventTypeToPersistenceActionMap[currentEventBufferItem.GetType()].Invoke(currentEventBufferItem);
            }
        }

        /// <inheritdoc/>
        public AccessManagerState Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return Load(DateTime.UtcNow, accessManagerToLoadTo, new PersistentStorageEmptyException("The database does not contain any existing events nor data."));
        }

        /// <inheritdoc/>
        public AccessManagerState Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            // Get the AccessManager state corresponding to eventId 
            IMongoCollection<EventIdToTransactionTimeMappingDocument> eventIdToTransactionTimeMappingCollection = database.GetCollection<EventIdToTransactionTimeMappingDocument>(eventIdToTransactionTimeMapCollectionName);
            DateTime transactionTime = DateTime.MinValue;
            try
            {
                var eventIdToTransactionTimeMappingCollectionFilterDefinition = Builders<EventIdToTransactionTimeMappingDocument>.Filter.Eq(document => document.EventId, eventId);
                EventIdToTransactionTimeMappingDocument transactionTimeDocument = Find(null, eventIdToTransactionTimeMappingCollection, eventIdToTransactionTimeMappingCollectionFilterDefinition)
                    .FirstOrDefault();
                if (transactionTimeDocument != null)
                {
                    transactionTime = transactionTimeDocument.TransactionTime;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to retrieve access manager state from collection '{eventIdToTransactionTimeMapCollectionName}' when loading from MongoDB.", e);
            }
            if (transactionTime == DateTime.MinValue)
                throw new ArgumentException($"No '{eventIdToTransactionTimeMapCollectionName}' documents were returned for EventId '{eventId.ToString()}'.", nameof(eventId));

            return Load(transactionTime, accessManagerToLoadTo);
        }

        /// <inheritdoc/>
        public AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return Load(stateTime, accessManagerToLoadTo, new ArgumentException($"No '{eventIdToTransactionTimeMapCollectionName}' documents were returned with TransactionTime less than or equal to '{stateTime.ToString(dateTimeExceptionMessageFormatString)}'.", nameof(stateTime)));
        }

        #region Private/Protected Methods

        #region Event Persistence Methods

        #pragma warning disable 1591

        protected void CreateEvent(IClientSessionHandle session, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<EventIdToTransactionTimeMappingDocument> eventIdToTransactionTimeMappingCollection = database.GetCollection<EventIdToTransactionTimeMappingDocument>(eventIdToTransactionTimeMapCollectionName);
            DateTime lastTransactionTime = DateTime.MinValue;
            Int32 lastTransactionSequence = 0;
            Int32 transactionSequence = 0;

            // Get the most recent transaction time
            try
            {
                EventIdToTransactionTimeMappingDocument lastTransactionTimeDocument = Find(session, eventIdToTransactionTimeMappingCollection, FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                    .SortByDescending(document => document.TransactionTime)
                    .FirstOrDefault();
                if (lastTransactionTimeDocument != null)
                {
                    // Get the largest transaction sequence within the most recent transaction time
                    FilterDefinition<EventIdToTransactionTimeMappingDocument> lastTransactionTimeFilter = Builders<EventIdToTransactionTimeMappingDocument>.Filter.Eq(document => document.TransactionTime, lastTransactionTimeDocument.TransactionTime);
                    EventIdToTransactionTimeMappingDocument lastTransactionSequenceDocument = Find(session, eventIdToTransactionTimeMappingCollection, lastTransactionTimeFilter)
                        .SortByDescending(document => document.TransactionSequence)
                        .FirstOrDefault();
                    lastTransactionTime = lastTransactionSequenceDocument.TransactionTime;
                    lastTransactionSequence = lastTransactionSequenceDocument.TransactionSequence;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to retrieve most recent transaction time and sequence from collection '{eventIdToTransactionTimeMapCollectionName}' when persisting a new event to MongoDB.", e);
            }

            if (transactionTime < lastTransactionTime)
            {
                throw new ArgumentException($"Parameter '{nameof(transactionTime)}' with value '{transactionTime.ToString(dateTimeExceptionMessageFormatString)}' must be greater than or equal to last transaction time '{lastTransactionTime.ToString(dateTimeExceptionMessageFormatString)}'.", nameof(transactionTime));
            }
            else if (transactionTime == lastTransactionTime)
            {
                transactionSequence = lastTransactionSequence + 1;
            }

            // Create the new event
            EventIdToTransactionTimeMappingDocument newDocument = new()
            {
                EventId = eventId,
                TransactionTime = transactionTime,
                TransactionSequence = transactionSequence
            };
            try
            {
                InsertOne(session, eventIdToTransactionTimeMappingCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{eventIdToTransactionTimeMapCollectionName}'.", e);
            }
        }

        protected void AddUser(IClientSessionHandle session, TUser user, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<UserDocument> usersCollection = database.GetCollection<UserDocument>(usersCollectionName);
            UserDocument newDocument = new()
            {
                User = userStringifier.ToString(user),
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, usersCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{usersCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddUserWithTransaction(TUser user, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddUser(session, user, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveUser(IClientSessionHandle session, TUser user, Guid eventId, DateTime transactionTime)
        {
            String stringifiedUser = userStringifier.ToString(user);
            IMongoCollection<UserDocument> usersCollection = database.GetCollection<UserDocument>(usersCollectionName);
            FilterDefinition<UserDocument> existingUserFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserDocument>.Filter.Eq(document => document.User, stringifiedUser));
            GetExistingDocument
            (
                session,
                usersCollection,
                existingUserFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(usersCollectionName, "user"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("user", stringifiedUser))
            );

            // Invalidate any UserToGroupMapping documents
            FilterDefinition<UserToGroupMappingDocument> userToGroupMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToGroupMappingDocument>.Filter.Eq(document => document.User, stringifiedUser));
            InvalidateDocuments(session, userToGroupMappingFilter, transactionTime, userToGroupMappingsCollectionName, "user to group mapping", "user");
            // Invalidate any UserToApplicationComponentAndAccessLevelMapping documents
            FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument> userToApplicationComponentAndAccessLevelMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.User, stringifiedUser));
            InvalidateDocuments(session, userToApplicationComponentAndAccessLevelMappingFilter, transactionTime, userToApplicationComponentAndAccessLevelMappingsCollectionName, "user to application component and access level mapping", "user");
            // Invalidate any UserToEntityMapping documents
            FilterDefinition<UserToEntityMappingDocument> userToEntityMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToEntityMappingDocument>.Filter.Eq(document => document.User, stringifiedUser));
            InvalidateDocuments(session, userToEntityMappingFilter, transactionTime, userToEntityMappingsCollectionName, "user to entity mapping", "user");

            // Invalidate the user
            UpdateDefinition<UserDocument> invalidationUpdate = Builders<UserDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, usersCollection, existingUserFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate user document in collecion '{usersCollectionName}' when removing user from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveUserWithTransaction(TUser user, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveUser(session, user, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddGroup(IClientSessionHandle session, TGroup group, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<GroupDocument> groupsCollection = database.GetCollection<GroupDocument>(groupsCollectionName);
            GroupDocument newDocument = new()
            {
                Group = groupStringifier.ToString(group),
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, groupsCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{groupsCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddGroupWithTransaction(TGroup group, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddGroup(session, group, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveGroup(IClientSessionHandle session, TGroup group, Guid eventId, DateTime transactionTime)
        {
            String stringifiedGroup = groupStringifier.ToString(group);
            IMongoCollection<GroupDocument> groupsCollection = database.GetCollection<GroupDocument>(groupsCollectionName);
            FilterDefinition<GroupDocument> existingGroupFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupDocument>.Filter.Eq(document => document.Group, stringifiedGroup));
            GetExistingDocument
            (
                session,
                groupsCollection,
                existingGroupFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(groupsCollectionName, "group"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("group", stringifiedGroup))
            );

            // Invalidate any UserToGroupMapping documents
            FilterDefinition<UserToGroupMappingDocument> userToGroupMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToGroupMappingDocument>.Filter.Eq(document => document.Group, stringifiedGroup));
            InvalidateDocuments(session, userToGroupMappingFilter, transactionTime, userToGroupMappingsCollectionName, "user to group mapping", "group");
            // Invalidate any GroupToGroupMapping documents
            FilterDefinition<GroupToGroupMappingDocument> groupToGroupMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToGroupMappingDocument>.Filter.Or
            (
                Builders<GroupToGroupMappingDocument>.Filter.Eq(document => document.FromGroup, stringifiedGroup),
                Builders<GroupToGroupMappingDocument>.Filter.Eq(document => document.ToGroup, stringifiedGroup)
            ));
            InvalidateDocuments(session, groupToGroupMappingFilter, transactionTime, groupToGroupMappingsCollectionName, "group to group mapping", "group");
            // Invalidate any GroupToApplicationComponentAndAccessLevelMapping documents
            FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument> groupToApplicationComponentAndAccessLevelMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.Group, stringifiedGroup));
            InvalidateDocuments(session, groupToApplicationComponentAndAccessLevelMappingFilter, transactionTime, groupToApplicationComponentAndAccessLevelMappingsCollectionName, "group to application component and access level mapping", "group");
            // Invalidate any GroupToEntityMapping documents
            FilterDefinition<GroupToEntityMappingDocument> groupToEntityMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToEntityMappingDocument>.Filter.Eq(document => document.Group, stringifiedGroup));
            InvalidateDocuments(session, groupToEntityMappingFilter, transactionTime, groupToEntityMappingsCollectionName, "group to entity mapping", "group");

            // Invalidate the group
            UpdateDefinition<GroupDocument> invalidationUpdate = Builders<GroupDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, groupsCollection, existingGroupFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate group document in collecion '{groupsCollectionName}' when removing group from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveGroupWithTransaction(TGroup group, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveGroup(session, group, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddUserToGroupMapping(IClientSessionHandle session, TUser user, TGroup group, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<UserToGroupMappingDocument> userToGroupMappingCollection = database.GetCollection<UserToGroupMappingDocument>(userToGroupMappingsCollectionName);
            UserToGroupMappingDocument newDocument = new()
            {
                User = userStringifier.ToString(user),
                Group = groupStringifier.ToString(group),
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, userToGroupMappingCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{userToGroupMappingsCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddUserToGroupMappingWithTransaction(TUser user, TGroup group, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddUserToGroupMapping(session, user, group, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveUserToGroupMapping(IClientSessionHandle session, TUser user, TGroup group, Guid eventId, DateTime transactionTime)
        {
            String stringifiedUser = userStringifier.ToString(user);
            String stringifiedGroup = groupStringifier.ToString(group);
            IMongoCollection<UserToGroupMappingDocument> userToGroupMappingCollection = database.GetCollection<UserToGroupMappingDocument>(userToGroupMappingsCollectionName);
            FilterDefinition<UserToGroupMappingDocument> existingUserToGroupMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToGroupMappingDocument>.Filter.And
            (
                Builders<UserToGroupMappingDocument>.Filter.Eq(document => document.User, stringifiedUser),
                Builders<UserToGroupMappingDocument>.Filter.Eq(document => document.Group, stringifiedGroup)
            ));
            GetExistingDocument
            (
                session,
                userToGroupMappingCollection,
                existingUserToGroupMappingFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(userToGroupMappingsCollectionName, "user to group mapping"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("user", stringifiedUser), Tuple.Create("group", stringifiedGroup))
            );

            // Invalidate the user to group mapping
            UpdateDefinition<UserToGroupMappingDocument> invalidationUpdate = Builders<UserToGroupMappingDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, userToGroupMappingCollection, existingUserToGroupMappingFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate user to group mapping document in collecion '{userToGroupMappingsCollectionName}' when removing user to group mapping from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveUserToGroupMappingWithTransaction(TUser user, TGroup group, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveUserToGroupMapping(session, user, group, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddGroupToGroupMapping(IClientSessionHandle session, TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<GroupToGroupMappingDocument> groupToGroupMappingCollection = database.GetCollection<GroupToGroupMappingDocument>(groupToGroupMappingsCollectionName);
            GroupToGroupMappingDocument newDocument = new()
            {
                FromGroup = groupStringifier.ToString(fromGroup),
                ToGroup = groupStringifier.ToString(toGroup),
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, groupToGroupMappingCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{groupToGroupMappingsCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddGroupToGroupMappingWithTransaction(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddGroupToGroupMapping(session, fromGroup, toGroup, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveGroupToGroupMapping(IClientSessionHandle session, TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
        {
            String stringifiedFromGroup = groupStringifier.ToString(fromGroup);
            String stringifiedToGroup = groupStringifier.ToString(toGroup);
            IMongoCollection<GroupToGroupMappingDocument> groupToGroupMappingCollection = database.GetCollection<GroupToGroupMappingDocument>(groupToGroupMappingsCollectionName);
            FilterDefinition<GroupToGroupMappingDocument> existingGroupToGroupMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToGroupMappingDocument>.Filter.And
            (
                Builders<GroupToGroupMappingDocument>.Filter.Eq(document => document.FromGroup, stringifiedFromGroup),
                Builders<GroupToGroupMappingDocument>.Filter.Eq(document => document.ToGroup, stringifiedToGroup)
            ));
            GetExistingDocument
            (
                session,
                groupToGroupMappingCollection,
                existingGroupToGroupMappingFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(groupToGroupMappingsCollectionName, "group to group mapping"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("from group", stringifiedFromGroup), Tuple.Create("to group", stringifiedToGroup))
            );

            // Invalidate the group to group mapping
            UpdateDefinition<GroupToGroupMappingDocument> invalidationUpdate = Builders<GroupToGroupMappingDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, groupToGroupMappingCollection, existingGroupToGroupMappingFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate group to group mapping document in collecion '{groupToGroupMappingsCollectionName}' when removing group to group mapping from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveGroupToGroupMappingWithTransaction(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveGroupToGroupMapping(session, fromGroup, toGroup, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddUserToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<UserToApplicationComponentAndAccessLevelMappingDocument> userToApplicationComponentAndAccessLevelMappingCollection = database.GetCollection<UserToApplicationComponentAndAccessLevelMappingDocument>(userToApplicationComponentAndAccessLevelMappingsCollectionName);
            UserToApplicationComponentAndAccessLevelMappingDocument newDocument = new()
            {
                User = userStringifier.ToString(user),
                ApplicationComponent = applicationComponentStringifier.ToString(applicationComponent),
                AccessLevel = accessLevelStringifier.ToString(accessLevel),
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, userToApplicationComponentAndAccessLevelMappingCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{userToApplicationComponentAndAccessLevelMappingsCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddUserToApplicationComponentAndAccessLevelMappingWithTransaction(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddUserToApplicationComponentAndAccessLevelMapping(session, user, applicationComponent, accessLevel, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveUserToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            String stringifiedUser = userStringifier.ToString(user);
            String stringifiedApplicationComponent = applicationComponentStringifier.ToString(applicationComponent);
            String stringifiedAccessLevel = accessLevelStringifier.ToString(accessLevel);
            IMongoCollection<UserToApplicationComponentAndAccessLevelMappingDocument> userToApplicationComponentAndAccessLevelMappingCollection = database.GetCollection<UserToApplicationComponentAndAccessLevelMappingDocument>(userToApplicationComponentAndAccessLevelMappingsCollectionName);
            FilterDefinition<UserToApplicationComponentAndAccessLevelMappingDocument> existingUserToApplicationComponentAndAccessLevelMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.Filter.And
            (
                Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.User, stringifiedUser),
                Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.ApplicationComponent, stringifiedApplicationComponent),
                Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.AccessLevel, stringifiedAccessLevel)
            ));
            GetExistingDocument
            (
                session,
                userToApplicationComponentAndAccessLevelMappingCollection,
                existingUserToApplicationComponentAndAccessLevelMappingFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(userToApplicationComponentAndAccessLevelMappingsCollectionName, "user to application component and access level mapping"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("user", stringifiedUser), Tuple.Create("application component", stringifiedApplicationComponent), Tuple.Create("access level", stringifiedAccessLevel))
            );

            // Invalidate the user to application component and access level mapping
            UpdateDefinition<UserToApplicationComponentAndAccessLevelMappingDocument> invalidationUpdate = Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, userToApplicationComponentAndAccessLevelMappingCollection, existingUserToApplicationComponentAndAccessLevelMappingFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate user to application component and access level mapping document in collecion '{userToApplicationComponentAndAccessLevelMappingsCollectionName}' when removing user to application component and access level mapping from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveUserToApplicationComponentAndAccessLevelMappingWithTransaction(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveUserToApplicationComponentAndAccessLevelMapping(session, user, applicationComponent, accessLevel, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddGroupToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<GroupToApplicationComponentAndAccessLevelMappingDocument> groupToApplicationComponentAndAccessLevelMappingCollection = database.GetCollection<GroupToApplicationComponentAndAccessLevelMappingDocument>(groupToApplicationComponentAndAccessLevelMappingsCollectionName);
            GroupToApplicationComponentAndAccessLevelMappingDocument newDocument = new()
            {
                Group = groupStringifier.ToString(group),
                ApplicationComponent = applicationComponentStringifier.ToString(applicationComponent),
                AccessLevel = accessLevelStringifier.ToString(accessLevel),
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, groupToApplicationComponentAndAccessLevelMappingCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{groupToApplicationComponentAndAccessLevelMappingsCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddGroupToApplicationComponentAndAccessLevelMappingWithTransaction(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddGroupToApplicationComponentAndAccessLevelMapping(session, group, applicationComponent, accessLevel, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveGroupToApplicationComponentAndAccessLevelMapping(IClientSessionHandle session, TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            String stringifiedGroup = groupStringifier.ToString(group);
            String stringifiedApplicationComponent = applicationComponentStringifier.ToString(applicationComponent);
            String stringifiedAccessLevel = accessLevelStringifier.ToString(accessLevel);
            IMongoCollection<GroupToApplicationComponentAndAccessLevelMappingDocument> groupToApplicationComponentAndAccessLevelMappingCollection = database.GetCollection<GroupToApplicationComponentAndAccessLevelMappingDocument>(groupToApplicationComponentAndAccessLevelMappingsCollectionName);
            FilterDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument> existingGroupToApplicationComponentAndAccessLevelMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.Filter.And
            (
                Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.Group, stringifiedGroup),
                Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.ApplicationComponent, stringifiedApplicationComponent),
                Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.Filter.Eq(document => document.AccessLevel, stringifiedAccessLevel)
            ));
            GetExistingDocument
            (
                session,
                groupToApplicationComponentAndAccessLevelMappingCollection,
                existingGroupToApplicationComponentAndAccessLevelMappingFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(groupToApplicationComponentAndAccessLevelMappingsCollectionName, "group to application component and access level mapping"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("group", stringifiedGroup), Tuple.Create("application component", stringifiedApplicationComponent), Tuple.Create("access level", stringifiedAccessLevel))
            );

            // Invalidate the group to application component and access level mapping
            UpdateDefinition<GroupToApplicationComponentAndAccessLevelMappingDocument> invalidationUpdate = Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, groupToApplicationComponentAndAccessLevelMappingCollection, existingGroupToApplicationComponentAndAccessLevelMappingFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate group to application component and access level mapping document in collecion '{groupToApplicationComponentAndAccessLevelMappingsCollectionName}' when removing group to application component and access level mapping from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveGroupToApplicationComponentAndAccessLevelMappingWithTransaction(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveGroupToApplicationComponentAndAccessLevelMapping(session, group, applicationComponent, accessLevel, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddEntityType(IClientSessionHandle session, String entityType, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<EntityTypeDocument> entityTypesCollection = database.GetCollection<EntityTypeDocument>(entityTypesCollectionName);
            EntityTypeDocument newDocument = new()
            {
                EntityType = entityType,
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, entityTypesCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{entityTypesCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddEntityTypeWithTransaction(String entityType, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddEntityType(session, entityType, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveEntityType(IClientSessionHandle session, String entityType, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<EntityTypeDocument> entityTypesCollection = database.GetCollection<EntityTypeDocument>(entityTypesCollectionName);
            FilterDefinition<EntityTypeDocument> existingEntityTypeFilter = AddTemporalTimestampFilter(transactionTime, Builders<EntityTypeDocument>.Filter.Eq(document => document.EntityType, entityType));
            GetExistingDocument
            (
                session,
                entityTypesCollection,
                existingEntityTypeFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(entityTypesCollectionName, "entity type"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("entity type", entityType))
            );

            // Invalidate any UserToEntityMapping documents
            FilterDefinition<UserToEntityMappingDocument> userToEntityMappingDocumentFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToEntityMappingDocument>.Filter.Eq(document => document.EntityType, entityType));
            InvalidateDocuments(session, userToEntityMappingDocumentFilter, transactionTime, userToEntityMappingsCollectionName, "user to entity mapping", "entity type");
            // Invalidate any GroupToEntityMapping documents
            FilterDefinition<GroupToEntityMappingDocument> groupToEntityMappingDocumentFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToEntityMappingDocument>.Filter.Eq(document => document.EntityType, entityType));
            InvalidateDocuments(session, groupToEntityMappingDocumentFilter, transactionTime, groupToEntityMappingsCollectionName, "group to entity mapping", "entity type");
            // Invalidate any Entity documents
            FilterDefinition<EntityDocument> entityFilter = AddTemporalTimestampFilter(transactionTime, Builders<EntityDocument>.Filter.Eq(document => document.EntityType, entityType));
            InvalidateDocuments(session, entityFilter, transactionTime, entitiesCollectionName, "entity", "entity type");

            // Invalidate the entity type
            UpdateDefinition<EntityTypeDocument> invalidationUpdate = Builders<EntityTypeDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, entityTypesCollection, existingEntityTypeFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate entity type document in collecion '{entityTypesCollectionName}' when removing entity type from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveEntityTypeWithTransaction(String entityType, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveEntityType(session, entityType, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddEntity(IClientSessionHandle session, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<EntityDocument> entitiesCollection = database.GetCollection<EntityDocument>(entitiesCollectionName);
            EntityDocument newDocument = new()
            {
                EntityType = entityType,
                Entity = entity,
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, entitiesCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{entitiesCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddEntityWithTransaction(String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddEntity(session, entityType, entity, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveEntity(IClientSessionHandle session, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<EntityDocument> entitiesCollection = database.GetCollection<EntityDocument>(entitiesCollectionName);
            FilterDefinition<EntityDocument> existingEntityFilter = AddTemporalTimestampFilter(transactionTime, Builders<EntityDocument>.Filter.And
            (
                Builders<EntityDocument>.Filter.Eq(document => document.EntityType, entityType),
                Builders<EntityDocument>.Filter.Eq(document => document.Entity, entity)
            ));
            GetExistingDocument
            (
                session,
                entitiesCollection,
                existingEntityFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(entitiesCollectionName, "entity"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("entity type", entityType), Tuple.Create("entity", entity))
            );

            // Invalidate any UserToEntityMapping documents
            FilterDefinition<UserToEntityMappingDocument> userToEntityMappingDocumentFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToEntityMappingDocument>.Filter.And
            (
                Builders<UserToEntityMappingDocument>.Filter.Eq(document => document.EntityType, entityType),
                Builders<UserToEntityMappingDocument>.Filter.Eq(document => document.Entity, entity)
            ));
            InvalidateDocuments(session, userToEntityMappingDocumentFilter, transactionTime, userToEntityMappingsCollectionName, "user to entity mapping", "entity");
            // Invalidate any GroupToEntityMapping documents
            FilterDefinition<GroupToEntityMappingDocument> groupToEntityMappingDocumentFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToEntityMappingDocument>.Filter.And
            (
                Builders<GroupToEntityMappingDocument>.Filter.Eq(document => document.EntityType, entityType),
                Builders<GroupToEntityMappingDocument>.Filter.Eq(document => document.Entity, entity)
            ));
            InvalidateDocuments(session, groupToEntityMappingDocumentFilter, transactionTime, groupToEntityMappingsCollectionName, "group to entity mapping", "entity");

            // Invalidate the entity
            UpdateDefinition<EntityDocument> invalidationUpdate = Builders<EntityDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, entitiesCollection, existingEntityFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate entity document in collecion '{entitiesCollectionName}' when removing entity from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveEntityWithTransaction(String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveEntity(session, entityType, entity, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddUserToEntityMapping(IClientSessionHandle session, TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<UserToEntityMappingDocument> userToEntityMappingCollection = database.GetCollection<UserToEntityMappingDocument>(userToEntityMappingsCollectionName);
            UserToEntityMappingDocument newDocument = new()
            {
                User = userStringifier.ToString(user),
                EntityType = entityType,
                Entity = entity,
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, userToEntityMappingCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{userToEntityMappingsCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddUserToEntityMappingWithTransaction(TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddUserToEntityMapping(session, user, entityType, entity, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveUserToEntityMapping(IClientSessionHandle session, TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            String stringifiedUser = userStringifier.ToString(user);
            IMongoCollection<UserToEntityMappingDocument> userToEntityMappingCollection = database.GetCollection<UserToEntityMappingDocument>(userToEntityMappingsCollectionName);
            FilterDefinition<UserToEntityMappingDocument> existingUserToEntityMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<UserToEntityMappingDocument>.Filter.And
            (
                Builders<UserToEntityMappingDocument>.Filter.Eq(document => document.User, stringifiedUser),
                Builders<UserToEntityMappingDocument>.Filter.Eq(document => document.EntityType, entityType),
                Builders<UserToEntityMappingDocument>.Filter.Eq(document => document.Entity, entity)
            ));
            GetExistingDocument
            (
                session,
                userToEntityMappingCollection,
                existingUserToEntityMappingFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(userToEntityMappingsCollectionName, "user to entity mapping"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("user", stringifiedUser), Tuple.Create("entity type", entityType), Tuple.Create("entity", entity))
            );

            // Invalidate the user to entity mapping
            UpdateDefinition<UserToEntityMappingDocument> invalidationUpdate = Builders<UserToEntityMappingDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, userToEntityMappingCollection, existingUserToEntityMappingFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate user to entity mapping document in collecion '{userToEntityMappingsCollectionName}' when removing user to entity mapping from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveUserToEntityMappingWithTransaction(TUser user, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveUserToEntityMapping(session, user, entityType, entity, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void AddGroupToEntityMapping(IClientSessionHandle session, TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<GroupToEntityMappingDocument> groupToEntityMappingCollection = database.GetCollection<GroupToEntityMappingDocument>(groupToEntityMappingsCollectionName);
            GroupToEntityMappingDocument newDocument = new()
            {
                Group = groupStringifier.ToString(group),
                EntityType = entityType,
                Entity = entity,
                TransactionFrom = transactionTime,
                TransactionTo = temporalMaxDate
            };
            try
            {
                InsertOne(session, groupToEntityMappingCollection, newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{groupToEntityMappingsCollectionName}'.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void AddGroupToEntityMappingWithTransaction(TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    AddGroupToEntityMapping(session, group, entityType, entity, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        protected void RemoveGroupToEntityMapping(IClientSessionHandle session, TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            String stringifiedGroup = groupStringifier.ToString(group);
            IMongoCollection<GroupToEntityMappingDocument> groupToEntityMappingCollection = database.GetCollection<GroupToEntityMappingDocument>(groupToEntityMappingsCollectionName);
            FilterDefinition<GroupToEntityMappingDocument> existingGroupToEntityMappingFilter = AddTemporalTimestampFilter(transactionTime, Builders<GroupToEntityMappingDocument>.Filter.And
            (
                Builders<GroupToEntityMappingDocument>.Filter.Eq(document => document.Group, stringifiedGroup),
                Builders<GroupToEntityMappingDocument>.Filter.Eq(document => document.EntityType, entityType),
                Builders<GroupToEntityMappingDocument>.Filter.Eq(document => document.Entity, entity)
            ));
            GetExistingDocument
            (
                session,
                groupToEntityMappingCollection,
                existingGroupToEntityMappingFilter,
                transactionTime,
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(groupToEntityMappingsCollectionName, "group to entity mapping"),
                GenerateRemoveElementNoDocumentExistsExceptionMessage(transactionTime, Tuple.Create("group", stringifiedGroup), Tuple.Create("entity type", entityType), Tuple.Create("entity", entity))
            );

            // Invalidate the group to entity mapping
            UpdateDefinition<GroupToEntityMappingDocument> invalidationUpdate = Builders<GroupToEntityMappingDocument>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
            try
            {
                UpdateOne(session, groupToEntityMappingCollection, existingGroupToEntityMappingFilter, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate group to entity mapping document in collecion '{groupToEntityMappingsCollectionName}' when removing group to entity mapping from MongoDB.", e);
            }
            CreateEvent(session, eventId, transactionTime);
        }

        protected void RemoveGroupToEntityMappingWithTransaction(TGroup group, String entityType, String entity, Guid eventId, DateTime transactionTime)
        {
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) =>
                {
                    RemoveGroupToEntityMapping(session, group, entityType, entity, eventId, transactionTime);
                    return new Object();
                });
            }
        }

        #pragma warning restore 1591

        #endregion

        #region Load Methods

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp (and greatest sequence number if multiple states exist at the same timestamp) from MongoDB.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <param name="eventIdToTransactionTimeMapRowDoesntExistException">An exception to throw if no rows exist in the 'EventIdToTransactionTimeMap' collection equal to or sequentially before the specified state time.</param>
        /// <returns>The state of the access manager loaded.</returns>
        protected AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo, Exception eventIdToTransactionTimeMapRowDoesntExistException)
        {
            if (stateTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' must be expressed as UTC.", nameof(stateTime));
            DateTime now = DateTime.UtcNow;
            if (stateTime > now)
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' with value '{stateTime.ToString(dateTimeExceptionMessageFormatString)}' is greater than the current time '{now.ToString(dateTimeExceptionMessageFormatString)}'.", nameof(stateTime));

            // Get the event id and transaction time equal to or immediately before the specified state time
            IMongoCollection<EventIdToTransactionTimeMappingDocument> eventIdToTransactionTimeMappingCollection = database.GetCollection<EventIdToTransactionTimeMappingDocument>(eventIdToTransactionTimeMapCollectionName);
            Guid eventId = default(Guid);
            DateTime transactionTime = DateTime.MinValue;
            Int32 transactionSequence = 0;
            try
            {
                var eventIdToTransactionTimeMappingCollectionFilterDefinition = Builders<EventIdToTransactionTimeMappingDocument>.Filter.Lte(document => document.TransactionTime, stateTime);
                EventIdToTransactionTimeMappingDocument maxTransactionTimeDocument = Find(null, eventIdToTransactionTimeMappingCollection, eventIdToTransactionTimeMappingCollectionFilterDefinition)
                    .SortByDescending(document => document.TransactionTime)
                    .FirstOrDefault();
                if (maxTransactionTimeDocument != null)
                {
                    // Get the largest transaction sequence within the most recent transaction time
                    FilterDefinition<EventIdToTransactionTimeMappingDocument> maxTransactionTimeFilter = Builders<EventIdToTransactionTimeMappingDocument>.Filter.Eq(document => document.TransactionTime, maxTransactionTimeDocument.TransactionTime);
                    EventIdToTransactionTimeMappingDocument maxTransactionSequenceDocument = Find(null, eventIdToTransactionTimeMappingCollection, maxTransactionTimeFilter)
                        .SortByDescending(document => document.TransactionSequence)
                        .FirstOrDefault();
                    eventId = maxTransactionSequenceDocument.EventId;
                    transactionTime = maxTransactionSequenceDocument.TransactionTime;
                    transactionSequence = maxTransactionSequenceDocument.TransactionSequence;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to retrieve access manager state from collection '{eventIdToTransactionTimeMapCollectionName}' when loading from MongoDB.", e);
            }
            if (transactionTime == DateTime.MinValue)
                throw eventIdToTransactionTimeMapRowDoesntExistException;

            LoadToAccessManager(transactionTime, accessManagerToLoadTo);

            return new AccessManagerState(eventId, transactionTime, transactionSequence);
        }

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp from MongoDB into the specified AccessManager instance.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        protected void LoadToAccessManager(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            accessManagerToLoadTo.Clear();
            foreach (String currentUser in GetUsers(stateTime))
            {
                accessManagerToLoadTo.AddUser(userStringifier.FromString(currentUser));
            }
            foreach (String currentGroup in GetGroups(stateTime))
            {
                accessManagerToLoadTo.AddGroup(groupStringifier.FromString(currentGroup));
            }
            foreach (Tuple<String, String> currentUserToGroupMapping in GetUserToGroupMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToGroupMapping(userStringifier.FromString(currentUserToGroupMapping.Item1), groupStringifier.FromString(currentUserToGroupMapping.Item2));
            }
            foreach (Tuple<String, String> currentGroupToGroupMapping in GetGroupToGroupMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToGroupMapping(groupStringifier.FromString(currentGroupToGroupMapping.Item1), groupStringifier.FromString(currentGroupToGroupMapping.Item2));
            }
            foreach (Tuple<String, String, String> currentUserToApplicationComponentAndAccessLevelMapping in GetUserToApplicationComponentAndAccessLevelMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToApplicationComponentAndAccessLevelMapping
                (
                    userStringifier.FromString(currentUserToApplicationComponentAndAccessLevelMapping.Item1),
                    applicationComponentStringifier.FromString(currentUserToApplicationComponentAndAccessLevelMapping.Item2),
                    accessLevelStringifier.FromString(currentUserToApplicationComponentAndAccessLevelMapping.Item3)
                );
            }
            foreach (Tuple<String, String, String> currentGroupToApplicationComponentAndAccessLevelMapping in GetGroupToApplicationComponentAndAccessLevelMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToApplicationComponentAndAccessLevelMapping
                (
                    groupStringifier.FromString(currentGroupToApplicationComponentAndAccessLevelMapping.Item1),
                    applicationComponentStringifier.FromString(currentGroupToApplicationComponentAndAccessLevelMapping.Item2),
                    accessLevelStringifier.FromString(currentGroupToApplicationComponentAndAccessLevelMapping.Item3)
                );
            }
            foreach (String currentEntityType in GetEntityTypes(stateTime))
            {
                accessManagerToLoadTo.AddEntityType(currentEntityType);
            }
            foreach (Tuple<String, String> currentEntityTypeAndEntity in GetEntities(stateTime))
            {
                accessManagerToLoadTo.AddEntity(currentEntityTypeAndEntity.Item1, currentEntityTypeAndEntity.Item2);
            }
            foreach (Tuple<String, String, String> currentUserToEntityMapping in GetUserToEntityMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToEntityMapping
                (
                    userStringifier.FromString(currentUserToEntityMapping.Item1),
                    currentUserToEntityMapping.Item2,
                    currentUserToEntityMapping.Item3
                );
            }
            foreach (Tuple<String, String, String> currentGroupToEntityMapping in GetGroupToEntityMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToEntityMapping
                (
                    groupStringifier.FromString(currentGroupToEntityMapping.Item1),
                    currentGroupToEntityMapping.Item2,
                    currentGroupToEntityMapping.Item3
                );
            }
        }

        #pragma warning disable 1591

        // The following Get*() methods return all elements in the database valid at the specified state time

        protected IEnumerable<String> GetUsers(DateTime stateTime)
        {
            return GetElements
            (
                stateTime, 
                usersCollectionName, 
                "users",
                (UserDocument document) => document.User
            );
        }

        protected IEnumerable<String> GetGroups(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                groupsCollectionName,
                "groups",
                (GroupDocument document) => document.Group
            );
        }

        protected IEnumerable<Tuple<String, String>> GetUserToGroupMappings(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                userToGroupMappingsCollectionName,
                "user to group mappings",
                (UserToGroupMappingDocument document) => Tuple.Create(document.User, document.Group)
            );
        }

        protected IEnumerable<Tuple<String, String>> GetGroupToGroupMappings(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                groupToGroupMappingsCollectionName,
                "group to group mappings",
                (GroupToGroupMappingDocument document) => Tuple.Create(document.FromGroup, document.ToGroup)
            );
        }

        protected IEnumerable<Tuple<String, String, String>> GetUserToApplicationComponentAndAccessLevelMappings(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                userToApplicationComponentAndAccessLevelMappingsCollectionName,
                "user to application component and access level mappings",
                (UserToApplicationComponentAndAccessLevelMappingDocument document) => Tuple.Create(document.User, document.ApplicationComponent, document.AccessLevel)
            );
        }

        protected IEnumerable<Tuple<String, String, String>> GetGroupToApplicationComponentAndAccessLevelMappings(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                groupToApplicationComponentAndAccessLevelMappingsCollectionName,
                "group to application component and access level mappings",
                (GroupToApplicationComponentAndAccessLevelMappingDocument document) => Tuple.Create(document.Group, document.ApplicationComponent, document.AccessLevel)
            );
        }

        protected IEnumerable<String> GetEntityTypes(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                entityTypesCollectionName,
                "entity types",
                (EntityTypeDocument document) => document.EntityType
            );
        }

        protected IEnumerable<Tuple<String, String>> GetEntities(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                entitiesCollectionName,
                "entities",
                (EntityDocument document) => Tuple.Create(document.EntityType, document.Entity)
            );
        }

        protected IEnumerable<Tuple<String, String, String>> GetUserToEntityMappings(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                userToEntityMappingsCollectionName,
                "user to entity mappings",
                (UserToEntityMappingDocument document) => Tuple.Create(document.User, document.EntityType, document.Entity)
            );
        }

        protected IEnumerable<Tuple<String, String, String>> GetGroupToEntityMappings(DateTime stateTime)
        {
            return GetElements
            (
                stateTime,
                groupToEntityMappingsCollectionName,
                "group to entity mappings",
                (GroupToEntityMappingDocument document) => Tuple.Create(document.Group, document.EntityType, document.Entity)
            );
        }

        #pragma warning restore 1591

        /// <summary>
        /// Returns all elements in the database valid at the specified state time.
        /// </summary>
        /// <typeparam name="TElement">The type of element to read/return.</typeparam>
        /// <typeparam name="TDocument">The type of document the elements are stored in in MongoDB.</typeparam>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="collectionName">The name of the collection to read the elements from.</param>
        /// <param name="elementName">The name of the element being read (e.g. 'users', to use in exception messages).</param>
        /// <returns>A collection of all elements in the database valid at the specified state time.</returns>
        protected IEnumerable<TElement> GetElements<TElement, TDocument>(DateTime stateTime, String collectionName, String elementName, Expression<Func<TDocument, TElement>> documentToElementConversionFunction)
            where TDocument : DocumentBase
        {
            var elementCollection = database.GetCollection<TDocument>(collectionName);
            FilterDefinition<TDocument> filterDefinition = AddTemporalTimestampFilter(stateTime, FilterDefinition<TDocument>.Empty);
            ProjectionDefinition<TDocument, TElement> projection = new FindExpressionProjectionDefinition<TDocument, TElement>(documentToElementConversionFunction);
            try
            {
                return Find(null, elementCollection, filterDefinition).Project(projection).ToEnumerable();
            }
            catch (Exception e)
            {
                throw new Exception(GenerateLoadFailedExceptionMessage(elementName, collectionName), e);
            }
        }

        #endregion

        #region Collection and Index Creation Methods

        /// <summary>
        /// Creates all AccessManager collections in the database if they don't already exist.
        /// </summary>
        protected void CreateCollections()
        {
            List<String> allCollectionNames = new()
            {
                eventIdToTransactionTimeMapCollectionName,
                usersCollectionName,
                groupsCollectionName,
                userToGroupMappingsCollectionName,
                groupToGroupMappingsCollectionName,
                userToApplicationComponentAndAccessLevelMappingsCollectionName,
                groupToApplicationComponentAndAccessLevelMappingsCollectionName,
                entityTypesCollectionName,
                entitiesCollectionName,
                userToEntityMappingsCollectionName,
                groupToEntityMappingsCollectionName
            };
            foreach (String currentCollectionName in allCollectionNames)
            {
                CreateCollection(currentCollectionName);
            }
        }

        /// <summary>
        /// Creates a collection in the database if it doesn't already exist.
        /// </summary>
        /// <param name="collectionName">The name of the collection to create.</param>
        protected void CreateCollection(String collectionName)
        {
            foreach (String currentCollectionName in database.ListCollectionNames().ToEnumerable())
            {
                if (currentCollectionName == collectionName)
                {
                    return;
                }
            }
            try
            {
                database.CreateCollection(collectionName);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create collection '{collectionName}' in MongoDB.", e);
            }
        }

        /// <summary>
        /// Creates all indexes in the database if they don't already exist.
        /// </summary>
        protected void CreateIndexes()
        {
            // 'EventIdToTransactionTimeMap' indexes
            var eventIdToTransactionTimeMapCollection = database.GetCollection<EventIdToTransactionTimeMappingDocument>(eventIdToTransactionTimeMapCollectionName);
            var eventIdToTransactionTimeMapCollectionEventIdIndexModel = new CreateIndexModel<EventIdToTransactionTimeMappingDocument>(Builders<EventIdToTransactionTimeMappingDocument>.IndexKeys.Ascending(document => document.EventId));
            eventIdToTransactionTimeMapCollection.Indexes.CreateOne(eventIdToTransactionTimeMapCollectionEventIdIndexModel);
            var eventIdToTransactionTimeMapCollectionTransactioIindexModel = new CreateIndexModel<EventIdToTransactionTimeMappingDocument>(Builders<EventIdToTransactionTimeMappingDocument>.IndexKeys
                .Descending(document => document.TransactionTime)
                .Descending(document => document.TransactionSequence));
            eventIdToTransactionTimeMapCollection.Indexes.CreateOne(eventIdToTransactionTimeMapCollectionTransactioIindexModel);
            // 'Users' indexes
            var usersCollection = database.GetCollection<UserDocument>(usersCollectionName);
            var usersCollectionUserIndexModel = new CreateIndexModel<UserDocument>(Builders<UserDocument>.IndexKeys
                .Ascending(document => document.User)
                .Ascending(document => document.TransactionTo));
            usersCollection.Indexes.CreateOne(usersCollectionUserIndexModel);
            CreateTransactionFieldIndex<UserDocument>(usersCollectionName);
            // 'Groups' indexes
            var groupsCollection = database.GetCollection<GroupDocument>(groupsCollectionName);
            var groupsCollectionUserIndexModel = new CreateIndexModel<GroupDocument>(Builders<GroupDocument>.IndexKeys
                .Ascending(document => document.Group)
                .Ascending(document => document.TransactionTo));
            groupsCollection.Indexes.CreateOne(groupsCollectionUserIndexModel);
            CreateTransactionFieldIndex<GroupDocument>(groupsCollectionName);
            // 'UserToGroupMappings' indexes
            var userToGroupMappingCollection = database.GetCollection<UserToGroupMappingDocument>(userToGroupMappingsCollectionName);
            var userToGroupMappingCollectionUserIndexModel = new CreateIndexModel<UserToGroupMappingDocument>(Builders<UserToGroupMappingDocument>.IndexKeys
                .Ascending(document => document.User)
                .Ascending(document => document.TransactionTo));
            userToGroupMappingCollection.Indexes.CreateOne(userToGroupMappingCollectionUserIndexModel);
            var userToGroupMappingCollectionGroupIndexModel = new CreateIndexModel<UserToGroupMappingDocument>(Builders<UserToGroupMappingDocument>.IndexKeys
                .Ascending(document => document.Group)
                .Ascending(document => document.TransactionTo));
            userToGroupMappingCollection.Indexes.CreateOne(userToGroupMappingCollectionGroupIndexModel);
            CreateTransactionFieldIndex<UserToGroupMappingDocument>(userToGroupMappingsCollectionName);
            // 'GroupToGroupMappings' indexes
            var groupToGroupMappingCollection = database.GetCollection<GroupToGroupMappingDocument>(groupToGroupMappingsCollectionName);
            var groupToGroupMappingCollectionFromGroupIndexModel = new CreateIndexModel<GroupToGroupMappingDocument>(Builders<GroupToGroupMappingDocument>.IndexKeys
                .Ascending(document => document.FromGroup)
                .Ascending(document => document.TransactionTo));
            groupToGroupMappingCollection.Indexes.CreateOne(groupToGroupMappingCollectionFromGroupIndexModel);
            var groupToGroupMappingCollectionToGroupIndexModel = new CreateIndexModel<GroupToGroupMappingDocument>(Builders<GroupToGroupMappingDocument>.IndexKeys
                .Ascending(document => document.ToGroup)
                .Ascending(document => document.TransactionTo));
            groupToGroupMappingCollection.Indexes.CreateOne(groupToGroupMappingCollectionToGroupIndexModel);
            CreateTransactionFieldIndex<GroupToGroupMappingDocument>(groupToGroupMappingsCollectionName);
            // 'UserToApplicationComponentAndAccessLevelMappings' indexes
            var userToApplicationComponentAndAccessLevelMappingCollection = database.GetCollection<UserToApplicationComponentAndAccessLevelMappingDocument>(userToApplicationComponentAndAccessLevelMappingsCollectionName); 
            var userToApplicationComponentAndAccessLevelMappingCollectionUserIndexModel = new CreateIndexModel<UserToApplicationComponentAndAccessLevelMappingDocument>(Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.IndexKeys
                .Ascending(document => document.User)
                .Ascending(document => document.ApplicationComponent)
                .Ascending(document => document.AccessLevel)
                .Ascending(document => document.TransactionTo));
            userToApplicationComponentAndAccessLevelMappingCollection.Indexes.CreateOne(userToApplicationComponentAndAccessLevelMappingCollectionUserIndexModel);
            var userToApplicationComponentAndAccessLevelMappingCollectionApplicationComponentIndexModel = new CreateIndexModel<UserToApplicationComponentAndAccessLevelMappingDocument>(Builders<UserToApplicationComponentAndAccessLevelMappingDocument>.IndexKeys
                .Ascending(document => document.ApplicationComponent)
                .Ascending(document => document.AccessLevel)
                .Ascending(document => document.TransactionTo));
            userToApplicationComponentAndAccessLevelMappingCollection.Indexes.CreateOne(userToApplicationComponentAndAccessLevelMappingCollectionApplicationComponentIndexModel);
            CreateTransactionFieldIndex<UserToApplicationComponentAndAccessLevelMappingDocument>(userToApplicationComponentAndAccessLevelMappingsCollectionName);
            // 'GroupToApplicationComponentAndAccessLevelMappings' indexes
            var groupToApplicationComponentAndAccessLevelMappingCollection = database.GetCollection<GroupToApplicationComponentAndAccessLevelMappingDocument>(groupToApplicationComponentAndAccessLevelMappingsCollectionName);
            var groupToApplicationComponentAndAccessLevelMappingCollectionGroupIndexModel = new CreateIndexModel<GroupToApplicationComponentAndAccessLevelMappingDocument>(Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.IndexKeys
                .Ascending(document => document.Group)
                .Ascending(document => document.ApplicationComponent)
                .Ascending(document => document.AccessLevel)
                .Ascending(document => document.TransactionTo));
            groupToApplicationComponentAndAccessLevelMappingCollection.Indexes.CreateOne(groupToApplicationComponentAndAccessLevelMappingCollectionGroupIndexModel);
            var groupToApplicationComponentAndAccessLevelMappingCollectionApplicationComponentIndexModel = new CreateIndexModel<GroupToApplicationComponentAndAccessLevelMappingDocument>(Builders<GroupToApplicationComponentAndAccessLevelMappingDocument>.IndexKeys
                .Ascending(document => document.ApplicationComponent)
                .Ascending(document => document.AccessLevel)
                .Ascending(document => document.TransactionTo));
            groupToApplicationComponentAndAccessLevelMappingCollection.Indexes.CreateOne(groupToApplicationComponentAndAccessLevelMappingCollectionApplicationComponentIndexModel);
            CreateTransactionFieldIndex<GroupToApplicationComponentAndAccessLevelMappingDocument>(groupToApplicationComponentAndAccessLevelMappingsCollectionName);
            // 'EntityTypes' indexes
            var entityTypesCollection = database.GetCollection<EntityTypeDocument>(entityTypesCollectionName);
            var entityTypesCollectionEntityTypeIndexModel = new CreateIndexModel<EntityTypeDocument>(Builders<EntityTypeDocument>.IndexKeys
                .Ascending(document => document.EntityType)
                .Ascending(document => document.TransactionTo));
            entityTypesCollection.Indexes.CreateOne(entityTypesCollectionEntityTypeIndexModel);
            CreateTransactionFieldIndex<EntityTypeDocument>(entityTypesCollectionName);
            // 'Entities' indexes
            var entitiesCollection = database.GetCollection<EntityDocument>(entitiesCollectionName);
            var entitiesCollectionEntityIndexModel = new CreateIndexModel<EntityDocument>(Builders<EntityDocument>.IndexKeys
                .Ascending(document => document.EntityType)
                .Ascending(document => document.Entity)
                .Ascending(document => document.TransactionTo));
            entitiesCollection.Indexes.CreateOne(entitiesCollectionEntityIndexModel);
            CreateTransactionFieldIndex<EntityDocument>(entitiesCollectionName);
            // 'UserToEntityMappings' indexes
            var userToEntityMappingCollection = database.GetCollection<UserToEntityMappingDocument>(userToEntityMappingsCollectionName);
            var userToEntityMappingCollectionUserIndexModel = new CreateIndexModel<UserToEntityMappingDocument>(Builders<UserToEntityMappingDocument>.IndexKeys
                .Ascending(document => document.User)
                .Ascending(document => document.EntityType)
                .Ascending(document => document.Entity)
                .Ascending(document => document.TransactionTo));
            userToEntityMappingCollection.Indexes.CreateOne(userToEntityMappingCollectionUserIndexModel);
            var userToEntityMappingCollectionEntityTypeIndexModel = new CreateIndexModel<UserToEntityMappingDocument>(Builders<UserToEntityMappingDocument>.IndexKeys
                .Ascending(document => document.EntityType)
                .Ascending(document => document.Entity)
                .Ascending(document => document.TransactionTo));
            userToEntityMappingCollection.Indexes.CreateOne(userToEntityMappingCollectionEntityTypeIndexModel);
            CreateTransactionFieldIndex<UserToEntityMappingDocument>(userToEntityMappingsCollectionName);
            // 'GroupToEntityMappings' indexes
            var groupToEntityMappingCollection = database.GetCollection<GroupToEntityMappingDocument>(groupToEntityMappingsCollectionName);
            var groupToEntityMappingCollectionGroupIndexModel = new CreateIndexModel<GroupToEntityMappingDocument>(Builders<GroupToEntityMappingDocument>.IndexKeys
                .Ascending(document => document.Group)
                .Ascending(document => document.EntityType)
                .Ascending(document => document.Entity)
                .Ascending(document => document.TransactionTo));
            groupToEntityMappingCollection.Indexes.CreateOne(groupToEntityMappingCollectionGroupIndexModel);
            var groupToEntityMappingCollectionEntityTypeIndexModel = new CreateIndexModel<GroupToEntityMappingDocument>(Builders<GroupToEntityMappingDocument>.IndexKeys
                .Ascending(document => document.EntityType)
                .Ascending(document => document.Entity)
                .Ascending(document => document.TransactionTo));
            groupToEntityMappingCollection.Indexes.CreateOne(groupToEntityMappingCollectionEntityTypeIndexModel);
            CreateTransactionFieldIndex<GroupToEntityMappingDocument>(groupToEntityMappingsCollectionName);
        }

        /// <summary>
        /// Creates a <see href="https://www.mongodb.com/docs/manual/core/indexes/index-types/index-compound/">compound index</see> on the <see cref="DocumentBase.TransactionFrom"/> and <see cref="DocumentBase.TransactionTo"/> fields of a collection holding documents derived from <see cref="DocumentBase"/>.
        /// </summary>
        /// <typeparam name="T">The type of documents (deriving from <see cref="DocumentBase"/>) held by the collection the index is being created on.</typeparam>
        /// <param name="collectionName">The name of the collection to create the index on.</param>
        protected void CreateTransactionFieldIndex<T>(String collectionName) where T : DocumentBase
        {
            var collection = database.GetCollection<T>(collectionName);
            var collectionTransactionIndexModel = new CreateIndexModel<T>
            (
                Builders<T>.IndexKeys
                    .Ascending(document => document.TransactionFrom)
                    .Ascending(document => document.TransactionTo)
            );
            collection.Indexes.CreateOne(collectionTransactionIndexModel);
        }

        #endregion

        /// <summary>
        /// Begins a find fluent interface, optionally within a specified session.
        /// </summary>
        /// <typeparam name="T">The type of document(s) to return.</typeparam>
        /// <param name="session">The (optional) session to execute the find in.  Set to null to not run in a session.</param>
        /// <param name="collection">The collection to read from.</param>
        /// <param name="filter">The filter to apply to execute the find.</param>
        /// <returns>A find fluent interface.</returns>
        protected IFindFluent<T, T> Find<T>(IClientSessionHandle? session, IMongoCollection<T> collection, FilterDefinition<T> filter)
        {
            if (session == null)
            {
                return collection.Find(filter);
            }
            else
            {
                return collection.Find(session, filter);
            }
        }

        /// <summary>
        /// Inserts a single document, optionally within a specified session.
        /// </summary>
        /// <typeparam name="T">The type of document to insert.</typeparam>
        /// <param name="session">The (optional) session to execute the insert in.  Set to null to not run in a session.</param>
        /// <param name="collection">The collection to insert into.</param>
        /// <param name="document">The document to insert.</param>
        protected void InsertOne<T>(IClientSessionHandle? session, IMongoCollection<T> collection, T document)
        {
            if (session == null)
            {
                collection.InsertOne(document);
            }
            else
            {
                collection.InsertOne(session, document);
            }
        }

        /// <summary>
        /// Updates a single document, optionally within a specified session.
        /// </summary>
        /// <typeparam name="T">The type of document to update.</typeparam>
        /// <param name="session">The (optional) session to execute the update in.  Set to null to not run in a session.</param>
        /// <param name="collection">The collection to update.</param>
        /// <param name="filterDefinition">The filter.</param>
        /// <param name="updateDefinition">The update.</param>
        /// <returns>The result of the update operation.</returns>
        protected UpdateResult UpdateOne<T>(IClientSessionHandle? session, IMongoCollection<T> collection, FilterDefinition<T> filterDefinition, UpdateDefinition<T> updateDefinition)
        {
            if (session == null)
            {
                return collection.UpdateOne(filterDefinition, updateDefinition);
            }
            else
            {
                return collection.UpdateOne(session, filterDefinition, updateDefinition);
            }
        }

        /// <summary>
        /// Updates multiple documents, optionally within a specified session.
        /// </summary>
        /// <typeparam name="T">The type of documents to update.</typeparam>
        /// <param name="session">The (optional) session to execute the update in.  Set to null to not run in a session.</param>
        /// <param name="collection">The collection to update.</param>
        /// <param name="filterDefinition">The filter.</param>
        /// <param name="updateDefinition">The update.</param>
        /// <returns>The result of the update operation.</returns>
        protected UpdateResult UpdateMany<T>(IClientSessionHandle? session, IMongoCollection<T> collection, FilterDefinition<T> filterDefinition, UpdateDefinition<T> updateDefinition)
        {
            if (session == null)
            {
                return collection.UpdateMany(filterDefinition, updateDefinition);
            }
            else
            {
                return collection.UpdateMany(session, filterDefinition, updateDefinition);
            }
        }

        /// <summary>
        /// Adds a timestamp filter (following the data's temporal model) to the <see cref="DocumentBase.TransactionFrom"/> and <see cref="DocumentBase.TransactionTo"/> of the data being filtered.
        /// </summary>
        /// <typeparam name="T">The type of data (deriving from <see cref="DocumentBase"/>) being filtered.</typeparam>
        /// <param name="transactionTime">The timestamp of the temporal filter.</param>
        /// <param name="existingFilters">The existing filter(s) to add to.</param>
        /// <returns>The appended <see cref="FilterDefinition{TDocument}"/>.</returns>
        protected FilterDefinition<T> AddTemporalTimestampFilter<T>(DateTime transactionTime, params FilterDefinition<T>[] existingFilters)
            where T : DocumentBase
        {
            IEnumerable<FilterDefinition<T>> allFilters = new List<FilterDefinition<T>>(existingFilters)
                .Append(Builders<T>.Filter.Lte(document => document.TransactionFrom, transactionTime))
                .Append(Builders<T>.Filter.Gte(document => document.TransactionTo, transactionTime));

            return Builders<T>.Filter.And(allFilters);
        }

        /// <summary>
        /// Invalidates a set of documents by setting their temporal <see cref="DocumentBase.TransactionTo"/> field to a historic value.  Designed to be used as part of an element remove/delete operation.
        /// </summary>
        /// <typeparam name="T">The type of data (deriving from <see cref="DocumentBase"/>) being invalidated.</typeparam>
        /// <param name="session">The (optional) session to execute the update in.  Set to null to not run in a session.</param>
        /// <param name="filterDefinition">A filter definition which identifies the documents to be invalidated.</param>
        /// <param name="transactionTime">The time that the invalidation (i.e. remove/delete operation) occurred.</param>
        /// <param name="collectionName">The name of the collection to apply the invalidation to.</param>
        /// <param name="documentDescription">A description of the documents being invalidated.  To use in exception messages (e.g. 'user to group mapping').</param>
        /// <param name="removedElementDescription">The type of element being removed/deleted. To use in exception messages (e.g. 'user').</param>
        protected void InvalidateDocuments<T>(IClientSessionHandle session, FilterDefinition<T> filterDefinition, DateTime transactionTime, String collectionName, String documentDescription, String removedElementDescription)
            where T : DocumentBase
        {
            try
            {
                UpdateDefinition<T> invalidationUpdate = Builders<T>.Update.Set(document => document.TransactionTo, SubtractTemporalMinimumTimeUnit(transactionTime));
                IMongoCollection<T> collection = database.GetCollection<T>(collectionName);
                UpdateMany(session, collection, filterDefinition, invalidationUpdate);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to invalidate {documentDescription} document(s) in collecion '{collectionName}' when removing {removedElementDescription} from MongoDB.", e);
            }
        }

        /// <summary>
        /// Retrieves an existing document as part of an element remove/delete operation.
        /// </summary>
        /// <typeparam name="T">The type of data (deriving from <see cref="DocumentBase"/>) being removed/deleted.</typeparam>
        /// <param name="session">The (optional) session to execute the get/find operation in.  Set to null to not run in a session.</param>
        /// <param name="collection">The collection to retrieve the data from.</param>
        /// <param name="baseFilterDefinition">The filter definition which retrieves the document (excluding the temporal model timestamp filter).</param>
        /// <param name="transactionTime">The time that the remove/delete operation occurred.</param>
        /// <param name="findFailedExceptionDescription">The description/message to use in an exception if the get/find operation against MongoDB fails.</param>
        /// <param name="noDocumentExistsExceptionDescription">The description/message to use in an exception if the existing document was not found.</param>
        /// <returns>The document.</returns>
        protected T GetExistingDocument<T>
        (
            IClientSessionHandle session,
            IMongoCollection<T> collection,
            FilterDefinition<T> baseFilterDefinition,
            DateTime transactionTime,
            String findFailedExceptionDescription,
            String noDocumentExistsExceptionDescription
        )
            where T : DocumentBase
        {
            FilterDefinition<T> filterDefinition = AddTemporalTimestampFilter(transactionTime, baseFilterDefinition);
            T existingDocument;
            try
            {
                existingDocument = Find(session, collection, filterDefinition).FirstOrDefault();
            }
            catch (Exception e)
            {
                throw new Exception(findFailedExceptionDescription, e);
            }
            if (existingDocument == null)
                throw new Exception(noDocumentExistsExceptionDescription);

            return existingDocument;
        }

        /// <summary>
        /// Subtracts the most granular supported time unit in the data's temporal model from the specified <see cref="DateTime" />.
        /// </summary>
        /// <param name="inputDateTime">The <see cref="DateTime" /> to subtract from.</param>
        /// <returns>The <see cref="DateTime" /> after the subtraction.</returns>
        protected DateTime SubtractTemporalMinimumTimeUnit(DateTime inputDateTime)
        {
            return inputDateTime.Subtract(TimeSpan.FromTicks(1));
        }

        /// <summary>
        /// Generates an exception message for the case that an attempt to retrieve the existing document failed, as part of an element remove/delete operation.
        /// </summary>
        /// <param name="collectionName">The name of the collection the documents are being removed/deleted in.</param>
        /// <param name="removedElementDescription">The type of element being removed/deleted (e.g. 'user').</param>
        /// <returns>The exception message.</returns>
        protected String GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(String collectionName, String removedElementDescription)
        {
            return $"Failed to retrieve existing document from collecion '{collectionName}' when removing {removedElementDescription} from MongoDB.";
        }

        /// <summary>
        /// Generates an exception message for the case that reading element data from a collection failed, as part of a load operation.
        /// </summary>
        /// <param name="elementName">The name of the element being read (e.g. 'users').</param>
        /// <param name="collectionName">The name of the collection being read from.</param>
        /// <returns></returns>
        protected String GenerateLoadFailedExceptionMessage(String elementName, String collectionName)
        {
            return $"Failed to read {elementName} from collection 'collectionName 'when loading from MongoDB.";
        }

        /// <summary>
        ///  Generates an exception message for the case that no document to remove/delete exists, part of an element remove/delete operation.
        /// </summary>
        /// <param name="transactionTime">The time that the remove/delete operation occurred.</param>
        /// <param name="keyDocumentProperties">The name value pairs of the properties that uniquely identify the document being removed/deleted.  Each tuple contains: the name of the key property, and the value of the key property.</param>
        /// <returns>The exception message.</returns>
        protected String GenerateRemoveElementNoDocumentExistsExceptionMessage(DateTime transactionTime, params Tuple<String, String>[] keyDocumentProperties)
        {
            StringBuilder keyDocumentPropertiesBuilder = new();
            Boolean firstKeyDocumentProperty = true;
            foreach (Tuple<String, String> currentKeyDocumentProperty in keyDocumentProperties)
            {
                if (firstKeyDocumentProperty == false)
                {
                    keyDocumentPropertiesBuilder.Append(",");
                }
                else
                {
                    firstKeyDocumentProperty = false;
                }
                keyDocumentPropertiesBuilder.Append($" {currentKeyDocumentProperty.Item1} '{currentKeyDocumentProperty.Item2}'");
            }

            return $"No document exists for{keyDocumentPropertiesBuilder.ToString()}, and transaction time '{transactionTime.ToString(dateTimeExceptionMessageFormatString)}'.";
        }

        /// <summary>
        /// Populates the dictionary in the 'eventTypeToPersistenceActionMap' field.
        /// </summary>
        protected void PopulateEventTypeToPersistenceActionMap()
        {
            #pragma warning disable 8625

            eventTypeToPersistenceActionMap.Add
            (
                typeof(UserEventBufferItem<TUser>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    UserEventBufferItem<TUser> typedEvent = (UserEventBufferItem<TUser>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddUserWithTransaction(typedEvent.User, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddUser(null, typedEvent.User, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveUserWithTransaction(typedEvent.User, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveUser(null, typedEvent.User, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(GroupEventBufferItem<TGroup>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    GroupEventBufferItem<TGroup> typedEvent = (GroupEventBufferItem<TGroup>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddGroupWithTransaction(typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddGroup(null, typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveGroupWithTransaction(typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveGroup(null, typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(UserToGroupMappingEventBufferItem<TUser, TGroup>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    UserToGroupMappingEventBufferItem<TUser, TGroup> typedEvent = (UserToGroupMappingEventBufferItem<TUser, TGroup>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddUserToGroupMappingWithTransaction(typedEvent.User, typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddUserToGroupMapping(null, typedEvent.User, typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveUserToGroupMappingWithTransaction(typedEvent.User, typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveUserToGroupMapping(null, typedEvent.User, typedEvent.Group, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(GroupToGroupMappingEventBufferItem<TGroup>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    GroupToGroupMappingEventBufferItem<TGroup> typedEvent = (GroupToGroupMappingEventBufferItem<TGroup>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddGroupToGroupMappingWithTransaction(typedEvent.FromGroup, typedEvent.ToGroup, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddGroupToGroupMapping(null, typedEvent.FromGroup, typedEvent.ToGroup, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveGroupToGroupMappingWithTransaction(typedEvent.FromGroup, typedEvent.ToGroup, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveGroupToGroupMapping(null, typedEvent.FromGroup, typedEvent.ToGroup, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> typedEvent = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddUserToApplicationComponentAndAccessLevelMappingWithTransaction(typedEvent.User, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddUserToApplicationComponentAndAccessLevelMapping(null, typedEvent.User, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveUserToApplicationComponentAndAccessLevelMappingWithTransaction(typedEvent.User, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveUserToApplicationComponentAndAccessLevelMapping(null, typedEvent.User, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> typedEvent = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddGroupToApplicationComponentAndAccessLevelMappingWithTransaction(typedEvent.Group, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddGroupToApplicationComponentAndAccessLevelMapping(null, typedEvent.Group, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveGroupToApplicationComponentAndAccessLevelMappingWithTransaction(typedEvent.Group, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveGroupToApplicationComponentAndAccessLevelMapping(null, typedEvent.Group, typedEvent.ApplicationComponent, typedEvent.AccessLevel, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(EntityTypeEventBufferItem),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    EntityTypeEventBufferItem typedEvent = (EntityTypeEventBufferItem)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddEntityTypeWithTransaction(typedEvent.EntityType, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddEntityType(null, typedEvent.EntityType, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveEntityTypeWithTransaction(typedEvent.EntityType, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveEntityType(null, typedEvent.EntityType, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(EntityEventBufferItem),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    EntityEventBufferItem typedEvent = (EntityEventBufferItem)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddEntityWithTransaction(typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddEntity(null, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveEntityWithTransaction(typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveEntity(null, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(UserToEntityMappingEventBufferItem<TUser>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    UserToEntityMappingEventBufferItem<TUser> typedEvent = (UserToEntityMappingEventBufferItem<TUser>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddUserToEntityMappingWithTransaction(typedEvent.User, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddUserToEntityMapping(null, typedEvent.User, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveUserToEntityMappingWithTransaction(typedEvent.User, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveUserToEntityMapping(null, typedEvent.User, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );
            eventTypeToPersistenceActionMap.Add
            (
                typeof(GroupToEntityMappingEventBufferItem<TGroup>),
                (TemporalEventBufferItemBase eventBufferItem) =>
                {
                    GroupToEntityMappingEventBufferItem<TGroup> typedEvent = (GroupToEntityMappingEventBufferItem<TGroup>)eventBufferItem;
                    if (typedEvent.EventAction == EventAction.Add)
                    {
                        if (useTransactions == true)
                        {
                            AddGroupToEntityMappingWithTransaction(typedEvent.Group, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            AddGroupToEntityMapping(null, typedEvent.Group, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                    else
                    {
                        if (useTransactions == true)
                        {
                            RemoveGroupToEntityMappingWithTransaction(typedEvent.Group, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                        else
                        {
                            RemoveGroupToEntityMapping(null, typedEvent.Group, typedEvent.EntityType, typedEvent.Entity, typedEvent.EventId, typedEvent.OccurredTime);
                        }
                    }
                }
            );

            #pragma warning restore 8625
        }

        #pragma warning disable 1591

        protected void ThrowExceptionIfStringParameterNullOrWhitespace(String parameterName, String parameterValue)
        {
            if (String.IsNullOrWhiteSpace(parameterValue))
            {
                throw new ArgumentNullException(parameterName, $"Parameter '{parameterName}' must contain a value.");
            }
        }

        #pragma warning restore 1591

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the MongoDbAccessManagerTemporalBulkPersister.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~MongoDbAccessManagerTemporalBulkPersister()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    mongoClient.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
