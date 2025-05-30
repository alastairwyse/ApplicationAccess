﻿/*
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
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.Validation.DistributedOperationCoordinatorNodeConfigurationValidator class.
    /// </summary>
    public class DistributedOperationCoordinatorNodeConfigurationValidatorTests
    {
        protected DistributedOperationCoordinatorNodeConfiguration testNodeConfiguration;
        protected DistributedOperationCoordinatorNodeConfigurationValidator testDistributedOperationCoordinatorNodeConfigurationValidator;

        [SetUp]
        protected void SetUp()
        {
            String stringifiedAppSettings = @"
            {
                ""ShardConfigurationRefresh"": {
                    ""RefreshInterval"": ""5000""
                }
            }";
            testNodeConfiguration = new DistributedOperationCoordinatorNodeConfiguration
            {
                TerminationGracePeriod = 1800,
                ContainerImage = "applicationaccess/eventcache:20250203-0900",
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
                AppSettingsConfigurationTemplate = JObject.Parse(stringifiedAppSettings),
                CpuResourceRequest = "50m",
                MemoryResourceRequest = "60Mi",
                StartupProbeFailureThreshold = 5,
                StartupProbePeriod = 10, 
                ReplicaCount = 3
            };
            testDistributedOperationCoordinatorNodeConfigurationValidator = new DistributedOperationCoordinatorNodeConfigurationValidator();
        }

        [Test]
        public void Validate_ReplicaCountProperty0()
        {
            testNodeConfiguration = testNodeConfiguration with { ReplicaCount = 0 };

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testDistributedOperationCoordinatorNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"DistributedOperationCoordinatorNodeConfiguration property 'ReplicaCount' with value 0 must be greater than 0."));
            Assert.AreEqual("ReplicaCount", e.ParamName);
        }

        [Test]
        public void Validate_ContainerImagePropertyNull()
        {
            testNodeConfiguration = testNodeConfiguration with { ContainerImage = null };

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testDistributedOperationCoordinatorNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"DistributedOperationCoordinatorNodeConfiguration property 'ContainerImage' cannot be null."));
            Assert.AreEqual("ContainerImage", e.ParamName);
        }

        [Test]
        public void Validate_AppSettingsConfigurationTemplateShardConfigurationRefreshPropertyDoesntExist()
        {
            testNodeConfiguration.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"] = null;

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDistributedOperationCoordinatorNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"DistributedOperationCoordinatorNodeConfiguration property 'AppSettingsConfigurationTemplate' did not contain configuration refresh interval configuration property 'RefreshInterval'."));
        }

        [Test]
        public void Validate_AppSettingsConfigurationTemplateShardConfigurationRefreshPropertyNotInteger()
        {
            testNodeConfiguration.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = "abc";

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testDistributedOperationCoordinatorNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"DistributedOperationCoordinatorNodeConfiguration property 'AppSettingsConfigurationTemplate' configuration refresh interval configuration property 'RefreshInterval' with value 'abc' could be no converted to an integer."));
        }

        [Test]
        public void Validate_AppSettingsConfigurationTemplateShardConfigurationRefreshProperty0()
        {
            testNodeConfiguration.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 0;

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testDistributedOperationCoordinatorNodeConfigurationValidator.Validate(testNodeConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"DistributedOperationCoordinatorNodeConfiguration property 'AppSettingsConfigurationTemplate' configuration refresh interval configuration property 'RefreshInterval' with value 0 must be greater than 0."));
            Assert.AreEqual("RefreshInterval", e.ParamName);
        }
    }
}
