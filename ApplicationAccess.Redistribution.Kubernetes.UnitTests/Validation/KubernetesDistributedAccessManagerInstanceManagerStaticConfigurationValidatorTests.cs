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
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Kubernetes.Validation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ApplicationAccess.Redistribution.Kubernetes.UnitTests.Validation
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.Validation.KubernetesDistributedAccessManagerInstanceManagerStaticConfigurationValidator class.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManagerStaticConfigurationValidatorTests
    {
        protected KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration testStaticConfiguration;
        protected KubernetesDistributedAccessManagerInstanceManagerStaticConfigurationValidator testStaticConfigurationValidator;

        [SetUp]
        protected void SetUp()
        {
            testStaticConfiguration = new KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration
            {
                PodPort = 5000,
                ExternalPort = 7000,
                NameSpace = "default",
                PersistentStorageInstanceNamePrefix = "applicationaccesstest",
                LoadBalancerServicesHttps = false,
                DeploymentWaitPollingInterval = 100,
                ServiceAvailabilityWaitAbortTimeout = 5000,
                ReaderNodeConfigurationTemplate = new ReaderNodeConfiguration
                {
                    ReplicaCount = 1,
                    TerminationGracePeriod = 3600,
                    ContainerImage = "applicationaccess/distributedreader:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning,
                    AppSettingsConfigurationTemplate = new JObject(),
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
                    AppSettingsConfigurationTemplate = new JObject(),
                    CpuResourceRequest = "50m",
                    MemoryResourceRequest = "60Mi",
                    StartupProbeFailureThreshold = 6,
                    StartupProbePeriod = 4
                },
                WriterNodeConfigurationTemplate = new WriterNodeConfiguration
                {
                    PersistentVolumeClaimName = "eventbackup-claim",
                    TerminationGracePeriod = 1200,
                    ContainerImage = "applicationaccess/distributedwriter:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Critical,
                    AppSettingsConfigurationTemplate = new JObject(),
                    CpuResourceRequest = "200m",
                    MemoryResourceRequest = "240Mi",
                    StartupProbeFailureThreshold = 7,
                    StartupProbePeriod = 5
                },
                DistributedOperationCoordinatorNodeConfigurationTemplate = new DistributedOperationCoordinatorNodeConfiguration
                {
                    ReplicaCount = 3,
                    TerminationGracePeriod = 60,
                    ContainerImage = "applicationaccess/distributedoperationcoordinator:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Warning,
                    AppSettingsConfigurationTemplate = new JObject(),
                    CpuResourceRequest = "500m",
                    MemoryResourceRequest = "600Mi",
                    StartupProbeFailureThreshold = 8,
                    StartupProbePeriod = 6
                },
                DistributedOperationRouterNodeConfigurationTemplate = new DistributedOperationRouterNodeConfiguration
                {
                    TerminationGracePeriod = 30,
                    ContainerImage = "applicationaccess/distributedoperationrouter:20250203-0900",
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Critical,
                    AppSettingsConfigurationTemplate = new JObject(),
                    CpuResourceRequest = "400m",
                    MemoryResourceRequest = "450Mi",
                    StartupProbeFailureThreshold = 9,
                    StartupProbePeriod = 7
                }
            };
            testStaticConfigurationValidator = new KubernetesDistributedAccessManagerInstanceManagerStaticConfigurationValidator();
        }

        [Test]
        public void Validate_NameSpacePropertyNull()
        {
            testStaticConfiguration = testStaticConfiguration with { NameSpace = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property 'NameSpace' cannot be null."));
            Assert.AreEqual("NameSpace", e.ParamName);
        }

        [Test]
        public void Validate_PersistentStorageInstanceNamePrefixPropertyNull()
        {
            testStaticConfiguration = testStaticConfiguration with { PersistentStorageInstanceNamePrefix = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property 'PersistentStorageInstanceNamePrefix' cannot be null."));
            Assert.AreEqual("PersistentStorageInstanceNamePrefix", e.ParamName);
        }

        [Test]
        public void Validate_DeploymentWaitPollingIntervalProperty0()
        {
            testStaticConfiguration = testStaticConfiguration with { DeploymentWaitPollingInterval = 0 };

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property 'DeploymentWaitPollingInterval' with value 0 must be greater than 0."));
            Assert.AreEqual("DeploymentWaitPollingInterval", e.ParamName);
        }

        [Test]
        public void Validate_ServiceAvailabilityWaitAbortTimeoutProperty0()
        {
            testStaticConfiguration = testStaticConfiguration with { ServiceAvailabilityWaitAbortTimeout = 0 };

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property 'ServiceAvailabilityWaitAbortTimeout' with value 0 must be greater than 0."));
            Assert.AreEqual("ServiceAvailabilityWaitAbortTimeout", e.ParamName);
        }

        [Test]
        public void Validate_ReaderNodeContainerImagePropertyNull()
        {
            testStaticConfiguration = testStaticConfiguration with 
            { 
                ReaderNodeConfigurationTemplate = testStaticConfiguration.ReaderNodeConfigurationTemplate with { ContainerImage = null }
            };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"ReaderNodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }

        [Test]
        public void Validate_EventCacheNodeContainerImagePropertyNull()
        {
            testStaticConfiguration = testStaticConfiguration with
            {
                EventCacheNodeConfigurationTemplate = testStaticConfiguration.EventCacheNodeConfigurationTemplate with { ContainerImage = null }
            };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }

        [Test]
        public void Validate_WriterNodeContainerImagePropertyNull()
        {
            testStaticConfiguration = testStaticConfiguration with
            {
                WriterNodeConfigurationTemplate = testStaticConfiguration.WriterNodeConfigurationTemplate with { ContainerImage = null }
            };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"WriterNodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }

        [Test]
        public void Validate_DistributedOperationCoordinatorNodeContainerImagePropertyNull()
        {
            testStaticConfiguration = testStaticConfiguration with
            {
                DistributedOperationCoordinatorNodeConfigurationTemplate = testStaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate with { ContainerImage = null }
            };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"DistributedOperationCoordinatorNodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }

        [Test]
        public void Validate_DistributedOperationRouterNodeContainerImagePropertyNull()
        {
            testStaticConfiguration = testStaticConfiguration with
            {
                DistributedOperationRouterNodeConfigurationTemplate = testStaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate with { ContainerImage = null }
            };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testStaticConfigurationValidator.Validate(testStaticConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }
    }
}
