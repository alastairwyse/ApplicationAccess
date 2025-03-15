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
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Metrics;
using Microsoft.Extensions.Configuration;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.OpenTelemetry;
using ApplicationMetrics.MetricLoggers.SqlServer;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Metrics.MetricLoggerFactory class.
    /// </summary>
    public class MetricLoggerFactoryTests
    {
        protected Func<IMetricLoggingComponent> testMetricLoggingComponentRetrievalFunction;
        protected TripSwitchActuator testTripSwitchActuator;
        protected IApplicationLogger mockMetricBufferProcessorLogger;
        protected IApplicationLogger mockMetricLoggerLogger;
        protected MetricLoggerFactory testMetricLoggerFactory;

        [SetUp]
        protected void SetUp()
        {
            testMetricLoggingComponentRetrievalFunction = () => { return null; };
            testTripSwitchActuator = new TripSwitchActuator();
            mockMetricBufferProcessorLogger = Substitute.For<IApplicationLogger>();
            mockMetricLoggerLogger = Substitute.For<IApplicationLogger>();
            testMetricLoggerFactory = new MetricLoggerFactory();
        }

        [TearDown]
        protected void TearDown()
        {
            testTripSwitchActuator.Dispose();
        }

        [Test]
        public void CreateMetricLoggerAndBufferProcessor_CategoryNamePrefixNull()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerFactory.CreateMetricLoggerAndBufferProcessor
                (
                    new MetricLoggingOptions(), 
                    null, 
                    testMetricLoggingComponentRetrievalFunction, 
                    testTripSwitchActuator, 
                    mockMetricBufferProcessorLogger, 
                    mockMetricLoggerLogger, 
                    IntervalMetricBaseTimeUnit.Nanosecond, 
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'categoryNamePrefix' must contain a value."));
        }

        [Test]
        public void CreateMetricLoggerAndBufferProcessor_CategoryNamePrefixWhitespace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerFactory.CreateMetricLoggerAndBufferProcessor
                (
                    new MetricLoggingOptions(),
                    " ",
                    testMetricLoggingComponentRetrievalFunction,
                    testTripSwitchActuator,
                    mockMetricBufferProcessorLogger,
                    mockMetricLoggerLogger,
                    IntervalMetricBaseTimeUnit.Nanosecond,
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'categoryNamePrefix' must contain a value."));
        }

        [Test]
        public void CreateMetricLoggerAndBufferProcessor_OpenTelemetryConnectionEndpointContainsInvalidUrl()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions
            {
                MetricLoggingEnabled = true, 
                OpenTelemetryConnection = new OpenTelemetryConnectionOptions()
                {
                    Protocol = OpenTelemetryConnectionProtocol.HttpProtobuf, 
                    Endpoint = "http//127.0.0.1:4318/v1/metrics"
                }
            };

            var e = Assert.Throws<Exception>(delegate
            {
                testMetricLoggerFactory.CreateMetricLoggerAndBufferProcessor
                (
                    testMetricLoggingOptions,
                    "ApplicationAccessReaderWriterNode",
                    testMetricLoggingComponentRetrievalFunction,
                    testTripSwitchActuator,
                    mockMetricBufferProcessorLogger,
                    mockMetricLoggerLogger,
                    ApplicationMetrics.MetricLoggers.IntervalMetricBaseTimeUnit.Nanosecond,
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to create a Uri from OpenTelemetry connection endpoint 'http//127.0.0.1:4318/v1/metrics'."));
        }

        [Test]
        public void CreateMetricLoggerAndBufferProcessor_OpenTelemetryMetricLogger()
        {
            var testMetricLoggingOptions = new MetricLoggingOptions
            {
                MetricLoggingEnabled = true,
                OpenTelemetryConnection = new OpenTelemetryConnectionOptions()
                {
                    Protocol = OpenTelemetryConnectionProtocol.HttpProtobuf,
                    Endpoint = "http://127.0.0.1:4318/v1/metrics"
                }
            };

            (IMetricLogger returnMetricLogger, WorkerThreadBufferProcessorBase returnBufferProcessor) = testMetricLoggerFactory.CreateMetricLoggerAndBufferProcessor
            (
                testMetricLoggingOptions,
                "ApplicationAccessReaderWriterNode",
                testMetricLoggingComponentRetrievalFunction,
                testTripSwitchActuator,
                mockMetricBufferProcessorLogger,
                mockMetricLoggerLogger,
                IntervalMetricBaseTimeUnit.Nanosecond,
                true
            );

            Assert.IsInstanceOf<OpenTelemetryMetricLogger>(returnMetricLogger);
            Assert.IsNull(returnBufferProcessor);
            ((OpenTelemetryMetricLogger)returnMetricLogger).Dispose();
        }

        // TODO: Add test for creating a SQL metric logger
        //   Need to find a way to instantiate SqlDatabaseConnectionOptions.ConnectionParameters
    }
}
