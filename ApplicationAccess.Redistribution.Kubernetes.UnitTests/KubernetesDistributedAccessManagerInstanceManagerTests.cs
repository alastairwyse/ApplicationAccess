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
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting.LaunchPreparer;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using k8s.Models;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Redistribution.Kubernetes.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManagerTests
    {
        protected IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials> mockPersistentStorageCreator;
        protected IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> mockAppSettingsConfigurer;
        protected IKubernetesClientShim mockKubernetesClientShim;
        protected IApplicationLogger mockApplicationLogger;
        protected IMetricLogger mockMetricLogger;
        protected KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers testKubernetesDistributedAccessManagerInstanceManager;

        [SetUp]
        protected void SetUp()
        {
            mockPersistentStorageCreator = Substitute.For<IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials>>();
            mockAppSettingsConfigurer = Substitute.For<IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials>>();
            mockKubernetesClientShim = Substitute.For<IKubernetesClientShim>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateConfiguration(),
                mockPersistentStorageCreator,
                mockAppSettingsConfigurer, 
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
        }

        [Test]
        public async Task CreateClusterIpServiceAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            String appLabelValue = "user-eventcache-n2147483648";
            UInt16 port = 5000;
            String nameSpace = "default";
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), nameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateClusterIpServiceAsync(appLabelValue, port, nameSpace);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create Kubernetes 'ClusterIP' service for pod '{appLabelValue}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateClusterIpServiceAsync()
        {
            String appLabelValue = "user-eventcache-n2147483648";
            UInt16 port = 5000;
            String nameSpace = "default";
            V1Service capturedServiceDefinition = null;
            await mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Do<V1Service>(argumentValue => capturedServiceDefinition = argumentValue), nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateClusterIpServiceAsync(appLabelValue, port, nameSpace);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), nameSpace);
            Assert.AreEqual($"{V1Service.KubeGroup}/{V1Service.KubeApiVersion}", capturedServiceDefinition.ApiVersion);
            Assert.AreEqual(V1Service.KubeKind, capturedServiceDefinition.Kind);
            Assert.AreEqual($"{appLabelValue}-service", capturedServiceDefinition.Metadata.Name);
            Assert.AreEqual("ClusterIP", capturedServiceDefinition.Spec.Type);
            Assert.AreEqual(1, capturedServiceDefinition.Spec.Selector.Count);
            Assert.IsTrue(capturedServiceDefinition.Spec.Selector.ContainsKey("app"));
            Assert.AreEqual(appLabelValue, capturedServiceDefinition.Spec.Selector["app"]);
            Assert.AreEqual(1, capturedServiceDefinition.Spec.Ports.Count);
            Assert.AreEqual("TCP", capturedServiceDefinition.Spec.Ports[0].Protocol);
            Assert.AreEqual(port, capturedServiceDefinition.Spec.Ports[0].Port);
            Assert.AreEqual($"""{port}""", capturedServiceDefinition.Spec.Ports[0].TargetPort.Value);
        }

        [Test]
        public async Task CreateLoadBalancerServiceAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            String appLabelValue = "operation-coordinator";
            UInt16 port = 7000;
            UInt16 targetPort = 5000;
            String nameSpace = "default";
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), nameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateLoadBalancerServiceAsync(appLabelValue, port, targetPort, nameSpace);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create Kubernetes 'LoadBalancer' service for pod '{appLabelValue}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateLoadBalancerServiceAsync()
        {
            String appLabelValue = "operation-coordinator";
            UInt16 port = 7000;
            UInt16 targetPort = 5000;
            String nameSpace = "default";
            V1Service capturedServiceDefinition = null;
            await mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Do<V1Service>(argumentValue => capturedServiceDefinition = argumentValue), nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateLoadBalancerServiceAsync(appLabelValue, port, targetPort, nameSpace);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), nameSpace);
            Assert.AreEqual($"{V1Service.KubeGroup}/{V1Service.KubeApiVersion}", capturedServiceDefinition.ApiVersion);
            Assert.AreEqual(V1Service.KubeKind, capturedServiceDefinition.Kind);
            Assert.AreEqual($"{appLabelValue}-service", capturedServiceDefinition.Metadata.Name);
            Assert.AreEqual("LoadBalancer", capturedServiceDefinition.Spec.Type);
            Assert.AreEqual(1, capturedServiceDefinition.Spec.Selector.Count);
            Assert.IsTrue(capturedServiceDefinition.Spec.Selector.ContainsKey("app"));
            Assert.AreEqual(appLabelValue, capturedServiceDefinition.Spec.Selector["app"]);
            Assert.AreEqual(1, capturedServiceDefinition.Spec.Ports.Count);
            Assert.AreEqual("TCP", capturedServiceDefinition.Spec.Ports[0].Protocol);
            Assert.AreEqual(port, capturedServiceDefinition.Spec.Ports[0].Port);
            Assert.AreEqual($"""{targetPort}""", capturedServiceDefinition.Spec.Ports[0].TargetPort.Value);
        }

        [Test]
        public void CreateReaderNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "user-reader-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            String nameSpace = "default";
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerConfiguration config = CreateConfiguration();
            config.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventCacheConnection");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventCacheConnection' was not found in JSON document containing appsettings configuration for reader nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateConfiguration();
            config.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for reader nodes."));
        }

        [Test]
        public async Task CreateReaderNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-reader-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            String nameSpace = "default";
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create reader node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateReaderNodeDeploymentAsync()
        {
            String name = "user-reader-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            String nameSpace = "default";
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateReaderNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["EventCacheConnection"]["Host"] = eventCacheServiceUrl.ToString();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
            ));
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);

            mockAppSettingsConfigurer.Received(1).ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Any<JObject>());
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.AreEqual($"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}", capturedDeploymentDefinition.ApiVersion);
            Assert.AreEqual(V1Deployment.KubeKind, capturedDeploymentDefinition.Kind);
            Assert.AreEqual(name, capturedDeploymentDefinition.Metadata.Name);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Replicas);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Selector.MatchLabels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Selector.MatchLabels["app"]);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Template.Metadata.Labels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Metadata.Labels["app"]);
            Assert.AreEqual(3600, capturedDeploymentDefinition.Spec.Template.Spec.TerminationGracePeriodSeconds);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers.Count);
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Name);
            Assert.AreEqual("applicationaccess/distributedreader:20250203-0900", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Image);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports.Count);
            Assert.AreEqual(5000, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports[0].ContainerPort);
            Assert.AreEqual(4, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env.Count);
            IList<V1EnvVar> deploymentEnvironmentVarriables = capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env;
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MODE", "Launch")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("LISTEN_PORT", "5000")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MINIMUM_LOG_LEVEL", "Warning")));
            ValidateEncodedJsonEnvironmentVariable(deploymentEnvironmentVarriables, expectedJsonConfiguration);
            Assert.AreEqual(2, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests.Count);
            Assert.AreEqual(new ResourceQuantity("100m"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["cpu"]);
            Assert.AreEqual(new ResourceQuantity("120Mi"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["memory"]);
            Assert.AreEqual("/api/v1/status", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].LivenessProbe.HttpGet.Path);
            Assert.AreEqual($"""5000""", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].LivenessProbe.HttpGet.Port.Value);
            Assert.AreEqual(10, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].LivenessProbe.PeriodSeconds);
            Assert.AreEqual("/api/v1/status", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.HttpGet.Path);
            Assert.AreEqual($"""5000""", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.HttpGet.Port.Value);
            Assert.AreEqual(12, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.FailureThreshold);
            Assert.AreEqual(11, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.PeriodSeconds);
        }

        [Test]
        public void CreateEventCacheNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "user-eventcache-n2147483648";
            String nameSpace = "default";
            KubernetesDistributedAccessManagerInstanceManagerConfiguration config = CreateConfiguration();
            config.EventCacheNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeDeploymentAsync(name, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for event cache nodes."));
        }

        [Test]
        public async Task CreateEventCacheNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-eventcache-n2147483648";
            String nameSpace = "default";
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeDeploymentAsync(name, nameSpace);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create event cache node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateEventCacheNodeDeploymentAsync()
        {
            String name = "user-eventcache-n2147483648";
            String nameSpace = "default";
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateEventCacheNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeDeploymentAsync(name, nameSpace);

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.AreEqual($"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}", capturedDeploymentDefinition.ApiVersion);
            Assert.AreEqual(V1Deployment.KubeKind, capturedDeploymentDefinition.Kind);
            Assert.AreEqual(name, capturedDeploymentDefinition.Metadata.Name);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Replicas);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Selector.MatchLabels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Selector.MatchLabels["app"]);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Template.Metadata.Labels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Metadata.Labels["app"]);
            Assert.AreEqual(1800, capturedDeploymentDefinition.Spec.Template.Spec.TerminationGracePeriodSeconds);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers.Count);
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Name);
            Assert.AreEqual("applicationaccess/eventcache:20250203-0900", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Image);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports.Count);
            Assert.AreEqual(5000, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports[0].ContainerPort);
            Assert.AreEqual(4, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env.Count);
            IList<V1EnvVar> deploymentEnvironmentVarriables = capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env;
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MODE", "Launch")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("LISTEN_PORT", "5000")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MINIMUM_LOG_LEVEL", "Information")));
            ValidateEncodedJsonEnvironmentVariable(deploymentEnvironmentVarriables, expectedJsonConfiguration);
            Assert.AreEqual(2, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests.Count);
            Assert.AreEqual(new ResourceQuantity("50m"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["cpu"]);
            Assert.AreEqual(new ResourceQuantity("60Mi"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["memory"]);
        }

        [Test]
        public void CreateWriterNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "user-writer-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            String nameSpace = "default";
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerConfiguration config = CreateConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventPersistence");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventPersistence' was not found in JSON document containing appsettings configuration for writer nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventCacheConnection");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventCacheConnection' was not found in JSON document containing appsettings configuration for writer nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for writer nodes."));
        }

        [Test]
        public async Task CreateWriterNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-writer-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            String nameSpace = "default";
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create writer node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateWriterNodeDeploymentAsync()
        {
            String name = "user-writer-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            String nameSpace = "default";
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateWriterNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["EventPersistence"]["EventPersisterBackupFilePath"] = "/eventbackup/user-writer-n2147483648-eventbackup.json";
            expectedJsonConfiguration["EventCacheConnection"]["Host"] = eventCacheServiceUrl.ToString();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
            ));
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl, nameSpace);

            mockAppSettingsConfigurer.Received(1).ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Any<JObject>());
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.AreEqual($"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}", capturedDeploymentDefinition.ApiVersion);
            Assert.AreEqual(V1Deployment.KubeKind, capturedDeploymentDefinition.Kind);
            Assert.AreEqual(name, capturedDeploymentDefinition.Metadata.Name);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Replicas);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Selector.MatchLabels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Selector.MatchLabels["app"]);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Template.Metadata.Labels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Metadata.Labels["app"]);
            Assert.AreEqual("eventbackup-storage", capturedDeploymentDefinition.Spec.Template.Spec.Volumes[0].Name);
            Assert.AreEqual("eventbackup-claim", capturedDeploymentDefinition.Spec.Template.Spec.Volumes[0].PersistentVolumeClaim.ClaimName);
            Assert.AreEqual(1200, capturedDeploymentDefinition.Spec.Template.Spec.TerminationGracePeriodSeconds);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers.Count);
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Name);
            Assert.AreEqual("applicationaccess/distributedwriter:20250203-0900", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Image);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports.Count);
            Assert.AreEqual(5000, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports[0].ContainerPort);
            Assert.AreEqual(4, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env.Count);
            IList<V1EnvVar> deploymentEnvironmentVarriables = capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env;
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MODE", "Launch")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("LISTEN_PORT", "5000")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MINIMUM_LOG_LEVEL", "Critical")));
            ValidateEncodedJsonEnvironmentVariable(deploymentEnvironmentVarriables, expectedJsonConfiguration);
            Assert.AreEqual(2, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests.Count);
            Assert.AreEqual(new ResourceQuantity("200m"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["cpu"]);
            Assert.AreEqual(new ResourceQuantity("240Mi"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["memory"]);
            Assert.AreEqual("/eventbackup", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].VolumeMounts[0].MountPath);
            Assert.AreEqual("eventbackup-storage", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].VolumeMounts[0].Name);
        }

        [Test]
        public void CreateDistributedOperationCoordinatorNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "operation-coordinator";
            TestPersistentStorageLoginCredentials storageCredentials = new("AccessManagerConfiguration");
            String nameSpace = "default";
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerConfiguration config = CreateConfiguration();
            config.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, storageCredentials, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for distributed operation coordinator nodes."));
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "operation-coordinator";
            TestPersistentStorageLoginCredentials storageCredentials = new("AccessManagerConfiguration");
            String nameSpace = "default";
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, storageCredentials, nameSpace);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create distributed operation coordinator node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync()
        {
            String name = "operation-coordinator";
            TestPersistentStorageLoginCredentials storageCredentials = new("AccessManagerConfiguration");
            String nameSpace = "default";
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateDistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
            ));
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, storageCredentials, nameSpace);

            mockAppSettingsConfigurer.Received(1).ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Any<JObject>());
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.AreEqual($"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}", capturedDeploymentDefinition.ApiVersion);
            Assert.AreEqual(V1Deployment.KubeKind, capturedDeploymentDefinition.Kind);
            Assert.AreEqual(name, capturedDeploymentDefinition.Metadata.Name);
            Assert.AreEqual(3, capturedDeploymentDefinition.Spec.Replicas);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Selector.MatchLabels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Selector.MatchLabels["app"]);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Template.Metadata.Labels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Metadata.Labels["app"]);
            Assert.AreEqual(60, capturedDeploymentDefinition.Spec.Template.Spec.TerminationGracePeriodSeconds);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers.Count);
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Name);
            Assert.AreEqual("applicationaccess/distributedoperationcoordinator:20250203-0900", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Image);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports.Count);
            Assert.AreEqual(5000, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports[0].ContainerPort);
            Assert.AreEqual(4, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env.Count);
            IList<V1EnvVar> deploymentEnvironmentVarriables = capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env;
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MODE", "Launch")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("LISTEN_PORT", "5000")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MINIMUM_LOG_LEVEL", "Warning")));
            ValidateEncodedJsonEnvironmentVariable(deploymentEnvironmentVarriables, expectedJsonConfiguration);
            Assert.AreEqual(2, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests.Count);
            Assert.AreEqual(new ResourceQuantity("500m"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["cpu"]);
            Assert.AreEqual(new ResourceQuantity("600Mi"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["memory"]);
        }

        [Test]
        public void CreateDistributedOperationRouterNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "operation-router";
            String nameSpace = "default";
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerConfiguration config = CreateConfiguration();
            config.DistributedOperationRouterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("ShardRouting");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeDeploymentAsync
                (
                    name,
                    DataElement.User,
                    new Uri("http://user-reader-n2147483648-service:5000"),
                    new Uri("http://user-writer-n2147483648-service:5000"),
                    -2_147_483_648,
                    -1,
                    new Uri("http://user-reader-0-service:5000"),
                    new Uri("http://user-writer-0-service:5000"),
                    0,
                    2_147_483_647,
                    true,
                    nameSpace
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'ShardRouting' was not found in JSON document containing appsettings configuration for distributed operation router nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateConfiguration();
            config.DistributedOperationRouterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, mockPersistentStorageCreator, mockAppSettingsConfigurer, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeDeploymentAsync
                (
                    name,
                    DataElement.User,
                    new Uri("http://user-reader-n2147483648-service:5000"),
                    new Uri("http://user-writer-n2147483648-service:5000"),
                    -2_147_483_648,
                    -1,
                    new Uri("http://user-reader-0-service:5000"),
                    new Uri("http://user-writer-0-service:5000"),
                    0,
                    2_147_483_647,
                    true,
                    nameSpace
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for distributed operation router nodes."));
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "operation-router";
            String nameSpace = "default";
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeDeploymentAsync
                (
                    name,
                    DataElement.User,
                    new Uri("http://user-reader-n2147483648-service:5000"),
                    new Uri("http://user-writer-n2147483648-service:5000"),
                    -2_147_483_648, 
                    -1,
                    new Uri("http://user-reader-0-service:5000"),
                    new Uri("http://user-writer-0-service:5000"),
                    0,
                    2_147_483_647,
                    true,
                    nameSpace
                );
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create distributed operation router node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeDeploymentAsync()
        {
            String name = "operation-router";
            String nameSpace = "default";
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateDistributedOperationRouterNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["ShardRouting"]["DataElementType"] = "User";
            expectedJsonConfiguration["ShardRouting"]["SourceQueryShardBaseUrl"] = "http://user-reader-n2147483648-service:5000/";
            expectedJsonConfiguration["ShardRouting"]["SourceEventShardBaseUrl"] = "http://user-writer-n2147483648-service:5000/";
            expectedJsonConfiguration["ShardRouting"]["SourceShardHashRangeStart"] = -2_147_483_648;
            expectedJsonConfiguration["ShardRouting"]["SourceShardHashRangeEnd"] = -1;
            expectedJsonConfiguration["ShardRouting"]["TargetQueryShardBaseUrl"] = "http://user-reader-0-service:5000/";
            expectedJsonConfiguration["ShardRouting"]["TargetEventShardBaseUrl"] = "http://user-writer-0-service:5000/";
            expectedJsonConfiguration["ShardRouting"]["TargetShardHashRangeStart"] = 0;
            expectedJsonConfiguration["ShardRouting"]["TargetShardHashRangeEnd"] = 2_147_483_647;
            expectedJsonConfiguration["ShardRouting"]["RoutingInitiallyOn"] = true;
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeDeploymentAsync
            (
                name,
                DataElement.User,
                new Uri("http://user-reader-n2147483648-service:5000"),
                new Uri("http://user-writer-n2147483648-service:5000"),
                -2_147_483_648,
                -1,
                new Uri("http://user-reader-0-service:5000"),
                new Uri("http://user-writer-0-service:5000"),
                0,
                2_147_483_647,
                true,
                nameSpace
            );

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), nameSpace);
            Assert.AreEqual($"{V1Deployment.KubeGroup}/{V1Deployment.KubeApiVersion}", capturedDeploymentDefinition.ApiVersion);
            Assert.AreEqual(V1Deployment.KubeKind, capturedDeploymentDefinition.Kind);
            Assert.AreEqual(name, capturedDeploymentDefinition.Metadata.Name);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Replicas);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Selector.MatchLabels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Selector.MatchLabels["app"]);
            Assert.IsTrue(capturedDeploymentDefinition.Spec.Template.Metadata.Labels.ContainsKey("app"));
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Metadata.Labels["app"]);
            Assert.AreEqual(30, capturedDeploymentDefinition.Spec.Template.Spec.TerminationGracePeriodSeconds);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers.Count);
            Assert.AreEqual(name, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Name);
            Assert.AreEqual("applicationaccess/distributedoperationrouter:20250203-0900", capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Image);
            Assert.AreEqual(1, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports.Count);
            Assert.AreEqual(5000, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Ports[0].ContainerPort);
            Assert.AreEqual(4, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env.Count);
            IList<V1EnvVar> deploymentEnvironmentVarriables = capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Env;
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MODE", "Launch")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("LISTEN_PORT", "5000")));
            Assert.IsTrue(EnvironmentVariablesContainsKeyValuePair(deploymentEnvironmentVarriables, KeyValuePair.Create("MINIMUM_LOG_LEVEL", "Critical")));
            ValidateEncodedJsonEnvironmentVariable(deploymentEnvironmentVarriables, expectedJsonConfiguration);
            Assert.AreEqual(2, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests.Count);
            Assert.AreEqual(new ResourceQuantity("400m"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["cpu"]);
            Assert.AreEqual(new ResourceQuantity("450Mi"), capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].Resources.Requests["memory"]);
        }

        [Test]
        public void WaitForDeploymentPredicateAsync_CheckIntervalParameter0()
        {
            String nameSpace = "default";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync(nameSpace, (deployment) => { return true; }, 0, 2000);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'checkInterval' with value 0 must be greater than 0."));
            Assert.AreEqual("checkInterval", e.ParamName);
        }

        [Test]
        public void WaitForDeploymentPredicateAsync_AbortTimeoutParameter0()
        {
            String nameSpace = "default";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync(nameSpace, (deployment) => { return true; }, 100, 0);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'abortTimeout' with value 0 must be greater than 0."));
            Assert.AreEqual("abortTimeout", e.ParamName);
        }

        [Test]
        public void WaitForDeploymentPredicateAsync_AbortTimeoutExpires()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                { 
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "OtherDeployment" } }
                }
            );
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, nameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<DeploymentPredicateWaitTimeoutExpiredException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync(nameSpace, (deployment) => { return false; }, 100, 1000);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 1000 milliseconds expired while waiting for deployment predicate to return true."));
            Assert.AreEqual(1000, e.Timeout);


            returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "OtherDeployment" } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = name } },
                }
            ); 
            Predicate<V1Deployment> predicate = (V1Deployment deployment) =>
            {
                if (deployment.Name() == name)
                {
                    return false;
                }
                else
                {
                    return false;
                }
            };
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, nameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            e = Assert.ThrowsAsync<DeploymentPredicateWaitTimeoutExpiredException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync(nameSpace, (deployment) => { return false; }, 100, 1000);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 1000 milliseconds expired while waiting for deployment predicate to return true."));
            Assert.AreEqual(1000, e.Timeout);
        }

        [Test]
        public async Task WaitForDeploymentPredicateAsync()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "OtherDeployment" } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = name } },
                }
            );
            Int32 predicateExecutionCount = 0;
            Predicate<V1Deployment> predicate = (V1Deployment deployment) =>
            {
                if (deployment.Name() == name)
                {
                    predicateExecutionCount++;
                    if (predicateExecutionCount == 5)
                    {
                        return true;
                    }
                }
                return false;
            };
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, nameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync(nameSpace, predicate, 100, 1000);

            await mockKubernetesClientShim.Received(5).ListNamespacedDeploymentAsync(null, nameSpace);
        }

        [Test]
        public void WaitForDeploymentAvailabilityAsync_AbortTimeoutExpires()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "OtherDeployment" } },
                    new V1Deployment() 
                    { 
                        Metadata = new V1ObjectMeta() { Name = name }, 
                        Status = new V1DeploymentStatus { AvailableReplicas = null }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, nameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentAvailabilityAsync(name, nameSpace, 100, 500);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 500 milliseconds expired while waiting for Kubernetes deployment 'user-reader-n2147483648' to become available."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Timeout value of 500 milliseconds expired while waiting for deployment predicate to return true."));
        }

        [Test]
        public async Task WaitForDeploymentAvailabilityAsync_ExceptionGettingDeployments()
        {
            var mockException = new Exception("Mock exception");
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, nameSpace).Returns(Task.FromException<V1DeploymentList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentAvailabilityAsync(name, nameSpace, 100, 500);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-reader-n2147483648' to become available."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task WaitForDeploymentAvailabilityAsync()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            V1DeploymentList unavailableDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "OtherDeployment" } },
                    new V1Deployment()
                    {
                        Metadata = new V1ObjectMeta() { Name = name },
                        Status = new V1DeploymentStatus { AvailableReplicas = null }
                    }
                }
            );
            V1DeploymentList availableDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "OtherDeployment" } },
                    new V1Deployment()
                    {
                        Metadata = new V1ObjectMeta() { Name = name },
                        Status = new V1DeploymentStatus { AvailableReplicas = 1 }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, nameSpace).Returns
            (
                Task.FromResult<V1DeploymentList>(unavailableDeployments),
                Task.FromResult<V1DeploymentList>(unavailableDeployments),
                Task.FromResult<V1DeploymentList>(unavailableDeployments),
                Task.FromResult<V1DeploymentList>(availableDeployments)
            );

            await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentAvailabilityAsync(name, nameSpace, 100, 1000);

            await mockKubernetesClientShim.Received(4).ListNamespacedDeploymentAsync(null, nameSpace);
        }

        [Test]
        public void WaitForDeploymentScaleDownAsync_CheckIntervalParameter0()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, nameSpace, 0, 2000);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'checkInterval' with value 0 must be greater than 0."));
            Assert.AreEqual("checkInterval", e.ParamName);
        }

        [Test]
        public void WaitForDeploymentScaleDownAsync_AbortTimeoutParameter0()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, nameSpace, 100, 0);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'abortTimeout' with value 0 must be greater than 0."));
            Assert.AreEqual("abortTimeout", e.ParamName);
        }

        [Test]
        public async Task WaitForDeploymentScaleDownAsync_ExceptionGettingPods()
        {
            var mockException = new Exception("Mock exception");
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            mockKubernetesClientShim.ListNamespacedPodAsync(null, nameSpace).Returns(Task.FromException<V1PodList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, nameSpace, 100, 2000);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedPodAsync(null, nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-reader-n2147483648' to scale down."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void WaitForDeploymentScaleDownAsync_AbortTimeoutExpires()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            V1PodList returnPods = new
            (
                new List<V1Pod>
                {
                    new V1Pod() { Metadata = new V1ObjectMeta() { Name = "OtherPod" } },
                    new V1Pod() { Metadata = new V1ObjectMeta() { Name = name } }
                }
            );
            mockKubernetesClientShim.ListNamespacedPodAsync(null, nameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, nameSpace, 100, 500);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 500 milliseconds expired while waiting for Kubernetes deployment 'user-reader-n2147483648' to scale down."));
        }

        [Test]
        public async Task WaitForDeploymentScaleDownAsync()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            V1PodList podsBeforeScaleDown = new
            (
                new List<V1Pod>
                {
                    new V1Pod() { Metadata = new V1ObjectMeta() { Name = "OtherPod" } },
                    new V1Pod() { Metadata = new V1ObjectMeta() { Name = name } }
                }
            );
            V1PodList podsAfterScaleDown = new
            (
                new List<V1Pod>
                {
                    new V1Pod() { Metadata = new V1ObjectMeta() { Name = "OtherPod" } }
                }
            );
            mockKubernetesClientShim.ListNamespacedPodAsync(null, nameSpace).Returns
            (
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsAfterScaleDown)
            );

            await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, nameSpace, 100, 1000);

            await mockKubernetesClientShim.Received(4).ListNamespacedPodAsync(null, nameSpace);
        }

        [Test]
        public void ScaleDeploymentAsync_ReplicaCountParameter0()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDeploymentAsync(name, -1, nameSpace);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'replicaCount' with value -1 must be greater than or equal to 0."));
            Assert.AreEqual("replicaCount", e.ParamName);
        }

        [Test]
        public async Task ScaleDeploymentAsync_ExceptionPatchingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, nameSpace).Returns(Task.FromException<V1Scale>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDeploymentAsync(name, 0, nameSpace);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, nameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-reader-n2147483648' to 0 replicas."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDeploymentAsync()
        {
            String nameSpace = "default";
            String name = "user-reader-n2147483648";
            String expectedPatchContentString = $"{{\"spec\": {{\"replicas\": 3}}}}";
            V1Patch capturedPatchDefinition = null;
            await mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Do<V1Patch>(argumentValue => capturedPatchDefinition = argumentValue), name, nameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.ScaleDeploymentAsync(name, 3, nameSpace);

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, nameSpace);
            Assert.AreEqual(expectedPatchContentString, capturedPatchDefinition.Content);
        }

        [Test]
        public async Task IntegrationTests_REMOVETHIS()
        {

        }

        #region Private/Protected Methods

        /// <summary>
        /// Checks whether the specific list of environment variables contains a key value pair.
        /// </summary>
        /// <param name="environmentVariables">The environment variables to check.</param>
        /// <param name="keyValuePair">The key/value pair to check for.</param>
        /// <returns>Whether the specific list of environment variables contains a key value pair.</returns>
        protected Boolean EnvironmentVariablesContainsKeyValuePair(IList<V1EnvVar> environmentVariables, KeyValuePair<String, String> keyValuePair)
        {
            foreach (V1EnvVar currentEnvironmentVariable in environmentVariables)
            {
                if (currentEnvironmentVariable.Name == keyValuePair.Key)
                {
                    if (currentEnvironmentVariable.Value == keyValuePair.Value)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Validates that environment variables contain an encoded JSON configuration variable which contains JSON matching that specified.
        /// </summary>
        /// <param name="environmentVariables">The environment variables to check.</param>
        /// <param name="expectedJson">The JSON the encoded JSON configuration variable is expected to contain.</param>
        protected void ValidateEncodedJsonEnvironmentVariable(IList<V1EnvVar> environmentVariables, JObject expectedJson)
        {
            const String encodedJsonConfigurationVariableName = "ENCODED_JSON_CONFIGURATION";

            Boolean foundEnvironmentVariable = false;
            String encodedJsonConfigurationString = null;
            foreach (V1EnvVar currentEnvironmentVariable in environmentVariables)
            {
                if (currentEnvironmentVariable.Name == encodedJsonConfigurationVariableName)
                {
                    encodedJsonConfigurationString = currentEnvironmentVariable.Value;
                    foundEnvironmentVariable = true;
                    break;
                }
            }
            if (foundEnvironmentVariable == false)
            {
                Assert.Fail($"Environment variables did not contain a variable with name '{encodedJsonConfigurationVariableName}'.");
            }

            var encoder = new Base64StringEncoder();
            String decodedJsonConfigurationString = null;
            try
            {
                decodedJsonConfigurationString = encoder.Decode(encodedJsonConfigurationString);
            }
            catch (Exception e)
            {
                Assert.Fail($"Failed to decode encoded JSON configuration value {encodedJsonConfigurationString}.  {e.Message}");
            }
            JObject decodedJsonConfiguration = null;
            try
            {
                decodedJsonConfiguration = JObject.Parse(decodedJsonConfigurationString);
            }
            catch (Exception e)
            {
                Assert.Fail($"Failed to parse decoded configuration as JSON.  {e.Message}");
            }

            Assert.AreEqual(expectedJson, decodedJsonConfiguration);
        }

        /// <summary>
        /// Creates a test <see cref="KubernetesDistributedAccessManagerInstanceManagerConfiguration"/> instance.
        /// </summary>
        /// <returns>The test <see cref="KubernetesDistributedAccessManagerInstanceManagerConfiguration"/> instance.</returns>
        protected KubernetesDistributedAccessManagerInstanceManagerConfiguration CreateConfiguration()
        {
            KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration = new()
            {
                PodPort = 5000,
                PersistentStorageInstanceNameSuffix = "test", 
                ReaderNodeConfigurationTemplate = new ReaderNodeConfiguration
                {
                    ReplicaCount = 1,
                    TerminationGracePeriod = 3600,
                    ContainerImage = "applicationaccess/distributedreader:20250203-0900", 
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning,
                    AppSettingsConfigurationTemplate = CreateReaderNodeAppSettingsConfigurationTemplate(),
                    CpuResourceRequest = "100m", 
                    MemoryResourceRequest = "120Mi", 
                    LivenessProbePeriod = 10, 
                    StartupProbeFailureThreshold = 12, 
                    StartupProbePeriod = 11
                },
                EventCacheNodeConfigurationTemplate = new EventCacheNodeConfiguration
                {
                    TerminationGracePeriod = 1800,
                    ContainerImage = "applicationaccess/eventcache:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
                    AppSettingsConfigurationTemplate = CreateEventCacheNodeAppSettingsConfigurationTemplate(),
                    CpuResourceRequest = "50m",
                    MemoryResourceRequest = "60Mi"
                },
                WriterNodeConfigurationTemplate = new WriterNodeConfiguration
                {
                    PersistentVolumeClaimName = "eventbackup-claim", 
                    TerminationGracePeriod = 1200,
                    ContainerImage = "applicationaccess/distributedwriter:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Critical,
                    AppSettingsConfigurationTemplate = CreateWriterNodeAppSettingsConfigurationTemplate(),
                    CpuResourceRequest = "200m",
                    MemoryResourceRequest = "240Mi"
                }, 
                DistributedOperationCoordinatorNodeConfigurationTemplate = new DistributedOperationCoordinatorNodeConfiguration
                {
                    ReplicaCount = 3,
                    TerminationGracePeriod = 60,
                    ContainerImage = "applicationaccess/distributedoperationcoordinator:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning,
                    AppSettingsConfigurationTemplate = CreateDistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate(),
                    CpuResourceRequest = "500m",
                    MemoryResourceRequest = "600Mi"
                },
                DistributedOperationRouterNodeConfigurationTemplate = new DistributedOperationRouterNodeConfiguration
                {
                    TerminationGracePeriod = 30,
                    ContainerImage = "applicationaccess/distributedoperationrouter:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Critical,
                    AppSettingsConfigurationTemplate = CreateDistributedOperationRouterNodeAppSettingsConfigurationTemplate(),
                    CpuResourceRequest = "400m",
                    MemoryResourceRequest = "450Mi"
                }
            };

            return configuration;
        }

        /// <summary>
        /// Creates a base/template for the 'appsettings.json' file contents for reader nodes.
        /// </summary>
        /// <returns>A base/template for the 'appsettings.json' file contents for reader nodes.</returns>
        protected JObject CreateReaderNodeAppSettingsConfigurationTemplate()
        {
            String stringifiedAppSettings = @"
            {
                ""AccessManagerSqlDatabaseConnection"": {
                    ""DatabaseType"": ""SqlServer"",
                    ""ConnectionParameters"": {
                        ""RetryCount"": 10,
                        ""RetryInterval"": 20,
                        ""OperationTimeout"": 0
                    }
                },
                ""EventCacheConnection"": {
                    ""RetryCount"": 10,
                    ""RetryInterval"": 5
                },
                ""EventCacheRefresh"": {
                    ""RefreshInterval"": 30000
                },
                ""MetricLogging"": {
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 500,
                        ""DequeueOperationLoopInterval"": 30000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""DataSource"": ""127.0.0.1"",
                            ""InitialCatalog"": ""ApplicationMetrics"",
                            ""UserId"": ""sa"",
                            ""Password"": ""password"",
                            ""RetryCount"": 10,
                            ""RetryInterval"": 20,
                            ""OperationTimeout"": 0
                        }
                    }
                }
            }";

            return JObject.Parse(stringifiedAppSettings);
        }

        /// <summary>
        /// Creates a base/template for the 'appsettings.json' file contents for event cache nodes.
        /// </summary>
        /// <returns>A base/template for the 'appsettings.json' file contents for event cache nodes.</returns>
        protected JObject CreateEventCacheNodeAppSettingsConfigurationTemplate()
        {
            String stringifiedAppSettings = @"
            {
                ""EventCaching"": {
                    ""CachedEventCount"": 5000
                },
                ""MetricLogging"": {
                    ""MetricLoggingEnabled"": false,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedBufferProcessor"",
                        ""BufferSizeLimit"": 501,
                        ""DequeueOperationLoopInterval"": 30001,
                        ""BufferProcessingFailureAction"": ""DisableMetricLogging""
                    },
                    ""MetricsSqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""DataSource"": ""127.0.0.1"",
                            ""InitialCatalog"": ""ApplicationMetrics"",
                            ""UserId"": ""sa"",
                            ""Password"": ""password"",
                            ""RetryCount"": 5,
                            ""RetryInterval"": 10,
                            ""OperationTimeout"": 60000
                        }
                    }
                }
            }";

            return JObject.Parse(stringifiedAppSettings);
        }

        /// <summary>
        /// Creates a base/template for the 'appsettings.json' file contents for writer nodes.
        /// </summary>
        /// <returns>A base/template for the 'appsettings.json' file contents for writer nodes.</returns>
        protected JObject CreateWriterNodeAppSettingsConfigurationTemplate()
        {
            String stringifiedAppSettings = @"
            {
                ""AccessManagerSqlDatabaseConnection"": {
                    ""DatabaseType"": ""SqlServer"",
                    ""ConnectionParameters"": {
                        ""RetryCount"": 4,
                        ""RetryInterval"": 5,
                        ""OperationTimeout"": 120000
                    }
                },
                ""EventBufferFlushing"": {
                    ""BufferSizeLimit"": 200,
                    ""FlushLoopInterval"": 60000
                },
                ""EventPersistence"": {
                },
                ""EventCacheConnection"": {
                    ""RetryCount"": 6,
                    ""RetryInterval"": 7
                },
                ""MetricLogging"": {
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 1000,
                        ""DequeueOperationLoopInterval"": 45000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""DataSource"": ""127.0.0.1"",
                            ""InitialCatalog"": ""ApplicationMetrics"",
                            ""UserId"": ""sa"",
                            ""Password"": ""password"",
                            ""RetryCount"": 5,
                            ""RetryInterval"": 10,
                            ""OperationTimeout"": 10000
                        }
                    }
                },
            }";

            return JObject.Parse(stringifiedAppSettings);
        }

        /// <summary>
        /// Creates a base/template for the 'appsettings.json' file contents for distributed operation coordinator nodes.
        /// </summary>
        /// <returns>A base/template for the 'appsettings.json' file contents for distributed operation coordinator nodes.</returns>
        protected JObject CreateDistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate()
        {
            String stringifiedAppSettings = @"
            {
                ""AccessManagerSqlDatabaseConnection"": {
                    ""DatabaseType"": ""SqlServer"",
                    ""ConnectionParameters"": {
                        ""RetryCount"": 3,
                        ""RetryInterval"": 9,
                        ""OperationTimeout"": 120000
                    }
                },
                ""ShardConfigurationRefresh"": {
                    ""RefreshInterval"": ""11000""
                },
                ""ShardConnection"": {
                    ""RetryCount"": ""3"",
                    ""RetryInterval"": ""11"",
                },
                ""MetricLogging"": {
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 2000,
                        ""DequeueOperationLoopInterval"": 46000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""DataSource"": ""127.0.0.1"",
                            ""InitialCatalog"": ""ApplicationMetrics"",
                            ""UserId"": ""sa"",
                            ""Password"": ""password"",
                            ""RetryCount"": 9,
                            ""RetryInterval"": 2,
                            ""OperationTimeout"": 61000
                        }
                    }
                },
            }";

            return JObject.Parse(stringifiedAppSettings);
        }

        /// <summary>
        /// Creates a base/template for the 'appsettings.json' file contents for distributed operation router nodes.
        /// </summary>
        /// <returns>A base/template for the 'appsettings.json' file contents for distributed operation router nodes.</returns>
        protected JObject CreateDistributedOperationRouterNodeAppSettingsConfigurationTemplate()
        {
            String stringifiedAppSettings = @"
            {
                ""ShardRouting"": {
                },
                ""ShardConnection"": {
                    ""RetryCount"": ""15"",
                    ""RetryInterval"": ""2"",
                },
                ""MetricLogging"": {
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 250,
                        ""DequeueOperationLoopInterval"": 2000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""DataSource"": ""127.0.0.1"",
                            ""InitialCatalog"": ""ApplicationMetrics"",
                            ""UserId"": ""sa"",
                            ""Password"": ""password"",
                            ""RetryCount"": 20,
                            ""RetryInterval"": 3,
                            ""OperationTimeout"": 125000
                        }
                    }
                },
            }";

            return JObject.Parse(stringifiedAppSettings);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the KubernetesDistributedAccessManagerInstanceManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        protected class KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers : KubernetesDistributedAccessManagerInstanceManager<TestPersistentStorageLoginCredentials>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.UnitTests.KubernetesDistributedAccessManagerInstanceManagerTests+KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="configuration">Configuration for the instance manager.</param>
            /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
            /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
            /// <param name="kubernetesClientShim">A mock <see cref="IKubernetesClientShim"/>.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                KubernetesDistributedAccessManagerInstanceManagerConfiguration configuration,
                IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials> persistentStorageCreator,
                IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> credentialsAppSettingsConfigurer,
                IKubernetesClientShim kubernetesClientShim, 
                IApplicationLogger logger, 
                IMetricLogger metricLogger
            )
                : base(configuration, persistentStorageCreator, credentialsAppSettingsConfigurer, kubernetesClientShim, logger, metricLogger)
            {
            }

            #pragma warning disable 1591

            public new async Task CreateClusterIpServiceAsync(String appLabelValue, UInt16 port, String nameSpace)
            {
                await base.CreateClusterIpServiceAsync(appLabelValue, port, nameSpace);
            }

            public new async Task CreateLoadBalancerServiceAsync(String appLabelValue, UInt16 port, UInt16 targetPort, String nameSpace)
            {
                await base.CreateLoadBalancerServiceAsync(appLabelValue, port, targetPort, nameSpace);
            }

            public new async Task CreateReaderNodeDeploymentAsync(String name, TestPersistentStorageLoginCredentials persistentStorageCredentials, Uri eventCacheServiceUrl, String nameSpace)
            {
                await base.CreateReaderNodeDeploymentAsync(name, persistentStorageCredentials, eventCacheServiceUrl, nameSpace);
            }

            public new async Task CreateEventCacheNodeDeploymentAsync(String name, String nameSpace)
            {
                await base.CreateEventCacheNodeDeploymentAsync(name, nameSpace);
            }

            public new async Task CreateWriterNodeDeploymentAsync(String name, TestPersistentStorageLoginCredentials persistentStorageCredentials, Uri eventCacheServiceUrl, String nameSpace)
            {
                await base.CreateWriterNodeDeploymentAsync(name, persistentStorageCredentials, eventCacheServiceUrl, nameSpace);
            }

            public new async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync(String name, TestPersistentStorageLoginCredentials persistentStorageCredentials, String nameSpace)
            {
                await base.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, persistentStorageCredentials, nameSpace);
            }

            public new async Task CreateDistributedOperationRouterNodeDeploymentAsync
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
                await base.CreateDistributedOperationRouterNodeDeploymentAsync
                (
                    name,
                    dataElement,
                    sourceReaderUrl,
                    sourceWriterUrl,
                    sourceHashRangeStart,
                    sourceHashRangeEnd,
                    targetReaderUrl,
                    targetWriterUrl,
                    targetHashRangeStart,
                    targetHashRangeEnd,
                    routingInitiallyOn,
                    nameSpace
                );
            }

            public new async Task ScaleDeploymentAsync(String name, Int32 replicaCount, String nameSpace)
            {
                await base.ScaleDeploymentAsync(name, replicaCount, nameSpace);
            }

            public new async Task WaitForDeploymentAvailabilityAsync(String name, String nameSpace, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.WaitForDeploymentAvailabilityAsync(name, nameSpace, checkInterval, abortTimeout);
            }

            public new async Task WaitForDeploymentScaleDownAsync(String name, String nameSpace, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.WaitForDeploymentScaleDownAsync(name, nameSpace, checkInterval, abortTimeout);
            }

            public new async Task WaitForDeploymentPredicateAsync(String nameSpace, Predicate<V1Deployment> predicate, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.WaitForDeploymentPredicateAsync(nameSpace, predicate, checkInterval, abortTimeout);
            }

            #pragma warning restore 1591
        }

        #endregion
    }
}
