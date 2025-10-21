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
using Microsoft.Extensions.Configuration;
using ApplicationAccess.Hosting.Models;

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Parses a <see cref="DatabaseConnectionOptions"/> instance, converting it to an instance of a subclass of <see cref="DatabaseConnectionParameters"/>.
    /// </summary>
    public class DatabaseConnectionOptionsParser
    {
        /// <summary>
        /// Parses a <see cref="DatabaseConnectionOptions"/> instance, converting it to an instance of <see cref="DatabaseConnectionParameters"/>.
        /// </summary>
        /// <param name="databaseConnectionOptions">The <see cref="DatabaseConnectionOptions"/> instance to parse.</param>
        /// <returns>The <see cref="DatabaseConnectionParameters"/>.</returns>
        public DatabaseConnectionParameters Parse(DatabaseConnectionOptions databaseConnectionOptions)
        {
            if (databaseConnectionOptions.SqlDatabaseConnection != null)
            {
                SqlDatabaseConnectionParametersParser sqlDatabaseConnectionParametersParser = new();
                SqlDatabaseConnectionParametersBase sqlDatabaseConnectionParameters = sqlDatabaseConnectionParametersParser.Parse(
                    databaseConnectionOptions.SqlDatabaseConnection.DatabaseType.Value,
                    databaseConnectionOptions.SqlDatabaseConnection.ConnectionParameters,
                    SqlDatabaseConnectionOptions.SqlDatabaseConnectionOptionsName
                );

                return new DatabaseConnectionParameters(sqlDatabaseConnectionParameters);
            }
            else if (databaseConnectionOptions.MongoDbDatabaseConnection != null)
            {
                var mongoDbDatabaseConnectionParameters = new MongoDbDatabaseConnectionParameters
                (
                    databaseConnectionOptions.MongoDbDatabaseConnection.ConnectionString,
                    databaseConnectionOptions.MongoDbDatabaseConnection.DatabaseName,
                    databaseConnectionOptions.MongoDbDatabaseConnection.UseTransactions.Value
                );

                return new DatabaseConnectionParameters(mongoDbDatabaseConnectionParameters);
            }
            else
            {
                return new DatabaseConnectionParameters();
            }
        }
    }
}
