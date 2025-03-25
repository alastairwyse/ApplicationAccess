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
using ApplicationAccess.Redistribution;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Models;
using Newtonsoft.Json.Linq;
using k8s;
using k8s.Models;

namespace ApplicationAccess.Redistribution.Kubernetes
{
    /// <summary>
    /// Manages a distributed AccessManager implementation hosted in Kubernetes.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManager : IDistributedAccessManagerInstanceManager, IDisposable
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
        protected const String clusterIpServiceType = "ClusterIP";
        protected const String tcpProtocol = "TCP";
        protected const String nodeModeEnvironmentVariableName = "MODE";
        protected const String nodeModeEnvironmentVariableValue = "Launch";
        protected const String nodeListenPortEnvironmentVariableName = "LISTEN_PORT";
        protected const String nodeMinimumLogLevelEnvironmentVariableName = "MINIMUM_LOG_LEVEL";
        protected const String nodeEncodedJsonConfigurationEnvironmentVariableName = "ENCODED_JSON_CONFIGURATION";
        protected const String nodeStatusApiEndpointUrl = "/api/v1/status";
        protected const String requestsCpuKey = "cpu";
        protected const String requestsMemoryKey = "memory";
        protected const String appsettingsAccessManagerSqlDatabaseConnectionPropertyName = "AccessManagerSqlDatabaseConnection";
        protected const String appsettingsConnectionParametersPropertyName = "ConnectionParameters";
        protected const String appsettingsInitialCatalogPropertyName = "InitialCatalog";
        protected const String appsettingsEventCacheConnectionPropertyName = "EventCacheConnection";
        protected const String appsettingsHostPropertyName = "Host";
        protected const String appsettingsMetricLoggingPropertyName = "MetricLogging";
        protected const String appsettingsMetricCategorySuffixPropertyName = "MetricCategorySuffix";

        #pragma warning restore 1591

        /// <summary>Configuration for the instance manager.</summary>
        protected KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration;
        /// <summary>The client to connect to Kubernetes.</summary>
        protected k8s.Kubernetes kubernetesClient;
        /// <summary>Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to the Kubernetes client class.</summary>
        protected IKubernetesClientShim kubernetesClientShim;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        public KubernetesDistributedAccessManagerInstanceManager(KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration)
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            InitializeFields(configuration, clientConfiguration);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        /// <param name="kubernetesConfigurationFilePath">The full path to the configuration file to use to connect to the Kubernetes cluster(s).</param>
        public KubernetesDistributedAccessManagerInstanceManager(KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration, String kubernetesConfigurationFilePath)
        {
            var clientConfiguration = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubernetesConfigurationFilePath);
            InitializeFields(configuration, clientConfiguration);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        /// <param name="kubernetesClientShim">A mock <see cref="IKubernetesClientShim"/>.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public KubernetesDistributedAccessManagerInstanceManager(KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration, IKubernetesClientShim kubernetesClientShim)
        {
            this.configuration = configuration;
            kubernetesClient = null;
            this.kubernetesClientShim = kubernetesClientShim;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes fields of the class.
        /// </summary>
        /// <param name="configuration">Configuration for the instance manager.</param>
        /// <param name="clientConfiguration">The Kubernetes client configuration.</param>
        protected void InitializeFields(KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration, KubernetesClientConfiguration clientConfiguration)
        {
            this.configuration = configuration;
            kubernetesClient = new k8s.Kubernetes(clientConfiguration);
            kubernetesClientShim = new DefaultKubernetesClientShim();
        }

        protected void CreateShardGroup(DataElement dataElement, Int32 hashRangeStart, Int32 hasRangeEnd)
        {
            // TODO
            //   Might need to be async
        }

        protected void StopShardGroup(DataElement dataElement, Int32 hashRangeStart)
        {
            // TODO
            //   Might need to be async
        }

        protected void StartShardGroup(DataElement dataElement, Int32 hashRangeStart)
        {
            // TODO
            //   Might need to be async
        }

        /// <summary>
        /// Creates a Kubernetes service.
        /// </summary>
        /// <param name="appLabelValue">The name of the pod/deployment targetted by the service.</param>
        /// <param name="port">The TCP port the service should expose.</param>
        /// <param name="nameSpace">The namespace in which to create the service.</param>
        protected async Task CreateServiceAsync(String appLabelValue, UInt16 port, String nameSpace)
        {
            V1Service serviceDefinition = ServiceTemplate;
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
                throw new Exception($"Failed to create Kubernetes service for pod '{appLabelValue}'.", e);
            }
        }

        /// <summary>
        /// Creates a Kubernetes deployment for a reader node.
        /// </summary>
        /// <param name="name">The name of the deployment.</param>
        /// <param name="eventCacheServiceUrl">The URL for the service for the event cache that the reader node should consume events from.</param>
        /// <param name="nameSpace">The namespace in which to create the deployment.</param>
        protected async Task CreateReaderNodeDeploymentAsync(String name, Uri eventCacheServiceUrl, String nameSpace)
        {
            // Prepare and encode the 'appsettings.json' file contents
            JObject appsettingsContents = configuration.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate;
            List<String> requiredPaths = new()
            {
                $"{appsettingsAccessManagerSqlDatabaseConnectionPropertyName}.{appsettingsConnectionParametersPropertyName}", 
                appsettingsEventCacheConnectionPropertyName,
                appsettingsMetricLoggingPropertyName
            };
            ValidatePathsExistInJsonDocument(appsettingsContents, requiredPaths, "appsettings configuration for reader nodes");

            appsettingsContents[appsettingsAccessManagerSqlDatabaseConnectionPropertyName][appsettingsConnectionParametersPropertyName][appsettingsInitialCatalogPropertyName] = name;
            appsettingsContents[appsettingsEventCacheConnectionPropertyName][appsettingsHostPropertyName] = eventCacheServiceUrl.ToString();
            appsettingsContents[appsettingsMetricLoggingPropertyName][appsettingsMetricCategorySuffixPropertyName] = name;
            var encoder = new Base64StringEncoder();
            var encodedAppsettingsContents = encoder.Encode(appsettingsContents.ToString());

            V1Deployment deploymentDefinition = ReaderNodeDeploymentTemplate;
            deploymentDefinition.Metadata.Name = name;
            deploymentDefinition.Spec.Selector.MatchLabels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Metadata.Labels.Add(appLabel, name);
            deploymentDefinition.Spec.Template.Spec.Containers[0].Name = name;
            deploymentDefinition.Spec.Template.Spec.Containers[0].Env = new[]
            {
                new V1EnvVar { Name = nodeModeEnvironmentVariableName, Value = $"""{nodeModeEnvironmentVariableValue}""" },
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
                throw new Exception($"Failed to create Kubernetes deployment '{name}'.", e);
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
        /// <returns></returns>
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
        /// <returns></returns>
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

        /// <summary>
        /// Validates that all of the specified JSON paths exist within a JSON document.
        /// </summary>
        /// <param name="jsonDocument">The JSON document to check.</param>
        /// <param name="paths">The JSON paths to check for.</param>
        /// <param name="jsonDocumentContentsDescription">A description of the contents of the JSON document, for use in exception messages.  E.g. 'appsettings configuration for reader nodes'.</param>
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
            StringBuilder stringifiedHashRangeStartBuilder = new();
            if (hashRangeStart < 0)
            {
                stringifiedHashRangeStartBuilder.Append("n");
            }
            stringifiedHashRangeStartBuilder.Append(hashRangeStart.ToString());

            return $"{dataElement.ToString().ToLower()}-{nodeType.ToString().ToLower()}-{stringifiedHashRangeStartBuilder.ToString()}";
        }

        #endregion

        #region Kubernetes Object Templates

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
                    Type = clusterIpServiceType,
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
