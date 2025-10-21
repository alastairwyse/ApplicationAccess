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

namespace ApplicationAccess.Hosting.Models
{
    /// <summary>
    /// Container class which holds parameters used to connect to a MongoDB database.
    /// </summary>
    public class MongoDbDatabaseConnectionParameters
    {
        /// <summary>The connection string to use to connect to MongoDB.</summary>
        protected String connectionString;
        /// <summary>The name of the database holding the AccessManager data.</summary>
        protected String databaseName;
        /// <summary>Whether to perform write/persistence operations using transactions.</summary>
        protected Boolean useTransactions;

        /// <summary>
        /// The connection string to use to connect to MongoDB.
        /// </summary>
        public String ConnectionString
        {
            get { return connectionString; }
        }

        /// <summary>
        /// The name of the database holding the AccessManager data.
        /// </summary>
        public String DatabaseName
        {
            get { return databaseName; }
        }

        /// <summary>
        /// Whether to perform write/persistence operations using transactions.
        /// </summary>
        public Boolean UseTransactions
        {
            get { return useTransactions; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Models.MongoDbDatabaseConnectionParameters class.
        /// </summary>
        /// <param name="connectionString">The connection string to use to connect to MongoDB.</param>
        /// <param name="databaseName">The name of the database holding the AccessManager data.</param>
        /// <param name="useTransactions">Whether to perform write/persistence operations using transactions.</param>
        public MongoDbDatabaseConnectionParameters(String connectionString, String databaseName, Boolean useTransactions)
        {
            this.connectionString = connectionString;
            this.databaseName = databaseName;
            this.useTransactions = useTransactions;
        }
    }
}
