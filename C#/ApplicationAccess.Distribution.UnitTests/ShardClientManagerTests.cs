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
using ApplicationAccess.Distribution.Metrics;
using MoreComplexDataStructures;
using NUnit.Framework;
using NSubstitute;
using ApplicationMetrics;
using ApplicationAccess.Metrics;

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
            var testClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
            var testShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, Int32.MinValue, testClientConfiguration);
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
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[dataElementAndOperation].Contains(testClient));
                Assert.AreEqual(1, testShardClientManager.HashRangeMapToClientMap.Count);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(dataElementAndOperation));
                Assert.AreEqual(1, testShardClientManager.HashRangeMapToClientMap[dataElementAndOperation].Count);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap[dataElementAndOperation].Contains(new HashRangeStartAndClient(Int32.MinValue, testClient)));
                Assert.AreEqual(testClient, testShardClientManager.HashRangeMapToClientMap[dataElementAndOperation].Get(new HashRangeStartAndClient(Int32.MinValue, null)).Client);
                Assert.AreEqual(1, testShardClientManager.CurrentConfiguration.Items.Count());
                Assert.IsTrue(testShardClientManager.CurrentConfiguration.Items.Contains(testShardConfiguration));
            }
        }

        [Test]
        public void Constructor_MultipleShardsAssignedToOneClient()
        {
            var userQueryClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQueryClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
            var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQueryClientConfiguration);
            var userQuery4ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 4, userQueryClientConfiguration);
            var userQuery8ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 8, userQueryClientConfiguration);
            var userQuery12ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 12, userQueryClientConfiguration);
            var userQuery16ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, userQueryClientConfiguration);
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
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(userQuery));
                Assert.AreEqual(5, testShardClientManager.HashRangeMapToClientMap[userQuery].Count);
                Assert.AreEqual(userQueryClient, testShardClientManager.HashRangeMapToClientMap[userQuery].Get(new HashRangeStartAndClient(0, null)).Client);
                Assert.AreEqual(userQueryClient, testShardClientManager.HashRangeMapToClientMap[userQuery].Get(new HashRangeStartAndClient(4, null)).Client);
                Assert.AreEqual(userQueryClient, testShardClientManager.HashRangeMapToClientMap[userQuery].Get(new HashRangeStartAndClient(8, null)).Client);
                Assert.AreEqual(userQueryClient, testShardClientManager.HashRangeMapToClientMap[userQuery].Get(new HashRangeStartAndClient(12, null)).Client);
                Assert.AreEqual(userQueryClient, testShardClientManager.HashRangeMapToClientMap[userQuery].Get(new HashRangeStartAndClient(16, null)).Client);
            }
        }

        [Test]
        public void RefreshConfiguration_ConfigurationNotChanged()
        {
            var testClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var testClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
            var testShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, Int32.MinValue, testClientConfiguration);
            var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>
            (
                new List<ShardConfiguration<AccessManagerRestClientConfiguration>>() { testShardConfiguration }
            );
            var testRefreshClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var testRefreshClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
            var testRefreshShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, Int32.MinValue, testClientConfiguration);
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
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[dataElementAndOperation].Contains(testClient));
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
                var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
                var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
                var userQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userQuery32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5001/");
                var userQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 32, userQuery32ClientConfiguration);
                var userEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5002/");
                var userEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, 0, userEvent0ClientConfiguration);
                var userEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var userEvent32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5003/");
                var userEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, 32, userEvent32ClientConfiguration);
                var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5004/");
                var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
                var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5005/");
                var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
                var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5006/");
                var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
                var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5007/");
                var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
                var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5008/");
                var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
                var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5009/");
                var groupToGroupEvent00ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
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
                    groupToGroupEvent00ShardConfiguration
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
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[userQuery].Contains(userQuery0Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[userQuery].Contains(userQuery32Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(userEvent));
                Assert.AreEqual(2, testShardClientManager.DataElementAndOperationToClientMap[userEvent].Count);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[userEvent].Contains(userEvent0Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[userEvent].Contains(userEvent32Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupQuery));
                Assert.AreEqual(2, testShardClientManager.DataElementAndOperationToClientMap[groupQuery].Count);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[groupQuery].Contains(groupQuery0Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[groupQuery].Contains(groupQuery32Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupEvent));
                Assert.AreEqual(2, testShardClientManager.DataElementAndOperationToClientMap[groupEvent].Count);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[groupEvent].Contains(groupEvent0Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[groupEvent].Contains(groupEvent32Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupToGroupQuery));
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap[groupToGroupQuery].Count);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[groupToGroupQuery].Contains(groupToGroupQuery0Client));
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap.ContainsKey(groupToGroupEvent));
                Assert.AreEqual(1, testShardClientManager.DataElementAndOperationToClientMap[groupToGroupEvent].Count);
                Assert.IsTrue(testShardClientManager.DataElementAndOperationToClientMap[groupToGroupEvent].Contains(groupToGroupEvent0Client));
                // Check contents of 'hashRangeMapToClientMap'
                Assert.AreEqual(6, testShardClientManager.HashRangeMapToClientMap.Count);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(userQuery));
                Assert.AreEqual(2, testShardClientManager.HashRangeMapToClientMap[userQuery].Count);
                Assert.AreEqual(userQuery0Client, testShardClientManager.HashRangeMapToClientMap[userQuery].Get(new HashRangeStartAndClient(0, null)).Client);
                Assert.AreEqual(userQuery32Client, testShardClientManager.HashRangeMapToClientMap[userQuery].Get(new HashRangeStartAndClient(32, null)).Client);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(userEvent));
                Assert.AreEqual(2, testShardClientManager.HashRangeMapToClientMap[userEvent].Count);
                Assert.AreEqual(userEvent0Client, testShardClientManager.HashRangeMapToClientMap[userEvent].Get(new HashRangeStartAndClient(0, null)).Client);
                Assert.AreEqual(userEvent32Client, testShardClientManager.HashRangeMapToClientMap[userEvent].Get(new HashRangeStartAndClient(32, null)).Client);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(groupQuery));
                Assert.AreEqual(2, testShardClientManager.HashRangeMapToClientMap[groupQuery].Count);
                Assert.AreEqual(groupQuery0Client, testShardClientManager.HashRangeMapToClientMap[groupQuery].Get(new HashRangeStartAndClient(0, null)).Client);
                Assert.AreEqual(groupQuery32Client, testShardClientManager.HashRangeMapToClientMap[groupQuery].Get(new HashRangeStartAndClient(32, null)).Client);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(groupEvent));
                Assert.AreEqual(2, testShardClientManager.HashRangeMapToClientMap[groupEvent].Count);
                Assert.AreEqual(groupEvent0Client, testShardClientManager.HashRangeMapToClientMap[groupEvent].Get(new HashRangeStartAndClient(0, null)).Client);
                Assert.AreEqual(groupEvent32Client, testShardClientManager.HashRangeMapToClientMap[groupEvent].Get(new HashRangeStartAndClient(32, null)).Client);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(groupToGroupQuery));
                Assert.AreEqual(1, testShardClientManager.HashRangeMapToClientMap[groupToGroupQuery].Count);
                Assert.AreEqual(groupToGroupQuery0Client, testShardClientManager.HashRangeMapToClientMap[groupToGroupQuery].Get(new HashRangeStartAndClient(0, null)).Client);
                Assert.IsTrue(testShardClientManager.HashRangeMapToClientMap.ContainsKey(groupToGroupEvent));
                Assert.AreEqual(1, testShardClientManager.HashRangeMapToClientMap[groupToGroupEvent].Count);
                Assert.AreEqual(groupToGroupEvent0Client, testShardClientManager.HashRangeMapToClientMap[groupToGroupEvent].Get(new HashRangeStartAndClient(0, null)).Client);
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
                var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
                var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
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
                var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
                var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
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
            var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
            var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
            var userQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5001/");
            var userQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 32, userQuery32ClientConfiguration);
            var userEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5002/");
            var userEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, 0, userEvent0ClientConfiguration);
            var userEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5003/");
            var userEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, 32, userEvent32ClientConfiguration);
            var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5004/");
            var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
            var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5005/");
            var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
            var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5006/");
            var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
            var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5007/");
            var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
            var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5008/");
            var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
            var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5009/");
            var groupToGroupEvent00ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
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
                    groupToGroupEvent00ShardConfiguration
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
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {

                var result = new HashSet<IDistributedAccessManagerAsyncClient<String, String, String, String>>(testShardClientManager.GetAllClients(DataElement.User, Operation.Query));

                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result.Contains(userQuery0Client));
                Assert.IsTrue(result.Contains(userQuery32Client));


                result = new HashSet<IDistributedAccessManagerAsyncClient<String, String, String, String>>(testShardClientManager.GetAllClients(DataElement.Group, Operation.Event));

                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result.Contains(groupEvent0Client));
                Assert.IsTrue(result.Contains(groupEvent32Client));


                result = new HashSet<IDistributedAccessManagerAsyncClient<String, String, String, String>>(testShardClientManager.GetAllClients(DataElement.GroupToGroupMapping, Operation.Query));

                Assert.AreEqual(1, result.Count);
                Assert.IsTrue(result.Contains(groupToGroupQuery0Client));
            }
        }

        [Test]
        public void GetClient()
        {
            IHashCodeGenerator<String> mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            IHashCodeGenerator<String> mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            var userQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5000/");
            var userQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, userQuery0ClientConfiguration);
            var userQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userQuery32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5001/");
            var userQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 32, userQuery32ClientConfiguration);
            var userEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5002/");
            var userEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, 0, userEvent0ClientConfiguration);
            var userEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var userEvent32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5003/");
            var userEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Event, 32, userEvent32ClientConfiguration);
            var groupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5004/");
            var groupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, groupQuery0ClientConfiguration);
            var groupQuery32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupQuery32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5005/");
            var groupQuery32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 32, groupQuery32ClientConfiguration);
            var groupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5006/");
            var groupEvent0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, groupEvent0ClientConfiguration);
            var groupEvent32Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupEvent32ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5007/");
            var groupEvent32ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 32, groupEvent32ClientConfiguration);
            var groupToGroupQuery0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupQuery0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5008/");
            var groupToGroupQuery0ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.GroupToGroupMapping, Operation.Query, 0, groupToGroupQuery0ClientConfiguration);
            var groupToGroupEvent0Client = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            var groupToGroupEvent0ClientConfiguration = new AccessManagerRestClientConfiguration("http://127.0.0.1:5009/");
            var groupToGroupEvent00ShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.GroupToGroupMapping, Operation.Event, 0, groupToGroupEvent0ClientConfiguration);
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
                    groupToGroupEvent00ShardConfiguration
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

                IDistributedAccessManagerAsyncClient<String, String, String, String> result = testShardClientManager.GetClient(DataElement.User, Operation.Query, "user1");

                Assert.AreEqual(userQuery32Client, result);


                mockUserHashCodeGenerator.GetHashCode("group1").Returns(Int32.MaxValue);

                result = testShardClientManager.GetClient(DataElement.Group, Operation.Query, "group1");

                Assert.AreEqual(groupQuery0Client, result);


                mockUserHashCodeGenerator.GetHashCode("group2").Returns(1);

                result = testShardClientManager.GetClient(DataElement.GroupToGroupMapping, Operation.Event, "group2");

                Assert.AreEqual(groupToGroupEvent0Client, result);
            }
        }

        [Test]
        public void GetClientForHashCode()
        {
            var testInitialShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(Enumerable.Empty<ShardConfiguration<AccessManagerRestClientConfiguration>>());
            using (var testShardClientManager = new ShardClientManagerWithProtectedMembers<AccessManagerRestClientConfiguration>(testInitialShardConfigurationSet, mockClientFactory, new StringHashCodeGenerator(), new StringHashCodeGenerator()))
            {
                var testTree = new WeightBalancedTree<HashRangeStartAndClient>();
                var client1 = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                var client5 = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                testTree.Add(new HashRangeStartAndClient(1, client1));
                testTree.Add(new HashRangeStartAndClient(5, client5));

                IDistributedAccessManagerAsyncClient<String, String, String, String> result = testShardClientManager.GetClientForHashCode(testTree, 1);

                Assert.AreEqual(client1, result);


                result = testShardClientManager.GetClientForHashCode(testTree, 2);

                Assert.AreEqual(client5, result);


                result = testShardClientManager.GetClientForHashCode(testTree, 5);

                Assert.AreEqual(client5, result);


                result = testShardClientManager.GetClientForHashCode(testTree, 6);

                Assert.AreEqual(client1, result);


                var clientMin = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                testTree.Add(new HashRangeStartAndClient(Int32.MinValue, clientMin));

                result = testShardClientManager.GetClientForHashCode(testTree, 6);

                Assert.AreEqual(clientMin, result);


                result = testShardClientManager.GetClientForHashCode(testTree, Int32.MinValue);

                Assert.AreEqual(clientMin, result);


                var clientMax = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
                testTree.Add(new HashRangeStartAndClient(Int32.MaxValue, clientMax));

                result = testShardClientManager.GetClientForHashCode(testTree, 6);

                Assert.AreEqual(clientMax, result);


                result = testShardClientManager.GetClientForHashCode(testTree, Int32.MaxValue);

                Assert.AreEqual(clientMax, result);
            }
        }

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
            /// <summary>Maps a <see cref="DataElement"/> and <see cref="Operation"/> to all clients connecting to shards which manage that data element and for operations of that type.</summary>
            public Dictionary<DataElementAndOperation, HashSet<IDistributedAccessManagerAsyncClient<String, String, String, String>>> DataElementAndOperationToClientMap
            {
                get { return dataElementAndOperationToClientMap; }
            }

            /// <summary>Maps a <see cref="DataElement"/> and <see cref="Operation"/> to a tree which stores clients connecting to shards for that element/operation, indexed by the range of hash values that client handles.</summary>
            public Dictionary<DataElementAndOperation, WeightBalancedTree<HashRangeStartAndClient>> HashRangeMapToClientMap
            {
                get { return hashRangeMapToClientMap; }
            }

            /// <summary>The current shard configuration.</summary>
            public ShardConfigurationSet<TClientConfiguration> CurrentConfiguration
            {
                get { return currentConfiguration; }
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
            /// Gets the client corresponding to a given hash code from the specified tree.
            /// </summary>
            /// <param name="tree">The tree to search.</param>
            /// <param name="hashCode">The hash code to search for.</param>
            /// <returns>The client which handles the given hash code.</returns>
            public new IDistributedAccessManagerAsyncClient<String, String, String, String> GetClientForHashCode(WeightBalancedTree<HashRangeStartAndClient> tree, Int32 hashCode)
            {
                return base.GetClientForHashCode(tree, hashCode);
            }
        }

        #endregion
    }
}
