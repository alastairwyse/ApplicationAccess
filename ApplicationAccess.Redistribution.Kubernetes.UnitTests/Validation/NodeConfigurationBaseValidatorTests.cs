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
using k8s.Models;
using NUnit.Framework;

namespace ApplicationAccess.Redistribution.Kubernetes.UnitTests.Validation
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.Validation.NodeConfigurationBaseValidator class.
    /// </summary>
    public class NodeConfigurationBaseValidatorTests
    {
        protected NodeConfigurationBase testNodeConfiguration;
        protected NodeConfigurationBaseValidator<NodeConfigurationBase> testNodeConfigurationBaseValidator;

        [SetUp]
        protected void SetUp()
        {
            testNodeConfiguration = new EventCacheNodeConfiguration
            {
                TerminationGracePeriod = 1800,
                ContainerImage = "applicationaccess/eventcache:20250203-0900",
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
                AppSettingsConfigurationTemplate = new JObject(),
                CpuResourceRequest = "50m",
                MemoryResourceRequest = "60Mi",
                StartupProbeFailureThreshold = 5,
                StartupProbePeriod = 10
            };
            testNodeConfigurationBaseValidator = new NodeConfigurationBaseValidator<NodeConfigurationBase>();
        }

        [Test]
        public void Validate_ContainerImagePropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { ContainerImage = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testNodeConfigurationBaseValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }

        [Test]
        public void Validate_AppSettingsConfigurationTemplatePropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { AppSettingsConfigurationTemplate = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testNodeConfigurationBaseValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'AppSettingsConfigurationTemplate' cannot be null."));
            Assert.AreEqual("AppSettingsConfigurationTemplate", e.ParamName);
        }

        [Test]
        public void Validate_CpuResourceRequestPropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { CpuResourceRequest = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testNodeConfigurationBaseValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'CpuResourceRequest' cannot be null."));
            Assert.AreEqual("CpuResourceRequest", e.ParamName);
        }

        [Test]
        public void Validate_MemoryResourceRequestPropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { MemoryResourceRequest = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testNodeConfigurationBaseValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'MemoryResourceRequest' cannot be null."));
            Assert.AreEqual("MemoryResourceRequest", e.ParamName);
        }

        [Test]
        public void Validate_CpuResourceRequestPropertyInvalid()
        {
            testNodeConfiguration = testNodeConfiguration with { CpuResourceRequest = "invalid" };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testNodeConfigurationBaseValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'CpuResourceRequest' with value 'invalid' failed to validate."));
            Assert.AreEqual("CpuResourceRequest", e.ParamName);
        }

        [Test]
        public void Validate_MemoryResourceRequestPropertyInvalid()
        {
            testNodeConfiguration = testNodeConfiguration with { MemoryResourceRequest = "invalid" };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testNodeConfigurationBaseValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"NodeConfiguration property 'MemoryResourceRequest' with value 'invalid' failed to validate."));
            Assert.AreEqual("MemoryResourceRequest", e.ParamName);
        }
    }
}
