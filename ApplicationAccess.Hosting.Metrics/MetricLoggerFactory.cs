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
using System.Diagnostics;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Metrics;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.OpenTelemetry;
using ApplicationLogging;
using OpenTelemetry.Exporter;

namespace ApplicationAccess.Hosting.Metrics
{
    /// <summary>
    /// Factory for creating instances of <see cref="IMetricLogger"/> and <see cref="WorkerThreadBufferProcessorBase"/> from <see cref="MetricLoggingOptions"/> configuration.
    /// </summary>
    public class MetricLoggerFactory
    {
        /// <summary>
        /// Creates a metric logger and optional metric buffer processor.
        /// </summary>
        /// <param name="metricLoggingOptions">The configuration to use to create the instances.</param>
        /// <param name="categoryNamePrefix">The prefix for the category to log metrics under.</param>
        /// <param name="processingFailureAction">The action to take if a critical/non-recoverable error occurs whilst attempting to process the buffer(s).</param>
        /// <param name="metricLoggingComponentRetrievalFunction">A func which returns the hosting component which logs the metrics.</param>
        /// <param name="tripSwitchActuator">A <see cref="TripSwitchActuator"/> for the hosting component.</param>
        /// <param name="metricBufferProcessorLogger">The logger to set on the returned metric buffer processor.</param>
        /// <param name="metricLoggerLogger">The logger to set on the returned metric logger.</param>
        /// <param name="intervalMetricBaseTimeUnit">The base time unit to use to log interval metrics.</param>
        /// <param name="intervalMetricChecking">Specifies whether an exception should be thrown if the correct order of interval metric logging is not followed (e.g. End() method called before Begin()).  Note that this parameter only has an effect when running in 'non-interleaved' mode.</param>
        /// <returns>A tuple containing: a metric logger, and a metric buffer processor (or null if the returned, metric logger does not require a buffer processor).</returns>
        public (IMetricLogger, WorkerThreadBufferProcessorBase) CreateMetricLoggerAndBufferProcessor
        (
            MetricLoggingOptions metricLoggingOptions,
            String categoryNamePrefix, 
            Func<IMetricLoggingComponent> metricLoggingComponentRetrievalFunction,
            TripSwitchActuator tripSwitchActuator,
            IApplicationLogger metricBufferProcessorLogger,
            IApplicationLogger metricLoggerLogger,
            IntervalMetricBaseTimeUnit intervalMetricBaseTimeUnit,
            Boolean intervalMetricChecking
        )
        {
            if (String.IsNullOrWhiteSpace(categoryNamePrefix) == true)
                throw new ArgumentException($"Parameter '{nameof(categoryNamePrefix)}' must contain a value.");

            String categoryName = categoryNamePrefix;
            if (metricLoggingOptions.MetricCategorySuffix != "")
            {
                categoryName = $"{categoryName}-{metricLoggingOptions.MetricCategorySuffix}";
            }

            if (metricLoggingOptions.OpenTelemetryConnection != null)
            {
                OtlpExportProtocol protocol;
                if (metricLoggingOptions.OpenTelemetryConnection.Protocol.Value == OpenTelemetryConnectionProtocol.HttpProtobuf)
                {
                    protocol = OtlpExportProtocol.HttpProtobuf;
                }
                else if (metricLoggingOptions.OpenTelemetryConnection.Protocol.Value == OpenTelemetryConnectionProtocol.Grpc)
                {
                    protocol = OtlpExportProtocol.Grpc;
                }
                else
                {
                    throw new Exception($"Encountered unsupported {nameof(OpenTelemetryConnectionProtocol)} value '{metricLoggingOptions.OpenTelemetryConnection.Protocol.Value}'.");
                }
                Boolean result = Uri.TryCreate(metricLoggingOptions.OpenTelemetryConnection.Endpoint, new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = false }, out Uri endpoint);
                if (result == false)
                {
                    throw new Exception($"Failed to create a {nameof(Uri)} from OpenTelemetry connection endpoint '{metricLoggingOptions.OpenTelemetryConnection.Endpoint}'.");
                }

                Action<OtlpExporterOptions> otlpExporterConfigurationAction = (OtlpExporterOptions otlpExporterOptions) =>
                {
                    otlpExporterOptions.Protocol = protocol;
                    otlpExporterOptions.Endpoint = endpoint;
                    otlpExporterOptions.Headers = metricLoggingOptions.OpenTelemetryConnection.Headers;
                    otlpExporterOptions.TimeoutMilliseconds = metricLoggingOptions.OpenTelemetryConnection.Timeout;
                    otlpExporterOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
                    otlpExporterOptions.BatchExportProcessorOptions = new OpenTelemetry.BatchExportProcessorOptions<Activity>
                    {
                        ExporterTimeoutMilliseconds = metricLoggingOptions.OpenTelemetryConnection.ExporterTimeout,
                        MaxExportBatchSize = metricLoggingOptions.OpenTelemetryConnection.MaxExportBatchSize, 
                        MaxQueueSize = metricLoggingOptions.OpenTelemetryConnection.MaxQueueSize, 
                        ScheduledDelayMilliseconds = metricLoggingOptions.OpenTelemetryConnection.ScheduledDelay
                    };
                };
                var metricLogger = new OpenTelemetryMetricLogger(intervalMetricBaseTimeUnit, intervalMetricChecking, categoryName, otlpExporterConfigurationAction);

                return (metricLogger, null);
            }
            else
            {
                var metricsBufferProcessorFactory = new MetricsBufferProcessorFactory();
                if (metricLoggingOptions.MetricBufferProcessing.BufferProcessingFailureAction == MetricBufferProcessingFailureAction.ReturnServiceUnavailable)
                {
                    // Parameter 'metricLoggingComponentRetrievalFunction' is not used when 'processingFailureAction' is set to 'ReturnServiceUnavailable', hence can return null
                    metricLoggingComponentRetrievalFunction = () => { return null; };
                }
                Action<Exception> bufferProcessingExceptionAction = metricsBufferProcessorFactory.GetBufferProcessingExceptionAction
                (
                    metricLoggingOptions.MetricBufferProcessing.BufferProcessingFailureAction,
                    metricLoggingComponentRetrievalFunction,
                    tripSwitchActuator,
                    metricBufferProcessorLogger
                );
                WorkerThreadBufferProcessorBase metricLoggerBufferProcessingStrategy = metricsBufferProcessorFactory.GetBufferProcessor(metricLoggingOptions.MetricBufferProcessing, bufferProcessingExceptionAction, false);
                var metricLoggerFactory = new SqlMetricLoggerFactory
                (
                    categoryName,
                    metricLoggerBufferProcessingStrategy,
                    IntervalMetricBaseTimeUnit.Nanosecond, 
                    true,
                    metricLoggerLogger
                );
                var databaseConnectionParametersParser = new SqlDatabaseConnectionParametersParser();
                SqlDatabaseConnectionParametersBase metricsDatabaseConnectionParameters = databaseConnectionParametersParser.Parse
                (
                    metricLoggingOptions.MetricsSqlDatabaseConnection.DatabaseType.Value,
                    metricLoggingOptions.MetricsSqlDatabaseConnection.ConnectionParameters,
                    MetricsSqlDatabaseConnectionOptions.MetricsSqlDatabaseConnectionOptionsName
                );
                IMetricLogger metricLogger = metricLoggerFactory.GetMetricLogger(metricsDatabaseConnectionParameters);

                return (metricLogger, metricLoggerBufferProcessingStrategy);
            }
        }
    }
}
