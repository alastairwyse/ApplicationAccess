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
using System.Text;
using System.Threading.Tasks;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting.LaunchPreparer;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution;
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
    /// <typeparam name="TPersistentStorageCredentials">An implementation of <see cref="IPersistentStorageLoginCredentials"/> containing login credentials for persistent storage instances.</typeparam>
    public class KubernetesDistributedAccessManagerInstanceManager<TPersistentStorageCredentials> : IDistributedAccessManagerInstanceManager, IDisposable
        where TPersistentStorageCredentials : class, IPersistentStorageLoginCredentials
    {
        // TODO:
        //   If I have to introduce a specific dependency on SQL server, add this to the class description...
        //     ", and using Microsoft SQL Server for persistence."
        //   Some method to get the URL for the public service which the distopcoord(s) expose
        //   Will want to allow different resource values for user readers, group readers, etc...
        //     Might want to also do the same for startup/liveliness etc...
        //     Maybe reader node replicas too... group to group might need more
        //   For node restart, see if there is a Scale() method
        //   Do we need different log parameters per database??
        //     Maybe just use connection string... will be easier
        //   ENSURE ALL portected members are marked as protected
        //   Use ResourceQuantity.Validate() to check '120Mi' and similar values
        //   Might need to have 'staticConfig' and 'instanceConfig' parameters
        //   CreateReaderNodeDeploymentAsync() should not be setting database name parameter from deployment name
        //     BUT if I expose as a param, that's SQL server specific
        //   **** I could have this TPersisterCredientials, and then a class which takes one of those and a JObject of the appsettings, and applies the credentials to the app settings
        //     That should work for anything
        //   CreateDistributedOperationCoordinatorNodeDeploymentAsync()
        //     The way the DB 'InitialCatalog' is set is completely wrong... need to pass as a TPersisterCredientials param
        //   Fix all the tests where I'm setting the db initial catalog to be the pod name... should not be set like this
        //   Creation of ShardConfig database should be optional... if creation false need to provide TConnectionCreds for it
        //   Should be able to supply a postfix for db names like...
        //     applicationaccess-{postfix}-user-n100
        //   Add code region for 'ApplicationAccess Node and Shard Group Creation Methods'
        //   Validation for 'configuration' parameter
        //   Will need to access writer node and router from outside the cluster during split.  Might have to create temporary 'ClusterIP' services to access them.


        // NEXT
        //   Should CreateServiceAsync() take in NodeType, DataElement, etc...
        //   It's currently taking port
        //   Add methods for waiting for deployment availability (via scaling)
        //     stopping a deployment
        //     restarting a deployment
        //   Then add metrics and loggings (and testing of both) to all existing methods


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

        /// <summary>Configuration for the instance manager.</summary>
        protected KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration;
        /// <summary>The client to connect to Kubernetes.</summary>
        protected k8s.Kubernetes kubernetesClient;
        /// <summary>Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to the Kubernetes client class.</summary>
        protected IKubernetesClientShim kubernetesClientShim;
        /// <summary>Used to create new instances of persistent storage used by the distributed AccessManager implementation.</summary>
        protected IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator;
        /// <summary>Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</summary>
        protected IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer, 
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            InitializeFields(configuration, clientConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        /// <param name="kubernetesConfigurationFilePath">The full path to the configuration file to use to connect to the Kubernetes cluster(s).</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration, 
            String kubernetesConfigurationFilePath, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationFilePath);
            InitializeFields(configuration, clientConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, logger, metricLogger);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="kubernetesClientShim">A mock <see cref="IKubernetesClientShim"/>.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public KubernetesDistributedAccessManagerInstanceManager
        (
            KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IKubernetesClientShim kubernetesClientShim, 
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            this.configuration = configuration;
            kubernetesClient = null;
            this.persistentStorageCreator = persistentStorageCreator;
            this.credentialsAppSettingsConfigurer = credentialsAppSettingsConfigurer;
            this.kubernetesClientShim = kubernetesClientShim;
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        public async Task CreateDistributedAccessManagerInstanceAsync()
        {
            throw new NotImplementedException();
        }

        #region Private/Protected Methods

        protected async Task StopShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            // TODO
            //   Might need to be async
        }

        protected async Task StartShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
        {
            // TODO
            //   Might need to be async
        }

        /// <summary>
        /// Initializes fields of the class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        /// <param name="clientConfiguration">The Kubernetes client configuration.</param>
        /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
        /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        protected void InitializeFields
        (
            KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration, 
            KubernetesClientConfiguration clientConfiguration, 
            IDistributedAccessManagerPersistentStorageCreator<TPersistentStorageCredentials> persistentStorageCreator,
            IPersistentStorageCredentialsAppSettingsConfigurer<TPersistentStorageCredentials> credentialsAppSettingsConfigurer,
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        )
        {
            this.configuration = configuration;
            kubernetesClient = new k8s.Kubernetes(clientConfiguration);
            this.persistentStorageCreator = persistentStorageCreator;
            this.credentialsAppSettingsConfigurer = credentialsAppSettingsConfigurer;
            kubernetesClientShim = new DefaultKubernetesClientShim();
            this.logger = logger;
            this.metricLogger = metricLogger;
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
            return $"{configuration.PersistentStorageInstanceNamePrefix}_{dataElement.ToString().ToLower()}_{StringifyHashRangeStart(hashRangeStart)}";
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

        #endregion

        #region ApplicationAccess Node and Shard Group Creation Methods

        /// <summary>
        /// Creates a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element to create the shard group for.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        /// <param name="nameSpace">The namespace to create the shard group in.</param>
        /// <param name="persistentStorageCredentials">Optional credentials for the persistent storage used by the reader and writer nodes.  If set to null, a new persistent storage instance will be created.</param>
        protected async Task CreateShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, String nameSpace, TPersistentStorageCredentials persistentStorageCredentials=null)
        {
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
                await CreateEventCacheNodeAsync(dataElement, hashRangeStart, nameSpace);
            }
            catch
            {
                metricLogger.CancelBegin(shardGroupBeginId, new ShardGroupCreateTime());
                throw;
            }
            Uri eventCacheServiceUrl = new($"http://{GenerateNodeIdentifier(dataElement, NodeType.EventCache, hashRangeStart)}{serviceNamePostfix}:{configuration.PodPort}");

            // Create reader and writer nodes
            Task createReaderNodeTask = Task.Run(async () => await CreateReaderNodeAsync(dataElement, hashRangeStart, persistentStorageCredentials, eventCacheServiceUrl, nameSpace));
            Task createWriterNodeTask = Task.Run(async () => await CreateWriterNodeAsync(dataElement, hashRangeStart, persistentStorageCredentials, eventCacheServiceUrl, nameSpace));
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
        }

        /// <summary>
        /// Restarts all nodes of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="dataElement">The data element of the shard group.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group.</param>
        /// <param name="nameSpace">The namespace of the shard group.</param>
        protected async Task RestartShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, String nameSpace)
        {
            logger.Log(ApplicationLogging.LogLevel.Information, $"Restarting shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ShardGroupRestartTime());

            try
            {
                await ScaleDownShardGroupAsync(dataElement, hashRangeStart, nameSpace);
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new ShardGroupRestartTime());
                throw new Exception($"Error scaling down shard group for data element '{dataElement}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'.", e);
            }
            try
            {
                await ScaleUpShardGroupAsync(dataElement, hashRangeStart, nameSpace);
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
        /// <param name="nameSpace">The namespace of the shard group.</param>
        protected async Task ScaleDownShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, String nameSpace)
        {
            logger.Log(ApplicationLogging.LogLevel.Information, $"Scaling down shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ShardGroupScaleDownTime());

            // Scale down reader and writer nodes
            String readerNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Reader, hashRangeStart);
            String writerNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.Writer, hashRangeStart);
            Task scaleDownReaderNodeTask = Task.Run(async () => await ScaleDeploymentAsync(readerNodeDeploymentName, 0, nameSpace));
            Task scaleDownWriterNodeTask = Task.Run(async () => await ScaleDeploymentAsync(writerNodeDeploymentName, 0, nameSpace));
            Task waitForScaleDownReaderNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentScaleDownAsync(readerNodeDeploymentName, nameSpace, configuration.DeploymentWaitPollingInterval, (configuration.ReaderNodeConfigurationTemplate.TerminationGracePeriod * 1000) + scaleDownTerminationGracePeriodBuffer);
            });
            Task waitForScaleDownWriterNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentScaleDownAsync(writerNodeDeploymentName, nameSpace, configuration.DeploymentWaitPollingInterval, (configuration.WriterNodeConfigurationTemplate.TerminationGracePeriod * 1000) + scaleDownTerminationGracePeriodBuffer);
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
            Task scaleDownEventCacheNodeTask = Task.Run(async () => await ScaleDeploymentAsync(eventCacheNodeDeploymentName, 0, nameSpace));
            Task waitForScaleDownEventCacheNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentScaleDownAsync(eventCacheNodeDeploymentName, nameSpace, configuration.DeploymentWaitPollingInterval, (configuration.EventCacheNodeConfigurationTemplate.TerminationGracePeriod * 1000) + scaleDownTerminationGracePeriodBuffer);
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
        /// <param name="nameSpace">The namespace of the shard group.</param>
        protected async Task ScaleUpShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, String nameSpace)
        {
            logger.Log(ApplicationLogging.LogLevel.Information, $"Scaling up shard group for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ShardGroupScaleUpTime());

            // Scale up event cache node
            String eventCacheNodeDeploymentName = GenerateNodeIdentifier(dataElement, NodeType.EventCache, hashRangeStart);
            Int32 eventCacheNodeAvailabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(configuration.EventCacheNodeConfigurationTemplate);
            Task scaleUpEventCacheNodeTask = Task.Run(async () => await ScaleDeploymentAsync(eventCacheNodeDeploymentName, 1, nameSpace));
            Task waitForScaleUpEventCacheNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentAvailabilityAsync(eventCacheNodeDeploymentName, nameSpace, configuration.DeploymentWaitPollingInterval, eventCacheNodeAvailabilityWaitAbortTimeout);
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
            Int32 readerNodeAvailabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(configuration.ReaderNodeConfigurationTemplate);
            Int32 writerNodeAvailabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(configuration.WriterNodeConfigurationTemplate);
            Task scaleUpReaderNodeTask = Task.Run(async () => await ScaleDeploymentAsync(readerNodeDeploymentName, configuration.ReaderNodeConfigurationTemplate.ReplicaCount, nameSpace));
            Task scaleUpWriterNodeTask = Task.Run(async () => await ScaleDeploymentAsync(writerNodeDeploymentName, 1, nameSpace));
            Task waitForScaleUpReaderNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentAvailabilityAsync(readerNodeDeploymentName, nameSpace, configuration.DeploymentWaitPollingInterval, readerNodeAvailabilityWaitAbortTimeout);
            });
            Task waitForScaleUpWriterNodeTask = Task.Run(async () =>
            {
                await WaitForDeploymentAvailabilityAsync(writerNodeDeploymentName, nameSpace, configuration.DeploymentWaitPollingInterval, writerNodeAvailabilityWaitAbortTimeout);
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
        /// <param name="nameSpace">The namespace to create the node in.</param>
        /// <returns></returns>
        protected async Task CreateReaderNodeAsync(DataElement dataElement, Int32 hashRangeStart, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl, String nameSpace)
        {
            String deploymentName = GenerateNodeIdentifier(dataElement, NodeType.Reader, hashRangeStart);
            Func<Task> createDeploymentFunction = () => CreateReaderNodeDeploymentAsync(deploymentName, persistentStorageCredentials, eventCacheServiceUrl, nameSpace);
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(configuration.ReaderNodeConfigurationTemplate);
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating reader node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new ReaderNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, hashRangeStart, createDeploymentFunction, "reader", nameSpace, availabilityWaitAbortTimeout);
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
        /// <param name="nameSpace">The namespace to create the node in.</param>
        protected async Task CreateEventCacheNodeAsync(DataElement dataElement, Int32 hashRangeStart, String nameSpace)
        {
            String deploymentName = GenerateNodeIdentifier(dataElement, NodeType.EventCache, hashRangeStart);
            Func<Task> createDeploymentFunction = () => CreateEventCacheNodeDeploymentAsync(deploymentName, nameSpace);
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(configuration.EventCacheNodeConfigurationTemplate);
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating event cache node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new EventCacheNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, hashRangeStart, createDeploymentFunction, "event cache", nameSpace, availabilityWaitAbortTimeout);
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
        /// <param name="nameSpace">The namespace to create the node in.</param>
        /// <returns></returns>
        protected async Task CreateWriterNodeAsync(DataElement dataElement, Int32 hashRangeStart, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl, String nameSpace)
        {
            String deploymentName = GenerateNodeIdentifier(dataElement, NodeType.Writer, hashRangeStart);
            Func<Task> createDeploymentFunction = () => CreateWriterNodeDeploymentAsync(deploymentName, persistentStorageCredentials, eventCacheServiceUrl, nameSpace);
            Int32 availabilityWaitAbortTimeout = GenerateAvailabilityWaitAbortTimeout(configuration.WriterNodeConfigurationTemplate);
            logger.Log(ApplicationLogging.LogLevel.Information, $"Creating writer node for data element '{dataElement.ToString()}' and hash range start value {hashRangeStart} in namespace '{nameSpace}'...");
            Guid beginId = metricLogger.Begin(new WriterNodeCreateTime());
            try
            {
                await CreateApplicationAccessNodeAsync(deploymentName, hashRangeStart, createDeploymentFunction, "writer", nameSpace, availabilityWaitAbortTimeout);
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
        /// Creates an ApplicationAccess 'node' as part of a shard group in a distributed AccessManager implementation.
        /// </summary>
        /// <param name="deploymentName">The name of the Kubernetes deployment to create to host the node.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes managed by the shard group the node is a member of.</param>
        /// <param name="createDeploymentFunction">An async <see cref="Func{TResult}"/> which creates the Kubernetes deployment for the node.</param>
        /// <param name="nodeTypeName">The name of the type of the node (to use in exception messages, e.g. 'event cache', 'reader', etc...).</param>
        /// <param name="nameSpace">The namespace to create the node in.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the node hasn't become available.</param>
        protected async Task CreateApplicationAccessNodeAsync
        (
            String deploymentName,
            Int32 hashRangeStart,
            Func<Task> createDeploymentFunction,
            String nodeTypeName,
            String nameSpace,
            Int32 abortTimeout
        )
        {
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
                    await CreateClusterIpServiceAsync(deploymentName, configuration.PodPort, nameSpace);
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
                    await WaitForDeploymentAvailabilityAsync(deploymentName, nameSpace, configuration.DeploymentWaitPollingInterval, abortTimeout);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error waiting for {nodeTypeName} deployment '{deploymentName}' in namespace '{nameSpace}' to become available.", e);
                }
            });
            await Task.WhenAll(createDeploymentTask, createServiceTask, waitForDeploymentTask);
        }

        #endregion

        #region Kubernetes Object Creation Methods

        /// <summary>
        /// Creates a Kubernetes service of type 'ClusterIP'.
        /// </summary>
        /// <param name="appLabelValue">The name of the pod/deployment targetted by the service.</param>
        /// <param name="port">The TCP port the service should expose.</param>
        /// <param name="nameSpace">The namespace in which to create the service.</param>
        protected async Task CreateClusterIpServiceAsync(String appLabelValue, UInt16 port, String nameSpace)
        {
            V1Service serviceDefinition = ClusterIpServiceTemplate;
            serviceDefinition.Metadata.Name = $"{appLabelValue}{serviceNamePostfix}";
            serviceDefinition.Spec.Selector.Add(appLabel, appLabelValue);
            serviceDefinition.Spec.Ports[0].Port = port;
            serviceDefinition.Spec.Ports[0].TargetPort = port;

            try
            {
                await kubernetesClientShim.CreateNamespacedServiceAsync(kubernetesClient, serviceDefinition, nameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create Kubernetes '{clusterIpServiceType}' service for pod '{appLabelValue}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes service of type 'LoadBalancer'.
        /// </summary>
        /// <param name="appLabelValue">The name of the pod/deployment targetted by the service.</param>
        /// <param name="port">The TCP port the load balancer service should expose.</param>
        /// <param name="targetPort">The TCP port to use to connect to the targetted pod/deployment.</param>
        /// <param name="nameSpace">The namespace in which to create the service.</param>
        protected async Task CreateLoadBalancerServiceAsync(String appLabelValue, UInt16 port, UInt16 targetPort, String nameSpace)
        {
            V1Service serviceDefinition = LoadBalancerServiceTemplate;
            serviceDefinition.Metadata.Name = $"{appLabelValue}{externalServiceNamePostfix}";
            serviceDefinition.Spec.Selector.Add(appLabel, appLabelValue);
            serviceDefinition.Spec.Ports[0].Port = port;
            serviceDefinition.Spec.Ports[0].TargetPort = targetPort;

            try
            {
                await kubernetesClientShim.CreateNamespacedServiceAsync(kubernetesClient, serviceDefinition, nameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to create Kubernetes '{loadBalancerServiceType}' service for pod '{appLabelValue}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes deployment for a reader node.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="persistentStorageCredentials">Credentials to connect to the persistent storage for the reader node.</param>
        /// <param name="eventCacheServiceUrl">The URL for the service for the event cache that the reader node should consume events from.</param>
        /// <param name="nameSpace">The namespace in which to create the deployment.</param>
        protected async Task CreateReaderNodeDeploymentAsync(String name, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl, String nameSpace)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = configuration.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
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
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = configuration.PodPort.ToString() }, 
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = configuration.ReaderNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, nameSpace);
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
        /// <param name="nameSpace">The namespace in which to create the deployment.</param>
        protected async Task CreateEventCacheNodeDeploymentAsync(String name, String nameSpace)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = configuration.EventCacheNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
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
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = configuration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = configuration.EventCacheNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, nameSpace);
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
        /// <param name="nameSpace">The namespace in which to create the deployment.</param>
        protected async Task CreateWriterNodeDeploymentAsync(String name, TPersistentStorageCredentials persistentStorageCredentials, Uri eventCacheServiceUrl, String nameSpace)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = configuration.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
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
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = configuration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = configuration.WriterNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, nameSpace);
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
        /// <param name="nameSpace">The namespace in which to create the deployment.</param>
        protected async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync(String name, TPersistentStorageCredentials persistentStorageCredentials, String nameSpace)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
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
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = configuration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, nameSpace);
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
        /// <param name="nameSpace">The namespace in which to create the deployment.</param>
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
            Boolean routingInitiallyOn,
            String nameSpace
        )
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = configuration.DistributedOperationRouterNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
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
                new V1EnvVar { Name = nodeListenPortEnvironmentVariableName, Value = configuration.PodPort.ToString() },
                new V1EnvVar { Name = nodeMinimumLogLevelEnvironmentVariableName, Value = configuration.DistributedOperationRouterNodeConfigurationTemplate.MinimumLogLevel.ToString() },
                new V1EnvVar { Name = nodeEncodedJsonConfigurationEnvironmentVariableName, Value = encodedAppsettingsContents }
            };

            try
            {
                await kubernetesClientShim.CreateNamespacedDeploymentAsync(kubernetesClient, deploymentDefinition, nameSpace);
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
        /// <param name="nameSpace">The namespace of the deployment.</param>
        protected async Task ScaleDeploymentAsync(String name, Int32 replicaCount, String nameSpace)
        {
            if (replicaCount < 0)
                throw new ArgumentOutOfRangeException(nameof(replicaCount), $"Parameter '{nameof(replicaCount)}' with value {replicaCount} must be greater than or equal to 0.");

            // TODO: This is the only way I could find to do this, but surely there is a more robust way?
            //   Using V1Scale and V1ScaleSpec objects rather than JSON?
            String patchJson = $"{{\"spec\": {{\"replicas\": {replicaCount}}}}}";
            V1Patch patchDefinition = new(patchJson, V1Patch.PatchType.MergePatch);

            try
            {
                await kubernetesClientShim.PatchNamespacedDeploymentScaleAsync(kubernetesClient, patchDefinition, name, nameSpace);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to scale Kubernetes deployment '{name}' to {replicaCount} replicas.", e);
            }
        }

        /// <summary>
        /// Waits for the specified deployment to become available.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="nameSpace">The namespace which contains the deployment.</param>
        /// <param name="checkInterval">The interval in milliseconds between successive checks.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the deployment hasn't become available.</param>
        protected async Task WaitForDeploymentAvailabilityAsync(String name, String nameSpace, Int32 checkInterval, Int32 abortTimeout)
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
                await WaitForDeploymentPredicateAsync(nameSpace, waitForAvailabilityPredicate, checkInterval, abortTimeout);
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
        /// <param name="nameSpace">The namespace which contains the deployment.</param>
        /// <param name="checkInterval">The interval in milliseconds between successive checks.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the deployment hasn't scaled down.</param>
        protected async Task WaitForDeploymentScaleDownAsync(String name, String nameSpace, Int32 checkInterval, Int32 abortTimeout)
        {
            if (checkInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(checkInterval), $"Parameter '{nameof(checkInterval)}' with value {checkInterval} must be greater than 0.");
            if (abortTimeout < 1)
                throw new ArgumentOutOfRangeException(nameof(abortTimeout), $"Parameter '{nameof(abortTimeout)}' with value {abortTimeout} must be greater than 0.");

            Boolean foundPod = true;
            Stopwatch stopwatch = new();
            stopwatch.Start();
            do
            {
                foundPod = false;
                try
                {
                    foreach (V1Pod currentPod in await kubernetesClientShim.ListNamespacedPodAsync(kubernetesClient, nameSpace))
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
        /// <param name="nameSpace">The namespace in which to wait for the deployment predicate.</param>
        /// <param name="predicate">The <see cref="Predicate{T}"/> to wait for.</param>
        /// <param name="checkInterval">The interval in milliseconds between executions of the predicate.</param>
        /// <param name="abortTimeout">The number of milliseconds to wait before throwing an exception if the predicate hasn't returned true.</param>
        protected async Task WaitForDeploymentPredicateAsync(String nameSpace, Predicate<V1Deployment> predicate, Int32 checkInterval, Int32 abortTimeout)
        {
            if (checkInterval < 1)
                throw new ArgumentOutOfRangeException(nameof(checkInterval), $"Parameter '{nameof(checkInterval)}' with value {checkInterval} must be greater than 0.");
            if (abortTimeout < 1)
                throw new ArgumentOutOfRangeException(nameof(abortTimeout), $"Parameter '{nameof(abortTimeout)}' with value {abortTimeout} must be greater than 0.");

            Boolean predicateReturnValue = false;
            Stopwatch stopwatch = new();
            stopwatch.Start();
            do
            {
                foreach (V1Deployment currentDeployment in await kubernetesClientShim.ListNamespacedDeploymentAsync(kubernetesClient, nameSpace))
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
                    Replicas = configuration.ReaderNodeConfigurationTemplate.ReplicaCount,
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
                            TerminationGracePeriodSeconds = configuration.ReaderNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = configuration.ReaderNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {                                  
                                        new V1ContainerPort
                                        {
                                            ContainerPort = configuration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(configuration.ReaderNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(configuration.ReaderNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    LivenessProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl, 
                                            Port = configuration.PodPort
                                        },
                                        PeriodSeconds = configuration.ReaderNodeConfigurationTemplate.LivenessProbePeriod
                                    }, 
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = configuration.PodPort
                                        }, 
                                        FailureThreshold = configuration.ReaderNodeConfigurationTemplate.StartupProbeFailureThreshold, 
                                        PeriodSeconds = configuration.ReaderNodeConfigurationTemplate.StartupProbePeriod
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
                            TerminationGracePeriodSeconds = configuration.EventCacheNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = configuration.EventCacheNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = configuration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(configuration.EventCacheNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(configuration.EventCacheNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    }, 
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = configuration.PodPort
                                        },
                                        FailureThreshold = configuration.EventCacheNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = configuration.EventCacheNodeConfigurationTemplate.StartupProbePeriod
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
                                        ClaimName = configuration.WriterNodeConfigurationTemplate.PersistentVolumeClaimName
                                    }
                                }
                            }, 
                            TerminationGracePeriodSeconds = configuration.WriterNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = configuration.WriterNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = configuration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(configuration.WriterNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(configuration.WriterNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = configuration.PodPort
                                        },
                                        FailureThreshold = configuration.WriterNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = configuration.WriterNodeConfigurationTemplate.StartupProbePeriod
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
                    Replicas = configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.ReplicaCount,
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
                            TerminationGracePeriodSeconds = configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = configuration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = configuration.PodPort
                                        },
                                        FailureThreshold = configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = configuration.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbePeriod
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
                            TerminationGracePeriodSeconds = configuration.DistributedOperationRouterNodeConfigurationTemplate.TerminationGracePeriod,
                            Containers = new List<V1Container>()
                            {
                                new V1Container()
                                {
                                    Image = configuration.DistributedOperationRouterNodeConfigurationTemplate.ContainerImage,
                                    Ports = new List<V1ContainerPort>()
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = configuration.PodPort
                                        }
                                    },
                                    Resources = new V1ResourceRequirements
                                    {
                                        Requests = new Dictionary<String, ResourceQuantity>()
                                        {
                                           [requestsCpuKey] = new ResourceQuantity(configuration.DistributedOperationRouterNodeConfigurationTemplate.CpuResourceRequest),
                                           [requestsMemoryKey] = new ResourceQuantity(configuration.DistributedOperationRouterNodeConfigurationTemplate.MemoryResourceRequest)
                                        }
                                    },
                                    StartupProbe = new V1Probe
                                    {
                                        HttpGet = new V1HTTPGetAction
                                        {
                                            Path = nodeStatusApiEndpointUrl,
                                            Port = configuration.PodPort
                                        },
                                        FailureThreshold = configuration.DistributedOperationRouterNodeConfigurationTemplate.StartupProbeFailureThreshold,
                                        PeriodSeconds = configuration.DistributedOperationRouterNodeConfigurationTemplate.StartupProbePeriod
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
