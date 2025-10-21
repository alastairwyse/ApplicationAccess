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
using System.ComponentModel.DataAnnotations;
using ApplicationAccess.Hosting.Models.Options;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Validates a <see cref="DatabaseConnectionOptions"/> instance.
    /// </summary>
    public class DatabaseConnectionOptionsValidator : OptionsValidatorBase
    {
        /// <inheritdoc/>
        protected override String OptionsName
        {
            get { return DatabaseConnectionOptions.DatabaseConnectionOptionsName; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DatabaseConnectionOptionsValidator class.
        /// </summary>
        public DatabaseConnectionOptionsValidator()
            : base()
        {
        }

        /// <summary>
        /// Validates a <see cref="DatabaseConnectionOptions"/> instance.
        /// </summary>
        /// <param name="databaseConnectionOptions"></param>
        public void Validate(DatabaseConnectionOptions databaseConnectionOptions)
        {
            if (databaseConnectionOptions.SqlDatabaseConnection != null && databaseConnectionOptions.MongoDbDatabaseConnection != null)
            {
                // Both 'Connection' configuration settings are set (can only have one)
                throw new ValidationException($"{GenerateExceptionMessagePrefix()}  Configuration for either section '{SqlDatabaseConnectionOptions.SqlDatabaseConnectionOptionsName}' or section '{MongoDbDatabaseConnectionOptions.MongoDbDatabaseConnectionOptionsName}' must be provided, but not both.");
            }
            else if (databaseConnectionOptions.SqlDatabaseConnection != null)
            {
                var validationContext = new ValidationContext(databaseConnectionOptions.SqlDatabaseConnection);
                try
                {
                    Validator.ValidateObject(databaseConnectionOptions.SqlDatabaseConnection, validationContext, true);
                }
                catch (Exception e)
                {
                    throw new ValidationException(GenerateExceptionMessagePrefix(), e);
                }
            }
            else if (databaseConnectionOptions.MongoDbDatabaseConnection != null)
            {
                var validationContext = new ValidationContext(databaseConnectionOptions.MongoDbDatabaseConnection);
                try
                {
                    Validator.ValidateObject(databaseConnectionOptions.MongoDbDatabaseConnection, validationContext, true);
                }
                catch (Exception e)
                {
                    throw new ValidationException(GenerateExceptionMessagePrefix(), e);
                }
            }
        }
    }
}
