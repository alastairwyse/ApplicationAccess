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

namespace ApplicationAccess.Persistence.Sql
{
    /// <summary>
    /// Generates queries used to read the current state of an AccessManager class from a SQL database.
    /// </summary>
    public abstract class ReadQueryGeneratorBase
    {
        /// <summary>Start delimiter for reserved keywords within a SQL query.</summary>
        protected abstract String ReservedKeywordStartDelimiter { get; }

        /// <summary>End delimiter for reserved keywords within a SQL query.</summary>
        protected abstract String ReservedKeywordEndDelimiter { get; }

        /// <summary>Delimiter (start and end) for aliases within a SQL query (i.e. after the 'AS' keyword).</summary>
        protected abstract String AliasDelimiter { get; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.ReadQueryGeneratorBase class.
        /// </summary>
        protected ReadQueryGeneratorBase()
        {
        }

        /// <summary>
        /// Generates a query which returns the transaction (state) time and sequence number of the specified event.
        /// </summary>
        /// <param name="eventId">The unique id of the event.</param>
        /// <returns>The query.</returns>
        /// <remarks>If multiple events occur at the same time as the specified event id, the transaction time and sequence numbers of all those events will be returned.</remarks>
        public abstract String GenerateGetTransactionTimeOfEventQuery(Guid eventId);

        /// <summary>
        /// Generates a query which returns the properties of the event at or immediately before the specified transaction (state) time (and greatest sequence number if multiple states exist at the same time).
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the event retrieve.</param>
        /// <returns>The query.</returns>
        public abstract String GenerateGetEventCorrespondingToStateTimeQuery(DateTime stateTime);

        /// <summary>
        /// Generates a query which returns all users in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetUsersQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  {ReservedKeywordStartDelimiter}User{ReservedKeywordEndDelimiter} 
            FROM    Users 
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN TransactionFrom AND TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all groups in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetGroupsQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  {ReservedKeywordStartDelimiter}Group{ReservedKeywordEndDelimiter} 
            FROM    Groups 
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN TransactionFrom AND TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all user to group mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetUserToGroupMappingsQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  u.{ReservedKeywordStartDelimiter}User{ReservedKeywordEndDelimiter}, 
                    g.{ReservedKeywordStartDelimiter}Group{ReservedKeywordEndDelimiter}
            FROM    UserToGroupMappings ug
                    INNER JOIN Users u
                      ON ug.UserId = u.Id
                    INNER JOIN Groups g
                      ON ug.GroupId = g.Id
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN ug.TransactionFrom AND ug.TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all group to group mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetGroupToGroupMappingsQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  gg.Id, 
                    fg.{ReservedKeywordStartDelimiter}Group{ReservedKeywordEndDelimiter} AS {AliasDelimiter}FromGroup{AliasDelimiter}, 
                    tg.{ReservedKeywordStartDelimiter}Group{ReservedKeywordEndDelimiter} AS {AliasDelimiter}ToGroup{AliasDelimiter}
            FROM    GroupToGroupMappings gg
                    INNER JOIN Groups fg
                      ON gg.FromGroupId = fg.Id
                    INNER JOIN Groups tg
                      ON gg.ToGroupId = tg.Id
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN gg.TransactionFrom AND gg.TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all user to application component and access level mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetUserToApplicationComponentAndAccessLevelMappingsQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  u.{ReservedKeywordStartDelimiter}User{ReservedKeywordEndDelimiter}, 
                    ac.ApplicationComponent, 
                    al.AccessLevel 
            FROM    UserToApplicationComponentAndAccessLevelMappings uaa
                    INNER JOIN Users u
                      ON uaa.UserId = u.Id
                    INNER JOIN ApplicationComponents ac
                      ON uaa.ApplicationComponentId = ac.Id
                    INNER JOIN AccessLevels al
                      ON uaa.AccessLevelId = al.Id
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN uaa.TransactionFrom AND uaa.TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all group to application component and access level mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetGroupToApplicationComponentAndAccessLevelMappingsQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  g.{ReservedKeywordStartDelimiter}Group{ReservedKeywordEndDelimiter}, 
                    ac.ApplicationComponent, 
                    al.AccessLevel 
            FROM    GroupToApplicationComponentAndAccessLevelMappings gaa
                    INNER JOIN Groups g
                      ON gaa.GroupId = g.Id
                    INNER JOIN ApplicationComponents ac
                      ON gaa.ApplicationComponentId = ac.Id
                    INNER JOIN AccessLevels al
                      ON gaa.AccessLevelId = al.Id
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN gaa.TransactionFrom AND gaa.TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all entity types in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetEntityTypesQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  EntityType
            FROM    EntityTypes 
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN TransactionFrom AND TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all entities in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetEntitiesQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  et.EntityType, 
                    e.Entity 
            FROM    Entities e
                    INNER JOIN EntityTypes et
                      ON e.EntityTypeId = et.Id
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN e.TransactionFrom AND e.TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all user to entity mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetUserToEntityMappingsQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  u.{ReservedKeywordStartDelimiter}User{ReservedKeywordEndDelimiter}, 
                    et.EntityType, 
                    e.Entity
            FROM    UserToEntityMappings ue
                    INNER JOIN Users u
                      ON ue.UserId = u.Id
                    INNER JOIN EntityTypes et
                      ON ue.EntityTypeId = et.Id
                    INNER JOIN Entities e
                      ON ue.EntityId = e.Id
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN ue.TransactionFrom AND ue.TransactionTo;";

            return query;
        }

        /// <summary>
        /// Generates a query which returns all group to entity mappings in the database valid at the specified state time.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <returns>The query.</returns>
        public String GenerateGetGroupToEntityMappingsQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  g.{ReservedKeywordStartDelimiter}Group{ReservedKeywordEndDelimiter}, 
                    et.EntityType, 
                    e.Entity
            FROM    GroupToEntityMappings ge
                    INNER JOIN Groups g
                        ON ge.GroupId = g.Id
                    INNER JOIN EntityTypes et
                        ON ge.EntityTypeId = et.Id
                    INNER JOIN Entities e
                        ON ge.EntityId = e.Id
            WHERE   {ConvertDateTimeToString(stateTime)} BETWEEN ge.TransactionFrom AND ge.TransactionTo;";

            return query;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Converts the specified <see cref="DateTime"/> into a string which can be embedded within a SQL query.
        /// </summary>
        /// <param name="inputDateTime">The <see cref="DateTime"/> to convert.</param>
        /// <returns>The <see cref="DateTime"/> as a string.</returns>
        protected abstract String ConvertDateTimeToString(DateTime inputDateTime);

        #endregion
    }
}
