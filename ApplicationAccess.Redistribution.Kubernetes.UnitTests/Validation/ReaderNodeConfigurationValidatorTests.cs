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
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.Validation.ReaderNodeConfigurationValidator class.
    /// </summary>
    public class ReaderNodeConfigurationValidatorTests
    {
        protected ReaderNodeConfiguration testNodeConfiguration;
        protected ReaderNodeConfigurationValidator testReaderNodeConfigurationValidator;

        [SetUp]
        protected void SetUp()
        {
            testNodeConfiguration = new ReaderNodeConfiguration
            {
                TerminationGracePeriod = 1800,
                ContainerImage = "applicationaccess/eventcache:20250203-0900",
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
                AppSettingsConfigurationTemplate = new JObject(),
                CpuResourceRequest = "50m",
                MemoryResourceRequest = "60Mi",
                StartupProbeFailureThreshold = 5,
                StartupProbePeriod = 10,
                UserReplicaCount = 1,
                GroupToGroupMappingReplicaCount = 2,
                GroupReplicaCount = 3,
                LivenessProbePeriod = 5
            };
            testReaderNodeConfigurationValidator = new ReaderNodeConfigurationValidator();
        }

        [Test]
        public void Validate_UserReplicaCountProperty0()
        {
            testNodeConfiguration = testNodeConfiguration with { UserReplicaCount = 0 };

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testReaderNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"ReaderNodeConfiguration property 'UserReplicaCount' with value 0 must be greater than 0."));
            Assert.AreEqual("UserReplicaCount", e.ParamName);
        }

        [Test]
        public void Validate_GroupToGroupMappingReplicaCountProperty0()
        {
            testNodeConfiguration = testNodeConfiguration with { GroupToGroupMappingReplicaCount = 0 };

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testReaderNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"ReaderNodeConfiguration property 'GroupToGroupMappingReplicaCount' with value 0 must be greater than 0."));
            Assert.AreEqual("GroupToGroupMappingReplicaCount", e.ParamName);
        }

        [Test]
        public void Validate_GroupReplicaCountProperty0()
        {
            testNodeConfiguration = testNodeConfiguration with { GroupReplicaCount = 0 };

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testReaderNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"ReaderNodeConfiguration property 'GroupReplicaCount' with value 0 must be greater than 0."));
            Assert.AreEqual("GroupReplicaCount", e.ParamName);
        }

        [Test]
        public void Validate_ContainerImagePropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { ContainerImage = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testReaderNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"ReaderNodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }
    }
}
