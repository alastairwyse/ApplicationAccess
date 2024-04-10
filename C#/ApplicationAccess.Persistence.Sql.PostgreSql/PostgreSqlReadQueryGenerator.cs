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
using ApplicationAccess.Persistence.Sql;

namespace ApplicationAccess.Persistence.Sql.PostgreSql
{
    /// <summary>
    /// Generates queries used to read the current state of an AccessManager class from a PostgreSQL database.
    /// </summary>
    public class PostgreSqlReadQueryGenerator : ReadQueryGeneratorBase
    {
        /// <summary>DateTime format string which can be interpreted by the <see href="https://www.postgresql.org/docs/8.1/functions-formatting.html">PostgreSQL to_timestamp() function</see>.</summary>
        protected const String postgreSQLTimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        /// <inheritdoc/>
        protected override String ReservedKeywordStartDelimiter
        {
            get { return "\""; }
        }

        /// <inheritdoc/>
        protected override String ReservedKeywordEndDelimiter
        {
            get { return "\""; }
        }

        /// <inheritdoc/>
        protected override string AliasDelimiter
        {
            get { return "\""; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.PostgreSql.PostgreSqlReadQueryGenerator class.
        /// </summary>
        public PostgreSqlReadQueryGenerator()
            : base()
        {
        }

        /// <inheritdoc/>
        public override String GenerateGetTransactionTimeOfEventQuery(Guid eventId)
        {
            String query =
            @$" 
            SELECT  EventId::varchar AS EventId,
                    TO_CHAR(TransactionTime, 'YYYY-MM-DD HH24:MI:ss.US') AS TransactionTime, 
                    TransactionSequence::varchar AS TransactionSequence 
            FROM    EventIdToTransactionTimeMap 
            WHERE   TransactionTime = 
                    (
                        SELECT  TransactionTime 
                        FROM    EventIdToTransactionTimeMap 
                        WHERE   EventId = '{eventId.ToString()}'
                    );";

            return query;
        }

        /// <inheritdoc/>
        public override String GenerateGetEventCorrespondingToStateTimeQuery(DateTime stateTime)
        {
            // Below can be done more succinctly using a 'LIMIT' clause, however doing this resulted in table scans during testing.
            String query =
            @$" 
            SELECT  EventId::varchar                                                  AS EventId,
                    TO_CHAR(EventTimeMap.TransactionTime, 'YYYY-MM-DD HH24:MI:ss.US') AS TransactionTime, 
                    EventTimeMap.TransactionSequence::varchar                         AS TransactionSequence 
            FROM    EventIdToTransactionTimeMap EventTimeMap
                    INNER JOIN
                    (
                        SELECT  TransactionTime           AS TransactionTime, 
                                MAX(TransactionSequence)  AS TransactionSequence
                        FROM    EventIdToTransactionTimeMap
                        WHERE   TransactionTime = 
                                (
                                    SELECT  MAX(TransactionTime)
                                    FROM    EventIdToTransactionTimeMap
                                    WHERE   TransactionTime <= TO_TIMESTAMP('{stateTime.ToString(postgreSQLTimestampFormat)}', 'YYYY-MM-DD HH24:MI:ss.US')::timestamp
                                )
                        GROUP   BY TransactionTime
                    ) MaxValues
                      ON EventTimeMap.TransactionTime = MaxValues.TransactionTime 
                      AND EventTimeMap.TransactionSequence = MaxValues.TransactionSequence;";

            return query;
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override String ConvertDateTimeToString(DateTime inputDateTime)
        {
            return $"TO_TIMESTAMP('{inputDateTime.ToString(postgreSQLTimestampFormat)}', 'YYYY-MM-DD HH24:MI:ss.US')::timestamp";
        }

        #endregion
    }
}
