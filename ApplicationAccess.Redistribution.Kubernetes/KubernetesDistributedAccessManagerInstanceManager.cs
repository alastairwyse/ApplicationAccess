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
using System.Threading.Tasks;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.LaunchPreparer;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution;
using ApplicationAccess.Redistribution.Kubernetes.Metrics;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Kubernetes.Validation;
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
        // Regarding the 'shardConfigurationSetPersisterCreationFunction' parameter... elsewhere in the solution a factory pattern is used to construct instances, 
        //   and following this trend, I would pass a factory for IShardConfigurationSetPersister instances into the constructor of this class.  In fact we already
        //   have such a factory defined in ApplicationAccess.Hosting.Persistence.Sql.SqlShardConfigurationSetPersisterFactory.  The problem with reusing this is as
        //   follows...
        //   * SqlShardConfigurationSetPersisterFactory doesn't implement an interface.  I could create an interface for it, but its GetPersister() method takes a
        //     subclass of ApplicationAccess.Hosting.Models.SqlDatabaseConnectionParametersBase.  This is ofcourse SQL specific, but in this class I'm trying to keep
        //     things more generic than SQL, in preparation for implementation/support of MongoDB.
        //   * SqlDatabaseConnectionParametersBase defines connection parameters as either a connection string, OR separate user/password fields, similar to how
        //     connection settings are defined in appsettings.json (in fact SqlDatabaseConnectionParametersBase is is primarily used by being constructed from
        //     appsettings config).  I could have SqlDatabaseConnectionParametersBase derive from ConnectionStringPersistentStorageLoginCredentials (which
        //     implements IPersistentStorageLoginCredentials), BUT ConnectionStringPersistentStorageLoginCredentials only uses a connection string to connect, and 
        //     throws an exception on construction if that connection string is null... it doesn't have the either/or optionality described above that 
        //     SqlDatabaseConnectionParametersBase has.  Hence making SqlDatabaseConnectionParametersBase derive from ConnectionStringPersistentStorageLoginCredentials 
        //     would break the intended use case of ConnectionStringPersistentStorageLoginCredentials... basically following the 'is a / has a' principle for inheritance,
        //     SqlDatabaseConnectionParametersBase is NOT (strictly speaking) a ConnectionStringPersistentStorageLoginCredentials, so we should not be using inheritance there.
        //   * Hence implemented approach is to use Func parameters (e.g. 'shardConfigurationSetPersisterCreationFunction' on the constructor) to create the 
        //     IShardConfigurationSetPersister instance instead of a factory class.

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
        protected const String shardConfigurationPersistentStorageInstanceName = "shard_configuration";
        protected const String supersededPersisentStorageInstanceNamePostfix = "_old";
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
        protected const String appsettingsShardConfigurationRefreshPropertyName = "ShardConfigurationRefresh";
        protected const String appsettingsRefreshIntervalPropertyName = "RefreshInterval";
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
        /// <summary>The number of random characters to include in the names of temporary persistent storage instances.</summary>
        protected const Int32 persistentStorageNameRandomComponentLength = 12;

        /// <summary>Static configuration for the instance manager.</summary>
        protected KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration;
        /// <summary>Configuration for the distributed AccessManager instance.</summary>
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration;
        /// <summary>Configuration for the shard groups managing users in the distributed AccessManager instance.</summary>
        protected KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials> userShardGroupConfigurationSet;
        /// <summary>Configuration for the shard groups managing group to group mappings in the distributed AccessManager instance.</summary>
        protected KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials> groupToGroupMappingShardGroupConfigurationSet;
        /// <summary>Configuration for the shard groups managing groups in the distributed AccessManager instance.</summary>
        protected KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials> groupShardGroupConfigurationSet;
        /// <summary>The client to connect to Kubernetes.</summary>
        protected k8s.Kubernetes kubernetesClient;
        /// <summary>Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to the Kubernetes client class.</summary>
        protected IKubernetesClientShim kubernetesClientShim;
        /// <summary>Used to manage instances of persistent storage used by the distributed AccessManager implementation.</summary>
        protected IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager;
        /// <summary>Random name generator for persistent storage instances.</summary>
        protected IPersistentStorageInstanceRandomNameGenerator persistentStorageInstanceRandomNameGenerator;
        /// <summary>Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</summary>
        protected IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer;
        /// <summary>A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</summary>
        protected Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction;
        /// <summary>Used to write shard configuration to persistent storage.</summary>
        protected IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> shardConfigurationSetPersister;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// URL for the distributed operation router component used for shard group splitting.
        /// </summary>
        /// <remarks>This property is included in classes' instance configuration, and is set when the <see cref="KubernetesDistributedAccessManagerInstanceManager{TPersistentStorageCredentials}.CreateDistributedOperationRouterLoadBalancerServiceAsync(ushort)">CreateDistributedOperationRouterLoadBalancerServiceAsync()</see> method is called.  However it may be initially set to a URL which is internal to the Kubernetes cluster host (e.g. when running in Minikube) and require a proxy or similar to expose it publically.  This setter allows the value to be overridden with the publically accessible URL.</remarks>
        public Uri DistributedOperationRouterUrl
        {
            set 
            {
                if (instanceConfiguration.DistributedOperationRouterUrl == null)
                    throw new InvalidOperationException($"Property '{nameof(DistributedOperationRouterUrl)}' cannot be set if it was not previously created by calling method {nameof(CreateDistributedOperationRouterLoadBalancerServiceAsync)}().");

                instanceConfiguration.DistributedOperationRouterUrl = value;
            }
        }

        /// <summary>
        /// URL for a first writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        /// <remarks>This property is included in classes' instance configuration, and is set when the <see cref="KubernetesDistributedAccessManagerInstanceManager{TPersistentStorageCredentials}.CreateWriter1LoadBalancerServiceAsync(ushort)">CreateWriter1LoadBalancerServiceAsync()</see> method is called.  However it may be initially set to a URL which is internal to the Kubernetes cluster host (e.g. when running in Minikube) and require a proxy or similar to expose it publically.  This setter allows the value to be overridden with the publically accessible URL.</remarks>
        public Uri Writer1Url
        {
            set
            {
                if (instanceConfiguration.Writer1Url == null)
                    throw new InvalidOperationException($"Property '{nameof(Writer1Url)}' cannot be set if it was not previously created by calling method {nameof(CreateWriter1LoadBalancerServiceAsync)}().");

                instanceConfiguration.Writer1Url = value;
            }
        }

        /// <summary>
        /// URL for a second writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        /// <remarks>This property is included in classes' instance configuration, and is set when the <see cref="KubernetesDistributedAccessManagerInstanceManager{TPersistentStorageCredentials}.CreateWriter2LoadBalancerServiceAsync(ushort)">CreateWriter2LoadBalancerServiceAsync()</see> method is called.  However it may be initially set to a URL which is internal to the Kubernetes cluster host (e.g. when running in Minikube) and require a proxy or similar to expose it publically.  This setter allows the value to be overridden with the publically accessible URL.</remarks>
        public Uri Writer2Url
        {
            set
            {
                if (instanceConfiguration.Writer2Url == null)
                    throw new InvalidOperationException($"Property '{nameof(Writer2Url)}' cannot be set if it was not previously created by calling method {nameof(CreateWriter2LoadBalancerServiceAsync)}().");

                instanceConfiguration.Writer2Url = value;
            }
        }

        /// <summary>
        /// URL for the distributed operation coordinator component.
        /// </summary>
        /// <remarks>This property is included in classes' instance configuration, and is set when the <see cref="KubernetesDistributedAccessManagerInstanceManager{TPersistentStorageCredentials}.CreateDistributedAccessManagerInstanceAsync(IList{ShardGroupConfiguration{TPersistentStorageCredentials}}, IList{ShardGroupConfiguration{TPersistentStorageCredentials}}, IList{ShardGroupConfiguration{TPersistentStorageCredentials}})">CreateDistributedAccessManagerInstanceAsync()</see> method is called.  However it may be initially set to a URL which is internal to the Kubernetes cluster host (e.g. when running in Minikube) and require a proxy or similar to expose it publically.  This setter allows the value to be overridden with the publically accessible URL.</remarks>
        public Uri DistributedOperationCoordinatorUrl
        {
            set
            {
                if (instanceConfiguration.DistributedOperationCoordinatorUrl == null)
                    throw new InvalidOperationException($"Property '{nameof(DistributedOperationCoordinatorUrl)}' cannot be set if it was not previously created by calling method {nameof(CreateDistributedAccessManagerInstanceAsync)}().");

                instanceConfiguration.DistributedOperationCoordinatorUrl = value;
            }
        }

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
        /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration, 
            IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction, 
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            kubernetesClient = new k8s.Kubernetes(clientConfiguration);
            InitializeFields(staticConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration, 
            IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            kubernetesClient = new k8s.Kubernetes(clientConfiguration);
            InitializeFields(staticConfiguration, instanceConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="kubernetesConfigurationFilePath">The full path to the configuration file to use to connect to the Kubernetes cluster(s).</param>
        /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration, 
            String kubernetesConfigurationFilePath, 
            IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationFilePath);
            kubernetesClient = new k8s.Kubernetes(clientConfiguration);
            InitializeFields(staticConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="kubernetesConfigurationFilePath">The full path to the configuration file to use to connect to the Kubernetes cluster(s).</param>
        /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration,
            String kubernetesConfigurationFilePath,
            IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationFilePath);
            kubernetesClient = new k8s.Kubernetes(clientConfiguration);
            InitializeFields(staticConfiguration, instanceConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="persistentStorageInstanceRandomNameGenerator">Random name generator for persistent storage instances.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
        /// <param name="kubernetesClientShim">A mock <see cref="IKubernetesClientShim"/>.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration,
            IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager,
            IPersistentStorageInstanceRandomNameGenerator persistentStorageInstanceRandomNameGenerator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
            IKubernetesClientShim kubernetesClientShim, 
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            kubernetesClient = null;
            InitializeFields(staticConfiguration, instanceConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger);
            this.persistentStorageInstanceRandomNameGenerator = persistentStorageInstanceRandomNameGenerator;
            this.kubernetesClientShim = kubernetesClientShim;
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access the distributed router component used for shard group splitting, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the load balancer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        /// <remarks>This method should be called before creating a distributed AccessManager instance.  Some Kubernetes hosting platforms (e.g. Minikube) require additional actions outside of the cluster to allow Kubernetes services to be accessed from outside of the host machine (e.g. in the case if Minikube the IP address and port of the load balancer service must exposed outside the machine using 'simpleproxy' or a similar tool).  Hence this method can be called, and then any required additional actions be performed.</remarks>
        public async Task<IPAddress> CreateDistributedOperationRouterLoadBalancerServiceAsync(UInt16 port)
        {
            if (instanceConfiguration.DistributedOperationRouterUrl != null)
                throw new InvalidOperationException("A load balancer service for the distributed operation router has already been created.");

            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating load balancer service for distributed operation router on port {port} in namespace '{nameSpace}'...");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed creating load balancer service.");

            return returnIpAddress;
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access a first writer component which is part of a shard group undergoing a split or merge operation, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the writer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        /// <remarks>This method should be called before creating a distributed AccessManager instance.  Some Kubernetes hosting platforms (e.g. Minikube) require additional actions outside of the cluster to allow Kubernetes services to be accessed from outside of the host machine (e.g. in the case if Minikube the IP address and port of the load balancer service must exposed outside the machine using 'simpleproxy' or a similar tool).  Hence this method can be called, and then any required additional actions be performed.</remarks>
        public async Task<IPAddress> CreateWriter1LoadBalancerServiceAsync(UInt16 port)
        {
            if (instanceConfiguration.Writer1Url != null)
                throw new InvalidOperationException("A load balancer service for the first writer component has already been created.");

            IPAddress writer1IpAddress = await CreateWriterLoadBalancerServiceAsync(GenerateWriter1LoadBalancerServiceName(), port);
            instanceConfiguration.Writer1Url = new($"{GetLoadBalancerServiceScheme()}://{writer1IpAddress.ToString()}:{port}");

            return writer1IpAddress;
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access a second writer component which is part of a shard group undergoing a split or merge operation, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the writer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        /// <remarks>This method should be called before creating a distributed AccessManager instance.  Some Kubernetes hosting platforms (e.g. Minikube) require additional actions outside of the cluster to allow Kubernetes services to be accessed from outside of the host machine (e.g. in the case if Minikube the IP address and port of the load balancer service must exposed outside the machine using 'simpleproxy' or a similar tool).  Hence this method can be called, and then any required additional actions be performed.</remarks>
        public async Task<IPAddress> CreateWriter2LoadBalancerServiceAsync(UInt16 port)
        {
            if (instanceConfiguration.Writer2Url != null)
                throw new InvalidOperationException("A load balancer service for the second writer component has already been created.");

            IPAddress writer2IpAddress = await CreateWriterLoadBalancerServiceAsync(GenerateWriter2LoadBalancerServiceName(), port);
            instanceConfiguration.Writer2Url = new($"{GetLoadBalancerServiceScheme()}://{writer2IpAddress.ToString()}:{port}");

            return writer2IpAddress;
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
                throw new InvalidOperationException($"A distributed operation router load balancer service must be created via method {nameof(CreateDistributedOperationRouterLoadBalancerServiceAsync)}() before creating a distributed AccessManager instance.");
            if (instanceConfiguration.Writer1Url == null)
                throw new InvalidOperationException($"A first writer load balancer service must be created via method {nameof(CreateWriter1LoadBalancerServiceAsync)}() before creating a distributed AccessManager instance.");
            if (instanceConfiguration.Writer2Url == null)
                throw new InvalidOperationException($"A second writer load balancer service must be created via method {nameof(CreateWriter2LoadBalancerServiceAsync)}() before creating a distributed AccessManager instance.");
            if (userShardGroupConfigurationSet.Items.Count != 0 || groupToGroupMappingShardGroupConfigurationSet.Items.Count != 0 || groupShardGroupConfigurationSet.Items.Count != 0)
                throw new InvalidOperationException($"A distributed AccessManager instance has already been created.");

            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating distributed AccessManager instance in namespace '{nameSpace}'...");
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
                PopulateShardGroupConfigurationSetFields
                (
                    userShardGroupConfiguration,
                    groupToGroupMappingShardGroupConfiguration,
                    groupShardGroupConfiguration
                );

                // Create persistent storage for the shard configuration
                String persistentStorageInstanceName = shardConfigurationPersistentStorageInstanceName;
                if (staticConfiguration.PersistentStorageInstanceNamePrefix != "")
                {
                    persistentStorageInstanceName = $"{staticConfiguration.PersistentStorageInstanceNamePrefix}_{persistentStorageInstanceName}";
                }
                if (instanceConfiguration.ShardConfigurationPersistentStorageCredentials == null)
                {
                    try
                    {
                        TPersistentStorageCredentials credentials = persistentStorageManager.CreateAccessManagerConfigurationPersistentStorage(persistentStorageInstanceName);
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
                    userShardGroupConfigurationSet.Items,
                    groupToGroupMappingShardGroupConfigurationSet.Items,
                    groupShardGroupConfigurationSet.Items
                );
                ConstructInstance
                (   
                    () => { shardConfigurationSetPersister = shardConfigurationSetPersisterCreationFunction(instanceConfiguration.ShardConfigurationPersistentStorageCredentials); }, 
                    "shardConfigurationSetPersister"
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
                IPAddress distributedOperationCoordinatorIpAddress = await CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(staticConfiguration.ExternalPort);
                Uri distributedOperationCoordinatorUrl = new($"{GetLoadBalancerServiceScheme()}://{distributedOperationCoordinatorIpAddress.ToString()}:{staticConfiguration.ExternalPort}");
                instanceConfiguration.DistributedOperationCoordinatorUrl = distributedOperationCoordinatorUrl;
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new DistributedAccessManagerInstanceCreateTime());
                throw;
            }

            // Copy the contents of the *ShardGroupConfigurationSet fields to the instance configuration
            CopyShardConfigurationSetsToInstanceConfiguration();

            metricLogger.End(beginId, new DistributedAccessManagerInstanceCreateTime());
            metricLogger.Increment(new DistributedAccessManagerInstanceCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
        }

        /// <inheritdoc/>>
        public async Task DeleteDistributedAccessManagerInstanceAsync(Boolean deletePersistentStorageInstances)
        {
            if (userShardGroupConfigurationSet.Items.Count == 0)
                throw new InvalidOperationException($"A distributed AccessManager instance has not been created.");

            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting distributed AccessManager instance in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new DistributedAccessManagerInstanceDeleteTime());

            // Scale down and delete the shard groups
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Scaling down and deleting shard groups...");
            async Task ScaleDownAndDeleteShardGroup(DataElement dataElement, IEnumerable<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> shardGroupConfigurationItems)
            {
                foreach (KubernetesShardGroupConfiguration<TPersistentStorageCredentials> currentShardGroupConfiguration in shardGroupConfigurationItems)
                {
                    try
                    {
                        await ScaleDownShardGroupAsync(dataElement, currentShardGroupConfiguration.HashRangeStart);
                    }
                    catch (Exception e)
                    {
                        metricLogger.CancelBegin(beginId, new DistributedAccessManagerInstanceDeleteTime());
                        throw new Exception($"Error scaling shard group with data element '{dataElement}' and hash range start value {currentShardGroupConfiguration.HashRangeStart}.", e);
                    }
                    try
                    {
                        await DeleteShardGroupAsync(dataElement, currentShardGroupConfiguration.HashRangeStart, deletePersistentStorageInstances);
                    }
                    catch
                    {
                        metricLogger.CancelBegin(beginId, new DistributedAccessManagerInstanceDeleteTime());
                        throw;
                    }
                }
            }
            await ScaleDownAndDeleteShardGroup(DataElement.User, userShardGroupConfigurationSet.Items);
            await ScaleDownAndDeleteShardGroup(DataElement.GroupToGroupMapping, groupToGroupMappingShardGroupConfigurationSet.Items);
            await ScaleDownAndDeleteShardGroup(DataElement.Group, groupShardGroupConfigurationSet.Items);
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed scaling down and deleting shard groups.");

            // Delete distributed operation coordinator 
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting distributed operation coordinator node...");
            try
            {
                await ScaleDownAndDeleteDeploymentAsync(distributedOperationCoordinatorObjectNamePrefix, staticConfiguration.DeploymentWaitPollingInterval, staticConfiguration.ServiceAvailabilityWaitAbortTimeout);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new DistributedAccessManagerInstanceDeleteTime());
                throw;
            }
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting distributed operation coordinator node.");

            // Delete the router Cluster IP service
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting distributed operation coordinator node load balancer service...");
            try
            {
                await DeleteServiceAsync($"{distributedOperationCoordinatorObjectNamePrefix}{externalServiceNamePostfix}");
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new DistributedAccessManagerInstanceDeleteTime());
                throw;
            }
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting distributed operation coordinator node load balancer service.");

            // Delete persistent storage for the shard configuration
            if (deletePersistentStorageInstances == true)
            {
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting shard configuration persistent storage instance...");
                String persistentStorageInstanceName = shardConfigurationPersistentStorageInstanceName;
                if (staticConfiguration.PersistentStorageInstanceNamePrefix != "")
                {
                    persistentStorageInstanceName = $"{staticConfiguration.PersistentStorageInstanceNamePrefix}_{persistentStorageInstanceName}";
                }
                try
                {
                    persistentStorageManager.DeletePersistentStorage(persistentStorageInstanceName);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new DistributedAccessManagerInstanceDeleteTime());
                    throw new Exception($"Error deleting persistent storage instance '{persistentStorageInstanceName}'.", e);
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting shard configuration persistent storage instance.");
            }

            // Update the *ShardGroupConfigurationSet fields and the instance configuration
            userShardGroupConfigurationSet.Clear();
            groupToGroupMappingShardGroupConfigurationSet.Clear();
            groupShardGroupConfigurationSet.Clear();
            instanceConfiguration.ShardConfigurationPersistentStorageCredentials = null;
            instanceConfiguration.UserShardGroupConfiguration.Clear();
            instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Clear();
            instanceConfiguration.GroupShardGroupConfiguration.Clear();
            instanceConfiguration.DistributedOperationCoordinatorUrl = null;

            metricLogger.End(beginId, new DistributedAccessManagerInstanceDeleteTime());
            metricLogger.Increment(new DistributedAccessManagerInstanceDeleted());
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed deleting distributed AccessManager instance.");
        }

        /// <inheritdoc/>>
        public async Task SplitShardGroupAsync
        (
            DataElement dataElement,
            Int32 hashRangeStart,
            Int32 splitHashRangeStart,
            Int32 splitHashRangeEnd,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBatchReader> sourceShardGroupEventReaderCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> targetShardGroupEventPersisterCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventDeleter> sourceShardGroupEventDeleterCreationFunction,
            Func<Uri, IDistributedAccessManagerOperationRouter> operationRouterCreationFunction,
            Func<Uri, IDistributedAccessManagerWriterAdministrator> sourceShardGroupWriterAdministratorCreationFunction,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        )
        {
            await SplitShardGroupAsync
            (
                dataElement, 
                hashRangeStart, 
                splitHashRangeStart, 
                splitHashRangeEnd,
                sourceShardGroupEventReaderCreationFunction,
                targetShardGroupEventPersisterCreationFunction,
                sourceShardGroupEventDeleterCreationFunction,
                operationRouterCreationFunction,
                sourceShardGroupWriterAdministratorCreationFunction,
                eventBatchSize,
                sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                sourceWriterNodeOperationsCompleteCheckRetryInterval,
                new DistributedAccessManagerShardGroupSplitter(logger, metricLogger)
            );
        }

        /// <inheritdoc/>>
        public async Task MergeShardGroupsAsync
        (
            DataElement dataElement,
            Int32 sourceShardGroup1HashRangeStart,
            Int32 sourceShardGroup2HashRangeStart,
            Int32 sourceShardGroup2HashRangeEnd,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBatchReader> sourceShardGroupEventReaderCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> targetShardGroupEventPersisterCreationFunction,
            Func<Uri, IDistributedAccessManagerOperationRouter> operationRouterCreationFunction,
            Func<Uri, IDistributedAccessManagerWriterAdministrator> sourceShardGroupWriterAdministratorCreationFunction,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval
        )
        {
            await MergeShardGroupsAsync
            (
                dataElement,
                sourceShardGroup1HashRangeStart,
                sourceShardGroup2HashRangeStart,
                sourceShardGroup2HashRangeEnd,
                sourceShardGroupEventReaderCreationFunction,
                targetShardGroupEventPersisterCreationFunction,
                operationRouterCreationFunction,
                sourceShardGroupWriterAdministratorCreationFunction,
                eventBatchSize,
                sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                sourceWriterNodeOperationsCompleteCheckRetryInterval, 
                new DistributedAccessManagerShardGroupMerger(logger, metricLogger)
            );
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes fields of the class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        protected void InitializeFields
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration, 
            IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            var staticConfigurationValidator = new KubernetesDistributedAccessManagerInstanceManagerStaticConfigurationValidator();
            try
            {
                staticConfigurationValidator.Validate(staticConfiguration);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Parameter '{nameof(staticConfiguration)}' failed to validate.", nameof(staticConfiguration), e);
            }
            this.staticConfiguration = staticConfiguration;
            instanceConfiguration = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials>();
            instanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>();
            instanceConfiguration.GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>();
            instanceConfiguration.GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>();
            userShardGroupConfigurationSet = new KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials>();
            groupToGroupMappingShardGroupConfigurationSet = new KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials>();
            groupShardGroupConfigurationSet = new KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials>();
            this.persistentStorageManager = persistentStorageManager;
            this.credentialsAppSettingsConfigurer = credentialsAppSettingsConfigurer;
            this.shardConfigurationSetPersisterCreationFunction = shardConfigurationSetPersisterCreationFunction;
            this.shardConfigurationSetPersister = null;
            persistentStorageInstanceRandomNameGenerator = new DefaultPersistentStorageInstanceRandomNameGenerator(persistentStorageNameRandomComponentLength);
            kubernetesClientShim = new DefaultKubernetesClientShim();
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initializes fields of the class.
        /// </summary>
        /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
        /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
        /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        protected void InitializeFields
        (
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TPersistentStorageCredentials> instanceConfiguration,
            IDistributedAccessManagerPersistentStorageManager<TPersistentStorageCredentials> persistentStorageManager,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            Func<TPersistentStorageCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            InitializeFields(staticConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger);
            var instanceConfigurationValidator = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfigurationValidator<TPersistentStorageCredentials>();
            try
            {
                instanceConfigurationValidator.Validate(instanceConfiguration);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Parameter '{nameof(instanceConfiguration)}' failed to validate.", nameof(instanceConfiguration), e);
            }
            this.instanceConfiguration = instanceConfiguration;
            if (instanceConfiguration.UserShardGroupConfiguration != null)
            {
                userShardGroupConfigurationSet.AddRange(instanceConfiguration.UserShardGroupConfiguration);
                this.instanceConfiguration.UserShardGroupConfiguration = userShardGroupConfigurationSet.Items;
            }
            if (instanceConfiguration.GroupToGroupMappingShardGroupConfiguration != null)
            {
                groupToGroupMappingShardGroupConfigurationSet.AddRange(instanceConfiguration.GroupToGroupMappingShardGroupConfiguration);
                this.instanceConfiguration.GroupToGroupMappingShardGroupConfiguration = groupToGroupMappingShardGroupConfigurationSet.Items;
            }
            if (instanceConfiguration.GroupShardGroupConfiguration != null)
            {
                groupShardGroupConfigurationSet.AddRange(instanceConfiguration.GroupShardGroupConfiguration);
                this.instanceConfiguration.GroupShardGroupConfiguration = groupShardGroupConfigurationSet.Items;
            }
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access a writer component which is part of a shard group undergoing a split operation, from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="appLabelValue">The name of the pod/deployment targetted by the service.</param>
        /// <param name="port">The external port to expose the writer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        protected async Task<IPAddress> CreateWriterLoadBalancerServiceAsync(String appLabelValue, UInt16 port)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating load balancer service '{appLabelValue}{externalServiceNamePostfix}' for writer on port {port} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new LoadBalancerServiceCreateTime());

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

            metricLogger.End(beginId, new LoadBalancerServiceCreateTime());
            metricLogger.Increment(new LoadBalancerServiceCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed creating load balancer service.");

            return returnIpAddress;
        }

        /// <summary>
        /// Splits a shard group in the distributed AccessManager instance, by moving elements whose hash codes fall within a specified range to a new shard group.
        /// </summary>
        /// <param name="dataElement">The data element of the shard group to split.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group to split.</param>
        /// <param name="splitHashRangeStart">The first (inclusive) in the range of hash codes to move to the new shard group.</param>
        /// <param name="splitHashRangeEnd">The last (inclusive) in the range of hash codes to move to the new shard group.</param>
        /// <param name="sourceShardGroupEventReaderCreationFunction">A function used to create a reader used to read events from the source shard group persistent storage instance.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerTemporalEventBatchReader"/> instance.</param>
        /// <param name="targetShardGroupEventPersisterCreationFunction">A function used to create a persister used to write events to the target shard group persistent storage instance.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerIdempotentTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance.</param>
        /// <param name="sourceShardGroupEventDeleterCreationFunction">A function used to create a deleter used to delete events from the source shard group persistent storage instance.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerTemporalEventDeleter"/> instance.</param>
        /// <param name="operationRouterCreationFunction">A function used to create a client used control the router which directs operations between the source and target shard groups.  Accepts a <see cref="Uri"/> and returns an <see cref="IDistributedAccessManagerOperationRouter"/> instance.</param>
        /// <param name="sourceShardGroupWriterAdministratorCreationFunction">A function used to create a client used control the writer node in the source shard group.  Accepts a <see cref="Uri"/> and returns an <see cref="IDistributedAccessManagerWriterAdministrator"/> instance.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard group in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard group, before copying of the final batch of events (event copy will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        /// <param name="shardGroupSplitter">The <see cref="IDistributedAccessManagerShardGroupSplitter"/> implementation used to perform the splitting.</param>
        protected async Task SplitShardGroupAsync
        (
            DataElement dataElement, 
            Int32 hashRangeStart, 
            Int32 splitHashRangeStart, 
            Int32 splitHashRangeEnd, 
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBatchReader> sourceShardGroupEventReaderCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> targetShardGroupEventPersisterCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventDeleter> sourceShardGroupEventDeleterCreationFunction,
            Func<Uri, IDistributedAccessManagerOperationRouter> operationRouterCreationFunction,
            Func<Uri, IDistributedAccessManagerWriterAdministrator> sourceShardGroupWriterAdministratorCreationFunction,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval, 
            IDistributedAccessManagerShardGroupSplitter shardGroupSplitter
        )
        {
            if (userShardGroupConfigurationSet.Items.Count == 0)
                throw new InvalidOperationException($"A distributed AccessManager instance has not been created.");
            if (dataElement == DataElement.GroupToGroupMapping)
                throw new ArgumentException($"Shard group splitting is not supported for '{typeof(DataElement).Name}' '{dataElement}'.", nameof(dataElement));
            if (splitHashRangeEnd < splitHashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(splitHashRangeEnd), $"Parameter '{nameof(splitHashRangeEnd)}' with value {splitHashRangeEnd} must be greater than or equal to parameter '{nameof(splitHashRangeStart)}' with value {splitHashRangeStart}.");
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> shardGroupConfiguration = GetShardGroupConfiguration(GetShardGroupConfigurationList(dataElement), hashRangeStart);
            if (shardGroupConfiguration == null)
                throw new ArgumentException($"Parameter '{nameof(hashRangeStart)}' with value {hashRangeStart} contains an invalid hash range start value for '{dataElement}' shard groups.", nameof(hashRangeStart));
            if (splitHashRangeStart <= hashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(splitHashRangeStart), $"Parameter '{nameof(splitHashRangeStart)}' with value {splitHashRangeStart} must be greater than parameter '{nameof(hashRangeStart)}' with value {hashRangeStart}.");
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> nextShardGroupConfiguration = null;
            try
            {
                nextShardGroupConfiguration = GetNextShardGroupConfiguration(GetShardGroupConfigurationList(dataElement), hashRangeStart);
            }
            catch
            {
                if (splitHashRangeEnd != Int32.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(splitHashRangeEnd), $"Parameter '{nameof(splitHashRangeEnd)}' with value {splitHashRangeEnd} contains a different hash range end value to the hash range end value {Int32.MaxValue} of the shard group being split.");
            }
            if (nextShardGroupConfiguration != null)
            {
                if (splitHashRangeStart >= nextShardGroupConfiguration.HashRangeStart)
                    throw new ArgumentOutOfRangeException(nameof(splitHashRangeStart), $"Parameter '{nameof(splitHashRangeStart)}' with value {splitHashRangeStart} must be less than the hash range start value {nextShardGroupConfiguration.HashRangeStart} of the next sequential shard group.");
                if (splitHashRangeEnd != nextShardGroupConfiguration.HashRangeStart - 1)
                    throw new ArgumentOutOfRangeException(nameof(splitHashRangeEnd), $"Parameter '{nameof(splitHashRangeEnd)}' with value {splitHashRangeEnd} contains a different hash range end value to the hash range end value {nextShardGroupConfiguration.HashRangeStart - 1} of the shard group being split.");
            }

            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Splitting {dataElement} shard group with hash range start value {hashRangeStart} to new shard group at hash range start value {splitHashRangeStart}...");
            Guid beginId = metricLogger.Begin(new ShardGroupSplitTime());

            // Create the target persistent storage instance
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating persistent storage instance for data element '{dataElement.ToString()}' and hash range start value {splitHashRangeStart}...");
            String targetPersistentStorageInstanceName = GeneratePersistentStorageInstanceName(dataElement, splitHashRangeStart);
            Guid storageBeginId = metricLogger.Begin(new PersistentStorageInstanceCreateTime());
            TPersistentStorageCredentials targetPersistentStorageCredentials;
            try
            {
                targetPersistentStorageCredentials = persistentStorageManager.CreateAccessManagerPersistentStorage(targetPersistentStorageInstanceName);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(storageBeginId, new PersistentStorageInstanceCreateTime());
                metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                throw new Exception($"Error creating persistent storage instance for data element type '{dataElement}' and hash range start value {splitHashRangeStart}.", e);
            }
            metricLogger.End(storageBeginId, new PersistentStorageInstanceCreateTime());
            metricLogger.Increment(new PersistentStorageInstanceCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating persistent storage instance.");

            // Create persisters and clients
            IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader = null;
            IAccessManagerTemporalEventBulkPersister<String, String, String, String> targetShardGroupEventPersister = null;
            IAccessManagerTemporalEventDeleter sourceShardGroupEventDeleter = null;
            IDistributedAccessManagerOperationRouter operationRouter = null;
            IDistributedAccessManagerWriterAdministrator sourceShardGroupWriterAdministrator = null;
            try
            {
                ConstructInstance
                (
                    () => { sourceShardGroupEventReader = sourceShardGroupEventReaderCreationFunction(shardGroupConfiguration.PersistentStorageCredentials); },
                    nameof(sourceShardGroupEventReader)
                );
                ConstructInstance
                (
                    () => { targetShardGroupEventPersister = targetShardGroupEventPersisterCreationFunction(targetPersistentStorageCredentials); },
                    nameof(targetShardGroupEventPersister)
                );
                ConstructInstance
                (
                    () => { sourceShardGroupEventDeleter = sourceShardGroupEventDeleterCreationFunction(shardGroupConfiguration.PersistentStorageCredentials); },
                    nameof(sourceShardGroupEventDeleter)
                );
                ConstructInstance
                (
                    () => { operationRouter = operationRouterCreationFunction(instanceConfiguration.DistributedOperationRouterUrl); },
                    nameof(operationRouter)
                );
                ConstructInstance
                (
                    () => { sourceShardGroupWriterAdministrator = sourceShardGroupWriterAdministratorCreationFunction(instanceConfiguration.Writer1Url); },
                    nameof(sourceShardGroupWriterAdministrator)
                );
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                throw;
            }

            try
            {
                // Create new/target shard group configuration
                Uri targetReaderNodeServiceUrl = GenerateNodeServiceUrl(dataElement, NodeType.Reader, splitHashRangeStart);
                Uri targetWriterNodeServiceUrl = GenerateNodeServiceUrl(dataElement, NodeType.Writer, splitHashRangeStart);

                // Create distributed operation router
                try
                {
                    await CreateDistributedOperationRouterNodeAsync
                    (
                        dataElement,
                        shardGroupConfiguration.ReaderNodeClientConfiguration.BaseUrl,
                        shardGroupConfiguration.WriterNodeClientConfiguration.BaseUrl,
                        hashRangeStart,
                        splitHashRangeStart - 1,
                        targetReaderNodeServiceUrl,
                        targetWriterNodeServiceUrl,
                        splitHashRangeStart,
                        splitHashRangeEnd,
                        false
                    );
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }

                // Update writer load balancer service to target source shard group writer node
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating writer load balancer service to target source shard group writer node...");
                String writerLoadBalancerServiceName = $"{GenerateWriter1LoadBalancerServiceName()}{externalServiceNamePostfix}";
                String sourceWriterNodeIdentifier = GenerateNodeIdentifier(dataElement, NodeType.Writer, hashRangeStart);
                try
                {
                    await UpdateServiceAsync(writerLoadBalancerServiceName, sourceWriterNodeIdentifier);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating writer load balancer service.");

                // Update the shard group configuration to redirect to the router
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating shard group configuration to redirect to router...");
                Uri routerInternalUrl = new($"http://{distributedOperationRouterObjectNamePrefix}{serviceNamePostfix}:{staticConfiguration.PodPort}");
                AccessManagerRestClientConfiguration routerClientConfiguration = new(routerInternalUrl);
                List<HashRangeStartAndClientConfigurations> configurationUpdates = new()
                {
                    new HashRangeStartAndClientConfigurations
                    {
                        HashRangeStart = hashRangeStart,
                        ReaderNodeClientConfiguration = routerClientConfiguration,
                        WriterNodeClientConfiguration = routerClientConfiguration
                    }
                };
                List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> configurationAdditions = new()
                {
                    new KubernetesShardGroupConfiguration<TPersistentStorageCredentials>
                    (
                        splitHashRangeStart,
                        targetPersistentStorageCredentials,
                        routerClientConfiguration,
                        routerClientConfiguration
                    )
                };
                try
                {
                    UpdateAndPersistShardConfigurationSets(dataElement, configurationUpdates, configurationAdditions, new List<Int32>());
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating shard group configuration.");

                // Wait for the Updated the shard group configuration to be read by the operation coordinator nodes
                Int32 configurationUpdateWait = GetDistributedOperationCoordinatorConfigurationRefreshInterval() + staticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer;
                await Task.Delay(configurationUpdateWait);

                // Copy events from the source to target shard group
                Boolean filterGroupEventsByHashRange = false;
                if (dataElement == DataElement.Group)
                {
                    filterGroupEventsByHashRange = true;
                }
                Guid copyBeginId = metricLogger.Begin(new EventCopyTime());
                try
                {
                    shardGroupSplitter.CopyEventsToTargetShardGroup
                    (
                        sourceShardGroupEventReader,
                        targetShardGroupEventPersister,
                        operationRouter,
                        sourceShardGroupWriterAdministrator,
                        splitHashRangeStart,
                        splitHashRangeEnd,
                        filterGroupEventsByHashRange,
                        eventBatchSize,
                        sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                        sourceWriterNodeOperationsCompleteCheckRetryInterval
                    );
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(copyBeginId, new EventCopyTime());
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw new Exception("Error copying events from source shard group to target shard group.", e);
                }
                metricLogger.End(copyBeginId, new EventCopyTime());

                // Create the new shard group
                try
                {
                    await CreateShardGroupAsync(dataElement, splitHashRangeStart, targetPersistentStorageCredentials);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }

                // Turn on the router
                try
                {
                    operationRouter.RoutingOn = true;
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw new Exception("Failed to switch routing on.", e);
                }

                // Release all paused/held operation requests
                logger.Log(this, ApplicationLogging.LogLevel.Information, "Resuming operations in the source and target shard groups.");
                try
                {
                    operationRouter.ResumeOperations();
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw new Exception("Failed to resume incoming operations in the source and target shard groups.", e);
                }

                // Update the shard group configuration to redirect to target shard group
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating shard group configuration to redirect to target shard group...");
                configurationUpdates = new List<HashRangeStartAndClientConfigurations>()
                {
                    new HashRangeStartAndClientConfigurations
                    {
                        HashRangeStart = splitHashRangeStart,
                        ReaderNodeClientConfiguration = new AccessManagerRestClientConfiguration(GenerateNodeServiceUrl(dataElement, NodeType.Reader, splitHashRangeStart)),
                        WriterNodeClientConfiguration = new AccessManagerRestClientConfiguration(GenerateNodeServiceUrl(dataElement, NodeType.Writer, splitHashRangeStart))
                    },
                };
                configurationAdditions = new List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>();
                try
                {
                    UpdateAndPersistShardConfigurationSets(dataElement, configurationUpdates, configurationAdditions, new List<Int32>());
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating shard group configuration.");

                // Wait for the Updated the shard group configuration to be read by the operation coordinator nodes
                await Task.Delay(configurationUpdateWait);

                // Delete events from the source shard group
                Boolean includeGroupEvents = false;
                if (dataElement == DataElement.Group)
                {
                    includeGroupEvents = true;
                }
                try
                {
                    shardGroupSplitter.DeleteEventsFromSourceShardGroup
                    (
                        sourceShardGroupEventDeleter,
                        splitHashRangeStart,
                        splitHashRangeEnd,
                        includeGroupEvents
                    );
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw new Exception("Error deleting events from source shard group.", e);
                }

                // Pause/hold any incoming operation requests
                logger.Log(this, ApplicationLogging.LogLevel.Information, "Pausing operations in the source shard group.");
                try
                {
                    operationRouter.PauseOperations();
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw new Exception("Failed to hold/pause incoming operations in the source shard group.", e);
                }

                // Restart the source shard group
                try
                {
                    await RestartShardGroupAsync(dataElement, hashRangeStart);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }

                // Release all paused/held operation requests
                logger.Log(this, ApplicationLogging.LogLevel.Information, "Resuming operations in the source shard group.");
                try
                {
                    operationRouter.ResumeOperations();
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw new Exception("Failed to resume incoming operations in the source shard group.", e);
                }

                // Update the shard group configuration to redirect to source shard group
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating shard group configuration to redirect to source shard group...");
                configurationUpdates = new List<HashRangeStartAndClientConfigurations>()
            {
                new HashRangeStartAndClientConfigurations
                {
                    HashRangeStart = hashRangeStart,
                    ReaderNodeClientConfiguration = new AccessManagerRestClientConfiguration(GenerateNodeServiceUrl(dataElement, NodeType.Reader, hashRangeStart)),
                    WriterNodeClientConfiguration = new AccessManagerRestClientConfiguration(GenerateNodeServiceUrl(dataElement, NodeType.Writer, hashRangeStart))
                },
            };
                configurationAdditions = new List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>();
                try
                {
                    UpdateAndPersistShardConfigurationSets(dataElement, configurationUpdates, configurationAdditions, new List<Int32>());
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating shard group configuration.");

                // Wait for the Updated the shard group configuration to be read by the operation coordinator nodes
                await Task.Delay(configurationUpdateWait);

                // Copy the contents of the *ShardGroupConfigurationSet fields to the instance configuration
                CopyShardConfigurationSetsToInstanceConfiguration();

                // Undo update to writer load balancer service 
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Reversing update to writer load balancer service...");
                try
                {
                    await UpdateServiceAsync(writerLoadBalancerServiceName, GenerateWriter1LoadBalancerServiceName());
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed reversing update to writer load balancer service.");

                // Delete the router Cluster IP service
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting distributed operation router node cluster ip service...");
                try
                {
                    await DeleteServiceAsync($"{distributedOperationRouterObjectNamePrefix}{serviceNamePostfix}");
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting distributed operation router node cluster ip service.");

                // Delete distributed operation router
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting distributed operation router node...");
                try
                {
                    await ScaleDownAndDeleteDeploymentAsync(distributedOperationRouterObjectNamePrefix, staticConfiguration.DeploymentWaitPollingInterval, staticConfiguration.ServiceAvailabilityWaitAbortTimeout);
                }
                catch
                {
                    metricLogger.CancelBegin(beginId, new ShardGroupSplitTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting distributed operation router node.");
            }
            finally
            {
                // Dispose persisters and clients
                DisposeObject(sourceShardGroupWriterAdministrator);
                DisposeObject(operationRouter);
                DisposeObject(sourceShardGroupEventDeleter);
                DisposeObject(targetShardGroupEventPersister);
                DisposeObject(sourceShardGroupEventReader);
            }

            metricLogger.End(beginId, new ShardGroupSplitTime());
            metricLogger.Increment(new ShardGroupSplit());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed splitting shard group.");
        }

        /// <summary>
        /// Merges a two shard groups with consecutive hash code ranges in the distributed AccessManager instance.
        /// </summary>
        /// <param name="dataElement">The data element of the shard groups to merge.</param>
        /// <param name="sourceShardGroup1HashRangeStart">The first (inclusive) in the range of hash codes managed by the first shard group to merge.</param>
        /// <param name="sourceShardGroup2HashRangeStart">The first (inclusive) in the range of hash codes managed by the second shard group to merge.</param>
        /// <param name="sourceShardGroup2HashRangeEnd">The last (inclusive) in the range of hash codes managed by the second shard group to merge.</param>
        /// <param name="sourceShardGroupEventReaderCreationFunction">A function used to create a readers used to read events from the source shard group's persistent storage instances.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerTemporalEventBatchReader"/> instance.</param>
        /// <param name="targetShardGroupEventPersisterCreationFunction">A function used to create a persister used to write events to the target shard group's persistent storage instance.  Accepts TPersistentStorageCredentials and returns an <see cref="IAccessManagerIdempotentTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance.</param>
        /// <param name="operationRouterCreationFunction">A function used to create a client used control the router which directs operations between the source shard groups.  Accepts a <see cref="Uri"/> and returns an <see cref="IDistributedAccessManagerOperationRouter"/> instance.</param>
        /// <param name="sourceShardGroupWriterAdministratorCreationFunction">A function used to create a clients used control the writer nodes in the source shard groups.  Accepts a <see cref="Uri"/> and returns an <see cref="IDistributedAccessManagerWriterAdministrator"/> instance.</param>
        /// <param name="eventBatchSize">The number of events which should be copied from the source to the target shard groups in each batch.</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryAttempts">The number of times to retry checking that there are no active operations in the source shard groups, before merging of the final batch of events (event merge will fail if all retries are exhausted before the number of active operations becomes 0).</param>
        /// <param name="sourceWriterNodeOperationsCompleteCheckRetryInterval">The time in milliseconds to wait between retries specified in parameter <paramref name="sourceWriterNodeOperationsCompleteCheckRetryAttempts"/>.</param>
        /// <param name="shardGroupMerger">The <see cref="IDistributedAccessManagerShardGroupMerger"/> implementation used to perform the merging.</param>
        protected async Task MergeShardGroupsAsync
        (
            DataElement dataElement,
            Int32 sourceShardGroup1HashRangeStart,
            Int32 sourceShardGroup2HashRangeStart,
            Int32 sourceShardGroup2HashRangeEnd,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBatchReader> sourceShardGroupEventReaderCreationFunction,
            Func<TPersistentStorageCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> targetShardGroupEventPersisterCreationFunction,
            Func<Uri, IDistributedAccessManagerOperationRouter> operationRouterCreationFunction,
            Func<Uri, IDistributedAccessManagerWriterAdministrator> sourceShardGroupWriterAdministratorCreationFunction,
            Int32 eventBatchSize,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
            Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval,
            IDistributedAccessManagerShardGroupMerger shardGroupMerger
        )
        {
            if (userShardGroupConfigurationSet.Items.Count == 0)
                throw new InvalidOperationException($"A distributed AccessManager instance has not been created.");
            if (dataElement == DataElement.GroupToGroupMapping)
                throw new ArgumentException($"Shard group merging is not supported for '{typeof(DataElement).Name}' '{dataElement}'.", nameof(dataElement));
            if (sourceShardGroup2HashRangeEnd < sourceShardGroup2HashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(sourceShardGroup2HashRangeEnd), $"Parameter '{nameof(sourceShardGroup2HashRangeEnd)}' with value {sourceShardGroup2HashRangeEnd} must be greater than or equal to parameter '{nameof(sourceShardGroup2HashRangeStart)}' with value {sourceShardGroup2HashRangeStart}.");
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> sourceShardGroup1Configuration = GetShardGroupConfiguration(GetShardGroupConfigurationList(dataElement), sourceShardGroup1HashRangeStart);
            if (sourceShardGroup1Configuration == null)
                throw new ArgumentException($"Parameter '{nameof(sourceShardGroup1HashRangeStart)}' with value {sourceShardGroup1HashRangeStart} contains an invalid hash range start value for '{dataElement}' shard groups.", nameof(sourceShardGroup1HashRangeStart));
            if (sourceShardGroup2HashRangeStart <= sourceShardGroup1HashRangeStart)
                throw new ArgumentOutOfRangeException(nameof(sourceShardGroup2HashRangeStart), $"Parameter '{nameof(sourceShardGroup2HashRangeStart)}' with value {sourceShardGroup2HashRangeStart} must be greater than parameter '{nameof(sourceShardGroup1HashRangeStart)}' with value {sourceShardGroup1HashRangeStart}.");
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> sourceShardGroup2Configuration = GetShardGroupConfiguration(GetShardGroupConfigurationList(dataElement), sourceShardGroup2HashRangeStart); 
            if (sourceShardGroup2Configuration == null)
                throw new ArgumentException($"Parameter '{nameof(sourceShardGroup2HashRangeStart)}' with value {sourceShardGroup2HashRangeStart} contains an invalid hash range start value for '{dataElement}' shard groups.", nameof(sourceShardGroup2HashRangeStart));
            // This should always succeed since we know sourceShardGroup2Configuration exists, and that sourceShardGroup2HashRangeStart > sourceShardGroup1HashRangeStart
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> nextShardGroupConfiguration = GetNextShardGroupConfiguration(GetShardGroupConfigurationList(dataElement), sourceShardGroup1HashRangeStart);
            if (sourceShardGroup2Configuration.HashRangeStart != nextShardGroupConfiguration.HashRangeStart)
                throw new ArgumentException($"The next consecutive shard group after shard group with hash range start value {sourceShardGroup1HashRangeStart} has hash range start value {nextShardGroupConfiguration.HashRangeStart}, whereas parameter '{nameof(sourceShardGroup2HashRangeStart)}' contained {sourceShardGroup2HashRangeStart}.  The shard groups specified by parameters '{nameof(sourceShardGroup1HashRangeStart)}' and '{nameof(sourceShardGroup2HashRangeStart)}' must be consecutive.", nameof(sourceShardGroup2HashRangeStart));
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> nextNextShardGroupConfiguration = null;
            try
            {
                nextNextShardGroupConfiguration = GetNextShardGroupConfiguration(GetShardGroupConfigurationList(dataElement), sourceShardGroup2HashRangeStart);
            }
            catch
            {
                if (sourceShardGroup2HashRangeEnd != Int32.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(sourceShardGroup2HashRangeEnd), $"Parameter '{nameof(sourceShardGroup2HashRangeEnd)}' with value {sourceShardGroup2HashRangeEnd} contains a different hash range end value to the hash range end value {Int32.MaxValue} of the second source shard group being merged.");
            }
            if (nextNextShardGroupConfiguration != null)
            {
                if (sourceShardGroup2HashRangeEnd != nextNextShardGroupConfiguration.HashRangeStart - 1)
                    throw new ArgumentOutOfRangeException(nameof(sourceShardGroup2HashRangeEnd), $"Parameter '{nameof(sourceShardGroup2HashRangeEnd)}' with value {sourceShardGroup2HashRangeEnd} contains a different hash range end value to the hash range end value {nextNextShardGroupConfiguration.HashRangeStart - 1} of the second shard group being merged.");
            }

            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Merging {dataElement} shard group with hash range start value {sourceShardGroup1HashRangeStart} with shard group with hash range start value {sourceShardGroup2HashRangeStart}...");
            Guid mergeBeginId = metricLogger.Begin(new ShardGroupMergeTime());

            // Create the target persistent storage instance
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating temporary persistent storage instance...");
            String temporaryPersistentStorageInstanceName = persistentStorageInstanceRandomNameGenerator.Generate();
            if (staticConfiguration.PersistentStorageInstanceNamePrefix != "")
            {
                temporaryPersistentStorageInstanceName = $"{staticConfiguration.PersistentStorageInstanceNamePrefix}_{temporaryPersistentStorageInstanceName}";
            }
            Guid storageCreateBeginId = metricLogger.Begin(new PersistentStorageInstanceCreateTime());
            TPersistentStorageCredentials targetPersistentStorageCredentials;
            try
            {
                targetPersistentStorageCredentials = persistentStorageManager.CreateAccessManagerPersistentStorage(temporaryPersistentStorageInstanceName);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(storageCreateBeginId, new PersistentStorageInstanceCreateTime());
                metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                throw new Exception($"Error creating temporary persistent storage instance.", e);
            }
            metricLogger.End(storageCreateBeginId, new PersistentStorageInstanceCreateTime());
            metricLogger.Increment(new PersistentStorageInstanceCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating temporary persistent storage instance.");

            // Create persisters and clients
            IAccessManagerTemporalEventBatchReader sourceShardGroup1EventReader = null;
            IAccessManagerTemporalEventBatchReader sourceShardGroup2EventReader = null;
            IAccessManagerTemporalEventBulkPersister<String, String, String, String> targetShardGroupEventPersister = null;
            IDistributedAccessManagerOperationRouter operationRouter = null;
            IDistributedAccessManagerWriterAdministrator sourceShardGroup1WriterAdministrator = null;
            IDistributedAccessManagerWriterAdministrator sourceShardGroup2WriterAdministrator = null;
            try
            {
                ConstructInstance
                (
                    () => { sourceShardGroup1EventReader = sourceShardGroupEventReaderCreationFunction(sourceShardGroup1Configuration.PersistentStorageCredentials); },
                    nameof(sourceShardGroup1EventReader)
                );
                ConstructInstance
                (
                    () => { sourceShardGroup2EventReader = sourceShardGroupEventReaderCreationFunction(sourceShardGroup2Configuration.PersistentStorageCredentials); },
                    nameof(sourceShardGroup2EventReader)
                );
                ConstructInstance
                (
                    () => { targetShardGroupEventPersister = targetShardGroupEventPersisterCreationFunction(targetPersistentStorageCredentials); },
                    nameof(targetShardGroupEventPersister)
                );
                ConstructInstance
                (
                    () => { operationRouter = operationRouterCreationFunction(instanceConfiguration.DistributedOperationRouterUrl); },
                    nameof(operationRouter)
                );
                ConstructInstance
                (
                    () => { sourceShardGroup1WriterAdministrator = sourceShardGroupWriterAdministratorCreationFunction(instanceConfiguration.Writer1Url); },
                    nameof(sourceShardGroup1WriterAdministrator)
                );
                ConstructInstance
                (
                    () => { sourceShardGroup2WriterAdministrator = sourceShardGroupWriterAdministratorCreationFunction(instanceConfiguration.Writer2Url); },
                    nameof(sourceShardGroup2WriterAdministrator)
                );
            }
            catch
            {
                metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                throw;
            }

            try
            {
                // Create distributed operation router
                try
                {
                    await CreateDistributedOperationRouterNodeAsync
                    (
                        dataElement,
                        sourceShardGroup1Configuration.ReaderNodeClientConfiguration.BaseUrl,
                        sourceShardGroup1Configuration.WriterNodeClientConfiguration.BaseUrl,
                        sourceShardGroup1HashRangeStart,
                        sourceShardGroup2HashRangeStart - 1,
                        sourceShardGroup2Configuration.ReaderNodeClientConfiguration.BaseUrl,
                        sourceShardGroup2Configuration.WriterNodeClientConfiguration.BaseUrl,
                        sourceShardGroup2HashRangeStart,
                        sourceShardGroup2HashRangeEnd,
                        true
                    );
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }

                // Update writer 1 load balancer service to target source shard group 1 writer node
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating writer 1 load balancer service to target first source shard group writer node...");
                String writer1LoadBalancerServiceName = $"{GenerateWriter1LoadBalancerServiceName()}{externalServiceNamePostfix}";
                String sourceWriter1NodeIdentifier = GenerateNodeIdentifier(dataElement, NodeType.Writer, sourceShardGroup1HashRangeStart);
                try
                {
                    await UpdateServiceAsync(writer1LoadBalancerServiceName, sourceWriter1NodeIdentifier);
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating writer 1 load balancer service.");

                // Update writer 2 load balancer service to target source shard group 2 writer node
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating writer 2 load balancer service to target first source shard group writer node...");
                String writer2LoadBalancerServiceName = $"{GenerateWriter2LoadBalancerServiceName()}{externalServiceNamePostfix}";
                String sourceWriter2NodeIdentifier = GenerateNodeIdentifier(dataElement, NodeType.Writer, sourceShardGroup2HashRangeStart);
                try
                {
                    await UpdateServiceAsync(writer2LoadBalancerServiceName, sourceWriter2NodeIdentifier);
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating writer 2 load balancer service.");

                // Update the shard group configuration to redirect to the router
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating shard group configuration to redirect to router...");
                Uri routerInternalUrl = new($"http://{distributedOperationRouterObjectNamePrefix}{serviceNamePostfix}:{staticConfiguration.PodPort}");
                AccessManagerRestClientConfiguration routerClientConfiguration = new(routerInternalUrl);
                List<HashRangeStartAndClientConfigurations> configurationUpdates = new()
                {
                    new HashRangeStartAndClientConfigurations
                    {
                        HashRangeStart = sourceShardGroup1HashRangeStart,
                        ReaderNodeClientConfiguration = routerClientConfiguration,
                        WriterNodeClientConfiguration = routerClientConfiguration
                    }
                };
                List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> configurationAdditions = new();
                List<Int32> configurationDeletes = new() { sourceShardGroup2HashRangeStart };
                try
                {
                    UpdateAndPersistShardConfigurationSets(dataElement, configurationUpdates, configurationAdditions, configurationDeletes);
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating shard group configuration.");

                // Wait for the Updated the shard group configuration to be read by the operation coordinator nodes
                Int32 configurationUpdateWait = GetDistributedOperationCoordinatorConfigurationRefreshInterval() + staticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer;
                await Task.Delay(configurationUpdateWait);

                // Merge events from the two source shard groups to the target shard group
                Guid eventMergeBeginId = metricLogger.Begin(new EventMergeTime());
                try
                {
                    shardGroupMerger.MergeEventsToTargetShardGroup
                    (
                        sourceShardGroup1EventReader,
                        sourceShardGroup2EventReader,
                        targetShardGroupEventPersister,
                        operationRouter,
                        sourceShardGroup1WriterAdministrator,
                        sourceShardGroup2WriterAdministrator,
                        eventBatchSize,
                        sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                        sourceWriterNodeOperationsCompleteCheckRetryInterval
                    );
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(eventMergeBeginId, new EventMergeTime());
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception("Error merging events from source shard group to target shard group.", e);
                }
                metricLogger.End(eventMergeBeginId, new EventMergeTime());

                // Shut down source shard group 1
                try
                {
                    await ScaleDownShardGroupAsync(dataElement, sourceShardGroup1HashRangeStart);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception($"Error scaling down source shard group 1 with data element '{dataElement}' and hash range start value {sourceShardGroup1HashRangeStart}.", e);
                }

                // Rename source shard group 1 persistent storage instance
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Renaming source shard group 1 persistent storage instance...");
                String source1PersistentStorageInstanceName = GeneratePersistentStorageInstanceName(dataElement, sourceShardGroup1HashRangeStart);
                Guid storageRenameBeginId = metricLogger.Begin(new PersistentStorageInstanceRenameTime());
                try
                {
                    persistentStorageManager.RenamePersistentStorage(source1PersistentStorageInstanceName, $"{source1PersistentStorageInstanceName}{supersededPersisentStorageInstanceNamePostfix}");
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(storageRenameBeginId, new PersistentStorageInstanceRenameTime());
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception($"Error renaming source shard group 1 persistent storage instance.", e);
                }
                metricLogger.End(storageRenameBeginId, new PersistentStorageInstanceRenameTime());
                metricLogger.Increment(new PersistentStorageInstanceRenamed());
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed renaming source shard group 1 persistent storage instance.");

                // Rename target/temporary persistent storage instance to become source shard group 1 persistent storage
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Renaming temporary persistent storage instance...");
                storageRenameBeginId = metricLogger.Begin(new PersistentStorageInstanceRenameTime());
                try
                {
                    persistentStorageManager.RenamePersistentStorage(temporaryPersistentStorageInstanceName, source1PersistentStorageInstanceName);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(storageRenameBeginId, new PersistentStorageInstanceRenameTime());
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception($"Error renaming temporary persistent storage instance.", e);
                }
                metricLogger.End(storageRenameBeginId, new PersistentStorageInstanceRenameTime());
                metricLogger.Increment(new PersistentStorageInstanceRenamed());
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed renaming temporary persistent storage instance.");

                // Restart source shard group 1
                try
                {
                    await ScaleUpShardGroupAsync(dataElement, sourceShardGroup1HashRangeStart);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception($"Error scaling up source shard group 1 with data element '{dataElement}' and hash range start value {sourceShardGroup1HashRangeStart}.", e);
                }

                // Turn off the router
                try
                {
                    operationRouter.RoutingOn = false;
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception("Failed to switch routing off.", e);
                }

                // Release all paused/held operation requests
                logger.Log(this, ApplicationLogging.LogLevel.Information, "Resuming operations in the source and target shard groups.");
                try
                {
                    operationRouter.ResumeOperations();
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception("Failed to resume incoming operations in the source and target shard groups.", e);
                }

                // Update the shard group configuration to redirect to source shard group 1
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Updating shard group configuration to redirect to source shard group 1...");
                routerClientConfiguration = new(routerInternalUrl);
                configurationUpdates = new()
                {
                    new HashRangeStartAndClientConfigurations
                    {
                        HashRangeStart = sourceShardGroup1HashRangeStart,
                        ReaderNodeClientConfiguration = new AccessManagerRestClientConfiguration(GenerateNodeServiceUrl(dataElement, NodeType.Reader, sourceShardGroup1HashRangeStart)),
                        WriterNodeClientConfiguration = new AccessManagerRestClientConfiguration(GenerateNodeServiceUrl(dataElement, NodeType.Writer, sourceShardGroup1HashRangeStart))
                    }
                };
                configurationAdditions = new List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>>();
                configurationDeletes = new List<Int32>();
                try
                {
                    UpdateAndPersistShardConfigurationSets(dataElement, configurationUpdates, configurationAdditions, configurationDeletes);
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed updating shard group configuration.");

                // Wait for the Updated the shard group configuration to be read by the operation coordinator nodes
                await Task.Delay(configurationUpdateWait);

                // Copy the contents of the *ShardGroupConfigurationSet fields to the instance configuration
                CopyShardConfigurationSetsToInstanceConfiguration();

                // Scale down source shard group 2
                try
                {
                    await ScaleDownShardGroupAsync(dataElement, sourceShardGroup2HashRangeStart);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception($"Error scaling down source shard group 2 with data element '{dataElement}' and hash range start value {sourceShardGroup2HashRangeStart}.", e);
                }

                // Delete source shard group 2
                try
                {
                    await DeleteShardGroupAsync(dataElement, sourceShardGroup2HashRangeStart, true);
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }

                // Delete the original source shard group 1 persistent storage instance
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting original source shard group 1 persistent storage instance...");
                try
                {
                    persistentStorageManager.DeletePersistentStorage($"{source1PersistentStorageInstanceName}{supersededPersisentStorageInstanceNamePostfix}");
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw new Exception($"Error deleting persistent storage instance '{source1PersistentStorageInstanceName}{supersededPersisentStorageInstanceNamePostfix}'.", e);
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting persistent storage instance.");

                // Undo updates to writer load balancer services
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Reversing updates to writer load balancer services...");
                try
                {
                    await UpdateServiceAsync(writer1LoadBalancerServiceName, GenerateWriter1LoadBalancerServiceName());
                    await UpdateServiceAsync(writer2LoadBalancerServiceName, GenerateWriter2LoadBalancerServiceName());
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed reversing updates to writer load balancer services.");

                // Delete the router Cluster IP service
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting distributed operation router node cluster ip service...");
                try
                {
                    await DeleteServiceAsync($"{distributedOperationRouterObjectNamePrefix}{serviceNamePostfix}");
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting distributed operation router node cluster ip service.");

                // Delete distributed operation router
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting distributed operation router node...");
                try
                {
                    await ScaleDownAndDeleteDeploymentAsync(distributedOperationRouterObjectNamePrefix, staticConfiguration.DeploymentWaitPollingInterval, staticConfiguration.ServiceAvailabilityWaitAbortTimeout);
                }
                catch
                {
                    metricLogger.CancelBegin(mergeBeginId, new ShardGroupMergeTime());
                    throw;
                }
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed deleting distributed operation router node.");
            }
            finally
            {
                // Dispose persisters and clients
                DisposeObject(sourceShardGroup2WriterAdministrator);
                DisposeObject(sourceShardGroup1WriterAdministrator);
                DisposeObject(operationRouter);
                DisposeObject(targetShardGroupEventPersister);
                DisposeObject(sourceShardGroup2EventReader);
                DisposeObject(sourceShardGroup1EventReader);
            }

            metricLogger.End(mergeBeginId, new ShardGroupMergeTime());
            metricLogger.Increment(new ShardGroupsMerged());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed merging shard groups.");
        }

        /// <summary>
        /// Populates the *ShardGroupConfigurationSet fields with the specified shard group configuration.
        /// </summary>
        /// <param name="userShardGroupConfiguration">The configuration of the user shard groups.</param>
        /// <param name="groupToGroupMappingShardGroupConfiguration">The configuration of the group to group mapping shard groups.</param>
        /// <param name="groupShardGroupConfiguration">The configuration of the group shard groups.</param>
        protected void PopulateShardGroupConfigurationSetFields
        (
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> userShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupToGroupMappingShardGroupConfiguration,
            IList<ShardGroupConfiguration<TPersistentStorageCredentials>> groupShardGroupConfiguration
        )
        {
            List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> userKubernetesShardGroupConfiguration = CreateKubernetesShardGroupConfigurationList(userShardGroupConfiguration, DataElement.User);
            userShardGroupConfigurationSet.AddRange(userKubernetesShardGroupConfiguration);
            List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> groupToGroupMappingKubernetesShardGroupConfiguration = CreateKubernetesShardGroupConfigurationList(groupToGroupMappingShardGroupConfiguration, DataElement.GroupToGroupMapping);
            groupToGroupMappingShardGroupConfigurationSet.AddRange(groupToGroupMappingKubernetesShardGroupConfiguration);
            List<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> groupKubernetesShardGroupConfiguration = CreateKubernetesShardGroupConfigurationList(groupShardGroupConfiguration, DataElement.Group);
            groupShardGroupConfigurationSet.AddRange(groupKubernetesShardGroupConfiguration);
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
            Uri readerNodeServiceUrl = GenerateNodeServiceUrl(dataElement, NodeType.Reader, shardGroupConfiguration.HashRangeStart);
            Uri writerNodeServiceUrl = GenerateNodeServiceUrl(dataElement, NodeType.Writer, shardGroupConfiguration.HashRangeStart);
            AccessManagerRestClientConfiguration readerNodeClientConfiguration = new(readerNodeServiceUrl);
            AccessManagerRestClientConfiguration writerNodeClientConfiguration = new(writerNodeServiceUrl);
            KubernetesShardGroupConfiguration<TPersistentStorageCredentials> kubernetesConfiguration = new
            (
                shardGroupConfiguration.HashRangeStart,
                shardGroupConfiguration.PersistentStorageCredentials,
                readerNodeClientConfiguration,
                writerNodeClientConfiguration
            );

            return kubernetesConfiguration;
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
                ShardConfiguration<AccessManagerRestClientConfiguration> readerConfiguration = new(dataElement, Operation.Query, currentConfiguration.HashRangeStart, readerClientConfiguration);
                returnList.Add(readerConfiguration);
                AccessManagerRestClientConfiguration writerClientConfiguration = new AccessManagerRestClientConfiguration(currentConfiguration.WriterNodeClientConfiguration.BaseUrl);
                ShardConfiguration<AccessManagerRestClientConfiguration> writerConfiguration = new(dataElement, Operation.Event, currentConfiguration.HashRangeStart, writerClientConfiguration);
                returnList.Add(writerConfiguration);
            }

            return returnList;
        }

        /// <summary>
        /// Retrieves the shard group configuration for the specified data element from the *ShardGroupConfigurationSet fields.
        /// </summary>
        /// <param name="dataElement">The data element to retrieve the shard group configuration for.</param>
        /// <returns>The shard group configuration.</returns>
        protected IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> GetShardGroupConfigurationList(DataElement dataElement)
        {
            if (dataElement == DataElement.User)
            {
                return userShardGroupConfigurationSet.Items;
            }
            else if (dataElement == DataElement.GroupToGroupMapping)
            {
                return groupToGroupMappingShardGroupConfigurationSet.Items;
            }
            else if (dataElement == DataElement.Group)
            {
                return groupShardGroupConfigurationSet.Items;
            }
            else
            {
                throw new ArgumentException($"Encountered unhandled {typeof(DataElement).Name} value '{dataElement}'.");
            }
        }

        /// <summary>
        /// Retrieves shard group configuration with the specified hash range start value from a list of shard group configuration.
        /// </summary>
        /// <param name="shardGroupConfigurationList">The list of shard group configuration to retrieve the specified configuration from.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of the shard group.</param>
        /// <returns>The shard group configuration or null if shard group configuration with the specified properties was not found.</returns>
        protected KubernetesShardGroupConfiguration<TPersistentStorageCredentials> GetShardGroupConfiguration(IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> shardGroupConfigurationList, Int32 hashRangeStart)
        {
            foreach (KubernetesShardGroupConfiguration<TPersistentStorageCredentials> currentConfiguration in shardGroupConfigurationList)
            {
                if (currentConfiguration.HashRangeStart == hashRangeStart)
                {
                    return currentConfiguration;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves shard group configuration sequentially after that with with the specified hash range start value, from a list of shard group configuration.
        /// </summary>
        /// <param name="shardGroupConfigurationList">The list of shard group configuration to retrieve the configuration from.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of the shard group before the one to return.</param>
        /// <returns>The shard group configuration or null if the shard group configuration with the specified hash range start value is the last (i.e. hash range ends on Int32.MaxValue).</returns>
        protected KubernetesShardGroupConfiguration<TPersistentStorageCredentials> GetNextShardGroupConfiguration(IList<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> shardGroupConfigurationList, Int32 hashRangeStart)
        {
            for (Int32 i = 0; i < shardGroupConfigurationList.Count; i++)
            {
                if (shardGroupConfigurationList[i].HashRangeStart == hashRangeStart)
                {
                    if (i == shardGroupConfigurationList.Count - 1)
                    {
                        throw new ArgumentException($"No shard configuration exists with greater hash range start value than that specified in parameter '{nameof(hashRangeStart)}' with value {hashRangeStart}.", nameof(hashRangeStart));
                    }

                    return shardGroupConfigurationList[i + 1];
                }
            }

            throw new ArgumentException($"No shard configuration exists with the hash range start value specified in parameter '{nameof(hashRangeStart)}' with value {hashRangeStart}.", nameof(hashRangeStart));
        }

        /// <summary>
        /// Updates and persists the shard group configuration set fields. 
        /// </summary>
        /// <param name="dataElement">The data element of the shard group configuration to apply the updates and additions to.</param>
        /// <param name="configurationUpdates">Updates that should be applied to the shard group configuration set of type specified in parameter <paramref name="dataElement"/> (keyed by hash rage start value).</param>
        /// <param name="configurationAdditions">Configuration which should be added to the shard group configuration set of type specified in parameter <paramref name="dataElement"/>.</param>
        /// <param name="configurationDeletes">Configuration which should be deleted (identified by the hash range start value of each configuration) from the shard group configuration set of type specified in parameter <paramref name="dataElement"/>.</param>
        protected void UpdateAndPersistShardConfigurationSets
        (
            DataElement dataElement, 
            IEnumerable<HashRangeStartAndClientConfigurations> configurationUpdates, 
            IEnumerable<KubernetesShardGroupConfiguration<TPersistentStorageCredentials>> configurationAdditions, 
            IEnumerable<Int32> configurationDeletes
        )
        {
            KubernetesShardGroupConfigurationSet<TPersistentStorageCredentials> shardGroupConfigurationSetToUpdate = null;
            if (dataElement == DataElement.User)
            {
                shardGroupConfigurationSetToUpdate = userShardGroupConfigurationSet;
            }
            else if (dataElement == DataElement.Group)
            {
                shardGroupConfigurationSetToUpdate = groupShardGroupConfigurationSet;
            }
            else
            {
                throw new Exception($"Encountered unhandled {typeof(DataElement).Name} '{dataElement}'.");
            }
            foreach (HashRangeStartAndClientConfigurations currentConfigurationUpdate in configurationUpdates)
            {
                shardGroupConfigurationSetToUpdate.UpdateRestClientConfiguration(currentConfigurationUpdate.HashRangeStart, currentConfigurationUpdate.ReaderNodeClientConfiguration, currentConfigurationUpdate.WriterNodeClientConfiguration);
            }
            shardGroupConfigurationSetToUpdate.AddRange(configurationAdditions);
            foreach(Int32 configurationDelete in configurationDeletes)
            {
                shardGroupConfigurationSetToUpdate.Remove(configurationDelete);
            }
            ShardConfigurationSet<AccessManagerRestClientConfiguration> shardConfigurationSet = CreateShardConfigurationSet
            (
                userShardGroupConfigurationSet.Items,
                groupToGroupMappingShardGroupConfigurationSet.Items,
                groupShardGroupConfigurationSet.Items
            );
            if (shardConfigurationSetPersister == null)
            {
                ConstructInstance
                (
                    () => { shardConfigurationSetPersister = shardConfigurationSetPersisterCreationFunction(instanceConfiguration.ShardConfigurationPersistentStorageCredentials); },
                    "shardConfigurationSetPersister"
                );
            }
            try
            {
                shardConfigurationSetPersister.Write(shardConfigurationSet, true);
            }
            catch (Exception e)
            {
                throw new Exception($"Error updating shard configuration in persistent storage.", e);
            }
        }

        /// <summary>
        /// Copies the contents of the shard group configuration set fields to the instance configuration.
        /// </summary>
        protected void CopyShardConfigurationSetsToInstanceConfiguration()
        {
            instanceConfiguration.UserShardGroupConfiguration = userShardGroupConfigurationSet.Items;
            instanceConfiguration.GroupToGroupMappingShardGroupConfiguration = groupToGroupMappingShardGroupConfigurationSet.Items;
            instanceConfiguration.GroupShardGroupConfiguration = groupShardGroupConfigurationSet.Items;
        }

        /// <summary>
        /// Constructs a class instance used within the AccessManager instance manager, throwing an exception if construction fails.
        /// </summary>
        /// <param name="instanceConstructionAction">Action which constructs the instance.</param>
        /// <param name="instanceName">The name of the instance being constructed (to use in exception messages).</param>
        protected void ConstructInstance(Action instanceConstructionAction, String instanceName)
        {
            try
            {
                instanceConstructionAction();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to construct {instanceName}.", e);
            }
        }

        /// <summary>
        /// Calls Dispose() on the specified object if it implements IDisposable.
        /// </summary>
        /// <param name="inputObject">The object to dispose.</param>
        protected void DisposeObject(Object inputObject)
        {
            if (inputObject is IDisposable)
            {
                ((IDisposable)inputObject).Dispose();
            }
        }

        /// <summary>
        /// Gets the distributed operation coordinator node's configuration refresh interval (in milliseconds).
        /// </summary>
        /// <returns>The refresh interval.</returns>
        protected Int32 GetDistributedOperationCoordinatorConfigurationRefreshInterval()
        {
            // Don't need to validate this as it's validated on construction by class KubernetesDistributedAccessManagerInstanceManagerStaticConfigurationValidatorTests
            JObject coordinatorNodeConfiguration = staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
            JToken refreshIntervalToken = coordinatorNodeConfiguration.SelectToken($"{appsettingsShardConfigurationRefreshPropertyName}.{appsettingsRefreshIntervalPropertyName}", false);
            Int32 refreshInterval = Int32.Parse(refreshIntervalToken.ToString());

            return refreshInterval;
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
        /// Generates the name ('app' label) for the first load balancer service used to access a writer component.
        /// </summary>
        /// <returns>The name.</returns>
        protected String GenerateWriter1LoadBalancerServiceName()
        {
            return $"{NodeType.Writer.ToString().ToLower()}1";
        }

        /// <summary>
        /// Generates the name ('app' label) for the second load balancer service used to access a writer component.
        /// </summary>
        /// <returns>The name.</returns>
        protected String GenerateWriter2LoadBalancerServiceName()
        {
            return $"{NodeType.Writer.ToString().ToLower()}2";
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

        #pragma warning disable 1591

        protected void ThrowExceptionIfIntegerParameterLessThan1(String parameterName, Int32 parameterValue)
        {
            if (parameterValue < 1)
                throw new ArgumentOutOfRangeException(parameterName, $"Parameter '{parameterName}' with value {parameterValue} must be greater than 0.");
        }

        #pragma warning restore 1591

        #endregion

        #region ApplicationAccess Node and Shard Group Create/Delete Methods

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
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid shardGroupCreateBeginId = metricLogger.Begin(new ShardGroupCreateTime());

            if (persistentStorageCredentials == null)
            {
                // Create a persistent storage instance
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating persistent storage instance for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart}...");
                String persistentStorageInstanceName = GeneratePersistentStorageInstanceName(dataElement, hashRangeStart);
                Guid storageBeginId = metricLogger.Begin(new PersistentStorageInstanceCreateTime());
                try
                {
                    persistentStorageCredentials = persistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(storageBeginId, new PersistentStorageInstanceCreateTime());
                    metricLogger.CancelBegin(shardGroupCreateBeginId, new ShardGroupCreateTime());
                    throw new Exception($"Error creating persistent storage instance for data element type '{dataElement}' and hash range start value {hashRangeStart}.", e);
                }
                metricLogger.End(storageBeginId, new PersistentStorageInstanceCreateTime());
                metricLogger.Increment(new PersistentStorageInstanceCreated());
                logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating persistent storage instance.");
            }

            // Create event cache node
            try
            {
                await CreateEventCacheNodeAsync(dataElement, hashRangeStart);
            }
            catch
            {
                metricLogger.CancelBegin(shardGroupCreateBeginId, new ShardGroupCreateTime());
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
                metricLogger.CancelBegin(shardGroupCreateBeginId, new ShardGroupCreateTime());
                throw;
            }

            metricLogger.End(shardGroupCreateBeginId, new ShardGroupCreateTime());
            metricLogger.Increment(new ShardGroupCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed creating shard group.");

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
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Restarting shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed restarting shard group.");
        }

        /// <summary>
        /// Scales down all nodes of a shard group in a distributed AccessManager implementation (i.e. sets all node's pod/deployment replica counts to 0, and waits for the scale down to complete).
        /// </summary>
        /// <param name="dataElement">The data element of the shard group.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        protected async Task ScaleDownShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Scaling down shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed scaling down shard group.");
        }

        /// <summary>
        /// Scales up all nodes of a shard group in a distributed AccessManager implementation (i.e. sets all node's pod/deployment replica counts to their original, non-0 values, and waits for the scale up to complete).
        /// </summary>
        /// <param name="dataElement">The data element of the shard group.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        protected async Task ScaleUpShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Scaling up shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed scaling up shard group.");
        }

        /// <summary>
        /// Deletes a shard group from the distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element to delete the shard group for.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        /// <param name="deletePersistentStorageInstance">Whether to additionally delete the persistent storage instance used by the shard group.</param>
        protected async Task DeleteShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, Boolean deletePersistentStorageInstance)
        {
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Deleting shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart}...");
            Guid shardGroupDeleteBeginId = metricLogger.Begin(new ShardGroupDeleteTime());

            String readerDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Reader, hashRangeStart);
            String writerDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Writer, hashRangeStart);
            String eventCacheDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.EventCache, hashRangeStart);

            // Delete reader and writer nodes
            Task deleteReaderNodeTask = Task.Run(async () => await DeleteApplicationAccessNodeAsync(readerDeploymentName, "reader"));
            Task deleteWriterNodeTask = Task.Run(async () => await DeleteApplicationAccessNodeAsync(writerDeploymentName, "writer"));
            try
            {
                await Task.WhenAll(deleteReaderNodeTask, deleteWriterNodeTask);
            }
            catch
            {
                metricLogger.CancelBegin(shardGroupDeleteBeginId, new ShardGroupDeleteTime());
                throw;
            }

            // Delete event cache node
            try
            {
                await DeleteApplicationAccessNodeAsync(eventCacheDeploymentName, "event cache");
            }
            catch
            {
                metricLogger.CancelBegin(shardGroupDeleteBeginId, new ShardGroupDeleteTime());
                throw;
            }

            // Delete persistent storage instance
            if (deletePersistentStorageInstance == true)
            {
                String persistentStorageInstanceName = GeneratePersistentStorageInstanceName(dataElement, hashRangeStart);
                try
                {
                    persistentStorageManager.DeletePersistentStorage(persistentStorageInstanceName);
                }
                catch (Exception e)
                {
                    metricLogger.CancelBegin(shardGroupDeleteBeginId, new ShardGroupDeleteTime());
                    throw new Exception($"Error deleting persistent storage instance '{persistentStorageInstanceName}'.", e);
                }
            }

            metricLogger.End(shardGroupDeleteBeginId, new ShardGroupDeleteTime());
            metricLogger.Increment(new ShardGroupDeleted());
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed deleting shard group.");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating reader node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ReaderNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, "reader", availabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new ReaderNodeCreateTime());
                throw new Exception($"Error creating reader node for data element type '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            metricLogger.End(beginId, new ReaderNodeCreateTime());
            metricLogger.Increment(new ReaderNodeCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating reader node.");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating event cache node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new EventCacheNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, "event cache", availabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventCacheNodeCreateTime());
                throw new Exception($"Error creating event cache node for data element type '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            metricLogger.End(beginId, new EventCacheNodeCreateTime());
            metricLogger.Increment(new EventCacheNodeCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating event cache node.");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating writer node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new WriterNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, "writer", availabilityWaitAbortTimeout);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new WriterNodeCreateTime());
                throw new Exception($"Error creating writer node for data element type '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            metricLogger.End(beginId, new WriterNodeCreateTime());
            metricLogger.Increment(new WriterNodeCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating writer node.");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating distributed operation coordinator node in namespace '{nameSpace}'...");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating distributed operation coordinator node.");
        }

        /// <summary>
        /// Creates a distributed operation router node in a distributed AccessManager implementation.
        /// </summary>
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
        protected async Task CreateDistributedOperationRouterNodeAsync
        (
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
            String nameSpace = staticConfiguration.NameSpace;
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(staticConfiguration.DistributedOperationRouterNodeConfigurationTemplate);
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating distributed operation router node...");
            Guid beginId = metricLogger.Begin(new DistributedOperationRouterNodeCreateTime());
            Task createDeploymentTask = Task.Run(async () =>
            {
                try
                {
                    await CreateDistributedOperationRouterNodeDeploymentAsync
                    (
                        distributedOperationRouterObjectNamePrefix,
                        dataElement,
                        sourceReaderUrl,
                        sourceWriterUrl,
                        sourceHashRangeStart,
                        sourceHashRangeEnd,
                        targetReaderUrl,
                        targetWriterUrl,
                        targetHashRangeStart,
                        targetHashRangeEnd,
                        routingInitiallyOn
                    );
                }
                catch (Exception e)
                {
                    throw new Exception($"Error creating distributed operation router deployment.", e);
                }
            });
            Task createServiceTask = Task.Run(async () =>
            {
                try
                {
                    await CreateClusterIpServiceAsync(distributedOperationRouterObjectNamePrefix, serviceNamePostfix, staticConfiguration.PodPort);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error creating operation router service '{distributedOperationRouterObjectNamePrefix}{serviceNamePostfix}'.", e);
                }
            });
            Task waitForDeploymentTask = Task.Run(async () =>
            {
                try
                {
                    await WaitForDeploymentAvailabilityAsync(distributedOperationRouterObjectNamePrefix, staticConfiguration.DeploymentWaitPollingInterval, availabilityWaitAbortTimeout);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error waiting for distributed operation router deployment to become available.", e);
                }
            });
            try
            {
                await Task.WhenAll(createDeploymentTask, createServiceTask, waitForDeploymentTask);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new DistributedOperationRouterNodeCreateTime());
                throw;
            }
            metricLogger.End(beginId, new DistributedOperationRouterNodeCreateTime());
            metricLogger.Increment(new DistributedOperationRouterNodeCreated());
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Completed creating distributed operation router node.");
        }

        /// <summary>
        /// Creates an ApplicationAccess 'node' as part of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="deploymentName">The name of the Kubernetes deployment to create to host the node.</param>
        /// <param name="createDeploymentFunction">An async <see cref="Func{TResult}"/> which creates the Kubernetes deployment for the node.</param>
        /// <param name="nodeTypeName">The name of the type of the node (to use in exception messages, e.g. 'event cache', 'reader', etc...).</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the node hasn't become available.</param>
        protected async Task CreateApplicationAccessNodeAsync
        (
            String deploymentName,
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
        /// Deletes an ApplicationAccess 'node' in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="deploymentName">The name of the Kubernetes deployment which hosts the node.</param>
        /// <param name="nodeTypeName">The name of the type of the node (to use in exception messages, e.g. 'event cache', 'reader', etc...).</param>
        protected async Task DeleteApplicationAccessNodeAsync(String deploymentName, String nodeTypeName)
        {
            Task deleteServiceTask = Task.Run(async () =>
            {
                try
                {
                    await DeleteServiceAsync($"{deploymentName}{serviceNamePostfix}");
                }
                catch (Exception e)
                {
                    throw new Exception($"Error deleting {nodeTypeName} service '{deploymentName}{serviceNamePostfix}'.", e);
                }
            });
            Task deleteDeploymentTask = Task.Run(async () =>
            {
                try
                {
                    await DeleteDeploymentAsync(deploymentName);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error deleting {nodeTypeName} deployment '{deploymentName}'.", e);
                }
            });
            await Task.WhenAll(deleteServiceTask, deleteDeploymentTask);
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer' which is used to access the distributed operation coordinator component from outside the Kubernetes cluster.
        /// </summary>
        /// <param name="port">The external port to expose the load balancer service on.</param>
        /// <returns>The IP address of the load balancer service.</returns>
        protected async Task<IPAddress> CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(UInt16 port)
        {
            String nameSpace = staticConfiguration.NameSpace;
            logger.Log(this, ApplicationLogging.LogLevel.Information, $"Creating load balancer service for distributed operation coordinator on port {port} in namespace '{nameSpace}'...");
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
            logger.Log(this, ApplicationLogging.LogLevel.Information, "Completed creating load balancer service.");

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
        /// Updates the 'app' label value (i.e. the targetted pod(s)) of a Kubernetes service.
        /// </summary>
        /// <param name="serviceName">The name of the service to update.</param>
        /// <param name="appLabelValue">The name of the new pod/deployment to be targetted by the service.</param>
        protected async Task UpdateServiceAsync(String serviceName, String appLabelValue)
        {
            V1Service servicePatch = new()
            {
                Spec = new V1ServiceSpec()
                {
                    Selector = new Dictionary<String, String>()
                }
            };
            servicePatch.Spec.Selector.Add(appLabel, appLabelValue);
            V1Patch patchDefinition = new(servicePatch, V1Patch.PatchType.MergePatch);

            try
            {
                await kubernetesClientShim.PatchNamespacedServiceAsync(kubernetesClient, patchDefinition, serviceName, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to update Kubernetes service '{serviceName}' to target pod '{appLabelValue}'.", e);
            }
        }

        /// <summary>
        /// Deletes the specified service.
        /// </summary>
        /// <param name="name">The name of the service.</param>
        protected async Task DeleteServiceAsync(String name)
        {
            try
            {
                await kubernetesClientShim.DeleteNamespacedServiceAsync(kubernetesClient, name, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to delete Kubernetes service '{name}'.", e);
            }
        }

        /// <summary>
        /// Retrieves the external endpoint IP address of a Kubernetes service of type 'LoadBalancer'.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <returns>The external endpoint IP address.</returns>
        protected async Task<IPAddress> GetLoadBalancerServiceIpAddressAsync(String serviceName)
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
            appsettingsContents[appsettingsShardRoutingPropertyName][appsettingsSourceQueryShardBaseUrlPropertyName] = sourceReaderUrl.ToString();
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

            V1Deployment deploymentPatch = new()
            {
                Spec = new V1DeploymentSpec()
                {
                    Replicas = replicaCount
                }
            };
            V1Patch patchDefinition = new(deploymentPatch, V1Patch.PatchType.MergePatch);

            try
            {
                await kubernetesClientShim.PatchNamespacedDeploymentScaleAsync(kubernetesClient, patchDefinition, name, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to scale Kubernetes deployment '{name}' to {replicaCount} replica(s).", e);
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

        /// <summary>
        /// Deletes the specified deployment.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        protected async Task DeleteDeploymentAsync(String name)
        {
            try
            {
                await kubernetesClientShim.DeleteNamespacedDeploymentAsync(kubernetesClient, name, staticConfiguration.NameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to delete Kubernetes deployment '{name}'.", e);
            }
        }

        /// <summary>
        /// Scales down and then deletes the specified deployment.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="checkInterval">The interval in milliseconds between successive checks as to whether the deployment has scaled down.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the deployment hasn't scaled down.</param>
        /// <remarks>Calling DeleteDeploymentAsync() seems to ignore the 'TerminationGracePeriod' set for the deployment and force deletes it.  This method instead first scales the deployment to 0 which will observe the 'TerminationGracePeriod'.</remarks>
        protected async Task ScaleDownAndDeleteDeploymentAsync(String name, Int32 checkInterval, Int32 abortTimeout)
        {
            await ScaleDeploymentAsync(name, 0);
            await WaitForDeploymentScaleDownAsync(name, checkInterval, abortTimeout);
            await DeleteDeploymentAsync(name);
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
                    if (shardConfigurationSetPersister != null)
                    {
                        DisposeObject(shardConfigurationSetPersister);
                    }
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
