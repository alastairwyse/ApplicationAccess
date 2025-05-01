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
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.Validation.WriterNodeConfigurationValidator class.
    /// </summary>
    public class WriterNodeConfigurationValidatorTests
    {
        protected WriterNodeConfiguration testNodeConfiguration;
        protected WriterNodeConfigurationValidator testWriterNodeConfigurationValidator;

        [SetUp]
        protected void SetUp()
        {
            testNodeConfiguration = new WriterNodeConfiguration
            {
                TerminationGracePeriod = 1800,
                ContainerImage = "applicationaccess/eventcache:20250203-0900",
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
                AppSettingsConfigurationTemplate = new JObject(),
                CpuResourceRequest = "50m",
                MemoryResourceRequest = "60Mi",
                StartupProbeFailureThreshold = 5,
                StartupProbePeriod = 10,
                PersistentVolumeClaimName = "eventbackup-claim"
            };
            testWriterNodeConfigurationValidator = new WriterNodeConfigurationValidator();
        }

        [Test]
        public void Validate_PersistentVolumeClaimNamePropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { PersistentVolumeClaimName = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testWriterNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"WriterNodeConfiguration property 'PersistentVolumeClaimName' cannot be null."));
            Assert.AreEqual("PersistentVolumeClaimName", e.ParamName);
        }

        [Test]
        public void Validate_ContainerImagePropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { ContainerImage = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testWriterNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"WriterNodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }
    }
}
