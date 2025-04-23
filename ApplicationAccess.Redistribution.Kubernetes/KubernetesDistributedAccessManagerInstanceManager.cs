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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.LaunchPreparer;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution;
using ApplicationAccess.Redistribution.Kubernetes.Metrics;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationAccess.Redistribution.Models;
using ApplicationLogging;
using ApplicationMetrics;
using Newtonsoft.Json.Linq;
using k8s;
using k8s.Models;

namespace ApplicationAccess.Redistribution.Kubernetes
{
    /// <summary>
    /// Manages a distributed AccessManager implementation hosted in Kubernetes.
    /// </summary>
    /// <typeparam name="TPersistentStorageCredentials">An implementation of <see cref="IPersistentStorageLoginCredentials"/> defining the type of login credentials for persistent storage instances.</typeparam>
    public class KubernetesDistributedAccessManagerInstanceManager<TPersistentStorageCredentials> : IDistributedAccessManagerInstanceManager<TPersistentStorageCredentials>, IDisposable
        where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
    {
        // TODO:
        //   If I have to introduce a specific dependency on SQL server, add this to the class description...
        //     ", and using Microsoft SQL Server for persistence."
        //   Some method to get the URL for the public service which the distopcoord(s) expose
        //   Will want to allow different resource values for user readers, group readers, etc...
        //     Might want to also do the same for startup/liveliness etc...
        //     Maybe reader node replicas too... group to group might need more
        //   Do we need different log parameters per database??
        //     Maybe just use connection string... will be easier
        //   ENSURE ALL portected members are marked as protected
        //   Use ResourceQuantity.Validate() to check '120Mi' and similar values
        //   Creation of ShardConfig database should be optional... if creation false need to provide TConnectionCreds for it
        //   Validation for 'configuration' parameter
        //   Will need to access writer node and router from outside the cluster during split.  Might have to create temporary 'ClusterIP' services to access them.
        //   In the thosted version of thie class, InvalidOperationException should be mapped to maybe 400?
        //     e.g. trying to create LB when it's already been created
        //   KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration class might need to
        //     return TPersistentStorageCredentials for each shard group 
        //     How to access those DBs to split??



        #pragma warning disable 1591

        protected const String appLabel = "app";
        protected const String serviceNamePostfix = "-service";
        protected const String externalServiceNamePostfix = "-externalservice";
        protected const String eventBackupVolumeMountPath = "/eventbackup";
        protected const String eventBackupPersistentVolumeName = "eventbackup-storage";
        protected const String eventBackupFilePostfix = "-eventbackup";
        protected const String clusterIpServiceType = "ClusterIP";
        protected const String loadBalancerServiceType = "LoadBalancer";
        protected const String tcpProtocol = "TCP";
        protected const String distributedOperationCoordinatorObjectNamePrefix = "operation-coordinator";
        protected const String distributedOperationRouterObjectNamePrefix = "operation-router";
        protected const String nodeModeEnvironmentVariableName = "MODE";
        protected const String nodeModeEnvironmentVariableValue = "Launch";
        protected const String nodeListenPortEnvironmentVariableName = "LISTEN_PORT";
        protected const String nodeMinimumLogLevelEnvironmentVariableName = "MINIMUM_LOG_LEVEL";
        protected const String nodeEncodedJsonConfigurationEnvironmentVariableName = "ENCODED_JSON_CONFIGURATION";
        protected const String nodeStatusApiEndpointUrl = "/api/v1/status";
        protected const String requestsCpuKey = "cpu";
        protected const String requestsMemoryKey = "memory";
        protected const String appsettingsInitialCatalogPropertyName = "InitialCatalog";
        protected const String appsettingsEventCacheConnectionPropertyName = "EventCacheConnection";
        protected const String appsettingsEventCachingPropertyName = "EventCaching";
        protected const String appsettingsHostPropertyName = "Host";
        protected const String appsettingsEventPersistencePropertyName = "EventPersistence";
        protected const String appsettingsEventPersisterBackupFilePathPropertyName = "EventPersisterBackupFilePath";
        protected const String appsettingsMetricLoggingPropertyName = "MetricLogging";
        protected const String appsettingsMetricCategorySuffixPropertyName = "MetricCategorySuffix";
        protected const String appsettingsShardRoutingPropertyName = "ShardRouting";
        protected const String appsettingsDataElementTypePropertyName = "DataElementType";
        protected const String appsettingsSourceQueryShardBaseUrlPropertyName = "SourceQueryShardBaseUrl";
        protected const String appsettingsSourceEventShardBaseUrlPropertyName = "SourceEventShardBaseUrl";
        protected const String appsettingsSourceShardHashRangeStartPropertyName = "SourceShardHashRangeStart";
        protected const String appsettingsSourceShardHashRangeEndPropertyName = "SourceShardHashRangeEnd";
        protected const String appsettingsTargetQueryShardBaseUrlPropertyName = "TargetQueryShardBaseUrl";
        protected const String appsettingsTargetEventShardBaseUrlPropertyName = "TargetEventShardBaseUrl";
        protected const String appsettingsTargetShardHashRangeStartPropertyName = "TargetShardHashRangeStart";
        protected const String appsettingsTargetShardHashRangeEndPropertyName = "TargetShardHashRangeEnd";
        protected const String appsettingsRoutingInitiallyOnPropertyName = "RoutingInitiallyOn";

        #pragma warning restore 1591

        /// <summary>The number of milliseconds to wait after the 'TerminationGracePeriod' expires for a deployment to scale down, before throwing an exception.</summary>
        protected const Int32 scaleDownTerminationGracePeriodBuffer = 1000;

        /// <summary>Static configuration for the instance manager.</summary>
        protected KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration;
        /// <summary>Configuration for the distributed AccessManager instance.</summary>
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration;
        /// <summary>The client to connect to Kubernetes.</summary>
        protected k8s.Kubernetes kubernetesClient;
        /// <summary>Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to the Kubernetes client class.</summary>
        protected IKubernetesClientShim kubernetesClientShim;
        /// <summary>Used to create new instances of persistent storage used by the distributed AccessManager implementation.</summary>
        protected IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator;
        /// <summary>Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</summary>
        protected IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer;
        /// <summary>Used to write shard configuration to persistent storage.</summary>
        protected IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>The unique id to use for newly created shard groups.</summary>
        protected Int32 nextShardGroupId;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Configuration for the distributed AccessManager instance.
        /// </summary>
        public KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> InstanceConfiguration
        {
            get { return instanceConfiguration; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister, 
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            InitializeFields(staticConfiguration, clientConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersister, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            InitializeFields(staticConfiguration, instanceConfiguration, clientConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersister, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="kubernetesConfigurationFilePath">The full path to the configuration file to use to connect to the Kubernetes cluster(s).</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration, 
            String kubernetesConfigurationFilePath, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister,
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationFilePath);
            InitializeFields(staticConfiguration, clientConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersister, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="kubernetesConfigurationFilePath">The full path to the configuration file to use to connect to the Kubernetes cluster(s).</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration,
            String kubernetesConfigurationFilePath,
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationFilePath);
            InitializeFields(staticConfiguration, instanceConfiguration, clientConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersister, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
        /// <param name="kubernetesClientShim">A mock <see cref="IKubernetesClientShim"/>.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration,
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister,
            IKubernetesClientShim kubernetesClientShim, 
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            this.staticConfiguration = staticConfiguration;
            this.instanceConfiguration = instanceConfiguration;
            kubernetesClient = null;
            this.persistentStorageCreator = persistentStorageCreator;
            this.credentialsAppSettingsConfigurer = credentialsAppSettingsConfigurer;
            this.shardConfigurationSetPersister = shardConfigurationSetPersister;
            this.kubernetesClientShim = kubernetesClientShim;
            this.logger = logger;
            this.metricLogger = metricLogger;
            UpdateNextShardGroupId(instanceConfiguration);
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access the distributed router component used for shard group splitting, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the load balancer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        /// <remarks>This method should be called before creating a distributed AccessManager instance.  Some Kubernetes hosting platforms (e.g. Minikube) require additional actions outside of the cluster to allow Kubernetes services to be accessed from outside of the host machine (e.g. in the case if Minikube the IP address and port of the load balancer service must exposed outside the machine using 'simpleproxy' or a similar tool).  Hence this method can be called, and then any required additional actions be performed.</remarks>
        public async Task<IPAddress> CreateDistributedOperationRouterLoadBalancerService(UInt16 port)
        {
            if (instanceConfiguration.DistributedOperationRouterUrl != null)
                throw new InvalidOperationException("A load balancer service for the distributed operation router has already been created.");

            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating load balancer service for distributed operation router on port {port} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new LoadBalancerServiceCreateTime());

            try
            {
                await CreateLoadBalancerServiceAsync(distributedOperationRouterObjectNamePrefix, externalServiceNamePostfix, port, staticConfiguration.PodPort);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Error creating distributed router load balancer service '{distributedOperationRouterObjectNamePrefix}{externalServiceNamePostfix}' in namespace '{nameSpace}'.", e);
            }
            try
            {
                await WaitForLoadBalancerServiceAsync($"{distributedOperationRouterObjectNamePrefix}{externalServiceNamePostfix}", staticConfiguration.DeploymentWaitPollingInterval, staticConfiguration.ServiceAvailabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Failed to wait for distributed router load balancer service '{distributedOperationRouterObjectNamePrefix}{externalServiceNamePostfix}' in namespace '{nameSpace}' to become available.", e);
            }
            IPAddress returnIpAddress = null;
            try
            {
                returnIpAddress = await GetLoadBalancerServiceIpAddressAsync($"{distributedOperationRouterObjectNamePrefix}{externalServiceNamePostfix}");
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Error retrieving IP address for distributed router load balancer service '{distributedOperationRouterObjectNamePrefix}{externalServiceNamePostfix}' in namespace '{nameSpace}'.", e);
            }
            Uri distributedOperationRouterUrl = new($"{GetLoadBalancerServiceScheme()}://{returnIpAddress.ToString()}:{port}");
            instanceConfiguration.DistributedOperationRouterUrl = distributedOperationRouterUrl;

            metricLogger.End(beginId, new LoadBalancerServiceCreateTime());
            metricLogger.Increment(new LoadBalancerServiceCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed creating load balancer service.");

            return returnIpAddress;
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access a writer component which is part of a shard group undergoing a split operation, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the writer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        /// <remarks>This method should be called before creating a distributed AccessManager instance.  Some Kubernetes hosting platforms (e.g. Minikube) require additional actions outside of the cluster to allow Kubernetes services to be accessed from outside of the host machine (e.g. in the case if Minikube the IP address and port of the load balancer service must exposed outside the machine using 'simpleproxy' or a similar tool).  Hence this method can be called, and then any required additional actions be performed.</remarks>
        public async Task<IPAddress> CreateWriterLoadBalancerService(UInt16 port)
        {
            if (instanceConfiguration.WriterUrl != null)
                throw new InvalidOperationException("A load balancer service for writer components has already been created.");

            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating load balancer service for writer on port {port} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new LoadBalancerServiceCreateTime());

            String appLabelValue = NodeType.Writer.ToString().ToLower();
            try
            {
                await CreateLoadBalancerServiceAsync(appLabelValue, externalServiceNamePostfix, port, staticConfiguration.PodPort);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Error creating writer load balancer service '{appLabelValue}{externalServiceNamePostfix}' in namespace '{nameSpace}'.", e);
            }
            try
            {
                await WaitForLoadBalancerServiceAsync($"{appLabelValue}{externalServiceNamePostfix}", staticConfiguration.DeploymentWaitPollingInterval, staticConfiguration.ServiceAvailabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Failed to wait for writer load balancer service '{appLabelValue}{externalServiceNamePostfix}' in namespace '{nameSpace}' to become available.", e);
            }
            IPAddress returnIpAddress = null;
            try
            {
                returnIpAddress = await GetLoadBalancerServiceIpAddressAsync($"{appLabelValue}{externalServiceNamePostfix}");
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Error retrieving IP address for writer load balancer service '{appLabelValue}{externalServiceNamePostfix}' in namespace '{nameSpace}'.", e);
            }
            Uri writerUrl = new($"{GetLoadBalancerServiceScheme()}://{returnIpAddress.ToString()}:{port}");
            instanceConfiguration.WriterUrl = writerUrl;

            metricLogger.End(beginId, new LoadBalancerServiceCreateTime());
            metricLogger.Increment(new LoadBalancerServiceCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed creating load balancer service.");

            return returnIpAddress;
        }

        /// <inheritdoc/>>
        public async Task CreateDistributedAccessManagerInstanceAsync
        (
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> userShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupToGroupMappingShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupShardGroupConfiguration
        )
        {
            if (instanceConfiguration.DistributedOperationRouterUrl == null)
                throw new InvalidOperationException($"A distributed operation router load balancer service must be created via method {nameof(CreateDistributedOperationRouterLoadBalancerService)}() before creating a distributed AccessManager instance.");
            if (instanceConfiguration.WriterUrl == null)
                throw new InvalidOperationException($"A writer load balancer service must be created via method {nameof(CreateWriterLoadBalancerService)}() before creating a distributed AccessManager instance.");
            if (instanceConfiguration.UserShardGroupConfiguration != null || instanceConfiguration.GroupToGroupMappingShardGroupConfiguration != null || instanceConfiguration.GroupShardGroupConfiguration != null)
                throw new InvalidOperationException($"A distributed AccessManager instance has already been created.");
            if (userShardGroupConfiguration.Count == 0)
                throw new ArgumentException($"Parameter '{nameof(userShardGroupConfiguration)}' cannot be empty.", nameof(userShardGroupConfiguration));
            if (groupToGroupMappingShardGroupConfiguration.Count != 1)
                throw new ArgumentException($"Parameter '{nameof(groupToGroupMappingShardGroupConfiguration)}' must contain a single value (actually contained {groupToGroupMappingShardGroupConfiguration.Count}).  Only a single group to group mapping shard group is supported.", nameof(groupToGroupMappingShardGroupConfiguration));
            if (groupShardGroupConfiguration.Count == 0)
                throw new ArgumentException($"Parameter '{nameof(groupShardGroupConfiguration)}' cannot be empty.", nameof(groupShardGroupConfiguration));
            ValidateShardGroupConfigurationParameter(nameof(userShardGroupConfiguration), userShardGroupConfiguration);
            ValidateShardGroupConfigurationParameter(nameof(groupToGroupMappingShardGroupConfiguration), groupToGroupMappingShardGroupConfiguration);
            ValidateShardGroupConfigurationParameter(nameof(groupShardGroupConfiguration), groupShardGroupConfiguration);

            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating distributed AccessManager instance in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new DistributedAccessManagerInstanceCreateTime());

            try
            {
                // Create the user shard groups
                foreach (ShardGroupConfiguration<TPersistentStorageCredentials> currentUserShardGroupConfiguration in userShardGroupConfiguration)
                {
                    TPersistentStorageCredentials credentials = await CreateShardGroupAsync(DataElement.User, currentUserShardGroupConfiguration.HashRangeStart, currentUserShardGroupConfiguration.PersistentStorageCredentials);
                    if (currentUserShardGroupConfiguration.PersistentStorageCredentials == null)
                    {
                        currentUserShardGroupConfiguration.PersistentStorageCredentials = credentials;
                    }
                }
                // Create the group to group mapping shard groups
                foreach (ShardGroupConfiguration<TPersistentStorageCredentials> currentGroupToGroupMappingShardGroupConfiguration in groupToGroupMappingShardGroupConfiguration)
                {
                    TPersistentStorageCredentials credentials = await CreateShardGroupAsync(DataElement.GroupToGroupMapping, currentGroupToGroupMappingShardGroupConfiguration.HashRangeStart, currentGroupToGroupMappingShardGroupConfiguration.PersistentStorageCredentials);
                    if (currentGroupToGroupMappingShardGroupConfiguration.PersistentStorageCredentials == null)
                    {
                        currentGroupToGroupMappingShardGroupConfiguration.PersistentStorageCredentials = credentials;
                    }
                }
                // Create the group shard groups
                foreach (ShardGroupConfiguration<TPersistentStorageCredentials> currentGroupShardGroupConfiguration in groupShardGroupConfiguration)
                {
                    TPersistentStorageCredentials credentials = await CreateShardGroupAsync(DataElement.Group, currentGroupShardGroupConfiguration.HashRangeStart, currentGroupShardGroupConfiguration.PersistentStorageCredentials);
                    if (currentGroupShardGroupConfiguration.PersistentStorageCredentials == null)
                    {
                        currentGroupShardGroupConfiguration.PersistentStorageCredentials = credentials;
                    }
                }

                // Populate the 'instanceConfiguration' field
                PopulateInstanceShardGroupConfiguration
                (
                    userShardGroupConfiguration,
                    groupToGroupMappingShardGroupConfiguration,
                    groupShardGroupConfiguration
                );

                // Create persistent storage for the shard configuration
                String persistentStorageInstanceName = "shard_configuration";
                if (staticConfiguration.PersistentStorageInstanceNamePrefix != "")
                {
                    persistentStorageInstanceName = $"{staticConfiguration.PersistentStorageInstanceNamePrefix}_{persistentStorageInstanceName}";
                }
                if (instanceConfiguration.ShardConfigurationPersistentStorageCredentials == null)
                {
                    try
                    {
                        TPersistentStorageCredentials credentials = persistentStorageCreator.CreateAccessManagerConfigurationPersistentStorage(persistentStorageInstanceName);
                        instanceConfiguration.ShardConfigurationPersistentStorageCredentials = credentials;
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error creating persistent storage instance for shard configuration.", e);
                    }
                }

                // Create and populate the shard configuration
                ShardConfigurationSet<AccessManagerRestClientConfiguration> shardConfigurationSet = CreateShardConfigurationSet
                (
                    instanceConfiguration.UserShardGroupConfiguration,
                    instanceConfiguration.GroupToGroupMappingShardGroupConfiguration,
                    instanceConfiguration.GroupShardGroupConfiguration
                );
                try
                {
                    shardConfigurationSetPersister.Write(shardConfigurationSet, true);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error writing shard configuration to persistent storage.", e);
                }

                // Create the distributed operation coordinator node(s) and service
                await CreateDistributedOperationCoordinatorNodeAsync(instanceConfiguration.ShardConfigurationPersistentStorageCredentials);
                IPAddress distributedOperationCoordinatorIpAddress = await CreateDistributedOperationCoordinatorLoadBalancerService(staticConfiguration.ExternalPort);
                Uri distributedOperationCoordinatorUrl = new($"{GetLoadBalancerServiceScheme()}://{distributedOperationCoordinatorIpAddress.ToString()}:{staticConfiguration.ExternalPort}");
                instanceConfiguration.DistributedOperationCoordinatorUrl = distributedOperationCoordinatorUrl;
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new DistributedAccessManagerInstanceCreateTime());
                throw;
            }

            metricLogger.End(beginId, new DistributedAccessManagerInstanceCreateTime());
            metricLogger.Increment(new DistributedAccessManagerInstanceCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes fields of the class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="clientConfiguration">The Kubernetes client configuration.</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        protected void InitializeFields
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration, 
            KubernetesClientConfiguration clientConfiguration, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer, 
            IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister,
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            this.staticConfiguration = staticConfiguration;
            instanceConfiguration = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>();
            kubernetesClient = new k8s.Kubernetes(clientConfiguration);
            this.persistentStorageCreator = persistentStorageCreator;
            this.credentialsAppSettingsConfigurer = credentialsAppSettingsConfigurer;
            this.shardConfigurationSetPersister = shardConfigurationSetPersister;
            kubernetesClientShim = new DefaultKubernetesClientShim();
            this.logger = logger;
            this.metricLogger = metricLogger;
            nextShardGroupId = 0;
        }

        /// <summary>
        /// Initializes fields of the class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="clientConfiguration">The Kubernetes client configuration.</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        protected void InitializeFields
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration,
            KubernetesClientConfiguration clientConfiguration,
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer, 
            IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            InitializeFields(staticConfiguration, clientConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersister, logger, metricLogger);
            this.instanceConfiguration = instanceConfiguration;
            UpdateNextShardGroupId(instanceConfiguration);
        }

        /// <summary>
        /// Populates the <see cref="KubernetesDistributedAccessManagerInstanceManager{TPersistentStorageCredentials}.instanceConfiguration">'instanceConfiguration'</see> member with the specified shard group configuration.
        /// </summary>
        /// <param name="userShardGroupConfiguration">The configuration of the user shard groups.</param>
        /// <param name="groupToGroupMappingShardGroupConfiguration">The configuration of the group to group mapping shard groups.</param>
        /// <param name="groupShardGroupConfiguration">The configuration of the group shard groups.</param>
        protected void PopulateInstanceShardGroupConfiguration
        (
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> userShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupToGroupMappingShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupShardGroupConfiguration
        )
        {
            SortShardGroupConfigurationByHashRangeStart(userShardGroupConfiguration);
            List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> userKubernetesShardGroupConfiguration = CreateKubernetesShardGroupConfigurationList(userShardGroupConfiguration, DataElement.User);
            instanceConfiguration.UserShardGroupConfiguration = userKubernetesShardGroupConfiguration;
            SortShardGroupConfigurationByHashRangeStart(groupToGroupMappingShardGroupConfiguration);
            List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> groupToGroupMappingKubernetesShardGroupConfiguration = CreateKubernetesShardGroupConfigurationList(groupToGroupMappingShardGroupConfiguration, DataElement.GroupToGroupMapping);
            instanceConfiguration.GroupToGroupMappingShardGroupConfiguration = groupToGroupMappingKubernetesShardGroupConfiguration;
            SortShardGroupConfigurationByHashRangeStart(groupShardGroupConfiguration);
            List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> groupKubernetesShardGroupConfiguration = CreateKubernetesShardGroupConfigurationList(groupShardGroupConfiguration, DataElement.Group);
            instanceConfiguration.GroupShardGroupConfiguration = groupKubernetesShardGroupConfiguration;
        }

        /// <summary>
        /// Creates a list of <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> from the specified list of <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/>.
        /// </summary>
        /// <param name="shardGroupConfiguration">The list of <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/> to create the Kubernetes configuration from.</param>
        /// <param name="dataElement">The data element to create the Kubernetes configuration for.</param>
        /// <returns>he list of <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/>.</returns>
        protected List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> CreateKubernetesShardGroupConfigurationList
        (
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> shardGroupConfiguration, 
            DataElement dataElement
        )
        {
            List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> returnShardGroupConfiguration = new();
            foreach (ShardGroupConfiguration<TPersistentStorageCredentials> currentShardGroupConfiguration in shardGroupConfiguration)
            {

                KubernetesShardGroupConfiguration<TPersistentStorageCredentials> kubernetesConfiguration = CreateKubernetesShardGroupConfiguration(currentShardGroupConfiguration, dataElement);
                returnShardGroupConfiguration.Add(kubernetesConfiguration);
            }

            return returnShardGroupConfiguration;
        }

        /// <summary>
        /// Creates <see cref="KubernetesShardGroupConfiguration{TPersistentStorageCredentials}"/> from the specified <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/>.
        /// </summary>
        /// <param name="shardGroupConfiguration">The <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/> to create the Kubernetes configuration from.</param>
        /// <param name="dataElement">The data element to create the Kubernetes configuration for.</param>
        /// <returns>The Kubernetes configuration.</returns>
        protected KubernetesShardGroupConfiguration<TPersistentStorageCredentials> CreateKubernetesShardGroupConfiguration(ShardGroupConfiguration<TPersistentStorageCredentials> shardGroupConfiguration, DataElement dataElement)
        {
            // Set id and then increment nextShardGroupId
            Int32 readerNodeId = nextShardGroupId++;
            Int32 writerNodeId = nextShardGroupId++;
            Uri readerNodeServiceUrl = GenerateNodeServiceUrl(dataElement, NodeType.Reader, shardGroupConfiguration.HashRangeStart);
            Uri writerNodeServiceUrl = GenerateNodeServiceUrl(dataElement, NodeType.Writer, shardGroupConfiguration.HashRangeStart);
            AccessManagerRestClientConfiguration readerNodeClientConfiguration = new(readerNodeServiceUrl);
            AccessManagerRestClientConfiguration writerNodeClientConfiguration = new(writerNodeServiceUrl);
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> kubernetesConfiguration = new
            (
                readerNodeId,
                writerNodeId,
                shardGroupConfiguration.HashRangeStart,
                shardGroupConfiguration.PersistentStorageCredentials,
                readerNodeClientConfiguration,
                writerNodeClientConfiguration
            );

            return kubernetesConfiguration;
        }

        /// <summary>
        /// Updates the 'nextShardGroupId' field to the next number after the maximum found in the specified instance configuration.
        /// </summary>
        /// <param name="instanceConfiguration">The instance configuration.</param>
        protected void UpdateNextShardGroupId(KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration)
        {
            Int32 maxId = -1;
            List<IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>> allShardGroupConfiguration = new();
            if (instanceConfiguration.UserShardGroupConfiguration != null)
            {
                allShardGroupConfiguration.Add(instanceConfiguration.UserShardGroupConfiguration);
            }
            if (instanceConfiguration.GroupToGroupMappingShardGroupConfiguration != null)
            {
                allShardGroupConfiguration.Add(instanceConfiguration.GroupToGroupMappingShardGroupConfiguration);
            }
            if (instanceConfiguration.GroupShardGroupConfiguration != null)
            {
                allShardGroupConfiguration.Add(instanceConfiguration.GroupShardGroupConfiguration);
            }
            foreach (IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> currentConfigurationList in allShardGroupConfiguration)
            {
                foreach (KubernetesShardGroupConfiguration<TPersistentStorageCredentials> currentConfiguration in currentConfigurationList)
                {
                    if (currentConfiguration.ReaderNodeId > maxId)
                    {
                        maxId = currentConfiguration.ReaderNodeId;
                    }
                    if (currentConfiguration.WriterNodeId > maxId)
                    {
                        maxId = currentConfiguration.WriterNodeId;
                    }
                }
            }
            nextShardGroupId = maxId + 1;
        }

        /// <summary>
        /// Creates a <see cref="ShardConfigurationSet{TClientConfiguration}"/> from the specified Kubernetes shard group configurations.
        /// </summary>
        /// <param name="userShardGroupConfiguration">The configuration of the user shard groups.</param>
        /// <param name="groupToGroupMappingShardGroupConfiguration">The configuration of the group to group mapping shard groups.</param>
        /// <param name="groupShardGroupConfiguration">The configuration of the group shard groups.</param>
        /// <returns>The <see cref="ShardConfigurationSet{TClientConfiguration}"/>.</returns>
        protected ShardConfigurationSet<AccessManagerRestClientConfiguration> CreateShardConfigurationSet
        (
            IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> userShardGroupConfiguration,
            IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> groupToGroupMappingShardGroupConfiguration,
            IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> groupShardGroupConfiguration
        )
        {
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> configurationList = new();
            configurationList.AddRange(CreateShardConfigurationList(userShardGroupConfiguration, DataElement.User));
            configurationList.AddRange(CreateShardConfigurationList(groupToGroupMappingShardGroupConfiguration, DataElement.GroupToGroupMapping));
            configurationList.AddRange(CreateShardConfigurationList(groupShardGroupConfiguration, DataElement.Group));

            return new ShardConfigurationSet<AccessManagerRestClientConfiguration>(configurationList);
        }

        /// <summary>
        /// Creates a list of <see cref="ShardConfiguration{TClientConfiguration}"/> from the specified Kubernetes shard group configuration.
        /// </summary>
        /// <param name="shardGroupConfiguration">The configuration of the shard groups.</param>
        /// <param name="dataElement">The data element to create the shard configuration set for.</param>
        /// <returns>The list of <see cref="ShardConfiguration{TClientConfiguration}"/>.</returns>
        protected IList<ShardConfiguration<AccessManagerRestClientConfiguration>> CreateShardConfigurationList
        (
            IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> shardGroupConfiguration,
            DataElement dataElement
        )
        {
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> returnList = new();
            foreach (KubernetesShardGroupConfiguration<TPersistentStorageCredentials> currentConfiguration in shardGroupConfiguration)
            {
                AccessManagerRestClientConfiguration readerClientConfiguration = new AccessManagerRestClientConfiguration(currentConfiguration.ReaderNodeClientConfiguration.BaseUrl);
                ShardConfiguration<AccessManagerRestClientConfiguration> readerConfiguration = new(currentConfiguration.ReaderNodeId, dataElement, Operation.Query, currentConfiguration.HashRangeStart, readerClientConfiguration);
                returnList.Add(readerConfiguration);
                AccessManagerRestClientConfiguration writerClientConfiguration = new AccessManagerRestClientConfiguration(currentConfiguration.WriterNodeClientConfiguration.BaseUrl);
                ShardConfiguration<AccessManagerRestClientConfiguration> writerConfiguration = new(currentConfiguration.WriterNodeId, dataElement, Operation.Event, currentConfiguration.HashRangeStart, writerClientConfiguration);
                returnList.Add(writerConfiguration);
            }

            return returnList;
        }

        /// <summary>
        /// Validates that all of the specified JSON paths exist within a JSON document.
        /// </summary>
        /// <param name="jsonDocument">The JSON document to check.</param>
        /// <param name="paths">The JSON paths to check for.</param>
        /// <param name="jsonDocumentContentsDescription">A description of the contents of the JSON document, for use in exception messages.  E.g. 'appsettings configuration for reader nodes'.</param>
        /// <remarks>This method is designed to be used to confirm the existence of paths in a <see cref="JObject"/> which this class writes to, so as to prevent null reference exceptions when accessing the paths.</remarks>
        protected void ValidatePathsExistInJsonDocument(JObject jsonDocument, IEnumerable<String> paths, String jsonDocumentContentsDescription)
        {
            foreach (String currentPath in paths)
            {
                try
                {
                    jsonDocument.SelectToken(currentPath, true);
                }
                catch (Exception e)
                {
                    throw new Exception($"JSON path '{currentPath}' was not found in JSON document containing {jsonDocumentContentsDescription}.", e);
                }
            }
        }

        /// <summary>
        /// Generates a unique identifier for an ApplicationAccess node (i.e. Kubernetes pod/deployment) based on its key properties.
        /// </summary>
        /// <param name="dataElement">The data element handled by the node.</param>
        /// <param name="nodeType">The type of the node.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes handled by the node.</param>
        /// <remarks>For use in pod/deployment names, service hostnames, database names, etc...</remarks>
        /// <returns>The identifier.</returns>
        protected String GenerateNodeIdentifier(DataElement dataElement, NodeType nodeType, Int32 hashRangeStart)
        {
            return $"{dataElement.ToString().ToLower()}-{nodeType.ToString().ToLower()}-{StringifyHashRangeStart(hashRangeStart)}";
        }

        /// <summary>
        /// Generates a name for a persistent storage instance.
        /// </summary>
        /// <param name="dataElement">The data element stored in the persistent storage instance.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes stored in the persistent storage instance.</param>
        /// <returns>The name.</returns>
        protected String GeneratePersistentStorageInstanceName(DataElement dataElement, Int32 hashRangeStart)
        {
            if (staticConfiguration.PersistentStorageInstanceNamePrefix != "")
            {
                return $"{staticConfiguration.PersistentStorageInstanceNamePrefix}_{dataElement.ToString().ToLower()}_{StringifyHashRangeStart(hashRangeStart)}";
            }
            else
            {
                return $"{dataElement.ToString().ToLower()}_{StringifyHashRangeStart(hashRangeStart)}";
            }
        }

        /// <summary>
        /// Generates the URL to connect to an ApplicationAccess node's service (i.e. a Kubernetes pod/deployment).
        /// </summary>
        /// <param name="dataElement">The data element managed by the node.</param>
        /// <param name="nodeType">The type of the node.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the node.</param>
        /// <returns>The URL.</returns>
        protected Uri GenerateNodeServiceUrl(DataElement dataElement, NodeType nodeType, Int32 hashRangeStart)
        {
            return new Uri($"http://{GenerateNodeIdentifier(dataElement, nodeType, hashRangeStart)}{serviceNamePostfix}:{staticConfiguration.PodPort}");
        }

        /// <summary>
        /// Converts the specified hash range value into a string which can be used in identifier names (i.e. replacing the negation symbol for negative hash range values).
        /// </summary>
        /// <param name="hashRangeStart">The hash range value.</param>
        /// <returns>The stringified hash range value.</returns>
        protected String StringifyHashRangeStart(Int32 hashRangeStart)
        {
            String returnString = hashRangeStart.ToString();
            if (hashRangeStart < 0)
            {
                returnString = returnString.Replace('-', 'n');
            }

            return returnString;
        }

        /// <summary>
        /// Generates the abort timeout in milliseconds to wait for a deployment to become available.
        /// </summary>
        /// <param name="nodeConfiguration">The node configuration to use to calculate the abort timeout.</param>
        /// <returns>The abort timeout in milliseconds</returns>
        protected Int32 GenerateAvailabilityWaitAbortTimeout(NodeConfigurationBase nodeConfiguration)
        {
            return (nodeConfiguration.StartupProbeFailureThreshold + 1) * nodeConfiguration.StartupProbePeriod * 1000;
        }

        /// <summary>
        /// Gets a string containing the HTTP scheme used to connect to load balancer services from outside the Kubernetes cluster.
        /// </summary>
        /// <returns>The HTTP scheme.</returns>
        protected String GetLoadBalancerServiceScheme()
        {
            if (staticConfiguration.LoadBalancerServicesHttps == true)
            {
                return "https";
            }
            else
            {
                return "http";
            }
        }

        /// <summary>
        /// Validates a method parameter containing a list of <see cref="ShardGroupConfiguration{TPersistentStorageCredentials}"/> objects.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        protected void ValidateShardGroupConfigurationParameter(String parameterName, IList<ShardGroupConfiguration<TPersistentStorageCredentials>> parameterValue)
        {
            HashSet<Int32> allHashRangeStartValues = new();
            Int32 minHashRangeStartValue = Int32.MaxValue;
            foreach (ShardGroupConfiguration<TPersistentStorageCredentials> currentParameterValue in parameterValue)
            {
                if (currentParameterValue.HashRangeStart < minHashRangeStartValue)
                {
                    minHashRangeStartValue = currentParameterValue.HashRangeStart;
                }
                if (allHashRangeStartValues.Contains(currentParameterValue.HashRangeStart) == true)
                {
                    throw new ArgumentException($"Parameter '{parameterName}' contains duplicate hash range start value {currentParameterValue.HashRangeStart}.", parameterName);
                }
                allHashRangeStartValues.Add(currentParameterValue.HashRangeStart);
            }
            if (minHashRangeStartValue != Int32.MinValue)
            {
                throw new ArgumentException($"Parameter '{parameterName}' must contain one element with value {Int32.MinValue}.", parameterName);
            }
        }

        #pragma warning disable 1591

        protected void SortShardGroupConfigurationByHashRangeStart(IList<ShardGroupConfiguration<TPersistentStorageCredentials>> shardGroupConfiguration)
        {
            List<ShardGroupConfiguration<TPersistentStorageCredentials>> sortedConfiguration = new(shardGroupConfiguration.OrderBy(shardGroupConfiguration => shardGroupConfiguration.HashRangeStart));
            shardGroupConfiguration.Clear();
            foreach (ShardGroupConfiguration<TPersistentStorageCredentials> currentSortedConfigurationItem in sortedConfiguration)
            {
                shardGroupConfiguration.Add(currentSortedConfigurationItem);
            }
        }

        protected void ThrowExceptionIfIntegerParameterLessThan1(String parameterName, Int32 parameterValue)
        {
            if (parameterValue < 1)
                throw new ArgumentOutOfRangeException(parameterName, $"Parameter '{parameterName}' with value {parameterValue} must be greater than 0.");
        }

        #pragma warning restore 1591

        #endregion

        #region ApplicationAccess Node and Shard Group Creation Methods

        /// <summary>
        /// Creates a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element to create the shard group for.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        /// <param name="persistentStorageCredentials">Optional credentials for the persistent storage used by the reader and writer nodes.  If set to null, a new persistent storage instance will be created.</param>
        /// <returns>The persistent storage credentials passed in parameter <paramref name="persistentStorageCredentials"/> (in the case <paramref name="persistentStorageCredentials"/> was non-null), or the credentials for the persistent storage instance created for the shard group (in the case <paramref name="persistentStorageCredentials"/> was null).</returns>
        protected async Task<TPersistentStorageCredentials> CreateShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, TPersistentStorageCredentials persistentStorageCredentials=null)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid shardGroupBeginId = metricLogger.Begin(new ShardGroupCreateTime());

            if (persistentStorageCredentials == null)
            {
                // Create a persistent storage instance
                logger.Log(ApplicationLogging.LogLevel.Information, $"Creating persistent storage instance for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart}...");
                String persistentStorageInstanceName = GeneratePersistentStorageInstanceName(dataElement, hashRangeStart);
                Guid storageBeginId = metricLogger.Begin(new PersistentStorageInstanceCreateTime());
                try
                {
                    persistentStorageCredentials = persistentStorageCreator.CreateAccessManagerPersistentStorage(persistentStorageInstanceName);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(storageBeginId, new PersistentStorageInstanceCreateTime());
                    metricLogger.CancelBegin(shardGroupBeginId, new ShardGroupCreateTime());
                    throw new Exception($"Error creating persistent storage instance for data element type '{dataElement}' and hash range start value {hashRangeStart}.", e);
                }
                metricLogger.End(storageBeginId, new PersistentStorageInstanceCreateTime());
                metricLogger.Increment(new PersistentStorageInstanceCreated());
                logger.Log(ApplicationLogging.LogLevel.Information, $"Completed creating persistent storage instance.");
            }

            // Create event cache node
            try
            {
                await CreateEventCacheNodeAsync(dataElement, hashRangeStart);
            }
            catch
            {
                metricLogger.CancelBegin(shardGroupBeginId, new ShardGroupCreateTime());
                throw;
            }
            Uri eventCacheServiceUrl = GenerateNodeServiceUrl(dataElement, NodeType.EventCache, hashRangeStart);

            // Create reader and writer nodes
            Task createReaderNodeTask = Task.Run(async () => await CreateReaderNodeAsync(dataElement, hashRangeStart, persistentStorageCredentials, eventCacheServiceUrl));
            Task createWriterNodeTask = Task.Run(async () => await CreateWriterNodeAsync(dataElement, hashRangeStart, persistentStorageCredentials, eventCacheServiceUrl));
            try
            {
                await Task.WhenAll(createReaderNodeTask, createWriterNodeTask);
            }
            catch
            {
                metricLogger.CancelBegin(shardGroupBeginId, new ShardGroupCreateTime());
                throw;
            }

            metricLogger.End(shardGroupBeginId, new ShardGroupCreateTime());
            metricLogger.Increment(new ShardGroupCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed creating shard group.");

            return persistentStorageCredentials;
        }

        /// <summary>
        /// Restarts all nodes of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element of the shard group.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        protected async Task RestartShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Restarting shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ShardGroupRestartTime());

            try
            {
                await ScaleDownShardGroupAsync(dataElement, hashRangeStart);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new ShardGroupRestartTime());
                throw new Exception($"Error scaling down shard group for data element '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            try
            {
                await ScaleUpShardGroupAsync(dataElement, hashRangeStart);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new ShardGroupRestartTime());
                throw new Exception($"Error scaling up shard group for data element '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }

            metricLogger.End(beginId, new ShardGroupRestartTime());
            metricLogger.Increment(new ShardGroupRestarted());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed restarting shard group.");
        }

        /// <summary>
        /// Scales down all nodes of a shard group in a distributed AccessManager implementation (i.e. sets all node's pod/deployment replica counts to 0, and waits for the scale down to complete).
        /// </summary>
        /// <param name="dataElement">The data element of the shard group.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        protected async Task ScaleDownShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Scaling down shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ShardGroupScaleDownTime());

            // Scale down reader and writer nodes
            String readerNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Reader, hashRangeStart);
            String writerNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Writer, hashRangeStart);
            Task scaleDownReaderNodeTask = Task.Run(async () => await ScaleDeploymentAsync(readerNodeDeploymentName, 0));
            Task scaleDownWriterNodeTask = Task.Run(async () => await ScaleDeploymentAsync(writerNodeDeploymentName, 0));
            Task waitForScaleDownReaderNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentScaleDownAsync(readerNodeDeploymentName, staticConfiguration.DeploymentWaitPollingInterval, (staticConfiguration.ReaderNodeConfigurationTemplate.TerminationGracePeriod * 1000) + scaleDownTerminationGracePeriodBuffer);
            });
            Task waitForScaleDownWriterNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentScaleDownAsync(writerNodeDeploymentName, staticConfiguration.DeploymentWaitPollingInterval, (staticConfiguration.WriterNodeConfigurationTemplate.TerminationGracePeriod * 1000) + scaleDownTerminationGracePeriodBuffer);
            });
            try
            {
                await Task.WhenAll(scaleDownReaderNodeTask, scaleDownWriterNodeTask, waitForScaleDownReaderNodeTask, waitForScaleDownWriterNodeTask);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ShardGroupScaleDownTime());
                throw;
            }

            // Scale down event cache node
            String eventCacheNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.EventCache, hashRangeStart);
            Task scaleDownEventCacheNodeTask = Task.Run(async () => await ScaleDeploymentAsync(eventCacheNodeDeploymentName, 0));
            Task waitForScaleDownEventCacheNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentScaleDownAsync(eventCacheNodeDeploymentName, staticConfiguration.DeploymentWaitPollingInterval, (staticConfiguration.EventCacheNodeConfigurationTemplate.TerminationGracePeriod * 1000) + scaleDownTerminationGracePeriodBuffer);
            });
            try
            {
                await Task.WhenAll(scaleDownEventCacheNodeTask, waitForScaleDownEventCacheNodeTask);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ShardGroupScaleDownTime());
                throw;
            }

            metricLogger.End(beginId, new ShardGroupScaleDownTime());
            metricLogger.Increment(new ShardGroupScaledDown());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed scaling down shard group.");
        }

        /// <summary>
        /// Scales up all nodes of a shard group in a distributed AccessManager implementation (i.e. sets all node's pod/deployment replica counts to their original, non-0 values, and waits for the scale up to complete).
        /// </summary>
        /// <param name="dataElement">The data element of the shard group.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        protected async Task ScaleUpShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Scaling up shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ShardGroupScaleUpTime());

            // Scale up event cache node
            String eventCacheNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.EventCache, hashRangeStart);
            Int32 eventCacheNodeAvailabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.EventCacheNodeConfigurationTemplate);
            Task scaleUpEventCacheNodeTask = Task.Run(async () => await ScaleDeploymentAsync(eventCacheNodeDeploymentName, 1));
            Task waitForScaleUpEventCacheNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentAvailabilityAsync(eventCacheNodeDeploymentName, staticConfiguration.DeploymentWaitPollingInterval, eventCacheNodeAvailabilityWaitAbortTimeout);
            });
            try
            {
                await Task.WhenAll(scaleUpEventCacheNodeTask, waitForScaleUpEventCacheNodeTask);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ShardGroupScaleUpTime());
                throw;
            }

            // Scale up reader and writer nodes
            String readerNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Reader, hashRangeStart);
            String writerNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Writer, hashRangeStart);
            Int32 readerNodeAvailabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.ReaderNodeConfigurationTemplate);
            Int32 writerNodeAvailabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.WriterNodeConfigurationTemplate);
            Task scaleUpReaderNodeTask = Task.Run(async () => await ScaleDeploymentAsync(readerNodeDeploymentName, staticConfiguration.ReaderNodeConfigurationTemplate.ReplicaCount));
            Task scaleUpWriterNodeTask = Task.Run(async () => await ScaleDeploymentAsync(writerNodeDeploymentName, 1));
            Task waitForScaleUpReaderNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentAvailabilityAsync(readerNodeDeploymentName, staticConfiguration.DeploymentWaitPollingInterval, readerNodeAvailabilityWaitAbortTimeout);
            });
            Task waitForScaleUpWriterNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentAvailabilityAsync(writerNodeDeploymentName, staticConfiguration.DeploymentWaitPollingInterval, writerNodeAvailabilityWaitAbortTimeout);
            });
            try
            {
                await Task.WhenAll(scaleUpReaderNodeTask, scaleUpWriterNodeTask, waitForScaleUpReaderNodeTask, waitForScaleUpWriterNodeTask);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ShardGroupScaleUpTime());
                throw;
            }

            metricLogger.End(beginId, new ShardGroupScaleUpTime());
            metricLogger.Increment(new ShardGroupScaledUp());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed scaling up shard group.");
        }

        /// <summary>
        /// Creates a reader node as part of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element to create the reader node for.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group the reader node is a member of.</param>
        /// <param name="persistentStorageCredentials">Credentials to connect to the persistent storage for the reader node.</param>
        /// <param name="eventCacheServiceUrl">The URL for the service for the event cache that the reader node should consume events from.</param>
        /// <returns></returns>
        protected async Task CreateReaderNodeAsync(DataElement dataElement, Int32 hashRangeStart, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
        {
            String nameSpace = staticConfiguration.NameSpace;
            String deploymentName = GenerateNodeIdentifier(dataElement, NodeType.Reader, hashRangeStart);
            Func<Task> createDeploymentFunction = () => CreateReaderNodeDeploymentAsync(deploymentName, persistentStorageCredentials, eventCacheServiceUrl);
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.ReaderNodeConfigurationTemplate);
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating reader node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ReaderNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, hashRangeStart, createDeploymentFunction, "reader", availabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new ReaderNodeCreateTime());
                throw new Exception($"Error creating reader node for data element type '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            metricLogger.End(beginId, new ReaderNodeCreateTime());
            metricLogger.Increment(new ReaderNodeCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, $"Completed creating reader node.");
        }

        /// <summary>
        /// Creates an event cache node as part of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element to create the event cache node for.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group the event cache node is a member of.</param>
        protected async Task CreateEventCacheNodeAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            String nameSpace = staticConfiguration.NameSpace;
            String deploymentName = GenerateNodeIdentifier(dataElement, NodeType.EventCache, hashRangeStart);
            Func<Task> createDeploymentFunction = () => CreateEventCacheNodeDeploymentAsync(deploymentName);
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.EventCacheNodeConfigurationTemplate);
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating event cache node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new EventCacheNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, hashRangeStart, createDeploymentFunction, "event cache", availabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventCacheNodeCreateTime());
                throw new Exception($"Error creating event cache node for data element type '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            metricLogger.End(beginId, new EventCacheNodeCreateTime());
            metricLogger.Increment(new EventCacheNodeCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, $"Completed creating event cache node.");
        }

        /// <summary>
        /// Creates a writer node as part of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element to create the writer node for.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group the writer node is a member of.</param>
        /// <param name="persistentStorageCredentials">Credentials to connect to the persistent storage for the writer node.</param>
        /// <param name="eventCacheServiceUrl">The URL for the service for the event cache that the writer node should consume events from.</param>
        /// <returns></returns>
        protected async Task CreateWriterNodeAsync(DataElement dataElement, Int32 hashRangeStart, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
        {
            String nameSpace = staticConfiguration.NameSpace;
            String deploymentName = GenerateNodeIdentifier(dataElement, NodeType.Writer, hashRangeStart);
            Func<Task> createDeploymentFunction = () => CreateWriterNodeDeploymentAsync(deploymentName, persistentStorageCredentials, eventCacheServiceUrl);
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.WriterNodeConfigurationTemplate);
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating writer node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new WriterNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, hashRangeStart, createDeploymentFunction, "writer", availabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new WriterNodeCreateTime());
                throw new Exception($"Error creating writer node for data element type '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            metricLogger.End(beginId, new WriterNodeCreateTime());
            metricLogger.Increment(new WriterNodeCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, $"Completed creating writer node.");
        }

        /// <summary>
        /// Creates a distributed operation coordinator node in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="shardConfigurationPersistentStorageCredentials">Credentials to connect to the persistent storage for the shard configuration.</param>
        /// <returns></returns>
        protected async Task CreateDistributedOperationCoordinatorNodeAsync(TPersistentStorageCredentials shardConfigurationPersistentStorageCredentials)
        {
            String nameSpace = staticConfiguration.NameSpace;
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate);
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating distributed operation coordinator node in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new DistributedOperationCoordinatorNodeCreateTime());
            Task createDeploymentTask = Task.Run(async () =>
            {
                try
                {
                    await CreateDistributedOperationCoordinatorNodeDeploymentAsync(distributedOperationCoordinatorObjectNamePrefix, shardConfigurationPersistentStorageCredentials);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error creating distributed operation coordinator deployment in namespace '{nameSpace}'.", e);
                }
            });
            Task waitForDeploymentTask = Task.Run(async () =>
            {
                try
                {
                    await WaitForDeploymentAvailabilityAsync(distributedOperationCoordinatorObjectNamePrefix, staticConfiguration.DeploymentWaitPollingInterval, availabilityWaitAbortTimeout);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error waiting for distributed operation coordinator deployment in namespace '{nameSpace}' to become available.", e);
                }
            });
            try
            {
                await Task.WhenAll(createDeploymentTask, waitForDeploymentTask);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new DistributedOperationCoordinatorNodeCreateTime());
                throw;
            }
            metricLogger.End(beginId, new DistributedOperationCoordinatorNodeCreateTime());
            metricLogger.Increment(new DistributedOperationCoordinatorNodeCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, $"Completed creating distributed operation coordinator node.");
        }

        /// <summary>
        /// Creates an ApplicationAccess 'node' as part of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="deploymentName">The name of the Kubernetes deployment to create to host the node.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group the node is a member of.</param>
        /// <param name="createDeploymentFunction">An async <see cref="Func{TResult}"/> which creates the Kubernetes deployment for the node.</param>
        /// <param name="nodeTypeName">The name of the type of the node (to use in exception messages, e.g. 'event cache', 'reader', etc...).</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the node hasn't become available.</param>
        protected async Task CreateApplicationAccessNodeAsync
        (
            String deploymentName,
            Int32 hashRangeStart,
            Func<Task> createDeploymentFunction,
            String nodeTypeName,
            Int32 abortTimeout
        )
        {
            String nameSpace = staticConfiguration.NameSpace;
            Task createDeploymentTask = Task.Run(async () =>
            {
                try
                {
                    await createDeploymentFunction();
                }
                catch (Exception e)
                {
                    throw new Exception($"Error creating {nodeTypeName} deployment '{deploymentName}' in namespace '{nameSpace}'.", e);
                }
            });
            Task createServiceTask = Task.Run(async () =>
            {
                try
                {
                    await CreateClusterIpServiceAsync(deploymentName, serviceNamePostfix, staticConfiguration.PodPort);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error creating {nodeTypeName} service '{deploymentName}{serviceNamePostfix}' in namespace '{nameSpace}'.", e);
                }
            });
            Task waitForDeploymentTask = Task.Run(async () =>
            {
                try
                {
                    await WaitForDeploymentAvailabilityAsync(deploymentName, staticConfiguration.DeploymentWaitPollingInterval, abortTimeout);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error waiting for {nodeTypeName} deployment '{deploymentName}' in namespace '{nameSpace}' to become available.", e);
                }
            });
            await Task.WhenAll(createDeploymentTask, createServiceTask, waitForDeploymentTask);
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access the distributed operation coordinator component from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the load balancer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        protected async Task<IPAddress> CreateDistributedOperationCoordinatorLoadBalancerService(UInt16 port)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating load balancer service for distributed operation coordinator on port {port} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new LoadBalancerServiceCreateTime());

            try
            {
                await CreateLoadBalancerServiceAsync(distributedOperationCoordinatorObjectNamePrefix, externalServiceNamePostfix, port, staticConfiguration.PodPort);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Error creating distributed operation coordinator load balancer service '{distributedOperationCoordinatorObjectNamePrefix}{externalServiceNamePostfix}' in namespace '{nameSpace}'.", e);
            }
            try
            {
                await WaitForLoadBalancerServiceAsync($"{distributedOperationCoordinatorObjectNamePrefix}{externalServiceNamePostfix}", staticConfiguration.DeploymentWaitPollingInterval, staticConfiguration.ServiceAvailabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Failed to wait for distributed operation coordinator load balancer service '{distributedOperationCoordinatorObjectNamePrefix}{externalServiceNamePostfix}' in namespace '{nameSpace}' to become available.", e);
            }
            IPAddress returnIpAddress = null;
            try
            {
                returnIpAddress = await GetLoadBalancerServiceIpAddressAsync($"{distributedOperationCoordinatorObjectNamePrefix}{externalServiceNamePostfix}");
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new LoadBalancerServiceCreateTime());
                throw new Exception($"Error retrieving IP address for distributed operation coordinator load balancer service '{distributedOperationCoordinatorObjectNamePrefix}{externalServiceNamePostfix}' in namespace '{nameSpace}'.", e);
            }
            Uri distributedOperationCoordinatorUrl = new($"{GetLoadBalancerServiceScheme()}://{returnIpAddress.ToString()}:{port}");
            instanceConfiguration.DistributedOperationCoordinatorUrl = distributedOperationCoordinatorUrl;

            metricLogger.End(beginId, new LoadBalancerServiceCreateTime());
            metricLogger.Increment(new LoadBalancerServiceCreated());
            logger.Log(ApplicationLogging.LogLevel.Information, "Completed creating load balancer service.");

            return returnIpAddress;
        }

        #endregion

        #region Kubernetes Object Creation Methods

        /// <summary>
        /// Creates a Kubernetes service of type 'ClusterIP'.
        /// </summary>
        /// <param name="appLabelValue">The name of the pod/deployment targetted by the service.</param>
        /// <param name="serviceNamePostfix">The postfix to attach to the <paramref name="appLabelValue"/> to form the name of the service.</param>
        /// <param name="port">The TCP port the service should expose.</param>
        protected async Task CreateClusterIpServiceAsync(String appLabelValue, String serviceNamePostfix, UInt16 port)
        {
            V1Service serviceDefinition = ClusterIpServiceTemplate;
            serviceDefinition.Metadata.Name = $"{appLabelValue}{serviceNamePostfix}";
            serviceDefinition.Spec.Selector.Add(appLabel, appLabelValue);
            serviceDefinition.Spec.Ports[0].Port = port;
            serviceDefinition.Spec.Ports[0].TargetPort = port;

            try
            {
                await kubernetesClientShim.CreateNamespacedServiceAsync(kubernetesClient, serviceDefinition, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create Kubernetes '{clusterIpServiceType}' service '{appLabelValue}{serviceNamePostfix}' for pod '{appLabelValue}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer'.
        /// </summary>
        /// <param name="appLabelValue">The name of the pod/deployment targetted by the service.</param>
        /// <param name="serviceNamePostfix">The postfix to attach to the <paramref name="appLabelValue"/> to form the name of the service.</param>
        /// <param name="port">The TCP port the load balancer service should expose.</param>
        /// <param name="targetPort">The TCP port to use to connect to the targetted pod/deployment.</param>
        protected async Task CreateLoadBalancerServiceAsync(String appLabelValue, String serviceNamePostfix, UInt16 port, UInt16 targetPort)
        {
            V1Service serviceDefinition = LoadBalancerServiceTemplate;
            serviceDefinition.Metadata.Name = $"{appLabelValue}{serviceNamePostfix}";
            serviceDefinition.Spec.Selector.Add(appLabel, appLabelValue);
            serviceDefinition.Spec.Ports[0].Port = port;
            serviceDefinition.Spec.Ports[0].TargetPort = targetPort;

            try
            {
                await kubernetesClientShim.CreateNamespacedServiceAsync(kubernetesClient, serviceDefinition, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create Kubernetes '{loadBalancerServiceType}' service '{appLabelValue}{serviceNamePostfix}' for pod '{appLabelValue}'.", e);
            }
        }

        /// <summary>
        /// Retrieves the external endpoint IP address of a Kubernetes service of type 'LoadBalancer'.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The external endpoint IP address.</returns>
        public async Task<IPAddress> GetLoadBalancerServiceIpAddressAsync(String serviceName)
        {
            // TODO: This works in Minikube, but not sure if it will work in AKS or EKS
            //   Need to test and adjust if necessary to something that works across different Kubernetes hosting platforms

            String nameSpace = staticConfiguration.NameSpace;
            V1Service loadBalancerService = null;
            try
            {
                foreach (V1Service currentService in await kubernetesClientShim.ListNamespacedServiceAsync(kubernetesClient, nameSpace))
                {
                    if (currentService.Name() == serviceName)
                    {
                        loadBalancerService = currentService;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to retrieve load balancer service '{serviceName}' in namespace '{nameSpace}'.", e);
            }
            if (loadBalancerService == null)
            {
                throw new Exception($"Could not find load balancer service '{serviceName}' in namespace '{nameSpace}'.");
            }
            if (loadBalancerService.Status?.LoadBalancer?.Ingress == null || loadBalancerService.Status.LoadBalancer.Ingress.Count == 0)
            {
                throw new Exception($"Load balancer service '{serviceName}' in namespace '{nameSpace}' did not contain an ingress point.");
            }
            if (IPAddress.TryParse(loadBalancerService.Status.LoadBalancer.Ingress[0].Ip, out IPAddress returnIPAddress) == false)
            {
                throw new Exception($"Failed to convert ingress 'Ip' property '{loadBalancerService.Status.LoadBalancer.Ingress[0].Ip}' to an IP address, for load balancer service '{serviceName}' in namespace '{nameSpace}'.");
            }
            
            return returnIPAddress;
        }

        /// <summary>
        /// Creates a Kubernetes deployment for a reader node.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="persistentStorageCredentials">Credentials to connect to the persistent storage for the reader node.</param>
        /// <param name="eventCacheServiceUrl">The URL for the service for the event cache that the reader node should consume events from.</param>
        protected async Task CreateReaderNodeDeploymentAsync(String name, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = staticConfiguration.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
            List<String> requiredPaths = new()
            {
                appsettingsEventCacheConnectionPropertyName,
                appsettingsMetricLoggingPropertyName
            };
            ValidatePathsExistInJsonDocument(appsettingsContents, requiredPaths, "appsettings configuration for reader nodes");
            appsettingsContents[appsettingsEventCacheConnectionPropertyName][appsettingsHostPropertyName] = eventCacheServiceUrl.ToString();
            appsettingsContents[appsettingsMetricLoggingPropertyName][appsettingsMetricCategorySuffixPropertyName] = name;
            credentialsAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(persistentStorageCredentials, appsettingsContents);
            var encoder = new Base64StringEncoder();
            var encodedAppsettingsContents = encoder.Encode(appsettingsContents.ToString());

            V1Deployment deploymentDefinition = ReaderNodeDeploymentTemplate;
            deploymentDefinition.Metadata.Name = name;
            deploymentDefinition.Spec.Selector.MatchLabels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Metadata.Labels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Spec.Containers[0].Name = name;
            deploymentDefinition.Spec.Template.Spec.Containers[0].Env = new[]
            {
                new V1EnvVar { Name = nodeModeEnvironmentVariableName, Value = nodeModeEnvironmentVariableValue },
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = staticConfiguration.PodPort.ToString() }, 
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = staticConfiguration.ReaderNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create reader node Kubernetes deployment '{name}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes deployment for an event cache node.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        protected async Task CreateEventCacheNodeDeploymentAsync(String name)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = staticConfiguration.EventCacheNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
            List<String> requiredPaths = new(){ appsettingsMetricLoggingPropertyName };
            ValidatePathsExistInJsonDocument(appsettingsContents, requiredPaths, "appsettings configuration for event cache nodes");
            appsettingsContents[appsettingsMetricLoggingPropertyName][appsettingsMetricCategorySuffixPropertyName] = name;
            var encoder = new Base64StringEncoder();
            var encodedAppsettingsContents = encoder.Encode(appsettingsContents.ToString());

            V1Deployment deploymentDefinition = EventCacheNodeDeploymentTemplate;
            deploymentDefinition.Metadata.Name = name;
            deploymentDefinition.Spec.Selector.MatchLabels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Metadata.Labels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Spec.Containers[0].Name = name;
            deploymentDefinition.Spec.Template.Spec.Containers[0].Env = new[]
            {
                new V1EnvVar { Name = nodeModeEnvironmentVariableName, Value = nodeModeEnvironmentVariableValue },
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = staticConfiguration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = staticConfiguration.EventCacheNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create event cache node Kubernetes deployment '{name}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes deployment for a writer node.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="persistentStorageCredentials">Credentials to connect to the persistent storage for the reader node.</param>
        /// <param name="eventCacheServiceUrl">The URL for the service for the event cache that the writer node should write events to.</param>
        protected async Task CreateWriterNodeDeploymentAsync(String name, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = staticConfiguration.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
            List<String> requiredPaths = new()
            {
                appsettingsEventPersistencePropertyName,
                appsettingsEventCacheConnectionPropertyName,
                appsettingsMetricLoggingPropertyName
            };
            ValidatePathsExistInJsonDocument(appsettingsContents, requiredPaths, "appsettings configuration for writer nodes");
            appsettingsContents[appsettingsEventPersistencePropertyName][appsettingsEventPersisterBackupFilePathPropertyName] = $"{eventBackupVolumeMountPath}/{name}{eventBackupFilePostfix}.json";
            appsettingsContents[appsettingsEventCacheConnectionPropertyName][appsettingsHostPropertyName] = eventCacheServiceUrl.ToString();
            appsettingsContents[appsettingsMetricLoggingPropertyName][appsettingsMetricCategorySuffixPropertyName] = name;
            credentialsAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(persistentStorageCredentials, appsettingsContents);
            var encoder = new Base64StringEncoder();
            var encodedAppsettingsContents = encoder.Encode(appsettingsContents.ToString());

            V1Deployment deploymentDefinition = WriterNodeDeploymentTemplate;
            deploymentDefinition.Metadata.Name = name;
            deploymentDefinition.Spec.Selector.MatchLabels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Metadata.Labels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Spec.Containers[0].Name = name;
            deploymentDefinition.Spec.Template.Spec.Containers[0].Env = new[]
            {
                new V1EnvVar { Name = nodeModeEnvironmentVariableName, Value = nodeModeEnvironmentVariableValue },
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = staticConfiguration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = staticConfiguration.WriterNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create writer node Kubernetes deployment '{name}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes deployment for a distributed operation coordinator node.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="persistentStorageCredentials">Credentials to connect to the persistent storage for the distributed operation coordinator node.</param>
        protected async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync(String name, TPersistentStorageCredentials persistentStorageCredentials)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
            List<String> requiredPaths = new()
            {
                appsettingsMetricLoggingPropertyName
            };
            ValidatePathsExistInJsonDocument(appsettingsContents, requiredPaths, "appsettings configuration for distributed operation coordinator nodes");
            appsettingsContents[appsettingsMetricLoggingPropertyName][appsettingsMetricCategorySuffixPropertyName] = name;
            credentialsAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(persistentStorageCredentials, appsettingsContents);
            var encoder = new Base64StringEncoder();
            var encodedAppsettingsContents = encoder.Encode(appsettingsContents.ToString());

            V1Deployment deploymentDefinition = DistributedOperationCoordinatorNodeDeploymentTemplate;
            deploymentDefinition.Metadata.Name = name;
            deploymentDefinition.Spec.Selector.MatchLabels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Metadata.Labels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Spec.Containers[0].Name = name;
            deploymentDefinition.Spec.Template.Spec.Containers[0].Env = new[]
            {
                new V1EnvVar { Name = nodeModeEnvironmentVariableName, Value = nodeModeEnvironmentVariableValue },
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = staticConfiguration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create distributed operation coordinator node Kubernetes deployment '{name}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes deployment for a distributed operation router node.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="dataElement">The type of data element of the shard groups the router should manage (i.e. sit in front of).</param>
        /// <param name="sourceReaderUrl">The URL of the reader node(s) of the source shard group.</param>
        /// <param name="sourceWriterUrl">The URL of the writer node of the source shard group.</param>
        /// <param name="sourceHashRangeStart">The first (inclusive) in the range of hash codes of data elements managed by the source shard group.</param>
        /// <param name="sourceHashRangeEnd">The last (inclusive) in the range of hash codes of data elements managed by the source shard group.</param>
        /// <param name="targetReaderUrl">The URL of the reader node(s) of the target shard group.</param>
        /// <param name="targetWriterUrl">The URL of the writer node of the target shard group.</param>
        /// <param name="targetHashRangeStart">The first (inclusive) in the range of hash codes of data elements managed by the target shard group.</param>
        /// <param name="targetHashRangeEnd">The last (inclusive) in the range of hash codes of data elements managed by the target shard group.</param>
        /// <param name="routingInitiallyOn">Whether or not the routing functionality is initially swicthed on.</param>
        protected async Task CreateDistributedOperationRouterNodeDeploymentAsync
        (
            String name, 
            DataElement dataElement,
            Uri sourceReaderUrl,
            Uri sourceWriterUrl,
            Int32 sourceHashRangeStart,
            Int32 sourceHashRangeEnd,
            Uri targetReaderUrl,
            Uri targetWriterUrl,
            Int32 targetHashRangeStart,
            Int32 targetHashRangeEnd,
            Boolean routingInitiallyOn
        )
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
            List<String> requiredPaths = new()
            {
                appsettingsShardRoutingPropertyName,
                appsettingsMetricLoggingPropertyName
            };
            ValidatePathsExistInJsonDocument(appsettingsContents, requiredPaths, "appsettings configuration for distributed operation router nodes");
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsDataElementTypePropertyName] = dataElement.ToString();
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsSourceQueryShardBaseUrlPropertyName] = sourceReaderUrl.ToString(); ;
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsSourceEventShardBaseUrlPropertyName] = sourceWriterUrl.ToString();
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsSourceShardHashRangeStartPropertyName] = sourceHashRangeStart;
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsSourceShardHashRangeEndPropertyName] = sourceHashRangeEnd;
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsTargetQueryShardBaseUrlPropertyName] = targetReaderUrl.ToString();
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsTargetEventShardBaseUrlPropertyName] = targetWriterUrl.ToString();
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsTargetShardHashRangeStartPropertyName] = targetHashRangeStart;
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsTargetShardHashRangeEndPropertyName] = targetHashRangeEnd;
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsRoutingInitiallyOnPropertyName] = routingInitiallyOn;
            appsettingsContents[appsettingsMetricLoggingPropertyName][appsettingsMetricCategorySuffixPropertyName] = name;
            var encoder = new Base64StringEncoder();
            var encodedAppsettingsContents = encoder.Encode(appsettingsContents.ToString());

            V1Deployment deploymentDefinition = DistributedOperationRouterNodeDeploymentTemplate;
            deploymentDefinition.Metadata.Name = name;
            deploymentDefinition.Spec.Selector.MatchLabels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Metadata.Labels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Spec.Containers[0].Name = name;
            deploymentDefinition.Spec.Template.Spec.Containers[0].Env = new[]
            {
                new V1EnvVar { Name = nodeModeEnvironmentVariableName, Value = nodeModeEnvironmentVariableValue },
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = staticConfiguration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create distributed operation router node Kubernetes deployment '{name}'.", e);
            }
        }

        /// <summary>
        /// Scales a deployment to the specified number of replicas
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="replicaCount">The number of replicas to scale to.</param>
        protected async Task ScaleDeploymentAsync(String name, Int32 replicaCount)
        {
            if (replicaCount < 0)
                throw new ArgumentOutOfRangeException(nameof(replicaCount), $"Parameter '{nameof(replicaCount)}' with value {replicaCount} must be greater than or equal to 0.");

            // TODO: This is the only way I could find to do this, but surely there is a more robust way?
            //   Using V1Scale and V1ScaleSpec objects rather than JSON?
            String patchJson = $"{{\"spec\": {{\"replicas\": {replicaCount}}}}}";
            V1Patch patchDefinition = new(patchJson, V1Patch.PatchType.MergePatch);

            try
            {
                await kubernetesClientShim.PatchNamespacedDeploymentScaleAsync(kubernetesClient, patchDefinition, name, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to scale Kubernetes deployment '{name}' to {replicaCount} replicas.", e);
            }
        }

        /// <summary>
        /// Waits for the specified load balancer service to become available.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="checkInterval">The interval in milliseconds between successive checks.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the service hasn't become available.</param>
        protected async Task WaitForLoadBalancerServiceAsync(String serviceName, Int32 checkInterval, Int32 abortTimeout)
        {
            // Same as for comment in GetLoadBalancerServiceIpAddressAsync()... need to check that this works outside of Minikube

            ThrowExceptionIfIntegerParameterLessThan1(nameof(checkInterval), checkInterval);
            ThrowExceptionIfIntegerParameterLessThan1(nameof(abortTimeout), abortTimeout);

            String nameSpace = staticConfiguration.NameSpace;
            Boolean foundServiceIp = true;
            Stopwatch stopwatch = new();
            stopwatch.Start();
            do
            {
                foundServiceIp = false;
                try
                {
                    foreach (V1Service currentService in await kubernetesClientShim.ListNamespacedServiceAsync(kubernetesClient, nameSpace))
                    {
                        if (currentService.Name() == serviceName)
                        {
                            if (currentService.Status?.LoadBalancer?.Ingress != null)
                            {
                                if (currentService.Status.LoadBalancer.Ingress.Count >= 1)
                                {
                                    foundServiceIp = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to wait for load balancer service '{serviceName}' in namespace '{nameSpace}' to become available.", e);
                }
                if (foundServiceIp == false)
                {
                    await Task.Delay(checkInterval);
                }
                else
                {
                    break;
                }
            }
            while (stopwatch.ElapsedMilliseconds < abortTimeout);

            if (foundServiceIp == false)
            {
                throw new Exception($"Timeout value of {abortTimeout} milliseconds expired while waiting for load balancer service '{serviceName}' in namespace '{nameSpace}' to become available.");
            }
        }

        /// <summary>
        /// Waits for the specified deployment to become available.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="checkInterval">The interval in milliseconds between successive checks.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the deployment hasn't become available.</param>
        protected async Task WaitForDeploymentAvailabilityAsync(String name, Int32 checkInterval, Int32 abortTimeout)
        {
            Predicate<V1Deployment> waitForAvailabilityPredicate = (V1Deployment deployment) =>
            {
                if (deployment.Name() == name)
                {
                    if (deployment.Status.AvailableReplicas != null && deployment.Status.AvailableReplicas.Value != 0)
                    {
                        return true;
                    }
                }

                return false;
            };

            try
            {
                await WaitForDeploymentPredicateAsync(waitForAvailabilityPredicate, checkInterval, abortTimeout);
            }
            catch (DeploymentPredicateWaitTimeoutExpiredException timeoutException)
            {
                throw new Exception($"Timeout value of {abortTimeout} milliseconds expired while waiting for Kubernetes deployment '{name}' to become available.", timeoutException);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to wait for Kubernetes deployment '{name}' to become available.", e);
            }
        }

        /// <summary>
        /// Waits for the pods of the specified deployment to shut down.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="checkInterval">The interval in milliseconds between successive checks.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the deployment hasn't scaled down.</param>
        protected async Task WaitForDeploymentScaleDownAsync(String name, Int32 checkInterval, Int32 abortTimeout)
        {
            ThrowExceptionIfIntegerParameterLessThan1(nameof(checkInterval), checkInterval);
            ThrowExceptionIfIntegerParameterLessThan1(nameof(abortTimeout), abortTimeout);

            Boolean foundPod = true;
            Stopwatch stopwatch = new();
            stopwatch.Start();
            do
            {
                foundPod = false;
                try
                {
                    foreach (V1Pod currentPod in await kubernetesClientShim.ListNamespacedPodAsync(kubernetesClient, staticConfiguration.NameSpace))
                    {
                        if (currentPod.Name().StartsWith(name) == true)
                        {
                            foundPod = true;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to wait for Kubernetes deployment '{name}' to scale down.", e);
                }
                if (foundPod == true)
                {
                    await Task.Delay(checkInterval);
                }
                else
                {
                    break;
                }
            }
            while (stopwatch.ElapsedMilliseconds < abortTimeout);

            if (foundPod == true)
            {
                throw new Exception($"Timeout value of {abortTimeout} milliseconds expired while waiting for Kubernetes deployment '{name}' to scale down.");
            }
        }

        /// <summary>
        /// Waits for the specified <see cref="V1Deployment"/> predicate to become true before returning.
        /// </summary>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to wait for.</param>
        /// <param name="checkInterval">The interval in milliseconds between executions of the predicate.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the predicate hasn't returned true.</param>
        protected async Task WaitForDeploymentPredicateAsync(Predicate<V1Deployment> predicate, Int32 checkInterval, Int32 abortTimeout)
        {
            ThrowExceptionIfIntegerParameterLessThan1(nameof(checkInterval), checkInterval);
            ThrowExceptionIfIntegerParameterLessThan1(nameof(abortTimeout), abortTimeout);

            Boolean predicateReturnValue = false;
            Stopwatch stopwatch = new();
            stopwatch.Start();
            do
            {
                foreach (V1Deployment currentDeployment in await kubernetesClientShim.ListNamespacedDeploymentAsync(kubernetesClient, staticConfiguration.NameSpace))
                {
                    if (predicate.Invoke(currentDeployment) == true)
                    {
                        predicateReturnValue = true;
                        break;
                    }
                }
                if (predicateReturnValue == false)
                {
                    await Task.Delay(checkInterval);
                }
            }
            while (stopwatch.ElapsedMilliseconds < abortTimeout && predicateReturnValue == false);

            if (predicateReturnValue == false)
            {
                throw new DeploymentPredicateWaitTimeoutExpiredException($"Timeout value of {abortTimeout} milliseconds expired while waiting for deployment predicate to return true.", abortTimeout);
            }
        }

        #endregion

        #region Kubernetes Object Templates

        /// <summary>
        /// The base/template for creating <see cref="V1Service"/> objects of type 'ClusterIP'.
        /// </summary>
        protected V1Service ClusterIpServiceTemplate
        {
            get
            {
                V1Service serviceTemplate = ServiceTemplate;
                serviceTemplate.Spec.Type = clusterIpServiceType;

                return serviceTemplate;
            }
        }

        /// <summary>
        /// The base/template for creating <see cref="V1Service"/> objects of type 'ClusterIP'.
        /// </summary>
        protected V1Service LoadBalancerServiceTemplate
        {
            get
            {
                V1Service serviceTemplate = ServiceTemplate;
                serviceTemplate.Spec.Type = loadBalancerServiceType;

                return serviceTemplate;
            }
        }

        /// <summary>
        /// The base/template for creating <see cref="V1Service"/> objects.
        /// </summary>
        protected V1Service ServiceTemplate
        {
            get => new V1Service()
            {
                ApiVersion = $"{V1Service.KubeGroup}/{V1Service.KubeApiVersion}",
                Kind = V1Service.KubeKind,
                Metadata = new V1ObjectMeta(),
                Spec = new V1ServiceSpec
                {
                    Selector = new Dictionary<String, String>(),
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort{
                            Protocol = tcpProtocol
                        }
                    }
                }
            };
        }

        /// <summary>
        /// The base/template for creating <see cref="V1Deployment"/> objects for reader nodes.
        /// </summary>
        protected V1Deployment ReaderNodeDeploymentTemplate
        {
            get => new V1Deployment()
            {
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Kind = V1Deployment.KubeKind,
                Metadata = new V1ObjectMeta(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = staticConfiguration.ReaderNodeConfigurationTemplate.ReplicaCount,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<string, string>()
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Labels = new Dictionary<string, string>()
                        },
                        Spec = new V1PodSpec
                        {
                            TerminationGracePeriodSeconds = staticConfiguration.ReaderNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = staticConfiguration.ReaderNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {                                  
                                        new V1ContainerPort
                                        {
                                            ContainerPort = staticConfiguration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(staticConfiguration.ReaderNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(staticConfiguration.ReaderNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    LivenessProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl, 
                                            Port = staticConfiguration.PodPort
                                        },
                                        PeriodSeconds = staticConfiguration.ReaderNodeConfigurationTemplate.LivenessProbePeriod
                                    }, 
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = staticConfiguration.PodPort
                                        }, 
                                        FailureThreshold = staticConfiguration.ReaderNodeConfigurationTemplate.StartupProbeFailureThreshold, 
                                        PeriodSeconds = staticConfiguration.ReaderNodeConfigurationTemplate.StartupProbePeriod
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// The base/template for creating <see cref="V1Deployment"/> objects for event cache nodes.
        /// </summary>
        protected V1Deployment EventCacheNodeDeploymentTemplate
        {
            get => new V1Deployment()
            {
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Kind = V1Deployment.KubeKind,
                Metadata = new V1ObjectMeta(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<string, string>()
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Labels = new Dictionary<string, string>()
                        },
                        Spec = new V1PodSpec
                        {
                            TerminationGracePeriodSeconds = staticConfiguration.EventCacheNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = staticConfiguration.EventCacheNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = staticConfiguration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(staticConfiguration.EventCacheNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(staticConfiguration.EventCacheNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    }, 
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = staticConfiguration.PodPort
                                        },
                                        FailureThreshold = staticConfiguration.EventCacheNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = staticConfiguration.EventCacheNodeConfigurationTemplate.StartupProbePeriod
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// The base/template for creating <see cref="V1Deployment"/> objects for writer nodes.
        /// </summary>
        protected V1Deployment WriterNodeDeploymentTemplate
        {
            get => new V1Deployment()
            {
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Kind = V1Deployment.KubeKind,
                Metadata = new V1ObjectMeta(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<string, string>()
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Labels = new Dictionary<string, string>()
                        },
                        Spec = new V1PodSpec
                        {
                            Volumes = new List<V1Volume>()
                            { 
                                new V1Volume()
                                {
                                    Name = eventBackupPersistentVolumeName, 
                                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource()
                                    {
                                        ClaimName = staticConfiguration.WriterNodeConfigurationTemplate.PersistentVolumeClaimName
                                    }
                                }
                            }, 
                            TerminationGracePeriodSeconds = staticConfiguration.WriterNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = staticConfiguration.WriterNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = staticConfiguration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(staticConfiguration.WriterNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(staticConfiguration.WriterNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = staticConfiguration.PodPort
                                        },
                                        FailureThreshold = staticConfiguration.WriterNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = staticConfiguration.WriterNodeConfigurationTemplate.StartupProbePeriod
                                    }, 
                                    VolumeMounts = new List<V1VolumeMount>()
                                    {
                                        new V1VolumeMount()
                                        {
                                            MountPath = eventBackupVolumeMountPath, 
                                            Name = eventBackupPersistentVolumeName
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// The base/template for creating <see cref="V1Deployment"/> objects for distributed operation coordinator nodes.
        /// </summary>
        protected V1Deployment DistributedOperationCoordinatorNodeDeploymentTemplate
        {
            get => new V1Deployment()
            {
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Kind = V1Deployment.KubeKind,
                Metadata = new V1ObjectMeta(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.ReplicaCount,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<string, string>()
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Labels = new Dictionary<string, string>()
                        },
                        Spec = new V1PodSpec
                        {
                            TerminationGracePeriodSeconds = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = staticConfiguration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = staticConfiguration.PodPort
                                        },
                                        FailureThreshold = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbePeriod
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// The base/template for creating <see cref="V1Deployment"/> objects for distributed operation router nodes.
        /// </summary>
        protected V1Deployment DistributedOperationRouterNodeDeploymentTemplate
        {
            get => new V1Deployment()
            {
                ApiVersion = $"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}",
                Kind = V1Deployment.KubeKind,
                Metadata = new V1ObjectMeta(),
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector()
                    {
                        MatchLabels = new Dictionary<string, string>()
                    },
                    Template = new V1PodTemplateSpec()
                    {
                        Metadata = new V1ObjectMeta()
                        {
                            Labels = new Dictionary<string, string>()
                        },
                        Spec = new V1PodSpec
                        {
                            TerminationGracePeriodSeconds = staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = staticConfiguration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = staticConfiguration.PodPort
                                        },
                                        FailureThreshold = staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.StartupProbePeriod
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the KubernetesDistributedAccessManagerInstanceManager.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~KubernetesDistributedAccessManagerInstanceManager()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    if (kubernetesClient != null)
                    {
                        kubernetesClient.Dispose();
                    }
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
