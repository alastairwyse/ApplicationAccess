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
using ApplicationMetrics.MetricLoggers;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.MetricLoggingOptionsValidator class.
    /// </summary>
    public class MetricLoggingOptionsValidatorTests
    {
        private MetricLoggingOptionsValidator testMetricLoggingOptionsValidator;

        [SetUp]
        protected void SetUp()
        {
            testMetricLoggingOptionsValidator = new MetricLoggingOptionsValidator();
        }

        [Test]
        public void Validate_EnabledNull()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions();

            var e = Assert.Throws<ValidationException>(delegate
            {
                testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricLogging options.  Configuration for 'Enabled' is required."));
        }

        [Test]
        public void Validate_NeitherSqlDatabaseConnectionNorOpenTelemetryConnectionDefined()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions() { Enabled = true };

            var e = Assert.Throws<ValidationException>(delegate
            {
                testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricLogging options.  Configuration for either section 'SqlDatabaseConnection' or section 'OpenTelemetryConnection' is required."));
        }

        [Test]
        public void Validate_BothSqlDatabaseConnectionAndOpenTelemetryConnectionDefined()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions() { Enabled = true };
            testMetricLoggingOptions.SqlDatabaseConnection = new SqlDatabaseConnectionOptions();
            testMetricLoggingOptions.OpenTelemetryConnection = new OpenTelemetryConnectionOptions();

            var e = Assert.Throws<ValidationException>(delegate
            {
                testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricLogging options.  Configuration for either section 'SqlDatabaseConnection' or section 'OpenTelemetryConnection' must be provided, but not both."));
        }

        [Test]
        public void Validate_BufferProcessingNull()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions() { Enabled = true };
            testMetricLoggingOptions.SqlDatabaseConnection = new SqlDatabaseConnectionOptions();

            var e = Assert.Throws<ValidationException>(delegate
            {
                testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricLogging options.  Configuration for section 'BufferProcessing' is required."));
        }

        [Test]
        public void Validate_BufferProcessingBufferProcessingStrategyNull()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions() { Enabled = true };
            testMetricLoggingOptions.SqlDatabaseConnection = new SqlDatabaseConnectionOptions();
            testMetricLoggingOptions.BufferProcessing = new MetricBufferProcessingOptions();
            testMetricLoggingOptions.BufferProcessing.BufferSizeLimit = 5;
            testMetricLoggingOptions.BufferProcessing.DequeueOperationLoopInterval = 1000;
            testMetricLoggingOptions.BufferProcessing.BufferProcessingFailureAction = MetricBufferProcessingFailureAction.ReturnServiceUnavailable;

            var e = Assert.Throws<ValidationException>(delegate
            {
                testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricLogging options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating BufferProcessing options.  Configuration for 'BufferProcessingStrategy' is required."));
        }

        [Test]
        public void Validate_SqlDatabaseConnectionDatabaseTypeNull()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions() { Enabled = true };
            testMetricLoggingOptions.SqlDatabaseConnection = new SqlDatabaseConnectionOptions();
            testMetricLoggingOptions.BufferProcessing = new MetricBufferProcessingOptions();
            testMetricLoggingOptions.BufferProcessing.BufferSizeLimit = 5;
            testMetricLoggingOptions.BufferProcessing.DequeueOperationLoopInterval = 1000;
            testMetricLoggingOptions.BufferProcessing.BufferProcessingFailureAction = MetricBufferProcessingFailureAction.ReturnServiceUnavailable;
            testMetricLoggingOptions.BufferProcessing.BufferProcessingStrategy = MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor;

            var e = Assert.Throws<ValidationException>(delegate
            {
                testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricLogging options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating SqlDatabaseConnection options.  Configuration for 'DatabaseType' is required."));
        }

        [Test]
        public void Validate_OpenTelemetryConnectionProtocolNull()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions() { Enabled = true };
            testMetricLoggingOptions.OpenTelemetryConnection = new OpenTelemetryConnectionOptions();

            var e = Assert.Throws<ValidationException>(delegate
            {
                testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricLogging options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating OpenTelemetryConnection options.  Configuration for 'Protocol' is required."));
        }

        [Test]
        public void Validate_MetricLoggingDisabled()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions() { Enabled = false };

            testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);


            testMetricLoggingOptions.SqlDatabaseConnection = new SqlDatabaseConnectionOptions();
            testMetricLoggingOptions.OpenTelemetryConnection = new OpenTelemetryConnectionOptions();

            testMetricLoggingOptionsValidator.Validate(testMetricLoggingOptions);
        }
    }
}
