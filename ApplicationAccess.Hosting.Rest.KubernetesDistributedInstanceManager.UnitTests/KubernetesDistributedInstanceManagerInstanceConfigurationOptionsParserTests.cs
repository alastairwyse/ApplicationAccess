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
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using NUnit.Framework;
using NUnit.Framework.Internal;


namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.KubernetesDistributedInstanceManagerInstanceConfigurationOptionsParser class.
    /// </summary>
    public class KubernetesDistributedInstanceManagerInstanceConfigurationOptionsParserTests
    {
        private InstanceConfigurationOptions instanceConfigurationOptions;
        private KubernetesDistributedInstanceManagerInstanceConfigurationOptionsParser testInstanceConfigurationOptionsParser;

        [SetUp]
        protected void SetUp()
        {
            instanceConfigurationOptions = CreateInstanceConfigurationOptions();
            testInstanceConfigurationOptionsParser = new KubernetesDistributedInstanceManagerInstanceConfigurationOptionsParser();
        }

        [Test]
        public void Parse_ShardGroupConfigurationOptionsNodeClientUrlsInvalid()
        {
            void AssertExceptionThrownWhenNodeClientUrlInvalid
            (
                Action<InstanceConfigurationOptions> instanceOptionsSetupAction, 
                String optionsPropertyName, 
                String optionsPropertyValue
            )
            {
                instanceConfigurationOptions = CreateInstanceConfigurationOptions();
                instanceOptionsSetupAction(instanceConfigurationOptions);

                var e = Assert.Throws<ValidationException>(delegate
                {
                    testInstanceConfigurationOptionsParser.Parse(instanceConfigurationOptions);
                });

                Assert.That(e.Message, Does.StartWith($"Error validating DistributedAccessManagerInstance options.  Error validating InstanceConfiguration options.  Error validating ShardGroupConfiguration options.  '{optionsPropertyName}' with value '{optionsPropertyValue}' contains an invalid URL."));
            }

            AssertExceptionThrownWhenNodeClientUrlInvalid((instOps) => { instOps.UserShardGroupConfiguration[0].ReaderNodeClientUrl = "invalid1"; }, "ReaderNodeClientUrl", "invalid1");
            AssertExceptionThrownWhenNodeClientUrlInvalid((instOps) => { instOps.UserShardGroupConfiguration[0].WriterNodeClientUrl = "invalid2"; }, "WriterNodeClientUrl", "invalid2");
            AssertExceptionThrownWhenNodeClientUrlInvalid((instOps) => { instOps.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientUrl = "invalid3"; }, "ReaderNodeClientUrl", "invalid3");
            AssertExceptionThrownWhenNodeClientUrlInvalid((instOps) => { instOps.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientUrl = "invalid4"; }, "WriterNodeClientUrl", "invalid4");
            AssertExceptionThrownWhenNodeClientUrlInvalid((instOps) => { instOps.GroupShardGroupConfiguration[0].ReaderNodeClientUrl = "invalid5"; }, "ReaderNodeClientUrl", "invalid5");
            AssertExceptionThrownWhenNodeClientUrlInvalid((instOps) => { instOps.GroupShardGroupConfiguration[0].WriterNodeClientUrl = "invalid6"; }, "WriterNodeClientUrl", "invalid6");
        }

        [Test]
        public void Parse_UrlsInvalid()
        {
            void AssertExceptionThrownWhenUrlInvalid
            (
                Action<InstanceConfigurationOptions> instanceOptionsSetupAction,
                String optionsPropertyName,
                String optionsPropertyValue
            )
            {
                instanceConfigurationOptions = CreateInstanceConfigurationOptions();
                instanceOptionsSetupAction(instanceConfigurationOptions);

                var e = Assert.Throws<ValidationException>(delegate
                {
                    testInstanceConfigurationOptionsParser.Parse(instanceConfigurationOptions);
                });

                Assert.That(e.Message, Does.StartWith($"Error validating DistributedAccessManagerInstance options.  Error validating InstanceConfiguration options.  '{optionsPropertyName}' with value '{optionsPropertyValue}' contains an invalid URL."));
            }

            AssertExceptionThrownWhenUrlInvalid((instOps) => { instOps.DistributedOperationRouterUrl = "invalid1"; }, "DistributedOperationRouterUrl", "invalid1");
            AssertExceptionThrownWhenUrlInvalid((instOps) => { instOps.Writer1Url = "invalid1"; }, "Writer1Url", "invalid1");
            AssertExceptionThrownWhenUrlInvalid((instOps) => { instOps.Writer2Url = "invalid1"; }, "Writer2Url", "invalid1");
            AssertExceptionThrownWhenUrlInvalid((instOps) => { instOps.DistributedOperationCoordinatorUrl = "invalid1"; }, "DistributedOperationCoordinatorUrl", "invalid1");
        }

        [Test]
        public void Parse_InstanceConfigurationOptionsNull()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> result = testInstanceConfigurationOptionsParser.Parse(null);

            Assert.IsNull(result);
        }

        [Test]
        public void Parse_InstanceConfigurationOptionsPartiallyPopulated()
        {
            instanceConfigurationOptions.ShardConfigurationSqlServerConnectionString = null;
            instanceConfigurationOptions.UserShardGroupConfiguration = null;
            instanceConfigurationOptions.GroupToGroupMappingShardGroupConfiguration = null;
            instanceConfigurationOptions.GroupShardGroupConfiguration = null;
            instanceConfigurationOptions.DistributedOperationCoordinatorUrl = null;

            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> result = testInstanceConfigurationOptionsParser.Parse(instanceConfigurationOptions);

            Assert.AreEqual(instanceConfigurationOptions.DistributedOperationRouterUrl, result.DistributedOperationRouterUrl.ToString());
            Assert.AreEqual(instanceConfigurationOptions.Writer1Url, result.Writer1Url.ToString());
            Assert.AreEqual(instanceConfigurationOptions.Writer2Url, result.Writer2Url.ToString());
            Assert.IsNull(result.ShardConfigurationPersistentStorageCredentials);
            Assert.IsNull(result.UserShardGroupConfiguration);
            Assert.IsNull(result.GroupToGroupMappingShardGroupConfiguration);
            Assert.IsNull(result.GroupShardGroupConfiguration);
            Assert.IsNull(result.DistributedOperationCoordinatorUrl);
        }

        [Test]
        public void Parse_InstanceConfigurationOptionsFullyPopulated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<SqlServerLoginCredentials> result = testInstanceConfigurationOptionsParser.Parse(instanceConfigurationOptions);

            Assert.AreEqual(instanceConfigurationOptions.DistributedOperationRouterUrl, result.DistributedOperationRouterUrl.ToString());
            Assert.AreEqual(instanceConfigurationOptions.Writer1Url, result.Writer1Url.ToString());
            Assert.AreEqual(instanceConfigurationOptions.Writer2Url, result.Writer2Url.ToString());
            Assert.AreEqual(instanceConfigurationOptions.ShardConfigurationSqlServerConnectionString, result.ShardConfigurationPersistentStorageCredentials.ConnectionString);
            Assert.AreEqual(1, result.UserShardGroupConfiguration.Count);
            Assert.AreEqual(instanceConfigurationOptions.UserShardGroupConfiguration[0].HashRangeStart, result.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(instanceConfigurationOptions.UserShardGroupConfiguration[0].SqlServerConnectionString, result.UserShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual(instanceConfigurationOptions.UserShardGroupConfiguration[0].ReaderNodeClientUrl, result.UserShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(instanceConfigurationOptions.UserShardGroupConfiguration[0].WriterNodeClientUrl, result.UserShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(1, result.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(instanceConfigurationOptions.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart, result.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(instanceConfigurationOptions.GroupToGroupMappingShardGroupConfiguration[0].SqlServerConnectionString, result.GroupToGroupMappingShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual(instanceConfigurationOptions.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientUrl, result.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(instanceConfigurationOptions.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientUrl, result.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(1, result.GroupShardGroupConfiguration.Count);
            Assert.AreEqual(instanceConfigurationOptions.GroupShardGroupConfiguration[0].HashRangeStart, result.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(instanceConfigurationOptions.GroupShardGroupConfiguration[0].SqlServerConnectionString, result.GroupShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual(instanceConfigurationOptions.GroupShardGroupConfiguration[0].ReaderNodeClientUrl, result.GroupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(instanceConfigurationOptions.GroupShardGroupConfiguration[0].WriterNodeClientUrl, result.GroupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(instanceConfigurationOptions.DistributedOperationCoordinatorUrl, result.DistributedOperationCoordinatorUrl.ToString());
        }

        #region Private/Protected Methods

        #pragma warning disable 1591

        protected InstanceConfigurationOptions CreateInstanceConfigurationOptions()
        {
            return new InstanceConfigurationOptions
            {
                DistributedOperationRouterUrl = "http://192.168.0.200:7001/",
                Writer1Url = "http://192.168.0.200:7002/",
                Writer2Url = "http://192.168.0.200:7003/",
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
            };
        }

        #pragma warning restore 1591

        #endregion
    }
}
