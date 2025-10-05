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

        #pragma warning disable 1591

        #region MongoDb Event Persistence Methods

        protected void CreateEvent(Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<EventIdToTransactionTimeMappingDocument> eventIdToTransactionTimeMappingCollection = database.GetCollection<EventIdToTransactionTimeMappingDocument>(eventIdToTransactionTimeMapCollectionName);
            DateTime lastTransactionTime = DateTime.MinValue;
            Int32 lastTransactionSequence = 0;
            Int32 transactionSequence = 0;

            // Get the most recent transaction time
            try
            {
                EventIdToTransactionTimeMappingDocument lastTransactionTimeDocument = eventIdToTransactionTimeMappingCollection.Find(FilterDefinition<EventIdToTransactionTimeMappingDocument>.Empty)
                    .SortByDescending(document => document.TransactionTime)
                    .FirstOrDefault();
                if (lastTransactionTimeDocument != null)
                {
                    // Get the largest transaction sequence within the most recent transaction time
                    FilterDefinition<EventIdToTransactionTimeMappingDocument> lastTransactionTimeFilter = Builders<EventIdToTransactionTimeMappingDocument>.Filter.Eq(document => document.TransactionTime, lastTransactionTimeDocument.TransactionTime);
                    EventIdToTransactionTimeMappingDocument lastTransactionSequenceDocument = eventIdToTransactionTimeMappingCollection.Find(lastTransactionTimeFilter)
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
                eventIdToTransactionTimeMappingCollection.InsertOne(newDocument);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to insert document into collection '{eventIdToTransactionTimeMapCollectionName}'.", e);
            }
        }

        protected void AddUser(TUser user, Guid eventId, DateTime transactionTime)
        {
            IMongoCollection<UserDocument> usersCollection = database.GetCollection<UserDocument>(usersCollectionName);
            using (IClientSessionHandle session = mongoClient.StartSession())
            {
                session.WithTransaction<Object>((IClientSessionHandle s, CancellationToken ct) => 
                {
                    CreateEvent(eventId, transactionTime);
                    UserDocument newDocument = new()
                    {
                        User = userStringifier.ToString(user), 
                        TransactionFrom = transactionTime, 
                        TransactionTo = temporalMaxDate
                    };
                    try
                    {
                        usersCollection.InsertOne(newDocument);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to insert document into collection '{usersCollectionName}'.", e);
                    }

                    // TODO: Test on BeeLink that WithTransaction() actually does a transaction... and what happens if it fails (2nd command is bogus??)
                    //   Figure out what to do with this return type

                    return new Object();
                });
            }
        }

        #endregion

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
