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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution.Kubernetes;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;

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
        
        // TODO
        protected DistributedAccessManagerInstanceOptions distributedAccessManagerInstanceOptions;
        protected ReaderNodeAppSettingsConfigurationTemplate readerNodeAppSettingsConfigurationTemplate;
        protected EventCacheNodeAppSettingsConfigurationTemplate eventCacheNodeAppSettingsConfigurationTemplate;
        protected WriterNodeAppSettingsConfigurationTemplate writerNodeAppSettingsConfigurationTemplate;
        protected DistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate distributedOperationCoordinatorNodeAppSettingsConfigurationTemplate;
        protected DistributedOperationRouterNodeAppSettingsConfigurationTemplate distributedOperationRouterNodeAppSettingsConfigurationTemplate;
        protected MetricLoggingOptions metricLoggingOptions;
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
            this.tripSwitchActuator = tripSwitchActuator;
            this.loggerFactory = loggerFactory;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {this.GetType().Name}...");

            logger.LogInformation($"Constructing KubernetesDistributedInstanceManagerNode instance...");


            logger.LogInformation($"Completed starting {this.GetType().Name}.");

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {

            return Task.CompletedTask;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'kubernetesDistributedInstanceManagerNode' member constructor parameter members based on the specified configuration/options objects.
        /// </summary>
        protected void InitializeKubernetesDistributedInstanceManagerNodeConstructorParameters
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
            
        }

        #endregion
    }
}
