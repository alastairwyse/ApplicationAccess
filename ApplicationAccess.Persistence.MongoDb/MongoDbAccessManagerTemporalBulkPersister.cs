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
using System.Text;
using MongoDB.Driver;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Persistence.MongoDb.Models.Documents;

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
        // TODO: 
        // Should I check the contents of UpdateResult after updates?
        // Figure out how I'm going to create indexes... e.g. on first call to PersistEvents()
        //   Assume you can't create indexes if collections don't exist...
        //   Hence can I idempotently create all collections aswell, and then idempotently create the indexes??
        // For PersistEvents() will need 2x maps of event type to method to call to process (one for with trans and one for without)
        // Also need to think about how to implement allowing duplicate events...
        //   ... probably there'll need to be a check within the loop inside PersistEvents()
        // Tests for remove need to include error case where the user doesn't exist

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
        /// <summary>The client to connect to MongoDB.</summary>
        protected IMongoClient mongoClient;
        /// <summary>The MongoDB database.</summary>
        protected IMongoDatabase database;
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
        public MongoDbAccessManagerTemporalBulkPersister
        (
            String connectionString,
            String databaseName,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier
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
            mongoClient = new MongoClient(this.connectionString);
            database = mongoClient.GetDatabase(databaseName);
            disposed = false;
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events, bool ignorePreExistingEvents)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public AccessManagerState Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public AccessManagerState Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            throw new NotImplementedException();
        }

        #region Private/Protected Methods

        #region MongoDb Event Persistence Methods

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
                GenerateRemoveElementFindExistingDocumentFailedExceptionMessage("user", usersCollectionName, "user"),
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

        #endregion

        /// <summary>
        /// Begins a find fluent interface, optionally within a specified session.
        /// </summary>
        /// <typeparam name="T">The type of document(s) to return.</typeparam>
        /// <param name="session">The (optional) session to execute the find in.  Set to null to not run in a session.</param>
        /// <param name="collection">The collection to read from.</param>
        /// <param name="filter">The filter to apply to execute the find.</param>
        /// <returns>A find fluent interface.</returns>
        protected IFindFluent<T, T> Find<T>(IClientSessionHandle session, IMongoCollection<T> collection, FilterDefinition<T> filter)
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
        protected void InsertOne<T>(IClientSessionHandle session, IMongoCollection<T> collection, T document)
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
        protected UpdateResult UpdateOne<T>(IClientSessionHandle session, IMongoCollection<T> collection, FilterDefinition<T> filterDefinition, UpdateDefinition<T> updateDefinition)
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
        protected UpdateResult UpdateMany<T>(IClientSessionHandle session, IMongoCollection<T> collection, FilterDefinition<T> filterDefinition, UpdateDefinition<T> updateDefinition)
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
        /// <param name="existingFilters">The existing filters to add to.</param>
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
        /// <param name="documentDescription">A description of the documents being invalidated (e.g. 'user to group mapping').</param>
        /// <param name="collectionName">The name of the collection the documents are being removed/deleted in.</param>
        /// <param name="removedElementDescription">The type of element being removed/deleted (e.g. 'user').</param>
        /// <returns>The exception message.</returns>
        protected String GenerateRemoveElementFindExistingDocumentFailedExceptionMessage(String documentDescription, String collectionName, String removedElementDescription)
        {
            return $"Failed to retrieve existing {documentDescription} document from collecion '{collectionName}' when removing {removedElementDescription} from MongoDB.";
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
