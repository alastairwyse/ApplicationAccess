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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Persistence.SqlServer;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.Metrics;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution.Kubernetes;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Persistence.SqlServer;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    /// <summary>
    /// Wraps an instance of <see cref="KubernetesDistributedInstanceManagerNode"/> and initializes it using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    public class KubernetesDistributedInstanceManagerNodeHostedServiceWrapper : NodeHostedServiceWrapperBase, IHostedService
    {
        /// <summary>The category to use for metric logging.</summary>
        protected const String metricLoggerCategoryName = "KubernetesDistributedInstanceManagerNode";

        #pragma warning disable 1591

        // Members passed in via dependency injection
        protected DistributedAccessManagerInstanceOptions distributedAccessManagerInstanceOptions;
        protected ReaderNodeAppSettingsConfigurationTemplate readerNodeAppSettingsConfigurationTemplate;
        protected EventCacheNodeAppSettingsConfigurationTemplate eventCacheNodeAppSettingsConfigurationTemplate;
        protected WriterNodeAppSettingsConfigurationTemplate writerNodeAppSettingsConfigurationTemplate;
        protected DistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate;
        protected DistributedOperationRouterNodeAppSettingsConfigurationTemplate distributedOperationRouterNodeAppSettingsConfigurationTemplate;
        protected MetricLoggingOptions metricLoggingOptions;
        protected KubernetesDistributedInstanceManagerHolder kubernetesDistributedInstanceManagerHolder;
        protected TripSwitchActuator tripSwitchActuator;
        protected ILoggerFactory loggerFactory;

        #pragma warning restore 1591

        /// <summary>Performs the underlying management of the distributed AccessManager instance.</summary>
        protected KubernetesDistributedAccessManagerInstanceManager<SqlServerLoginCredentials> kubernetesDistributedInstanceManager;
        /// <summary>The <see cref="KubernetesDistributedInstanceManagerNode"/>.</summary>
        protected KubernetesDistributedInstanceManagerNode kubernetesDistributedInstanceManagerNode;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.KubernetesDistributedInstanceManagerNodeHostedServiceWrapper class.
        /// </summary>
        public KubernetesDistributedInstanceManagerNodeHostedServiceWrapper
        (
            IOptions<DistributedAccessManagerInstanceOptions> distributedAccessManagerInstanceOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            ReaderNodeAppSettingsConfigurationTemplate readerNodeAppSettingsConfigurationTemplate,
            EventCacheNodeAppSettingsConfigurationTemplate eventCacheNodeAppSettingsConfigurationTemplate,
            WriterNodeAppSettingsConfigurationTemplate writerNodeAppSettingsConfigurationTemplate,
            DistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate,
            DistributedOperationRouterNodeAppSettingsConfigurationTemplate distributedOperationRouterNodeAppSettingsConfigurationTemplate,
            KubernetesDistributedInstanceManagerHolder kubernetesDistributedInstanceManagerHolder,
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<KubernetesDistributedInstanceManagerNodeHostedServiceWrapper> logger
        ) : base(logger)
        {
            this.distributedAccessManagerInstanceOptions = distributedAccessManagerInstanceOptions.Value;
            this.readerNodeAppSettingsConfigurationTemplate = readerNodeAppSettingsConfigurationTemplate;
            this.eventCacheNodeAppSettingsConfigurationTemplate = eventCacheNodeAppSettingsConfigurationTemplate;
            this.writerNodeAppSettingsConfigurationTemplate = writerNodeAppSettingsConfigurationTemplate;
            this.distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate = distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate;
            this.distributedOperationRouterNodeAppSettingsConfigurationTemplate = distributedOperationRouterNodeAppSettingsConfigurationTemplate;
            this.metricLoggingOptions = metricLoggingOptions.Value;
            this.kubernetesDistributedInstanceManagerHolder = kubernetesDistributedInstanceManagerHolder;
            this.tripSwitchActuator = tripSwitchActuator;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {this.GetType().Name}...");

            logger.LogInformation($"Constructing KubernetesDistributedInstanceManagerNode instance...");

            // Create field 'kubernetesDistributedInstanceManagerNode'
            CreateKubernetesDistributedInstanceManagerFields
            (
                distributedAccessManagerInstanceOptions,
                readerNodeAppSettingsConfigurationTemplate,
                eventCacheNodeAppSettingsConfigurationTemplate,
                writerNodeAppSettingsConfigurationTemplate,
                distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate,
                distributedOperationRouterNodeAppSettingsConfigurationTemplate,
                metricLoggingOptions,
                loggerFactory
            );

            kubernetesDistributedInstanceManagerHolder.KubernetesDistributedInstanceManager = kubernetesDistributedInstanceManagerNode;

            logger.LogInformation($"Completed constructing KubernetesDistributedInstanceManagerNode instance.");

            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                StartMetricLogging();
            }

            logger.LogInformation($"Completed starting {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {this.GetType().Name}...");

            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                StopMetricLogging();
            }
            logger.LogInformation($"Disposing objects...");
            kubernetesDistributedInstanceManagerNode.Dispose();
            kubernetesDistributedInstanceManager.Dispose();
            if (metricLoggingOptions.MetricLoggingEnabled.Value == true)
            {
                DisposeMetricLogger();
            }
            logger.LogInformation($"Completed disposing objects.");

            logger.LogInformation($"Completed stopping {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'kubernetesDistributedInstanceManager*' fields based on the specified configuration/options objects.
        /// </summary>
        protected void CreateKubernetesDistributedInstanceManagerFields
        (
            DistributedAccessManagerInstanceOptions distributedAccessManagerInstanceOptions,
            ReaderNodeAppSettingsConfigurationTemplate readerNodeAppSettingsConfigurationTemplate,
            EventCacheNodeAppSettingsConfigurationTemplate eventCacheNodeAppSettingsConfigurationTemplate,
            WriterNodeAppSettingsConfigurationTemplate writerNodeAppSettingsConfigurationTemplate,
            DistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate,
            DistributedOperationRouterNodeAppSettingsConfigurationTemplate distributedOperationRouterNodeAppSettingsConfigurationTemplate,
            MetricLoggingOptions metricLoggingOptions,
            ILoggerFactory loggerFactory
        )
        {
            // Create metric logger
            metricLogger = new NullMetricLogger();
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                String fullMetricLoggerCategoryName = metricLoggerCategoryName;
                if (metricLoggingOptions.MetricCategorySuffix != "")
                {
                    fullMetricLoggerCategoryName = $"{metricLoggerCategoryName}-{metricLoggingOptions.MetricCategorySuffix}";
                }
                IApplicationLogger metricBufferProcessorLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter(loggerFactory.CreateLogger<WorkerThreadBufferProcessorBase>());
                IApplicationLogger metricLoggerLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter(loggerFactory.CreateLogger<MetricLoggerBuffer>());
                var metricLoggerFactory = new MetricLoggerFactory();
                (metricLogger, metricLoggerBufferProcessingStrategy) = metricLoggerFactory.CreateMetricLoggerAndBufferProcessor
                (
                    metricLoggingOptions,
                    fullMetricLoggerCategoryName,
                    () => { return null; },
                    tripSwitchActuator,
                    metricBufferProcessorLogger,
                    metricLoggerLogger,
                    IntervalMetricBaseTimeUnit.Nanosecond,
                    true
                );
            }

            // Parse SqlServer connection parameters
            if (distributedAccessManagerInstanceOptions.SqlServerDatabaseConnection.DatabaseType.Value != DatabaseType.SqlServer)
            {
                throw new ValidationException($"Error validating {DistributedAccessManagerInstanceOptions.DistributedAccessManagerInstanceOptionsName} options.  Error validating {nameof(distributedAccessManagerInstanceOptions.SqlServerDatabaseConnection)} options.  Only '{nameof(distributedAccessManagerInstanceOptions.SqlServerDatabaseConnection.DatabaseType)}' '{DatabaseType.SqlServer.ToString()}' is supported.");
            }
            var databaseConnectionParametersParser = new SqlDatabaseConnectionParametersParser();
            SqlDatabaseConnectionParametersBase databaseConnectionParameters = databaseConnectionParametersParser.Parse
            (
                distributedAccessManagerInstanceOptions.SqlServerDatabaseConnection.DatabaseType.Value,
                distributedAccessManagerInstanceOptions.SqlServerDatabaseConnection.ConnectionParameters,
                AccessManagerSqlDatabaseConnectionOptions.AccessManagerSqlDatabaseConnectionOptionsName
            );

            // Create static configuration
            StaticConfigurationOptions staticConfigurationOptions = distributedAccessManagerInstanceOptions.StaticConfiguration;
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = new()
            {
                PodPort = staticConfigurationOptions.PodPort.Value,
                ExternalPort = staticConfigurationOptions.ExternalPort.Value,
                NameSpace = staticConfigurationOptions.NameSpace,
                PersistentStorageInstanceNamePrefix = staticConfigurationOptions.PersistentStorageInstanceNamePrefix,
                LoadBalancerServicesHttps = staticConfigurationOptions.LoadBalancerServicesHttps.Value,
                DeploymentWaitPollingInterval = staticConfigurationOptions.DeploymentWaitPollingInterval.Value,
                ServiceAvailabilityWaitAbortTimeout = staticConfigurationOptions.ServiceAvailabilityWaitAbortTimeout.Value,
                DistributedOperationCoordinatorRefreshIntervalWaitBuffer = staticConfigurationOptions.DistributedOperationCoordinatorRefreshIntervalWaitBuffer.Value,
                ReaderNodeConfigurationTemplate = new ReaderNodeConfiguration
                {
                    ReplicaCount = staticConfigurationOptions.ReaderNodeConfigurationTemplate.ReplicaCount.Value,
                    TerminationGracePeriod = staticConfigurationOptions.ReaderNodeConfigurationTemplate.TerminationGracePeriod.Value,
                    ContainerImage = staticConfigurationOptions.ReaderNodeConfigurationTemplate.ContainerImage,
                    MinimumLogLevel = staticConfigurationOptions.ReaderNodeConfigurationTemplate.MinimumLogLevel.Value,
                    AppSettingsConfigurationTemplate = readerNodeAppSettingsConfigurationTemplate,
                    CpuResourceRequest = staticConfigurationOptions.ReaderNodeConfigurationTemplate.CpuResourceRequest,
                    MemoryResourceRequest = staticConfigurationOptions.ReaderNodeConfigurationTemplate.MemoryResourceRequest,
                    LivenessProbePeriod = staticConfigurationOptions.ReaderNodeConfigurationTemplate.LivenessProbePeriod.Value,
                    StartupProbeFailureThreshold = staticConfigurationOptions.ReaderNodeConfigurationTemplate.StartupProbeFailureThreshold.Value,
                    StartupProbePeriod = staticConfigurationOptions.ReaderNodeConfigurationTemplate.StartupProbePeriod.Value
                },
                EventCacheNodeConfigurationTemplate = new EventCacheNodeConfiguration
                {
                    TerminationGracePeriod = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.TerminationGracePeriod.Value,
                    ContainerImage = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.ContainerImage,
                    MinimumLogLevel = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.MinimumLogLevel.Value,
                    AppSettingsConfigurationTemplate = eventCacheNodeAppSettingsConfigurationTemplate,
                    CpuResourceRequest = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.CpuResourceRequest,
                    MemoryResourceRequest = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.MemoryResourceRequest,
                    StartupProbeFailureThreshold = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.StartupProbeFailureThreshold.Value,
                    StartupProbePeriod = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.StartupProbePeriod.Value
                },
                WriterNodeConfigurationTemplate = new WriterNodeConfiguration
                {
                    PersistentVolumeClaimName = "eventbackup-claim",
                    TerminationGracePeriod = staticConfigurationOptions.WriterNodeConfigurationTemplate.TerminationGracePeriod.Value,
                    ContainerImage = staticConfigurationOptions.WriterNodeConfigurationTemplate.ContainerImage,
                    MinimumLogLevel = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.MinimumLogLevel.Value,
                    AppSettingsConfigurationTemplate = eventCacheNodeAppSettingsConfigurationTemplate,
                    CpuResourceRequest = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.CpuResourceRequest,
                    MemoryResourceRequest = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.MemoryResourceRequest,
                    StartupProbeFailureThreshold = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.StartupProbeFailureThreshold.Value,
                    StartupProbePeriod = staticConfigurationOptions.EventCacheNodeConfigurationTemplate.StartupProbePeriod.Value
                },
                DistributedOperationCoordinatorNodeConfigurationTemplate = new DistributedOperationCoordinatorNodeConfiguration
                {
                    ReplicaCount = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.ReplicaCount.Value,
                    TerminationGracePeriod = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.TerminationGracePeriod.Value,
                    ContainerImage = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.ContainerImage,
                    MinimumLogLevel = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.MinimumLogLevel.Value,
                    AppSettingsConfigurationTemplate = distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate,
                    CpuResourceRequest = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.CpuResourceRequest,
                    MemoryResourceRequest = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.MemoryResourceRequest,
                    StartupProbeFailureThreshold = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbeFailureThreshold.Value,
                    StartupProbePeriod = staticConfigurationOptions.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbePeriod.Value
                },
                DistributedOperationRouterNodeConfigurationTemplate = new DistributedOperationRouterNodeConfiguration
                {
                    TerminationGracePeriod = staticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate.TerminationGracePeriod.Value,
                    ContainerImage = staticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate.ContainerImage,
                    MinimumLogLevel = staticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate.MinimumLogLevel.Value,
                    AppSettingsConfigurationTemplate = distributedOperationRouterNodeAppSettingsConfigurationTemplate,
                    CpuResourceRequest = staticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate.CpuResourceRequest,
                    MemoryResourceRequest = staticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate.MemoryResourceRequest,
                    StartupProbeFailureThreshold = staticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate.StartupProbeFailureThreshold.Value,
                    StartupProbePeriod = staticConfigurationOptions.DistributedOperationRouterNodeConfigurationTemplate.StartupProbePeriod.Value
                }
            };

            // Create instance configuration
            KubernetesDistributedInstanceManagerInstanceConfigurationOptionsParser instanceConfigurationParser = new();
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> instanceConfiguration = instanceConfigurationParser.Parse(distributedAccessManagerInstanceOptions.InstanceConfiguration);

            // Create KubernetesDistributedAccessManagerInstanceManager
            SqlServerDistributedAccessManagerPersistentStorageManager persistentStorageManager = new(databaseConnectionParameters.ConnectionString, true);
            SqlServerCredentialsAppSettingsConfigurer credentialsAppSettingsConfigurer = new();
            AccessManagerRestClientConfigurationJsonSerializer jsonSerializer = new();
            Int32 ReadConnectionParameter(String configurationValueName)
            {
                String value = distributedAccessManagerInstanceOptions.SqlServerDatabaseConnection.ConnectionParameters[configurationValueName];
                return Int32.Parse(value);
            }
            Int32 sqlServerRetryCount = ReadConnectionParameter("RetryCount");
            Int32 sqlServerRetryInterval = ReadConnectionParameter("RetryInterval");
            Int32 sqlServerOperationTimeout = ReadConnectionParameter("OperationTimeout");
            IApplicationLogger shardConfigurationSetPersisterLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter(loggerFactory.CreateLogger<SqlServerShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>>());
            Func<SqlServerLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction = (SqlServerLoginCredentials credentials) =>
            {
                var configurationDbConnectionString = new SqlConnectionStringBuilder(credentials.ConnectionString);
                return new SqlServerShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>
                (
                    configurationDbConnectionString.ToString(),
                    sqlServerRetryCount,
                    sqlServerRetryInterval,
                    sqlServerOperationTimeout, 
                    jsonSerializer,
                    shardConfigurationSetPersisterLogger
                );
            };
            IApplicationLogger kubernetesDistributedAccessManagerInstanceManagerLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter(loggerFactory.CreateLogger<KubernetesDistributedAccessManagerInstanceManager<SqlServerLoginCredentials>>());
            if (instanceConfiguration == null)
            {
                kubernetesDistributedInstanceManager = new KubernetesDistributedAccessManagerInstanceManager<SqlServerLoginCredentials>
                (
                    staticConfiguration,
                    persistentStorageManager,
                    credentialsAppSettingsConfigurer,
                    shardConfigurationSetPersisterCreationFunction,
                    kubernetesDistributedAccessManagerInstanceManagerLogger,
                    metricLogger
                );
            }
            else
            {
                kubernetesDistributedInstanceManager = new KubernetesDistributedAccessManagerInstanceManager<SqlServerLoginCredentials>
                (
                    staticConfiguration,
                    instanceConfiguration,
                    persistentStorageManager,
                    credentialsAppSettingsConfigurer,
                    shardConfigurationSetPersisterCreationFunction,
                    kubernetesDistributedAccessManagerInstanceManagerLogger,
                    metricLogger
                );
            }
            IApplicationLogger kubernetesDistributedInstanceManagerNodeLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter(loggerFactory.CreateLogger<KubernetesDistributedInstanceManagerNode>());
            kubernetesDistributedInstanceManagerNode = new KubernetesDistributedInstanceManagerNode
            (
                kubernetesDistributedInstanceManager,
                sqlServerRetryCount,
                sqlServerRetryInterval,
                sqlServerOperationTimeout,
                distributedAccessManagerInstanceOptions.ShardConnection.RetryCount.Value,
                distributedAccessManagerInstanceOptions.ShardConnection.RetryInterval.Value,
                distributedAccessManagerInstanceOptions.ShardConnection.ConnectionTimeout.Value,
                kubernetesDistributedInstanceManagerNodeLogger,
                metricLogger
            );
        }

        #endregion
    }
}
