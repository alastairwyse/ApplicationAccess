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
using ApplicationMetrics.MetricLoggers;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Models.Options.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.OpenTelemetryConnectionOptions class.
    /// </summary>
    public class OpenTelemetryConnectionOptionsTests
    {
        protected OpenTelemetryConnectionOptions testOpenTelemetryConnectionOptions;

        [SetUp]
        protected void SetUp()
        {
            testOpenTelemetryConnectionOptions = new OpenTelemetryConnectionOptions
            {
                Protocol = OpenTelemetryConnectionProtocol.HttpProtobuf,
                Endpoint = "http://127.0.0.1:4318/v1/metrics"
            };
        }

        [Test]
        public void Constructor()
        {
            Assert.AreEqual(10_000, testOpenTelemetryConnectionOptions.Timeout);
            Assert.AreEqual("", testOpenTelemetryConnectionOptions.Headers);
            Assert.AreEqual(30_000, testOpenTelemetryConnectionOptions.ExporterTimeout);
            Assert.AreEqual(512, testOpenTelemetryConnectionOptions.MaxExportBatchSize);
            Assert.AreEqual(2_048, testOpenTelemetryConnectionOptions.MaxQueueSize);
            Assert.AreEqual(5_000, testOpenTelemetryConnectionOptions.ScheduledDelay);
        }

        [Test]
        public void Validate_ProtocolNull()
        {
            testOpenTelemetryConnectionOptions.Protocol = null;
            var validationContext = new ValidationContext(testOpenTelemetryConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testOpenTelemetryConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating OpenTelemetryConnection options.  Configuration for 'Protocol' is required."));
        }

        [Test]
        public void Validate_EndpointNull()
        {
            testOpenTelemetryConnectionOptions.Endpoint = null;
            var validationContext = new ValidationContext(testOpenTelemetryConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testOpenTelemetryConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating OpenTelemetryConnection options.  Configuration for 'Endpoint' is required."));
        }
    }
}
