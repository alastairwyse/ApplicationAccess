/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Linq;
using System.Threading;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using MoreComplexDataStructures;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.ShardClientManager class.
    /// </summary>
    public class ShardClientManagerTests
    {
        private IMetricLogger mockMetricLogger;
        private IDistributedAccessManagerAsyncClientFactory<AccessManagerRestClientConfiguration, String, String, String, String> mockClientFactory;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockClientFactory = Substitute.For<IDistributedAccessManagerAsyncClientFactory<AccessManagerRestClientConfiguration, String, String, String, String>>();
        }

        [Test]
        public void Constructor()
        {
            var testClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var testClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var testShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, Int32.MinValue, testClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>() { testShardConfiguration }
            );
            mockClientFactory.GetClient(testClientConfiguration).Returns(testClient);

            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {
                mockClientFactory.Received(1).GetClient(testClientConfiguration);
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap.Count);
                var dataElementAndOperation = new DataElementAndOperation(DataElement.User, Operation.Query);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(dataElementAndOperation));
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap[dataElementAndOperation].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[dataElementAndOperation], testClient, testShardConfiguration.Describe(false));
                Assert.AreEqual(1, testShardClientManager.HashRangeToClientMap.Count);
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(dataElementAndOperation));
                Assert.AreEqual(1, testShardClientManager.HashRangeToClientMap[dataElementAndOperation].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[dataElementAndOperation], Int32.MinValue, testClient, testShardConfiguration.Describe(true));
                Assert.AreEqual(1, testShardClientManager.CurrentConfiguration.Items.Count());
                Assert.IsTrue(testShardClientManager.CurrentConfiguration.Items.Contains(testShardConfiguration));
            }
        }
        
        [Test]
        public void Constructor_MultipleShardsAssignedToOneClient()
        {
            var userQueryClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQueryClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, 0, userQueryClientConfiguration);
            var userQuery4ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(2, DataElement.User, Operation.Query, 4, userQueryClientConfiguration);
            var userQuery8ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(3, DataElement.User, Operation.Query, 8, userQueryClientConfiguration);
            var userQuery12ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(4, DataElement.User, Operation.Query, 12, userQueryClientConfiguration);
            var userQuery16ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(5, DataElement.User, Operation.Query, 16, userQueryClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
                {
                    userQuery0ShardConfiguration,
                    userQuery4ShardConfiguration,
                    userQuery8ShardConfiguration,
                    userQuery12ShardConfiguration,
                    userQuery16ShardConfiguration
                }
            );
            mockClientFactory.GetClient(userQueryClientConfiguration).Returns(userQueryClient);

            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {
                mockClientFactory.Received(1).GetClient(userQueryClientConfiguration);
                var userQuery = new DataElementAndOperation(DataElement.User, Operation.Query);
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap.Count);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(userQuery));
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap[userQuery].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[userQuery], userQueryClient, userQuery0ShardConfiguration.Describe(false));
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(userQuery));
                Assert.AreEqual(5, testShardClientManager.HashRangeToClientMap[userQuery].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userQuery], 0, userQueryClient, userQuery0ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userQuery], 4, userQueryClient, userQuery4ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userQuery], 8, userQueryClient, userQuery8ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userQuery], 12, userQueryClient, userQuery12ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userQuery], 16, userQueryClient, userQuery16ShardConfiguration.Describe(true));
            }
        }
        
        [Test]
        public void RefreshConfiguration_ConfigurationNotChanged()
        {
            var testClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var testClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var testShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, Int32.MinValue, testClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>() { testShardConfiguration }
            );
            var testRefreshClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var testRefreshClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var testRefreshShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, Int32.MinValue, testClientConfiguration);
            var testRefreshShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>() { testShardConfiguration }
            );
            mockClientFactory.GetClient(testClientConfiguration).Returns(testClient, testRefreshClient);
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {

                testShardClientManager.RefreshConfiguration(testRefreshShardConfigurationSet);

                mockClientFactory.Received(1).GetClient(testClientConfiguration);
                var dataElementAndOperation = new DataElementAndOperation(DataElement.User, Operation.Query);
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap[dataElementAndOperation].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[dataElementAndOperation], testClient, testShardConfiguration.Describe(false));

            }
        }
        
        [Test]
        public void RefreshConfiguration()
        {
            var testInitialShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testInitialShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {
                // Setup test client and shard config
                var userQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
                var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
                var userQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5001/"));
                var userQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(2, DataElement.User, Operation.Query, 32, userQuery32ClientConfiguration);
                var userEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5002/"));
                var userEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(3, DataElement.User, Operation.Event, 0, userEvent0ClientConfiguration);
                var userEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5003/"));
                var userEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(4, DataElement.User, Operation.Event, 32, userEvent32ClientConfiguration);
                var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5004/"));
                var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(5, DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
                var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5005/"));
                var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(6, DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
                var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5006/"));
                var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(7, DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
                var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5007/"));
                var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(8, DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
                var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5008/"));
                var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(9, DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
                var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5009/"));
                var groupToGroupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(10, DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
                var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
                (
                    new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
                    {
                    userQuery0ShardConfiguration,
                    userQuery32ShardConfiguration,
                    userEvent0ShardConfiguration,
                    userEvent32ShardConfiguration,
                    groupQuery0ShardConfiguration,
                    groupQuery32ShardConfiguration,
                    groupEvent0ShardConfiguration,
                    groupEvent32ShardConfiguration,
                    groupToGroupQuery0ShardConfiguration,
                    groupToGroupEvent0ShardConfiguration
                    }
                );
                mockClientFactory.GetClient(userQuery0ClientConfiguration).Returns(userQuery0Client);
                mockClientFactory.GetClient(userQuery32ClientConfiguration).Returns(userQuery32Client);
                mockClientFactory.GetClient(userEvent0ClientConfiguration).Returns(userEvent0Client);
                mockClientFactory.GetClient(userEvent32ClientConfiguration).Returns(userEvent32Client);
                mockClientFactory.GetClient(groupQuery0ClientConfiguration).Returns(groupQuery0Client);
                mockClientFactory.GetClient(groupQuery32ClientConfiguration).Returns(groupQuery32Client);
                mockClientFactory.GetClient(groupEvent0ClientConfiguration).Returns(groupEvent0Client);
                mockClientFactory.GetClient(groupEvent32ClientConfiguration).Returns(groupEvent32Client);
                mockClientFactory.GetClient(groupToGroupQuery0ClientConfiguration).Returns(groupToGroupQuery0Client);
                mockClientFactory.GetClient(groupToGroupEvent0ClientConfiguration).Returns(groupToGroupEvent0Client);

                testShardClientManager.RefreshConfiguration(testShardConfigurationSet);

                mockClientFactory.Received(1).GetClient(userQuery0ClientConfiguration);
                mockClientFactory.Received(1).GetClient(userQuery32ClientConfiguration);
                mockClientFactory.Received(1).GetClient(userEvent0ClientConfiguration);
                mockClientFactory.Received(1).GetClient(userEvent32ClientConfiguration);
                mockClientFactory.Received(1).GetClient(groupQuery0ClientConfiguration);
                mockClientFactory.Received(1).GetClient(groupQuery32ClientConfiguration);
                mockClientFactory.Received(1).GetClient(groupEvent0ClientConfiguration);
                mockClientFactory.Received(1).GetClient(groupEvent32ClientConfiguration);
                mockClientFactory.Received(1).GetClient(groupToGroupQuery0ClientConfiguration);
                mockClientFactory.Received(1).GetClient(groupToGroupEvent0ClientConfiguration);
                var userQuery = new DataElementAndOperation(DataElement.User, Operation.Query);
                var userEvent = new DataElementAndOperation(DataElement.User, Operation.Event);
                var groupQuery = new DataElementAndOperation(DataElement.Group, Operation.Query);
                var groupEvent = new DataElementAndOperation(DataElement.Group, Operation.Event);
                var groupToGroupQuery = new DataElementAndOperation(DataElement.GroupToGroupMapping, Operation.Query);
                var groupToGroupEvent = new DataElementAndOperation(DataElement.GroupToGroupMapping, Operation.Event);
                // Check contents of 'dataElementAndOperationToClientMap'
                Assert.AreEqual(6, testShardClientManager.DataElementAndOperationToClientMap.Count);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(userQuery));
                Assert.AreEqual(2, testShardClientManager.DataElementAndOperationToClientMap[userQuery].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[userQuery], userQuery0Client, userQuery0ShardConfiguration.Describe(false));
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[userQuery], userQuery32Client, userQuery32ShardConfiguration.Describe(false));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(userEvent));
                Assert.AreEqual(2, testShardClientManager.DataElementAndOperationToClientMap[userEvent].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[userEvent], userEvent0Client, userEvent0ShardConfiguration.Describe(false));
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[userEvent], userEvent32Client, userEvent32ShardConfiguration.Describe(false));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupQuery));
                Assert.AreEqual(2, testShardClientManager.DataElementAndOperationToClientMap[groupQuery].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[groupQuery], groupQuery0Client, groupQuery0ShardConfiguration.Describe(false));
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[groupQuery], groupQuery32Client, groupQuery32ShardConfiguration.Describe(false));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupEvent));
                Assert.AreEqual(2, testShardClientManager.DataElementAndOperationToClientMap[groupEvent].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[groupEvent], groupEvent0Client, groupEvent0ShardConfiguration.Describe(false));
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[groupEvent], groupEvent32Client, groupEvent32ShardConfiguration.Describe(false));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupToGroupQuery));
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap[groupToGroupQuery].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[groupToGroupQuery], groupToGroupQuery0Client, groupToGroupQuery0ShardConfiguration.Describe(false));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupToGroupEvent));
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap[groupToGroupEvent].Count);
                AssertHashSetContainsClientAndDescription(testShardClientManager.DataElementAndOperationToClientMap[groupToGroupEvent], groupToGroupEvent0Client, groupToGroupEvent0ShardConfiguration.Describe(false));
                // Check contents of 'hashRangeToClientMap'
                Assert.AreEqual(6, testShardClientManager.HashRangeToClientMap.Count);
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(userQuery));
                Assert.AreEqual(2, testShardClientManager.HashRangeToClientMap[userQuery].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userQuery], 0, userQuery0Client, userQuery0ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userQuery], 32, userQuery32Client, userQuery32ShardConfiguration.Describe(true));
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(userEvent));
                Assert.AreEqual(2, testShardClientManager.HashRangeToClientMap[userEvent].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userEvent], 0, userEvent0Client, userEvent0ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[userEvent], 32, userEvent32Client, userEvent32ShardConfiguration.Describe(true));
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(groupQuery));
                Assert.AreEqual(2, testShardClientManager.HashRangeToClientMap[groupQuery].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[groupQuery], 0, groupQuery0Client, groupQuery0ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[groupQuery], 32, groupQuery32Client, groupQuery32ShardConfiguration.Describe(true));
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(groupEvent));
                Assert.AreEqual(2, testShardClientManager.HashRangeToClientMap[groupEvent].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[groupEvent], 0, groupEvent0Client, groupEvent0ShardConfiguration.Describe(true));
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[groupEvent], 32, groupEvent32Client, groupEvent32ShardConfiguration.Describe(true));
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(groupToGroupQuery));
                Assert.AreEqual(1, testShardClientManager.HashRangeToClientMap[groupToGroupQuery].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[groupToGroupQuery], 0, groupToGroupQuery0Client, groupToGroupQuery0ShardConfiguration.Describe(true));
                Assert.IsTrue(testShardClientManager.HashRangeToClientMap.ContainsKey(groupToGroupEvent));
                Assert.AreEqual(1, testShardClientManager.HashRangeToClientMap[groupToGroupEvent].Count);
                AssertTreeContainsClientAndDescription(testShardClientManager.HashRangeToClientMap[groupToGroupEvent], 0, groupToGroupEvent0Client, groupToGroupEvent0ShardConfiguration.Describe(true));
                // Check contents of 'currentConfiguration'
                Assert.AreEqual(10, testShardClientManager.CurrentConfiguration.Items.Count());
            }
        }

        [Test]
        public void RefreshConfiguration_MetricsLogged()
        {
            Guid testBeginId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            mockMetricLogger.Begin(Arg.Any<ConfigurationRefreshTime>()).Returns(testBeginId);
            var testInitialShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testInitialShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator(), mockMetricLogger))
            {
                var userQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
                var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
                var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
                (
                    new List<ShardConfiguration<AccessManagerRestClientConfiguration>>(){ userQuery0ShardConfiguration }
                );
                mockClientFactory.GetClient(userQuery0ClientConfiguration).Returns(userQuery0Client);

                testShardClientManager.RefreshConfiguration(testShardConfigurationSet);

                mockMetricLogger.Received(1).Begin(Arg.Any<ConfigurationRefreshTime>());
                mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ConfigurationRefreshTime>());
                mockMetricLogger.Received(1).Increment(Arg.Any<ConfigurationRefreshed>());
            }
        }
        
        [Test]
        public void RefreshConfiguration_ExceptionWhenRefreshing()
        {
            Guid testBeginId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            mockMetricLogger.Begin(Arg.Any<ConfigurationRefreshTime>()).Returns(testBeginId);
            var testInitialShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testInitialShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator(), mockMetricLogger))
            {
                var userQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
                var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
                var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
                (
                    new List<ShardConfiguration<AccessManagerRestClientConfiguration>>() { userQuery0ShardConfiguration }
                );
                mockClientFactory.When((factory) => factory.GetClient(userQuery0ClientConfiguration)).Do((callInfo) => throw new Exception("Mock exception"));

                var e = Assert.Throws<Exception>(delegate
                {
                    testShardClientManager.RefreshConfiguration(testShardConfigurationSet);
                });
                
                mockMetricLogger.Received(1).Begin(Arg.Any<ConfigurationRefreshTime>());
                mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ConfigurationRefreshTime>());
                Assert.AreEqual(2, mockMetricLogger.ReceivedCalls().Count());
            }
        }
        
        [Test]
        public void GetAllClients()
        {
            var userQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
            var userQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5001/"));
            var userQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(2, DataElement.User, Operation.Query, 32, userQuery32ClientConfiguration);
            var userEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5002/"));
            var userEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(3, DataElement.User, Operation.Event, 0, userEvent0ClientConfiguration);
            var userEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5003/"));
            var userEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(4, DataElement.User, Operation.Event, 32, userEvent32ClientConfiguration);
            var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5004/"));
            var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(5, DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
            var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5005/"));
            var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(6, DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
            var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5006/"));
            var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(7, DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
            var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5007/"));
            var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(8, DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
            var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5008/"));
            var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(9, DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
            var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5009/"));
            var groupToGroupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(10, DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
                {
                    userQuery0ShardConfiguration,
                    userQuery32ShardConfiguration,
                    userEvent0ShardConfiguration,
                    userEvent32ShardConfiguration,
                    groupQuery0ShardConfiguration,
                    groupQuery32ShardConfiguration,
                    groupEvent0ShardConfiguration,
                    groupEvent32ShardConfiguration,
                    groupToGroupQuery0ShardConfiguration,
                    groupToGroupEvent0ShardConfiguration
                }
            );
            mockClientFactory.GetClient(userQuery0ClientConfiguration).Returns(userQuery0Client);
            mockClientFactory.GetClient(userQuery32ClientConfiguration).Returns(userQuery32Client);
            mockClientFactory.GetClient(userEvent0ClientConfiguration).Returns(userEvent0Client);
            mockClientFactory.GetClient(userEvent32ClientConfiguration).Returns(userEvent32Client);
            mockClientFactory.GetClient(groupQuery0ClientConfiguration).Returns(groupQuery0Client);
            mockClientFactory.GetClient(groupQuery32ClientConfiguration).Returns(groupQuery32Client);
            mockClientFactory.GetClient(groupEvent0ClientConfiguration).Returns(groupEvent0Client);
            mockClientFactory.GetClient(groupEvent32ClientConfiguration).Returns(groupEvent32Client);
            mockClientFactory.GetClient(groupToGroupQuery0ClientConfiguration).Returns(groupToGroupQuery0Client);
            mockClientFactory.GetClient(groupToGroupEvent0ClientConfiguration).Returns(groupToGroupEvent0Client);
            var userQuery = new DataElementAndOperation(DataElement.User, Operation.Query);
            var groupEvent = new DataElementAndOperation(DataElement.Group, Operation.Event);
            var groupToGroupQuery = new DataElementAndOperation(DataElement.GroupToGroupMapping, Operation.Query);
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {

                var result = new HashSet<DistributedClientAndShardDescription>(testShardClientManager.GetAllClients(DataElement.User, Operation.Query));

                Assert.AreEqual(2, result.Count);
                AssertHashSetContainsClientAndDescription(result, userQuery0Client, userQuery0ShardConfiguration.Describe(false));
                AssertHashSetContainsClientAndDescription(result, userQuery32Client, userQuery32ShardConfiguration.Describe(false));


                result = new HashSet<DistributedClientAndShardDescription>(testShardClientManager.GetAllClients(DataElement.Group, Operation.Event));

                Assert.AreEqual(2, result.Count);
                AssertHashSetContainsClientAndDescription(result, groupEvent0Client, groupEvent0ShardConfiguration.Describe(false));
                AssertHashSetContainsClientAndDescription(result, groupEvent32Client, groupEvent32ShardConfiguration.Describe(false));


                result = new HashSet<DistributedClientAndShardDescription>(testShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query));

                Assert.AreEqual(1, result.Count);
                AssertHashSetContainsClientAndDescription(result, groupToGroupQuery0Client, groupToGroupQuery0ShardConfiguration.Describe(false));
            }
        }

        [Test]
        public void GetAllClients_ShardConfigurationDoesntExist()
        {
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {

                var e = Assert.Throws<ArgumentException>(delegate
                {
                    testShardClientManager.GetAllClients(DataElement.User, Operation.Query);
                });

                Assert.That(e.Message, Does.StartWith("No shard configuration exists for DataElement 'User' and Operation 'Query'."));
            }
        }

        [Test]
        public void GetClientDataElementValueOverload()
        {
            IHashCodeGenerator<String> mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            IHashCodeGenerator<String> mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            var userQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
            var userQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5001/"));
            var userQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(2, DataElement.User, Operation.Query, 32, userQuery32ClientConfiguration);
            var userEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5002/"));
            var userEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(3, DataElement.User, Operation.Event, 0, userEvent0ClientConfiguration);
            var userEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5003/"));
            var userEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(4, DataElement.User, Operation.Event, 32, userEvent32ClientConfiguration);
            var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5004/"));
            var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(5, DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
            var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5005/"));
            var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(6, DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
            var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5006/"));
            var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(7, DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
            var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5007/"));
            var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(8, DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
            var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5008/"));
            var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(9, DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
            var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5009/"));
            var groupToGroupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(10, DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
                {
                    userQuery0ShardConfiguration,
                    userQuery32ShardConfiguration,
                    userEvent0ShardConfiguration,
                    userEvent32ShardConfiguration,
                    groupQuery0ShardConfiguration,
                    groupQuery32ShardConfiguration,
                    groupEvent0ShardConfiguration,
                    groupEvent32ShardConfiguration,
                    groupToGroupQuery0ShardConfiguration,
                    groupToGroupEvent0ShardConfiguration
                }
            );
            mockClientFactory.GetClient(userQuery0ClientConfiguration).Returns(userQuery0Client);
            mockClientFactory.GetClient(userQuery32ClientConfiguration).Returns(userQuery32Client);
            mockClientFactory.GetClient(userEvent0ClientConfiguration).Returns(userEvent0Client);
            mockClientFactory.GetClient(userEvent32ClientConfiguration).Returns(userEvent32Client);
            mockClientFactory.GetClient(groupQuery0ClientConfiguration).Returns(groupQuery0Client);
            mockClientFactory.GetClient(groupQuery32ClientConfiguration).Returns(groupQuery32Client);
            mockClientFactory.GetClient(groupEvent0ClientConfiguration).Returns(groupEvent0Client);
            mockClientFactory.GetClient(groupEvent32ClientConfiguration).Returns(groupEvent32Client);
            mockClientFactory.GetClient(groupToGroupQuery0ClientConfiguration).Returns(groupToGroupQuery0Client);
            mockClientFactory.GetClient(groupToGroupEvent0ClientConfiguration).Returns(groupToGroupEvent0Client);
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, mockUserHashCodeGenerator, mockGroupHashCodeGenerator))
            {
                mockUserHashCodeGenerator.GetHashCode("user1").Returns(32);

                DistributedClientAndShardDescription result = testShardClientManager.GetClient(DataElement.User, Operation.Query, "user1");

                Assert.AreSame(userQuery32Client, result.Client);
                Assert.AreEqual(userQuery32ShardConfiguration.Describe(true), result.ShardConfigurationDescription);


                mockGroupHashCodeGenerator.GetHashCode("group1").Returns(Int32.MaxValue);

                result = testShardClientManager.GetClient(DataElement.Group, Operation.Query, "group1");

                Assert.AreSame(groupQuery32Client, result.Client);
                Assert.AreEqual(groupQuery32ShardConfiguration.Describe(true), result.ShardConfigurationDescription);


                mockGroupHashCodeGenerator.GetHashCode("group2").Returns(1);

                result = testShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, "group2");

                Assert.AreSame(groupToGroupEvent0Client, result.Client);
                Assert.AreEqual(groupToGroupEvent0ShardConfiguration.Describe(true), result.ShardConfigurationDescription);
            }
        }

        [Test]
        public void GetClientDataElementValueOverload_ShardConfigurationDoesntExist()
        {
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {

                var e = Assert.Throws<ArgumentException>(delegate
                {
                    testShardClientManager.GetClient(DataElement.User, Operation.Query, "user1");
                });

                Assert.That(e.Message, Does.StartWith("No shard configuration exists for DataElement 'User' and Operation 'Query'."));
            }
        }

        [Test]
        public void GetClientHashCodeOverload()
        {
            IHashCodeGenerator<String> mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            IHashCodeGenerator<String> mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            var userQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5000/"));
            var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
            var userQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5001/"));
            var userQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(2, DataElement.User, Operation.Query, 32, userQuery32ClientConfiguration);
            var userEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5002/"));
            var userEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(3, DataElement.User, Operation.Event, 0, userEvent0ClientConfiguration);
            var userEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5003/"));
            var userEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(4, DataElement.User, Operation.Event, 32, userEvent32ClientConfiguration);
            var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5004/"));
            var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(5, DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
            var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5005/"));
            var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(6, DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
            var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5006/"));
            var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(7, DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
            var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5007/"));
            var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(8, DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
            var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5008/"));
            var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(9, DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
            var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5009/"));
            var groupToGroupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(10, DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
                {
                    userQuery0ShardConfiguration,
                    userQuery32ShardConfiguration,
                    userEvent0ShardConfiguration,
                    userEvent32ShardConfiguration,
                    groupQuery0ShardConfiguration,
                    groupQuery32ShardConfiguration,
                    groupEvent0ShardConfiguration,
                    groupEvent32ShardConfiguration,
                    groupToGroupQuery0ShardConfiguration,
                    groupToGroupEvent0ShardConfiguration
                }
            );
            mockClientFactory.GetClient(userQuery0ClientConfiguration).Returns(userQuery0Client);
            mockClientFactory.GetClient(userQuery32ClientConfiguration).Returns(userQuery32Client);
            mockClientFactory.GetClient(userEvent0ClientConfiguration).Returns(userEvent0Client);
            mockClientFactory.GetClient(userEvent32ClientConfiguration).Returns(userEvent32Client);
            mockClientFactory.GetClient(groupQuery0ClientConfiguration).Returns(groupQuery0Client);
            mockClientFactory.GetClient(groupQuery32ClientConfiguration).Returns(groupQuery32Client);
            mockClientFactory.GetClient(groupEvent0ClientConfiguration).Returns(groupEvent0Client);
            mockClientFactory.GetClient(groupEvent32ClientConfiguration).Returns(groupEvent32Client);
            mockClientFactory.GetClient(groupToGroupQuery0ClientConfiguration).Returns(groupToGroupQuery0Client);
            mockClientFactory.GetClient(groupToGroupEvent0ClientConfiguration).Returns(groupToGroupEvent0Client);
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, mockUserHashCodeGenerator, mockGroupHashCodeGenerator))
            {
                DistributedClientAndShardDescription result = testShardClientManager.GetClient(DataElement.User, Operation.Query, 32);

                Assert.AreSame(userQuery32Client, result.Client);
                Assert.AreEqual(userQuery32ShardConfiguration.Describe(true), result.ShardConfigurationDescription);


                result = testShardClientManager.GetClient(DataElement.Group, Operation.Query, Int32.MaxValue);

                Assert.AreSame(groupQuery32Client, result.Client);
                Assert.AreEqual(groupQuery32ShardConfiguration.Describe(true), result.ShardConfigurationDescription);


                result = testShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, 1);

                Assert.AreSame(groupToGroupEvent0Client, result.Client);
                Assert.AreEqual(groupToGroupEvent0ShardConfiguration.Describe(true), result.ShardConfigurationDescription);
            }
        }

        [Test]
        public void GetClientHashCodeOverload_ShardConfigurationDoesntExist()
        {
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {

                var e = Assert.Throws<ArgumentException>(delegate
                {
                    testShardClientManager.GetClient(DataElement.User, Operation.Query, 0);
                });

                Assert.That(e.Message, Does.StartWith("No shard configuration exists for DataElement 'User' and Operation 'Query'."));
            }
        }

        [Test]
        public void GetClients()
        {
            IHashCodeGenerator<String> mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            IHashCodeGenerator<String> mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5004/"));
            var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(1, DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
            var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5005/"));
            var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(2, DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
            var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5006/"));
            var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(3, DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
            var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5007/"));
            var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(4, DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
            var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5008/"));
            var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(5, DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
            var groupToGroupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5009/"));
            var groupToGroupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(6, DataElement.GroupToGroupMapping, Operation.Query, 32, groupToGroupQuery32ClientConfiguration);
            var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5010/"));
            var groupToGroupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(7, DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
                {
                    groupQuery0ShardConfiguration,
                    groupQuery32ShardConfiguration,
                    groupEvent0ShardConfiguration,
                    groupEvent32ShardConfiguration,
                    groupToGroupQuery0ShardConfiguration,
                    groupToGroupQuery32ShardConfiguration, 
                    groupToGroupEvent0ShardConfiguration
                }
            );
            mockClientFactory.GetClient(groupQuery0ClientConfiguration).Returns(groupQuery0Client);
            mockClientFactory.GetClient(groupQuery32ClientConfiguration).Returns(groupQuery32Client);
            mockClientFactory.GetClient(groupEvent0ClientConfiguration).Returns(groupEvent0Client);
            mockClientFactory.GetClient(groupEvent32ClientConfiguration).Returns(groupEvent32Client);
            mockClientFactory.GetClient(groupToGroupQuery0ClientConfiguration).Returns(groupToGroupQuery0Client);
            mockClientFactory.GetClient(groupToGroupQuery32ClientConfiguration).Returns(groupToGroupQuery32Client);
            mockClientFactory.GetClient(groupToGroupEvent0ClientConfiguration).Returns(groupToGroupEvent0Client);
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, mockUserHashCodeGenerator, mockGroupHashCodeGenerator))
            {
                var testGroups = new List<String>() { "group1", "group2", "group3", "group4", "group5", "group6"};
                mockGroupHashCodeGenerator.GetHashCode(testGroups[0]).Returns(0);
                mockGroupHashCodeGenerator.GetHashCode(testGroups[1]).Returns(32);
                mockGroupHashCodeGenerator.GetHashCode(testGroups[2]).Returns(1);
                mockGroupHashCodeGenerator.GetHashCode(testGroups[3]).Returns(33);
                mockGroupHashCodeGenerator.GetHashCode(testGroups[4]).Returns(2);
                mockGroupHashCodeGenerator.GetHashCode(testGroups[5]).Returns(-1);

                IEnumerable<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>> result = testShardClientManager.GetClients(DataElement.Group, Operation.Query, testGroups);

                // TODO: These asserts assume an order in the returned IEnumerable which isn't deteministic (underlying structure is Dictionary)... tests may break if Dictionary implementation changes
                Assert.AreEqual(groupQuery0Client, result.ElementAt(0).Item1.Client);
                Assert.IsTrue(result.ElementAt(0).Item2.Contains(testGroups[0]));
                Assert.IsTrue(result.ElementAt(0).Item2.Contains(testGroups[2]));
                Assert.IsTrue(result.ElementAt(0).Item2.Contains(testGroups[4]));
                Assert.AreEqual(3, result.ElementAt(0).Item2.Count());
                Assert.AreEqual(groupQuery32Client, result.ElementAt(1).Item1.Client);
                Assert.IsTrue(result.ElementAt(1).Item2.Contains(testGroups[1]));
                Assert.IsTrue(result.ElementAt(1).Item2.Contains(testGroups[3]));
                Assert.IsTrue(result.ElementAt(1).Item2.Contains(testGroups[5]));
                Assert.AreEqual(3, result.ElementAt(1).Item2.Count());
                Assert.AreEqual(2, result.Count());


                testGroups = new List<String>() { "group7", "group8", "group9" };
                mockGroupHashCodeGenerator.GetHashCode(testGroups[0]).Returns(-1);
                mockGroupHashCodeGenerator.GetHashCode(testGroups[1]).Returns(1);
                mockGroupHashCodeGenerator.GetHashCode(testGroups[2]).Returns(50);

                result = testShardClientManager.GetClients(DataElement.GroupToGroupMapping, Operation.Query, testGroups);

                Assert.AreEqual(groupToGroupQuery32Client, result.ElementAt(0).Item1.Client);
                Assert.IsTrue(result.ElementAt(0).Item2.Contains(testGroups[0]));
                Assert.IsTrue(result.ElementAt(0).Item2.Contains(testGroups[2]));
                Assert.AreEqual(2, result.ElementAt(0).Item2.Count());
                Assert.AreEqual(groupToGroupQuery0Client, result.ElementAt(1).Item1.Client);
                Assert.IsTrue(result.ElementAt(1).Item2.Contains(testGroups[1]));
                Assert.AreEqual(1, result.ElementAt(1).Item2.Count());
                Assert.AreEqual(2, result.Count());
            }
        }

        [Test]
        public void GetClients_ShardConfigurationDoesntExist()
        {
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {

                var e = Assert.Throws<ArgumentException>(delegate
                {
                    testShardClientManager.GetClients(DataElement.User, Operation.Query, new List<String>() { "user1", "user2" }).Count();
                });

                Assert.That(e.Message, Does.StartWith("No shard configuration exists for DataElement 'User' and Operation 'Query'."));
            }
        }

        [Test]
        public void GetClientForHashCode()
        {
            var testInitialShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testInitialShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {
                var testTree = new WeightBalancedTree<HashRangeStartClientAndShardDescription>();
                var client1 = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var client5 = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var client1ShardDescription = "client1ShardDescription";
                var client5ShardDescription = "client5ShardDescription";
                testTree.Add(new HashRangeStartClientAndShardDescription(1, new DistributedClientAndShardDescription(client1, client1ShardDescription)));
                testTree.Add(new HashRangeStartClientAndShardDescription(5, new DistributedClientAndShardDescription(client5, client5ShardDescription)));

                DistributedClientAndShardDescription result = testShardClientManager.GetClientForHashCode(testTree, 1);

                Assert.AreSame(client1, result.Client);
                Assert.AreEqual(client1ShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, 2);

                Assert.AreSame(client1, result.Client);
                Assert.AreEqual(client1ShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, 0);

                Assert.AreSame(client5, result.Client);
                Assert.AreEqual(client5ShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, 5);

                Assert.AreSame(client5, result.Client);
                Assert.AreEqual(client5ShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, 6);

                Assert.AreSame(client5, result.Client);
                Assert.AreEqual(client5ShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, 4);

                Assert.AreSame(client1, result.Client);
                Assert.AreEqual(client1ShardDescription, result.ShardConfigurationDescription);


                var clientMax = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var clientMaxShardDescription = "clientMaxShardDescription";
                testTree.Add(new HashRangeStartClientAndShardDescription(Int32.MaxValue, new DistributedClientAndShardDescription(clientMax, clientMaxShardDescription)));

                result = testShardClientManager.GetClientForHashCode(testTree, 0);

                Assert.AreSame(clientMax, result.Client);
                Assert.AreEqual(clientMaxShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, Int32.MinValue);

                Assert.AreSame(clientMax, result.Client);
                Assert.AreEqual(clientMaxShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, Int32.MaxValue);

                Assert.AreSame(clientMax, result.Client);
                Assert.AreEqual(clientMaxShardDescription, result.ShardConfigurationDescription);


                var clientMin = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var clientMinShardDescription = "clientMinShardDescription";
                testTree.Add(new HashRangeStartClientAndShardDescription(Int32.MinValue, new DistributedClientAndShardDescription(clientMin, clientMinShardDescription)));

                result = testShardClientManager.GetClientForHashCode(testTree, 0);

                Assert.AreSame(clientMin, result.Client);
                Assert.AreEqual(clientMinShardDescription, result.ShardConfigurationDescription);


                result = testShardClientManager.GetClientForHashCode(testTree, Int32.MinValue);

                Assert.AreSame(clientMin, result.Client);
                Assert.AreEqual(clientMinShardDescription, result.ShardConfigurationDescription);
            }
        }
        
        #region Private/Protected Methods

        /// <summary>
        /// Asserts whether a HashSet of <see cref="DistributedClientAndShardDescription"/> objects contains the specified client and shard description.
        /// </summary>
        /// <param name="hashSet">The HashSet of <see cref="DistributedClientAndShardDescription"/> objects</param>
        /// <param name="client">The client to check for.</param>
        /// <param name="shardDescription">The shard configuration description to check for.</param>
        /// <remarks><see cref="DistributedClientAndShardDescription"/> only considers the 'Client' property in its <see cref="IEquatable{T}"/> implementation, so this method also explicity checks the 'ShardConfigurationDescription' property.</remarks>
        private void AssertHashSetContainsClientAndDescription
        (
            HashSet<DistributedClientAndShardDescription> hashSet,
            IDistributedAccessManagerAsyncClient<String, String, String, String> client, 
            String shardDescription
        )
        {
            if (hashSet.Contains(new DistributedClientAndShardDescription(client, shardDescription)) == false)
            {
                Assert.Fail("HashSet does not contain the specified client");
            }
            Func<DistributedClientAndShardDescription, Boolean> filter = (current) => 
            {
                return (Object.ReferenceEquals(current.Client, client));
            };
            IEnumerable<DistributedClientAndShardDescription> filterResults = hashSet.Where(filter);
            if (filterResults.Count() != 1)
            {
                Assert.Fail("HashSet did not contain a unique item with the specified client");
            }
            if (filterResults.First().ShardConfigurationDescription != shardDescription)
            {
                Assert.Fail($"'{nameof(DistributedClientAndShardDescription.ShardConfigurationDescription)}' property was expected to be '{shardDescription}' but was '{filterResults.First().ShardConfigurationDescription}'");
            }
        }

        /// <summary>
        /// Asserts whether a tree of <see cref="HashRangeStartClientAndShardDescription"/> objects contains the specified client and shard description.
        /// </summary>
        /// <param name="tree">The tree of <see cref="HashRangeStartClientAndShardDescription"/> objects.</param>
        /// <param name="hashRangeStart">The hash range start value for the client.</param>
        /// <param name="client">The client to check for.</param>
        /// <param name="shardDescription">The shard configuration description to check for.</param>
        /// <remarks><see cref="HashRangeStartClientAndShardDescription"/> only considers the 'HashRangeStart' property in its <see cref="IComparable{T}"/> implementation, so this method also explicity checks the 'ClientAndDescription' property.</remarks>
        private void AssertTreeContainsClientAndDescription
        (
            WeightBalancedTree<HashRangeStartClientAndShardDescription> tree,
            Int32 hashRangeStart, 
            IDistributedAccessManagerAsyncClient<String, String, String, String> client,
            String shardDescription
        )
        {
            var comparisonNodeValue = new HashRangeStartClientAndShardDescription(hashRangeStart, new DistributedClientAndShardDescription(client, shardDescription));
            if (tree.Contains(comparisonNodeValue) == false)
            {
                Assert.Fail("Tree does not contain the specified hash range start value");
            }
            HashRangeStartClientAndShardDescription nodeValue = tree.Get(comparisonNodeValue);
            if (nodeValue.ClientAndDescription.Client != client)
            {
                Assert.Fail($"Tree node with hash range start value {hashRangeStart} does not contain the specified client");
            }
            if (nodeValue.ClientAndDescription.ShardConfigurationDescription != shardDescription)
            {
                Assert.Fail($"'{nameof(DistributedClientAndShardDescription.ShardConfigurationDescription)}' property was expected to be '{shardDescription}' but was '{nodeValue.ClientAndDescription.ShardConfigurationDescription}'");
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Default implementation of <see cref="IHashCodeGenerator{T}"/> for strings which uses the standard <see cref="String.GetHashCode"/> method.
        /// </summary>
        private class StringHashCodeGenerator : IHashCodeGenerator<String>
        {
            /// <inheritdoc/>
            public Int32 GetHashCode(String inputValue)
            {
                return inputValue.GetHashCode();
            }
        }

        /// <summary>
        ///  Version of the ShardClientManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TClientConfiguration">The type of AccessManager client configuration stored in the shard configuration.</typeparam>
        private class ShardClientManagerWithProtectedMembers<TClientConfiguration> : ShardClientManager<TClientConfiguration>
            where TClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<TClientConfiguration>
        {
            /// <summary>Maps a <see cref="DataElement"/> and <see cref="Operation"/> to all clients (and corresponding shard descriptions) connecting to shards which manage that data element and for operations of that type.</summary>
            public Dictionary<DataElementAndOperation, HashSet<DistributedClientAndShardDescription>> DataElementAndOperationToClientMap
            {
                get { return dataElementAndOperationToClientMap; }
            }

            /// <summary>Maps a <see cref="DataElement"/> and <see cref="Operation"/> to a tree which stores clients (and corresponding shard descriptions) connecting to shards for that element/operation, indexed by the range of hash values that client handles.</summary>
            public Dictionary<DataElementAndOperation, WeightBalancedTree<HashRangeStartClientAndShardDescription>> HashRangeToClientMap
            {
                get { return hashRangeToClientMap; }
            }

            /// <summary>The current shard configuration.</summary>
            public ShardConfigurationSet<TClientConfiguration> CurrentConfiguration
            {
                get { return currentConfiguration; }
            }

            /// <summary>Lock object for the structures which store the shard configuration.</summary>
            public ReaderWriterLockSlim ConfigurationLock
            {
                get { return configurationLock; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Distribution.UnitTests.ShardClientManagerTests+ShardClientManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="initialConfiguration">The initial shard configuration.</param>
            /// <param name="clientFactory">An <see cref="IDistributedAccessManagerAsyncClientFactory{TClientConfiguration, TUser, TGroup, TComponent, TAccess}"/> instance used to create AccessManager client instances from configuration.</param>
            /// <param name="userHashCodeGenerator">A hash code generator for users.</param>
            /// <param name="groupHashCodeGenerator">A hash code generator for groups.</param>
            public ShardClientManagerWithProtectedMembers
            (
                ShardConfigurationSet<TClientConfiguration> initialConfiguration,
                IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, String, String, String, String> clientFactory,
                IHashCodeGenerator<String> userHashCodeGenerator,
                IHashCodeGenerator<String> groupHashCodeGenerator
            )
                : base(initialConfiguration, clientFactory, userHashCodeGenerator, groupHashCodeGenerator)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Distribution.UnitTests.ShardClientManagerTests+ShardClientManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="initialConfiguration">The initial shard configuration.</param>
            /// <param name="clientFactory">An <see cref="IDistributedAccessManagerAsyncClientFactory{TClientConfiguration, TUser, TGroup, TComponent, TAccess}"/> instance used to create AccessManager client instances from configuration.</param>
            /// <param name="userHashCodeGenerator">A hash code generator for users.</param>
            /// <param name="groupHashCodeGenerator">A hash code generator for groups.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public ShardClientManagerWithProtectedMembers
            (
                ShardConfigurationSet<TClientConfiguration> initialConfiguration,
                IDistributedAccessManagerAsyncClientFactory<TClientConfiguration, String, String, String, String> clientFactory,
                IHashCodeGenerator<String> userHashCodeGenerator,
                IHashCodeGenerator<String> groupHashCodeGenerator,
                IMetricLogger metricLogger
            )
                : base(initialConfiguration, clientFactory, userHashCodeGenerator, groupHashCodeGenerator, metricLogger)
            {
            }

            /// <summary>
            /// Gets the client and shard description corresponding to a given hash code from the specified tree.
            /// </summary>
            /// <param name="tree">The tree to search.</param>
            /// <param name="hashCode">The hash code to search for.</param>
            /// <returns>The client which handles the given hash code, and its corresponding shard description.</returns>
            public new DistributedClientAndShardDescription GetClientForHashCode(WeightBalancedTree<HashRangeStartClientAndShardDescription> tree, Int32 hashCode)
            {
                return base.GetClientForHashCode(tree, hashCode);
            }
        }

        #endregion
    }
}
