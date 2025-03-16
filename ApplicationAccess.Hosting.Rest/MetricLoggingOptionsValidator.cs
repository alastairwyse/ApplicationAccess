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
using Microsoft.AspNetCore.Builder;
using ApplicationAccess.Hosting.Models.Options;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Validates a <see cref="MetricLoggingOptions"/> instance.
    /// </summary>
    public class MetricLoggingOptionsValidator
    {
        /// <summary>Contains utility method for Options classes.</summary>
        protected OptionsUtilities optionsUtilities;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.MetricLoggingOptionsValidator class.
        /// </summary>
        public MetricLoggingOptionsValidator()
        {
            optionsUtilities = new OptionsUtilities();
        }

        /// <summary>
        /// Validates a <see cref="MetricLoggingOptions"/> instance.
        /// </summary>
        /// <param name="metricLoggingOptions">The <see cref="MetricLoggingOptions"/> instance to validate.</param>
        public void Validate(MetricLoggingOptions metricLoggingOptions)
        {
            // Validate data annotations in the top level object
            var validationContext = new ValidationContext(metricLoggingOptions);
            Validator.ValidateObject(metricLoggingOptions, validationContext, true);
            if (metricLoggingOptions.MetricLoggingEnabled == false)
            {
                return;
            }

            if (metricLoggingOptions.MetricsSqlDatabaseConnection == null && metricLoggingOptions.OpenTelemetryConnection == null)
            {
                // Both 'Connection' configuration settings are missing
                throw new ValidationException($"{GenerateExceptionMessagePrefix()}  Configuration for either section '{MetricsSqlDatabaseConnectionOptions.MetricsSqlDatabaseConnectionOptionsName}' or section '{OpenTelemetryConnectionOptions.OpenTelemetryConnectionOptionsName}' is required.");
            }
            else if (metricLoggingOptions.MetricsSqlDatabaseConnection != null && metricLoggingOptions.OpenTelemetryConnection != null)
            {
                // Both 'Connection' configuration settings are set (can only have one)
                throw new ValidationException($"{GenerateExceptionMessagePrefix()}  Configuration for either section '{MetricsSqlDatabaseConnectionOptions.MetricsSqlDatabaseConnectionOptionsName}' or section '{OpenTelemetryConnectionOptions.OpenTelemetryConnectionOptionsName}' must be provided, but not both.");
            }
            else if (metricLoggingOptions.MetricsSqlDatabaseConnection != null)
            {
                // SQL database connection configured
                if (metricLoggingOptions.MetricBufferProcessing == null)
                {
                    throw new ValidationException($"{GenerateExceptionMessagePrefix()}  Configuration for section '{nameof(metricLoggingOptions.MetricBufferProcessing)}' is required.");
                }
                validationContext = new ValidationContext(metricLoggingOptions.MetricBufferProcessing);
                try
                {
                    Validator.ValidateObject(metricLoggingOptions.MetricBufferProcessing, validationContext, true);
                }
                catch (Exception e)
                {
                    throw new ValidationException(GenerateExceptionMessagePrefix(), e);
                }
                validationContext = new ValidationContext(metricLoggingOptions.MetricsSqlDatabaseConnection);
                try
                {
                    Validator.ValidateObject(metricLoggingOptions.MetricsSqlDatabaseConnection, validationContext, true);
                }
                catch (Exception e)
                {
                    throw new ValidationException(GenerateExceptionMessagePrefix(), e);
                }
            }
            else
            {
                validationContext = new ValidationContext(metricLoggingOptions.OpenTelemetryConnection);
                try
                {
                    Validator.ValidateObject(metricLoggingOptions.OpenTelemetryConnection, validationContext, true);
                }
                catch (Exception e)
                {
                    throw new ValidationException(GenerateExceptionMessagePrefix(), e);
                }
            }
        }

        #region Private/Protected Methods

        #pragma warning disable 1591

        protected String GenerateExceptionMessagePrefix()
        {
            return optionsUtilities.GenerateValidationExceptionMessagePrefix(MetricLoggingOptions.MetricLoggingOptionsName);
        }

        #pragma warning restore 1591

        #endregion
    }
}
