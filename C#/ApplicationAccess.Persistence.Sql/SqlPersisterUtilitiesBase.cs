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
using System.Data;
using System.Globalization;
using ApplicationAccess;
using ApplicationAccess.Persistence;

namespace ApplicationAccess.Persistence.Sql
{
    /// <summary>
    /// Base for classes which provide utility methods for classes which write and read data associated with AccessManager classes to and from a SQL database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class SqlPersisterUtilitiesBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;

        /// <summary>The name of the SQL database platform/product.</summary>
        protected abstract String DatabaseName { get; }

        /// <summary>The format used to represent timestamp columns in SQL query results.</summary>
        protected abstract String TimestampColumnFormatString { get; }

        /// <summary>Generates queries used to read the current state of an AccessManager.</summary>
        protected abstract ReadQueryGeneratorBase ReadQueryGenerator { get; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlPersisterUtilitiesBase class.
        /// </summary>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        public SqlPersisterUtilitiesBase
        (
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier
        )
        {
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
        }

        /// <summary>
        /// Loads the access manager from a SQL database.
        /// </summary>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <returns>Values representing the state of the access manager loaded.  The returned tuple contains 2 values: The id of the most recent event persisted into the access manager at the returned state, and the UTC timestamp the event occurred at.</returns>
        /// <exception cref="PersistentStorageEmptyException">The SQL database did not contain any existing events nor data.</exception>
        public Tuple<Guid, DateTime> Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return Load(DateTime.UtcNow, accessManagerToLoadTo, new PersistentStorageEmptyException("The database does not contain any existing events nor data."));
        }

        /// <summary>
        /// Loads the access manager with state corresponding to the specified event id from a SQL database.
        /// </summary>
        /// <param name="eventId">The id of the most recent event persisted into the access manager, at the desired state to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <returns>Values representing the state of the access manager loaded.  The returned tuple contains 2 values: The id of the most recent event persisted into the access manager at the returned state, and the UTC timestamp the event occurred at.</returns>
        /// <remarks>
        ///   <para>Any existing items and mappings stored in parameter 'accessManagerToLoadTo' will be cleared.</para>
        ///   <para>The AccessManager instance is passed as a parameter rather than returned from the method, to allow loading into types derived from AccessManager aswell as AccessManager itself.</para>
        /// </remarks>
        public Tuple<Guid, DateTime> Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            // Get the transaction time corresponding to specified event id
            String query = ReadQueryGenerator.GenerateGetTransactionTimeOfEventQuery(eventId);

            IEnumerable<String> queryResults = ExecuteMultiResultQueryAndHandleException
            (
                query,
                "TransactionTime",
                (String cellValue) => { return cellValue; }
            );
            DateTime stateTime = DateTime.MinValue;
            foreach (String currentResult in queryResults)
            {
                if (stateTime == DateTime.MinValue)
                {
                    stateTime = DateTime.ParseExact(currentResult, TimestampColumnFormatString, DateTimeFormatInfo.InvariantInfo);
                    stateTime = DateTime.SpecifyKind(stateTime, DateTimeKind.Utc);
                }
                else
                {
                    throw new Exception($"Multiple EventIdToTransactionTimeMap rows were returned with EventId '{eventId.ToString()}'.");
                }
            }
            if (stateTime == DateTime.MinValue)
            {
                throw new ArgumentException($"No EventIdToTransactionTimeMap rows were returned for EventId '{eventId.ToString()}'.", nameof(eventId));
            }

            LoadToAccessManager(stateTime, accessManagerToLoadTo);

            return new Tuple<Guid, DateTime>(eventId, stateTime);
        }

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp from a SQL database.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <returns>Values representing the state of the access manager loaded.  The returned tuple contains 2 values: The id of the most recent event persisted into the access manager at the returned state, and the UTC timestamp the event occurred at.</returns>
        /// <remarks>
        ///   <para>Any existing items and mappings stored in parameter 'accessManagerToLoadTo' will be cleared.</para>
        ///   <para>The AccessManager instance is passed as a parameter rather than returned from the method, to allow loading into types derived from AccessManager aswell as AccessManager itself.</para>
        /// </remarks>
        public Tuple<Guid, DateTime> Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return Load(stateTime, accessManagerToLoadTo, new ArgumentException($"No EventIdToTransactionTimeMap rows were returned with TransactionTime less than or equal to '{stateTime.ToString(TimestampColumnFormatString)}'.", nameof(stateTime)));
        }

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp from persistent storage.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <param name="eventIdToTransactionTimeMapRowDoesntExistException">An exception to throw if no rows exist in the 'EventIdToTransactionTimeMap' table equal to or sequentially before the specified state time.</param>
        /// <returns>Values representing the state of the access manager loaded.  The returned tuple contains 2 values: The id of the most recent event persisted into the access manager at the returned state, and the UTC timestamp the event occurred at.</returns>
        public Tuple<Guid, DateTime> Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo, Exception eventIdToTransactionTimeMapRowDoesntExistException)
        {
            if (stateTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' must be expressed as UTC.", nameof(stateTime));
            DateTime now = DateTime.UtcNow;
            if (stateTime > now)
                throw new ArgumentException($"Parameter '{nameof(stateTime)}' will value '{stateTime.ToString(TimestampColumnFormatString)}' is greater than the current time '{now.ToString(TimestampColumnFormatString)}'.", nameof(stateTime));

            // Get the event id and transaction time equal to or immediately before the specified state time
            String query = ReadQueryGenerator.GenerateGetEventCorrespondingToStateTimeQuery(stateTime);

            IEnumerable<Tuple<Guid, DateTime>> queryResults = ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EventId",
                "TransactionTime",
                (String cellValue) => { return Guid.Parse(cellValue); },
                (String cellValue) =>
                {
                    var stateTime = DateTime.ParseExact(cellValue, TimestampColumnFormatString, DateTimeFormatInfo.InvariantInfo);
                    stateTime = DateTime.SpecifyKind(stateTime, DateTimeKind.Utc);

                    return stateTime;
                }
            );
            Guid eventId = default(Guid);
            DateTime transactionTime = DateTime.MinValue;
            foreach (Tuple<Guid, DateTime> currentResult in queryResults)
            {
                eventId = currentResult.Item1;
                transactionTime = currentResult.Item2;
                break;
            }
            if (transactionTime == DateTime.MinValue)
                throw eventIdToTransactionTimeMapRowDoesntExistException;

            LoadToAccessManager(transactionTime, accessManagerToLoadTo);

            return new Tuple<Guid, DateTime>(eventId, transactionTime);
        }

        /// <summary>
        /// Attempts to execute the specified query which is expected to return multiple rows, handling any resulting exception.
        /// </summary>
        /// <typeparam name="TReturn">The type of data returned from the query.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert">The name of the column in the results to convert to the specified type.</param>
        /// <param name="conversionFromStringFunction">A function which converts a single string-valued cell in the results to the specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        public IEnumerable<TReturn> ExecuteMultiResultQueryAndHandleException<TReturn>(String query, String columnToConvert, Func<String, TReturn> conversionFromStringFunction)
        {
            try
            {
                return ExecuteQueryAndConvertColumn(query, columnToConvert, conversionFromStringFunction);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute query '{query}' in {DatabaseName}.", e);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query which is expected to return multiple rows, handling any resulting exception.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item returned from the query.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item returned from the query.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <returns>A collection of tuples of the items returned by the query.</returns>
        public IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteMultiResultQueryAndHandleException<TReturn1, TReturn2>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction
        )
        {
            try
            {
                return ExecuteQueryAndConvertColumn(query, columnToConvert1, columnToConvert2, returnType1ConversionFromStringFunction, returnType2ConversionFromStringFunction);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute query '{query}' in {DatabaseName}.", e);
            }
        }

        /// <summary>
        /// Attempts to execute the specified query which is expected to return multiple rows, handling any resulting exception.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item returned from the query.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item returned from the query.</typeparam>
        /// <typeparam name="TReturn3">The type of the third data item returned from the query.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert3">The name of the third column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <param name="returnType3ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the third specified return type.</param>
        /// <returns>A collection of tuples of the items returned by the query.</returns>
        public IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteMultiResultQueryAndHandleException<TReturn1, TReturn2, TReturn3>
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
            try
            {
                return ExecuteQueryAndConvertColumn
                (
                    query,
                    columnToConvert1,
                    columnToConvert2,
                    columnToConvert3,
                    returnType1ConversionFromStringFunction,
                    returnType2ConversionFromStringFunction,
                    returnType3ConversionFromStringFunction
                );
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to execute query '{query}' in {DatabaseName}.", e);
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns all users in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all users in the database valid at the specified time.</returns>
        protected IEnumerable<TUser> GetUsers(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetUsersQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                (String cellValue) => { return userStringifier.FromString(cellValue); }
            );
        }

        /// <summary>
        /// Returns all groups in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all groups in the database valid at the specified time.</returns>
        protected IEnumerable<TGroup> GetGroups(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetGroupsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "Group",
                (String cellValue) => { return groupStringifier.FromString(cellValue); }
            );
        }

        /// <summary>
        /// Returns all user to group mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all user to group mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TUser, TGroup>> GetUserToGroupMappings(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetUserToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                "Group",
                (String cell1Value) => { return userStringifier.FromString(cell1Value); },
                (String cell2Value) => { return groupStringifier.FromString(cell2Value); }
            );
        }

        /// <summary>
        /// Returns all group to group mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all group to group mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TGroup, TGroup>> GetGroupToGroupMappings(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetGroupToGroupMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "FromGroup",
                "ToGroup",
                (String cell1Value) => { return groupStringifier.FromString(cell1Value); },
                (String cell2Value) => { return groupStringifier.FromString(cell2Value); }
            );
        }

        /// <summary>
        /// Returns all user to application component and access level mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all user to application component and access level mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TUser, TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetUserToApplicationComponentAndAccessLevelMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                "ApplicationComponent",
                "AccessLevel",
                (String cell1Value) => { return userStringifier.FromString(cell1Value); },
                (String cell2Value) => { return applicationComponentStringifier.FromString(cell2Value); },
                (String cell3Value) => { return accessLevelStringifier.FromString(cell3Value); }
            );
        }

        /// <summary>
        /// Returns all group to application component and access level mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all group to application component and access level mappings in the database valid at the specified state time.</returns>
        protected IEnumerable<Tuple<TGroup, TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetGroupToApplicationComponentAndAccessLevelMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "Group",
                "ApplicationComponent",
                "AccessLevel",
                (String cell1Value) => { return groupStringifier.FromString(cell1Value); },
                (String cell2Value) => { return applicationComponentStringifier.FromString(cell2Value); },
                (String cell3Value) => { return accessLevelStringifier.FromString(cell3Value); }
            );
        }

        /// <summary>
        /// Returns all entity types in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all entity types in the database valid at the specified time.</returns>
        protected IEnumerable<String> GetEntityTypes(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetEntityTypesQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EntityType",
                (String cellValue) => { return cellValue; }
            );
        }

        /// <summary>
        /// Returns all entities in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all entities in the database valid at the specified state time. Each tuple contains: the type of the entity, and the entity itself.</returns>
        protected IEnumerable<Tuple<String, String>> GetEntities(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetEntitiesQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "EntityType",
                "Entity",
                (String cell1Value) => { return cell1Value; },
                (String cell2Value) => { return cell2Value; }
            );
        }

        /// <summary>
        /// Returns all user to entity mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all user to entity mappings in the database valid at the specified state time.  Each tuple contains: the user, the type of the entity, and the entity.</returns>
        protected IEnumerable<Tuple<TUser, String, String>> GetUserToEntityMappings(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetUserToEntityMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "User",
                "EntityType",
                "Entity",
                (String cell1Value) => { return userStringifier.FromString(cell1Value); },
                (String cell2Value) => { return cell2Value; },
                (String cell3Value) => { return cell3Value; }
            );
        }

        /// <summary>
        /// Returns all group to entity mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>A collection of all group to entity mappings in the database valid at the specified state time.  Each tuple contains: the group, the type of the entity, and the entity.</returns>
        protected IEnumerable<Tuple<TGroup, String, String>> GetGroupToEntityMappings(DateTime stateTime)
        {
            String query = ReadQueryGenerator.GenerateGetGroupToEntityMappingsQuery(stateTime);

            return ExecuteMultiResultQueryAndHandleException
            (
                query,
                "Group",
                "EntityType",
                "Entity",
                (String cell1Value) => { return groupStringifier.FromString(cell1Value); },
                (String cell2Value) => { return cell2Value; },
                (String cell3Value) => { return cell3Value; }
            );
        }

        /// <summary>
        /// Attempts to execute the specified query, converting a specified column from each row of the results to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert">The name of the column in the results to convert to the specified type.</param>
        /// <param name="conversionFromStringFunction">A function which converts a single string-valued cell in the results to the specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected abstract IEnumerable<T> ExecuteQueryAndConvertColumn<T>(String query, String columnToConvert, Func<String, T> conversionFromStringFunction);

        /// <summary>
        /// Attempts to execute the specified query, converting the specified columns from each row of the results to the specified types.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected abstract IEnumerable<Tuple<TReturn1, TReturn2>> ExecuteQueryAndConvertColumn<TReturn1, TReturn2>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction
        );

        /// <summary>
        /// Attempts to execute the specified query, converting the specified columns from each row of the results to the specified types.
        /// </summary>
        /// <typeparam name="TReturn1">The type of the first data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn2">The type of the second data item to convert to and return.</typeparam>
        /// <typeparam name="TReturn3">The type of the third data item to convert to and return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="columnToConvert1">The name of the first column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert2">The name of the second column in the results to convert to the specified type.</param>
        /// <param name="columnToConvert3">The name of the third column in the results to convert to the specified type.</param>
        /// <param name="returnType1ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the first specified return type.</param>
        /// <param name="returnType2ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the second specified return type.</param>
        /// <param name="returnType3ConversionFromStringFunction">A function which converts a single string-valued cell in the results to the third specified return type.</param>
        /// <returns>A collection of items returned by the query.</returns>
        protected abstract IEnumerable<Tuple<TReturn1, TReturn2, TReturn3>> ExecuteQueryAndConvertColumn<TReturn1, TReturn2, TReturn3>
        (
            String query,
            String columnToConvert1,
            String columnToConvert2,
            String columnToConvert3,
            Func<String, TReturn1> returnType1ConversionFromStringFunction,
            Func<String, TReturn2> returnType2ConversionFromStringFunction,
            Func<String, TReturn3> returnType3ConversionFromStringFunction
        );

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp from persistent storage into the specified AccessManager instance.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        protected void LoadToAccessManager(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            accessManagerToLoadTo.Clear();
            foreach (TUser currentUser in GetUsers(stateTime))
            {
                accessManagerToLoadTo.AddUser(currentUser);
            }
            foreach (TGroup currentGroup in GetGroups(stateTime))
            {
                accessManagerToLoadTo.AddGroup(currentGroup);
            }
            foreach (Tuple<TUser, TGroup> currentUserToGroupMapping in GetUserToGroupMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToGroupMapping(currentUserToGroupMapping.Item1, currentUserToGroupMapping.Item2);
            }
            foreach (Tuple<TGroup, TGroup> currentGroupToGroupMapping in GetGroupToGroupMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToGroupMapping(currentGroupToGroupMapping.Item1, currentGroupToGroupMapping.Item2);
            }
            foreach (Tuple<TUser, TComponent, TAccess> currentUserToApplicationComponentAndAccessLevelMapping in GetUserToApplicationComponentAndAccessLevelMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToApplicationComponentAndAccessLevelMapping
                (
                    currentUserToApplicationComponentAndAccessLevelMapping.Item1,
                    currentUserToApplicationComponentAndAccessLevelMapping.Item2,
                    currentUserToApplicationComponentAndAccessLevelMapping.Item3
                );
            }
            foreach (Tuple<TGroup, TComponent, TAccess> currentGroupToApplicationComponentAndAccessLevelMapping in GetGroupToApplicationComponentAndAccessLevelMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToApplicationComponentAndAccessLevelMapping
                (
                    currentGroupToApplicationComponentAndAccessLevelMapping.Item1,
                    currentGroupToApplicationComponentAndAccessLevelMapping.Item2,
                    currentGroupToApplicationComponentAndAccessLevelMapping.Item3
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
            foreach (Tuple<TUser, String, String> currentUserToEntityMapping in GetUserToEntityMappings(stateTime))
            {
                accessManagerToLoadTo.AddUserToEntityMapping
                (
                    currentUserToEntityMapping.Item1,
                    currentUserToEntityMapping.Item2,
                    currentUserToEntityMapping.Item3
                );
            }
            foreach (Tuple<TGroup, String, String> currentGroupToEntityMapping in GetGroupToEntityMappings(stateTime))
            {
                accessManagerToLoadTo.AddGroupToEntityMapping
                (
                    currentGroupToEntityMapping.Item1,
                    currentGroupToEntityMapping.Item2,
                    currentGroupToEntityMapping.Item3
                );
            }
        }

        #endregion
    }
}
