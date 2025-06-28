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
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.DistributedAccessManagerInstanceOptionsValidator class.
    /// </summary>
    public class DistributedAccessManagerInstanceOptionsValidatorTests
    {
        private DistributedAccessManagerInstanceOptions distributedAccessManagerInstanceOptions;
        private DistributedAccessManagerInstanceOptionsValidator testDistributedAccessManagerInstanceOptionsValidator;

        [SetUp]
        protected void SetUp()
        {
            distributedAccessManagerInstanceOptions = GenerateDistributedAccessManagerInstanceOptions();
            testDistributedAccessManagerInstanceOptionsValidator = new DistributedAccessManagerInstanceOptionsValidator();
        }

        [Test]
        public void Validate_RequiredConfigurationMissing()
        {
            Validate_TopLevelRequiredConfigurationMissing((instOpts) => { instOpts.SqlServerDatabaseConnection = null; }, "SqlServerDatabaseConnection");
            Validate_TopLevelRequiredConfigurationMissing((instOpts) => { instOpts.ShardConnection = null; }, "ShardConnection");
            Validate_TopLevelRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration = null; }, "StaticConfiguration");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.ShardConnection.RetryCount = null; }, "ShardConnection", "RetryCount");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.ShardConnection.RetryInterval = null; }, "ShardConnection", "RetryInterval");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.ShardConnection.ConnectionTimeout = null; }, "ShardConnection", "ConnectionTimeout");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.PodPort = null; }, "StaticConfiguration", "PodPort");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ExternalPort = null; }, "StaticConfiguration", "ExternalPort");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.NameSpace = null; }, "StaticConfiguration", "NameSpace");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.PersistentStorageInstanceNamePrefix = null; }, "StaticConfiguration", "PersistentStorageInstanceNamePrefix");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.LoadBalancerServicesHttps = null; }, "StaticConfiguration", "LoadBalancerServicesHttps");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DeploymentWaitPollingInterval = null; }, "StaticConfiguration", "DeploymentWaitPollingInterval");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ServiceAvailabilityWaitAbortTimeout = null; }, "StaticConfiguration", "ServiceAvailabilityWaitAbortTimeout");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer = null; }, "StaticConfiguration", "DistributedOperationCoordinatorRefreshIntervalWaitBuffer");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.ReplicaCount = null; }, "ReaderNodeConfigurationTemplate", "ReplicaCount");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.TerminationGracePeriod = null; }, "ReaderNodeConfigurationTemplate", "TerminationGracePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.ContainerImage = null; }, "ReaderNodeConfigurationTemplate", "ContainerImage");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.MinimumLogLevel = null; }, "ReaderNodeConfigurationTemplate", "MinimumLogLevel");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.CpuResourceRequest = null; }, "ReaderNodeConfigurationTemplate", "CpuResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.MemoryResourceRequest = null; }, "ReaderNodeConfigurationTemplate", "MemoryResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.LivenessProbePeriod = null; }, "ReaderNodeConfigurationTemplate", "LivenessProbePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.StartupProbeFailureThreshold = null; }, "ReaderNodeConfigurationTemplate", "StartupProbeFailureThreshold");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.StartupProbePeriod = null; }, "ReaderNodeConfigurationTemplate", "StartupProbePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.TerminationGracePeriod = null; }, "EventCacheNodeConfigurationTemplate", "TerminationGracePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.ContainerImage = null; }, "EventCacheNodeConfigurationTemplate", "ContainerImage");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.MinimumLogLevel = null; }, "EventCacheNodeConfigurationTemplate", "MinimumLogLevel");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.CpuResourceRequest = null; }, "EventCacheNodeConfigurationTemplate", "CpuResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.MemoryResourceRequest = null; }, "EventCacheNodeConfigurationTemplate", "MemoryResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.StartupProbeFailureThreshold = null; }, "EventCacheNodeConfigurationTemplate", "StartupProbeFailureThreshold");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.StartupProbePeriod = null; }, "EventCacheNodeConfigurationTemplate", "StartupProbePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.PersistentVolumeClaimName = null; }, "WriterNodeConfigurationTemplate", "PersistentVolumeClaimName");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.TerminationGracePeriod = null; }, "WriterNodeConfigurationTemplate", "TerminationGracePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.ContainerImage = null; }, "WriterNodeConfigurationTemplate", "ContainerImage");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.MinimumLogLevel = null; }, "WriterNodeConfigurationTemplate", "MinimumLogLevel");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.CpuResourceRequest = null; }, "WriterNodeConfigurationTemplate", "CpuResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.MemoryResourceRequest = null; }, "WriterNodeConfigurationTemplate", "MemoryResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.StartupProbeFailureThreshold = null; }, "WriterNodeConfigurationTemplate", "StartupProbeFailureThreshold");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.StartupProbePeriod = null; }, "WriterNodeConfigurationTemplate", "StartupProbePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.ReplicaCount = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "ReplicaCount");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.TerminationGracePeriod = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "TerminationGracePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.ContainerImage = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "ContainerImage");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.MinimumLogLevel = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "MinimumLogLevel");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.CpuResourceRequest = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "CpuResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.MemoryResourceRequest = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "MemoryResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbeFailureThreshold = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "StartupProbeFailureThreshold");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbePeriod = null; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "StartupProbePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.TerminationGracePeriod = null; }, "DistributedOperationRouterNodeConfigurationTemplate", "TerminationGracePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.ContainerImage = null; }, "DistributedOperationRouterNodeConfigurationTemplate", "ContainerImage");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.MinimumLogLevel = null; }, "DistributedOperationRouterNodeConfigurationTemplate", "MinimumLogLevel");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.CpuResourceRequest = null; }, "DistributedOperationRouterNodeConfigurationTemplate", "CpuResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.MemoryResourceRequest = null; }, "DistributedOperationRouterNodeConfigurationTemplate", "MemoryResourceRequest");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.StartupProbeFailureThreshold = null; }, "DistributedOperationRouterNodeConfigurationTemplate", "StartupProbeFailureThreshold");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.StartupProbePeriod = null; }, "DistributedOperationRouterNodeConfigurationTemplate", "StartupProbePeriod");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart = null; }, "ShardGroupConfiguration", "HashRangeStart");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeClientUrl = null; }, "ShardGroupConfiguration", "ReaderNodeClientUrl");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.UserShardGroupConfiguration[0].WriterNodeClientUrl = null; }, "ShardGroupConfiguration", "WriterNodeClientUrl");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.UserShardGroupConfiguration[0].SqlServerConnectionString = null; }, "ShardGroupConfiguration", "SqlServerConnectionString");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart = null; }, "ShardGroupConfiguration", "HashRangeStart");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientUrl = null; }, "ShardGroupConfiguration", "ReaderNodeClientUrl");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientUrl = null; }, "ShardGroupConfiguration", "WriterNodeClientUrl");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].SqlServerConnectionString = null; }, "ShardGroupConfiguration", "SqlServerConnectionString");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart = null; }, "ShardGroupConfiguration", "HashRangeStart");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeClientUrl = null; }, "ShardGroupConfiguration", "ReaderNodeClientUrl");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeClientUrl = null; }, "ShardGroupConfiguration", "WriterNodeClientUrl");
            Validate_ChildRequiredConfigurationMissing((instOpts) => { instOpts.InstanceConfiguration.GroupShardGroupConfiguration[0].SqlServerConnectionString = null; }, "ShardGroupConfiguration", "SqlServerConnectionString");
        }

        [Test]
        public void Validate_NumericConfigurationValueOutOfRange()
        {
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.ShardConnection.RetryCount = 0; }, "ShardConnection", "RetryCount", 1, Int32.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.ShardConnection.RetryInterval = 0; }, "ShardConnection", "RetryInterval", 1, Int32.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.ShardConnection.ConnectionTimeout = 0; }, "ShardConnection", "ConnectionTimeout", 1, Int32.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.PodPort = 0; }, "StaticConfiguration", "PodPort", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.ExternalPort = 0; }, "StaticConfiguration", "ExternalPort", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.DeploymentWaitPollingInterval = 0; }, "StaticConfiguration", "DeploymentWaitPollingInterval", 1, Int32.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.ServiceAvailabilityWaitAbortTimeout = 0; }, "StaticConfiguration", "ServiceAvailabilityWaitAbortTimeout", 1, Int32.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 0; }, "StaticConfiguration", "DistributedOperationCoordinatorRefreshIntervalWaitBuffer", 1, Int32.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.ReplicaCount = 0; }, "ReaderNodeConfigurationTemplate", "ReplicaCount", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.LivenessProbePeriod = 0; }, "ReaderNodeConfigurationTemplate", "LivenessProbePeriod", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.StartupProbePeriod = 0; }, "ReaderNodeConfigurationTemplate", "StartupProbePeriod", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.StartupProbePeriod = 0; }, "EventCacheNodeConfigurationTemplate", "StartupProbePeriod", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.StartupProbePeriod = 0; }, "WriterNodeConfigurationTemplate", "StartupProbePeriod", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.ReplicaCount = 0; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "ReplicaCount", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.StartupProbePeriod = 0; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "StartupProbePeriod", 1, UInt16.MaxValue);
            Validate_ChildNumericConfigurationValueOutOfRange((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.StartupProbePeriod = 0; }, "DistributedOperationRouterNodeConfigurationTemplate", "StartupProbePeriod", 1, UInt16.MaxValue);
        }

        [Test]
        public void Validate_KubernetesResourceValueInvalid()
        {
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.CpuResourceRequest = "abc"; }, "ReaderNodeConfigurationTemplate", "CpuResourceRequest", "abc");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.ReaderNodeConfigurationTemplate.MemoryResourceRequest = "def"; }, "ReaderNodeConfigurationTemplate", "MemoryResourceRequest", "def");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.CpuResourceRequest = "hij"; }, "EventCacheNodeConfigurationTemplate", "CpuResourceRequest", "hij");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.EventCacheNodeConfigurationTemplate.MemoryResourceRequest = "lmn"; }, "EventCacheNodeConfigurationTemplate", "MemoryResourceRequest", "lmn");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.CpuResourceRequest = "opq"; }, "WriterNodeConfigurationTemplate", "CpuResourceRequest", "opq");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.WriterNodeConfigurationTemplate.MemoryResourceRequest = "rst"; }, "WriterNodeConfigurationTemplate", "MemoryResourceRequest", "rst");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.CpuResourceRequest = "uvw"; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "CpuResourceRequest", "uvw");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.MemoryResourceRequest = "xyz"; }, "DistributedOperationCoordinatorNodeConfigurationTemplate", "MemoryResourceRequest", "xyz");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.CpuResourceRequest = "zyx"; }, "DistributedOperationRouterNodeConfigurationTemplate", "CpuResourceRequest", "zyx");
            Validate_KubernetesResourceValueInvalid((instOpts) => { instOpts.StaticConfiguration.DistributedOperationRouterNodeConfigurationTemplate.MemoryResourceRequest = "wvu"; }, "DistributedOperationRouterNodeConfigurationTemplate", "MemoryResourceRequest", "wvu");
        }

        #region Private/Protected Methods

        /// <summary>
        /// Asserts that an exception is thrown when a property of the <see cref="DistributedAccessManagerInstanceOptions"/> class is null.
        /// </summary>
        /// <param name="distributedAccessManagerInstanceOptionsSetupAction">Action which sets the property being tested to null.</param>
        /// <param name="propertyName">The name of the property being set null.</param>
        protected void Validate_TopLevelRequiredConfigurationMissing
        (
            Action<DistributedAccessManagerInstanceOptions> distributedAccessManagerInstanceOptionsSetupAction,
            String propertyName
        )
        {
            distributedAccessManagerInstanceOptions = GenerateDistributedAccessManagerInstanceOptions();
            distributedAccessManagerInstanceOptionsSetupAction(distributedAccessManagerInstanceOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                testDistributedAccessManagerInstanceOptionsValidator.Validate(distributedAccessManagerInstanceOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating DistributedAccessManagerInstance options.  Configuration for '{propertyName}' is required."));
        }

        /// <summary>
        /// Asserts that an exception is thrown when a child property (i.e. property of a child options class) of the <see cref="DistributedAccessManagerInstanceOptions"/> class is null.
        /// </summary>
        /// <param name="distributedAccessManagerInstanceOptionsSetupAction">Action which sets the property being tested to null.</param>
        /// <param name="optionsClassName">The name of the nested options object which holds the property.</param>
        /// <param name="propertyName">The name of the property being set null.</param>
        protected void Validate_ChildRequiredConfigurationMissing
        (
            Action<DistributedAccessManagerInstanceOptions> distributedAccessManagerInstanceOptionsSetupAction,
            String optionsClassName,
            String propertyName
        )
        {
            distributedAccessManagerInstanceOptions = GenerateDistributedAccessManagerInstanceOptions();
            distributedAccessManagerInstanceOptionsSetupAction(distributedAccessManagerInstanceOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                testDistributedAccessManagerInstanceOptionsValidator.Validate(distributedAccessManagerInstanceOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating DistributedAccessManagerInstance options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating {optionsClassName} options.  Configuration for '{propertyName}' is required."));
        }

        /// <summary>
        /// Asserts that an exception is thrown when a numeric child property (i.e. property of a child options class) of the <see cref="DistributedAccessManagerInstanceOptions"/> class contains an invalid value.
        /// </summary>
        /// <param name="distributedAccessManagerInstanceOptionsSetupAction">Action which sets the property being tested to an invalid value.</param>
        /// <param name="optionsClassName">The name of the nested options object which holds the property.</param>
        /// <param name="propertyName">The name of the property being made invalid.</param>
        /// <param name="expectedMinimumValue">The expected minimum value of the proprty.</param>
        /// <param name="expectedMaximumValue">he expected maximum value of the proprty.</param>
        protected void Validate_ChildNumericConfigurationValueOutOfRange
        (
            Action<DistributedAccessManagerInstanceOptions> distributedAccessManagerInstanceOptionsSetupAction,
            String optionsClassName,
            String propertyName, 
            Int32 expectedMinimumValue,
            Int32 expectedMaximumValue
        )
        {
            distributedAccessManagerInstanceOptions = GenerateDistributedAccessManagerInstanceOptions();
            distributedAccessManagerInstanceOptionsSetupAction(distributedAccessManagerInstanceOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                testDistributedAccessManagerInstanceOptionsValidator.Validate(distributedAccessManagerInstanceOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating DistributedAccessManagerInstance options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating {optionsClassName} options.  Value for '{propertyName}' must be between {expectedMinimumValue} and {expectedMaximumValue}."));
        }

        /// <summary>
        /// Asserts that an exception is thrown when a resource value from a node template within the distributed instance options static configuration contains an invalid value.
        /// </summary>
        /// <param name="distributedAccessManagerStaticOptionsSetupAction">Action which sets the resource value being tested to an invalid value.</param>
        /// <param name="nodeConfigurationTemplatePropertyName">The name of the node template configuration containing the resource value.</param>
        /// <param name="resourceValuePropertyName">The name of the resource value.</param>
        /// <param name="resourceValue">The resource value.</param>
        protected void Validate_KubernetesResourceValueInvalid
        (
            Action<DistributedAccessManagerInstanceOptions> distributedAccessManagerStaticOptionsSetupAction,
            String nodeConfigurationTemplatePropertyName,
            String resourceValuePropertyName,
            String resourceValue
        )
        {
            distributedAccessManagerInstanceOptions = GenerateDistributedAccessManagerInstanceOptions();
            distributedAccessManagerStaticOptionsSetupAction(distributedAccessManagerInstanceOptions);


            var e = Assert.Throws<ValidationException>(delegate
            {
                testDistributedAccessManagerInstanceOptionsValidator.Validate(distributedAccessManagerInstanceOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating DistributedAccessManagerInstance options."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error validating StaticConfiguration options.  Error validating {nodeConfigurationTemplatePropertyName} options.  Value '{resourceValue}' for '{resourceValuePropertyName}' is invalid."));
        }

        #pragma warning disable 1591

        protected DistributedAccessManagerInstanceOptions GenerateDistributedAccessManagerInstanceOptions()
        {
            return new DistributedAccessManagerInstanceOptions
            {
                SqlServerDatabaseConnection = new AccessManagerSqlDatabaseConnectionOptions
                {
                    DatabaseType = DatabaseType.SqlServer,
                    ConnectionParameters = new ConfigurationBuilder().Build().GetSection("EmptySection")
                },
                ShardConnection = new ShardConnectionOptions
                {
                    RetryCount = 10,
                    RetryInterval = 5,
                    ConnectionTimeout = 300000
                },
                StaticConfiguration = new StaticConfigurationOptions
                {
                    PodPort = 5000,
                    ExternalPort = 7000,
                    NameSpace = "default",
                    PersistentStorageInstanceNamePrefix = "appaccesstest",
                    LoadBalancerServicesHttps = false,
                    DeploymentWaitPollingInterval = 100,
                    ServiceAvailabilityWaitAbortTimeout = 5000,
                    DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 1000,
                    ReaderNodeConfigurationTemplate = new ReaderNodeConfigurationTemplateOptions
                    {
                        ReplicaCount = 2,
                        TerminationGracePeriod = 3600,
                        ContainerImage = "applicationaccess/distributedreader:20250203-0900",
                        MinimumLogLevel = LogLevel.Warning,
                        CpuResourceRequest = "100m",
                        MemoryResourceRequest = "120Mi",
                        LivenessProbePeriod = 10,
                        StartupProbeFailureThreshold = 12,
                        StartupProbePeriod = 11
                    },
                    EventCacheNodeConfigurationTemplate = new EventCacheNodeConfigurationTemplateOptions
                    {
                        TerminationGracePeriod = 1800,
                        ContainerImage = "applicationaccess/eventcache:20250203-0900",
                        MinimumLogLevel = LogLevel.Information,
                        CpuResourceRequest = "50m",
                        MemoryResourceRequest = "60Mi",
                        StartupProbeFailureThreshold = 6,
                        StartupProbePeriod = 4
                    },
                    WriterNodeConfigurationTemplate = new WriterNodeConfigurationTemplateOptions
                    {
                        PersistentVolumeClaimName = "eventbackup-claim",
                        TerminationGracePeriod = 1200,
                        ContainerImage = "applicationaccess/distributedwriter:20250203-0900",
                        MinimumLogLevel = LogLevel.Critical,
                        CpuResourceRequest = "200m",
                        MemoryResourceRequest = "240Mi",
                        StartupProbeFailureThreshold = 7,
                        StartupProbePeriod = 5
                    },
                    DistributedOperationCoordinatorNodeConfigurationTemplate = new DistributedOperationCoordinatorNodeConfigurationTemplateOptions
                    {
                        ReplicaCount = 3,
                        TerminationGracePeriod = 60,
                        ContainerImage = "applicationaccess/distributedoperationcoordinator:20250203-0900",
                        MinimumLogLevel = LogLevel.Warning,
                        CpuResourceRequest = "500m",
                        MemoryResourceRequest = "600Mi",
                        StartupProbeFailureThreshold = 6,
                        StartupProbePeriod = 6
                    },
                    DistributedOperationRouterNodeConfigurationTemplate = new DistributedOperationRouterNodeConfigurationTemplateOptions
                    {
                        TerminationGracePeriod = 30,
                        ContainerImage = "applicationaccess/distributedoperationrouter:20250203-0900",
                        MinimumLogLevel = LogLevel.Critical,
                        CpuResourceRequest = "400m",
                        MemoryResourceRequest = "450Mi",
                        StartupProbeFailureThreshold = 9,
                        StartupProbePeriod = 7
                    }
                },
                InstanceConfiguration = new InstanceConfigurationOptions
                {
                    DistributedOperationRouterUrl = "http://192.168.0.200:7001/",
                    Writer1Url = "http://192.168.0.200:7002/",
                    Writer2Url = "http://192.168.0.200:7003",
                    ShardConfigurationSqlServerConnectionString = "Server=127.0.0.1;User Id=sa;Password=pwd;Initial Catalog=aatest_shard_configuration;Encrypt=false;Authentication=SqlPassword",
                    UserShardGroupConfiguration = new List<ShardGroupConfigurationOptions>
                    {
                        new ShardGroupConfigurationOptions
                        {
                            HashRangeStart = -2147483648,
                            ReaderNodeClientUrl = "http://user-reader-n2147483648-service:5000/",
                            WriterNodeClientUrl = "http://user-writer-n2147483648-service:5000/",
                            SqlServerConnectionString = "Server=127.0.0.1;User Id=sa;Password=pwd;Encrypt=false;Authentication=SqlPassword;Initial Catalog=aatest_user_n2147483648"
                        }
                    },
                    GroupToGroupMappingShardGroupConfiguration = new List<ShardGroupConfigurationOptions>
                    {
                        new ShardGroupConfigurationOptions
                        {
                            HashRangeStart = -2147483648,
                            ReaderNodeClientUrl = "http://grouptogroupmapping-reader-n2147483648-service:5000/",
                            WriterNodeClientUrl = "http://grouptogroupmapping-writer-n2147483648-service:5000/",
                            SqlServerConnectionString = "Server=127.0.0.1;User Id=sa;Password=pwd;Encrypt=false;Authentication=SqlPassword;Initial Catalog=aatest_grouptogroupmapping_n2147483648"
                        }
                    },
                    GroupShardGroupConfiguration = new List<ShardGroupConfigurationOptions>
                    {
                        new ShardGroupConfigurationOptions
                        {
                            HashRangeStart = -2147483648,
                            ReaderNodeClientUrl = "http://group-reader-n2147483648-service:5000/",
                            WriterNodeClientUrl = "http://group-writer-n2147483648-service:5000/",
                            SqlServerConnectionString = "Server=127.0.0.1;User Id=sa;Password=pwd;Encrypt=false;Authentication=SqlPassword;Initial Catalog=aatest_group_n2147483648"
                        }
                    },
                    DistributedOperationCoordinatorUrl = "http://192.168.0.251:7000/"
                }
            };
        }

        #pragma warning restore 1591

        #endregion
    }
}