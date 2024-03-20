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

namespace ApplicationAccess.Persistence.Sql.SqlServer
{
    /// <summary>
    /// Generates queries used to read the current state of an AccessManager class from a SQL Server database.
    /// </summary>
    public class SqlServerReadQueryGenerator : ReadQueryGeneratorBase
    {
        /// <summary>DateTime format string which matches the <see href="https://docs.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver16#date-and-time-styles">Transact-SQL 126 date and time style</see>.</summary>
        protected const String transactionSql126DateStyle = "yyyy-MM-ddTHH:mm:ss.fffffff";

        /// <inheritdoc/>
        protected override String ReservedKeywordStartDelimiter
        {
            get { return "["; }
        }

        /// <inheritdoc/>
        protected override String ReservedKeywordEndDelimiter
        {
            get { return "]"; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Sql.SqlServer.SqlServerReadQueryGenerator class.
        /// </summary>
        public SqlServerReadQueryGenerator()
            : base()
        {
        }

        /// <inheritdoc/>
        public override String GenerateGetTransactionTimeOfEventQuery(Guid eventId)
        {
            String query =
            @$" 
            SELECT  CONVERT(nvarchar(30), TransactionTime , 126) AS 'TransactionTime'
            FROM    EventIdToTransactionTimeMap
            WHERE   EventId = '{eventId.ToString()}';";

            return query;
        }

        /// <inheritdoc/>
        public override String GenerateGetEventCorrespondingToStateTimeQuery(DateTime stateTime)
        {
            String query =
            @$" 
            SELECT  TOP(1)
                    CONVERT(nvarchar(40), EventId) AS 'EventId',
                    CONVERT(nvarchar(30), TransactionTime , 126) AS 'TransactionTime'
            FROM    EventIdToTransactionTimeMap
            WHERE   TransactionTime <= CONVERT(datetime2, '{stateTime.ToString(transactionSql126DateStyle)}', 126)
            ORDER   BY TransactionTime DESC;";

            return query;
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override String ConvertDateTimeToString(DateTime inputDateTime)
        {
            return $"CONVERT(datetime2, '{inputDateTime.ToString(transactionSql126DateStyle)}', 126)";
        }

        #endregion
    }
}
